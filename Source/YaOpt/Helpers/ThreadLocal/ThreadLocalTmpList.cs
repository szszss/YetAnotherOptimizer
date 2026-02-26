using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace YaOpt.Helpers.ThreadLocal
{
	public static class ThreadLocalTmpList<K, T>
	{
		private static ThreadLocal<List<T>> tmpLists = new ThreadLocal<List<T>>(() => new List<T>());

		// ReSharper disable once StaticMemberInGenericType
		private static bool _trackAllValues = false;

		public static bool TrackAllValues
		{
			get => _trackAllValues;
			set
			{
				if (_trackAllValues != value)
				{
					ClearCache();
				}
				_trackAllValues = value;
			}
		}

		public static IList<List<T>> Values
		{
			get
			{
				if (!_trackAllValues)
				{
					throw new Exception("Attempting to retrieve all values of a " +
										"ThreadLocalTmpList that does not have TrackAllValues enabled.");
				}
				return tmpLists.Values;
			}
		}

		static ThreadLocalTmpList()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static List<T> Get()
		{
			return tmpLists.Value;
		}

		private static void ClearCache()
		{
			tmpLists.Dispose();
			tmpLists = new ThreadLocal<List<T>>(() => new List<T>(), _trackAllValues);
		}
	}
}