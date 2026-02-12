using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_GenAdjFast
	{
		[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
		private class FakeGenAdjFast
		{
		}

		[ThreadStatic]
		public static bool Working;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(GenAdjFast), 
				nameof(GenAdjFast.AdjacentCells8Way),
				new [] { typeof(IntVec3) });
			yield return AccessTools.Method(typeof(GenAdjFast), 
				nameof(GenAdjFast.AdjacentCells8Way),
				new [] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2) });
			yield return AccessTools.Method(typeof(GenAdjFast), 
				nameof(GenAdjFast.AdjacentCellsCardinal),
				new [] { typeof(IntVec3) });
			yield return AccessTools.Method(typeof(GenAdjFast), 
				nameof(GenAdjFast.AdjacentCellsCardinal),
				new [] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2) });
		}

		static bool Prepare()
		{
			// Don't sure if parallel job fail tests need this.
			// Some WorkGivers need this. (i.e. Fluffy_Breakdowns.WorkGiver_Maintenance)
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			LocalBuilder local = generator.DeclareLocal(typeof(List<IntVec3>));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<FakeGenAdjFast, IntVec3>),
				nameof(ThreadLocalTmpList<FakeGenAdjFast, IntVec3>.Get));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo1)
				{
					if (fieldInfo1.Name == "resultList")
					{
						yield return CodeInstruction.LoadLocal(local.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo1.Name == "working")
					{
						instruction.operand = AccessTools.Field(
							typeof(MultiTargets_GenAdjFast),
							nameof(Working));
					}
				}
				else if (instruction.opcode == OpCodes.Stsfld && instruction.operand is FieldInfo fieldInfo2)
				{
					if (fieldInfo2.Name == "working")
					{
						instruction.operand = AccessTools.Field(
							typeof(MultiTargets_GenAdjFast),
							nameof(Working));
					}
				}
				yield return instruction;
			}
		}

		static void Finalizer()
		{
			Working = false;
		}
	}
}