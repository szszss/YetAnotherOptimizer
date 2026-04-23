using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VEF.Hediffs;
using VEF.Pawns;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.VanillaExpandedFramework.Helpers
{
	internal static class MemoryLeakFixer
	{
		private const int CLEAR_INTERVAL = 60000;

		public static bool Enable = false;

		private static int _lastClearTick = -1;

		private static readonly AccessTools.FieldRef<Dictionary<Hediff, HediffComp_Spreadable>> _fieldRef;

		static MemoryLeakFixer()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreRenderCallback(PreRender);
			try
			{
				_fieldRef = AccessTools.StaticFieldRefAccess<Dictionary<Hediff, HediffComp_Spreadable>>(AccessTools.Field(
					typeof(VanillaExpandedFramework_Pawn_InteractionsTracker_TryInteractWith_Patch),
					"cachedComps"));
			}
			catch (Exception ex)
			{
				YaOptMod.Error("Cannot find cachedComps from VanillaExpandedFramework_Pawn_" +
							   "InteractionsTracker_TryInteractWith_Patch.\n" + ex);
			}
		}

		private static void PreRender(int tick)
		{
			if (!Enable)
				return;
			if (_lastClearTick == -1)
			{
				_lastClearTick = tick;
				return;
			}
			if (tick - _lastClearTick < CLEAR_INTERVAL)
			{
				return;
			}
			_lastClearTick = -tick;
			ClearMemory();
		}

		private static void ClearCache()
		{
			_lastClearTick = -1;
			if (!Enable)
				return;
			ClearMemory();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ClearMemory()
		{
			YaOptMod.Debug("Clear VEF pawn cache");
			VanillaExpandedFramework_Pawn_RelationsTracker_ExposeData_Patch.pawnPregnancyApproachData.Clear();
			if (_fieldRef != null)
				_fieldRef().Clear();
		}
	}
}