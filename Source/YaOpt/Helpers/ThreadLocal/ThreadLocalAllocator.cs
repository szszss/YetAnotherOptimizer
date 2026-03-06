using System;
using System.Collections.Generic;
using System.Threading;

namespace YaOpt.Helpers.ThreadLocal
{
	public static class ThreadLocalAllocator<T> where T : new()
	{
		private static readonly Dictionary<string, int> _keyMapping = new Dictionary<string, int>();
		private static readonly List<ThreadLocal<T>> _threadLocals = new List<ThreadLocal<T>>();

		static ThreadLocalAllocator()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		public static int TryAllocate(string key, bool trackAllValues = false)
		{
			if (_keyMapping.TryGetValue(key, out var index))
				return index;
			return Allocate(key, trackAllValues);
		}

		public static int Allocate(string key, bool trackAllValues = false)
		{
			if (_keyMapping.ContainsKey(key))
				throw new ArgumentException($"Key {key} is already in use.", nameof(key));
			_threadLocals.Add(new ThreadLocal<T>(() => new T(), trackAllValues));
			var index = _threadLocals.Count - 1;
			_keyMapping[key] = index;
			return index;
		}

		public static T Get(int index)
		{
			return _threadLocals[index].Value;
		}

		public static IList<T> GetAllValues(int index)
		{
			return _threadLocals[index].Values;
		}

		private static void ClearCache()
		{
			for (var i = 0; i < _threadLocals.Count; i++)
			{
				_threadLocals[i].Dispose();
				_threadLocals[i] = new ThreadLocal<T>(() => new T());
			}
		}
	}
}