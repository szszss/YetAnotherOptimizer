using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace YaOpt.Helpers
{
	internal static class CacheWarmer
	{
		public static void PostInit()
		{
			try
			{
				WarmPawnRenderNodeWorker();
				WarmDubsBadHygiene();
			}
			catch (Exception ex)
			{
				YaOptMod.Error($"Error when warm up the caches: {ex.ToString()}");
			}
		}

		private static void WarmPawnRenderNodeWorker()
		{
			var count = 0;
			foreach (Type type in typeof(PawnRenderNodeWorker).AllSubclassesNonAbstract())
			{
				GenWorker<PawnRenderNodeWorker>.Get(type);
				count++;
			}
			YaOptMod.Debug($"CacheWarmer: Warm {count} PawnRenderNodeWorkers");
		}

		private static void WarmDubsBadHygiene()
		{
			if (!YaOptGlobal.HasMod("dubwise.dubsbadhygiene"))
				return;

			var typeCompWaterFillable = AccessTools.TypeByName("DubsBadHygiene.CompWaterFillable");
			var typeBuildingWashBucket = AccessTools.TypeByName("DubsBadHygiene.Building_washbucket");
			var fieldCachedRefillableDefs = AccessTools.Field(
				AccessTools.TypeByName("DubsBadHygiene.WorkGiver_RefillWater"),
				"cachedRefillableDefs"
			);
			var cachedRefillableDefs = new List<ThingDef>();
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
			{
				if (thingDef.HasComp(typeCompWaterFillable))
				{
					cachedRefillableDefs.Add(thingDef);
				}
				if (typeBuildingWashBucket.IsAssignableFrom(thingDef.thingClass))
				{
					cachedRefillableDefs.Add(thingDef);
				}
			}
			fieldCachedRefillableDefs.SetValue(null, cachedRefillableDefs);
			YaOptMod.Debug("CacheWarmer: Warm Dubs Bad Hygiene water storage cache");
		}
	}
}