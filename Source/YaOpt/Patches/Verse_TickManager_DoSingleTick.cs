using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
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

			foreach (var instruction in instructions)
			{
				yield return instruction;

				// Call ParallelPawnTickManager.ParellellyPreTickMaps
				if (ppt && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo1 &&
					methodInfo1.Name == "get_Maps")
				{
					yield return new CodeInstruction(OpCodes.Dup);
					yield return CodeInstruction.Call(
						typeof(ParallelMapTickManager), nameof(ParallelMapTickManager.ParellellyPreTickMaps));
				}

				// Call ParallelPawnTickManager.ParellellyPostTickMaps
				if (ppmt && instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo2 &&
					methodInfo2.Name == "WorldPostTick")
				{
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