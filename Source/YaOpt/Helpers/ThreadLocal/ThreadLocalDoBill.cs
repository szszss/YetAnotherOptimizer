using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;
using static YaOpt.Helpers.ThreadLocal.ThreadLocalHelper;

namespace YaOpt.Helpers.ThreadLocal
{
	internal static class ThreadLocalDoBill
	{
		public static ThreadLocal<List<IngredientCount>> MissingIngredients =
			new ThreadLocal<List<IngredientCount>>(NewList<IngredientCount>);

		public static ThreadLocal<List<Thing>> TmpMissingUniqueIngredients =
			new ThreadLocal<List<Thing>>(NewThingList);

		public static ThreadLocal<List<Thing>> RelevantThings =
			new ThreadLocal<List<Thing>>(NewThingList);

		public static ThreadLocal<HashSet<Thing>> ProcessedThings =
			new ThreadLocal<HashSet<Thing>>(NewThingSet);

		public static ThreadLocal<List<Thing>> NewRelevantThings =
			new ThreadLocal<List<Thing>>(NewThingList);

		public static ThreadLocal<List<Thing>> TmpMedicine =
			new ThreadLocal<List<Thing>>(NewThingList);

		public static ThreadLocal<object> AvailableCounts =
			new ThreadLocal<object>(NewDefCountList);

		private static readonly ConstructorInfo constructorDefCountList;

		static ThreadLocalDoBill()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			constructorDefCountList = AccessTools.Constructor(
				AccessTools.TypeByName("RimWorld.WorkGiver_DoBill/DefCountList"));
		}

		private static void ClearCache()
		{
			MissingIngredients.Dispose();
			MissingIngredients = new ThreadLocal<List<IngredientCount>>(NewList<IngredientCount>);
			TmpMissingUniqueIngredients.Dispose();
			TmpMissingUniqueIngredients = new ThreadLocal<List<Thing>>(NewThingList);
			RelevantThings.Dispose();
			RelevantThings = new ThreadLocal<List<Thing>>(NewThingList);
			ProcessedThings.Dispose();
			ProcessedThings = new ThreadLocal<HashSet<Thing>>(NewThingSet);
			NewRelevantThings.Dispose();
			NewRelevantThings = new ThreadLocal<List<Thing>>(NewThingList);
			TmpMedicine.Dispose();
			TmpMedicine = new ThreadLocal<List<Thing>>(NewThingList);
			AvailableCounts.Dispose();
			AvailableCounts = new ThreadLocal<object>(NewDefCountList);
		}

		private static object NewDefCountList() => constructorDefCountList.Invoke(null);
	}
}