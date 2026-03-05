using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.Compatibility.WhileYouAreUp
{
	[HarmonyPatch]
	internal static class WhileYoureUp_BeforeSupplyDetour_Job
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("WhileYoureUp.Mod/WorkGiver_ConstructDeliverResources__ResourceDeliverJobFor_Patch"),
				"BeforeSupplyDetour_Job");
		}

		static bool Prepare(MethodBase original)
		{
			if (original != null)
				return true;

			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled && YaOptGlobal.HasType("WhileYoureUp.Mod");
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("Create"))
				{
					instruction.operand = AccessTools.Method(
						typeof(Traverse),
						nameof(Traverse.Create),
						Type.EmptyTypes,
						new[] { typeof(ThreadLocalConstructDeliverResources) });
				}
				else if (instruction.Calls("Field"))
				{
					instruction.operand = AccessTools
						.FirstMethod(typeof(Traverse), method => method.Name == "Field" && method.IsGenericMethod)
						.MakeGenericMethod(typeof(ThreadLocal<List<Thing>>));
				}
				else if (instruction.LoadsConstant("resourcesAvailable"))
				{
					instruction.operand = "ResourcesAvailable";
				}

				yield return instruction;

				if (instruction.Calls("get_Value"))
				{
					yield return new CodeInstruction(OpCodes.Call,
						AccessTools.PropertyGetter(typeof(ThreadLocal<List<Thing>>), "Value"));
				}
			}
		}
	}
}