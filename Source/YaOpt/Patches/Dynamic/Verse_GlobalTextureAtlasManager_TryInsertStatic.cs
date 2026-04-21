using HarmonyLib;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Dynamic
{
	/// <summary>
	/// Prevents downsampled textures from mistakenly entering the StaticTextureAtlas.
	/// </summary>
	[HarmonyPatch(typeof(GlobalTextureAtlasManager))]
	[HarmonyPatch("TryInsertStatic")]
	internal static class Verse_GlobalTextureAtlasManager_TryInsertStatic
	{
		static bool Prefix(Texture2D texture, ref bool __result)
		{
			if (!ContentManager.MayInsertIntoAtlas(texture))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}