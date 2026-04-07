using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptCameraUpdateRate"/>
	[HarmonyPatch(typeof(GenTicks))]
	[HarmonyPatch(nameof(GenTicks.GetCameraUpdateRate))]
	internal static class Verse_GenTicks_GetCameraUpdateRate
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptCameraUpdateRate.Enabled;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool InViewOf(CameraDriver _, Thing thing)
		{
			return CameraHelper.CameraInViewOf(thing);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CameraZoomRange CurrentZoom(CameraDriver _)
		{
			return CameraHelper.CurrentZoomRange;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var oldDrawingMap = AccessTools.PropertyGetter(
				typeof(WorldRendererUtility), nameof(WorldRendererUtility.DrawingMap));
			var newDrawingMap = AccessTools.PropertyGetter(
				typeof(CameraHelper), nameof(CameraHelper.DrawingMap));
			var oldCurrentZoom = AccessTools.PropertyGetter(
				typeof(CameraDriver), nameof(CameraDriver.CurrentZoom));

			foreach (var instruction in instructions)
			{
				if (oldDrawingMap.Equals(instruction.operand))
				{
					instruction.operand = newDrawingMap;
				}
				if (instruction.Calls("InViewOf"))
				{
					yield return CodeInstruction.Call(
						typeof(Verse_GenTicks_GetCameraUpdateRate),
						nameof(InViewOf));
					continue;
				}
				if (oldCurrentZoom.Equals(instruction.operand))
				{
					yield return CodeInstruction.Call(
						typeof(Verse_GenTicks_GetCameraUpdateRate),
						nameof(CurrentZoom));
					continue;
				}

				yield return instruction;
			}
		}
	}
}