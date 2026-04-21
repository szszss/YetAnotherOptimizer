using HarmonyLib;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Dynamic
{
	/// <summary>
	/// Restores downsampled textures to high resolution when a Thing is spawned onto a map.
	/// </summary>
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch("SpawnSetup")]
	internal static class Verse_Thing_SpawnSetup
	{
		static void Postfix(Thing __instance)
		{
			ContentManager.LoadFullResolutionTexture(__instance);
		}
	}
}