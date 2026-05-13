using HarmonyLib;
using System.Collections.Generic;
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
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
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

			foreach (var instruction in instructions)
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
				// Experimental fix for crash
				if (instruction.Calls("ContainedAndAdjacentThings_Replacement"))
				{
					yield return CodeInstruction.LoadLocal(localSet.LocalIndex);
					yield return CodeInstruction.LoadLocal(localList.LocalIndex);
					instruction.operand = AccessTools.Method(
						typeof(Verse_Room_ContainedAndAdjacentThings),
						nameof(ContainedAndAdjacentThings));
				}
				yield return instruction;
			}
		}

		static List<Thing> ContainedAndAdjacentThings(Room instance,
			HashSet<Thing> uniqueContainedThingsSet,
			List<Thing> uniqueContainedThings)
		{
			uniqueContainedThingsSet.Clear();
			uniqueContainedThings.Clear();
			var regions = instance.Regions;
			for (var i = 0; i < regions.Count; i++)
			{
				var allThings = regions[i].ListerThings.AllThings;
				if (allThings != null)
				{
					for (var j = 0; j < allThings.Count; j++)
					{
						var thing = allThings[j];
						if (uniqueContainedThingsSet.Add(thing))
						{
							uniqueContainedThings.Add(thing);
						}
					}
				}
			}
			uniqueContainedThingsSet.Clear();
			return uniqueContainedThings;
		}
	}
}