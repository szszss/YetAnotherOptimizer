using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.Reach
{
	[HarmonyPatch(typeof(Reachability))]
	[HarmonyPatch("GetCachedResult")]
	internal static class Verse_Reachability_GetCachedResult
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			ThreadLocalReachability.EnterLock(ref __state);
		}

		static void Finalizer(bool __state)
		{
			ThreadLocalReachability.ExitLock(__state);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var localIsMainThread = generator.DeclareLocal(typeof(bool));
			var localStartingRegions = generator.DeclareLocal(typeof(List<Region>));
			var localDestRegions = generator.DeclareLocal(typeof(List<Region>));
			var labelIfMainThreadElse = generator.DefineLabel();
			var labelIfMainThreadEnd = generator.DefineLabel();
			// var isMainThread = UnityData.IsInMainThread;
			yield return CodeInstruction.Call(typeof(UnityData), "get_IsInMainThread");
			yield return CodeInstruction.StoreLocal(localIsMainThread.LocalIndex);
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
			// }
			yield return new CodeInstruction(OpCodes.Br_S, labelIfMainThreadEnd);
			// else {
			//     var tlr = ThreadLocalReachability.Get();
			yield return CodeInstruction.Call(
				typeof(ThreadLocalReachability),
				nameof(ThreadLocalReachability.Get)).WithLabels(labelIfMainThreadElse);
			yield return new CodeInstruction(OpCodes.Dup);
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
				yield return instruction;
			}
		}
	}
}