using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_MapPawnsFactionList
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(MapPawns), nameof(MapPawns.PawnsInFaction));
			yield return AccessTools.Method(typeof(MapPawns), nameof(MapPawns.FreeHumanlikesOfFaction));
			yield return AccessTools.Method(typeof(MapPawns), nameof(MapPawns.FreeHumanlikesSpawnedOfFaction));
			yield return AccessTools.Method(typeof(MapPawns), nameof(MapPawns.SpawnedBabiesInFaction));
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			var labelElse = generator.DefineLabel();
			var labelEnd = generator.DefineLabel();
			/*
			 * List<Pawn> list;
			 * if (!UnityData.IsInMainThread)
			 *   list = ThreadLocalMapPawns.GetPooledList()
			 * else
			 *   list = this.factionDictionary.GetPawnList(faction);
			 */
			yield return CodeInstruction.Call(typeof(UnityData), "get_IsInMainThread");
			yield return new CodeInstruction(OpCodes.Brtrue_S, labelElse);
			yield return new CodeInstruction(OpCodes.Call,
				AccessTools.Method(typeof(ThreadLocalMapPawns), nameof(ThreadLocalMapPawns.GetPooledList)));
			yield return new CodeInstruction(OpCodes.Br_S, labelEnd);
			var firstInst = true;
			var firstStloc0 = true;
			foreach (var instruction in instructions)
			{
				if (firstInst)
				{
					firstInst = false;
					instruction.WithLabels(labelElse);
				}
				else if (firstStloc0 && (instruction.opcode == OpCodes.Stloc || instruction.opcode == OpCodes.Stloc_0))
				{
					firstStloc0 = false;
					instruction.WithLabels(labelEnd);
				}
				yield return instruction;
			}
		}
	}
}