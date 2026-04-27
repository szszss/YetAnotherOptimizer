using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.Collections;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Patches DynamicDrawManager.DrawDynamicThings to enable early render preparation and wind update optimization.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptEarlyRenderPrepare"/>
	/// <seealso cref="YaOptSettings.OptWindUpdate"/>
	[HarmonyPatch(typeof(DynamicDrawManager))]
	[HarmonyPatch(nameof(DynamicDrawManager.DrawDynamicThings))]
	[ManualPatch]
	internal static class Verse_DynamicDrawManager_DrawDynamicThings
	{
		static bool Prepare()
		{
			var settings = YaOptGlobal.Settings;
			return settings.OptEarlyRenderPrepare.Enabled || settings.OptWindUpdate.Enabled;
		}

		static void Patch(Harmony harmony)
		{
			var settings = YaOptGlobal.Settings;
			var prefix = settings.OptWindUpdate.Enabled
				? new HarmonyMethod(typeof(Verse_DynamicDrawManager_DrawDynamicThings), nameof(Prefix))
				: null;
			var transpiler = settings.OptEarlyRenderPrepare.Enabled
				? new HarmonyMethod(typeof(Verse_DynamicDrawManager_DrawDynamicThings), nameof(Transpiler))
				: null;
			if (prefix != null || transpiler != null)
			{
				harmony.Patch(AccessTools.Method(
						typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings)),
					prefix: prefix,
					transpiler: transpiler);
			}
		}

		static void Prefix()
		{
			WindHelper.UpdateWindForMaterials();
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var firstTime = true;
			var skip = false;
			foreach (var instruction in instructions)
			{
				/* remove code between:
				 * bool flag = SilhouetteUtility.CanHighlightAny();
				 * and
				 * try { using (new ProfilerBlock("Draw Visible"))
				 */
				if (firstTime && instruction.opcode == OpCodes.Stloc_0)
				{
					firstTime = false;
					skip = true;
				}
				else if (instruction.opcode == OpCodes.Ldstr && "Draw Visible".Equals(instruction.operand))
				{
					var typeThingCullDetails = AccessTools.TypeByName("Verse.DynamicDrawManager/ThingCullDetails");
					var typeNativeArrayThingCullDetails = typeof(NativeArray<>).MakeGenericType(typeThingCullDetails);
					skip = false;
					yield return CodeInstruction.Call(typeof(ParallelPreDrawHelper),
						nameof(ParallelPreDrawHelper.WaitUntilPreDrawJobComplete));
					yield return CodeInstruction.LoadField(
						typeof(ParallelPreDrawHelper),
						nameof(ParallelPreDrawHelper.Data));
					yield return new CodeInstruction(OpCodes.Unbox_Any, typeNativeArrayThingCullDetails);
					yield return CodeInstruction.StoreLocal(1);
				}
				else if (skip)
				{
					continue;
				}
				yield return instruction;
			}
		}
	}
}