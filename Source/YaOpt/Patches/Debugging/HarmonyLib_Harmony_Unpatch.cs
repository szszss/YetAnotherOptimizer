using HarmonyLib;
using System.Reflection;
using YaOpt.Helpers;
using YaOpt.Patches.Early;

namespace YaOpt.Patches.Debugging
{
	[HarmonyPatch(typeof(Harmony))]
	[HarmonyPatch(nameof(Harmony.Unpatch), typeof(MethodBase), typeof(MethodInfo))]
	[EarlyPatch]
	internal static class HarmonyLib_Harmony_Unpatch
	{
		static bool Prepare()
		{
#if DEBUG
			return true;
#endif
			return false;
		}

		static void Prefix(MethodBase original)
		{
			YaOptMod.Debug($"Unpatching {original.FullName()}");
		}
	}
}