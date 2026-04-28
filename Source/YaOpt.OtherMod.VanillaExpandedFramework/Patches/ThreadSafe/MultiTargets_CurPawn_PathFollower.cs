using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using VEF.Weapons;
using Verse;
using YaOpt.Helpers;
using YaOpt.Patches.Early;

namespace YaOpt.OtherMod.VanillaExpandedFramework.Patches.ThreadSafe
{
	// TODO: Dirty hack. Mono JIT inlines Prefix and Finalizer of VEF, so modifications to
	// them must be made before the VEF patcher runs.
	[EarlyPatch]
	[HarmonyPatch]
	internal static class MultiTargets_CurPawn_PathFollower
	{
		[ThreadStatic]
		public static Pawn CurPawn;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(VanillaExpandedFramework_Pawn_PathFollower_AtDestinationPosition_Patch),
				"Prefix");
			yield return AccessTools.Method(
				typeof(VanillaExpandedFramework_Pawn_PathFollower_AtDestinationPosition_Patch),
				"Finalizer");
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
						typeof(MultiTargets_CurPawn_PathFollower),
						nameof(CurPawn));
				}
				else if (instruction.StoresField("curPawn", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_CurPawn_PathFollower),
						nameof(CurPawn));
				}
				yield return instruction;
			}
		}
	}
}