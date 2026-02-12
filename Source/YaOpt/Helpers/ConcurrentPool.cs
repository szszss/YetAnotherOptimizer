using System.Collections.Concurrent;

namespace YaOpt.Helpers
{
	internal static class ConcurrentPool<T> where T : new()
	{
		public static ConcurrentBag<T> Bag = new ConcurrentBag<T>();

		public static int FreeItemsCount => Bag.Count;

		public static T Get()
		{
			return Bag.TryTake(out var result) ? result : new T();
		}

		public static void Return(T item)
		{
			Bag.Add(item);
		}
	}
}