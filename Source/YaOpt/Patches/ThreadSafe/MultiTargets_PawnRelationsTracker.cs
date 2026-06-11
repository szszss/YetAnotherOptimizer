using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch]
	internal static class MultiTargets_PawnRelationsTracker
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var nested in typeof(Pawn_RelationsTracker).GetNestedTypes(BindingFlags.NonPublic))
			{
				if (!nested.Name.Contains("<get_FamilyByBlood>") &&
					!nested.Name.Contains("<get_RelatedPawns>"))
				{
					continue;
				}
				if (nested.GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance) == null)
				{
					continue;
				}
				foreach (var method in nested.GetMethods(
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
				{
					if (method.Name == "MoveNext")
						yield return method;
				}
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var getIsInMainThread = AccessTools.PropertyGetter(
				typeof(YaOptGlobal), nameof(YaOptGlobal.IsInMainThread));

			var codes = instructions.ToList();
			for (var i = 0; i < codes.Count - 2; i++)
			{
				if (codes[i].opcode != OpCodes.Ldarg_0)
				{
					continue;
				}
				if (!codes[i + 1].LoadsField("canCacheFamilyByBlood") &&
					!codes[i + 1].LoadsField("familyByBloodIsCached"))
				{
					continue;
				}
				if (codes[i + 2].opcode != OpCodes.Brtrue && codes[i + 2].opcode != OpCodes.Brtrue_S)
				{
					continue;
				}

				codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, getIsInMainThread));
				codes.Insert(i + 3, new CodeInstruction(OpCodes.And));
				break;
			}
			return codes;
		}
	}
}
