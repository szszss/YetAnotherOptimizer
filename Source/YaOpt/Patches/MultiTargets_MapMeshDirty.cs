using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="Helpers.MapMeshUpdateThrottle"/>
	/// <seealso cref="YaOptSettings.OptMapMeshUpdateThrottle"/>
	/// </summary>
	[HarmonyPatch]
	internal static class MultiTargets_MapMeshDirty
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(SandGrid), "CheckVisualOrPathCostChange");
			yield return AccessTools.Method(typeof(SandGrid), "MakeMeshDirty");
			yield return AccessTools.Method(typeof(SnowGrid), "CheckVisualOrPathCostChange");
			yield return AccessTools.Method(typeof(SnowGrid), "MakeMeshDirty");
			//yield return AccessTools.Method(typeof(PollutionGrid), "SetPolluted");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptMapMeshUpdateThrottle.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
		{
			var list = instructions.ToList();
			for (var i = 0; i < list.Count; i++)
			{
				var instruction = list[i];
				if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo &&
					methodInfo.Name == "MapMeshDirty")
				{
					var ldf = list[i - 6];
					if (ldf.opcode != OpCodes.Ldfld || !(ldf.operand is FieldInfo fieldInfo) ||
						fieldInfo.Name != "mapDrawer")
					{
						YaOptMod.Error($"Mismatched IL for method {method.FullName()}. Skipped.");
						continue;
					}
					list[i - 6] = new CodeInstruction(OpCodes.Nop);
					instruction.opcode = OpCodes.Call;
					instruction.operand = AccessTools.Method(
						typeof(MapMeshUpdateThrottle),
						nameof(MapMeshUpdateThrottle.MarkMapDirty));
				}
			}
			return list;
		}
	}
}