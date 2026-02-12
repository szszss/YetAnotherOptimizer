using System;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	//[HarmonyPatch(typeof(PreceptComp_UnwillingToDo_Gendered))]
	//[HarmonyPatch(nameof(PreceptComp_UnwillingToDo_Gendered.MemberWillingToDo))]
	[Obsolete]
	//TODO: delete
	internal static class RimWorld_PreceptComp_UnwillingToDo_Gendered_MemberWillingToDo
	{
		/*static bool Prepare()
		{
			return true;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("TryGetArg"))
				{
					instruction.operand = AccessTools.Method(
						typeof(MiscHelper),
						nameof(MiscHelper.SignalArgsTryGetArgFast), null, new[] { typeof(Pawn) });
				}
				yield return instruction;
			}
		}*/
	}
}