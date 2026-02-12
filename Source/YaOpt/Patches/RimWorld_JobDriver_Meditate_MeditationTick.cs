using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptMeditationTick"/>
	/// </summary>
	[HarmonyPatch(typeof(JobDriver_Meditate))]
	[HarmonyPatch("MeditationTick")]
	internal static class RimWorld_JobDriver_Meditate_MeditationTick
	{
		public const int RATIO = 100;

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptMeditationTick.Enabled;
		}

		static bool Prefix(JobDriver_Meditate __instance, Sustainer ___sustainer)
		{
			if (___sustainer != null && !___sustainer.Ended)
			{
				___sustainer.Maintain();
			}
			return __instance.pawn.IsHashIntervalTick(RATIO);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			CodeInstruction lastCode = null;
			foreach (var instruction in instructions)
			{
				// this.pawn.skills.Learn(SkillDefOf.Intellectual, 0.018000001f, false, false);
				// change 0.018 to 1.8
				if (instruction.opcode == OpCodes.Ldc_R4 
				    && Mathf.Approximately(Convert.ToSingle(instruction.operand), 0.018f))
				{
					instruction.operand = 0.018f * RATIO;
					YaOptMod.Log("YaOpt Meditation Patched1");
				}
				// change GainComfortFromCellIfPossible(1, false) to GainComfortFromCellIfPossible(100, false)
				// change JoyTickCheckEnd(this.pawn, 1, ... to JoyTickCheckEnd(this.pawn, 100
				// change GainPsyfocus_NewTemp(1, this.Focus.Thing) to GainPsyfocus_NewTemp(100, this.Focus.Thing);
				else if (instruction.opcode == OpCodes.Ldc_I4_1)
				{
					if (lastCode != null && lastCode.opcode == OpCodes.Ldfld)
					{
						YaOptMod.Log("YaOpt Meditation Patched2");
						yield return new CodeInstruction(OpCodes.Ldc_I4, RATIO);
						continue;
					}
				}
				lastCode = instruction;
				yield return instruction;
			}
		}
	}
}