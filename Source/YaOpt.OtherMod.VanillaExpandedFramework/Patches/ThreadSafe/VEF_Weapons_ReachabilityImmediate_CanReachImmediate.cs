using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using VEF.Weapons;
using YaOpt.Patches.Early;

namespace YaOpt.OtherMod.VanillaExpandedFramework.Patches.ThreadSafe
{
	// TODO: Dirty hack. Mono JIT inlines Prefix and Finalizer of VEF, so modifications to
	// them must be made before the VEF patcher runs.
	[EarlyPatch]
	[HarmonyPatch(typeof(VanillaExpandedFramework_ReachabilityImmediate_CanReachImmediate_Patch))]
	[HarmonyPatch(nameof(VanillaExpandedFramework_ReachabilityImmediate_CanReachImmediate_Patch.Postfix))]
	internal static class VEF_Weapons_ReachabilityImmediate_CanReachImmediate
	{
		static bool Prepare()
		{
			return true;
			//return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo field &&
					field.Name == "curPawn")
				{
					if (field.DeclaringType ==
						typeof(VanillaExpandedFramework_AttackTargetFinder_FindBestReachableMeleeTarget_Patch))
					{
						instruction.operand = AccessTools.Field(
							typeof(MultiTargets_CurPawn_AttackTargetFinder),
							nameof(MultiTargets_CurPawn_AttackTargetFinder.CurPawn));
					}
					else if (field.DeclaringType ==
							 typeof(VanillaExpandedFramework_Toils_Combat_FollowAndMeleeAttack_Patch))
					{
						instruction.operand = AccessTools.Field(
							typeof(MultiTargets_CurPawn_Combat),
							nameof(MultiTargets_CurPawn_Combat.CurPawn));
					}
					else if (field.DeclaringType ==
							 typeof(VanillaExpandedFramework_JobGiver_ConfigurableHostilityResponse_TryGetAttackNearbyEnemyJob_Patch))
					{
						instruction.operand = AccessTools.Field(
							typeof(MultiTargets_CurPawn_ConfigurableHostilityResponse),
							nameof(MultiTargets_CurPawn_ConfigurableHostilityResponse.CurPawn));
					}
					else if (field.DeclaringType ==
							 typeof(VanillaExpandedFramework_Pawn_PathFollower_AtDestinationPosition_Patch))
					{
						instruction.operand = AccessTools.Field(
							typeof(MultiTargets_CurPawn_PathFollower),
							nameof(MultiTargets_CurPawn_PathFollower.CurPawn));
					}
					else if (field.DeclaringType ==
							 typeof(VanillaExpandedFramework_Verb_TryFindShootLineFromTo_Patch))
					{
						instruction.operand = AccessTools.Field(
							typeof(MultiTargets_CurPawn_Verb),
							nameof(MultiTargets_CurPawn_Verb.CurPawn));
					}
				}
				yield return instruction;
			}
		}
	}
}