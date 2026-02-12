using System;

namespace YaOpt.Helpers.ThreadLocal
{
	[Obsolete]
	//TODO: remove
	internal static class ThreadLocalFloodFiller
	{
		/*private static ThreadLocal<bool> HasParentGrids = new ThreadLocal<bool>(() => false);
		private static ThreadLocal<CellGrid> ParentGrids = new ThreadLocal<CellGrid>();

		static ThreadLocalFloodFiller()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			HasParentGrids.Dispose();
			ParentGrids.Dispose();
			HasParentGrids = new ThreadLocal<bool>(() => false);
			ParentGrids = new ThreadLocal<CellGrid>();
		}

		public static void SetParentGrid(CellGrid cellGrid)
		{
			HasParentGrids.Value = true;
			ParentGrids.Value = cellGrid;
		}

		public static CellGrid GetParentGrid(Map map)
		{
			if (HasParentGrids.Value == false)
				return null;

			var cellGrid = ParentGrids.Value;
			if (!cellGrid.MapSizeMatches(map))
			{
				YaOptMod.Error("Calling ReconstructLastFloodFillPath " +
				               "returns a ParentGrids that does not match " +
				               "the current map, meaning that FloodFill was not " +
				               "called before ReconstructLastFloodFillPath was called, " +
				               "which may result in an error.");
			}
			return cellGrid;
		}*/
	}
}