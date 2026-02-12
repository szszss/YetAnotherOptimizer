using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse.AI;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	/// </summary>
	[HarmonyPatch(typeof(Pawn_JobTracker))]
	[HarmonyPatch(nameof(Pawn_JobTracker.JobTrackerTickInterval))]
	internal static class Verse_AI_Pawn_JobTracker_JobTrackerTickInterval
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var found = false;
			foreach (var instruction in instructions)
			{
				yield return instruction;

				// Change if (this.pawn.IsHashIntervalTick(30, delta))
				// To if (this.pawn.IsHashIntervalTick(30, delta) && JobPredictor.ShouldCheckConstantJob(this.pawn))
				if (!found && (instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S))
				{
					found = true;
					var label = instruction.operand;
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(typeof(Pawn_JobTracker), "pawn");
					yield return CodeInstruction.Call(
						typeof(JobPredictor),
						nameof(JobPredictor.ShouldCheckConstantJob));
					yield return new CodeInstruction(OpCodes.Brfalse, label);
				}
			}
		}
	}
}