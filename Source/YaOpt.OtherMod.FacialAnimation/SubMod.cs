using FacialAnimation;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;
using YaOpt.OtherMod.FacialAnimation.Helpers;
using YaOpt.Settings;
using HeadTypeDef = FacialAnimation.HeadTypeDef;

namespace YaOpt.OtherMod.FacialAnimation
{
	/// <summary>
	/// Compatibility module for the Facial Animation mod. Provides parallel updates and GC optimizations.
	/// </summary>
	internal class SubMod : YaOptSubMod
	{
		/// <summary>
		/// Moves facial animation updates from the main thread to the parallel render preparation phase.
		/// </summary>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_FacialAnimationControllerComp_GatherPawnParam"/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_NLFacialAnimationMasterNodeWorker_PreDraw"/>
		/// <seealso cref="FacialAnimation.Patches.RimWorld_PortraitsCache_SetDirty"/>
		/// <seealso cref="FacialAnimation.Patches.Verse_Corpse_DynamicDrawPhaseAt"/>
		/// <seealso cref="FacialAnimation.Patches.Verse_Pawn_DynamicDrawPhaseAt"/>
		/// <seealso cref="FacialAnimation.Patches.ThreadSafe.FacialAnimation_FaceAnimationDef_GetCachedAnimationFrames"/>
		/// <seealso cref="FacialAnimation.Patches.ThreadSafe.FacialAnimation_FaceAnimationDef_GetSequentialAnimationFrames"/>
		public static OptimizationOption OptFAParallelUpdate { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FAParallelUpdate",
			Desc = "YaOpt.Setting.Option.FAParallelUpdate.Desc",
			SettingId = "OptFAParallelUpdate",
			RequiredMod = "Nals.FacialAnimation",
			SubCategory = "YaOpt.Setting.SubCategory.FacialAnimation",
			Category = OptimizationCategory.Fps,
		};

		/// <summary>
		/// Rewrites LINQ expressions into GC-friendly loops to reduce garbage collection overhead.
		/// </summary>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_AnimationFrameAccumulator_AccumResultFrameAndClear"/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_AnimationFrameAccumulator_UpdateAninmation"/>
		public static OptimizationOption OptFADeLinq { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FADeLinq",
			Desc = "YaOpt.Setting.Option.FADeLinq.Desc",
			SettingId = "OptFADeLinq",
			RequiredMod = "Nals.FacialAnimation",
			SubCategory = "YaOpt.Setting.SubCategory.FacialAnimation",
			Category = OptimizationCategory.Fps,
		};

		/// <summary>
		/// Accelerates facial animation frame accumulation using Burst SIMD instructions.
		/// </summary>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_AnimationFrameAccumulator_AccumResultFrameAndClear"/>
		public static OptimizationOption OptFADeLinqBurst { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FADeLinqBurst",
			Desc = "YaOpt.Setting.Option.FADeLinqBurst.Desc",
			SettingId = "OptFADeLinqBurst",
			RequiredMod = "Nals.FacialAnimation",
			SubCategory = "YaOpt.Setting.SubCategory.FacialAnimation",
			Category = OptimizationCategory.Fps,
			Flags = OptimizationFlags.RequireBurst,
			RequiredOption = OptFADeLinq,
			FuncShow = (_) => OptFADeLinq.Enabled
		};

		/// <summary>
		/// Caches facial animation lists to avoid frequent reconstruction during updates.
		/// </summary>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_FacialAnimationControllerComp_InitializeIfNeed"/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_FacialAnimationControllerComp_UpdateAnimation"/>
		public static OptimizationOption OptFAAnimCache { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FAAnimCache",
			Desc = "YaOpt.Setting.Option.FAAnimCache.Desc",
			SettingId = "OptFAAnimCache",
			RequiredMod = "Nals.FacialAnimation",
			SubCategory = "YaOpt.Setting.SubCategory.FacialAnimation",
			Category = OptimizationCategory.Fps,
		};

		/// <summary>
		/// Caches information used during pawn generation to reduce spawn stuttering.
		/// </summary>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_FAHelper_CreateAnimationDict"/>
		public static OptimizationOption OptFAPawnSpawn { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FAPawnSpawn",
			Desc = "YaOpt.Setting.Option.FAPawnSpawn.Desc",
			SettingId = "OptFAPawnSpawn",
			RequiredMod = "Nals.FacialAnimation",
			SubCategory = "YaOpt.Setting.SubCategory.FacialAnimation",
			Category = OptimizationCategory.Misc,
		};

		/// <summary>
		/// Integrates Facial Animation texture loading with YaOpt's texture cache.
		/// </summary>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_GraphicHelper_CheckTexPathExist"/>
		public static OptimizationOption OptFATextureCache { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FATextureCache",
			Desc = "YaOpt.Setting.Option.FATextureCache.Desc",
			SettingId = "OptFATextureCache",
			RequiredMod = "Nals.FacialAnimation",
			SubCategory = "YaOpt.Setting.SubCategory.FacialAnimation",
			Category = OptimizationCategory.Misc,
		};

		public override void OnPostInit()
		{
			try
			{
				ToHashSet<BrowControllerComp, BrowTypeDef, BrowShapeDef>();
				ToHashSet<EmotionControllerComp, EmotionTypeDef, EmotionShapeDef>();
				ToHashSet<LidOptionControllerComp, LidOptionTypeDef, LidOptionShapeDef>();
				ToHashSet<EyeballControllerComp, EyeballTypeDef, EyeballShapeDef>();
				ToHashSet<HeadControllerComp, HeadTypeDef, HeadShapeDef>();
				ToHashSet<LidControllerComp, LidTypeDef, LidShapeDef>();
				ToHashSet<MouthControllerComp, MouthTypeDef, MouthShapeDef>();
				ToHashSet<SkinControllerComp, SkinTypeDef, SkinShapeDef>();
			}
			catch (Exception ex)
			{
				YaOptMod.Error($"Failed to optimize faceShapeList\n{ex}");
			}
		}

		public override IEnumerable<OptimizationOption> OnCreateSettings()
		{
			yield return OptFAParallelUpdate;
			yield return OptFADeLinq;
			yield return OptFADeLinqBurst;
			yield return OptFAAnimCache;
			yield return OptFAPawnSpawn;
			yield return OptFATextureCache;
		}

		public override bool OnPatch(Harmony harmony)
		{
			var assembly = Assembly.GetExecutingAssembly();
			ParallelUpdateHelper.Enabled = OptFAParallelUpdate.Enabled;
			return harmony.TryPatchAll(assembly);
		}

		public override void OnUnpatch(Harmony harmony)
		{
			harmony.UnpatchAll(harmony.Id);
			ParallelUpdateHelper.Enabled = false;
		}

		private static void ToHashSet<K, T, S>()
			where K : ControllerBaseComp<T, S>
			where T : FaceTypeDef, new()
			where S : Def, IFaceShapeDef, new()
		{
			var fieldInfo = AccessTools.Field(typeof(K), "faceShapeList");
			if (fieldInfo == null)
			{
				YaOptMod.Error($"Cannot find faceShapeList from {typeof(K).FullName}");
				return;
			}
			var enumerable = fieldInfo.GetValue(null) as IEnumerable<S>;
			if (enumerable == null)
			{
				YaOptMod.Error($"faceShapeList of {typeof(K).FullName} is null");
				return;
			}
			var list = new List<S>(enumerable);
			enumerable = list.Count > 4 ? (IEnumerable<S>)new HashSet<S>(list) : list;
			fieldInfo.SetValue(null, enumerable);
		}
	}
}