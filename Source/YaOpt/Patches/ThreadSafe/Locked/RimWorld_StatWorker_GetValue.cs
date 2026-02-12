using HarmonyLib;
using RimWorld;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(StatWorker))]
	[HarmonyPatch(nameof(StatWorker.GetValue), typeof(Thing), typeof(bool), typeof(int))]
	internal static class RimWorld_StatWorker_GetValue
	{
		private static readonly ConcurrentDictionary<StatWorker, ReaderWriterLockSlim> statLocks =
			new ConcurrentDictionary<StatWorker, ReaderWriterLockSlim>();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		public static ReaderWriterLockSlim GetLock(StatWorker worker)
		{
			return statLocks.GetOrAdd(worker, _ => new ReaderWriterLockSlim(/*LockRecursionPolicy.SupportsRecursion*/));
		}

		/*static void Postfix(StatWorker __instance, ref float __result)
		{
			YaOptMod.Warning($"{__instance} return {__result} from thread {Thread.CurrentThread.ManagedThreadId}");
		}*/

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// The leave instruction empties the evaluation stack, so we have to save the result to a local var
			const int LOCAL_RESULT = 0;
			var typeReaderWriterLockSlim = typeof(ReaderWriterLockSlim);
			var codeMatcher = new CodeMatcher(instructions, generator);
			codeMatcher.DeclareLocal(typeof(ReaderWriterLockSlim), out var localLock);
			/*
			 * (StatCacheEntry statCacheEntry)
			 * var lock = RimWorld_StatWorker_GetValue.GetLock(this);
			 * lock.EnterReadLock();
			 * try {
			 *     (if (cacheStaleAfterTicks != -1 ...))
			 */
			codeMatcher.MatchStartForward(
				CodeMatch.LoadsArgument(),
				CodeMatch.LoadsConstant(-1),
				CodeMatch.Branches())
				.ThrowIfInvalid("CodeMatcher cannot find 'cacheStaleAfterTicks != -1'")
				.InsertAndAdvance(
					// var lock = RimWorld_StatWorker_GetValue.GetLock(this);
					CodeInstruction.LoadArgument(0),
					CodeInstruction.Call(typeof(RimWorld_StatWorker_GetValue), nameof(GetLock)),
					CodeInstruction.StoreLocal(localLock.LocalIndex),
					// localLock.Enter(ref localHasTaken)
					CodeInstruction.LoadLocal(localLock.LocalIndex),
					new CodeInstruction(OpCodes.Callvirt,
						AccessTools.Method(
							typeReaderWriterLockSlim,
							nameof(ReaderWriterLockSlim.EnterReadLock))),
					new CodeInstruction(OpCodes.Nop).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock))
					)
			/*
			 *     (return statCacheEntry.statValue;)
			 * } finally {
			 *     lock.ExitReadLock();
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
					CodeInstruction.LoadLocal(localLock.LocalIndex).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock)),
					CodeInstruction.Call(
						typeReaderWriterLockSlim,
						nameof(ReaderWriterLockSlim.ExitReadLock)),
					new CodeInstruction(OpCodes.Endfinally).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.EndExceptionBlock)),
					CodeInstruction.LoadArgument(0)
					)
			/*
			 * (this.GetValue(StatRequest.For(thing), true))
			 * lock.EnterWriteLock();
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

					CodeInstruction.LoadLocal(localLock.LocalIndex),
					CodeInstruction.Call(
						typeReaderWriterLockSlim,
						nameof(ReaderWriterLockSlim.EnterWriteLock)),
					new CodeInstruction(OpCodes.Nop).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock))
					)
			/*
			 * } finally {
			 *     lock.ExitWriteLock();
			 * }
			 * (return)
			 */
			.MatchStartForward(new CodeMatch(OpCodes.Ret))
				.ThrowIfInvalid("CodeMatcher cannot find 'return'")
				.Insert(
					// Save the original result to our local var
					CodeInstruction.StoreLocal(LOCAL_RESULT),
					CodeInstruction.LoadLocal(localLock.LocalIndex).WithBlocks(
						new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock)),
					CodeInstruction.Call(
						typeReaderWriterLockSlim,
						nameof(ReaderWriterLockSlim.ExitWriteLock)),
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