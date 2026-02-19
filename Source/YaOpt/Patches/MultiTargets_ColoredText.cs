using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// Replaces culture-sensitive string.IndexOf with ordinal comparison in ColoredText methods.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptColoredText"/>
	[HarmonyPatch]
	internal static class MultiTargets_ColoredText
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(ColoredText), nameof(ColoredText.Resolve));
			yield return AccessTools.Method(typeof(ColoredText), nameof(ColoredText.StripTags));
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptColoredText.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo &&
					methodInfo.Name == "IndexOf" && methodInfo.GetParameters().Length == 1 &&
					methodInfo.GetParameters()[0].ParameterType == typeof(string))
				{
					yield return new CodeInstruction(OpCodes.Ldc_I4_4);
					instruction.operand = AccessTools.Method(typeof(string), nameof(string.IndexOf),
						new[] { typeof(string), typeof(StringComparison) });
				}
				yield return instruction;
			}
		}
	}
}