using RimWorld.Planet;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace YaOpt.Helpers
{
	internal static class CameraHelper
	{
		public static CellRect ViewOfCamera { get; private set; }

		public static CameraZoomRange CurrentZoomRange { get; private set; }

		public static bool DrawingMap { get; private set; }

		private static bool _noCamera;

		static CameraHelper()
		{
			UpdateCallbackHelper.RegisterPreTickCallback(PreTick);
		}

		private static void PreTick(int tick)
		{
			var cameraDriver = Find.CameraDriver;
			var map = Find.CurrentMap;
			if (cameraDriver == null || map == null)
			{
				ViewOfCamera = CellRect.Empty;
				CurrentZoomRange = CameraZoomRange.Furthest;
				DrawingMap = false;
				_noCamera = true;
				return;
			}
			var rect = cameraDriver.CurrentViewRect.ExpandedBy(1);
			rect.ClipInsideMap(map);
			ViewOfCamera = rect;
			CurrentZoomRange = cameraDriver.CurrentZoom;
			DrawingMap = WorldRendererUtility.DrawingMap;
			_noCamera = false;
		}

		public static bool CameraInViewOf(Thing thing)
		{
			if (_noCamera)
				return false;
			var drawSize = thing.DrawSize;
			var drawSizeInt = new IntVec2(Mathf.CeilToInt(drawSize.x), Mathf.CeilToInt(drawSize.y));
			if (drawSizeInt.x <= 0 || drawSizeInt.z <= 0)
				return false;

			var pos = thing.Position;
			var maxSide = Math.Max(drawSizeInt.x, drawSizeInt.z);
			var rect = new CellRect(pos.x - (maxSide - 1) / 2, pos.z - (maxSide - 1) / 2, maxSide, maxSide);
			return Overlaps(ViewOfCamera, rect);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Overlaps(in CellRect a, in CellRect b)
		{
			return a.minX <= b.maxX && a.maxX >= b.minX && a.maxZ >= b.minZ && a.minZ <= b.maxZ;
		}
	}
}