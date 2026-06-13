using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using Verse.AI;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptAttackTargetFinder"/>
	[HarmonyPatch(typeof(AttackTargetFinder))]
	[HarmonyPatch("FriendlyFireConeTargetScoreOffset")]
	internal static class Verse_AI_AttackTargetFinder_FriendlyFireConeTargetScoreOffset
	{
		private static readonly ThreadLocal<HashSet<IntVec3>> _threadLocalCellSet =
			new ThreadLocal<HashSet<IntVec3>>(ThreadLocalHelper.NewSet<IntVec3>);

		private static readonly ThreadLocal<HashSet<IntVec3>> _threadLocalCellList =
			new ThreadLocal<HashSet<IntVec3>>(ThreadLocalHelper.NewSet<IntVec3>);

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptAttackTargetFinder.Enabled;
		}

		static IEnumerable<IntVec3> GetCellsWhereBulletFlyThrough(
			Pawn shooter, in ShotReport report, float radius)
		{
			var cellSet = _threadLocalCellSet.Value;
			var cellList = _threadLocalCellList.Value;
			cellSet.Clear();
			cellList.Clear();
			var map = shooter.Map;
			foreach (var dest in GenRadial.RadialCellsAround(report.ShootLine.Dest, radius, true))
			{
				if (!dest.InBounds(map))
					continue;
				var shouldBreak = false;
				var shootLine = new ShootLine(report.ShootLine.Source, dest);
				foreach (var pos in shootLine.Points())
				{
					if (!pos.CanBeSeenOverFast(map))
					{
						shouldBreak = true;
						break;
					}
					if (pos.GetThingList(map).Count > 0 &&
						cellSet.Add(pos))
					{
						cellList.Add(pos);
					}
				}
				if (!shouldBreak && shootLine.Dest.CanBeSeenOverFast(map)
								 && shootLine.Dest.GetThingList(map).Count > 0
								 && cellSet.Add(shootLine.Dest))
				{
					cellList.Add(shootLine.Dest);
				}
			}
			return cellList;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			const int LOCAL_CLOSURE = 0;
			const int LOCAL_PAWN = 1;
			const int LOCAL_RADIUS = 4;
			FieldInfo fieldHitReport = null;
			var prepareSkip = false;
			var skipping = false;
			foreach (var instruction in instructions)
			{
				if (!prepareSkip && instruction.Calls("Max"))
				{
					prepareSkip = true;
				}
				if (prepareSkip && instruction.opcode == OpCodes.Ldloc_0)
				{
					prepareSkip = false;
					skipping = true;
				}
				if (skipping && fieldHitReport == null && instruction.opcode == OpCodes.Ldflda)
				{
					fieldHitReport = instruction.operand as FieldInfo;
				}
				if (skipping && instruction.Calls("Distinct"))
				{
					skipping = false;

					if (fieldHitReport == null)
						throw new MissingFieldException("Cannot find HitReport field for " +
														"FriendlyFireConeTargetScoreOffset");

					yield return CodeInstruction.LoadLocal(LOCAL_PAWN);
					yield return CodeInstruction.LoadLocal(LOCAL_CLOSURE);
					yield return new CodeInstruction(OpCodes.Ldflda, fieldHitReport);
					yield return CodeInstruction.LoadLocal(LOCAL_RADIUS);
					yield return CodeInstruction.Call(
						typeof(Verse_AI_AttackTargetFinder_FriendlyFireConeTargetScoreOffset),
						nameof(GetCellsWhereBulletFlyThrough));
					continue;
				}

				if (!skipping)
					yield return instruction;
			}
		}
	}
}