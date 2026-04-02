using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace YaOpt.Helpers
{
	public struct GreedySpinLock
	{
		private int _state;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enter(ref bool taken)
		{
			Enter();
			taken = true;
		}

		public void Enter()
		{
			while (true)
			{
				var spinCount = 0;
				while (Volatile.Read(ref _state) == 1)
				{
					var waitTime = 4 << spinCount;
					Thread.SpinWait(waitTime);
					spinCount = Math.Min(10, spinCount + 1);
				}

				if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
				{
					break;
				}
			}
		}

		public void Exit()
		{
			Volatile.Write(ref _state, 0);
		}
	}
}