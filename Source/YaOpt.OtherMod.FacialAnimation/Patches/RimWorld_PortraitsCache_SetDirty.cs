using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using YaOpt.OtherMod.FacialAnimation.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	/// <summary>
	/// <seealso cref="SubMod.OptFAParallelUpdate"/>
	/// </summary>
	[HarmonyPatch(typeof(PortraitsCache))]
	[HarmonyPatch(nameof(PortraitsCache.SetDirty))]
	internal static class RimWorld_PortraitsCache_SetDirty
	{
		static bool Prepare(MethodBase original)
		{
			return SubMod.OptFAParallelUpdate.Enabled;
		}

		/*
		 * Add ParallelUpdateHelper.AddPendingPawn(pawn)
		 * after dictionary.Add(pawn, ...);
		 */
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;

				if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo method &&
					method.Name == "Add")
				{
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.Call(
						typeof(ParallelUpdateHelper),
						nameof(ParallelUpdateHelper.AddPendingPawn));
				}
			}
		}
	}
}