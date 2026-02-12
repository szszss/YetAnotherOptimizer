using System.Collections.Generic;
using System.Threading;
using Verse;
using static YaOpt.Helpers.ThreadLocal.ThreadLocalHelper;

namespace YaOpt.Helpers.ThreadLocal
{
	internal static class ThreadLocalConstructDeliverResources
	{
		public static ThreadLocal<List<Thing>> ResourcesAvailable = 
			new ThreadLocal<List<Thing>>(NewThingList);

		public static ThreadLocal<Dictionary<ThingDef, int>> MissingResources = 
			new ThreadLocal<Dictionary<ThingDef, int>>(NewDictionary<ThingDef, int>);

		static ThreadLocalConstructDeliverResources()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			ResourcesAvailable.Dispose();
			ResourcesAvailable = new ThreadLocal<List<Thing>>(NewThingList);
			MissingResources.Dispose();
			MissingResources = new ThreadLocal<Dictionary<ThingDef, int>>(NewDictionary<ThingDef, int>);
		}
	}
}