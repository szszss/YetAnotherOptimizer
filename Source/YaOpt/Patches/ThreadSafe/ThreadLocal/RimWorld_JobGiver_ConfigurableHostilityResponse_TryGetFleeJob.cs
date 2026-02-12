using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse))]
	[HarmonyPatch("TryGetFleeJob")]
	internal static class RimWorld_JobGiver_ConfigurableHostilityResponse_TryGetFleeJob
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<Thing>));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<JobGiver_ConfigurableHostilityResponse, Thing>),
				nameof(ThreadLocalTmpList<JobGiver_ConfigurableHostilityResponse, Thing>.Get));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("tmpThreats", true))
				{
					yield return CodeInstruction.LoadLocal(local.LocalIndex)
						.WithLabels(instruction.labels);
					continue;
				}
				yield return instruction;
			}
		}
	}
}