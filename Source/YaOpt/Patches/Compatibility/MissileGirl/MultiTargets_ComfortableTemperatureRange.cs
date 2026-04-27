using HarmonyLib;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThirdParty;

namespace YaOpt.Patches.Compatibility.MissileGirl
{
	[ManualPatch]
	internal static class MultiTargets_ComfortableTemperatureRange
	{
		private static UnfairRwLock _rwLock = new UnfairRwLock();

		private static AccessTools.FieldRef<int> _lastTickFieldRef;

		private static AccessTools.FieldRef<Dictionary<int, FloatRange>> _tempCacheFieldRef;

		private static bool _callbackRegistered = false;

		static void Patch(Harmony harmony)
		{
			if (YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasType("MissileGirl.Optimizations.GenTemperature_Patch"))
			{
				var type = AccessTools.TypeByName(
					"MissileGirl.Optimizations.GenTemperature_Patch");
				var helperType = typeof(MultiTargets_ComfortableTemperatureRange);
				harmony.Patch(AccessTools.Method(type, "Prefix"),
					prefix: new HarmonyMethod(helperType, nameof(EnterReadLock)),
					finalizer: new HarmonyMethod(helperType, nameof(ExitReadLock)));
				harmony.Patch(AccessTools.Method(type, "Postfix"),
					prefix: new HarmonyMethod(helperType, nameof(EnterWriteLock)),
					finalizer: new HarmonyMethod(helperType, nameof(ExitWriteLock)));

				if (!_callbackRegistered)
				{
					_callbackRegistered = true;
					_lastTickFieldRef = AccessTools.StaticFieldRefAccess<int>(
						AccessTools.Field(type, "LastTick"));
					_tempCacheFieldRef = AccessTools.StaticFieldRefAccess<Dictionary<int, FloatRange>>(
						AccessTools.Field(type, "tempCache"));
					UpdateCallbackHelper.RegisterPreTickCallback(PreTick);
				}
			}
		}

		private static void PreTick(int tick)
		{
			_lastTickFieldRef() = tick;
			_tempCacheFieldRef().Clear();
		}

		private static void EnterReadLock(out bool __state)
		{
			__state = true;
			_rwLock.EnterReadLock();
		}

		private static void ExitReadLock(bool __state)
		{
			if (__state)
				_rwLock.ExitReadLock();
		}

		private static void EnterWriteLock(Pawn p, Dictionary<int, FloatRange> ___tempCache,
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

		private static void ExitWriteLock(bool __state)
		{
			if (__state)
				_rwLock.ExitWriteLock();
		}
	}
}