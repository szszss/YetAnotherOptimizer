using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using static YaOpt.Helpers.ListerThingsIndexer;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch]
	internal static class Verse_ListerThings_Remove
	{
		private const int INDEX_TYPE_DEF = -1;

		static MethodBase TargetMethod()
		{
			if (YaOptGlobal.HasMod("Vortex.Kingfisher"))
			{
				return AccessTools.Method(
					AccessTools.TypeByName("Kingfisher.Features.Things.ListerThingsRewrite"),
					"Remove");
			}
			else
			{
				return AccessTools.Method(typeof(ListerThings), nameof(ListerThings.Remove));
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var count = 0;
			var inited = false;
			var hasKingfisher = YaOptGlobal.HasMod("Vortex.Kingfisher");
			var localThingRequestGroup = hasKingfisher ? 6 : 4;
			var localUse = generator.DeclareLocal(typeof(ListerThingsUse));
			var localIndexer = generator.DeclareLocal(typeof(ListerThingsIndexer));
			var localRecord = generator.DeclareLocal(typeof(ThingRecord));

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
				else if (IsRemove<Thing>(instruction))
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
					if (hasKingfisher)
						yield return new CodeInstruction(OpCodes.Pop);

					count++;
					continue;
				}
				else if (IsRemove<IHaulSource>(instruction))
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
					if (hasKingfisher)
						yield return new CodeInstruction(OpCodes.Pop);
					continue;
				}

				yield return instruction;
			}
		}

		private static bool IsRemove<T>(CodeInstruction instruction)
		{
			if (instruction.operand is MethodInfo methodInfo)
			{
				if (methodInfo.Name == "Remove")
				{
					return methodInfo.DeclaringType == typeof(List<T>);
				}
				// Compatible with Kingfisher
				if (methodInfo.Name == "RemoveFromTail")
				{
					return methodInfo.IsGenericMethod && methodInfo.GetGenericArguments()[0] == typeof(T);
				}
			}
			return false;
		}

		static bool RemoveFromThingList(List<Thing> list, Thing thing,
			ListerThingsIndexer indexer, ThingRecord record, ListerThingsUse use, int indexType)
		{
			int index;

			if (use != ListerThingsUse.Global)
			{
				// For the ListerThings of a Region, we use a reverse search.
				// The existence time of things in the ListerThings of a Region is polarized.
				// They either exist for a long time or will be removed soon.
				// The latter are usually located at the end of the array.
				list.ReverseRemove(thing);
				return true;
			}

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
			int index;

			if (use != ListerThingsUse.Global)
			{
				index = list.LastIndexOf(haul);
				if (index >= 0)
					list.RemoveAt(index);
				return true;
			}

			index = record.HaulIndex;
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