using HarmonyLib;
using System.Xml;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// Creates XPath cache before patch operations for faster lookups.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastPatchOperation"/>
	[HarmonyPatch(typeof(LoadedModManager))]
	[HarmonyPatch(nameof(LoadedModManager.ApplyPatches))]
	[EarlyPatch]
	internal static class Verse_LoadedModManager_ApplyPatches
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastPatchOperation.Enabled;
		}

		static void Prefix(XmlDocument xmlDoc)
		{
			XPathReducer.CreateCache(xmlDoc);
		}

		static void Postfix()
		{
			XPathReducer.ClearCache();
		}
	}
}