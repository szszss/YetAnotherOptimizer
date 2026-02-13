using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch]
	internal static class RimWorld_ListerBuldingOfDefInProximity_GetForCell
	{
		private static SpinLock spinLock = new SpinLock();

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(ListerBuldingOfDefInProximity), nameof(ListerBuldingOfDefInProximity.GetForCell),
				new Type[]
				{
					typeof(IntVec3), typeof(float), typeof(List<MeditationFocusOffsetPerBuilding>), typeof(Thing)
				});
			yield return AccessTools.Method(
				typeof(ListerBuldingOfDefInProximity), nameof(ListerBuldingOfDefInProximity.GetForCell),
				new Type[]
				{
					typeof(IntVec3), typeof(float), typeof(ThingDef), typeof(Thing)
				});
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			spinLock.Enter(ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				spinLock.Exit();
		}
	}
}