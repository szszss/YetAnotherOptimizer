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
	internal static class MultiTargets_FoodUtility
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(FoodUtility), "BestFoodSourceOnMap");
			yield return AccessTools.Method(typeof(FoodUtility), "BestPawnToHuntForPredator");
			yield return AccessTools.Method(typeof(FoodUtility), "AddThoughtsFromIdeo");
			yield return AccessTools.Method(typeof(FoodUtility), "ThoughtsFromIngesting");
			yield return AccessTools.Method(typeof(FoodUtility), "AddIngestThoughtsFromIngredient");

			foreach (var nestedType in typeof(FoodUtility).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				var method = AccessTools.FirstMethod(nestedType, methodInfo =>
				{
					var param = methodInfo.GetParameters();
					return param?.Length == 1 && param[0].ParameterType == typeof(Region) &&
						   methodInfo.Name.Contains("<BestPawnToHuntForPredator>");
				});
				if (method != null)
				{
					YaOptMod.Debug($"MultiTargets_FoodUtility found a method from FoodUtility: {method.FullName()}");
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
			LocalBuilder localFiltered = null;
			LocalBuilder localTmpPredatorCandidates = null;
			LocalBuilder localIngestThoughts = null;
			LocalBuilder localExtraIngestThoughtsFromTraits = null;
			LocalBuilder localIdeoIngestThoughtsCache = null;

			if (methodBase.Name == "BestFoodSourceOnMap")
			{
				localFiltered = generator.DeclareLocal(typeof(HashSet<Thing>));

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalFoodUtility),
						nameof(ThreadLocalFoodUtility.Filtered)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<HashSet<Thing>>), "Value"));
				yield return CodeInstruction.StoreLocal(localFiltered.LocalIndex);
			}

			if (methodBase.Name.Contains("BestPawnToHuntForPredator"))
			{
				localTmpPredatorCandidates = generator.DeclareLocal(typeof(List<Pawn>));

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalFoodUtility),
						nameof(ThreadLocalFoodUtility.TmpPredatorCandidates)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<Pawn>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTmpPredatorCandidates.LocalIndex);
			}

			if (methodBase.Name == "AddThoughtsFromIdeo")
			{
				localIngestThoughts = generator.DeclareLocal(typeof(List<FoodUtility.ThoughtFromIngesting>));
				localIdeoIngestThoughtsCache = generator.DeclareLocal(
					typeof(Dictionary<Ideo, Dictionary<HistoryEventDef, List<Precept>>>));

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalFoodUtility),
						nameof(ThreadLocalFoodUtility.IngestThoughts)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<FoodUtility.ThoughtFromIngesting>>), "Value"));
				yield return CodeInstruction.StoreLocal(localIngestThoughts.LocalIndex);

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalFoodUtility),
						nameof(ThreadLocalFoodUtility.IdeoIngestThoughtsCache)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(
						typeof(ThreadLocal<Dictionary<Ideo, Dictionary<HistoryEventDef, List<Precept>>>>), "Value"));
				yield return CodeInstruction.StoreLocal(localIdeoIngestThoughtsCache.LocalIndex);
			}

			if (methodBase.Name == "ThoughtsFromIngesting")
			{
				localIngestThoughts = generator.DeclareLocal(typeof(List<FoodUtility.ThoughtFromIngesting>));
				localExtraIngestThoughtsFromTraits = generator.DeclareLocal(typeof(List<ThoughtDef>));

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalFoodUtility),
						nameof(ThreadLocalFoodUtility.IngestThoughts)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<FoodUtility.ThoughtFromIngesting>>), "Value"));
				yield return CodeInstruction.StoreLocal(localIngestThoughts.LocalIndex);

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalFoodUtility),
						nameof(ThreadLocalFoodUtility.ExtraIngestThoughtsFromTraits)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<ThoughtDef>>), "Value"));
				yield return CodeInstruction.StoreLocal(localExtraIngestThoughtsFromTraits.LocalIndex);
			}

			if (methodBase.Name == "AddIngestThoughtsFromIngredient")
			{
				localExtraIngestThoughtsFromTraits = generator.DeclareLocal(typeof(List<ThoughtDef>));

				yield return new CodeInstruction(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(ThreadLocalFoodUtility),
						nameof(ThreadLocalFoodUtility.ExtraIngestThoughtsFromTraits)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<ThoughtDef>>), "Value"));
				yield return CodeInstruction.StoreLocal(localExtraIngestThoughtsFromTraits.LocalIndex);
			}

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo)
				{
					if (fieldInfo.Name == "filtered" && localFiltered != null)
					{
						yield return CodeInstruction.LoadLocal(localFiltered.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "tmpPredatorCandidates" && localTmpPredatorCandidates != null)
					{
						yield return CodeInstruction.LoadLocal(localTmpPredatorCandidates.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "ingestThoughts" && localIngestThoughts != null)
					{
						yield return CodeInstruction.LoadLocal(localIngestThoughts.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "extraIngestThoughtsFromTraits" && localExtraIngestThoughtsFromTraits != null)
					{
						yield return CodeInstruction.LoadLocal(localExtraIngestThoughtsFromTraits.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "ideoIngestThoughtsCache" && localIdeoIngestThoughtsCache != null)
					{
						yield return CodeInstruction.LoadLocal(localIdeoIngestThoughtsCache.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}