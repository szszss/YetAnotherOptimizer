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
	/// <summary>
	/// Routes texture lookups through YaOpt's texture cache for faster repeated access.
	/// </summary>
	/// <seealso cref="SubMod.OptHARTextureCache"/>
	[HarmonyPatch(typeof(AlienPartGenerator.BodyAddon))]
	[HarmonyPatch(nameof(AlienPartGenerator.BodyAddon.GetGraphic))]
	internal static class AlienRace_AlienPartGenerator_BodyAddon_GetGraphic
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("a7f020908f977d03c1b9477aa57b8d80"));
			}
			return SubMod.OptHARTextureCache.Enabled;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Replace ContentFinder<Texture2D>.Get(texPath + "_southm", false) to TextureCache.GetSouthm(texPath, false, true)
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
						{
							yield return new CodeInstruction(OpCodes.Ldc_I4_1); // needPostfixM: true
							instruction.operand =
								AccessTools.Method(typeof(TextureCache), nameof(TextureCache.GetSouthm));
						}
					}
				}

				yield return instruction;
			}
		}
	}
}