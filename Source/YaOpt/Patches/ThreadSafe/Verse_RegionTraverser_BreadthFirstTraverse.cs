using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch(typeof(RegionTraverser))]
	[HarmonyPatch(nameof(RegionTraverser.BreadthFirstTraverse),
		typeof(Region), typeof(RegionEntryPredicate), typeof(RegionProcessor), typeof(int), typeof(RegionType))]
	internal static class Verse_RegionTraverser_BreadthFirstTraverse
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static bool Prefix(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
		{
			if (YaOptGlobal.IsInMainThread)
				return true;
			ParallelRegionTraverser.BreadthFirstTraverse(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
			return false;
		}

		/*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo &&
				    fieldInfo.Name == "freeWorkers")
				{
					if (instruction.labels?.Count > 0)
					{
						instruction.opcode = OpCodes.Nop;
						instruction.operand = null;
					}
					else
						continue;
				}
				if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo)
				{
					if (methodInfo.Name == "get_Count")
					{
						instruction.operand = AccessTools.Method(
							typeof(ThreadLocalBFSWorkerPool),
							nameof(ThreadLocalBFSWorkerPool.Count));
					}
					if (methodInfo.Name == "Dequeue")
					{
						yield return CodeInstruction.Call(
							typeof(ThreadLocalBFSWorkerPool),
							nameof(ThreadLocalBFSWorkerPool.Dequeue));
						yield return new CodeInstruction(OpCodes.Castclass,
							AccessTools.TypeByName("Verse.RegionTraverser/BFSWorker"));
						continue;
					}

					if (methodInfo.Name == "Enqueue")
					{
						yield return new CodeInstruction(OpCodes.Castclass, typeof(object));
						yield return CodeInstruction.Call(
							typeof(ThreadLocalBFSWorkerPool),
							nameof(ThreadLocalBFSWorkerPool.Enqueue));
						continue;
					}
				}
				yield return instruction;
			}
		}*/
	}
}