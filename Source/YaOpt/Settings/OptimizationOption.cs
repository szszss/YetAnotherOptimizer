using System;
using Verse;
using YaOpt.Defines;

namespace YaOpt.Settings
{
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
				if (CompatibilityDefines.CachedBannedOptimizations.Contains(SettingId))
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

		public string NotePlatform { get; set; } = string.Empty;

		public string NoteMultithread { get; set; } = string.Empty;

		public string RequiredMod { get; set; } = string.Empty;

		public string SubCategory { get; set; } = string.Empty;

		public string SettingId { get; set; } = string.Empty;

		public OptimizationCategory Category { get; set; }

		public OptimizationFlags Flags { get; set; }

		public OptimizationOption RequiredOption;

		public Func<YaOptSettings, bool> FuncShow { get; set; } = null;

		public Action<SettingsPanel, Listing_Standard, OptimizationOption> FuncPostDraw { get; set; } = null;

		public Action<YaOptSettings> FuncExposeData { get; set; } = null;

		public bool MultiplayerCompatibility => (Flags & OptimizationFlags.MultiplayerIncompatible) == 0;

		public bool Validate(bool dryRun, bool printError, bool translateMessage, out string message)
		{
			// Validator doesn't validate multiplay and mod requirements. They are validated in the getter of Enabled
			message = string.Empty;
			var error = false;
			if (_enabled && (Flags & OptimizationFlags.RequireNative) > 0 && !YaOptGlobal.IsNativeAvailable)
			{
				if (!dryRun)
					_enabled = false;
				error = true;
				message = "YaOpt.Setting.InvalidOption.RequireNative";
				if (translateMessage)
					message = message.Translate().ToString();
			}
			if (_enabled && (Flags & OptimizationFlags.RequireBurst) > 0 && !YaOptGlobal.IsBurstAvailable)
			{
				if (!dryRun)
					_enabled = false;
				error = true;
				message = "YaOpt.Setting.InvalidOption.RequireBurst";
				if (translateMessage)
					message = message.Translate().ToString();
			}
			if (printError && error)
			{
				var name = Name;
				if (translateMessage)
					name = name.Translate();
				YaOptMod.Error($"Optimization {name} has been disabled because {message}");
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
}