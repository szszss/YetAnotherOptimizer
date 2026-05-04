using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Caches texture lookups for Graphic_Multi to avoid repeated path concatenation and content finder calls.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptGraphicTextureCache"/>
	[HarmonyPatch(typeof(Graphic_Multi))]
	[HarmonyPatch(nameof(Graphic_Multi.Init))]
	internal static class Verse_Graphic_Multi_Init
	{
		static bool Prepare(MethodBase original)
		{
			return YaOptGlobal.Settings.OptGraphicTextureCache.Enabled;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var type = TextureCache.TYPE_COLOR;
			var variant = TextureCache.VARIANT_DEFAULT;
			var skipNextConcat = false;
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string str)
				{
					switch (str)
					{
						case "_north":
							variant = TextureCache.VARIANT_NORTH;
							skipNextConcat = true;
							continue;
						case "_east":
							variant = TextureCache.VARIANT_EAST;
							skipNextConcat = true;
							continue;
						case "_south":
							variant = TextureCache.VARIANT_SOUTH;
							skipNextConcat = true;
							continue;
						case "_west":
							variant = TextureCache.VARIANT_WEST;
							skipNextConcat = true;
							continue;
						case "m":
							type = TextureCache.TYPE_MASK;
							break;
					}
				}
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo)
				{
					if (skipNextConcat && methodInfo.Name == "Concat")
					{
						if (methodInfo.GetParameters().Length == 2)
						{
							skipNextConcat = false;
							continue;
						}
						if (methodInfo.GetParameters().Length == 3)
						{
							skipNextConcat = false;
							yield return new CodeInstruction(OpCodes.Pop);
							continue;
						}
					}

					if (methodInfo.Name == "Get" && methodInfo.DeclaringType == typeof(ContentFinder<Texture2D>))
					{
						string methodName = null;
						switch (type)
						{
							case TextureCache.TYPE_COLOR:
								switch (variant)
								{
									case TextureCache.VARIANT_DEFAULT: methodName = nameof(TextureCache.GetDefault); break;
									case TextureCache.VARIANT_NORTH: methodName = nameof(TextureCache.GetNorth); break;
									case TextureCache.VARIANT_EAST: methodName = nameof(TextureCache.GetEast); break;
									case TextureCache.VARIANT_SOUTH: methodName = nameof(TextureCache.GetSouth); break;
									case TextureCache.VARIANT_WEST: methodName = nameof(TextureCache.GetWest); break;
								}
								break;
							case TextureCache.TYPE_MASK:
								switch (variant)
								{
									case TextureCache.VARIANT_NORTH: methodName = nameof(TextureCache.GetNorthm); break;
									case TextureCache.VARIANT_EAST: methodName = nameof(TextureCache.GetEastm); break;
									case TextureCache.VARIANT_SOUTH: methodName = nameof(TextureCache.GetSouthm); break;
									case TextureCache.VARIANT_WEST: methodName = nameof(TextureCache.GetWestm); break;
								}
								break;
						}
						if (methodName == null)
						{
							throw new Exception(); // todo: exception info
						}
						instruction.operand = AccessTools.Method(typeof(TextureCache), methodName);
						variant = TextureCache.VARIANT_DEFAULT;
					}
				}

				yield return instruction;
			}
		}
	}
}