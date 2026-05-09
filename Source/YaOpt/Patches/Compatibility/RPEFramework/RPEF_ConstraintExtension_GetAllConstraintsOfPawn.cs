using HarmonyLib;
using System;
using System.Reflection;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches.Compatibility.RPEFramework
{
	[HarmonyPatch]
	internal static class RPEF_ConstraintExtension_GetAllConstraintsOfPawn
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

		static MethodBase TargetMethod()
		{
			var type = AccessTools.TypeByName("RPEF.ConstraintExtension");
			//var method = AccessTools.Method(type, "GetAllConstraintsOfPawn");
			foreach (var nested in type.GetNestedTypes(BindingFlags.NonPublic))
			{
				if (!nested.Name.Contains("GetAllConstraintsOfPawn"))
					continue;
				var moveNext = nested.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
				if (moveNext != null)
					return moveNext;
			}
			throw new MissingMethodException(
				"Cannot find GetAllConstraintsOfPawn.MoveNext for RPEF.ConstraintExtension. " +
				"This may be due to the mod update. Please report this to the YaOpt developers.");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelWorkGiver.Enabled &&
				   YaOptGlobal.HasType("RPEF.ConstraintExtension");
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			_spinLock.Enter(ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				_spinLock.Exit();
		}

		// For the next version of RPE Framework
		/*
		private const int WRITE = 1;
		private const int READ = -1;

		private static UnfairRwLock _rwLock = new UnfairRwLock();

		static void Prefix(Pawn pawn, IDictionary ____pawnConstraintCache, out int __state)
		{
			if (pawn == null)
			{
				__state = 0;
				return;
			}
			_rwLock.EnterReadLock();
			__state = READ;
			if (!____pawnConstraintCache.Contains(pawn.thingIDNumber))
			{
				_rwLock.ExitReadLock();
				__state = 0;
				_rwLock.EnterWriteLock();
				__state = WRITE;
			}
		}

		static void Finalizer(int __state)
		{
			if (__state == READ)
				_rwLock.ExitReadLock();
			else if (__state == WRITE)
				_rwLock.ExitWriteLock();
		}
		*/
	}
}