using HarmonyLib;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using YaOpt.Patches.Early;

namespace YaOpt.Patches
{
	/// <summary>
	/// Prevents downsampled textures from mistakenly entering the StaticTextureAtlas.
	/// </summary>
	[EarlyPatch]
	[HarmonyPatch(typeof(GlobalTextureAtlasManager))]
	[HarmonyPatch(nameof(GlobalTextureAtlasManager.TryInsertStatic))]
	internal static class Verse_GlobalTextureAtlasManager_TryInsertStatic
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptLazyTextureLoad.Enabled && YaOptGlobal.Settings.LazyTextureLoadRadical;
		}

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