using System;
using System.Collections.Generic;
using System.Threading;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.VehicleMapFramework
{
	internal static class VehicleMapFrameworkCompatibility
	{
		private static bool _inited;

		private static ThreadLocal<ThreadLocalData> _threadLocalMapData =
			new ThreadLocal<ThreadLocalData>(() => new ThreadLocalData());

		private class ThreadLocalData
		{
			public int LastBaseMapAndVehicleMapsUpdate = -1;
			public Map LastThisMap;
			public readonly HashSet<Map> MapsWithThisMap = new HashSet<Map>();
			public readonly HashSet<Map> MapsWithoutThisMap = new HashSet<Map>();
		}

		internal class VehiclePawnWithMapStub : Pawn
		{
			public Map VehicleMap => throw new NotImplementedException();
		}

		internal class CompZiplineStub : ThingComp
		{
			public Thing Pair => throw new NotImplementedException();
		}

		internal static IReadOnlyList<ThingDef> ZiplineDefsStub => throw new NotImplementedException();

		public static void Init()
		{
			if (!_inited)
			{
				_inited = true;
				UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			}
		}

		private static void ClearCache()
		{
			_threadLocalMapData.Dispose();
			_threadLocalMapData = new ThreadLocal<ThreadLocalData>(() => new ThreadLocalData());
		}

		#region BaseMapAndVehicleMaps

		public static HashSet<Map> GetThreadSafeBaseMapAndVehicleMaps(HashSet<Map> originalMap,
			Map thisMap, bool includeThisMap)
		{
			var data = _threadLocalMapData.Value;
			var mapsWithThis = data.MapsWithThisMap;
			var mapsWithoutThis = data.MapsWithoutThisMap;
			if (data.LastBaseMapAndVehicleMapsUpdate != GenTicks.TicksGame || data.LastThisMap != thisMap)
			{
				data.LastBaseMapAndVehicleMapsUpdate = GenTicks.TicksGame;
				data.LastThisMap = thisMap;
				mapsWithThis.Clear();
				mapsWithThis.AddRange(originalMap);
				mapsWithThis.Add(thisMap);
				mapsWithoutThis.Clear();
				mapsWithoutThis.AddRange(originalMap);
				mapsWithoutThis.Remove(thisMap);
			}
			return includeThisMap
				? mapsWithThis
				: mapsWithoutThis;
		}

		#endregion

		public static bool IsVehicleMapOfStub(Map map, out VehiclePawnWithMapStub vehicle)
		{
			throw new NotImplementedException();
		}
	}
}