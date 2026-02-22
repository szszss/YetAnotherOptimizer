using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace YaOpt.Patches.Compatibility.CombatExtended
{
	/// <summary>
	/// Prevent reservation operations during the constant job predicting
	/// </summary>
	[HarmonyPatch]
	internal static class CombatExtended_CompInventory_TrySwitchToWeapon
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("CombatExtended.CompInventory"),
				"TrySwitchToWeapon");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasMod("CETeam.CombatExtended");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool ShouldStopJob(bool requestToStopJob)
		{
			if (YaOptGlobal.IsParallelRunningInTick)
				return false;
			return requestToStopJob;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;

				if (instruction.opcode == OpCodes.Ldarg_2)
				{
					yield return CodeInstruction.Call(
						typeof(CombatExtended_CompInventory_TrySwitchToWeapon),
						nameof(ShouldStopJob));
				}
			}
		}
	}
}