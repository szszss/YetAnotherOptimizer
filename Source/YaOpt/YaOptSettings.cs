using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt
{
	public class YaOptSettings : ModSettings
	{
		private enum SettingTab
		{
			Main,
			Fps,
			Tps,
			Misc
		}

		[Flags]
		public enum OptimizationCategory : byte
		{
			Hidden = 0,
			Main = 0b0001,
			Fps  = 0b0010,
			Tps  = 0b0100,
			Misc = 0b1000,
			Any  = 0b1111
		}

		[Flags]
		public enum OptimizationFlag : ushort
		{
			None = 0,
			MultiplayerIncompatible = 0b0000_0001,
			RequireWin64            = 0b0000_0010,
			RequireBurst            = 0b0001_0000,

			NoSnapshot            = 0b0001_0000_0000_0000,
			IgnoreEnableAll       = 0b0010_0000_0000_0000,
			IgnoreDisableAll      = 0b0100_0000_0000_0000,
			DontSave              = 0b1000_0000_0000_0000,
		}

		public class OptimizationOption
		{
			internal bool _enabled = true;

			private bool _default = true;

			public bool Enabled
			{
				get
				{
					if (!MultiplayerCompatibility && YaOptGlobal.IsMultiplayer)
						return false;
					if (!string.IsNullOrWhiteSpace(RequiredMod) && !YaOptGlobal.HasMod(RequiredMod))
						return false;
					if (RequiredOption != null && !RequiredOption.Enabled)
						return false;
					if (CompatibilityDef.CachedBannedOptimizations.Contains(SettingId))
						return false;
					return _enabled;
				}
				set => _enabled = value;
			}

			public bool Default
			{
				get => _default;
				set
				{
					_default = value;
					_enabled = value;
				}
			}

			public string Name { get; set; } = string.Empty;

			public string Desc { get; set; } = string.Empty;

			public string NoteStability { get; set; } = string.Empty;

			public string NoteCompatibility { get; set; } = string.Empty;

			public string RequiredMod { get; set; } = string.Empty;

			public string SubCategory { get; set; } = string.Empty;

			public string SettingId { get; set; } = string.Empty;

			public OptimizationCategory Category { get; set; }

			public OptimizationFlag Flags { get; set; }

			public OptimizationOption RequiredOption;

			public Func<YaOptSettings, bool> FuncShow { get; set; } = null;

			public Action<YaOptSettings, Listing_Standard, OptimizationOption> FuncPostDraw { get; set; } = null;

			public Action<YaOptSettings> FuncExposeData { get; set; } = null;

			public bool MultiplayerCompatibility => (Flags & OptimizationFlag.MultiplayerIncompatible) == 0;

			public bool Validate(bool dryRun, bool silent, out string message)
			{
				// Validator doesn't validate multiplay and mod requirements. They are validated in the getter of Enabled
				message = string.Empty;
				var error = false;
				if (_enabled && (Flags & OptimizationFlag.RequireWin64) > 0 && !YaOptGlobal.IsWindows)
				{
					if (!dryRun)
						_enabled = false;
					error = true;
					message = "YaOpt.Setting.InvalidOption.RequireWin64".Translate().ToString();
				}
				if (_enabled && (Flags & OptimizationFlag.RequireBurst) > 0 && !YaOptGlobal.IsBurstAvailable)
				{
					if (!dryRun)
						_enabled = false;
					error = true;
					message = "YaOpt.Setting.InvalidOption.RequireBurst".Translate().ToString();
				}
				if (!silent && message != string.Empty)
				{
					YaOptMod.Error($"Optimization {Name.Translate()} has been disabled because {message}");
				}
				return !error;
			}

			public bool ShouldShow(YaOptSettings settings)
			{
				if (!MultiplayerCompatibility && YaOptGlobal.IsMultiplayer)
					return false;
				if (!string.IsNullOrWhiteSpace(RequiredMod) && !YaOptGlobal.HasMod(RequiredMod))
					return false;
				if (FuncShow != null && !FuncShow(settings))
					return false;
				return true;
			}
		}

		public OptimizationOption DebugOutput { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.DebugOutput",
			Desc = "YaOpt.Setting.Option.DebugOutput.Desc",
			Category = OptimizationCategory.Main,
			Flags = OptimizationFlag.IgnoreEnableAll | OptimizationFlag.IgnoreDisableAll | OptimizationFlag.NoSnapshot,
			Default = false
		};

		/// <summary>
		/// Optimizes <see cref="Material.color"/> by caching the value in a managed container.
		/// Unity <see cref="Material.color"/> involves a managed-to-native transition,
		/// which creates significant overhead when called frequently.
		/// This option patchs the setter of <see cref="Material.color"/> to update the cache and
		///  the getter of <see cref="Material.GetColor(string)"/> to retrieve the value from cache.
		/// <br/>
		/// <seealso cref="Patches.UnityEngine_Material_GetColor"/>
		/// <seealso cref="Patches.UnityEngine_Material_SetColor"/>
		/// </summary>
		public OptimizationOption OptMaterialGetColor { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.MaterialGetColor",
			Desc = "YaOpt.Setting.Option.MaterialGetColor.Desc",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Optimizes <see cref="ColoredText.Resolve(TaggedString)"/> and <see cref="ColoredText.StripTags(string)"/>.
		/// Replaces <see cref="string.IndexOf(string)"/> (culture-sensitive) with <see cref="string.IndexOf(string, StringComparison)"/>
		/// using <see cref="StringComparison.Ordinal"/> to avoid unnecessary overhead during text tagging.
		/// <br/>
		/// <seealso cref="Patches.MultiTargets_ColoredText"/>
		/// </summary>
		public OptimizationOption OptColoredText { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ColoredText",
			Desc = "YaOpt.Setting.Option.ColoredText.Desc",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Starts the render preparation for dynamic objects earlier.
		/// <br/>
		/// Details:
		/// <list type="bullet">
		/// <item>Removes <c>ComputeCulledThings</c>, <c>DynamicDrawPhase(DrawPhase.EnsureInitialized)</c>,
		///		and <c>PreDrawVisibleThings</c> from <c>DynamicDrawManager.DrawDynamicThings</c>.</item>
		/// <item>Moves camera culling (<c>ComputeCulledThings</c>) to <c>MapMeshDrawerUpdate_First</c>
		///		(which runs before dynamic drawing).</item>
		/// <item>Moves initialization and parallel preparation to <c>DrawMapMesh</c>
		///		(which allows the main thread to render the static map mesh while worker threads prepare dynamic things).</item>
		/// </list>
		/// <br/>
		/// <seealso cref="Patches.Verse_DynamicDrawManager_ComputeCulledThings"/>
		/// <seealso cref="Patches.Verse_DynamicDrawManager_DrawDynamicThings"/>
		/// <seealso cref="Patches.Verse_DynamicDrawManager_PreDrawVisibleThings"/>
		/// <seealso cref="Patches.Verse_MapDrawer_DrawMapMesh"/>
		/// <seealso cref="Patches.Verse_MapDrawer_MapMeshDrawerUpdate_First"/>
		/// </summary>
		public OptimizationOption OptEarlyRenderPrepare { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.EarlyRenderPrepare",
			Desc = "YaOpt.Setting.Option.EarlyRenderPrepare.Desc",
			NoteCompatibility = "YaOpt.Setting.Option.EarlyRenderPrepare.Compatibility",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Optimizes the batch size for parallel render preparation.
		/// Vanilla uses a large batch size for parallel render preparation.
		/// However, rendering costs vary significantly between objects (e.g., pawns vs. items),
		/// and objects are often clustered (e.g., raids).
		/// This can lead to thread imbalance where some threads finish early while others are stuck with heavy batches.
		/// This optimization reduces the batch size, allowing the work-stealing algorithm to distribute the load more evenly.
		/// <br/>
		/// <seealso cref="Patches.Verse_DynamicDrawManager_PreDrawVisibleThings"/>
		/// </summary>
		public OptimizationOption OptPrepareBatchCount { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.PrepareBatchCount",
			Desc = "YaOpt.Setting.Option.PrepareBatchCount.Desc",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Moves material parameter updates (e.g., via <see cref="PawnRenderNodeWorker"/>)
		/// to the parallel rendering preparation phase.
		/// Vanilla performs these updates on the main thread, but they are generally thread-safe.
		/// If a mod overrides the update logic in a way that might not be thread-safe,
		/// YaOpt will detect this and falls back to the main thread for those specific objects.
		/// <br/>
		/// <seealso cref="Patches.Verse_PawnRenderNodeWorker_GetMaterialPropertyBlock"/>
		/// <seealso cref="Patches.Verse_PawnRenderNodeWorker_PreDraw"/>
		/// <seealso cref="Patches.Verse_PawnRenderTree_ParallelPreDraw"/>
		/// </summary>
		public OptimizationOption OptParallelMaterialUpdate { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ParallelMaterialUpdate",
			Desc = "YaOpt.Setting.Option.ParallelMaterialUpdate.Desc",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Optimizes the check for whether an object needs to regenerate render parameters.
		/// Vanilla checks this by recursively calling every child node in the render tree.
		/// Since 97% of render trees are shallow (less than or equal to 4 layers),
		/// this optimization inlines the check for the first 4 layers into a single function,
		/// avoiding virtual function call overhead.
		/// <br/>
		/// <seealso cref="Patches.Verse_PawnRenderNode_EnsureInitialized"/>
		/// <seealso cref="Patches.Verse_PawnRenderTree_ParallelPreDraw"/>
		/// </summary>
		public OptimizationOption OptFastRecacheRequested { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FastRecacheRequested",
			Desc = "YaOpt.Setting.Option.FastRecacheRequested.Desc",
			NoteCompatibility = "YaOpt.Setting.Option.FastRecacheRequested.Compatibility",
			Category = OptimizationCategory.Fps,
			FuncShow = (_) => ParallelPreDrawHelper.FastRecacheRequestedAvailable
		};

		/// <summary>
		/// Throttles the update frequency of the map mesh (terrain rendering).
		/// In vanilla, any change in snow or dust thickness triggers an immediate mesh update in the rendering frame,
		/// consuming significant resources during frequent changes (e.g., snowfall or melting).
		/// This optimization imposes a minimum real-time interval between updates.
		/// <br/>
		/// <seealso cref="Patches.MultiTargets_MapMeshDirty"/>
		/// </summary>
		public OptimizationOption OptMapMeshUpdateThrottle { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.MapMeshUpdateThrottle",
			Desc = "YaOpt.Setting.Option.MapMeshUpdateThrottle.Desc",
			Category = OptimizationCategory.Fps,
			FuncPostDraw = MapMeshUpdateThrottlePostDraw,
			FuncExposeData = (settings) =>
			{
				var interval = settings.MapMeshUpdateInterval;
				Scribe_Values.Look(ref interval, "OptMapMeshUpdateThrottle_Interval", 300);
				settings.MapMeshUpdateInterval = interval;
			}
		};

		public int MapMeshUpdateInterval
		{
			get => _mapMeshUpdateInterval;
			set => _mapMeshUpdateInterval = math.clamp(value, 100, 1000);
		}

		/// <summary>
		/// Fixes a bug where <see cref="PawnRenderNodeProperties.Worker"/>
		/// cache was not being used, causing slow initialization.
		/// <br/>
		/// <seealso cref="Patches.Verse_PawnRenderNodeProperties_Worker"/>
		/// </summary>
		public OptimizationOption OptPRNRWorker { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.PRNRWorker",
			Desc = "YaOpt.Setting.Option.PRNRWorker.Desc",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Optimizes the initialization of <see cref="Graphic_Multi"/> by caching the textures
		/// for the four cardinal directions.
		/// Vanilla constructs texture paths via string concatenation (e.g. "_north", "_south")
		/// and performs a content lookup for every new graphic instance.
		/// This optimization caches the resolved textures based on the base path,
		/// eliminating the overhead of string manipulation and repeated asset lookups.
		/// <br/>
		/// <seealso cref="Patches.Verse_Graphic_Multi_Init"/>
		/// </summary>
		public OptimizationOption OptGraphicTextureCache { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.GraphicTextureCache",
			Desc = "YaOpt.Setting.Option.GraphicTextureCache.Desc",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Optimizes <see cref="SilhouetteUtility.GetCachedSilhouetteData"/>.
		/// Vanilla uses a struct as a dictionary key for silhouette caching but fails to implement <see cref="IEquatable{T}"/>.
		/// This forces the runtime to use <see cref="ValueType.Equals(object)"/>, causing boxing and reflection overhead for every lookup.
		/// This optimization replaces the cache key with a custom implementation that properly implements <see cref="IEquatable{T}"/> to eliminate this overhead.
		/// <br/>
		/// <seealso cref="Patches.Verse_SilhouetteUtility_GetCachedSilhouetteData"/>
		/// <seealso cref="Patches.Verse_SilhouetteUtility_NotifyGraphicDirty"/>
		/// </summary>
		public OptimizationOption OptSilhouette { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.Silhouette",
			Desc = "YaOpt.Setting.Option.Silhouette.Desc",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Throttles the visibility check for toggleable tabs (e.g., the Mechanoid tab).
		/// The game queries whether each tab should be hidden every render frame.
		/// While vanilla tabs cache this check, some modded tabs perform complex queries every frame without caching.
		/// This optimization implements an upper-level cache that throttles visibility updates to once every 500ms (real-time).
		/// The cache is forcibly refreshed when pausing/unpausing or switching maps.
		/// <br/>
		/// <seealso cref="Patches.MultiTargets_ToggleTab"/>
		/// </summary>
		public OptimizationOption OptToggleTabCheck { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ToggleTabCheck",
			Desc = "YaOpt.Setting.Option.ToggleTabCheck.Desc",
			Category = OptimizationCategory.Fps
		};

		/// <summary>
		/// Accelerates the matrix computation step in render preparation using Burst.
		/// Vanilla intended to use Burst for this computationally expensive step but failed due to an implementation oversight.
		/// This optimization rewrites the Burst implementation to make it functional.
		/// <br/>
		/// <seealso cref="Patches.Verse_PawnRenderTree_TryGetMatrix"/>
		/// </summary>
		public OptimizationOption OptComputeMatrixBurst { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ComputeMatrixBurst",
			Desc = "YaOpt.Setting.Option.ComputeMatrixBurst.Desc",
			NoteStability = "YaOpt.Setting.Option.ComputeMatrixBurst.Stable",
			Category = OptimizationCategory.Fps,
			Flags = OptimizationFlag.RequireWin64 | OptimizationFlag.RequireBurst,
		};

		/// <summary>
		/// Accelerates <see cref="ThingWithComps.GetComp{T}"/>.
		/// Vanilla lookup is fast for existing components but very slow for missing ones,
		/// as it iterates through the entire component list.
		/// Unfortunately, checking if a component exists requires calling GetComp and waiting for a null result.
		/// This optimization rewrites the lookup mechanism to ensure fast failure for missing components.
		/// <br/>
		/// <seealso cref="Patches.Trampolines.Verse_ThingWithComps_GetComp"/>
		/// </summary>
		public OptimizationOption OptThingGetComp { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ThingGetComp",
			Desc = "YaOpt.Setting.Option.ThingGetComp.Desc",
			NoteStability = "YaOpt.Setting.Option.ThingGetComp.Stable",
			Category = OptimizationCategory.Tps,
			Flags = OptimizationFlag.RequireWin64,
		};

		/// <summary>
		/// Throttles pawn meditation ticks from every tick to once every 100 ticks.
		/// Each throttled tick provides 100x the effect.
		/// Meditation can be performance-intensive,
		/// especially for tribal psykers scanning for Anima grass every frame.
		/// Note: This has a minor gameplay impact. When a pawn stops meditating,
		/// any accrued progress between the 100-tick intervals is lost.
		/// <br/>
		/// <seealso cref="Patches.RimWorld_JobDriver_Meditate_MeditationTick"/>
		/// </summary>
		public OptimizationOption OptMeditationTick { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.MeditationTick",
			Desc = "YaOpt.Setting.Option.MeditationTick.Desc",
			NoteCompatibility = "YaOpt.Setting.Option.MeditationTick.Compatibility",
			Category = OptimizationCategory.Tps,
		};

		/// <summary>
		/// Extends the vanilla stat caching system to include <see cref="StatDefOf.ComfyTemperatureMin"/>,
		/// <see cref="StatDefOf.ComfyTemperatureMax"/>, and <see cref="StatDefOf.FilthRate"/>.
		/// Comfortable Temperature is updated every 10 ticks (and invalidated on apparel changes).
		/// Filth Rate is updated every 60 ticks.
		/// <br/>
		/// <seealso cref="Patches.MultiTargets_ComfortableTemperature"/>
		/// <seealso cref="Patches.MultiTargets_FilthRate"/>
		/// <seealso cref="Patches.RimWorld_Pawn_ApparelTracker_Notify_ApparelChanged"/>
		/// </summary>
		public OptimizationOption OptStatCache { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.StatCache",
			Desc = "YaOpt.Setting.Option.StatCache.Desc",
			Category = OptimizationCategory.Tps,
		};

		/// <summary>
		/// Introduces a limited multi-threaded handling for pawn updates.
		/// It implements a multi-threaded job interruption predictor that checks if the current job
		/// might fail or be interrupted by emergency jobs (e.g., fleeing enemies) in the current frame.
		/// If the prediction passes (no interruption), the main thread skips the redundant check.
		/// Otherwise, the main thread performs the standard check.
		/// <br/>
		/// <seealso cref="Patches.Verse_AI_JobDriver_DriverTick"/>
		/// <seealso cref="Patches.Verse_AI_Pawn_JobTracker_JobTrackerTickInterval"/>
		/// <seealso cref="Patches.Verse_TickList_BucketOf"/>
		/// <seealso cref="Patches.Verse_TickList_Constructor"/>
		/// <seealso cref="Patches.Verse_TickList_Tick"/>
		/// <seealso cref="Patches.Verse_TickManager_DoSingleTick"/>
		/// <seealso cref="YaOptGlobal.NeedThreadSafe"/>
		/// </summary>
		public OptimizationOption OptParallelPawnTick { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ParallelPawnTick",
			Desc = "YaOpt.Setting.Option.ParallelPawnTick.Desc",
			NoteStability = "YaOpt.Setting.Option.ParallelPawnTick.Stable",
			NoteCompatibility = "YaOpt.Setting.Option.ParallelPawnTick.Compatibility",
			Category = OptimizationCategory.Tps,
			Flags = OptimizationFlag.MultiplayerIncompatible,
		};

		/// <summary>
		/// Optimizes job giving by checking job priorities in parallel.
		/// Job giving is one of the most expensive operations. It iterates through a prioritized list of allowed jobs
		/// until a valid one is found. This optimization uses multiple threads to check this list.
		/// When a thread finds a valid job, it truncates the list (discarding lower priority jobs)
		/// and waits for threads checking higher priority jobs to finish.
		/// Finally, the valid job with the highest priority is selected.
		/// <br/>
		/// <seealso cref="Patches.RimWorld_JobGiver_Work_TryIssueJobPackage"/>
		/// <seealso cref="YaOptGlobal.NeedThreadSafe"/>
		/// </summary>
		public OptimizationOption OptParallelJobGiver { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ParallelJobGiver",
			Desc = "YaOpt.Setting.Option.ParallelJobGiver.Desc",
			NoteStability = "YaOpt.Setting.Option.ParallelJobGiver.Stable",
			NoteCompatibility = "YaOpt.Setting.Option.ParallelJobGiver.Compatibility",
			Category = OptimizationCategory.Tps,
			Flags = OptimizationFlag.MultiplayerIncompatible,
		};

		/// <summary>
		/// Optimizes map post-tick processing by running independent updates in parallel.
		/// Specifically, it targets Steady Environment Effects (e.g., rain/snow settlement) and Gas updates.
		/// These operations iterate over the map grid and are safe to execute on worker threads,
		/// reducing the main thread's workload during the map tick.
		/// <br/>
		/// <seealso cref="Patches.Verse_Map_MapPostTick"/>
		/// <seealso cref="Patches.Verse_TickManager_DoSingleTick"/>
		/// </summary>
		public OptimizationOption OptParallelPostMapTick { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ParallelPostMapTick",
			Desc = "YaOpt.Setting.Option.ParallelPostMapTick.Desc",
			Category = OptimizationCategory.Tps,
			Flags = OptimizationFlag.MultiplayerIncompatible,
		};

		/// <summary>
		/// <seealso cref="Patches.Verse_TickManager_DoSingleTick"/>
		/// </summary>
		public OptimizationOption OptFastCacheClear { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FastCacheClear",
			Desc = "YaOpt.Setting.Option.FastCacheClear.Desc",
			Category = OptimizationCategory.Tps,
		};

		/// <summary>
		/// <seealso cref="Patches.RimWorld_Ideo_IdeoTick"/>
		/// <seealso cref="Patches.RimWorld_Ideo_MemberWillingToDo"/>
		/// <seealso cref="Patches.RimWorld_Ideo_RecachePrecepts"/>
		/// </summary>
		public OptimizationOption OptIdeoCheck { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.IdeoCheck",
			Desc = "YaOpt.Setting.Option.IdeoCheck.Desc",
			Category = OptimizationCategory.Tps,
		};

		/// <summary>
		/// <seealso cref="Patches.Verse_DynamicDrawManager_DrawDynamicThings"/>
		/// <seealso cref="Patches.Verse_WindManager_WindManagerTick"/>
		/// </summary>
		public OptimizationOption OptWindUpdate { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.WindUpdate",
			Desc = "YaOpt.Setting.Option.WindUpdate.Desc",
			Category = OptimizationCategory.Tps,
		};

		/// <summary>
		/// <seealso cref="Patches.Trampolines.Verse_ContentFinder_Get"/>
		/// <seealso cref="Patches.Early.MultiTargets_PatchOperationMulti"/>
		/// <seealso cref="Patches.Early.MultiTargets_PatchOperationSingle"/>
		/// <seealso cref="Patches.Early.Verse_ModContentLoader_LoadTexture"/>
		/// </summary>
		public OptimizationOption OptLazyTextureLoad { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.LazyTextureLoad",
			Desc = "YaOpt.Setting.Option.LazyTextureLoad.Desc",
			Category = OptimizationCategory.Misc,
			Flags = OptimizationFlag.RequireWin64,
			FuncPostDraw = LazyTextureLoadPostDraw,
			FuncExposeData = (settings) =>
			{
				var ddsOnly = settings.LazyTextureLoadDdsOnly;
				Scribe_Values.Look(ref ddsOnly, "OptLazyTextureLoad_DdsOnly", true);
				settings.LazyTextureLoadDdsOnly = ddsOnly;
			}
		};

		[field: Unsaved]
		public bool LazyTextureLoadDdsOnly { get; set; } = true;

		/// <summary>
		/// <seealso cref="Patches.Early.MultiTargets_PatchOperationMulti"/>
		/// <seealso cref="Patches.Early.MultiTargets_PatchOperationSingle"/>
		/// <seealso cref="Patches.Early.Verse_LoadedModManager_ApplyPatches"/>
		/// </summary>
		public OptimizationOption OptFastPatchOperation { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FastPatchOperation",
			Desc = "YaOpt.Setting.Option.FastPatchOperation.Desc",
			Category = OptimizationCategory.Misc
		};

		/// <summary>
		/// <seealso cref="Patches.Early.Verse_DefInjectionPackage_InjectIntoDefs"/>
		/// <seealso cref="Patches.Early.Verse_DefInjectionPackage_SetDefFieldAtPath"/>
		/// </summary>
		public OptimizationOption OptFastTranslationInjection { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FastTranslationInjection",
			Desc = "YaOpt.Setting.Option.FastTranslationInjection.Desc",
			Category = OptimizationCategory.Misc
		};

		/// <summary>
		/// <seealso cref="Patches.Early.HarmonyLib_AccessTools_TypeByName"/>
		/// <seealso cref="Patches.Early.Verse_DefInjectionPackage_SetDefFieldAtPath"/>
		/// </summary>
		public OptimizationOption OptRuntimeInfoCache { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.RuntimeInfoCache",
			Desc = "YaOpt.Setting.Option.RuntimeInfoCache.Desc",
			Category = OptimizationCategory.Misc
		};

		[Unsaved]
		private readonly List<OptimizationOption> _allOptimizations;

		[Unsaved]
		private SettingTab _selectedTab = SettingTab.Main;

		public IReadOnlyList<OptimizationOption> AllOptimizations => _allOptimizations.AsReadOnly();

		[Unsaved]
		private Vector2 _optionScrollPos = Vector2.zero;

		[Unsaved]
		private Vector2 _descTextScrollPos = Vector2.zero;

		[Unsaved]
		private float _optionViewHeight;

		[Unsaved]
		private OptimizationCategory _categoryFilter = OptimizationCategory.Any;

		[Unsaved]
		private OptimizationOption _lastMouseOverOption = null;

		[Unsaved]
		private Window _lastWindow = null;

		[Unsaved]
		private string _showingDesc = string.Empty;

		[Unsaved]
		private bool _checkOptionChanged = false;

		[Unsaved]
		private int _mapMeshUpdateInterval = 300;

		public bool DebugLogging
		{
			set => DebugOutput.Enabled = value;
			get => DebugOutput.Enabled;
		}

		public YaOptSettings()
		{
			_allOptimizations = new List<OptimizationOption>();
			var props = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var propertyInfo in props)
			{
				if (typeof(OptimizationOption).IsAssignableFrom(propertyInfo.PropertyType))
				{
					if (!(propertyInfo.GetValue(this) is OptimizationOption option))
					{
						// Should never happen
						continue;
					}
					_allOptimizations.Add(option);
					if (string.IsNullOrWhiteSpace(option.SettingId))
						option.SettingId = propertyInfo.Name;
				}
			}

			foreach (var subMod in YaOptGlobal.SubMods)
			{
				try
				{
					_allOptimizations.AddRange(subMod.OnCreateSettings());
				}
				catch (Exception ex)
				{
					YaOptMod.Error($"Failed to run OnCreateSettings for {subMod.GetType().FullName}\n{ex}");
				}
			}

			_allOptimizations.Sort((a, b) =>
			{
				var i = a.Category.CompareTo(b.Category);
				if (i == 0)
				{
					// Don't use translated text, so that the layout will be the same in any language
					var subCatA = !string.IsNullOrWhiteSpace(a.SubCategory) ? a.SubCategory : null;
					var subCatB = !string.IsNullOrWhiteSpace(b.SubCategory) ? b.SubCategory : null;
					i = string.Compare(subCatA, subCatB, StringComparison.Ordinal);
					if (i == 0)
					{
						i = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
					}
				}
				return i;
			});
		}

		public override void ExposeData()
		{
			base.ExposeData();
			if (YaOptGlobal.IsLibraryLoaded && Scribe.mode == LoadSaveMode.Saving)
			{
				foreach (var option in _allOptimizations)
				{
					option.Validate(false, false, out _);
				}
			}
			foreach (var option in _allOptimizations)
			{
				if ((option.Flags & OptimizationFlag.DontSave) == 0)
				{
					Scribe_Values.Look(ref option._enabled, option.SettingId, option.Default);
					if (option.FuncExposeData != null)
						option.FuncExposeData(this);
				}
			}
			if (YaOptGlobal.IsLibraryLoaded && Scribe.mode == LoadSaveMode.LoadingVars)
			{
				foreach (var option in _allOptimizations)
				{
					option.Validate(false, false, out _);
				}
			}
		}

		public static string GetCategoryText(OptimizationCategory category)
		{
			switch (category)
			{
				case OptimizationCategory.Hidden:
				case OptimizationCategory.Main:
					return string.Empty;
				case OptimizationCategory.Fps: return "YaOpt.Setting.Category.Fps".Translate();
				case OptimizationCategory.Tps: return "YaOpt.Setting.Category.Tps".Translate();
				case OptimizationCategory.Misc: return "YaOpt.Setting.Category.Misc".Translate();
				case OptimizationCategory.Any: return string.Empty;
				default:
					throw new ArgumentOutOfRangeException(nameof(category), category, null);
			}
		}

		public void ValidateOptions(bool silent)
		{
			foreach (var option in _allOptimizations)
			{
				option.Validate(false, silent, out _);
			}
		}

		public void DoSettingsWindowContents(Rect inRect)
		{
			var currentWindow = Find.WindowStack.currentlyDrawnWindow;
			if (_lastWindow != currentWindow)
			{
				_lastWindow = currentWindow;
				_optionScrollPos = Vector2.zero;
				_descTextScrollPos = Vector2.zero;
				_lastMouseOverOption = null;
				_showingDesc = string.Empty;
			}

			var tabHeader = inRect;
			tabHeader.y += 35f;

			var tabBody = tabHeader;
			tabBody.height -= 40f;
			Widgets.DrawMenuSection(tabBody);

			var list = new List<TabRecord>
			{
				new TabRecord("YaOpt.Setting.Tab.Main".Translate(), delegate
				{
					_optionScrollPos = Vector2.zero;
					_descTextScrollPos = Vector2.zero;
					_lastMouseOverOption = null;
					_showingDesc = string.Empty;
					_categoryFilter = OptimizationCategory.Any;
					_selectedTab = SettingTab.Main;
				}, _selectedTab == SettingTab.Main),
				new TabRecord("YaOpt.Setting.Tab.Fps".Translate(), delegate
				{
					_optionScrollPos = Vector2.zero;
					_descTextScrollPos = Vector2.zero;
					_lastMouseOverOption = null;
					_showingDesc = string.Empty;
					_categoryFilter = OptimizationCategory.Fps;
					_selectedTab = SettingTab.Fps;
				}, _selectedTab == SettingTab.Fps),
				new TabRecord("YaOpt.Setting.Tab.Tps".Translate(), delegate
				{
					_optionScrollPos = Vector2.zero;
					_descTextScrollPos = Vector2.zero;
					_lastMouseOverOption = null;
					_showingDesc = string.Empty;
					_categoryFilter = OptimizationCategory.Tps;
					_selectedTab = SettingTab.Tps;
				}, _selectedTab == SettingTab.Tps),
				new TabRecord("YaOpt.Setting.Tab.Misc".Translate(), delegate
				{
					_optionScrollPos = Vector2.zero;
					_descTextScrollPos = Vector2.zero;
					_lastMouseOverOption = null;
					_showingDesc = string.Empty;
					_categoryFilter = OptimizationCategory.Misc;
					_selectedTab = SettingTab.Misc;
				}, _selectedTab == SettingTab.Misc),
			};
			TabDrawer.DrawTabs(tabHeader, list);
			DrawPage(tabBody.ContractedBy(10));

			if (_checkOptionChanged)
			{
				if (!YaOptGlobal.AnyOptionChanged())
				{
					_checkOptionChanged = false;
				}
				else
				{
					var messageRect = new Rect(inRect.x + 5, inRect.yMax + 5, inRect.width * 0.4f, 50);
					Text.Font = GameFont.Tiny;
					Widgets.Label(messageRect, "YaOpt.Setting.Message.RequireReload".Translate());
					Text.Font = GameFont.Small;
				}
			}
		}

		private void DrawPage(Rect inRect)
		{
			inRect.SplitVertically(inRect.width * 0.6f, out var leftRect, out var rightRect);

			Widgets.DrawLineVertical(leftRect.xMax - 5f, leftRect.yMin, leftRect.height);
			leftRect = leftRect.ContractedBy(0, 5);
			leftRect.width -= 25;
			var viewRect = new Rect(0, 0, leftRect.width - 25, _optionViewHeight);
			Widgets.BeginScrollView(leftRect, ref _optionScrollPos, viewRect, true);
			var listing = new Listing_Standard
			{
				verticalSpacing = 4f,
				maxOneColumn = true,
				ColumnWidth = viewRect.width * 0.93f
			};
			listing.Begin(viewRect);
			var lastCategory = string.Empty;
			var lastSubCategory = string.Empty;
			switch (_selectedTab)
			{
				case SettingTab.Main:
					lastCategory = GetCategoryText(OptimizationCategory.Main);
					break;
				case SettingTab.Fps:
					lastCategory = GetCategoryText(OptimizationCategory.Fps);
					break;
				case SettingTab.Tps:
					lastCategory = GetCategoryText(OptimizationCategory.Tps);
					break;
				case SettingTab.Misc:
					lastCategory = GetCategoryText(OptimizationCategory.Misc);
					break;
			}
			foreach (var option in _allOptimizations)
			{
				if ((_categoryFilter & option.Category) > 0 && option.ShouldShow(this))
				{
					var cateText = GetCategoryText(option.Category);
					if (lastCategory != cateText)
					{
						lastCategory = cateText;
						lastSubCategory = string.Empty;
						if (!string.IsNullOrWhiteSpace(cateText))
						{
							Text.Font = GameFont.Medium;
							listing.Label(cateText);
							Text.Font = GameFont.Small;
						}
					}

					var subCateText = !string.IsNullOrWhiteSpace(option.SubCategory) ?
						option.SubCategory.Translate().ToString() :
						string.Empty;
					if (lastSubCategory != subCateText)
					{
						lastSubCategory = subCateText;
						if (!string.IsNullOrWhiteSpace(subCateText))
						{
							listing.Label(subCateText);
						}
					}
					DrawOption(listing, option);
				}
			}
			listing.End();
			Widgets.EndScrollView();
			if (Event.current.type == EventType.Layout)
			{
				_optionViewHeight = listing.CurHeight;
			}

			Rect drawRect;
#if DEBUG
			rightRect.SplitHorizontally(rightRect.height - 35f, out rightRect, out drawRect);
			Widgets.ButtonText(drawRect.ContractedBy(30, 2.5f), "YaOpt.Setting.Button.ShowDebugMenu".Translate());
			string btnTextDisable;
			string btnTextEnable;
#endif
			OptimizationCategory category;
			switch (_selectedTab)
			{
				case SettingTab.Main:
					btnTextDisable = "YaOpt.Setting.Button.DisableAll";
					btnTextEnable = "YaOpt.Setting.Button.EnableAll";
					category = OptimizationCategory.Any;
					break;
				case SettingTab.Fps:
					btnTextDisable = "YaOpt.Setting.Button.DisableAllFps";
					btnTextEnable = "YaOpt.Setting.Button.EnableAllFps";
					category = OptimizationCategory.Fps;
					break;
				case SettingTab.Tps:
					btnTextDisable = "YaOpt.Setting.Button.DisableAllTps";
					btnTextEnable = "YaOpt.Setting.Button.EnableAllTps";
					category = OptimizationCategory.Tps;
					break;
				case SettingTab.Misc:
					btnTextDisable = "YaOpt.Setting.Button.DisableAllMisc";
					btnTextEnable = "YaOpt.Setting.Button.EnableAllMisc";
					category = OptimizationCategory.Misc;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			rightRect.SplitHorizontally(rightRect.height - 35f, out rightRect, out drawRect);
			if (Widgets.ButtonText(drawRect.ContractedBy(30, 2.5f), btnTextDisable.Translate()))
			{
				SetAllOption(false, category);
			}
			rightRect.SplitHorizontally(rightRect.height - 35f, out rightRect, out drawRect);
			if (Widgets.ButtonText(drawRect.ContractedBy(30, 2.5f), btnTextEnable.Translate()))
			{
				SetAllOption(true, category);
			}

			rightRect = rightRect.ContractedBy(10);
			Widgets.LabelScrollable(rightRect, _showingDesc, ref _descTextScrollPos, true, false, true);
		}

		private void DrawOption(Listing_Standard listing, OptimizationOption option)
		{
			var label = option.Name.Translate().ToString();
			var hasNoteS = !string.IsNullOrWhiteSpace(option.NoteStability);
			var hasNoteC = !string.IsNullOrWhiteSpace(option.NoteCompatibility);
			if (hasNoteS && hasNoteC)
			{
				label = string.Concat(label, " <color=#FF4040>[S]</color><color=#DEB0D0>[C]</color>");
			}
			else if (hasNoteS)
			{
				label = string.Concat(label, " <color=#FF4040>[S]</color>");
			}
			else if (hasNoteC)
			{
				label = string.Concat(label, " <color=#DEB0D0>[C]</color>");
			}
			var enabled = option._enabled;
			var disabledByDef = CompatibilityDef.CachedBannedOptimizations.Contains(option.SettingId);
			DrawCheckboxLabeled(listing, label, enabled, disabledByDef, out var mouseOver, out var result);
			if (mouseOver && _lastMouseOverOption != option)
			{
				MouseOverOption(option);
			}
			if (result != enabled && !disabledByDef)
			{
				option._enabled = result;
				if (!option.Validate(false, true, out var reason))
				{
					Messages.Message("YaOpt.Setting.InvalidOption".Translate().ToString() + reason,
						null, MessageTypeDefOf.RejectInput, false);
				}
				CheckIfOptionChanged();
			}
			if (option.FuncPostDraw != null)
				option.FuncPostDraw(this, listing, option);
			listing.Gap(listing.verticalSpacing);
		}

		private static void DrawCheckboxLabeled(Listing_Standard listing, string label,
			bool isChecked, bool isDisabled, out bool mouseOver, out bool result, float widthOffset = 0)
		{
			mouseOver = false;
			result = false;
			Rect rect = listing.GetRect(Text.CalcHeight(label, listing.ColumnWidth));
			rect.width += widthOffset;
			//rect.width = Math.Min(rect.width + 24f, listing.ColumnWidth);
			Rect? boundingRectCached = listing.BoundingRectCached;
			if (boundingRectCached.HasValue)
			{
				ref Rect local = ref rect;
				Rect other = boundingRectCached.Value;
				if (!local.Overlaps(other))
				{
					listing.Gap(listing.verticalSpacing);
					return;
				}
			}
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
				mouseOver = true;
			}
			var enabled = isChecked;
			Widgets.CheckboxLabeled(rect, label, ref enabled, isDisabled);
			result = enabled;
		}

		private void MouseOverOption(OptimizationOption option)
		{
			_lastMouseOverOption = option;
			var sb = new StringBuilder();

			if (CompatibilityDef.CachedBannedOptimizations.Contains(option.SettingId))
			{
				sb.Append("<color=#FF2020>")
					.Append("YaOpt.Setting.Note.Banned".Translate(
						CompatibilityDef.CachedBannedBy[option.SettingId]))
					.AppendLine("</color>");
			}

			sb.AppendLine(option.Desc.Translate());

			if (!string.IsNullOrWhiteSpace(option.NoteStability))
			{
				sb.Append("\n\n").Append("<color=#FF4040>").Append("YaOpt.Setting.Note.Stability".Translate()).Append("\n")
					.Append(option.NoteStability.Translate()).Append("</color>");
			}

			if (!string.IsNullOrWhiteSpace(option.NoteCompatibility))
			{
				sb.Append("\n\n").Append("<color=#DEB0D0>").Append("YaOpt.Setting.Note.Compatibility".Translate()).Append("\n")
					.Append(option.NoteCompatibility.Translate()).Append("</color>");
			}
			_showingDesc = sb.ToString();
		}

		private static void MapMeshUpdateThrottlePostDraw(YaOptSettings settings,
			Listing_Standard listing, OptimizationOption option)
		{
			if (option.Enabled)
			{
				listing.Indent();
				var rect = listing.GetRect(30);
				listing.Gap(-30);
				var result = (int)listing.SliderLabeled(
					"YaOpt.Setting.Option.MapMeshUpdateThrottle.UpdateInterval".Translate(settings.MapMeshUpdateInterval),
					settings.MapMeshUpdateInterval, 100, 1000);
				settings.MapMeshUpdateInterval = result / 100 * 100;
				listing.Outdent();
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
					if (settings._lastMouseOverOption != null)
					{
						settings._lastMouseOverOption = null;
						settings._showingDesc = "YaOpt.Setting.Option.MapMeshUpdateThrottle.UpdateInterval.Desc".Translate();
					}
				}
			}
		}

		private static void LazyTextureLoadPostDraw(YaOptSettings settings,
			Listing_Standard listing, OptimizationOption option)
		{
			if (option.Enabled)
			{
				listing.Gap(listing.verticalSpacing);
				listing.Indent();
				var ddsOnly = settings.LazyTextureLoadDdsOnly;
				DrawCheckboxLabeled(listing, "YaOpt.Setting.Option.LazyTextureLoad.DdsOnly".Translate(),
					ddsOnly, false, out var mouseOver, out var result, -12);
				if (mouseOver)
				{
					settings._lastMouseOverOption = null;
					settings._showingDesc = "YaOpt.Setting.Option.LazyTextureLoad.DdsOnly.Desc".Translate();
				}
				if (ddsOnly != result)
					settings.LazyTextureLoadDdsOnly = result;
				listing.Outdent();
				listing.Gap(listing.verticalSpacing);
			}
		}

		private void SetAllOption(bool enable, OptimizationCategory category)
		{
			var filter = enable ? OptimizationFlag.IgnoreEnableAll : OptimizationFlag.IgnoreDisableAll;
			foreach (var optimization in _allOptimizations)
			{
				if ((optimization.Category & category) > 0 && (optimization.Flags & filter) == 0)
				{
					optimization.Enabled = enable;
				}
			}
			CheckIfOptionChanged();
		}

		private void CheckIfOptionChanged()
		{
			if (YaOptGlobal.AnyOptionChanged())
			{
				_checkOptionChanged = true;
			}
		}
	}
}