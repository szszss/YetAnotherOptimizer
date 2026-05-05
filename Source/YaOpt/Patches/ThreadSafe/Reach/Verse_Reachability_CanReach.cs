using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.Reach
{
	[HarmonyPatch(typeof(Reachability))]
	[HarmonyPatch(nameof(Reachability.CanReach),
		typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms))]
	internal static class Verse_Reachability_CanReach
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			const int ARG_TRAVERSE_PARAMS = 4;
			var localIsMainThread = generator.DeclareLocal(typeof(bool));
			var localOpenQueue = generator.DeclareLocal(typeof(Queue<Region>));
			var localStartingRegions = generator.DeclareLocal(typeof(List<Region>));
			var localDestRegions = generator.DeclareLocal(typeof(List<Region>));
			var labelIfMainThreadElse = generator.DefineLabel();
			var labelIfMainThreadEnd = generator.DefineLabel();
			var hasSkippedWorkingCheck = false;
			var foundTryBegin = false;
			var foundDestRegionsClear = false;
			var beginFieldReplace = false;
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
				else if (!foundTryBegin && instruction.HasBlock(ExceptionBlockType.BeginExceptionBlock))
				{
					foundTryBegin = true;
					instruction.blocks.Clear();
					// var isMainThread = YaOptGlobal.IsInMainThread;
					yield return CodeInstruction.Call(typeof(YaOptGlobal), "get_IsInMainThread")
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
					yield return CodeInstruction.StoreLocal(localIsMainThread.LocalIndex);
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
				}
				else if (beginFieldReplace)
				{
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
				}

				yield return instruction;

				if (foundTryBegin && !foundDestRegionsClear && instruction.Calls("Clear"))
				{
					foundDestRegionsClear = true;
					beginFieldReplace = true;
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
					//     tlr.Setup(this.map, traverseParams);
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(typeof(Reachability), "map");
					yield return CodeInstruction.LoadArgument(ARG_TRAVERSE_PARAMS, true);
					yield return CodeInstruction.Call(
						typeof(ThreadLocalReachability),
						nameof(ThreadLocalReachability.Setup));
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
					// }
					yield return new CodeInstruction(OpCodes.Nop).WithLabels(labelIfMainThreadEnd);
				}
			}
		}
	}
}