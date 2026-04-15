using HarmonyLib;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers.ThirdParty;

namespace YaOpt.Patches.Compatibility.MissileGirl
{
	[ManualPatch]
	internal static class MultiTargets_ComfortableTemperatureRange
	{
		private static UnfairRwLock _rwLock = new UnfairRwLock();

		static void Patch(Harmony harmony)
		{
			if (YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasType("RocketMan.Optimizations.GenTemperature_Patch"))
			{
				var type = AccessTools.TypeByName(
					"RocketMan.Optimizations.GenTemperature_Patch");
				var helperType = typeof(MultiTargets_ComfortableTemperatureRange);
				harmony.Patch(AccessTools.Method(type, "Prefix"),
					prefix: new HarmonyMethod(helperType, nameof(EnterReadLock)),
					finalizer: new HarmonyMethod(helperType, nameof(ExitReadLock)));
				harmony.Patch(AccessTools.Method(type, "Postfix"),
					prefix: new HarmonyMethod(helperType, nameof(EnterWriteLock)),
					finalizer: new HarmonyMethod(helperType, nameof(ExitWriteLock)));
			}
		}

		public static void EnterReadLock(out bool __state)
		{
			__state = true;
			_rwLock.EnterReadLock();
		}

		public static void ExitReadLock(bool __state)
		{
			if (__state)
				_rwLock.ExitReadLock();
		}

		public static void EnterWriteLock(Pawn p, Dictionary<int, FloatRange> ___tempCache,
			out bool __state)
		{
			_rwLock.EnterReadLock();
			try
			{
				if (___tempCache.ContainsKey(p.thingIDNumber))
				{
					__state = false;
					return;
				}
			}
			finally
			{
				_rwLock.ExitReadLock();
			}
			__state = true;
			_rwLock.EnterWriteLock();
		}

		public static void ExitWriteLock(bool __state)
		{
			if (__state)
				_rwLock.ExitWriteLock();
		}
	}
}