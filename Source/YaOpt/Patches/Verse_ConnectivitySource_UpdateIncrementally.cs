using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptFastCacheClear"/>
	/// </summary>
	[HarmonyPatch(typeof(ConnectivitySource))]
	[HarmonyPatch(nameof(ConnectivitySource.UpdateIncrementally))]
	internal static class Verse_ConnectivitySource_UpdateIncrementally
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastCacheClear.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(SimplePathFinderDataSource<CellConnection>), "map");
			yield return CodeInstruction.Call(
				typeof(ConnectivityCellCache),
				nameof(ConnectivityCellCache.SetupCurrentSet));
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo &&
				    fieldInfo.Name == "checkedCells")
				{
					instruction.operand = AccessTools.Field(
						typeof(ConnectivityCellCache),
						nameof(ConnectivityCellCache.CurrentSet));
				}
				else if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo &&
				         methodInfo.Name == "Clear")
				{
					instruction.opcode = OpCodes.Pop;
					instruction.operand = null;
				}

				yield return instruction;
			}
		}
	}
}