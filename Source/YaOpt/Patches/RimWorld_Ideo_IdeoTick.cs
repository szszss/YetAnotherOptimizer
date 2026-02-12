using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptIdeoCheck"/>
	/// </summary>
	[HarmonyPatch(typeof(Ideo))]
	[HarmonyPatch(nameof(Ideo.IdeoTick))]
	internal static class RimWorld_Ideo_IdeoTick
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptIdeoCheck.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<Precept>));
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.Call(
				typeof(IdeoHelper),
				nameof(IdeoHelper.GetPreceptsWithOverridenTick));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("precepts"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					continue;
				}
				yield return instruction;
			}
		}
	}
}