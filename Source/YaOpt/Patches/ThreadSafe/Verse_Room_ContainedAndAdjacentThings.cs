using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch]
	[HarmonyAfter("bs.performance")]
	internal static class Verse_Room_ContainedAndAdjacentThings
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.PropertyGetter(typeof(Room), nameof(Room.ContainedAndAdjacentThings));
			if (YaOptGlobal.HasType("PerformanceFish.RoomOptimizations/ContainedAndAdjacentThings_Patch"))
			{
				yield return AccessTools.Method(
					AccessTools.TypeByName("PerformanceFish.RoomOptimizations/ContainedAndAdjacentThings_Patch"),
					"ContainedAndAdjacentThings_Replacement");
				yield return AccessTools.Method(
					AccessTools.TypeByName("PerformanceFish.RoomOptimizations/ContainedAndAdjacentThings_Patch"),
					"RefreshContainedAndAdjacentThings");
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// If Performance Fish has already replaced this method, then just skip. 
			var list = instructions.ToList();
			if (list.Any(instruction => instruction.Calls("ContainedAndAdjacentThings_Replacement")))
			{
				foreach (var instruction in list)
				{
					yield return instruction;
				}
				yield break;
			}

			var localSet = generator.DeclareLocal(typeof(HashSet<Thing>));
			var localList = generator.DeclareLocal(typeof(List<Thing>));
			var fieldSet = AccessTools.Field(typeof(Room), "uniqueContainedThingsSet");
			var fieldList = AccessTools.Field(typeof(Room), "uniqueContainedThings");
			// var localSet = TransientPool.BorrowIfNotMainThread(this.uniqueContainedThingsSet);
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(Room), "uniqueContainedThingsSet");
			yield return CodeInstruction.Call(
				typeof(TransientPool<HashSet<Thing>>),
				nameof(TransientPool<HashSet<Thing>>.BorrowIfNotMainThread));
			yield return CodeInstruction.StoreLocal(localSet.LocalIndex);
			// var localList = TransientPool.BorrowIfNotMainThread(this.uniqueContainedThings);
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(Room), "uniqueContainedThings");
			yield return CodeInstruction.Call(
				typeof(TransientPool<List<Thing>>),
				nameof(TransientPool<List<Thing>>.BorrowIfNotMainThread));
			yield return CodeInstruction.StoreLocal(localList.LocalIndex);

			foreach (var instruction in list)
			{
				// replace this.uniqueContainedThingsSet with localSet
				if (instruction.LoadsField(fieldSet))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(localSet.LocalIndex);
					continue;
				}
				// replace this.uniqueContainedThings with localList
				if (instruction.LoadsField(fieldList))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(localList.LocalIndex);
					continue;
				}
				yield return instruction;
			}
		}
	}
}