using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers;
using YaOpt.Settings;

namespace YaOpt.OtherMod.CombatExtended
{
	/// <summary>
	/// Compatibility module for Combat Extended. Provides Burst optimizations.
	/// </summary>
	internal class SubMod : YaOptSubMod
	{
		public static OptimizationOption OptCELineOfSightBurst { get; } = new OptimizationOption
		{
			Name = "YaOpt.Setting.Option.CELineOfSightBurst",
			Desc = "YaOpt.Setting.Option.CELineOfSightBurst.Desc",
			NotePlatform = "YaOpt.Setting.Option.CELineOfSightBurst.Platform",
			SettingId = "OptCELineOfSightBurst",
			RequiredMod = "CETeam.CombatExtended",
			SubCategory = "YaOpt.Setting.SubCategory.CombatExtended",
			Category = OptimizationCategory.Tps,
		};

		public override IEnumerable<OptimizationOption> OnCreateSettings()
		{
			yield return OptCELineOfSightBurst;
		}

		public override bool OnPatch(Harmony harmony)
		{
#if false && DEBUG
			AccessTools.Field(typeof(global::CombatExtended.Settings), "debuggingMode")
				.SetValue(Controller.settings, true);
			AccessTools.Field(typeof(global::CombatExtended.Settings), "debugDrawPartialLoSChecks")
				.SetValue(Controller.settings, true);
#endif

			var assembly = Assembly.GetExecutingAssembly();
			return harmony.TryPatchAll(assembly);
		}

		public override void OnUnpatch(Harmony harmony)
		{
			harmony.UnpatchAll(harmony.Id);
		}
	}
}
