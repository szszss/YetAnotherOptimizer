using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// Replaces SelectNodes with optimized enumerator for multi-node patch operations.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastPatchOperation"/>
	[HarmonyPatch]
	[EarlyPatch]
	internal static class MultiTargets_PatchOperationMulti
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(PatchOperationAdd), "ApplyWorker");
			yield return AccessTools.Method(typeof(PatchOperationAddModExtension), "ApplyWorker");
			yield return AccessTools.Method(typeof(PatchOperationAttributeAdd), "ApplyWorker");
			yield return AccessTools.Method(typeof(PatchOperationAttributeRemove), "ApplyWorker");
			yield return AccessTools.Method(typeof(PatchOperationAttributeSet), "ApplyWorker");
			yield return AccessTools.Method(typeof(PatchOperationInsert), "ApplyWorker");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastPatchOperation.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var removed = false;
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("SelectNodes"))
				{
					yield return CodeInstruction.Call(
						typeof(XPathReducer),
						nameof(XPathReducer.GetXmlEnumerator));
					continue;
				}
				else if (!removed && instruction.Calls("GetEnumerator"))
				{
					removed = true;
					continue;
				}
				yield return instruction;
			}
		}
	}
}