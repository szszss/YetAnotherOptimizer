using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using YaOpt.Settings;

namespace YaOpt
{
	public class YaOptSettings : ModSettings
	{
		public OptimizationOption DebugOutput { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.DebugOutput",
			Desc = "YaOpt.Setting.Option.DebugOutput.Desc",
			Category = OptimizationCategory.Main,
			Flags = OptimizationFlags.IgnoreEnableAll | OptimizationFlags.IgnoreDisableAll | OptimizationFlags.NoSnapshot,
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
			FuncPostDraw = SettingsPanel.MapMeshUpdateThrottlePostDraw,
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
			Flags = OptimizationFlags.RequireWin64 | OptimizationFlags.RequireBurst,
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
			Flags = OptimizationFlags.RequireWin64,
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
		/// Comfortable Temperature is updated every 20 ticks (and invalidated on apparel changes).
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
		/// Introduces a restricted multi-threaded handling for pawn updates.
		/// It implements a multi-threaded job interruption predictor that checks if the current job
		/// might fail or be interrupted by emergency jobs (e.g. fleeing enemies) in the current frame.
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
			Flags = OptimizationFlags.MultiplayerIncompatible,
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
			Flags = OptimizationFlags.MultiplayerIncompatible,
		};

		/// <summary>
		/// Optimizes map post-tick processing by running independent updates in parallel.
		/// Supports steady environment effects and gas updates.
		/// <br/>
		/// <seealso cref="Patches.Verse_Map_MapPostTick"/>
		/// <seealso cref="Patches.Verse_TickManager_DoSingleTick"/>
		/// </summary>
		public OptimizationOption OptParallelPostMapTick { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.ParallelPostMapTick",
			Desc = "YaOpt.Setting.Option.ParallelPostMapTick.Desc",
			Category = OptimizationCategory.Tps,
			Flags = OptimizationFlags.MultiplayerIncompatible,
		};

		/// <summary>
		/// Optimizes the check for whether an Ideoligion allows a willing action.
		/// Vanilla iterates through all precepts to make this determination.
		/// This optimization creates a cache of precepts that impose restrictions whenever precepts are updated.
		/// When checking permissions, it only iterates through these cached restrictive precepts, ignoring irrelevant ones.
		/// <br/>
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
		/// Optimizes plant sway effect updates.
		/// Vanilla updates wind strength parameters for all plant materials every logic tick, which is unnecessary.
		/// This optimization restricts these updates to render frames only.
		/// </summary>
		/// <seealso cref="Patches.Verse_DynamicDrawManager_DrawDynamicThings"/>
		/// <seealso cref="Patches.Verse_WindManager_WindManagerTick"/>
		public OptimizationOption OptWindUpdate { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.WindUpdate",
			Desc = "YaOpt.Setting.Option.WindUpdate.Desc",
			Category = OptimizationCategory.Tps,
		};

		/// <summary>
		/// 
		/// </summary>
		/// <seealso cref="Patches.Verse_ListerThings_Add"/>
		public OptimizationOption OptFastListerRemove { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FastListerRemove",
			Desc = "YaOpt.Setting.Option.FastListerRemove.Desc",
			Category = OptimizationCategory.Tps,
		};

		/// <summary>
		/// Delays texture loading until they are first requested.
		/// Vanilla loads all mod textures into VRAM at startup.
		/// This optimization reduces startup time and initial VRAM usage,
		/// but may cause stutters when textures are loaded during gameplay.
		/// Note: Texture unloading is not implemented, so VRAM usage may increase over time.
		/// <br/>
		/// <seealso cref="Patches.Trampolines.Verse_ContentFinder_Get"/>
		/// <seealso cref="Patches.Early.MultiTargets_PatchOperationMulti"/>
		/// <seealso cref="Patches.Early.MultiTargets_PatchOperationSingle"/>
		/// <seealso cref="Patches.Early.Verse_ModContentLoader_LoadTexture"/>
		/// </summary>
		public OptimizationOption OptLazyTextureLoad { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.LazyTextureLoad",
			Desc = "YaOpt.Setting.Option.LazyTextureLoad.Desc",
			NoteStability = "YaOpt.Setting.Option.LazyTextureLoad.Stable",
			Category = OptimizationCategory.Misc,
			Flags = OptimizationFlags.RequireWin64,
			FuncPostDraw = SettingsPanel.LazyTextureLoadPostDraw,
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
		/// Optimizes XML patch operations by simplifying common XPath expressions.
		/// If an XPath expression follows a simple pattern like <c>Defs/DefType[defName="DefName"]</c>,
		/// this optimization replaces the complex XPath evaluation with a faster, direct node lookup.
		/// This significantly improves game startup time, especially with many mods.
		/// <br/>
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
		/// Optimizes the translation injection process during game startup.
		/// When injecting translation data into XML Defs, the game checks for errors (e.g. duplicate injections).
		/// Vanilla uses an O(N^2) nested loop for this check, which is very slow with large numbers of Defs.
		/// This optimization replaces the inner loop with a hash lookup, reducing the complexity to O(N).
		/// This speeds up game startup, especially for non-English languages.
		/// <br/>
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
		/// Caches type information at startup to accelerate Harmony type retrieval.
		/// This speeds up Harmony patch processing for some mods, improving game startup time in modded environments.
		/// It has no effect in a vanilla environment.
		/// <br/>
		/// <seealso cref="Patches.Early.HarmonyLib_AccessTools_TypeByName"/>
		/// <seealso cref="Patches.Early.System_Reflection_RuntimeAssembly_FullName"/>
		/// </summary>
		public OptimizationOption OptRuntimeInfoCache { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.RuntimeInfoCache",
			Desc = "YaOpt.Setting.Option.RuntimeInfoCache.Desc",
			Category = OptimizationCategory.Misc
		};

		/// <summary>
		/// 
		/// </summary>
		/// <seealso cref="Patches.Early.MultiTargets_CalcRectsForAtlas"/>
		public OptimizationOption OptFixTextureAtlas { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FixTextureAtlas",
			Desc = "YaOpt.Setting.Option.FixTextureAtlas.Desc",
			Category = OptimizationCategory.Misc
		};

		[Unsaved]
		private readonly List<OptimizationOption> _allOptimizations;

		[Unsaved]
		private readonly SettingsPanel _settingsPanel;

		[Unsaved]
		private int _mapMeshUpdateInterval = 300;

		public IReadOnlyList<OptimizationOption> AllOptimizations => _allOptimizations.AsReadOnly();

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

			_settingsPanel = new SettingsPanel(this);
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
				if ((option.Flags & OptimizationFlags.DontSave) == 0)
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

		public void ValidateOptions(bool silent)
		{
			foreach (var option in _allOptimizations)
			{
				option.Validate(false, silent, out _);
			}
		}

		public void DoSettingsWindowContents(Rect inRect)
		{
			_settingsPanel.Draw(inRect);
		}
	}
}
