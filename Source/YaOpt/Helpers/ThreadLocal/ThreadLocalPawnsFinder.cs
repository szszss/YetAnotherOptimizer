using RimWorld;
using System.Collections.Generic;
using System.Threading;
using Verse;
using static YaOpt.Helpers.ThreadLocal.ThreadLocalHelper;

namespace YaOpt.Helpers.ThreadLocal
{
	internal static class ThreadLocalPawnsFinder
	{
		public static ThreadLocal<Dictionary<Faction, List<Pawn>>> AllMaps_SpawnedPawnsInFaction_Result =
			new ThreadLocal<Dictionary<Faction, List<Pawn>>>(NewDictionary<Faction, List<Pawn>>);

		public static ThreadLocal<List<Pawn>> AllMapsWorldAndTemporary_AliveOrDead_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsWorldAndTemporary_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsAndWorld_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_Spawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> All_AliveOrDead_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> Temporary_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> Temporary_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> Temporary_Dead_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_AliveSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllCaravansAndTravellingTransporters_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllCaravansAndTravellingTransporters_AliveOrDead_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_Colonists_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_NoCryptosleep_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_AliveSpawned_PrisonersOfColony_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_NoCryptosleep_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_PrisonersOfColonySpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_PrisonersOfColony_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_FreeColonists_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_FreeColonistsSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_FreeColonistsAndPrisonersSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_FreeColonistsAndPrisoners_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMaps_ColonySubhumansSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_ColonySubhumans_NoSuspended_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllMapsCaravansAndTravellingTransporters_Alive_Colonists_OfXenotype_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> HomeMaps_FreeColonistsSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);

		static ThreadLocalPawnsFinder()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			AllMaps_SpawnedPawnsInFaction_Result.Dispose();
			AllMaps_SpawnedPawnsInFaction_Result = new ThreadLocal<Dictionary<Faction, List<Pawn>>>(NewDictionary<Faction, List<Pawn>>);
			AllMapsWorldAndTemporary_AliveOrDead_Result.Dispose();
			AllMapsWorldAndTemporary_AliveOrDead_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsWorldAndTemporary_Alive_Result.Dispose();
			AllMapsWorldAndTemporary_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsAndWorld_Alive_Result.Dispose();
			AllMapsAndWorld_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_Result.Dispose();
			AllMaps_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_Spawned_Result.Dispose();
			AllMaps_Spawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			All_AliveOrDead_Result.Dispose();
			All_AliveOrDead_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			Temporary_Result.Dispose();
			Temporary_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			Temporary_Alive_Result.Dispose();
			Temporary_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			Temporary_Dead_Result.Dispose();
			Temporary_Dead_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllCaravansAndTravellingTransporters_Alive_Result.Dispose();
			AllCaravansAndTravellingTransporters_Alive_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllCaravansAndTravellingTransporters_AliveOrDead_Result.Dispose();
			AllCaravansAndTravellingTransporters_AliveOrDead_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_Colonists_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_Colonists_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_NoCryptosleep_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction_NoCryptosleep_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_PrisonersOfColony_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_PrisonersOfColony_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners_NoCryptosleep_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_NoCryptosleep_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonistsAndPrisoners_NoCryptosleep_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_PrisonersOfColonySpawned_Result.Dispose();
			AllMaps_PrisonersOfColonySpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_PrisonersOfColony_Result.Dispose();
			AllMaps_PrisonersOfColony_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_FreeColonists_Result.Dispose();
			AllMaps_FreeColonists_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_FreeColonistsSpawned_Result.Dispose();
			AllMaps_FreeColonistsSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_FreeColonistsAndPrisonersSpawned_Result.Dispose();
			AllMaps_FreeColonistsAndPrisonersSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_FreeColonistsAndPrisoners_Result.Dispose();
			AllMaps_FreeColonistsAndPrisoners_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMaps_ColonySubhumansSpawned_Result.Dispose();
			AllMaps_ColonySubhumansSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_ColonySubhumans_NoSuspended_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_ColonySubhumans_NoSuspended_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllMapsCaravansAndTravellingTransporters_Alive_Colonists_OfXenotype_Result.Dispose();
			AllMapsCaravansAndTravellingTransporters_Alive_Colonists_OfXenotype_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
			HomeMaps_FreeColonistsSpawned_Result.Dispose();
			HomeMaps_FreeColonistsSpawned_Result = new ThreadLocal<List<Pawn>>(NewPawnList);
		}
	}
}
