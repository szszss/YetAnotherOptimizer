using System.Threading;
using YaOpt.Helpers.ThirdParty;
// ReSharper disable StaticMemberInGenericType

namespace YaOpt.Helpers
{
	public static class LockBoilerplate
	{
		public const string ENTER = "EnterLock";
		public const string ENTER_READ = "EnterReadLock";
		public const string ENTER_WRITE = "EnterWriteLock";
		public const string EXIT = "ExitLock";
		public const string EXIT_READ = "ExitReadLock";
		public const string EXIT_WRITE = "ExitWriteLock";

		public static class Spin<T>
		{
			private static SpinLock _spinLock = new SpinLock();

			public static void EnterLock(out bool __state)
			{
				__state = false;
				_spinLock.Enter(ref __state);
			}

			public static void ExitLock(bool __state)
			{
				if (__state)
					_spinLock.Exit();
			}
		}

		public static class UnfairReadWrite<T>
		{
			private static UnfairRwLock _rwLock = new UnfairRwLock();

			public static void EnterReadLock(out bool __state)
			{
				__state = true;
				_rwLock.EnterReadLock();
			}

			public static void ExitReadLock(bool __state)
			{
				if (__state)
					_rwLock.ExitReadLock();
			}

			public static void EnterWriteLock(out bool __state)
			{
				__state = true;
				_rwLock.EnterWriteLock();
			}

			public static void ExitWriteLock(bool __state)
			{
				if (__state)
					_rwLock.ExitWriteLock();
			}
		}
	}
}