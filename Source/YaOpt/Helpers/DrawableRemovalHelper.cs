using Gilzoide.ManagedJobs;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	internal static class DrawableRemovalHelper
	{
		private static readonly ConcurrentQueue<(List<Thing>, Thing)> _thingToDeRegisterDrawable =
			new ConcurrentQueue<(List<Thing>, Thing)>();

		private static JobHandle _handle;

		private static bool _isJobRunning;

		static DrawableRemovalHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPostTickCallback(PostTick);
		}

		private static void ClearCache()
		{
			_thingToDeRegisterDrawable.Clear();
		}

		private static void PostTick(int _)
		{
			FinishRemovalJob();
		}

		public static bool DeRegisterDrawable(List<Thing> list, Thing thing)
		{
			_thingToDeRegisterDrawable.Enqueue((list, thing));
			return true;
		}

		public static void StartRemovalJob()
		{
			_isJobRunning = !_thingToDeRegisterDrawable.IsEmpty;
			if (_isJobRunning)
			{
				_handle = new ManagedJob(new RemoveDrawableJob()).Schedule();
			}
		}

		public static void FinishRemovalJob()
		{
			if (_isJobRunning)
			{
				_handle.Complete();
				_isJobRunning = false;
			}
		}

		private readonly struct RemoveDrawableJob : IJob
		{
			public void Execute()
			{
				while (_thingToDeRegisterDrawable.TryDequeue(out var result))
				{
					result.Item1.ReverseRemove(result.Item2);
				}
			}
		}
	}
}