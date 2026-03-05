using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(Region))]
	[HarmonyPatch(nameof(Region.DangerFor))]
	internal static class Verse_Region_DangerFor
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			const int LOCAL_RANGE = 2;
			var localIsMainThread = generator.DeclareLocal(typeof(bool));
			var labelIfEnd = generator.DefineLabel();
			var prepareEmitLockCheck = false;
			var skip = false;
			var shouldEmitLabel = false;
			var counter = 0;
			Label targetLabel = default;
			// Step 1: var isMainThread = YaOptGlobal.IsInMainThread;
			yield return CodeInstruction.Call(typeof(YaOptGlobal), "get_IsInMainThread");
			yield return CodeInstruction.StoreLocal(localIsMainThread.LocalIndex);
			foreach (var instruction in instructions)
			{
				// Step 2, 4:
				if (instruction.Calls("get_ProgramState"))
				{
					prepareEmitLockCheck = true;
					counter++;
				}
				// Step 8:
				else if (instruction.Calls("set_Item"))
				{
					// don't write cachedDangers if current thread is not the main thread
					var labelIsMainThreadElse = generator.DefineLabel();
					var labelIsMainThreadEnd = generator.DefineLabel();
					yield return CodeInstruction.LoadLocal(localIsMainThread.LocalIndex);
					yield return new CodeInstruction(OpCodes.Brfalse_S, labelIsMainThreadElse);
					yield return instruction;
					yield return new CodeInstruction(OpCodes.Br_S, labelIsMainThreadEnd);
					yield return new CodeInstruction(OpCodes.Pop).WithLabels(labelIsMainThreadElse);
					yield return new CodeInstruction(OpCodes.Pop);
					yield return new CodeInstruction(OpCodes.Pop);
					yield return new CodeInstruction(OpCodes.Nop).WithLabels(labelIsMainThreadEnd);
					continue;
				}
				// Step 6:
				else if (skip && instruction.labels.Contains(targetLabel))
				{
					skip = false;
					shouldEmitLabel = true;
				}

				// Step 7:
				if (shouldEmitLabel && instruction.opcode == OpCodes.Ldloca_S)
				{
					shouldEmitLabel = false;
					instruction.WithLabels(labelIfEnd);
				}
				if (!skip)
				{
					yield return instruction;
				}

				if (prepareEmitLockCheck && instruction.opcode == OpCodes.Bne_Un_S)
				{
					prepareEmitLockCheck = false;
					if (counter == 1) // cachedDangers block
					{
						// Step 3:
						// don't read cachedDangers if current thread is not the main thread
						yield return CodeInstruction.LoadLocal(localIsMainThread.LocalIndex);
						yield return new CodeInstruction(OpCodes.Brfalse_S, instruction.operand);
					}
					else if (counter == 2) // cachedSafeTemperatureRanges block
					{
						// Step 5:
						// floatRange = TemperatureHelper.GetSafeTemperatureRange(pawn);
						// skip other code
						yield return CodeInstruction.LoadArgument(1);
						yield return CodeInstruction.Call(
							typeof(RegionDangerHelper),
							nameof(RegionDangerHelper.GetSafeTemperatureRange));
						yield return CodeInstruction.StoreLocal(LOCAL_RANGE);
						yield return new CodeInstruction(OpCodes.Br_S, labelIfEnd);
						targetLabel = (Label)instruction.operand;
						skip = true;
					}
				}
			}
		}
	}
}