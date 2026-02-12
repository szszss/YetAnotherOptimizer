using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelJobGiver"/>
	/// </summary>
	[HarmonyPatch(typeof(JobGiver_Work))]
	[HarmonyPatch(nameof(JobGiver_Work.TryIssueJobPackage))]
	internal static class RimWorld_JobGiver_Work_TryIssueJobPackage
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var label = generator.DefineLabel();
			foreach (var instruction in instructions)
			{
				/*
				 * if (!emergency && !ParallelJobGiver.Running)
				 *   return ParallelJobGiver.ParellellyIssueJobPackage(this, pawn, list);
				 */
				if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int i && i == -999)
				{
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(typeof(JobGiver_Work), nameof(JobGiver_Work.emergency));
					yield return new CodeInstruction(OpCodes.Brtrue_S, label);
					yield return CodeInstruction.LoadField(typeof(ParallelJobGiver), nameof(ParallelJobGiver.Running));
					yield return new CodeInstruction(OpCodes.Brtrue_S, label);
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadArgument(1);
					yield return CodeInstruction.LoadLocal(1);
					yield return CodeInstruction.Call(
						typeof(ParallelJobGiver),
						nameof(ParallelJobGiver.ParellellyIssueJobPackage));
					yield return new CodeInstruction(OpCodes.Ret);
					instruction.WithLabels(label);
				}
				yield return instruction;
			}
		}
	}
}