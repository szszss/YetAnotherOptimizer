using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers.ThirdParty;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Patches.ThreadSafe
{
	[HarmonyPatch(typeof(ThoughtSettings))]
	[HarmonyPatch(nameof(ThoughtSettings.CanGetThought), typeof(ThoughtDef), typeof(ThingDef))]
	internal static class AlienRace_ThoughtSettings_CanGetThought
	{
		private static UnfairRwLock _rwLock = new UnfairRwLock();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelThoughtUpdate.Enabled;
		}

		static bool Prefix(ThoughtDef def, ThingDef race,
			Dictionary<uint, bool> ___canGetThoughtCache, out bool __state, ref bool __result)
		{
			__state = false;
			var key = (uint)(def.shortHash | (race.shortHash << 16));
			_rwLock.EnterReadLock();
			try
			{
				if (___canGetThoughtCache.TryGetValue(key, out __result))
				{
					return false;
				}
			}
			finally
			{
				_rwLock.ExitReadLock();
			}
			_rwLock.EnterWriteLock();
			__state = true;
			return true;
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				_rwLock.ExitWriteLock();
		}
	}
}