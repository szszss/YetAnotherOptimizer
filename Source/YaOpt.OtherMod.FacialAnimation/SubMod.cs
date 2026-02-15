using FacialAnimation;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;
using YaOpt.OtherMod.FacialAnimation.Helpers;
using static YaOpt.YaOptSettings;
using HeadTypeDef = FacialAnimation.HeadTypeDef;

namespace YaOpt.OtherMod.FacialAnimation
{
	internal class SubMod : YaOptSubMod
	{
		/// <summary>
		/// Moves facial animation updates from the main thread to the parallel render preparation phase.
		/// <br/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_FacialAnimationControllerComp_GatherPawnParam"/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_NLFacialAnimationMasterNodeWorker_PreDraw"/>
		/// <seealso cref="FacialAnimation.Patches.RimWorld_PortraitsCache_SetDirty"/>
		/// <seealso cref="FacialAnimation.Patches.Verse_Corpse_DynamicDrawPhaseAt"/>
		/// <seealso cref="FacialAnimation.Patches.Verse_Pawn_DynamicDrawPhaseAt"/>
		/// </summary>
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
		/// Rewrites LINQ expressions into GC-friendly loops.
		/// LINQ generates significant GC overhead in Unity.
		/// <br/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_AnimationFrameAccumulator_AccumResultFrameAndClear"/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_AnimationFrameAccumulator_UpdateAninmation"/>
		/// </summary>
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
		/// Accelerates facial animation processing using Burst.
		/// <br/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_AnimationFrameAccumulator_AccumResultFrameAndClear"/>
		/// </summary>
		public static OptimizationOption OptFADeLinqBurst { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.FADeLinqBurst",
			Desc = "YaOpt.Setting.Option.FADeLinqBurst.Desc",
			NoteStability = "YaOpt.Setting.Option.OptFADeLinqBurst.Stable",
			SettingId = "OptFADeLinqBurst",
			RequiredMod = "Nals.FacialAnimation",
			SubCategory = "YaOpt.Setting.SubCategory.FacialAnimation",
			Category = OptimizationCategory.Fps,
			Flags = OptimizationFlag.RequireWin64 | OptimizationFlag.RequireBurst,
			RequiredOption = OptFADeLinq,
			FuncShow = (_) => OptFADeLinq.Enabled
		};

		/// <summary>
		/// Caches the list of facial animations to avoid frequent reconstruction.
		/// <br/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_FacialAnimationControllerComp_InitializeIfNeed"/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_FacialAnimationControllerComp_UpdateAnimation"/>
		/// </summary>
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
		/// Caches some information used during pawn generation to reduce stuttering when pawns spawn.
		/// <br/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_FAHelper_CreateAnimationDict"/>
		/// </summary>
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
		/// Optimizes the loading of facial textures by the Facial Animation mod,
		/// making it use the texture caching system provided by this mod,
		/// reducing stuttering during pawn generation.
		/// <br/>
		/// <seealso cref="FacialAnimation.Patches.FacialAnimation_GraphicHelper_CheckTexPathExist"/>
		/// </summary>
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