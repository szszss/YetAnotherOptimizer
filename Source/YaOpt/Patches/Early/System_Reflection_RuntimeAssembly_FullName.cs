using HarmonyLib;
using System.Reflection;
using YaOpt.Helpers;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// Caches assembly full name lookups to avoid repeated reflection calls.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptRuntimeInfoCache"/>
	[HarmonyPatch("System.Reflection.RuntimeAssembly", "FullName", MethodType.Getter)]
	[EarlyPatch]
	internal static class System_Reflection_RuntimeAssembly_FullName
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptRuntimeInfoCache.Enabled;
		}

		static bool Prefix(Assembly __instance, ref string __result)
		{
			__result = RuntimeInfoCache.GetCachedAssemblyName(__instance);
			return __result == null;
		}

		static void Postfix(Assembly __instance, ref string __result, bool __runOriginal)
		{
			if (__runOriginal)
			{
				RuntimeInfoCache.SetCachedAssemblyName(__instance, __result);
			}
		}
	}
}