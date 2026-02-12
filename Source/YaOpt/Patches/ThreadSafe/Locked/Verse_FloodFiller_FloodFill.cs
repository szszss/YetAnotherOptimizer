using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(FloodFiller))]
	[HarmonyPatch(nameof(FloodFiller.FloodFill), 
		typeof(IntVec3), typeof(Predicate<IntVec3>), 
		typeof(Func<IntVec3, int, bool>), typeof(int), 
		typeof(bool), typeof(IEnumerable<IntVec3>))]
	internal static class Verse_FloodFiller_FloodFill
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(FloodFiller __instance, out bool __state)
		{
			__state = false;
			Monitor.Enter(__instance, ref __state);
		}

		static void Finalizer(FloodFiller __instance, bool __state)
		{
			if (__state)
				Monitor.Exit(__instance);
		}

		/*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var localIsMainThread = generator.DeclareLocal(typeof(bool));
			var localParentGrid = generator.DeclareLocal(typeof(CellGrid));
			var labelIsMainThreadElse = generator.DefineLabel();
			var labelIsMainThreadEnd = generator.DefineLabel();
			var hasSkippedWorkingCheck = false;
			// var isMainThread = UnityData.IsInMainThread;
			yield return CodeInstruction.Call(typeof(UnityData), "get_IsInMainThread");
			yield return CodeInstruction.StoreLocal(localIsMainThread.LocalIndex);
			// if (isMainThread) {
			yield return CodeInstruction.LoadLocal(localIsMainThread.LocalIndex);
			yield return new CodeInstruction(OpCodes.Brfalse_S, labelIsMainThreadElse);
			// var parentGrid = this.parentGrid;
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(FloodFiller), "parentGrid");
			yield return CodeInstruction.StoreLocal(localParentGrid.LocalIndex);
			// } else {
			yield return new CodeInstruction(OpCodes.Br_S, labelIsMainThreadEnd);
			// var parentGrid = ThreadLocalFloodFiller.GetParentGrid(this.map);
			yield return CodeInstruction.LoadArgument(0).WithLabels(labelIsMainThreadElse);
			yield return CodeInstruction.LoadField(typeof(FloodFiller), "map");
			yield return CodeInstruction.Call(
				typeof(ThreadLocalFloodFiller),
				nameof(ThreadLocalFloodFiller.GetParentGrid));
			yield return CodeInstruction.StoreLocal(localParentGrid.LocalIndex);
			yield return new CodeInstruction(OpCodes.Nop).WithLabels(labelIsMainThreadEnd);

			foreach (var instruction in instructions)
			{
				// skip working check
				if (instruction.LoadsField("working"))
				{
					if (!hasSkippedWorkingCheck)
					{
						hasSkippedWorkingCheck = true;
						yield return new CodeInstruction(OpCodes.Pop);
						yield return new CodeInstruction(OpCodes.Ldc_I4_0);
						continue;
					}
				}

				yield return instruction;
			}
		}*/
	}
}