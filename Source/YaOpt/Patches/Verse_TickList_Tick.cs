using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Executes parallel pawn tick prediction and processes pawns from dedicated bucket.
	/// Also includes optimizations for thing removal.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(TickList))]
	[HarmonyPatch(nameof(TickList.Tick))]
	internal static class Verse_TickList_Tick
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled ||
				   YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var ppt = YaOptGlobal.Settings.OptParallelPawnTick.Enabled;
			var tr = YaOptGlobal.Settings.OptFastListerRemove.Enabled;

			if (ppt)
			{
				// Register/Unregister pawns to ParallelTickerManager
				var label = generator.DefineLabel();
				// if (tickType == TickerType.Normal) {
				yield return CodeInstruction.LoadArgument(0);
				yield return CodeInstruction.LoadField(typeof(TickList), "tickType");
				yield return new CodeInstruction(OpCodes.Ldc_I4_1); // TickerType.Normal
				yield return new CodeInstruction(OpCodes.Bne_Un_S, label);
				//   ParallelPawnTickManager.AddThings(thingsToRegister);
				yield return CodeInstruction.LoadArgument(0);
				yield return CodeInstruction.LoadField(typeof(TickList), "thingsToRegister");
				yield return CodeInstruction.Call(
					typeof(ParallelPawnTickManager), nameof(ParallelPawnTickManager.AddThings));
				//   ParallelPawnTickManager.RemoveThings(thingsToDeregister);
				yield return CodeInstruction.LoadArgument(0);
				yield return CodeInstruction.LoadField(typeof(TickList), "thingsToDeregister");
				yield return CodeInstruction.Call(
					typeof(ParallelPawnTickManager), nameof(ParallelPawnTickManager.RemoveThings));
				// }
				yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
			}

			var list = instructions.ToList();
			list.RemoveLast(); // remove Ret
			foreach (var instruction in list)
			{
				// Replace list.Remove with MiscHelper.ReverseRemove
				if (tr && instruction.Calls("Remove"))
				{
					yield return CodeInstruction.Call(
						typeof(MiscHelper),
						nameof(MiscHelper.ReverseRemove), null, new[] { typeof(Thing) });
					continue;
				}
				yield return instruction;
			}

			if (ppt)
			{
				// Tick all pawns
				var label = generator.DefineLabel();
				// if (tickType == TickerType.Normal) {
				yield return CodeInstruction.LoadArgument(0);
				yield return CodeInstruction.LoadField(typeof(TickList), "tickType");
				yield return new CodeInstruction(OpCodes.Ldc_I4_1); // TickerType.Normal
				yield return new CodeInstruction(OpCodes.Bne_Un_S, label);
				//   TickPawns(thingLists);
				yield return CodeInstruction.LoadArgument(0);
				yield return CodeInstruction.LoadField(typeof(TickList), "thingLists");
				yield return CodeInstruction.Call(
					typeof(Verse_TickList_Tick), nameof(TickPawns));
				// }
				yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
			}

			yield return new CodeInstruction(OpCodes.Ret);
		}

		private static void TickPawns(List<List<Thing>> thingLists)
		{
			if (thingLists.Count > 1)
			{
				ParallelPawnTickManager.ParellellyTickPawns();
				foreach (var thing in thingLists[1])
				{
					if (thing.Destroyed)
						continue;
					try
					{
						thing.DoTick();
					}
					catch (Exception ex)
					{
						string text = (thing.Spawned ? $" (at {thing.Position})" : "");
						if (Prefs.DevMode)
						{
							Log.Error($"Exception ticking {thing.ToStringSafe()}{text}: {ex}");
						}
						else
						{
							Log.ErrorOnce(
								$"Exception ticking {thing.ToStringSafe()}{text}. " +
								$"Suppressing further errors. Exception: {ex}", thing.thingIDNumber ^ 576876901);
						}
					}
				}
			}
			else
			{
				Log.ErrorOnce("Can't find pawn list from TickList. " +
							  "Pawns won't be ticked.", typeof(Verse_TickList_Tick).GetHashCode());
			}
		}
	}
}