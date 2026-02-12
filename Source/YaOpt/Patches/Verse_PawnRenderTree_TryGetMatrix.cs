using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Unity;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptComputeMatrixBurst"/>
	/// </summary>
	[HarmonyPatch(typeof(PawnRenderTree))]
	[HarmonyPatch(nameof(PawnRenderTree.TryGetMatrix))]
	internal static class Verse_PawnRenderTree_TryGetMatrix
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptComputeMatrixBurst.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var targetLabel = new Label();
			var popUnused = false;
			var skip = false;
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Beq_S)
				{
					targetLabel = (Label)instruction.operand;
					skip = true;
					yield return instruction;
					yield return CodeInstruction.LoadArgument(3);
					yield return CodeInstruction.LoadLocal(1);
					yield return CodeInstruction.Call(typeof(YaOptBurst), nameof(YaOptBurst.ApplyAltitude));
					continue;
				}
				if (skip)
				{
					if (instruction.labels.Contains(targetLabel))
						skip = false;
					else
						continue;
				}

				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo &&
				    methodInfo.Name == "ComputeMatrix")
				{
					instruction.operand = AccessTools.Method(
						typeof(YaOptBurst),
						nameof(YaOptBurst.ComputeMatrix));
					popUnused = true;
					YaOptMod.Log("ComputeMatrix Patched");
				}
				yield return instruction;
				if (popUnused)
				{
					yield return new CodeInstruction(OpCodes.Pop);
					popUnused = false;
				}
			}
		}
	}
}