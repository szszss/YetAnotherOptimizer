using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using VEF.Genes;
using Verse;
using YaOpt.Helpers;
using YaOpt.Patches.Early;

namespace YaOpt.OtherMod.VanillaExpandedFramework.Patches.ThreadSafe
{
	// TODO: Dirty hack. Mono JIT inlines Prefix and Finalizer of VEF, so modifications to
	// them must be made before the VEF patcher runs.
	[EarlyPatch]
	[HarmonyPatch]
	internal static class MultiTargets_CurPawn_Vomit
	{
		[ThreadStatic]
		public static Pawn CurPawn;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(VanillaExpandedFramework_JobDriver_Vomit_MakeNewToils_Patch),
				"StorePawn");
			yield return AccessTools.Method(
				typeof(VanillaExpandedFramework_JobDriver_Vomit_MoveNext_Patch),
				"GetVomitEffecter");
			yield return AccessTools.Method(
				typeof(VanillaExpandedFramework_JobDriver_Vomit_MakeNewToils_Transpiler_Patch),
				"GetVomitFilth");
		}

		static bool Prepare()
		{
			return true;
			//return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("curPawn", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_CurPawn_Vomit),
						nameof(CurPawn));
				}
				else if (instruction.StoresField("curPawn", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_CurPawn_Vomit),
						nameof(CurPawn));
				}
				yield return instruction;
			}
		}
	}
}