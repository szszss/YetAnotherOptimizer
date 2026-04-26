using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using YaOpt.Patches.ThreadSafe.ThreadLocal;

namespace YaOpt.Patches.Compatibility.MapPreview
{
	[HarmonyPatch]
	internal static class MultiTargets_RandFix
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				AccessTools.TypeByName("MapPreview.Patches.Patch_Verse_Map"),
				"FillComponents_Prefix");
			yield return AccessTools.Method(
				AccessTools.TypeByName("MapPreview.Patches.Patch_Verse_Map"),
				"FillComponents_CheckRand");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
			       YaOptGlobal.HasType("MapPreview.Patches.Patch_Verse_Map");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var field = AccessTools.Field(typeof(Rand), "iterations");
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField(field))
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_Rand), nameof(MultiTargets_Rand._iterations));
				yield return instruction;
			}
		}
	}
}