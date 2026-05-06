using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Executes parallel pawn tick prediction.
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

			var parellellyTickInserted = false;

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

			foreach (var instruction in instructions)
			{
				// Replace list.Remove with MiscHelper.ReverseRemove
				if (tr && instruction.Calls("Remove"))
				{
					yield return CodeInstruction.Call(
						typeof(MiscHelper),
						nameof(MiscHelper.ReverseRemove), null, new[] { typeof(Thing) });
					continue;
				}
				// Call ParallelTickerManager.ParellellyTickPawns() if this is a normal TickList
				if (ppt && instruction.LoadsField("fastEcology", true))
				{
					parellellyTickInserted = true;
					var label = generator.DefineLabel();
					// if (tickType == TickerType.Normal) {
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(typeof(TickList), "tickType");
					yield return new CodeInstruction(OpCodes.Ldc_I4_1); // TickerType.Normal
					yield return new CodeInstruction(OpCodes.Bne_Un_S, label);
					//   ParallelPawnTickManager.ParellellyTickPawns();
					yield return CodeInstruction.Call(
						typeof(ParallelPawnTickManager), nameof(ParallelPawnTickManager.ParellellyTickPawns));
					// }
					yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
				}
				yield return instruction;
			}

			if (ppt && !parellellyTickInserted)
			{
				throw new Exception("Unable to find the insertion point for " +
									"ParallelPawnTickManager.ParellellyTickPawns in TickList.Tick");
			}
		}
	}
}
