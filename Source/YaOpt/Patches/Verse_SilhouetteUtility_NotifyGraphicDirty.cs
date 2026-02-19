using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Invalidates silhouette cache when pawn graphics change.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptSilhouette"/>
	[HarmonyPatch(typeof(SilhouetteUtility))]
	[HarmonyPatch(nameof(SilhouetteUtility.NotifyGraphicDirty))]
	internal static class Verse_SilhouetteUtility_NotifyGraphicDirty
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptSilhouette.Enabled;
		}

		static bool Prefix(Thing __0)
		{
			var key = SilhouetteHelper.GetKey(__0);
			SilhouetteHelper.RemoveCache(key);
			return false;
		}
	}
}