using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches.Compatibility.PerformanceFish
{
	[HarmonyPatch]
	internal static class RimWorld_WorkGiver_DoBill_TryFindBestIngredientsHelper
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

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
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled && YaOptGlobal.HasMod("bs.performance");
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
				throw new Exception("Cannot find get_Dirty in PerformanceFish WorkGiver_DoBill patch.");
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

			// 1. Move labels from skipLabelIndex to a new leave instruction
			var leaveInst = new CodeInstruction(OpCodes.Leave);
			var labelFinally = generator.DefineLabel();
			leaveInst.operand = labelFinally;
			leaveInst.labels.AddRange(codes[skipLabelIndex].labels);
			codes[skipLabelIndex].labels.Clear();
			codes[skipLabelIndex].labels.Add(labelFinally);

			// 2. Create finally block instructions
			var finallyStart = new CodeInstruction(OpCodes.Ldsflda, AccessTools.Field(
				typeof(RimWorld_WorkGiver_DoBill_TryFindBestIngredientsHelper),
				nameof(_spinLock)));
			finallyStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));

			var finallyCall = new CodeInstruction(OpCodes.Call, AccessTools.Method(
				typeof(GreedySpinLock),
				nameof(GreedySpinLock.Exit)));

			var endFinally = new CodeInstruction(OpCodes.Endfinally);
			endFinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));

			// Insert leave and finally instructions at skipLabelIndex
			codes.Insert(skipLabelIndex, leaveInst);
			codes.Insert(skipLabelIndex + 1, finallyStart);
			codes.Insert(skipLabelIndex + 2, finallyCall);
			codes.Insert(skipLabelIndex + 3, endFinally);

			// 3. Create try block start and enter lock
			codes[insertTarget].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));

			var enterLock1 = new CodeInstruction(OpCodes.Ldsflda, AccessTools.Field(
				typeof(RimWorld_WorkGiver_DoBill_TryFindBestIngredientsHelper),
				nameof(_spinLock)));
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