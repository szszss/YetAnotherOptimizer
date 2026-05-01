using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Patches.Compatibility.EliteBionicsFramework
{
	/// <summary>
	/// Fixes race condition in Elite Bionics Framework's health cache when accessed from parallel threads.
	/// Replaces random cache lifespan with fixed value to avoid Rand state corruption.
	/// </summary>
	[HarmonyPatch("EBF.Util.MaxHealthCache", "SetCachedBodyPartMaxHealth")]
	internal static class EBF_Util_MaxHealthCache_SetCachedBodyPartMaxHealth
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("fe3a324f0872b548d5778c4aae14b812"));
			}
			return SubMod.OptFAParallelUpdate.Enabled && YaOptGlobal.HasMod("V1024.EBFramework");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			/*
			 * FacialAnimationControllerComp.GatherPawnParam calls FAHelper.CheckDisableHighlight,
			 * which will call PawnCapacityUtility.CalculateCapacityLevel, and it will call
			 * MaxHealthCache.SetCachedBodyPartMaxHealth finally.
			 * Original SetCachedBodyPartMaxHealth uses Rand to cache data with a random lifespan.
			 * It uses Rand.PushState and Rand.PopState to protect Rand state. There, however,
			 * is a very small chance that multi-threaded operations on the state stack could
			 * cause a race condition, resulting in a System.InvalidOperationException.
			 * This patch removed the random number and replaced it with a fixed cache lifespan.
			 * We later added locks to all operations involving the state stack, but this patch
			 * was retained.
			 */

			var skip = true;
			yield return new CodeInstruction(OpCodes.Ldc_I4, 120000);
			yield return CodeInstruction.StoreLocal(0);
			foreach (var instruction in instructions)
			{
				if (!skip)
					yield return instruction;

				if (skip && instruction.Calls("PopState"))
				{
					skip = false;
				}
			}
		}
	}
}
