using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	[HarmonyPatch]
	internal static class RimWorld_Toils_LayDown_LayDown
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptIdleThrottle.Enabled;
		}

		static MethodBase TargetMethod()
		{
			foreach (var nestedType in typeof(Toils_LayDown).GetNestedTypes(BindingFlags.NonPublic))
			{
				foreach (var method in nestedType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					if (method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
					{
						var instructions = PatchProcessor.GetOriginalInstructions(method);
						if (instructions != null && instructions.Any(i => i.LoadsConstant(211)))
						{
							return method;
						}
					}
				}
			}
			YaOptMod.Error("Cannot find TargetMethod for Toils_LayDown.LayDown compiler-generated method.");
			return null;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsConstant(211))
				{
					yield return CodeInstruction.Call(typeof(IdleHelper), nameof(IdleHelper.GetUpInterval));
					continue;
				}
				yield return instruction;
			}
		}
	}
}