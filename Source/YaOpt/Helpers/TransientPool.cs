using LudeonTK;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Similar to <c>ConcurrentPool</c>, objects borrowed from it
	/// are automatically returned at the end of each tick or render.
	/// </summary>
	public static class TransientPool<T> where T : new()
	{
		private const int CLEAR_INTERVAL = 30001;

		private static ConcurrentBag<T> _front = new ConcurrentBag<T>();

		private static ConcurrentBag<T> _back = new ConcurrentBag<T>();

		private static int _lastClearTick = -1;

		static TransientPool()
		{
			UpdateCallbackHelper.RegisterPostRenderCallback(PingPong);
			UpdateCallbackHelper.RegisterPostTickCallback(PingPongWithClearCheck);
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			TransientPoolDebug.PrintDebug += TransientPoolPrintDebug;
		}

		private static void PingPongWithClearCheck(int tick)
		{
			PingPong(tick);
			if (_lastClearTick == -1)
			{
				// Force clear on the first tick after loading game.
				_lastClearTick = tick - CLEAR_INTERVAL;
			}
			if (tick - _lastClearTick < CLEAR_INTERVAL)
			{
				return;
			}
			_lastClearTick = tick;
			// When clear, we keep the bag with less items.
			if (_back.Count < _front.Count)
			{
				(_front, _back) = (_back, _front);
			}
			_back.Clear();
		}

		private static void PingPong(int _)
		{
			// When swap, we keep the bag with more items.
			if (_back.Count > _front.Count)
			{
				(_front, _back) = (_back, _front);
			}
		}

		private static void ClearCache()
		{
			_front.Clear();
			_back.Clear();
			_lastClearTick = -1;
		}

		private static void TransientPoolPrintDebug()
		{
			var f = _front.Count;
			var b = _back.Count;
			var total = f + b;
			YaOptMod.Log($"TransientPool<{typeof(T)}>");
			YaOptMod.Log($"Count: {total} (F: {f}, B: {b})");
		}

		public static T Borrow()
		{
			var t = _front.TryTake(out var result) ? result : new T();
			_back.Add(t);
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T BorrowIfNotMainThread(T t)
		{
			return YaOptGlobal.IsInMainThread ? t : Borrow();
		}
	}

	internal static class TransientPoolDebug
	{
		internal static event Action PrintDebug;

		[DebugOutput("YaOpt", true)]
		public static void PrintTransientPoolUsage()
		{
			YaOptMod.Log("Printing the usage of TransientPool...");
			PrintDebug?.Invoke();
		}
	}
}