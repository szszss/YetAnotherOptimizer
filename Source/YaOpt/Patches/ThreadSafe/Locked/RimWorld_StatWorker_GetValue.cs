using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers.ThirdParty;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(StatWorker))]
	[HarmonyPatch(nameof(StatWorker.GetValue), typeof(Thing), typeof(bool), typeof(int))]
	internal static class RimWorld_StatWorker_GetValue
	{
		private const int PARTITION_COUNT = 8;

		internal static readonly UnfairRwLock[] StatLocks = new UnfairRwLock[PARTITION_COUNT];

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetLockPartition(StatWorker worker)
		{
			return Math.Abs(worker.GetHashCode()) % PARTITION_COUNT;
		}

		/*static void Postfix(StatWorker __instance, ref float __result)
		{
			YaOptMod.Warning($"{__instance} return {__result} from thread {Thread.CurrentThread.ManagedThreadId}");
		}*/

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// The leave instruction empties the evaluation stack, so we have to save the result to a local var
			const int LOCAL_RESULT = 0;
			var codeMatcher = new CodeMatcher(instructions, generator);
			codeMatcher.DeclareLocal(typeof(int), out var localLock);
			/*
			 * (StatCacheEntry statCacheEntry)
			 * var lockIndex = RimWorld_StatWorker_GetValue.GetLock(this);
			 * StatLocks[lockIndex].EnterReadLock();
			 * try {
			 *     (if (cacheStaleAfterTicks != -1 ...))
			 */
			codeMatcher.MatchStartForward(
				CodeMatch.LoadsArgument(),
				CodeMatch.LoadsConstant(-1),
				CodeMatch.Branches())
				.ThrowIfInvalid("CodeMatcher cannot find 'cacheStaleAfterTicks != -1'")
				.InsertAndAdvance(
					// var lockIndex = RimWorld_StatWorker_GetValue.GetLock(this);
					CodeInstruction.LoadArgument(0),
					CodeInstruction.Call(typeof(RimWorld_StatWorker_GetValue), nameof(GetLockPartition)),
					CodeInstruction.StoreLocal(localLock.LocalIndex),
					// StatLocks[lockIndex].Enter(ref localHasTaken)
					CodeInstruction.LoadField(typeof(RimWorld_StatWorker_GetValue), nameof(StatLocks)),
					CodeInstruction.LoadLocal(localLock.LocalIndex),
					new CodeInstruction(OpCodes.Ldelema, typeof(UnfairRwLock)),
					CodeInstruction.Call(typeof(UnfairRwLock), nameof(UnfairRwLock.EnterReadLock)),
					new CodeInstruction(OpCodes.Nop).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock))
					)
			/*
			 *     (return statCacheEntry.statValue;)
			 * } finally {
			 *     StatLocks[lockIndex].ExitReadLock();
			 * }
			 *
			 * Change:
			 * ret
			 * To:
			 * stloc.0
			 * leave
			 */
			.MatchStartForward(new CodeMatch(OpCodes.Ret))
				.ThrowIfInvalid("CodeMatcher cannot find 'return statCacheEntry.statValue'")
				.DefineLabel(out var labelRet)
				.Set(OpCodes.Leave, labelRet)
				.Insert(CodeInstruction.StoreLocal(LOCAL_RESULT))
			.MatchStartForward(CodeMatch.LoadsArgument())
				.Set(OpCodes.Nop, null)
				.InsertAfterAndAdvance(
					CodeInstruction.LoadField(typeof(RimWorld_StatWorker_GetValue), nameof(StatLocks))
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock)),
					CodeInstruction.LoadLocal(localLock.LocalIndex),
					new CodeInstruction(OpCodes.Ldelema, typeof(UnfairRwLock)),
					CodeInstruction.Call(typeof(UnfairRwLock), nameof(UnfairRwLock.ExitReadLock)),
					new CodeInstruction(OpCodes.Endfinally).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.EndExceptionBlock)),
					CodeInstruction.LoadArgument(0)
					)
			/*
			 * (this.GetValue(StatRequest.For(thing), true))
			 * StatLocks[lockIndex].EnterWriteLock();
			 * try {
			 */
			.MatchEndForward(
					CodeMatch.Calls(
						AccessTools.Method(
							typeof(StatWorker),
							nameof(StatWorker.GetValue),
							new[] { typeof(StatRequest), typeof(bool) })),
					CodeMatch.StoresLocal())
				.ThrowIfInvalid("CodeMatcher cannot find 'this.GetValue(StatRequest.For(thing), true)'")
				.InsertAfterAndAdvance(

					CodeInstruction.LoadField(typeof(RimWorld_StatWorker_GetValue), nameof(StatLocks)),
					CodeInstruction.LoadLocal(localLock.LocalIndex),
					new CodeInstruction(OpCodes.Ldelema, typeof(UnfairRwLock)),
					CodeInstruction.Call(typeof(UnfairRwLock), nameof(UnfairRwLock.EnterWriteLock)),
					new CodeInstruction(OpCodes.Nop).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock))
					)
			/*
			 * } finally {
			 *     StatLocks[lockIndex].ExitWriteLock();
			 * }
			 * (return)
			 */
			.MatchStartForward(new CodeMatch(OpCodes.Ret))
				.ThrowIfInvalid("CodeMatcher cannot find 'return'")
				.Insert(
					// Save the original result to our local var
					CodeInstruction.StoreLocal(LOCAL_RESULT),
					CodeInstruction.LoadField(typeof(RimWorld_StatWorker_GetValue), nameof(StatLocks))
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock)),
					CodeInstruction.LoadLocal(localLock.LocalIndex),
					new CodeInstruction(OpCodes.Ldelema, typeof(UnfairRwLock)),
					CodeInstruction.Call(typeof(UnfairRwLock), nameof(UnfairRwLock.ExitWriteLock)),
					new CodeInstruction(OpCodes.Endfinally).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.EndExceptionBlock)),
					// Load the result from our local var
					CodeInstruction.LoadLocal(LOCAL_RESULT).WithLabels(labelRet),
					new CodeInstruction(OpCodes.Ret)
				);
			return codeMatcher.Instructions();
		}
	}
}