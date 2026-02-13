using System.Collections.Generic;
using System.Threading;

namespace YaOpt.Helpers
{
	public static class ThreadLocalTmpList<K, T>
	{
		private static ThreadLocal<List<T>> tmpLists = new ThreadLocal<List<T>>(() => new List<T>());

		static ThreadLocalTmpList()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		public static List<T> Get()
		{
			return tmpLists.Value;
		}

		private static void ClearCache()
		{
			tmpLists.Dispose();
			tmpLists = new ThreadLocal<List<T>>(() => new List<T>());
		}
	}
}