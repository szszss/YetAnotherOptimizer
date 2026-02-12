using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_WorkGiverGrower
	{
		[ThreadStatic]
		public static ThingDef WantedPlantDef;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(WorkGiver_GrowerHarvest),
				nameof(WorkGiver_GrowerHarvest.HasJobOnCell));
			yield return AccessTools.Method(typeof(WorkGiver_GrowerSow),
				nameof(WorkGiver_GrowerSow.JobOnCell));
			yield return AccessTools.Method(typeof(WorkGiver_GrowerSow),
				"ExtraRequirements");
			foreach (var nestedType in typeof(WorkGiver_Grower).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				var method = nestedType.GetMethod("MoveNext",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					YaOptMod.Debug($"MultiTargets_WorkGiverGrower found a method from WorkGiver_Grower: {method.FullName()}");
					yield return method;
				}
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if ((instruction.opcode == OpCodes.Ldsfld || instruction.opcode == OpCodes.Stsfld) &&
					instruction.operand is FieldInfo fieldInfo && fieldInfo.Name == "wantedPlantDef")
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_WorkGiverGrower),
						nameof(WantedPlantDef));
				}
				yield return instruction;
			}
		}
	}
}