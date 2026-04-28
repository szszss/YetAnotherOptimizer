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
	internal static class MultiTargets_CurPawn_Combat
	{
		[ThreadStatic]
		public static Pawn CurPawn;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(VanillaExpandedFramework_Toils_Combat_FollowAndMeleeAttack_Patch),
				"Prefix");
			yield return AccessTools.Method(
				typeof(VanillaExpandedFramework_Toils_Combat_FollowAndMeleeAttack_Patch),
				"Finalizer");
			yield return AccessTools.Method(
				typeof(VanillaExpandedFramework_Toils_Combat_FollowAndMeleeAttack_Patch),
				"TryOverrideDestinationAndPathMode");
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
						typeof(MultiTargets_CurPawn_Combat),
						nameof(CurPawn));
				}
				else if (instruction.StoresField("curPawn", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_CurPawn_Combat),
						nameof(CurPawn));
				}
				yield return instruction;
			}
		}
	}
}