using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.WhileYouAreUp
{
	/// <summary>
	/// When PUAH is installed, WhileYoureUp modifies the haul priority on the map
	/// before each execution of WorkGiver_HaulToInventory,
	/// causing allHaulSourcesInOrder to be reordered.
	/// During this process, any code that iterates over allHaulSourcesInOrder
	/// will encounter System.InvalidOperationException: Collection was modified.
	/// </summary>
	/// <remarks>
	/// This patch changes the Notify_HaulDestinationChangedPriority update to
	/// a copy-on-write operation. To reduce GC pressure, it uses a circular queue to
	/// store pre-allocated Lists. One buffer only contains two Lists because
	/// WhileYoureUp only calls Notify_HaulDestinationChangedPriority twice per job giving.
	/// While this isn't a robust fix, it JustWorks.
	/// </remarks>
	[HarmonyPatch(typeof(HaulDestinationManager))]
	[HarmonyPatch(nameof(HaulDestinationManager.Notify_HaulDestinationChangedPriority))]
	internal static class RimWorld_HaulDestinationManager_Notify_HaulDestinationChangedPriority
	{
		private static SpinLock _spinLock = new SpinLock();

		private static readonly Queue<List<IHaulDestination>> _allHaulDestinationsInOrderQueue =
			new Queue<List<IHaulDestination>>(4);

		private static readonly Queue<List<IHaulSource>> _allHaulSourcesInOrderQueue =
			new Queue<List<IHaulSource>>(4);

		private static readonly Queue<List<SlotGroup>> _allGroupsInOrder =
			new Queue<List<SlotGroup>>(4);

		private static List<IHaulDestination> _newHaulDestinationsInOrderQueue;
		private static List<IHaulSource> _newHaulSourcesInOrderQueue;
		private static List<SlotGroup> _newGroupsInOrder;

		static RimWorld_HaulDestinationManager_Notify_HaulDestinationChangedPriority()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		static void ClearCache()
		{
			foreach (var list in _allHaulDestinationsInOrderQueue)
			{
				list.Clear();
			}
			foreach (var list in _allHaulSourcesInOrderQueue)
			{
				list.Clear();
			}
			foreach (var list in _allGroupsInOrder)
			{
				list.Clear();
			}
		}

		static bool Prepare()
		{
			var result = YaOptGlobal.Settings.OptParallelWorkGiver.Enabled &&
						 YaOptGlobal.HasType("PickUpAndHaul.WorkGiver_HaulToInventory") &&
						 YaOptGlobal.HasType("WhileYoureUp.Mod");
			if (result && _allHaulDestinationsInOrderQueue.Count == 0)
			{
				_allHaulDestinationsInOrderQueue.Enqueue(new List<IHaulDestination>());
				_allHaulDestinationsInOrderQueue.Enqueue(new List<IHaulDestination>());
				_allHaulSourcesInOrderQueue.Enqueue(new List<IHaulSource>());
				_allHaulSourcesInOrderQueue.Enqueue(new List<IHaulSource>());
				_allGroupsInOrder.Enqueue(new List<SlotGroup>());
				_allGroupsInOrder.Enqueue(new List<SlotGroup>());
			}
			return result;
		}

		static void Prefix(out bool __state,
			List<IHaulDestination> ___allHaulDestinationsInOrder,
			List<IHaulSource> ___allHaulSourcesInOrder,
			List<SlotGroup> ___allGroupsInOrder)
		{
			__state = false;
			_spinLock.Enter(ref __state);

			_newHaulDestinationsInOrderQueue = _allHaulDestinationsInOrderQueue.Dequeue();
			_newHaulSourcesInOrderQueue = _allHaulSourcesInOrderQueue.Dequeue();
			_newGroupsInOrder = _allGroupsInOrder.Dequeue();

			_newHaulDestinationsInOrderQueue.Clear();
			_newHaulDestinationsInOrderQueue.AddRangeFast(___allHaulDestinationsInOrder);
			_newHaulSourcesInOrderQueue.Clear();
			_newHaulSourcesInOrderQueue.AddRangeFast(___allHaulSourcesInOrder);
			_newGroupsInOrder.Clear();
			_newGroupsInOrder.AddRangeFast(___allGroupsInOrder);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("allHaulDestinationsInOrder"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadField(
						typeof(RimWorld_HaulDestinationManager_Notify_HaulDestinationChangedPriority),
						nameof(_newHaulDestinationsInOrderQueue));
					continue;
				}
				else if (instruction.LoadsField("allGroupsInOrder"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadField(
						typeof(RimWorld_HaulDestinationManager_Notify_HaulDestinationChangedPriority),
						nameof(_newGroupsInOrder));
					continue;
				}
				else if (instruction.LoadsField("allHaulSourcesInOrder"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadField(
						typeof(RimWorld_HaulDestinationManager_Notify_HaulDestinationChangedPriority),
						nameof(_newHaulSourcesInOrderQueue));
					continue;
				}
				yield return instruction;
			}
		}

		static void Postfix(ref List<IHaulDestination> ___allHaulDestinationsInOrder,
			ref List<IHaulSource> ___allHaulSourcesInOrder,
			ref List<SlotGroup> ___allGroupsInOrder)
		{
			_allHaulDestinationsInOrderQueue.Enqueue(___allHaulDestinationsInOrder);
			_allHaulSourcesInOrderQueue.Enqueue(___allHaulSourcesInOrder);
			_allGroupsInOrder.Enqueue(___allGroupsInOrder);
			___allHaulDestinationsInOrder = _newHaulDestinationsInOrderQueue;
			___allHaulSourcesInOrder = _newHaulSourcesInOrderQueue;
			___allGroupsInOrder = _newGroupsInOrder;
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				_spinLock.Exit();
		}
	}
}