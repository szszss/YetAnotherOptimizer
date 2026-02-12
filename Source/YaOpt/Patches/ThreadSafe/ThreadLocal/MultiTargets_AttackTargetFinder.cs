using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using Verse.AI;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_AttackTargetFinder
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget));
			yield return AccessTools.Method(typeof(AttackTargetFinder), nameof(AttackTargetFinder.CanSee));
			yield return AccessTools.Method(typeof(AttackTargetFinder), "GetAvailableShootingTargetsByScore");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			LocalBuilder localTmpTargets = null;
			LocalBuilder localAvailableShootingTargets = null;
			LocalBuilder localTmpTargetScores = null;
			LocalBuilder localTmpCanShootAtTarget = null;
			LocalBuilder localTempDestList = null;
			LocalBuilder localTempSourceList = null;

			if (methodBase.Name == nameof(AttackTargetFinder.BestAttackTarget))
			{
				localTmpTargets = generator.DeclareLocal(typeof(List<IAttackTarget>));

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalAttackTargetFinder),
						nameof(ThreadLocalAttackTargetFinder.TmpTargets)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<IAttackTarget>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTmpTargets.LocalIndex);
			}

			if (methodBase.Name == nameof(AttackTargetFinder.CanSee))
			{
				localTempDestList = generator.DeclareLocal(typeof(List<IntVec3>));
				localTempSourceList = generator.DeclareLocal(typeof(List<IntVec3>));

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalAttackTargetFinder),
						nameof(ThreadLocalAttackTargetFinder.TempDestList)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<IntVec3>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTempDestList.LocalIndex);

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalAttackTargetFinder),
						nameof(ThreadLocalAttackTargetFinder.TempSourceList)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<IntVec3>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTempSourceList.LocalIndex);
			}

			if (methodBase.Name == "GetAvailableShootingTargetsByScore")
			{
				localAvailableShootingTargets = generator.DeclareLocal(typeof(List<Pair<IAttackTarget, float>>));
				localTmpTargetScores = generator.DeclareLocal(typeof(List<float>));
				localTmpCanShootAtTarget = generator.DeclareLocal(typeof(List<bool>));

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalAttackTargetFinder),
						nameof(ThreadLocalAttackTargetFinder.AvailableShootingTargets)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<Pair<IAttackTarget, float>>>), "Value"));
				yield return CodeInstruction.StoreLocal(localAvailableShootingTargets.LocalIndex);

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalAttackTargetFinder),
						nameof(ThreadLocalAttackTargetFinder.TmpTargetScores)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<float>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTmpTargetScores.LocalIndex);

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalAttackTargetFinder),
						nameof(ThreadLocalAttackTargetFinder.TmpCanShootAtTarget)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<bool>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTmpCanShootAtTarget.LocalIndex);
			}


			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo)
				{
					if (fieldInfo.Name == "tmpTargets" && localTmpTargets != null)
					{
						yield return CodeInstruction.LoadLocal(localTmpTargets.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "availableShootingTargets" && localAvailableShootingTargets != null)
					{
						yield return CodeInstruction.LoadLocal(localAvailableShootingTargets.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "tmpTargetScores" && localTmpTargetScores != null)
					{
						yield return CodeInstruction.LoadLocal(localTmpTargetScores.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "tmpCanShootAtTarget" && localTmpCanShootAtTarget != null)
					{
						yield return CodeInstruction.LoadLocal(localTmpCanShootAtTarget.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "tempDestList" && localTempDestList != null)
					{
						yield return CodeInstruction.LoadLocal(localTempDestList.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "tempSourceList" && localTempSourceList != null)
					{
						yield return CodeInstruction.LoadLocal(localTempSourceList.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}