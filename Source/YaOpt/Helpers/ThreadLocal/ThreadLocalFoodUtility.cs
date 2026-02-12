using RimWorld;
using System.Collections.Generic;
using System.Threading;
using Verse;
using static YaOpt.Helpers.ThreadLocal.ThreadLocalHelper;

namespace YaOpt.Helpers.ThreadLocal
{
	internal static class ThreadLocalFoodUtility
	{
		public static ThreadLocal<HashSet<Thing>> Filtered =
			new ThreadLocal<HashSet<Thing>>(NewThingSet);

		public static ThreadLocal<List<Pawn>> TmpPredatorCandidates =
			new ThreadLocal<List<Pawn>>(NewPawnList);

		public static ThreadLocal<List<FoodUtility.ThoughtFromIngesting>> IngestThoughts =
			new ThreadLocal<List<FoodUtility.ThoughtFromIngesting>>(NewList<FoodUtility.ThoughtFromIngesting>);

		public static ThreadLocal<List<ThoughtDef>> ExtraIngestThoughtsFromTraits =
			new ThreadLocal<List<ThoughtDef>>(NewList<ThoughtDef>);

		public static ThreadLocal<Dictionary<Ideo, Dictionary<HistoryEventDef, List<Precept>>>> IdeoIngestThoughtsCache =
			new ThreadLocal<Dictionary<Ideo, Dictionary<HistoryEventDef, List<Precept>>>>(
				NewDictionary<Ideo, Dictionary<HistoryEventDef, List<Precept>>>);

		static ThreadLocalFoodUtility()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			Filtered.Dispose();
			Filtered = new ThreadLocal<HashSet<Thing>>(NewThingSet);
			TmpPredatorCandidates.Dispose();
			TmpPredatorCandidates = new ThreadLocal<List<Pawn>>(NewPawnList);
			IngestThoughts.Dispose();
			IngestThoughts = new ThreadLocal<List<FoodUtility.ThoughtFromIngesting>>(NewList<FoodUtility.ThoughtFromIngesting>);
			ExtraIngestThoughtsFromTraits.Dispose();
			ExtraIngestThoughtsFromTraits = new ThreadLocal<List<ThoughtDef>>(NewList<ThoughtDef>);
			IdeoIngestThoughtsCache.Dispose();
			IdeoIngestThoughtsCache = new ThreadLocal<Dictionary<Ideo, Dictionary<HistoryEventDef, List<Precept>>>>(
					NewDictionary<Ideo, Dictionary<HistoryEventDef, List<Precept>>>);
		}
	}
}