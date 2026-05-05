using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.Reach
{
	[HarmonyPatch(typeof(Reachability))]
	[HarmonyPatch("CheckCellBasedReachability")]
	internal static class Verse_Reachability_CheckCellBasedReachability
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var localIsMainThread = generator.DeclareLocal(typeof(bool));
			var localLockTaken = generator.DeclareLocal(typeof(bool));
			var localStartingRegions = generator.DeclareLocal(typeof(List<Region>));
			var localDestRegions = generator.DeclareLocal(typeof(List<Region>));
			var localRegionGrid = generator.DeclareLocal(typeof(RegionGrid));
			var labelIfMainThreadElse = generator.DefineLabel();
			var labelIfMainThreadEnd = generator.DefineLabel();
			var findNextBrfalse = false;
			var fixNextBrfalse = false;
			var prepareEmitFinally = false;
			Label jumpTarget = default;
			Label labelBrfalseFix = default;

			// var isMainThread = YaOptGlobal.IsInMainThread;
			yield return CodeInstruction.Call(typeof(YaOptGlobal), "get_IsInMainThread");
			yield return CodeInstruction.StoreLocal(localIsMainThread.LocalIndex);

			// var lockTaken = false;
			yield return new CodeInstruction(OpCodes.Ldc_I4_0);
			yield return CodeInstruction.StoreLocal(localLockTaken.LocalIndex);

			// if (isMainThread) {
			yield return CodeInstruction.LoadLocal(localIsMainThread.LocalIndex);
			yield return new CodeInstruction(OpCodes.Brfalse_S, labelIfMainThreadElse);

			//     var startingRegions = this.startingRegions;
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(Reachability), "startingRegions");
			yield return CodeInstruction.StoreLocal(localStartingRegions.LocalIndex);

			//     var destRegions = this.destRegions;
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(Reachability), "destRegions");
			yield return CodeInstruction.StoreLocal(localDestRegions.LocalIndex);

			//     var regionGrid = this.regionGrid;
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(Reachability), "regionGrid");
			yield return CodeInstruction.StoreLocal(localRegionGrid.LocalIndex);

			// }
			yield return new CodeInstruction(OpCodes.Br_S, labelIfMainThreadEnd);

			// else {
			//     var tlr = ThreadLocalReachability.Get();
			yield return CodeInstruction.Call(
				typeof(ThreadLocalReachability),
				nameof(ThreadLocalReachability.Get)).WithLabels(labelIfMainThreadElse);
			yield return new CodeInstruction(OpCodes.Dup);
			yield return new CodeInstruction(OpCodes.Dup);

			//     var startingRegions = tlr.StartingRegions;
			yield return CodeInstruction.LoadField(typeof(ThreadLocalReachability),
				nameof(ThreadLocalReachability.StartingRegions));
			yield return CodeInstruction.StoreLocal(localStartingRegions.LocalIndex);

			//     var destRegions = tlr.DestRegions;
			yield return CodeInstruction.LoadField(typeof(ThreadLocalReachability),
				nameof(ThreadLocalReachability.DestRegions));
			yield return CodeInstruction.StoreLocal(localDestRegions.LocalIndex);

			//     var regionGrid = tlr.RegionGrid;
			yield return CodeInstruction.LoadField(typeof(ThreadLocalReachability),
				nameof(ThreadLocalReachability.RegionGrid));
			yield return CodeInstruction.StoreLocal(localRegionGrid.LocalIndex);

			// }
			yield return new CodeInstruction(OpCodes.Nop).WithLabels(labelIfMainThreadEnd);

			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("startingRegions"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(localStartingRegions.LocalIndex);
					continue;
				}
				else if (instruction.LoadsField("destRegions"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(localDestRegions.LocalIndex);
					continue;
				}
				else if (instruction.LoadsField("regionGrid"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(localRegionGrid.LocalIndex);
					continue;
				}

				if (instruction.Calls("CanUseCache"))
				{
					findNextBrfalse = true;
				}

				// (Try-Finally block: Stage2 - Fix brfalse) (Optional, only used in foundCell.IsValid block)
				// This is a dirty hack
				// Prevent it from jumping out the try block
				if (fixNextBrfalse && (instruction.opcode == OpCodes.Brfalse ||
									   instruction.opcode == OpCodes.Brfalse_S))
				{
					fixNextBrfalse = false;
					labelBrfalseFix = generator.DefineLabel();
					instruction.operand = labelBrfalseFix;
				}

				// (Try-Finally block: Stage3 - End)
				// } finally {
				//   ThreadLocalReachability.ExitLock(lockTaken);
				// }
				if (prepareEmitFinally && instruction.labels.Contains(jumpTarget))
				{
					prepareEmitFinally = false;

					if (!fixNextBrfalse) // if the brfalse is found and a label is created
					{
						yield return new CodeInstruction(OpCodes.Nop).WithLabels(labelBrfalseFix);
					}
					fixNextBrfalse = false;

					yield return CodeInstruction.LoadLocal(localLockTaken.LocalIndex)
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
					yield return CodeInstruction.Call(
						typeof(ThreadLocalReachability),
						nameof(ThreadLocalReachability.ExitLock));
					yield return new CodeInstruction(OpCodes.Endfinally)
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
				}

				yield return instruction;

				// (Try-Finally block: Stage1 - Begin)
				// ThreadLocalReachability.EnterLock(ref lockTaken);
				// try {
				if (findNextBrfalse && instruction.Branches(out var label))
				{
					if (!label.HasValue)
						throw new Exception("Cannot find the branch target in CheckCellBasedReachability");
					jumpTarget = label.Value;
					findNextBrfalse = false;
					prepareEmitFinally = true;
					fixNextBrfalse = true;
					yield return CodeInstruction.LoadLocal(localLockTaken.LocalIndex, true);
					yield return CodeInstruction.Call(
						typeof(ThreadLocalReachability),
						nameof(ThreadLocalReachability.EnterLock));
					yield return new CodeInstruction(OpCodes.Nop)
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
				}
			}

		}
	}
}
