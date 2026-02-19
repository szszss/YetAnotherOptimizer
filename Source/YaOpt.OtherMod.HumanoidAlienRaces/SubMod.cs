using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers;
using YaOpt.Settings;

namespace YaOpt.OtherMod.HumanoidAlienRaces
{
	/// <summary>
	/// Compatibility module for Humanoid Alien Races mod. Provides LINQ and texture optimizations.
	/// </summary>
	internal class SubMod : YaOptSubMod
	{
		/// <summary>
		/// Rewrites LINQ expressions into GC-friendly loops to reduce garbage collection overhead.
		/// </summary>
		/// <seealso cref="HumanoidAlienRaces.Patches.AlienRace_AlienPartGenerator_RotationOffset_GetOffset"/>
		/// <seealso cref="HumanoidAlienRaces.Patches.AlienRace_ExtendedGraphics_ConditionApparel_Satisfied"/>
		/// <seealso cref="HumanoidAlienRaces.Patches.AlienRace_ExtendedGraphics_ExtendedGraphicsPawnWrapper_GetBodyPart"/>
		/// <seealso cref="HumanoidAlienRaces.Patches.AlienRace_ThoughtSettings_ReplaceIfApplicable"/>
		public static OptimizationOption OptHARDeLinq { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.HARDeLinq",
			Desc = "YaOpt.Setting.Option.HARDeLinq.Desc",
			SettingId = "OptHARDeLinq",
			RequiredMod = "erdelf.HumanoidAlienRaces",
			SubCategory = "YaOpt.Setting.SubCategory.HumanoidAlienRaces",
			Category = OptimizationCategory.Fps,
		};

		/// <summary>
		/// Integrates HAR texture loading with YaOpt's texture cache for dynamic body parts.
		/// </summary>
		/// <seealso cref="HumanoidAlienRaces.Patches.AlienRace_AlienPartGenerator_BodyAddon_GetGraphic"/>
		/// <seealso cref="HumanoidAlienRaces.Patches.AlienRace_AlienRenderTreePatches_CheckMaskShader"/>
		public static OptimizationOption OptHARTextureCache { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.HARTextureCache",
			Desc = "YaOpt.Setting.Option.HARTextureCache.Desc",
			SettingId = "OptHARTextureCache",
			RequiredMod = "erdelf.HumanoidAlienRaces",
			SubCategory = "YaOpt.Setting.SubCategory.HumanoidAlienRaces",
			Category = OptimizationCategory.Fps,
		};

		public override IEnumerable<OptimizationOption> OnCreateSettings()
		{
			yield return OptHARDeLinq;
			yield return OptHARTextureCache;
		}

		public override bool OnPatch(Harmony harmony)
		{
			var assembly = Assembly.GetExecutingAssembly();
			return harmony.TryPatchAll(assembly);
		}

		public override void OnUnpatch(Harmony harmony)
		{
			harmony.UnpatchAll(harmony.Id);
		}
	}
}
