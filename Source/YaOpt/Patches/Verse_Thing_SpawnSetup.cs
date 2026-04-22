using HarmonyLib;
using Verse;
using YaOpt.Helpers;
using YaOpt.Patches.Early;

namespace YaOpt.Patches
{
	/// <summary>
	/// Restores downsampled textures to high resolution when a Thing is spawned onto a map.
	/// </summary>
	[EarlyPatch]
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.SpawnSetup))]
	internal static class Verse_Thing_SpawnSetup
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptLazyTextureLoad.Enabled && YaOptGlobal.Settings.LazyTextureLoadRadical;
		}

		static void Postfix(Thing __instance)
		{
			ContentManager.LoadFullResolutionTexture(__instance);
		}
	}
}