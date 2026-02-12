using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Verse;
using static YaOpt.Helpers.ThreadLocal.ThreadLocalHelper;

namespace YaOpt.Helpers.ThreadLocal
{
	internal static class ThreadLocalMapPawns
	{
		public static ThreadLocal<List<Pawn>> AllPawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> AllPawnsUnspawnedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> PrisonersOfColonyResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> HumanlikePawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> HumanlikeSpawnedPawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> FreeColonistsAndPrisonersResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> FreeAdultColonistsSpawnedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> FreeColonistsAndPrisonersSpawnedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedPawnsWithAnyHediffResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedHumanlikesWithAnyHediffResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedAnimalsWithAnyHediffResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedHungryPawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedPawnsWithMiscNeedsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> ColonyAnimalsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedColonyAnimalsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedColonyMechsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> ColonySubhumansResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedColonySubhumansResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedDownedPawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedPawnsWhoShouldHaveSurgeryDoneNowResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SpawnedPawnsWhoShouldHaveInventoryUnloadedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Pawn>> SlavesAndPrisonersOfColonySpawnedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
		public static ThreadLocal<List<Thing>> TmpThings = new ThreadLocal<List<Thing>>(NewThingList);
		public static ThreadLocal<List<Faction>> TmpFactionsOnMap = new ThreadLocal<List<Faction>>(NewList<Faction>);

		private static readonly ConcurrentBag<List<Pawn>>[] takenPooledFactionListStack = new ConcurrentBag<List<Pawn>>[]
		{
			new ConcurrentBag<List<Pawn>>(),
			new ConcurrentBag<List<Pawn>>(),
			new ConcurrentBag<List<Pawn>>()
		};

		private static int stackDepth = 0;

		static ThreadLocalMapPawns()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreTickCallback((_) => { PushPooledListsStack(); });
			UpdateCallbackHelper.RegisterPostTickCallback((_) => { PopPooledListsStack(); });
			UpdateCallbackHelper.RegisterPostRenderCallback((_) =>
			{
				PopPooledListsStack();
				PushPooledListsStack();
			});
		}

		private static void ClearCache()
		{
			AllPawnsResult.Dispose();
			AllPawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			AllPawnsUnspawnedResult.Dispose();
			AllPawnsUnspawnedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			PrisonersOfColonyResult.Dispose();
			PrisonersOfColonyResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			HumanlikePawnsResult.Dispose();
			HumanlikePawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			HumanlikeSpawnedPawnsResult.Dispose();
			HumanlikeSpawnedPawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			FreeColonistsAndPrisonersResult.Dispose();
			FreeColonistsAndPrisonersResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			FreeAdultColonistsSpawnedResult.Dispose();
			FreeAdultColonistsSpawnedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			FreeColonistsAndPrisonersSpawnedResult.Dispose();
			FreeColonistsAndPrisonersSpawnedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedPawnsWithAnyHediffResult.Dispose();
			SpawnedPawnsWithAnyHediffResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedHumanlikesWithAnyHediffResult.Dispose();
			SpawnedHumanlikesWithAnyHediffResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedAnimalsWithAnyHediffResult.Dispose();
			SpawnedAnimalsWithAnyHediffResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedHungryPawnsResult.Dispose();
			SpawnedHungryPawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedPawnsWithMiscNeedsResult.Dispose();
			SpawnedPawnsWithMiscNeedsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			ColonyAnimalsResult.Dispose();
			ColonyAnimalsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedColonyAnimalsResult.Dispose();
			SpawnedColonyAnimalsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedColonyMechsResult.Dispose();
			SpawnedColonyMechsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			ColonySubhumansResult.Dispose();
			ColonySubhumansResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedColonySubhumansResult.Dispose();
			SpawnedColonySubhumansResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedDownedPawnsResult.Dispose();
			SpawnedDownedPawnsResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedPawnsWhoShouldHaveSurgeryDoneNowResult.Dispose();
			SpawnedPawnsWhoShouldHaveSurgeryDoneNowResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SpawnedPawnsWhoShouldHaveInventoryUnloadedResult.Dispose();
			SpawnedPawnsWhoShouldHaveInventoryUnloadedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			SlavesAndPrisonersOfColonySpawnedResult.Dispose();
			SlavesAndPrisonersOfColonySpawnedResult = new ThreadLocal<List<Pawn>>(NewPawnList);
			TmpThings.Dispose();
			TmpThings = new ThreadLocal<List<Thing>>(NewThingList);
			TmpFactionsOnMap.Dispose();
			TmpFactionsOnMap = new ThreadLocal<List<Faction>>(NewList<Faction>);

			while (stackDepth >= 0)
			{
				PopPooledListsStack();
			}
			PushPooledListsStack();
		}

		public static List<Pawn> GetPooledList()
		{
			if (stackDepth < 0)
			{
				throw new Exception("Unable to get pooled list. The stack is empty.");
			}
			var list = ConcurrentPool<List<Pawn>>.Get();
			takenPooledFactionListStack[stackDepth].Add(list);
			return list;
		}

		public static void PushPooledListsStack()
		{
			if (stackDepth >= takenPooledFactionListStack.Length - 1)
			{
				throw new Exception("Unable to push PooledListsStack. The stack is already full.");
			}
			stackDepth++;
		}

		public static void PopPooledListsStack()
		{
			if (stackDepth < 0)
			{
				throw new Exception("Unable to pop PooledListsStack. The stack is already empty.");
			}
			var lists = takenPooledFactionListStack[stackDepth--];
			while (lists.TryTake(out var list))
			{
				list.Clear();
				ConcurrentPool<List<Pawn>>.Return(list);
			}
		}
	}
}