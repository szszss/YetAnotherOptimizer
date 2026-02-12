using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch]
	internal static class MultiTargets_Rand
	{
		private static int lastThreadId;
		private static readonly object objLock = new object();

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.ChanceSeeded));
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.ValueSeeded));
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.RangeSeeded),
				new[] { typeof(float), typeof(float), typeof(int) });
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.RangeSeeded),
				new[] { typeof(int), typeof(int), typeof(int) });
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.RangeInclusiveSeeded));
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.PushState), Type.EmptyTypes);
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.PushState), new[] { typeof(int) });
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.PopState));
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(out bool __state)
		{
			/*var threadId = Thread.CurrentThread.ManagedThreadId;
			if (lastThreadId != threadId)
			{
				lastThreadId = threadId;
				YaOptMod.Warning($"Change rand state fron different thread: {threadId}");
			}*/
			__state = false;
			Monitor.Enter(objLock, ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				Monitor.Exit(objLock);
		}
	}
}