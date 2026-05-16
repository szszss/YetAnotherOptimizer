using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using static YaOpt.Patches.Compatibility.VehicleMapFramework.VehicleMapFrameworkCompatibility;

namespace YaOpt.Patches.Compatibility.VehicleMapFramework
{
	[HarmonyPatch]
	internal static class MultiTargets_ResolveStubs
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(ParallelRegionTraverser.ParallelBFSWorker),
				nameof(ParallelRegionTraverser.ParallelBFSWorker.VMFPrefix));
			yield return AccessTools.Method(
				typeof(ParallelRegionTraverser.ParallelBFSWorker),
				nameof(ParallelRegionTraverser.ParallelBFSWorker.VMFPostFix));
		}

		static bool Prepare()
		{
			var shouldDo = YaOptGlobal.NeedThreadSafe &&
						   YaOptGlobal.HasMod("oels.vehiclemapframework");
			ParallelRegionTraverser.HasVehicleMapFramework = shouldDo;
			return shouldDo;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase method)
		{
			var typeVehiclePawnWithMapStub = typeof(VehiclePawnWithMapStub);
			var typeCompZiplineStub = typeof(CompZiplineStub);
			var typeVehiclePawnWithMap = AccessTools.TypeByName("VehicleMapFramework.VehiclePawnWithMap");
			var typeCompZipline = AccessTools.TypeByName("VehicleMapFramework.CompZipline");
			var localVehiclePawnWithMapStub = -1;
			var localCompZiplineStub = -1;
			LocalBuilder localVehiclePawnWithMap = default;
			LocalBuilder localCompZipline = default;
			foreach (var localVariableInfo in method.GetMethodBody().LocalVariables)
			{
				if (localVariableInfo.LocalType == typeof(VehiclePawnWithMapStub))
				{
					localVehiclePawnWithMapStub = localVariableInfo.LocalIndex;
					localVehiclePawnWithMap = generator.DeclareLocal(typeVehiclePawnWithMap);
				}
				else if (localVariableInfo.LocalType == typeof(CompZiplineStub))
				{
					localCompZiplineStub = localVariableInfo.LocalIndex;
					localCompZipline = generator.DeclareLocal(typeCompZipline);
				}
			}

			foreach (var instruction in instructions)
			{
				// Fix load local
				if (instruction.IsLdloc())
				{
					if (instruction.LocalIndex() == localVehiclePawnWithMapStub)
					{
						var inst = instruction.opcode == OpCodes.Ldloca_S
							? new CodeInstruction(OpCodes.Ldloca_S, localVehiclePawnWithMap.LocalIndex)
							: CodeInstruction.LoadLocal(localVehiclePawnWithMap.LocalIndex);
						instruction.opcode = inst.opcode;
						instruction.operand = inst.operand;
					}
					else if (instruction.LocalIndex() == localCompZiplineStub)
					{
						var inst = instruction.opcode == OpCodes.Ldloca_S
							? new CodeInstruction(OpCodes.Ldloca_S, localCompZipline.LocalIndex)
							: CodeInstruction.LoadLocal(localCompZipline.LocalIndex);
						instruction.opcode = inst.opcode;
						instruction.operand = inst.operand;
					}
				}
				// Fix store local
				else if (instruction.IsStloc())
				{
					if (instruction.LocalIndex() == localVehiclePawnWithMapStub)
					{
						var inst = CodeInstruction.StoreLocal(localVehiclePawnWithMap.LocalIndex);
						instruction.opcode = inst.opcode;
						instruction.operand = inst.operand;
					}
					else if (instruction.LocalIndex() == localCompZiplineStub)
					{
						var inst = CodeInstruction.StoreLocal(localCompZipline.LocalIndex);
						instruction.opcode = inst.opcode;
						instruction.operand = inst.operand;
					}
				}
				else if (typeVehiclePawnWithMapStub.Equals(instruction.operand))
				{
					instruction.operand = typeVehiclePawnWithMap;
				}
				else if (typeCompZiplineStub.Equals(instruction.operand))
				{
					instruction.operand = typeCompZipline;
				}
				else if (instruction.Calls("get_VehicleMap"))
				{
					instruction.operand = AccessTools.PropertyGetter(
						typeVehiclePawnWithMap, "VehicleMap");
				}
				else if (instruction.Calls("get_Pair"))
				{
					instruction.operand = AccessTools.PropertyGetter(
						typeCompZipline, "Pair");
				}
				else if (instruction.Calls("get_ZiplineDefsStub"))
				{
					instruction.operand = AccessTools.PropertyGetter(
						AccessTools.TypeByName("VehicleMapFramework.RegionTraverserAcrossMaps"),
						"ZiplineDefs");
				}
				else if (instruction.Calls("IsVehicleMapOfStub"))
				{
					instruction.operand = AccessTools.Method(
						AccessTools.TypeByName("VehicleMapFramework.VehicleMapUtility"),
						"IsVehicleMapOf");
				}
				else if (instruction.Calls("TryGetComp") && instruction.operand is MethodInfo methodInfo &&
						 methodInfo.IsGenericMethod && methodInfo.GetGenericArguments()[0] == typeCompZiplineStub)
				{
					instruction.operand = AccessTools.FirstMethod(
						typeof(ThingCompUtility), info =>
						{
							var param = info.GetParameters();
							return param.Length == 2 && param[0].ParameterType == typeof(Thing);
						}).MakeGenericMethod(typeCompZipline);
				}

				yield return instruction;
			}
		}
	}
}