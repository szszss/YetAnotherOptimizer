using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;
using static YaOpt.Helpers.ListerThingsIndexer;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(ListerThings))]
	[HarmonyPatch(nameof(ListerThings.Add))]
	internal static class Verse_ListerThings_Add
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
			var methodListAddThing = AccessTools.Method(typeof(List<Thing>), "Add");
			var methodListAddHaul = AccessTools.Method(typeof(List<IHaulSource>), "Add");

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
					// var record = indexer.Add(thing, use);
					yield return CodeInstruction.LoadLocal(localIndexer.LocalIndex);
					yield return CodeInstruction.LoadArgument(1);
					yield return CodeInstruction.LoadLocal(localUse.LocalIndex);
					yield return CodeInstruction.Call(
						typeof(ListerThingsIndexer),
						nameof(ListerThingsIndexer.Add));
					yield return CodeInstruction.StoreLocal(localRecord.LocalIndex);
				}
				else if (instruction.Calls(methodListAddThing))
				{
					// Replace
					// list.Add(thing);
					// to
					// AddToThingList(list, thing, record, use, indexType);
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
							typeof(Verse_ListerThings_Add), nameof(AddToThingList));
					count++;
					continue;
				}
				else if (instruction.Calls(methodListAddHaul))
				{
					// Replace
					// list.Add(haulSources);
					// to
					// AddToHaulList(list, haulSources, record, use);
					yield return CodeInstruction.LoadLocal(localRecord.LocalIndex);
					yield return CodeInstruction.LoadLocal(localUse.LocalIndex);
					yield return CodeInstruction.Call(
						typeof(Verse_ListerThings_Add), nameof(AddToHaulList));
					continue;
				}

				yield return instruction;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void AddToThingList(List<Thing> list, Thing thing,
			ThingRecord record, ListerThingsUse use, int indexType)
		{
			list.Add(thing);
			if (use != ListerThingsUse.Global)
				return;
			var index = list.Count - 1;
			if (indexType == INDEX_TYPE_DEF)
			{
				record.DefIndex = index;
			}
			else
			{
				record.GroupIndex[indexType] = index;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void AddToHaulList(List<IHaulSource> list, IHaulSource haulSource,
			ThingRecord record, ListerThingsUse use)
		{
			list.Add(haulSource);
			if (use != ListerThingsUse.Global)
				return;
			var index = list.Count - 1;
			record.HaulIndex = index;
		}
	}
}