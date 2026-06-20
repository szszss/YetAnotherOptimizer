using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace YaOpt.Helpers.ThirdParty
{
	/// <summary>
	/// High-performance read-write lock written by Sebastian Schöner.
	/// It has extremely high read lock performance. The disadvantages are unfairness
	/// and non-reentrant. These can be circumvented in specific scenarios.
	/// </summary>
	/// <seealso cref="https://blog.s-schoener.com/2026-01-28-rwlocks/"/>
	[StructLayout(LayoutKind.Auto, Size = 64)] // Fill a cache line to avoid false sharing
	public struct UnfairRwLock
	{
		// If zero: no reader, no writer
		// If negative: writer active (MinValue + any in-flight reader increments)
		// If positive: that many readers hold the lock
		private long _state;

		public void EnterReadLock()
		{
			long value = Interlocked.Increment(ref _state);
			if (value > 0)
				return;

			Interlocked.Decrement(ref _state);

			var spinCount = 0;
			while (true)
			{
				var waitTime = 4 << spinCount;
				Thread.SpinWait(waitTime);
				value = Interlocked.Increment(ref _state);
				if (value > 0)
					return;
				Interlocked.Decrement(ref _state);
				spinCount = Math.Min(10, spinCount + 1);
			}
		}

		public void EnterWriteLock()
		{
			var spinCount = 0;
			while (true)
			{
				long current = Volatile.Read(ref _state);

				if (current < 0)
				{
					var waitTime = 4 << spinCount;
					Thread.SpinWait(waitTime);
					spinCount = Math.Min(10, spinCount + 1);
					continue;
				}

				if (Interlocked.CompareExchange(ref _state, long.MinValue + current, current) == current)
				{
					// Wait for pre-existing readers to drain
					while (Volatile.Read(ref _state) != long.MinValue)
					{
						var waitTime = 4 << spinCount;
						Thread.SpinWait(waitTime);
						spinCount = Math.Min(10, spinCount + 1);
					}
					return;
				}
			}
		}

		public void ExitReadLock() => Interlocked.Decrement(ref _state);

		// Add back MinValue to undo the subtraction, preserving any in-flight reader increments
		public void ExitWriteLock() => Interlocked.Add(ref _state, long.MinValue);

		public static class InstanceOf<T>
		{
			public static UnfairRwLock Lock = new UnfairRwLock();
		}
	}
}
