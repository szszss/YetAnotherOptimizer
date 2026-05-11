using System.Collections.Concurrent;

namespace YaOpt.Helpers
{
	public static class ConcurrentPool<T> where T : new()
	{
		private static readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();

		public static int FreeItemsCount => _bag.Count;

		public static T Get()
		{
			return _bag.TryTake(out var result) ? result : new T();
		}

		public static void Return(T item)
		{
			_bag.Add(item);
		}
	}
}