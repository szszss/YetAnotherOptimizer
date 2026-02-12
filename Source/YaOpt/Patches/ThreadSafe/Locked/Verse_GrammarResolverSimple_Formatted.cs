using HarmonyLib;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(GrammarResolverSimple))]
	[HarmonyPatch(nameof(GrammarResolverSimple.Formatted))]
	internal static class Verse_GrammarResolverSimple_Formatted
	{
		private static readonly object lockObj = new object();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			Monitor.Enter(lockObj, ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				Monitor.Exit(lockObj);
		}
	}
}