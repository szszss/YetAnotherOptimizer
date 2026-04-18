using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace YaOpt.Helpers.ThreadSafe
{
	public struct GreedySpinLock
	{
		private const int DEADLOCK_TIMEOUT = 10000; // The unit is Ms
		private int _state;
		private int _recursion;
		private bool _supportRecursion;
		private bool _dontCheckDeadlock;
		private bool _panic;
		private Thread _ownerThread;

		public bool SupportRecursion
		{
			get => _supportRecursion;
			set => _supportRecursion = value;
		}

		public bool DetectDeadlock
		{
			get => !_dontCheckDeadlock;
			set => _dontCheckDeadlock = !value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enter(ref bool taken)
		{
			Enter();
			taken = true;
		}

		public void Enter()
		{
			if (_panic)
				Panic();

			var waitBeginTime = 0;
			while (true)
			{
				var spinCount = 0;
				while (Volatile.Read(ref _state) == 1)
				{
					if (_supportRecursion)
					{
						var currentOwner = Volatile.Read(ref _ownerThread);
						if (currentOwner == Thread.CurrentThread)
						{
							Interlocked.Increment(ref _recursion);
							return;
						}
					}
					var waitTime = 4 << Math.Min(spinCount, 10);
					Thread.SpinWait(waitTime);
					spinCount++;
					// Detect deadlock if the lock cost too many time,
					if (!_dontCheckDeadlock && spinCount > 100)
					{
						CheckDeadlock(ref waitBeginTime);
					}
					if (spinCount > 10000) // Prevent the OS freezing
					{
						Thread.Yield();
					}
				}

				if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
				{
					if (_supportRecursion)
					{
						Volatile.Write(ref _ownerThread, Thread.CurrentThread);
						Interlocked.Increment(ref _recursion);
					}
					break;
				}
			}
		}

		public void Exit()
		{
			if (_supportRecursion)
			{
				if (Interlocked.Decrement(ref _recursion) != 0)
				{
					return;
				}
				Volatile.Write(ref _ownerThread, null);
			}
			Volatile.Write(ref _state, 0);
		}

		private void Panic()
		{
			throw new LockException("The program is unable to run due to a faulty synchronization lock. " +
									"Please report this issue to the YaOpt developers. If you wish to save " +
									"the game, it is recommended to save as a new save file. " +
									"Disabling all multi-threading optimizations in YaOpt (those with " +
									"the [MT] tag after their names) can temporarily workaround this problem.");
		}

		private void CheckDeadlock(ref int beginTime)
		{
			var currentTime = Environment.TickCount;
			if (beginTime == 0)
				beginTime = currentTime;
			if (currentTime - beginTime > DEADLOCK_TIMEOUT)
			{
				if (!_panic)
				{
					_panic = true;
					var reason = _supportRecursion
						? "a deadlock. "
						: "either a deadlock or lock recursion. ";
					YaOptMod.Panic("The program encountered a faulty synchronization lock, " +
								   "which could be caused by " + reason +
								   "A series of error logs containing call stack information " +
								   "will be printed below. Please report this issue to the YaOpt developers.");
				}
				Panic();
			}
		}
	}
}