using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Defer the execution of the thing DeRegisterDrawable to the worker thread.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(DynamicDrawManager))]
	[HarmonyPatch(nameof(DynamicDrawManager.DeRegisterDrawable))]
	internal static class Verse_DynamicDrawManager_DeRegisterDrawable
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("Remove"))
				{
					yield return CodeInstruction.Call(
						typeof(DrawableRemovalHelper),
						nameof(DrawableRemovalHelper.DeRegisterDrawable));
					continue;
				}
				yield return instruction;
			}
		}
	}
}