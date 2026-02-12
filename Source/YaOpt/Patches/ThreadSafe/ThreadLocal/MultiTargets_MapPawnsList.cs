using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_MapPawnsList
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.AllPawns));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.AllPawnsUnspawned));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.PrisonersOfColony));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.AllHumanlike));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.AllHumanlikeSpawned));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.FreeColonistsAndPrisoners));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.FreeAdultColonistsSpawned));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.FreeColonistsAndPrisonersSpawned));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedPawnsWithAnyHediff));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedHumanlikesWithAnyHediff));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedAnimalsWithAnyHediff));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedHungryPawns));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedPawnsWithMiscNeeds));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.ColonyAnimals));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedColonyAnimals));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedColonyMechs));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.ColonySubhumansControllable));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedColonySubhumansPlayerControlled));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedDownedPawns));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedPawnsWhoShouldHaveSurgeryDoneNow));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SpawnedPawnsWhoShouldHaveInventoryUnloaded));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount));
			yield return AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.SlavesAndPrisonersOfColonySpawned));
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			var labelElse = generator.DefineLabel();
			var labelEnd = generator.DefineLabel();
			string fieldName;
			var listType = typeof(List<Pawn>);
			var threadLocalType = typeof(ThreadLocal<List<Pawn>>);
			switch (methodBase.Name)
			{
				case "get_AllPawns": fieldName = "allPawnsResult"; break;
				case "get_AllPawnsUnspawned": fieldName = "allPawnsUnspawnedResult"; break;
				case "get_PrisonersOfColony": fieldName = "prisonersOfColonyResult"; break;
				case "get_AllHumanlike": fieldName = "humanlikePawnsResult"; break;
				case "get_AllHumanlikeSpawned": fieldName = "humanlikeSpawnedPawnsResult"; break;
				case "get_FreeColonistsAndPrisoners": fieldName = "freeColonistsAndPrisonersResult"; break;
				case "get_AnyPawnBlockingMapRemoval": fieldName = "tmpThings";
					listType = typeof(List<Thing>);
					threadLocalType =typeof(ThreadLocal<List<Thing>>);
					break;
				case "get_FreeAdultColonistsSpawned": fieldName = "freeAdultColonistsSpawnedResult"; break;
				case "get_FreeColonistsAndPrisonersSpawned": fieldName = "freeColonistsAndPrisonersSpawnedResult"; break;
				case "get_SpawnedPawnsWithAnyHediff": fieldName = "spawnedPawnsWithAnyHediffResult"; break;
				case "get_SpawnedHumanlikesWithAnyHediff": fieldName = "spawnedHumanlikesWithAnyHediffResult"; break;
				case "get_SpawnedAnimalsWithAnyHediff": fieldName = "spawnedAnimalsWithAnyHediffResult"; break;
				case "get_SpawnedHungryPawns": fieldName = "spawnedHungryPawnsResult"; break;
				case "get_SpawnedPawnsWithMiscNeeds": fieldName = "spawnedPawnsWithMiscNeedsResult"; break;
				case "get_ColonyAnimals": fieldName = "colonyAnimalsResult"; break;
				case "get_SpawnedColonyAnimals": fieldName = "spawnedColonyAnimalsResult"; break;
				case "get_SpawnedColonyMechs": fieldName = "spawnedColonyMechsResult"; break;
				case "get_ColonySubhumansControllable": fieldName = "colonySubhumansResult"; break;
				case "get_SpawnedColonySubhumansPlayerControlled": fieldName = "spawnedColonySubhumansResult"; break;
				case "get_SpawnedDownedPawns": fieldName = "spawnedDownedPawnsResult"; break;
				case "get_SpawnedPawnsWhoShouldHaveSurgeryDoneNow": fieldName = "spawnedPawnsWhoShouldHaveSurgeryDoneNowResult"; break;
				case "get_SpawnedPawnsWhoShouldHaveInventoryUnloaded": fieldName = "spawnedPawnsWhoShouldHaveInventoryUnloadedResult"; break;
				case "get_FreeColonistsSpawnedOrInPlayerEjectablePodsCount": fieldName = "tmpThings";
					listType = typeof(List<Thing>);
					threadLocalType =typeof(ThreadLocal<List<Thing>>);
					break;
				case "get_SlavesAndPrisonersOfColonySpawned": fieldName = "slavesAndPrisonersOfColonySpawnedResult"; break;
				default: throw new Exception($"Unexpected method {methodBase.Name}");
			}
			var local = generator.DeclareLocal(listType);
			/*
			 * List<Pawn> list;
			 * if (UnityData.IsInMainThread)
			 *	 list = this.xxx
			 * else
			 *   list = ThreadLocalMapPawns.Xxx
			 */
			yield return CodeInstruction.Call(typeof(UnityData), "get_IsInMainThread");
			yield return new CodeInstruction(OpCodes.Brfalse_S, labelElse);
			yield return CodeInstruction.LoadArgument(0);
			yield return new CodeInstruction(OpCodes.Ldfld, 
				AccessTools.Field(typeof(MapPawns), fieldName));
			yield return new CodeInstruction(OpCodes.Br_S, labelEnd);
			yield return new CodeInstruction(OpCodes.Ldsfld, 
				AccessTools.Field(typeof(ThreadLocalMapPawns), fieldName.CapitalizeFirst())).WithLabels(labelElse);
			yield return new CodeInstruction(OpCodes.Call,
				AccessTools.PropertyGetter(threadLocalType, "Value"));
			yield return CodeInstruction.StoreLocal(local.LocalIndex).WithLabels(labelEnd);

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo fieldInfo && 
				    fieldInfo.Name.EqualsIgnoreCase(fieldName))
				{
					yield return new CodeInstruction(OpCodes.Pop).WithBlocks(instruction.blocks);
					yield return CodeInstruction.LoadLocal(local.LocalIndex).WithBlocks(instruction.blocks);
					continue;
				}
				yield return instruction;
			}
		}
	}
}