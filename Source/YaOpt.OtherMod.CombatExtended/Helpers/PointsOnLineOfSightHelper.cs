using LudeonTK;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Verse;
using YaOpt.Unity;

namespace YaOpt.OtherMod.CombatExtended.Helpers
{
	internal static class PointsOnLineOfSightHelper
	{
		private const int MAX_RESULT_COUNT = 768;

		private static NativeArray<int3> _resultArray;

		private static NativeArray<IntVec3> _resultArrayIntVecView;


		static PointsOnLineOfSightHelper()
		{
			_resultArray = new NativeArray<int3>(MAX_RESULT_COUNT, Allocator.Persistent);
			_resultArrayIntVecView = _resultArray.Reinterpret<IntVec3>();
		}

		public static IEnumerable<IntVec3> PointsOnLineOfSight(in Vector3 startPos, in Vector3 endPos)
		{
			_resultArray.Clear();
			YaOptBurst.PointsOnLineOfSight(startPos, endPos, ref _resultArray, out var count, out bool oob);
			if (oob)
			{
				YaOptMod.Error($"PointsOnLineOfSight returned too many results (>{MAX_RESULT_COUNT})");
			}
			return _resultArrayIntVecView.GetSubArray(0, count);
		}
	}
}