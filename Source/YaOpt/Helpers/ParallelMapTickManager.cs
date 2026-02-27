using Gilzoide.ManagedJobs;
using RimWorld;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Verse;
using YaOpt.Patches.ThreadSafe.Delayed;

namespace YaOpt.Helpers
{
	public class ParallelMapTickManager
	{
		/// <summary>
		/// Handle for the parallel map post-tick job.
		/// </summary>
		private static JobHandle _postMapTickJobHandle = default;

		/// <summary>
		/// Reusable array for job handles when processing multiple maps.
		/// </summary>
		private static NativeArray<JobHandle> _tmpJobHandles = default;

		/// <summary>
		/// Indicate whether thread safety patches should check the current thread.
		/// </summary>
		public static bool ShouldCheckThread { get; private set; }

		static ParallelMapTickManager()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreRenderCallback(FinishPostMapTick);
			UpdateCallbackHelper.RegisterPreTickCallback(FinishPostMapTick);
		}

		/// <summary>
		/// Performs parallel pre-tick processing for all maps.
		/// </summary>
		/// <remarks>
		/// Currently a placeholder for future pre-tick parallelization opportunities.
		/// </remarks>
		public static void ParellellyPreTickMaps(List<Map> maps)
		{
		}

		/// <summary>
		/// Schedules parallel post-tick processing for all maps.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Runs the following in parallel for each map:
		/// <list type="bullet">
		/// <item><see cref="SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick"/> - Environmental effects like temperature.</item>
		/// <item><see cref="GasGrid.Tick"/> - Gas simulation.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static void ParellellyPostTickMaps()
		{
			var maps = Find.Maps;
			_postMapTickJobHandle = default;
			if (!_tmpJobHandles.IsCreated || _tmpJobHandles.Length != maps.Count * 2)
			{
				_tmpJobHandles.Dispose();
				_tmpJobHandles = new NativeArray<JobHandle>(maps.Count * 2, Allocator.Persistent);
			}
			var i = 0;
			ShouldCheckThread = true;
			foreach (var map in maps)
			{
				// Rebuilding any dirty region.
				map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
				_tmpJobHandles[i++] = new ManagedJob(new SteadyEnvironmentEffectsJob(map.steadyEnvironmentEffects)).Schedule();
				//_tmpJobHandles[i++] = new ManagedJob(new TempTerrainManagerJob(map.tempTerrain)).Schedule();
				_tmpJobHandles[i++] = new ManagedJob(new GasGridJob(map.gasGrid)).Schedule();
			}
			_postMapTickJobHandle = JobHandle.CombineDependencies(_tmpJobHandles);
		}

		/// <summary>
		/// Completes the pending post-map-tick job. Then process the delayed operations.
		/// </summary>
		/// <remarks>
		/// Called before rendering and before the next tick to ensure all parallel work is complete.
		/// </remarks>
		public static void FinishPostMapTick(int tick)
		{
			_postMapTickJobHandle.Complete();
			_postMapTickJobHandle = default;

			if (!YaOptGlobal.IsInMainThread)
			{
				YaOptMod.Error("Delayed operations must be playbacked in the main thread.");
				return;
			}
			ShouldCheckThread = false;
			RimWorld_SteadyEnvironmentEffects_SteadyEnvironmentEffectsTick.Playback();
			RimWorld_SteadyEnvironmentEffects_DoDeteriorationDamage.Playback();
			Verse_Thing_Destroy.Playback();
			RimWorld_FireUtility_TryStartFireIn.Playback();
			Verse_FleckManager_CreateFleck.Playback();
		}

		/// <summary>
		/// Clears all cached data and resets state.
		/// </summary>
		private static void ClearCache()
		{
			_postMapTickJobHandle = default;

			RimWorld_SteadyEnvironmentEffects_SteadyEnvironmentEffectsTick.Clear();
			RimWorld_SteadyEnvironmentEffects_DoDeteriorationDamage.Clear();
			Verse_Thing_Destroy.Clear();
			RimWorld_FireUtility_TryStartFireIn.Clear();
			Verse_FleckManager_CreateFleck.Clear();
		}

		/// <summary>
		/// Unity Job that runs <see cref="SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick"/> on a worker thread.
		/// </summary>
		private readonly struct SteadyEnvironmentEffectsJob : IJob
		{
			private readonly SteadyEnvironmentEffects _steadyEnvironmentEffects;

			public SteadyEnvironmentEffectsJob(SteadyEnvironmentEffects steadyEnvironmentEffects)
			{
				_steadyEnvironmentEffects = steadyEnvironmentEffects;
			}

			public void Execute()
			{
				try
				{
					_steadyEnvironmentEffects.SteadyEnvironmentEffectsTick();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		/// <summary>
		/// Unity Job that runs <see cref="TempTerrainManager.Tick"/> on a worker thread.
		/// </summary>
		/// <remarks>
		/// <b>Obsolete:</b> Has a race condition with FishShadowComponent which modifies WaterBody concurrently.
		/// </remarks>
		[Obsolete]
		private readonly struct TempTerrainManagerJob : IJob
		{
			private readonly TempTerrainManager _tempTerrainManager;

			public TempTerrainManagerJob(TempTerrainManager tempTerrainManager)
			{
				_tempTerrainManager = tempTerrainManager;
			}

			public void Execute()
			{
				try
				{
					_tempTerrainManager.Tick();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		/// <summary>
		/// Unity Job that runs <see cref="GasGrid.Tick"/> on a worker thread.
		/// </summary>
		private readonly struct GasGridJob : IJob
		{
			private readonly GasGrid _gasGrid;

			public GasGridJob(GasGrid gasGrid)
			{
				_gasGrid = gasGrid;
			}

			public void Execute()
			{
				try
				{
					_gasGrid.Tick();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}
	}
}