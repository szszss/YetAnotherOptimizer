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
		private static readonly  ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

		private static readonly ConcurrentBag<T> _loans = new ConcurrentBag<T>();

		private static volatile bool _hasLoan;

		public static int FreeItemsCount => _queue.Count;

		public static int BorrowedItemsCount => _loans.Count;

		static TransientPool()
		{
			UpdateCallbackHelper.RegisterPostRenderCallback(Recovery);
			UpdateCallbackHelper.RegisterPostTickCallback(Recovery);
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			TransientPoolDebug.PrintDebug += TransientPoolPrintDebug;
		}

		private static void Recovery(int _)
		{
			if (_hasLoan)
			{
				_hasLoan = false;
				while (_loans.TryTake(out var t))
				{
					_queue.Enqueue(t);
				}
			}
		}

		private static void ClearCache()
		{
			_queue.Clear();
			_loans.Clear();
			_hasLoan = false;
		}

		private static void TransientPoolPrintDebug()
		{
			var total = FreeItemsCount + BorrowedItemsCount;
			YaOptMod.Log($"TransientPool<{typeof(T)}>");
			YaOptMod.Log($"Count: {total}");
		}

		public static T Borrow()
		{
			_hasLoan = true;
			var t = _queue.TryDequeue(out var result) ? result : new T();
			_loans.Add(t);
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