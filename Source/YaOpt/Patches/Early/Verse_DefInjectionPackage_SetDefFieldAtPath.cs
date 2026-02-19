using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// Replaces O(N²) duplicate check with O(N) hash lookup for translation injection.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastTranslationInjection"/>
	[HarmonyPatch(typeof(DefInjectionPackage))]
	[HarmonyPatch("SetDefFieldAtPath")]
	[EarlyPatch]
	internal static class Verse_DefInjectionPackage_SetDefFieldAtPath
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastTranslationInjection.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			const int ARG_NORMALIZED_PATH = 8;
			const int ARG_SUGGESTED_PATH = 9;
			const int LOCAL_PATH = 0;
			const int LOCAL_PATH_2 = 1;
			const int LOCAL_HAS_ERROR = 12;
			var skip = false;
			var local = generator.DeclareLocal(typeof(DefInjectionPackage.DefInjection));
			Label label = generator.DefineLabel();
			foreach (var instruction in instructions)
			{
				if (!skip)
				{
					yield return instruction;
				}
				if (instruction.opcode == OpCodes.Stloc_S && instruction.LocalIndex() == LOCAL_HAS_ERROR)
				{
					skip = true;
				}
				else if (skip && instruction.opcode == OpCodes.Endfinally)
				{
					skip = false;
					// var duplicate = DefInjectionHelper.CheckDuplicateInjection(normalizedPath, path);
					yield return CodeInstruction.LoadArgument(ARG_NORMALIZED_PATH);
					yield return new CodeInstruction(OpCodes.Ldind_Ref);
					yield return CodeInstruction.LoadLocal(LOCAL_PATH);
					yield return CodeInstruction.Call(
						typeof(DefInjectionHelper),
						nameof(DefInjectionHelper.CheckDuplicateInjection));
					yield return CodeInstruction.StoreLocal(local.LocalIndex);
					// if (duplicate != null) {
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					yield return new CodeInstruction(OpCodes.Brfalse, label);
					//     DefInjectionPackage.loadErrors(duplicate, path2, suggestedPath, this.loadErrors);
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					yield return CodeInstruction.LoadLocal(LOCAL_PATH_2);
					yield return CodeInstruction.LoadArgument(ARG_SUGGESTED_PATH);
					yield return new CodeInstruction(OpCodes.Ldind_Ref);
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(
						typeof(DefInjectionPackage),
						nameof(DefInjectionPackage.loadErrors));
					yield return CodeInstruction.Call(
						typeof(DefInjectionHelper),
						nameof(DefInjectionHelper.PrintError));
					//      hasError = true; }
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return CodeInstruction.StoreLocal(LOCAL_HAS_ERROR);
					yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
				}
			}
		}
	}
}