using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	/// <summary>
	/// Fixed a bug caused by a race condition in TileTemperaturesComp.RetrieveCachedData.
	/// </summary>
	[HarmonyPatch]
	internal static class MultiTargets_TileTemperaturesComp
	{
		private static SpinLock _spinLock = new SpinLock();

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(TileTemperaturesComp),
				nameof(TileTemperaturesComp.WorldComponentTick));
			yield return AccessTools.Method(typeof(TileTemperaturesComp),
				"RetrieveCachedData");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			_spinLock.Enter(ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				_spinLock.Exit();
		}
	}
}