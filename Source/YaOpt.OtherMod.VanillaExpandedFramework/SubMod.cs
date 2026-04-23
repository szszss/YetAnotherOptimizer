using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers;
using YaOpt.OtherMod.VanillaExpandedFramework.Helpers;
using YaOpt.Settings;

namespace YaOpt.OtherMod.VanillaExpandedFramework
{
	/// <summary>
	/// Compatibility module for Vanilla Expanded Framework. Provides thread-safe and memory leak fix.
	/// </summary>
	public class SubMod : YaOptSubMod
	{
		/// <seealso cref="YaOpt.OtherMod.VanillaExpandedFramework.Helpers.MemoryLeakFixer"/>
		public static OptimizationOption OptVEFMemoryLeakFix { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.VEFMemoryLeakFix",
			Desc = "YaOpt.Setting.Option.VEFMemoryLeakFix.Desc",
			SettingId = "OptVEFMemoryLeakFix",
			RequiredMod = "OskarPotocki.VanillaFactionsExpanded.Core",
			SubCategory = "YaOpt.Setting.SubCategory.VanillaExpandedFramework",
			Category = OptimizationCategory.Misc
		};

		public override IEnumerable<OptimizationOption> OnCreateSettings()
		{
			yield return OptVEFMemoryLeakFix;
		}

		public override bool OnPatch(Harmony harmony)
		{
			MemoryLeakFixer.Enable = OptVEFMemoryLeakFix.Enabled;
			var assembly = Assembly.GetExecutingAssembly();
			return harmony.TryPatchAll(assembly);
		}

		public override void OnUnpatch(Harmony harmony)
		{
			MemoryLeakFixer.Enable = false;
			harmony.UnpatchAll(harmony.Id);
		}
	}
}
