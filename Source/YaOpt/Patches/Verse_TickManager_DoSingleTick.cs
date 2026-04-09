using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Patches TickManager.DoSingleTick to enable parallel pawn ticks and map post-tick processing.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	/// <seealso cref="YaOptSettings.OptParallelPostMapTick"/>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(TickManager))]
	[HarmonyPatch(nameof(TickManager.DoSingleTick))]
	internal static class Verse_TickManager_DoSingleTick
	{
		static bool Prepare()
		{
			return true;
		}

		static void Prefix()
		{
			UpdateCallbackHelper.PreTick();
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var ppt = YaOptGlobal.Settings.OptParallelPawnTick.Enabled;
			var ppmt = YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
			var tr = YaOptGlobal.Settings.OptFastListerRemove.Enabled;

			foreach (var instruction in instructions)
			{
				// Call DrawableRemovalHelper.StartRemovalJob before WorldTick
				if (tr && instruction.Calls("WorldTick"))
				{
					tr = false;
					yield return CodeInstruction.Call(
						typeof(DrawableRemovalHelper), nameof(DrawableRemovalHelper.StartRemovalJob));
				}

				yield return instruction;

				// Call ParallelPawnTickManager.ParellellyPreTickMaps before MapPreTick
				if (ppt && instruction.Calls("get_Maps"))
				{
					ppt = false;
					yield return new CodeInstruction(OpCodes.Dup);
					yield return CodeInstruction.Call(
						typeof(ParallelMapTickManager), nameof(ParallelMapTickManager.ParellellyPreTickMaps));
				}

				// Call ParallelPawnTickManager.ParellellyPostTickMaps before MapPostTick 
				if (ppmt && instruction.Calls("WorldPostTick"))
				{
					ppmt = false;
					yield return CodeInstruction.Call(
						typeof(ParallelMapTickManager), nameof(ParallelMapTickManager.ParellellyPostTickMaps));
				}
			}
		}

		static void Postfix()
		{
			UpdateCallbackHelper.PostTick();
		}
	}
}