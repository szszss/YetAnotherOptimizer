using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	[HarmonyPriority(Priority.VeryLow)]
	internal static class MultiTargets_PawnsFinder
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsWorldAndTemporary_Alive));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsAndWorld_Alive));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_Spawned));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.All_AliveOrDead));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.Temporary));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.Temporary_Alive));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.Temporary_Dead));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllCaravansAndTravellingTransporters_Alive));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_NoCryptosleep));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_PrisonersOfColony));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_NoCryptosleep));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_NoCryptosleep));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_PrisonersOfColonySpawned));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_PrisonersOfColony));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_FreeColonists));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_FreeColonistsSpawned));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_FreeColonistsAndPrisoners));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_ColonySubhumansSpawnedPlayerControlled));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_ColonySubhumans_NoSuspended));
			yield return AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.HomeMaps_FreeColonistsSpawned));
			yield return AccessTools.Method(typeof(PawnsFinder), nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists_OfXenotype));
			yield return AccessTools.Method(typeof(PawnsFinder), nameof(PawnsFinder.AllMaps_SpawnedPawnsInFaction));
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			var labelElse = generator.DefineLabel();
			var labelEnd = generator.DefineLabel();

			FieldInfo fieldPawnFinder;
			FieldInfo fieldThreadLocal;

			switch (methodBase.Name)
			{
				case "get_AllMapsWorldAndTemporary_AliveOrDead":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsWorldAndTemporary_AliveOrDead_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsWorldAndTemporary_AliveOrDead_Result");
					break;
				case "get_AllMapsWorldAndTemporary_Alive":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsWorldAndTemporary_Alive_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsWorldAndTemporary_Alive_Result");
					break;
				case "get_AllMapsAndWorld_Alive":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsAndWorld_Alive_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsAndWorld_Alive_Result");
					break;
				case "get_AllMaps":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_Result");
					break;
				case "get_AllMaps_Spawned":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_Spawned_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_Spawned_Result");
					break;
				case "get_All_AliveOrDead":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "all_AliveOrDead_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "All_AliveOrDead_Result");
					break;
				case "get_Temporary":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "temporary_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "Temporary_Result");
					break;
				case "get_Temporary_Alive":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "temporary_Alive_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "Temporary_Alive_Result");
					break;
				case "get_Temporary_Dead":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "temporary_Dead_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "Temporary_Dead_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_AliveSpawned":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_AliveSpawned_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_AliveSpawned_Result");
					break;
				case "get_AllCaravansAndTravellingTransporters_Alive":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allCaravansAndTravellingTransporters_Alive_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllCaravansAndTravellingTransporters_Alive_Result");
					break;
				case "get_AllCaravansAndTravellingTransporters_AliveOrDead":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allCaravansAndTravellingTransporters_AliveOrDead_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllCaravansAndTravellingTransporters_AliveOrDead_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_Colonists":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_Colonists_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_Colonists_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_FreeColonists_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_NoCryptosleep":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_NoCryptosleep_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_NoCryptosleep_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_AliveSpawned_PrisonersOfColony":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_AliveSpawned_PrisonersOfColony_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_AliveSpawned_PrisonersOfColony_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_NoCryptosleep":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_NoCryptosleep":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_NoCryptosleep_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_NoCryptosleep_Result");
					break;
				case "get_AllMaps_PrisonersOfColonySpawned":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_PrisonersOfColonySpawned_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_PrisonersOfColonySpawned_Result");
					break;
				case "get_AllMaps_PrisonersOfColony":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_PrisonersOfColony_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_PrisonersOfColony_Result");
					break;
				case "get_AllMaps_FreeColonists":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_FreeColonists_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_FreeColonists_Result");
					break;
				case "get_AllMaps_FreeColonistsSpawned":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_FreeColonistsSpawned_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_FreeColonistsSpawned_Result");
					break;
				case "get_AllMaps_FreeColonistsAndPrisonersSpawned":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_FreeColonistsAndPrisonersSpawned_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_FreeColonistsAndPrisonersSpawned_Result");
					break;
				case "get_AllMaps_FreeColonistsAndPrisoners":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_FreeColonistsAndPrisoners_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_FreeColonistsAndPrisoners_Result");
					break;
				case "get_AllMaps_ColonySubhumansSpawnedPlayerControlled":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_ColonySubhumansSpawned_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_ColonySubhumansSpawned_Result");
					break;
				case "get_AllMapsCaravansAndTravellingTransporters_Alive_ColonySubhumans_NoSuspended":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_ColonySubhumans_NoSuspended_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_ColonySubhumans_NoSuspended_Result");
					break;
				case "AllMapsCaravansAndTravellingTransporters_Alive_Colonists_OfXenotype":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMapsCaravansAndTravellingTransporters_Alive_Colonists_OfXenotype_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMapsCaravansAndTravellingTransporters_Alive_Colonists_OfXenotype_Result");
					break;
				case "AllMaps_SpawnedPawnsInFaction":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "allMaps_SpawnedPawnsInFaction_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "AllMaps_SpawnedPawnsInFaction_Result");
					break;
				case "get_HomeMaps_FreeColonistsSpawned":
					fieldPawnFinder = AccessTools.Field(typeof(PawnsFinder), "homeMaps_FreeColonistsSpawned_Result");
					fieldThreadLocal = AccessTools.Field(typeof(ThreadLocalPawnsFinder), "HomeMaps_FreeColonistsSpawned_Result");
					break;
				default:
					throw new Exception($"Unexpected method {methodBase.Name}");
			}

			var local = generator.DeclareLocal(fieldPawnFinder.FieldType);
			/*
			 * List<Pawn> list;
			 * if (UnityData.IsInMainThread)
			 *     list = PawnsFinder.fieldName
			 * else
			 *     list = ThreadLocalPawnsFinder.FieldName.Value
			 */
			yield return CodeInstruction.Call(typeof(UnityData), "get_IsInMainThread");
			yield return new CodeInstruction(OpCodes.Brfalse_S, labelElse);
			yield return new CodeInstruction(OpCodes.Ldsfld, fieldPawnFinder);
			yield return new CodeInstruction(OpCodes.Br_S, labelEnd);
			yield return new CodeInstruction(OpCodes.Ldsfld, fieldThreadLocal).WithLabels(labelElse);
			yield return new CodeInstruction(OpCodes.Call,
				AccessTools.PropertyGetter(fieldThreadLocal.FieldType, "Value"));
			yield return CodeInstruction.StoreLocal(local.LocalIndex).WithLabels(labelEnd);

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo &&
					fieldInfo.Name.EqualsIgnoreCase(fieldPawnFinder.Name))
				{
					yield return CodeInstruction.LoadLocal(local.LocalIndex)
						.WithBlocks(instruction.blocks).WithLabels(instruction.labels);
					continue;
				}
				yield return instruction;
			}
		}
	}
}
