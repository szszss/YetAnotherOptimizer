using System;
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
		static MethodBase TargetMethod()
		{
			if (YaOptGlobal.HasMod("Vortex.Kingfisher"))
			{
				return AccessTools.Method(AccessTools.TypeByName("Kingfisher.Features.ListerThingsRewrite"), "Remove");
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
						yield return new CodeInstruction(OpCodes.Ldc_I4, ListerThingsHelper.INDEX_TYPE_DEF);
					}
					else
					{
						// indexType = thingRequestGroup
						yield return CodeInstruction.LoadLocal(localThingRequestGroup);
					}
					yield return CodeInstruction.Call(
						typeof(ListerThingsHelper), nameof(ListerThingsHelper.RemoveFromThingList));
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
						typeof(ListerThingsHelper), nameof(ListerThingsHelper.RemoveFromHaulList));
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
	}
}