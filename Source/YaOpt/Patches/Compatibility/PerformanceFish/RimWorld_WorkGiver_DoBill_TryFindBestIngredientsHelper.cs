using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches.Compatibility.PerformanceFish
{
	[HarmonyPatch]
	internal static class RimWorld_WorkGiver_DoBill_TryFindBestIngredientsHelper
	{
		static MethodBase TargetMethod()
		{
			foreach (var nestedType in typeof(WorkGiver_DoBill).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				var method = AccessTools.FirstMethod(nestedType, methodInfo =>
				{
					var param = methodInfo.GetParameters();
					return param?.Length == 1 && param[0].ParameterType == typeof(Region) &&
						   (methodInfo.Name.Contains("<TryFindBestIngredientsHelper>b__4"));
				});
				if (method != null)
				{
					return method;
				}
			}
			throw new MissingMethodException(nameof(WorkGiver_DoBill), "<TryFindBestIngredientsHelper>b__4");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelWorkGiver.Enabled && YaOptGlobal.HasMod("bs.performance");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var codes = new List<CodeInstruction>(instructions);

			// Find the call to get_Dirty inside the cache check logic
			var dirtyCallIndex = codes.FindIndex(i =>
				i.opcode == OpCodes.Call && i.operand is MethodInfo method &&
				method.Name == "get_Dirty" && method.DeclaringType != null &&
				method.DeclaringType.Name.Contains("RecipeIngredientCacheValue"));
			if (dirtyCallIndex == -1)
			{
				// The user doesn't enable TryFindBestIngredientsHelpers_InnerDelegate in Performance Fish
				return codes;
			}

			// The instruction immediately following get_Dirty should be a branch (brfalse) to skip the update block
			var branchToSkip = codes[dirtyCallIndex + 1];
			if (!(branchToSkip.operand is Label skipLabel))
			{
				throw new Exception("Cannot find the target of branchToSkip in PerformanceFish WorkGiver_DoBill patch.");
			}

			// Scan backwards to find the GetOrAddReference call
			var getOrAddIndex = -1;
			for (var i = dirtyCallIndex - 1; i >= 0; i--)
			{
				if (codes[i].opcode == OpCodes.Call
					&& codes[i].operand is MethodInfo method
					&& method.Name == "GetOrAddReference")
				{
					getOrAddIndex = i;
					break;
				}
			}

			if (getOrAddIndex == -1)
			{
				throw new Exception("Cannot find the GetOrAddReference before get_Dirty.");
			}

			// The evaluation stack for GetOrAddReference starts 2 instructions before it (ldloc for bill, then call GetLoadID)
			var insertTarget = getOrAddIndex - 2;

			// Find the instruction that serves as the end of the update block (where the skip branch jumps to)
			var skipLabelIndex = codes.FindIndex(i => i.labels.Contains(skipLabel));
			if (skipLabelIndex == -1)
			{
				throw new Exception("Cannot find the target label for cache update skip.");
			}

			// Find the GetList call after the cache check. GetList reads cache.thingDefs,
			// which UpdateCache mutates in-place (Clear/Add on inner Defs lists). Reading
			// it outside the lock races with a concurrent UpdateCache on the same Bill.
			// Extend the try-finally to cover GetList so read and write are atomic.
			var getListCallIndex = -1;
			for (var i = skipLabelIndex + 1; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Call
					&& codes[i].operand is MethodInfo method
					&& method.Name == "GetList")
				{
					getListCallIndex = i;
					break;
				}
			}
			if (getListCallIndex == -1)
			{
				throw new Exception("Cannot find the GetList call after the cache check.");
			}

			// The stloc right after GetList stores its return value. The leave must go
			// after that stloc, otherwise Leave would discard the GetList result on stack.
			var getListStoreIndex = getListCallIndex + 1;
			if (getListStoreIndex >= codes.Count
				|| !codes[getListStoreIndex].opcode.Name.StartsWith("stloc"))
			{
				throw new Exception("Cannot find the stloc following the GetList call.");
			}

			// The instruction following the stloc is where execution continues after the
			// try-finally (ActualLoop etc.). This is the new leave target.
			var afterStoreIndex = getListStoreIndex + 1;

			// 1. Create a new leave instruction after GetList's stloc.
			// Label1 and Label8 (used by br jumps from outside the try region) must
			// remain on the after-store instruction so they land after the finally block.
			// Add labelFinally as an extra label on the same instruction for the leave target.
			var leaveInst = new CodeInstruction(OpCodes.Leave);
			var labelFinally = generator.DefineLabel();
			leaveInst.operand = labelFinally;
			codes[afterStoreIndex].labels.Add(labelFinally);

			// 2. Create finally block instructions
			var finallyStart = new CodeInstruction(OpCodes.Ldsflda, AccessTools.Field(
				typeof(MultiTargets_RecipeIngredientCache),
				nameof(SpinLock)));
			finallyStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));

			var finallyCall = new CodeInstruction(OpCodes.Call, AccessTools.Method(
				typeof(GreedySpinLock),
				nameof(GreedySpinLock.Exit)));

			var endFinally = new CodeInstruction(OpCodes.Endfinally);
			endFinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));

			// Insert leave and finally instructions right after the GetList stloc.
			// skipLabelIndex keeps its original labels so brfalse still lands there
			// and falls through into the GetList call, now inside the try block.
			codes.Insert(afterStoreIndex, leaveInst);
			codes.Insert(afterStoreIndex + 1, finallyStart);
			codes.Insert(afterStoreIndex + 2, finallyCall);
			codes.Insert(afterStoreIndex + 3, endFinally);

			// 3. Create try block start and enter lock
			codes[insertTarget].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));

			var enterLock1 = new CodeInstruction(OpCodes.Ldsflda, AccessTools.Field(
				typeof(MultiTargets_RecipeIngredientCache),
				nameof(SpinLock)));
			var enterLock2 = new CodeInstruction(OpCodes.Call, AccessTools.Method(
				typeof(GreedySpinLock),
				nameof(GreedySpinLock.Enter), Type.EmptyTypes));

			// Preserve labels on the original start instruction by moving them to the lock enter
			enterLock1.labels.AddRange(codes[insertTarget].labels);
			codes[insertTarget].labels.Clear();

			codes.Insert(insertTarget, enterLock1);
			codes.Insert(insertTarget + 1, enterLock2);

			return codes;
		}
	}
}