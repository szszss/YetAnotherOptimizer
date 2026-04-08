using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	[HarmonyPatch(typeof(AttackTargetFinder))]
	[HarmonyPatch("FriendlyFireConeTargetScoreOffset")]
	internal static class Verse_AI_AttackTargetFinder_FriendlyFireConeTargetScoreOffset
	{
		private static readonly HashSet<IntVec3> _cells = new HashSet<IntVec3>();

		static IEnumerable<IntVec3> GetCellsWhereBulletFlyThrough(
			Pawn shooter, in ShotReport report, float radius)
		{
			_cells.Clear();
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
					_cells.Add(pos);
				}
				if (!shouldBreak && shootLine.Dest.CanBeSeenOverFast(map))
				{
					_cells.Add(shootLine.Dest);
				}
			}
			return _cells;
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