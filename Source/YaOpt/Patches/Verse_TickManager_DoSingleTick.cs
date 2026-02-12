using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="Helpers.UpdateCallbackHelper"/>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	/// <seealso cref="YaOptSettings.OptParallelPostMapTick"/>
	/// <seealso cref="YaOptSettings.OptFastCacheClear"/>
	/// </summary>
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
			var fcc = YaOptGlobal.Settings.OptFastCacheClear.Enabled;

			// Call ConnectivityCellCache.EnsureCleared before any MapPreTick
			if (fcc)
			{
				yield return CodeInstruction.Call(
					typeof(ConnectivityCellCache),
					nameof(ConnectivityCellCache.EnsureCleared));
			}

			foreach (var instruction in instructions)
			{
				// Call ConnectivityCellCache.StartClearJob after all MapPreTick
				if (fcc && instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo &&
					fieldInfo.Name == "fastEcology")
				{
					yield return CodeInstruction.Call(
						typeof(ConnectivityCellCache),
						nameof(ConnectivityCellCache.StartClearJob));
				}

				yield return instruction;

				// Call ParallelTickManager.ParellellyPreTickMaps
				if (ppt && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo1 &&
					methodInfo1.Name == "get_Maps")
				{
					yield return new CodeInstruction(OpCodes.Dup);
					yield return CodeInstruction.Call(
						typeof(ParallelTickManager), nameof(ParallelTickManager.ParellellyPreTickMaps));
				}

				// Call ParallelTickManager.ParellellyPostTickMaps
				if (ppmt && instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo2 &&
					methodInfo2.Name == "WorldPostTick")
				{
					yield return CodeInstruction.Call(
						typeof(ParallelTickManager), nameof(ParallelTickManager.ParellellyPostTickMaps));
				}
			}
		}

		static void Postfix()
		{
			UpdateCallbackHelper.PostTick();
		}
	}
}