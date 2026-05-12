using LudeonTK;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Similar to <c>ConcurrentPool</c>, objects borrowed from it
	/// are automatically returned at the end of each tick or render.
	/// </summary>
	public static class TransientPool<T> where T : new()
	{
		private const int CLEAR_INTERVAL = 6000;
		private const int EXPAND_COUNT = 32;

		private static T[] _pool = new T[EXPAND_COUNT];
		private static int _borrowCount = 0;
		private static int _maxBorrowCount = 0;
		private static int _lastClearTick = -1;
		private static GreedySpinLock _lock = new GreedySpinLock();

		static TransientPool()
		{
			for (int i = 0; i < _pool.Length; i++)
			{
				_pool[i] = new T();
			}

			UpdateCallbackHelper.RegisterPostRenderCallback(Recover);
			UpdateCallbackHelper.RegisterPostTickCallback(RecoverAndTryReduce);
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			TransientPoolDebug.PrintDebug += TransientPoolPrintDebug;
		}

		private static void RecoverAndTryReduce(int tick)
		{
			Recover(tick);
			if (_lastClearTick == -1)
			{
				// Force clear on the first tick after loading game.
				ClearCache();
			}
			else if (tick - _lastClearTick < CLEAR_INTERVAL)
			{
				return;
			}
			_lastClearTick = tick;

			// If the maximum usage of the previous 6000 ticks was less than half the capacity,
			// then release the remaining half of the cache objects.
			if (_maxBorrowCount < _pool.Length / 2 && _pool.Length > EXPAND_COUNT)
			{
				var newLength = Math.Max(EXPAND_COUNT, _pool.Length / 2);
				newLength = (newLength + (EXPAND_COUNT - 1)) / EXPAND_COUNT * EXPAND_COUNT;
				if (newLength < _pool.Length)
				{
					var newPool = new T[newLength];
					Array.Copy(_pool, newPool, newLength);
					_pool = newPool;
				}
			}
			
			_maxBorrowCount = 0;
		}

		private static void Recover(int _)
		{
			int currentBorrow = Interlocked.Exchange(ref _borrowCount, 0);
			if (currentBorrow > _maxBorrowCount)
			{
				_maxBorrowCount = currentBorrow;
			}
		}

		private static void ClearCache()
		{
			_lock.Enter();
			try
			{
				_pool = new T[EXPAND_COUNT];
				for (var i = 0; i < EXPAND_COUNT; i++)
				{
					_pool[i] = new T();
				}
				_borrowCount = 0;
				_maxBorrowCount = 0;
				_lastClearTick = -1;
			}
			finally
			{
				_lock.Exit();
			}
		}

		private static void TransientPoolPrintDebug()
		{
			YaOptMod.Log($"TransientPool<{typeof(T)}>");
			YaOptMod.Log($"Count: {_pool.Length} (MaxBorrowed: {_maxBorrowCount}, CurrentBorrowed: {_borrowCount})");
		}

		public static T Borrow()
		{
			var index = Interlocked.Increment(ref _borrowCount) - 1;
			var currentPool = _pool;

			if (index < currentPool.Length)
			{
				return currentPool[index];
			}

			return ExpandAndBorrow(index);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T ExpandAndBorrow(int index)
		{
			_lock.Enter();
			try
			{
				var currentPool = Volatile.Read(ref _pool);
				if (index >= currentPool.Length)
				{
					var currentLength = currentPool.Length;
					var newLength = Math.Max(currentPool.Length + EXPAND_COUNT, index + 1);
					newLength = (newLength + (EXPAND_COUNT - 1)) / EXPAND_COUNT * EXPAND_COUNT;

					var newPool = new T[newLength];
					Array.Copy(currentPool, newPool, currentLength);
					for (var i = currentLength; i < newLength; i++)
					{
						newPool[i] = new T();
					}
					currentPool = newPool;
					Volatile.Write(ref _pool, newPool);
				}

				return currentPool[index];
			}
			finally
			{
				_lock.Exit();
			}
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