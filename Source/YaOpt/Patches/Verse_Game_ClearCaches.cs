using HarmonyLib;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="Helpers.UpdateCallbackHelper"/>
	/// </summary>
	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch("ClearCaches")]
	internal static class Verse_Game_ClearCaches
	{
		static void Postfix()
		{
			YaOptMod.Instance.ClearCaches();
		}
	}
}