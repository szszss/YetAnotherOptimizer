using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_WorkGiverConstructDeliverResources
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources),
				"ResourceDeliverJobFor");
			yield return AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources),
				"FindAvailableNearbyResources");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			LocalBuilder localResourcesAvailable = generator.DeclareLocal(typeof(List<Thing>));
			LocalBuilder localMissingResources = null;

			yield return new CodeInstruction(OpCodes.Ldsfld,
				AccessTools.Field(
					typeof(ThreadLocalConstructDeliverResources),
					nameof(ThreadLocalConstructDeliverResources.ResourcesAvailable)));
			yield return new CodeInstruction(OpCodes.Call,
				AccessTools.PropertyGetter(typeof(ThreadLocal<List<Thing>>), "Value"));
			yield return CodeInstruction.StoreLocal(localResourcesAvailable.LocalIndex);

			if (methodBase.Name == "ResourceDeliverJobFor")
			{
				localMissingResources = generator.DeclareLocal(typeof(Dictionary<ThingDef, int>));
				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalConstructDeliverResources),
						nameof(ThreadLocalConstructDeliverResources.MissingResources)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<Dictionary<ThingDef, int>>), "Value"));
				yield return CodeInstruction.StoreLocal(localMissingResources.LocalIndex);
			}

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo)
				{
					if (fieldInfo.Name == "missingResources" && localMissingResources != null)
					{
						yield return CodeInstruction.LoadLocal(localMissingResources.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "resourcesAvailable")
					{
						yield return CodeInstruction.LoadLocal(localResourcesAvailable.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}