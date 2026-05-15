using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptEarlyRenderPrepare"/>
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.MapUpdate))]
	internal static class Verse_Map_MapUpdate
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptEarlyRenderPrepare.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;

				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo)
				{
					// Call ParallelPreDrawHelper.StartCullingJob after PlantFallColors.SetFallShaderGlobals
					// (Before mapDrawer.MapMeshDrawerUpdate_First)
					if (methodInfo.Name == "SetFallShaderGlobals")
					{
						yield return CodeInstruction.Call(
							typeof(ParallelPreDrawHelper),
							nameof(ParallelPreDrawHelper.StartCullingJob));
					}
					// Call ParallelPreDrawHelper.StartCullingJob after DoorsDebugDrawer.DrawDebug
					// (Before mapDrawer.DrawMapMesh)
					else if (methodInfo.Name == "DrawDebug" && methodInfo.DeclaringType == typeof(DoorsDebugDrawer))
					{
						yield return CodeInstruction.Call(
							typeof(ParallelPreDrawHelper),
							nameof(ParallelPreDrawHelper.StartPreDrawJob));
					}
				}
			}
		}
	}
}