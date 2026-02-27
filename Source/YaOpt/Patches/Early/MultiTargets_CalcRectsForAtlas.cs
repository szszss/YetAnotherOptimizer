using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Verse;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// 
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFixTextureAtlas"/>
	[HarmonyPatch]
	[EarlyPatch]
	internal static class MultiTargets_CalcRectsForAtlas
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(StaticTextureAtlas), "CalcRectsForAtlas");
			yield return AccessTools.Method(typeof(StaticTextureAtlas), "CalcRectsForAtlasNew");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFixTextureAtlas.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Newobj && instruction.operand is ConstructorInfo info &&
					info.DeclaringType == typeof(Texture2D))
				{
					yield return CodeInstruction.Call(typeof(MultiTargets_CalcRectsForAtlas), nameof(CreateTexture));
					continue;
				}
				yield return instruction;
			}
		}

		private static Texture2D CreateTexture(int width, int height,
			GraphicsFormat format, int mipCount, TextureCreationFlags flags)
		{
			if (mipCount < 1)
			{
				mipCount = 0;
				flags ^= TextureCreationFlags.MipChain;
			}
			return new Texture2D(width, height, format, mipCount, flags);
		}
	}
}