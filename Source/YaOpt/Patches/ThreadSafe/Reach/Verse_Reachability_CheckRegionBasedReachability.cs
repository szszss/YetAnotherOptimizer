using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.Reach
{
	[HarmonyPatch(typeof(Reachability))]
	[HarmonyPatch("CheckRegionBasedReachability")]
	internal static class Verse_Reachability_CheckRegionBasedReachability
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			const int LOCAL_TESTING_REGION = 4;
			var localIsMainThread = generator.DeclareLocal(typeof(bool));
			var localLockTaken = generator.DeclareLocal(typeof(bool));
			var localResult = generator.DeclareLocal(typeof(int));
			var localOpenQueue = generator.DeclareLocal(typeof(Queue<Region>));
			var localStartingRegions = generator.DeclareLocal(typeof(List<Region>));
			var localDestRegions = generator.DeclareLocal(typeof(List<Region>));
			var localRegionGrid = generator.DeclareLocal(typeof(RegionGrid));
			var localReachedRegions = generator.DeclareLocal(typeof(HashSet<int>));
			var labelReturn = generator.DefineLabel();
			var labelIfMainThreadElse = generator.DefineLabel();
			var labelIfMainThreadEnd = generator.DefineLabel();
			var list = instructions.ToList();
			var firstTryFinallyBlock = false;
			// var isMainThread = UnityData.IsInMainThread;
			yield return CodeInstruction.Call(typeof(UnityData), "get_IsInMainThread");
			yield return CodeInstruction.StoreLocal(localIsMainThread.LocalIndex);
			// var lockTaken = false;
			yield return new CodeInstruction(OpCodes.Ldc_I4_0);
			yield return CodeInstruction.StoreLocal(localLockTaken.LocalIndex);
			// var result = false;
			yield return new CodeInstruction(OpCodes.Ldc_I4_0);
			yield return CodeInstruction.StoreLocal(localResult.LocalIndex);
			// if (isMainThread) {
			yield return CodeInstruction.LoadLocal(localIsMainThread.LocalIndex);
			yield return new CodeInstruction(OpCodes.Brfalse_S, labelIfMainThreadElse);
			//     var openQueue = this.openQueue;
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(Reachability), "openQueue");
			yield return CodeInstruction.StoreLocal(localOpenQueue.LocalIndex);
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
			//     var reachedRegions = null;
			yield return new CodeInstruction(OpCodes.Ldnull);
			yield return CodeInstruction.StoreLocal(localReachedRegions.LocalIndex);
			// }
			yield return new CodeInstruction(OpCodes.Br_S, labelIfMainThreadEnd);
			// else {
			//     var tlr = ThreadLocalReachability.Get();
			yield return CodeInstruction.Call(
				typeof(ThreadLocalReachability),
				nameof(ThreadLocalReachability.Get)).WithLabels(labelIfMainThreadElse);
			yield return new CodeInstruction(OpCodes.Dup);
			yield return new CodeInstruction(OpCodes.Dup);
			yield return new CodeInstruction(OpCodes.Dup);
			yield return new CodeInstruction(OpCodes.Dup);
			//     var openQueue = tlr.OpenQueue;
			yield return CodeInstruction.LoadField(typeof(ThreadLocalReachability),
				nameof(ThreadLocalReachability.OpenQueue));
			yield return CodeInstruction.StoreLocal(localOpenQueue.LocalIndex);
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
			//     var reachedRegions = tlr.ReachedRegions;
			yield return CodeInstruction.LoadField(typeof(ThreadLocalReachability),
				nameof(ThreadLocalReachability.ReachedRegions));
			yield return CodeInstruction.StoreLocal(localReachedRegions.LocalIndex);
			// }
			yield return new CodeInstruction(OpCodes.Nop).WithLabels(labelIfMainThreadEnd);
			for (var i = 0; i < list.Count; i++)
			{
				var instruction = list[i];
				if (instruction.LoadsField("openQueue"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(localOpenQueue.LocalIndex);
					continue;
				}
				else if (instruction.LoadsField("startingRegions"))
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
				else if ((instruction.opcode == OpCodes.Beq || instruction.opcode == OpCodes.Beq_S) &&
						 list[i - 1].LoadsField("reachedIndex"))
				{
					yield return CodeInstruction.LoadLocal(localIsMainThread.LocalIndex);
					yield return CodeInstruction.LoadLocal(localReachedRegions.LocalIndex);
					yield return CodeInstruction.LoadLocal(LOCAL_TESTING_REGION);
					yield return CodeInstruction.Call(
						typeof(ThreadLocalReachability),
						nameof(ThreadLocalReachability.IsRegionAlreadyReached));
					instruction.opcode = OpCodes.Brtrue;
				}
				else if (instruction.opcode == OpCodes.Ret)
				{
					//     (return true)
					// }
					// finally
					// {
					//     ThreadLocalReachability.ExitLock(lockTaken);
					// }
					if (firstTryFinallyBlock)
					{
						firstTryFinallyBlock = false;
						yield return CodeInstruction.StoreLocal(localResult.LocalIndex);
						yield return CodeInstruction.LoadLocal(localLockTaken.LocalIndex)
							.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
						yield return CodeInstruction.Call(
							typeof(ThreadLocalReachability),
							nameof(ThreadLocalReachability.ExitLock));
						yield return new CodeInstruction(OpCodes.Leave, labelReturn);
						yield return new CodeInstruction(OpCodes.Endfinally)
							.WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
						continue;
					}
					// }
					// finally
					// {
					//     ThreadLocalReachability.ExitLock(lockTaken);
					// }
					// return result;
					else
					{
						yield return new CodeInstruction(OpCodes.Pop);
						yield return CodeInstruction.LoadLocal(localLockTaken.LocalIndex)
							.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
						yield return CodeInstruction.Call(
							typeof(ThreadLocalReachability),
							nameof(ThreadLocalReachability.ExitLock));
						yield return new CodeInstruction(OpCodes.Endfinally)
							.WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
						yield return CodeInstruction.LoadLocal(localResult.LocalIndex)
							.WithLabels(labelReturn);
					}
				}

				yield return instruction;

				/* (if (this.destRegions.Contains(region2)) {)
				 * ThreadLocalReachability.EnterLock(ref lockTaken);
				 * try {
				 */
				if ((instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S) &&
					list[i - 1].Calls("Contains"))
				{
					firstTryFinallyBlock = true;
					yield return CodeInstruction.LoadLocal(localLockTaken.LocalIndex, true);
					yield return CodeInstruction.Call(
						typeof(ThreadLocalReachability),
						nameof(ThreadLocalReachability.EnterLock));
					yield return new CodeInstruction(OpCodes.Nop)
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
				}
				/*
				 * ThreadLocalReachability.EnterLock(ref lockTaken);
				 * try {
				 *     (for (int l = 0; l < this.startingRegions.Count; l++))
				 */
				else if ((instruction.opcode == OpCodes.Bgt || instruction.opcode == OpCodes.Bgt_S) &&
						 list[i - 2].Calls("get_Count"))
				{
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