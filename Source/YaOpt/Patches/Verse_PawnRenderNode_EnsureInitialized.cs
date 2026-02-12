using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptFastRecacheRequested"/>
	/// </summary>
	[HarmonyPatch(typeof(PawnRenderNode))]
	[HarmonyPatch(nameof(PawnRenderNode.EnsureInitialized))]
	internal static class Verse_PawnRenderNode_EnsureInitialized
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastRecacheRequested.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo &&
				    methodInfo.Name == "get_RecacheRequested")
				{
					yield return CodeInstruction.Call(
						typeof(ParallelPreDrawHelper),
						nameof(ParallelPreDrawHelper.FastRecacheRequested));
					continue;
				}
				yield return instruction;
			}
		}
	}
}