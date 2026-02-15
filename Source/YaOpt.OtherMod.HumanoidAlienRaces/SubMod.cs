using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers;
using static YaOpt.YaOptSettings;

namespace YaOpt.OtherMod.HumanoidAlienRaces
{
	internal class SubMod : YaOptSubMod
	{
		/// <summary>
		/// Rewrites LINQ expressions into GC-friendly loops.
		/// LINQ generates significant GC overhead in Unity.
		/// </summary>
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
		/// Optimizes texture loading for HAR by utilizing the texture caching system.
		/// Significantly improves performance for races with dynamic body parts.
		/// </summary>
		public static OptimizationOption OptHARTextureCache { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.HARTextureCache",
			Desc = "YaOpt.Setting.Option.HARTextureCache.Desc",
			SettingId = "OptHARTextureCache",
			RequiredMod = "erdelf.HumanoidAlienRaces",
			SubCategory = "YaOpt.Setting.SubCategory.HumanoidAlienRaces",
			Category = OptimizationCategory.Fps,
		};

		public override IEnumerable<YaOptSettings.OptimizationOption> OnCreateSettings()
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
