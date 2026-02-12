using FacialAnimation;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	[HarmonyPatch(typeof(GraphicHelper))]
	[HarmonyPatch(nameof(GraphicHelper.CheckTexPathExist))]
	internal static class FacialAnimation_GraphicHelper_CheckTexPathExist
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("8befcfd39526a6759a696b863c11651d"));
			}
			return SubMod.OptFATextureCache.Enabled;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Replace ContentFinder<Texture2D>.Get(path + "_south", false) to TextureCache.GetSouth(texPath, false)
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldstr)
					continue;
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo)
				{
					if (methodInfo.Name == "Concat")
						continue;

					if (methodInfo.Name == "Get")
					{
						instruction.operand = AccessTools.Method(typeof(TextureCache), nameof(TextureCache.GetSouth));
					}
				}

				yield return instruction;
			}
		}
	}
}