using AlienRace;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Patches
{
	/// <summary>
	/// Routes mask texture lookups through YaOpt's texture cache.
	/// </summary>
	/// <seealso cref="SubMod.OptHARTextureCache"/>
	[HarmonyPatch(typeof(AlienRenderTreePatches))]
	[HarmonyPatch(nameof(AlienRenderTreePatches.CheckMaskShader))]
	internal static class AlienRace_AlienRenderTreePatches_CheckMaskShader
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("c630cb069c919c1c6fe84c79f633afa6"));
			}
			return SubMod.OptHARTextureCache.Enabled;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Replace ContentFinder<Texture2D>.Get(texPath + "_northm", false) to TextureCache.GetNorthm(texPath, false)
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
						instruction.operand = AccessTools.Method(typeof(TextureCache), nameof(TextureCache.GetNorthm));
					}
				}

				yield return instruction;
			}
		}
	}
}