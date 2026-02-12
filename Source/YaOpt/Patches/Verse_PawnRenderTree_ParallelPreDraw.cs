using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptFastRecacheRequested"/>
	/// <seealso cref="YaOptSettings.OptParallelMaterialUpdate"/>
	/// </summary>
	[HarmonyPatch(typeof(PawnRenderTree))]
	[HarmonyPatch(nameof(PawnRenderTree.ParallelPreDraw))]
	internal static class Verse_PawnRenderTree_ParallelPreDraw
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastRecacheRequested.Enabled ||
				   YaOptGlobal.Settings.OptParallelMaterialUpdate.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var frr = YaOptGlobal.Settings.OptFastRecacheRequested.Enabled;
			foreach (var instruction in instructions)
			{
				if (frr && instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo &&
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

		static void Postfix(PawnRenderTree __instance, PawnDrawParms __0, List<PawnGraphicDrawRequest> ___drawRequests)
		{
			if (!YaOptGlobal.IsParallelMaterialUpdateEnabled)
				return;

			var colorId = ShaderPropertyIDs.Color;
			foreach (var drawRequest in ___drawRequests)
			{
				var node = drawRequest.node;
				var material = drawRequest.material;
				var worker = node.Worker;
				var matPropBlock = node.MatPropBlock;
				if (matPropBlock is null || drawRequest.material is null)
					continue;

				if (ParallelPreDrawHelper.CanParallelMaterialUpdate(worker))
				{
					if (__0.Statue)
					{
						matPropBlock.SetColor(colorId, __0.statueColor.Value);
					}
					else
					{
						matPropBlock.SetColor(colorId, __0.tint * material.color);
					}
					if (material.shader == ShaderDatabase.CutoutWithOverlay)
					{
						if (__0.pawn.Faction != null && material.GetMaskTexture() != null)
						{
							PawnRenderUtility.SetMatPropBlockOverlay(matPropBlock, __0.pawn.Faction.AllegianceColor, 0.5f);
						}
						else
						{
							PawnRenderUtility.SetMatPropBlockOverlay(matPropBlock, Color.white, 0f);
						}
					}
				}
				else
				{
					matPropBlock.SetColor(colorId, __0.tint * material.color);
				}
			}
		}
	}
}