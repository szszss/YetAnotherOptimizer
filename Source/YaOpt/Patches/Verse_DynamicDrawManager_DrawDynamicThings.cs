using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.Collections;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptEarlyRenderPrepare"/>
	/// <seealso cref="YaOptSettings.OptWindUpdate"/>
	/// </summary>
	[HarmonyPatch(typeof(DynamicDrawManager))]
	[HarmonyPatch(nameof(DynamicDrawManager.DrawDynamicThings))]
	internal static class Verse_DynamicDrawManager_DrawDynamicThings
	{
		static bool Prepare()
		{
			var settings = YaOptGlobal.Settings;
			return settings.OptEarlyRenderPrepare.Enabled || settings.OptWindUpdate.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var settings = YaOptGlobal.Settings;
			var prp = settings.OptEarlyRenderPrepare.Enabled;
			var wind = settings.OptWindUpdate.Enabled;
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
					if (prp)
					{
						skip = true;
					}
					// update wind for current map
					if (wind)
					{
						yield return CodeInstruction.Call(typeof(WindHelper), nameof(WindHelper.UpdateWindForMaterials));
					}
				}
				else if (prp && instruction.opcode == OpCodes.Ldstr && "Draw Visible".Equals(instruction.operand))
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