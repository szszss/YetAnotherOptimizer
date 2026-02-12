using HarmonyLib;
using System.Runtime.CompilerServices;
using Verse;

namespace YaOpt.Patches.Compatibility.WhileYouAreUp
{
	internal static class WhileYouAreUpAccess
	{
		public static readonly object GlobalLock = new object();

		private delegate void ClearTempDetourDelegate(Pawn pawn);

		private static readonly ClearTempDetourDelegate clearTempDetourDelegate =
			AccessTools.MethodDelegate<ClearTempDetourDelegate>(AccessTools.Method(
				AccessTools.TypeByName("WhileYoureUp.Mod/WorkGiver_Scanner__HasJobOnThing_Patch"),
				"ClearTempDetour"), null, false, null);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClearTempDetour(Pawn pawn)
		{
			clearTempDetourDelegate(pawn);
		}
	}
}