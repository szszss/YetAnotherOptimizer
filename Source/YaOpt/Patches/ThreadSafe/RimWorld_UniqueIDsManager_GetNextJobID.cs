using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch(typeof(UniqueIDsManager))]
	[HarmonyPatch(nameof(UniqueIDsManager.GetNextJobID))]
	internal static class RimWorld_UniqueIDsManager_GetNextJobID
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				// from UniqueIDsManager.GetNextID(ref this.nextJobID)
				// to ConcurrentUniqueIDHelper.GetNextIdThreadSafely(ref this.nextJobID, this.wasLoaded)
				if (instruction.opcode == OpCodes.Call)
				{
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(typeof(UniqueIDsManager), "wasLoaded");
					yield return CodeInstruction.Call(
						typeof(ConcurrentUniqueIDHelper), nameof(ConcurrentUniqueIDHelper.GetNextIdThreadSafely));
					continue;
				}
				yield return instruction;
			}
		}
	}
}