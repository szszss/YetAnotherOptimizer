using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Builds compsByType lookup during initialization for fast GetComp lookups.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptThingGetComp"/>
	[HarmonyPatch(typeof(ThingWithComps))]
	[HarmonyPatch(nameof(ThingWithComps.InitializeComps))]
	internal static class Verse_ThingWithComps_InitializeComps
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptThingGetComp.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var label = generator.DefineLabel();
			var skip = false;
			var bltFound = false;
			foreach (var instruction in instructions)
			{
				if (instruction.StoresField("compsByType"))
				{
					skip = false;
					yield return CodeInstruction.LoadArgument(0);
					yield return new CodeInstruction(OpCodes.Dup);
					yield return new CodeInstruction(OpCodes.Dup);
					yield return CodeInstruction.LoadField(typeof(ThingWithComps), "comps");
					yield return CodeInstruction.Call(typeof(GetCompHelper), nameof(GetCompHelper.CreateCompsByType));
				}

				if (!skip)
					yield return instruction;

				if (!bltFound && (instruction.opcode == OpCodes.Blt || instruction.opcode == OpCodes.Blt_S))
				{
					bltFound = true;
					skip = true;
				}
			}
		}
	}
}