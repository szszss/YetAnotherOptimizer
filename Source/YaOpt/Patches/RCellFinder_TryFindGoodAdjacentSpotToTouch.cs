using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Caches IsGoodDestinationFor and CanReach results within TryFindGoodAdjacentSpotToTouch
	/// using a per-thread cache. The cache is invalidated when the toucher or game tick changes.
	/// This eliminates redundant pathfinding calls when the method is called multiple times
	/// for the same pawn in the same tick (e.g. HasJobOnThing then JobOnThing).
	/// </summary>
	/// <seealso cref="YaOptSettings.OptConstructDeliverResources"/>
	[HarmonyPatch(typeof(RCellFinder))]
	[HarmonyPatch(nameof(RCellFinder.TryFindGoodAdjacentSpotToTouch))]
	[HarmonyPriority(Priority.VeryHigh)]
	internal static class RCellFinder_TryFindGoodAdjacentSpotToTouch
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptConstructDeliverResources.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(
			IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(RCellFinderCache));
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.Call(
				typeof(RCellFinderCache),
				nameof(RCellFinderCache.GetCache));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);

			foreach (var instruction in instructions)
			{
				if (instruction.Calls("IsGoodDestinationFor"))
				{
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					instruction.operand = AccessTools.Method(
						typeof(RCellFinderCache),
						nameof(RCellFinderCache.CachedIsGoodDestinationFor));
				}
				else if (instruction.Calls("CanReach"))
				{
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					instruction.operand = AccessTools.Method(
						typeof(RCellFinderCache),
						nameof(RCellFinderCache.CachedCanReach));
				}
				yield return instruction;
			}
		}
	}
}
