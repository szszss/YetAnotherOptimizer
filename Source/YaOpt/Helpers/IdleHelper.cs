using LudeonTK;
using RimWorld;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Verse;

namespace YaOpt.Helpers
{
	internal static class IdleHelper
	{
		private const int DEFAULT_LAYDOWN_INTERVAL = 211;
		private const int DEFAULT_WANDER_INTERVAL = 125;
		private const int UPDATE_INTERVAL = 10;

		private static int _lastUpdateTick = -1;

		private static int _lyingColonists;

		private static int _idleColonists;

		private static int _getUpInterval;

		private static int _stopWanderInterval;

		static IdleHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreTickCallback(PreTick);
		}

		private static void ClearCache()
		{
			_lastUpdateTick = -1;
		}

		private static void PreTick(int tick)
		{
			if (!YaOptGlobal.IsIdleThrottleEnabled)
				return;

			if (_lastUpdateTick != -1 && tick - _lastUpdateTick < UPDATE_INTERVAL)
			{
				return;
			}
			_lastUpdateTick = tick;
			var settings = YaOptGlobal.Settings;
			var _getUpDynamic = settings.IdleThrottleGetUpDynamic;
			var _stopWanderDynamic = settings.IdleThrottleStopWanderDynamic;
			_getUpInterval = settings.IdleThrottleGetUpIntervalMin;
			_stopWanderInterval = settings.IdleThrottleStopWanderIntervalMin;
			if (!_getUpDynamic && !_stopWanderDynamic)
				return;

			_lyingColonists = 0;
			_idleColonists = 0;
			var _getUpMin = settings.IdleThrottleGetUpIntervalMin;
			var _getUpMax = settings.IdleThrottleGetUpIntervalMax;
			var _getUpPeopleMin = settings.IdleThrottleGetUpPeopleMin;
			var _getUpPeopleMax = settings.IdleThrottleGetUpPeopleMax;
			var _stopWanderMin = settings.IdleThrottleStopWanderIntervalMin;
			var _stopWanderMax = settings.IdleThrottleStopWanderIntervalMax;
			var _stopWanderPeopleMin = settings.IdleThrottleStopWanderPeopleMin;
			var _stopWanderPeopleMax = settings.IdleThrottleStopWanderPeopleMax;
			foreach (var pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
			{
				if (_getUpDynamic && !pawn.Downed && RestUtility.IsLayingForJobCleanup(pawn))
				{
					_lyingColonists++;
				}

				if (_stopWanderDynamic && pawn?.mindState.IsIdle == true)
				{
					_idleColonists++;
				}
			}
			if (_getUpDynamic)
			{
				if (_lyingColonists <= _getUpPeopleMin)
					_getUpInterval = _getUpMin;
				else if (_lyingColonists >= _getUpPeopleMax)
					_getUpInterval = _getUpMax;
				else
					_getUpInterval = (int)math.lerp(_getUpMin, _getUpMax,
						(float)(_lyingColonists - _getUpPeopleMin) / (_getUpPeopleMax - _getUpPeopleMin));
			}
			if (_stopWanderDynamic)
			{
				if (_idleColonists <= _stopWanderPeopleMin)
					_stopWanderInterval = _stopWanderMin;
				else if (_idleColonists >= _stopWanderPeopleMax)
					_stopWanderInterval = _stopWanderMax;
				else
					_stopWanderInterval = (int)math.lerp(_stopWanderMin, _stopWanderMax,
						(float)(_idleColonists - _stopWanderPeopleMin) / (_stopWanderPeopleMax - _stopWanderPeopleMin));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetUpInterval()
		{
			return _getUpInterval;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int StopWanderInterval(int oldValue)
		{
			var fluctuation = oldValue - DEFAULT_WANDER_INTERVAL;
			return _stopWanderInterval + fluctuation;
		}

		public static bool CanUseBedNowLight(Pawn actor, Thing bedThing)
		{
			if (bedThing.Destroyed)
				return false;
			if (!bedThing.Spawned)
				return false;
			if (bedThing.Map != actor.MapHeld)
				return false;
			return true;
		}

		[DebugOutput("YaOpt", true)]
		public static void PrintIdleThrottleInfo()
		{
			YaOptMod.Log($"Idle Throttle Enabled: {YaOptGlobal.Settings.OptIdleThrottle.Enabled}");
			YaOptMod.Log($"Lying pawns: {_lyingColonists}");
			YaOptMod.Log($"Idle pawns: {_idleColonists}");
			YaOptMod.Log($"Get up interval: {GetUpInterval()}");
			YaOptMod.Log($"Stop wander interval: {StopWanderInterval(125)}~{StopWanderInterval(200)}");
		}
	}
}