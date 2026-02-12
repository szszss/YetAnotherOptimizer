using AlienRace;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using YaOpt.OtherMod.HumanoidAlienRaces.Helpers;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Patches
{
	[HarmonyPatch(typeof(AlienPartGenerator.BodyAddon))]
	[HarmonyPatch(nameof(AlienPartGenerator.BodyAddon.GetGraphic))]
	internal static class AlienRace_AlienPartGenerator_BodyAddon_GetGraphic
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("15b4c475856ce969f702bb6d737ece9c"));
			}
			return SubMod.OptHARTextureCache.Enabled;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Replace ContentFinder<Texture2D>.Get(texPath + "_southm", false) to TextureCache.GetSouthm(texPath, false)
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string str && str == "_southm")
					continue;
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo)
				{
					if (methodInfo.Name == "Concat")
						continue;

					if (methodInfo.Name == "Get")
					{
						if (methodInfo.DeclaringType == typeof(GraphicDatabase))
							instruction.operand = AccessTools.Method(typeof(HarHelper), nameof(HarHelper.GetGraphic));
						else if (methodInfo.DeclaringType == typeof(ContentFinder<Texture2D>))
							instruction.operand = AccessTools.Method(typeof(TextureCache), nameof(TextureCache.GetSouthm));
					}
				}

				yield return instruction;
			}
		}
	}
}