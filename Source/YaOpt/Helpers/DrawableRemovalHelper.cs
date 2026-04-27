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
			UpdateCallbackHelper.RegisterPreRenderCallback(PreRender);
		}

		private static void ClearCache()
		{
			_thingToDeRegisterDrawable.Clear();
		}

		private static void PostTick(int _)
		{
			FinishRemovalJob();
		}

		/// <summary>
		/// The resurrection of corpses caused by deadlife dust is calculated in PostMapTick,
		/// and this resurrection triggers a DeRegisterDrawable.
		/// Therefore, we must recheck for any unprocessed DeRegisterDrawable requests before rendering begins.
		/// </summary>
		private static void PreRender(int _)
		{
			ParallelMapTickManager.FinishPostMapTick(_);
			if (!_thingToDeRegisterDrawable.IsEmpty)
			{
#if DEBUG
				YaOptMod.Debug("DrawableRemovalHelper.PreRender found unprocessed DeRegisterDrawable requests.");
#endif
				while (_thingToDeRegisterDrawable.TryDequeue(out var result))
				{
#if DEBUG
					YaOptMod.Debug($"Unprocessed request: {result.Item2.ToStringSafe()}");
#endif
					result.Item1.ReverseRemove(result.Item2);
				}
			}
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
				_handle = new YaOptManagedJobs.Job(new RemoveDrawableJob()).Schedule();
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