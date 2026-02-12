using System.Collections.Generic;
using System.Threading;
using Verse;
using Verse.AI;
using static YaOpt.Helpers.ThreadLocal.ThreadLocalHelper;

namespace YaOpt.Helpers.ThreadLocal
{
	internal static class ThreadLocalAttackTargetFinder
	{
		public static ThreadLocal<List<IAttackTarget>> TmpTargets =
			new ThreadLocal<List<IAttackTarget>>(NewList<IAttackTarget>);

		public static ThreadLocal<List<Pair<IAttackTarget, float>>> AvailableShootingTargets =
			new ThreadLocal<List<Pair<IAttackTarget, float>>>(NewList<Pair<IAttackTarget, float>>);

		public static ThreadLocal<List<float>> TmpTargetScores =
			new ThreadLocal<List<float>>(NewList<float>);

		public static ThreadLocal<List<bool>> TmpCanShootAtTarget =
			new ThreadLocal<List<bool>>(NewList<bool>);

		public static ThreadLocal<List<IntVec3>> TempDestList =
			new ThreadLocal<List<IntVec3>>(NewList<IntVec3>);

		public static ThreadLocal<List<IntVec3>> TempSourceList =
			new ThreadLocal<List<IntVec3>>(NewList<IntVec3>);

		static ThreadLocalAttackTargetFinder()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			TmpTargets.Dispose();
			TmpTargets = new ThreadLocal<List<IAttackTarget>>(NewList<IAttackTarget>);
			AvailableShootingTargets.Dispose();
			AvailableShootingTargets =
				new ThreadLocal<List<Pair<IAttackTarget, float>>>(NewList<Pair<IAttackTarget, float>>);
			TmpTargetScores.Dispose();
			TmpTargetScores = new ThreadLocal<List<float>>(NewList<float>);
			TmpCanShootAtTarget.Dispose();
			TmpCanShootAtTarget = new ThreadLocal<List<bool>>(NewList<bool>);
			TempDestList.Dispose();
			TempDestList = new ThreadLocal<List<IntVec3>>(NewList<IntVec3>);
			TempSourceList.Dispose();
			TempSourceList = new ThreadLocal<List<IntVec3>>(NewList<IntVec3>);
		}
	}
}