using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.Trampoline;
using YaOpt.Patches;
using YaOpt.Patches.Trampolines;

namespace YaOpt
{
	public class YaOptMod : Mod
	{
		public static YaOptMod Instance { get; private set; }

		public Harmony Harmony { get; } = new Harmony("YetAnotherOptimizer");

		public Harmony EarlyHarmony { get; } = new Harmony("YetAnotherOptimizer.Early");

		public YaOptSettings Settings { get; }

		public List<YaOptSubMod> SubMods { get; } = new List<YaOptSubMod>();

		public YaOptMod(ModContentPack content) : base(content)
		{
			Instance = this;
			YaOptGlobal.IsMultiplayer = YaOptGlobal.HasMod("rwmt.Multiplayer");

			InitSubMods();

			Settings = GetSettings<YaOptSettings>();
			Log("Now loading...");

			NativeLoader.LoadLibraries(Assembly.GetExecutingAssembly());

			ApplySettings();
			ApplyEarlyPatches();
		}

		private void InitSubMods()
		{
			SubMods.AddRange(YaOptSubMod.LoadAll());
			YaOptSubMod.PreInitAll(SubMods);
		}

		private void ApplySettings()
		{
			Settings.ValidateOptions(true);
			YaOptSubMod.InitAll(SubMods);

#if DEBUG
			HarmonyLib.Harmony.DEBUG = true;
#endif
		}

		private void ApplyEarlyPatches()
		{
			if (Settings.OptLazyTextureLoad.Enabled)
			{
				ContentManager.Init();
				ContentManager.OnlyLazilyLoadDds = Settings.LazyTextureLoadDdsOnly;
			}
			TrampolineInstaller.EarlyInit();
			TrampolineInstaller.EarlyInstallAll();
			EarlyHarmony.TryPatchAll(Assembly.GetExecutingAssembly(), true);
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "YaOpt";
		}

		public void ClearCaches()
		{
			UpdateCallbackHelper.ClearCache();
			Patcher.CheckAndRePatchOnLoadGame();
		}

		public static void Debug(string message)
		{
			if (Instance?.Settings?.DebugLogging == true)
				Verse.Log.Message("[YaOpt] " + message);
		}

		public static void Log(string message)
		{
			Verse.Log.Message("[YaOpt] " + message);
		}

		public static void Warning(string message)
		{
			Verse.Log.Warning("[YaOpt] " + message);
		}

		public static void Error(string message)
		{
			Verse.Log.Error("[YaOpt] " + message);
		}

		public static void ErrorOnce(string message, int key)
		{
			Verse.Log.ErrorOnce("[YaOpt] " + message, key);
		}
	}
}
