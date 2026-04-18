using System;
using System.Threading;

namespace YaOpt.Helpers.ThreadSafe
{
	public static class GreedyMonitor
	{
		private const int DEADLOCK_TIMEOUT = 10000; // The unit is Ms

		public static void Enter(object obj, bool detectDeadlock = true)
		{
			var taken = false;
			Enter(obj, ref taken, detectDeadlock);
		}

		public static void Enter(object obj, ref bool lockTaken, bool detectDeadlock = true)
		{
			var waitBeginTime = 0;
			var spinCount = 0;
			while (!Monitor.TryEnter(obj))
			{
				var waitTime = 4 << Math.Min(spinCount, 10);
				Thread.SpinWait(waitTime);
				spinCount++;
				if (detectDeadlock && spinCount > 100)
				{
					CheckDeadlock(ref waitBeginTime);
				}
				if (spinCount > 10000) // Prevent the OS freezing
				{
					Thread.Yield();
				}
			}
			lockTaken = true;
		}

		public static void Exit(object obj)
		{
			Monitor.Exit(obj);
		}

		private static void CheckDeadlock(ref int beginTime)
		{
			var currentTime = Environment.TickCount;
			if (beginTime == 0)
				beginTime = currentTime;
			if (currentTime - beginTime > DEADLOCK_TIMEOUT)
			{
				YaOptMod.Panic("The program encountered a faulty synchronization lock, " +
							   "which could be caused by a deadlock. " +
							   "A series of error logs containing call stack information " +
							   "will be printed below. Please report this issue to the YaOpt developers.");
				throw new LockException("The program is unable to run due to a faulty synchronization lock. " +
										"Please report this issue to the YaOpt developers. If you wish to save " +
										"the game, it is recommended to save as a new save file. " +
										"Disabling all multi-threading optimizations in YaOpt (those with " +
										"the [MT] tag after their names) can temporarily workaround this problem.");
			}
		}
	}
}