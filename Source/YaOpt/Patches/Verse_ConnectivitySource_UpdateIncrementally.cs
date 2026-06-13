using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	[HarmonyPatch(typeof(ConnectivitySource))]
	[HarmonyPatch(nameof(ConnectivitySource.UpdateIncrementally))]
	internal static class Verse_ConnectivitySource_UpdateIncrementally
	{
		private static readonly HashSet<IntVec3> _checkedCellsSmall = new HashSet<IntVec3>(256);
		private static readonly HashSet<IntVec3> _checkedCellsBig = new HashSet<IntVec3>(640);

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptConnectivityUpdate.Enabled;
		}

		private static HashSet<IntVec3> GetCheckedCells(List<IntVec3> cellDeltas)
		{
			var list = cellDeltas.Count > 16 ? _checkedCellsBig : _checkedCellsSmall;
			list.Clear();
			return list;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(HashSet<IntVec3>));
			yield return CodeInstruction.LoadArgument(2); // cellDeltas
			yield return CodeInstruction.Call(
				typeof(Verse_ConnectivitySource_UpdateIncrementally),
				nameof(GetCheckedCells));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("checkedCells"))
				{
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					continue;
				}
				else if (instruction.Calls("Clear"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					continue;
				}

				yield return instruction;
			}
		}
	}
}
