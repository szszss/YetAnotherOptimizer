using HarmonyLib;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// Hooks into Game.ClearCaches to clear mod caches on map change or save load.
	/// </summary>
	/// <seealso cref="Helpers.UpdateCallbackHelper"/>
	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.ClearCaches))]
	internal static class Verse_Game_ClearCaches
	{
		static void Postfix()
		{
			YaOptMod.Instance.ClearCaches();
		}
	}
}