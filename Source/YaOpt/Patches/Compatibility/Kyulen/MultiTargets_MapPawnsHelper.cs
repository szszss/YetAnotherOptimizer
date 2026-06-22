using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.Kyulen
{
	[HarmonyPatch]
	internal static class MultiTargets_MapPawnsHelper
	{
		private delegate bool IsHumanlikeOrAmp(Pawn pawn);

		private static IsHumanlikeOrAmp _isHumanlikeOrAmp;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(MapPawnsHelper),
				nameof(MapPawnsHelper.FreeColonistsAndSubhumansControllable));
			yield return AccessTools.Method(
				typeof(MapPawnsHelper),
				nameof(MapPawnsHelper.FreeColonistsAndPrisoners));
		}

		static bool Prepare()
		{
			var shouldDo = YaOptGlobal.Settings.OptGetMapPawns.Enabled &&
						   YaOptGlobal.HasMod("bichang.kyulen");
			if (shouldDo && _isHumanlikeOrAmp == null)
			{
				_isHumanlikeOrAmp = AccessTools.MethodDelegate<IsHumanlikeOrAmp>(
					AccessTools.Method(
						AccessTools.TypeByName("Ninetail.AmpBodyGuards"),
						"IsHumanlikeOrAmp"));
				if (_isHumanlikeOrAmp == null)
					throw new MissingMemberException("Ninetail.AmpBodyGuards", "IsHumanlikeOrAmp");
			}
			return shouldDo;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("IsFreeHumanlike"))
					instruction.operand = AccessTools.Method(
						typeof(MultiTargets_MapPawnsHelper),
						nameof(IsFreeHumanlike));
				yield return instruction;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFreeHumanlike(Pawn pawn, Faction faction)
		{
			return (!ModsConfig.AnomalyActive || !pawn.IsSubhuman) && pawn.Faction == faction &&
				   (pawn.HostFaction == null || pawn.IsSlave) && _isHumanlikeOrAmp(pawn);
		}
	}
}