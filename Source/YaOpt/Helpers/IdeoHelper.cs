using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace YaOpt.Helpers
{
	[StaticConstructorOnStartup]
	internal static class IdeoHelper
	{
		private static readonly HashSet<Type>
			preceptsWithOverridenTick = new HashSet<Type>();
		private static readonly HashSet<Type>
			preceptCompsWithOverridenMemberWillingToDo = new HashSet<Type>();
		private static readonly Dictionary<Ideo, IdeoCache> caches = new Dictionary<Ideo, IdeoCache>();

		public class IdeoCache
		{
			public int Version;
			public List<Precept> PreceptsWithTick = new List<Precept>();
			public List<PreceptComp> CompsWithMemberWillingToDo = new List<PreceptComp>();

			public void Update(Ideo ideo, int cacheVersion)
			{
				Version = cacheVersion;
				PreceptsWithTick.Clear();
				CompsWithMemberWillingToDo.Clear();
				foreach (var precept in ideo.PreceptsListForReading)
				{
					if (preceptsWithOverridenTick.Contains(precept.GetType()))
					{
						PreceptsWithTick.Add(precept);
					}
					foreach (var preceptComp in precept.def.comps)
					{
						if (preceptCompsWithOverridenMemberWillingToDo.Contains(preceptComp.GetType()))
						{
							CompsWithMemberWillingToDo.Add(preceptComp);
						}
					}
				}
			}
		}

		static IdeoHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			TypeSearcher.RegisterSearchingType(typeof(Precept), ProcessPreceptSubtypes);
			TypeSearcher.RegisterSearchingType(typeof(PreceptComp), ProcessPreceptCompSubtypes);
		}

		private static void ClearCache()
		{
			caches.Clear();
		}

		private static void ProcessPreceptSubtypes(Type type)
		{
			if (type != typeof(Precept) && type.IsMethodOverriden(nameof(Precept.Tick)))
			{
				preceptsWithOverridenTick.Add(type);
			}
		}

		private static void ProcessPreceptCompSubtypes(Type type)
		{
			if (type != typeof(PreceptComp) && type.IsMethodOverriden(nameof(PreceptComp.MemberWillingToDo)))
			{
				preceptCompsWithOverridenMemberWillingToDo.Add(type);
			}
		}

		public static void UpdateCache(Ideo ideo, int cacheVersion)
		{
			if (!caches.TryGetValue(ideo, out var cache))
			{
				cache = new IdeoCache();
				caches[ideo] = cache;
			}
			cache.Update(ideo, cacheVersion);
		}

		public static IdeoCache GetCache(Ideo ideo)
		{
			caches.TryGetValue(ideo, out var cache);
			return cache;
		}

		public static List<Precept> GetPreceptsWithOverridenTick(Ideo ideo)
		{
			if (caches.TryGetValue(ideo, out var cache))
			{
				if (cache.Version == ideo.currentCacheId)
					return cache.PreceptsWithTick;
			}
			return ideo.PreceptsListForReading;
		}
	}
}