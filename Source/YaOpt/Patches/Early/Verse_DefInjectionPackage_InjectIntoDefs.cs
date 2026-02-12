using HarmonyLib;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptFastTranslationInjection"/>
	/// </summary>
	[HarmonyPatch(typeof(DefInjectionPackage))]
	[HarmonyPatch(nameof(DefInjectionPackage.InjectIntoDefs))]
	[EarlyPatch]
	internal static class Verse_DefInjectionPackage_InjectIntoDefs
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastTranslationInjection.Enabled;
		}

		static void Prefix(DefInjectionPackage __instance)
		{
			DefInjectionHelper.ChangeMapping(__instance);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;
				if (instruction.StoresField("suggestedPath"))
				{
					yield return CodeInstruction.LoadLocal(1, true);
					yield return CodeInstruction.Call(
						typeof(KeyValuePair<string, DefInjectionPackage.DefInjection>),
						"get_Value");
					yield return CodeInstruction.Call(
						typeof(DefInjectionHelper),
						nameof(DefInjectionHelper.AddInjection));
				}
			}
		}
	}
}