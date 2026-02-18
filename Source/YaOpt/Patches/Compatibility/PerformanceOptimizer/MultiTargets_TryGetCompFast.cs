using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.PerformanceOptimizer
{
	[HarmonyPatch]
	[HarmonyAfter("PerformanceOptimizer.Main")]
	internal static class MultiTargets_TryGetCompFast
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Hediff), nameof(Hediff.TendableNow));
			yield return AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.IsTended));
			yield return AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.IsPermanent));
			yield return AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.FullyImmune));
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("TryGetHediffCompFast"))
				{
					var originalMethod = (MethodInfo)instruction.operand;
					var jumpElse = generator.DefineLabel();
					var jumpEnd = generator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Call,
						AccessTools.PropertyGetter(typeof(YaOptGlobal), nameof(YaOptGlobal.IsInMainThread)));
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Beq_S, jumpElse);
					yield return instruction;
					yield return new CodeInstruction(OpCodes.Br_S, jumpEnd);
					var method = AccessTools.Method(typeof(HediffUtility),
							nameof(HediffUtility.TryGetComp)).MakeGenericMethod(originalMethod.GetGenericArguments());
					yield return new CodeInstruction(OpCodes.Call, method).WithLabels(jumpElse);
					yield return new CodeInstruction(OpCodes.Nop).WithLabels(jumpEnd);
					continue;
				}
				yield return instruction;
			}
		}
	}
}