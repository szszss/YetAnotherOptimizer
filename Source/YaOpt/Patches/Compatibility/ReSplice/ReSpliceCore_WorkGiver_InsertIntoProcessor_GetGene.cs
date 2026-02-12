using HarmonyLib;
using System.Reflection;
using System.Threading;

namespace YaOpt.Patches.Compatibility.ReSplice
{
	[HarmonyPatch]
	internal static class ReSpliceCore_WorkGiver_InsertIntoProcessor_GetGene
	{
		private static readonly object lockObj = new object();

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("ReSpliceCore.WorkGiver_InsertIntoProcessor"),
				"GetGene");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled && YaOptGlobal.HasMod("ReSplice.XOTR.Core");
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