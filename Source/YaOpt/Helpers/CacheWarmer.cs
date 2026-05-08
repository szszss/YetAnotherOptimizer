using System;
using Verse;

namespace YaOpt.Helpers
{
	internal static class CacheWarmer
	{
		public static void PostInit()
		{
			var count = 0;
			foreach (Type type in typeof(PawnRenderNodeWorker).AllSubclassesNonAbstract())
			{
				GenWorker<PawnRenderNodeWorker>.Get(type);
				count++;
			}
			YaOptMod.Debug($"CacheWarmer PostInit: Warm {count} PawnRenderNodeWorkers");
		}
	}
}