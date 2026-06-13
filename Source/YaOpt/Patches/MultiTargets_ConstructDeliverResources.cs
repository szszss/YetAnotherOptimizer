using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// Eliminates redundant CanTouchTargetFromValidCell checks in construction resource delivery.
	/// Vanilla calls CanTouchTargetFromValidCell before FirstBlockingThing in both HasJobOnThing
	/// and JobOnThing of WorkGiver_ConstructDeliverResourcesToBlueprints/Frames.
	/// CanConstruct then calls it again. This optimization defers the check to only when
	/// FirstBlockingThing actually finds something, halving the calls in the common case.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptConstructDeliverResources"/>
	[HarmonyPatch]
	internal static class MultiTargets_ConstructDeliverResources
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(WorkGiver_ConstructDeliverResourcesToBlueprints),
				nameof(WorkGiver_ConstructDeliverResourcesToBlueprints.HasJobOnThing));
			yield return AccessTools.Method(
				typeof(WorkGiver_ConstructDeliverResourcesToBlueprints),
				nameof(WorkGiver_ConstructDeliverResourcesToBlueprints.JobOnThing));
			yield return AccessTools.Method(
				typeof(WorkGiver_ConstructDeliverResourcesToFrames),
				nameof(WorkGiver_ConstructDeliverResourcesToFrames.HasJobOnThing));
			yield return AccessTools.Method(
				typeof(WorkGiver_ConstructDeliverResourcesToFrames),
				nameof(WorkGiver_ConstructDeliverResourcesToFrames.JobOnThing));
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptConstructDeliverResources.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var canTouchMethod = AccessTools.Method(
				typeof(GenConstruct), nameof(GenConstruct.CanTouchTargetFromValidCell));
			var firstBlockingMethod = AccessTools.Method(
				typeof(GenConstruct), nameof(GenConstruct.FirstBlockingThing));

			var dummyCanTouchMethod = AccessTools.Method(
				typeof(MultiTargets_ConstructDeliverResources),
				nameof(DummyCanTouchTargetFromValidCell));
			var firstBlockingOptimizedMethod = AccessTools.Method(
				typeof(MultiTargets_ConstructDeliverResources),
				nameof(FirstBlockingThing_Optimized));

			foreach (var inst in instructions)
			{
				if (inst.opcode == OpCodes.Call && inst.operand is MethodInfo method)
				{
					if (method == canTouchMethod)
					{
						inst.operand = dummyCanTouchMethod;
					}
					else if (method == firstBlockingMethod)
					{
						inst.operand = firstBlockingOptimizedMethod;
					}
				}
				yield return inst;
			}
		}

		/// <summary>
		/// Dummy that always returns true, replacing the redundant early CanTouchTargetFromValidCell call.
		/// The real check still happens inside CanConstruct, or inside FirstBlockingThing_Optimized when a blocker exists.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool DummyCanTouchTargetFromValidCell(Thing constructible, Pawn worker)
		{
			return true;
		}

		/// <summary>
		/// Combines FirstBlockingThing with CanTouchTargetFromValidCell.
		/// Only validates reachability when there is actually a blocking thing,
		/// avoiding the redundant CanTouch check in the common no-blocker case.
		/// </summary>
		public static Thing FirstBlockingThing_Optimized(Thing constructible, Pawn pawnToIgnore)
		{
			Thing blockingThing = GenConstruct.FirstBlockingThing(constructible, pawnToIgnore);
			if (blockingThing != null && !GenConstruct.CanTouchTargetFromValidCell(constructible, pawnToIgnore))
			{
				return null;
			}
			return blockingThing;
		}
	}
}
