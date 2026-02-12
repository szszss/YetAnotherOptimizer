using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	[HarmonyAfter("OskarPotocki.VEF")]
	internal static class MultiTargets_WorkGiverDoBill
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(WorkGiver_DoBill), 
				"StartOrResumeBillJob");
			yield return AccessTools.Method(typeof(WorkGiver_DoBill),
				"TryFindBestIngredientsHelper");
			yield return AccessTools.Method(typeof(WorkGiver_DoBill),
				"AddEveryMedicineToRelevantThings");
			yield return AccessTools.Method(typeof(WorkGiver_DoBill),
				"TryFindBestIngredientsInSet_NoMixHelper");
			foreach (var nestedType in typeof(WorkGiver_DoBill).GetNestedTypes(
				         BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				var method = AccessTools.FirstMethod(nestedType, methodInfo =>
				{
					var param = methodInfo.GetParameters();
					return param?.Length == 1 && param[0].ParameterType == typeof(Region) &&
					       (methodInfo.Name.Contains("<TryFindBestIngredientsHelper>b__4"));
				});
				if (method != null)
				{
					YaOptMod.Debug($"MultiTargets_WorkGiverDoBill found a method from WorkGiver_DoBill: {method.FullName()}");
					yield return method;
				}
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			LocalBuilder localMissingIngredients = null;
			LocalBuilder localTmpMissingUniqueIngredients = null;
			LocalBuilder localRelevantThings = null;
			LocalBuilder localProcessedThings = null;
			LocalBuilder localNewRelevantThings = null;
			LocalBuilder localTmpMedicine = null;
			LocalBuilder localAvailableCounts = null;
			
			if (methodBase.Name == "StartOrResumeBillJob")
			{
				localMissingIngredients = generator.DeclareLocal(typeof(List<IngredientCount>));
				localTmpMissingUniqueIngredients = generator.DeclareLocal(typeof(List<Thing>));

				yield return new CodeInstruction(OpCodes.Ldsfld, 
					AccessTools.Field(
						typeof(ThreadLocalDoBill),
						nameof(ThreadLocalDoBill.MissingIngredients)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<IngredientCount>>), "Value"));
				yield return CodeInstruction.StoreLocal(localMissingIngredients.LocalIndex);

				yield return new CodeInstruction(OpCodes.Ldsfld, 
					AccessTools.Field(
						typeof(ThreadLocalDoBill),
						nameof(ThreadLocalDoBill.TmpMissingUniqueIngredients)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<Thing>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTmpMissingUniqueIngredients.LocalIndex);
			}

			if (methodBase.Name.Contains("TryFindBestIngredientsHelper"))
			{
				localRelevantThings = generator.DeclareLocal(typeof(List<Thing>));
				localProcessedThings = generator.DeclareLocal(typeof(HashSet<Thing>));
				localNewRelevantThings = generator.DeclareLocal(typeof(List<Thing>));

				yield return new CodeInstruction(OpCodes.Ldsfld, 
					AccessTools.Field(
						typeof(ThreadLocalDoBill),
						nameof(ThreadLocalDoBill.RelevantThings)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<Thing>>), "Value"));
				yield return CodeInstruction.StoreLocal(localRelevantThings.LocalIndex);

				yield return new CodeInstruction(OpCodes.Ldsfld, 
					AccessTools.Field(
						typeof(ThreadLocalDoBill),
						nameof(ThreadLocalDoBill.ProcessedThings)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<HashSet<Thing>>), "Value"));
				yield return CodeInstruction.StoreLocal(localProcessedThings.LocalIndex);

				yield return new CodeInstruction(OpCodes.Ldsfld, 
					AccessTools.Field(
						typeof(ThreadLocalDoBill),
						nameof(ThreadLocalDoBill.NewRelevantThings)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<Thing>>), "Value"));
				yield return CodeInstruction.StoreLocal(localNewRelevantThings.LocalIndex);
			}

			if (methodBase.Name == "AddEveryMedicineToRelevantThings")
			{
				localTmpMedicine = generator.DeclareLocal(typeof(List<Thing>));
				yield return new CodeInstruction(OpCodes.Ldsfld, 
					AccessTools.Field(
						typeof(ThreadLocalDoBill),
						nameof(ThreadLocalDoBill.TmpMedicine)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<Thing>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTmpMedicine.LocalIndex);
			}

			if (methodBase.Name == "TryFindBestIngredientsInSet_NoMixHelper")
			{
				var type = AccessTools.TypeByName("RimWorld.WorkGiver_DoBill/DefCountList");
				localAvailableCounts = generator.DeclareLocal(type);
				yield return new CodeInstruction(OpCodes.Ldsfld, 
					AccessTools.Field(
						typeof(ThreadLocalDoBill),
						nameof(ThreadLocalDoBill.AvailableCounts)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<object>), "Value"));
				yield return new CodeInstruction(OpCodes.Castclass, type);
				yield return CodeInstruction.StoreLocal(localAvailableCounts.LocalIndex);
			}

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo)
				{
					if (fieldInfo.Name == "missingIngredients" && localMissingIngredients != null)
					{
						yield return CodeInstruction.LoadLocal(localMissingIngredients.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "tmpMissingUniqueIngredients" && localTmpMissingUniqueIngredients != null)
					{
						yield return CodeInstruction.LoadLocal(localTmpMissingUniqueIngredients.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "relevantThings" && localRelevantThings != null)
					{
						yield return CodeInstruction.LoadLocal(localRelevantThings.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "processedThings" && localProcessedThings != null)
					{
						yield return CodeInstruction.LoadLocal(localProcessedThings.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "newRelevantThings" && localNewRelevantThings != null)
					{
						yield return CodeInstruction.LoadLocal(localNewRelevantThings.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "tmpMedicine" && localTmpMedicine != null)
					{
						yield return CodeInstruction.LoadLocal(localTmpMedicine.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "availableCounts" && localAvailableCounts != null)
					{
						yield return CodeInstruction.LoadLocal(localAvailableCounts.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}