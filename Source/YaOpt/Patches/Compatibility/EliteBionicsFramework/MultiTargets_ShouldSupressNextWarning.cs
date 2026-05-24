using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace YaOpt.Patches.Compatibility.EliteBionicsFramework
{
	[HarmonyPatch]
	internal static class MultiTargets_ShouldSupressNextWarning
	{
		[ThreadStatic]
		private static bool _shouldSupressNextWarning = false;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				AccessTools.TypeByName("EBF.Patches.PostFix_BodyPart_GetMaxHealth"),
				"SuppressNextWarning");
			yield return AccessTools.Method(
				AccessTools.TypeByName("EBF.Patches.PostFix_BodyPart_GetMaxHealth"),
				"CheckEbfProtocolViolation");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasMod("v1024.ebframework");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.operand is FieldInfo field &&
					field.Name == "shouldSupressNextWarning")
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_ShouldSupressNextWarning),
						nameof(_shouldSupressNextWarning));
				}
				yield return instruction;
			}
		}
	}
}