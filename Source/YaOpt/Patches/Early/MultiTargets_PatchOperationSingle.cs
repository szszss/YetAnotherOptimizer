using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// Replaces SelectSingleNode with optimized direct node lookup for conditional/test patches.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastPatchOperation"/>
	[HarmonyPatch]
	[EarlyPatch]
	internal static class MultiTargets_PatchOperationSingle
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(PatchOperationConditional), "ApplyWorker");
			yield return AccessTools.Method(typeof(PatchOperationTest), "ApplyWorker");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastPatchOperation.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("SelectSingleNode"))
				{
					yield return CodeInstruction.Call(
						typeof(XPathReducer),
						nameof(XPathReducer.GetXmlFirstNode));
					continue;
				}
				yield return instruction;
			}
		}
	}
}