using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using static YaOpt.Helpers.ListerThingsIndexer;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(ListerThings))]
	[HarmonyPatch(nameof(ListerThings.Remove))]
	internal static class Verse_ListerThings_Remove
	{
		private const int INDEX_TYPE_DEF = -1;

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			const int localThingRequestGroup = 4;
			var count = 0;
			var inited = false;
			var localUse = generator.DeclareLocal(typeof(ListerThingsUse));
			var localIndexer = generator.DeclareLocal(typeof(ListerThingsIndexer));
			var localRecord = generator.DeclareLocal(typeof(ThingRecord));
			var methodListRemoveThing = AccessTools.Method(typeof(List<Thing>), "Remove");
			var methodListRemoveHaul = AccessTools.Method(typeof(List<IHaulSource>), "Remove");

			foreach (var instruction in instructions)
			{
				if (!inited && instruction.Calls("TryGetValue"))
				{
					inited = true;
					// var use = this.use;
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(typeof(ListerThings), nameof(ListerThings.use));
					yield return CodeInstruction.StoreLocal(localUse.LocalIndex);
					// var indexer = ListerThingsIndexer.GetListerThingsIndex(this);
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.Call(
						typeof(ListerThingsIndexer),
						nameof(GetListerThingsIndex));
					yield return CodeInstruction.StoreLocal(localIndexer.LocalIndex);
					// var record = indexer.GetThingRecord(thing, use);
					yield return CodeInstruction.LoadLocal(localIndexer.LocalIndex);
					yield return CodeInstruction.LoadArgument(1);
					yield return CodeInstruction.LoadLocal(localUse.LocalIndex);
					yield return CodeInstruction.Call(
						typeof(ListerThingsIndexer),
						nameof(ListerThingsIndexer.GetThingRecord));
					yield return CodeInstruction.StoreLocal(localRecord.LocalIndex);
					// indexer.Remove(thing, use);
					yield return CodeInstruction.LoadLocal(localIndexer.LocalIndex);
					yield return CodeInstruction.LoadArgument(1);
					yield return CodeInstruction.LoadLocal(localUse.LocalIndex);
					yield return CodeInstruction.Call(
						typeof(ListerThingsIndexer),
						nameof(ListerThingsIndexer.Remove));
				}
				else if (instruction.Calls(methodListRemoveThing))
				{
					// Replace
					// list.Remove(thing);
					// to
					// RemoveFromThingList(list, thing, indexer, record, use, indexType);
					yield return CodeInstruction.LoadLocal(localIndexer.LocalIndex);
					yield return CodeInstruction.LoadLocal(localRecord.LocalIndex);
					yield return CodeInstruction.LoadLocal(localUse.LocalIndex);
					if (count == 0)
					{
						// indexType = INDEX_TYPE_DEF
						yield return new CodeInstruction(OpCodes.Ldc_I4, INDEX_TYPE_DEF);
					}
					else
					{
						// indexType = thingRequestGroup
						yield return CodeInstruction.LoadLocal(localThingRequestGroup);
					}
					yield return CodeInstruction.Call(
						typeof(Verse_ListerThings_Remove), nameof(RemoveFromThingList));
					count++;
					continue;
				}
				else if (instruction.Calls(methodListRemoveHaul))
				{
					// Replace
					// list.Remove(haulSources);
					// to
					// RemoveFromHaulList(list, haulSources, indexer, record, use);
					yield return CodeInstruction.LoadLocal(localIndexer.LocalIndex);
					yield return CodeInstruction.LoadLocal(localRecord.LocalIndex);
					yield return CodeInstruction.LoadLocal(localUse.LocalIndex);
					yield return CodeInstruction.Call(
						typeof(Verse_ListerThings_Remove), nameof(RemoveFromHaulList));
					continue;
				}

				yield return instruction;
			}
		}

		static bool RemoveFromThingList(List<Thing> list, Thing thing,
			ListerThingsIndexer indexer, ThingRecord record, ListerThingsUse use, int indexType)
		{
			if (use != ListerThingsUse.Global)
			{
				list.Remove(thing);
				return true;
			}

			int index;

			if (indexType == INDEX_TYPE_DEF)
			{
				index = record.DefIndex;
				record.DefIndex = -1;
			}
			else
			{
				index = record.GroupIndex[indexType];
				record.GroupIndex[indexType] = -1;
			}
			var listCount = list.Count;

			if (index < 0 || index >= listCount)
			{
				YaOptMod.Error($"Invalid index: {index} for thing {thing}. Fallback to the original path.");
				list.Remove(thing);
				return true;
			}
			if (index == listCount - 1)
			{
				if (list[index] == thing)
				{
					list.RemoveAt(index);
					return true;
				}
				YaOptMod.Error($"Thing does not match its index: {index} for thing {thing}. " +
							   "Fallback to the original path.");
				list.Remove(thing);
				return true;
			}
			var swapThing = list[listCount - 1];
			var swapThingRecord = indexer.GetThingRecord(swapThing, use);
			list.RemoveAt(listCount - 1);
			list[index] = swapThing;
			if (indexType == INDEX_TYPE_DEF)
			{
				swapThingRecord.DefIndex = index;
			}
			else
			{
				swapThingRecord.GroupIndex[indexType] = index;
			}
			return true;
		}


		static bool RemoveFromHaulList(List<IHaulSource> list, IHaulSource haul,
			ListerThingsIndexer indexer, ThingRecord record, ListerThingsUse use)
		{
			if (use != ListerThingsUse.Global)
			{
				list.Remove(haul);
				return true;
			}

			var index = record.HaulIndex;
			record.HaulIndex = -1;
			var listCount = list.Count;

			if (index < 0 || index >= listCount)
			{
				YaOptMod.Error($"Invalid index: {index} for thing {haul}. Fallback to the original path.");
				list.Remove(haul);
				return true;
			}
			if (index == listCount - 1)
			{
				if (list[index] == haul)
				{
					list.RemoveAt(index);
					return true;
				}
				YaOptMod.Error($"Thing does not match its index: {index} for thing {haul}. " +
							   "Fallback to the original path.");
				list.Remove(haul);
				return true;
			}
			var swapThing = list[listCount - 1];
			var swapThingRecord = indexer.GetThingRecord((Thing)swapThing, use);
			list.RemoveAt(listCount - 1);
			list[index] = swapThing;
			swapThingRecord.HaulIndex = index;
			return true;
		}
	}
}