using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Burst;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
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
			SubMods.AddRange(YaOptSubMod.LoadAll());
			YaOptSubMod.PreInitAll(SubMods);
			Settings = GetSettings<YaOptSettings>();
			Log("Now loading...");
			var dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (string.IsNullOrEmpty(dllDirectory))
			{
				Error("Cannot get the location of mod dll. Some functions will not work.");
			}
			string nativeDllName = null;
			string initerName = null;
			string burstDllName = null;

			if (Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
			{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				{
					YaOptGlobal.IsWindows = true;
					nativeDllName = "YaOpt.Native.Win64.dll";
					initerName = "YaOpt.Native.Win64.Initer";
					burstDllName = "yaopt_burst_win64.dll";
				}
				// else if (Environment.OSVersion.Platform == PlatformID.Unix) { ... }
			}
			
			if (nativeDllName != null)
			{
				if (!string.IsNullOrEmpty(dllDirectory))
				{
					try
					{
						var nativeDll = Path.GetFullPath(Path.Combine(dllDirectory, nativeDllName));
						Debug($"Load Native library from {nativeDll}");
						var dll = Assembly.LoadFile(nativeDll);
						var type = dll.GetType(initerName, true);
						// ReSharper disable once PossibleNullReferenceException
						type.GetMethod("Init").Invoke(null, null);
					}
					catch (Exception e)
					{
						Error($"Error when load {nativeDllName}. Trampoline will be unavailable\n {e}");
					}
					try
					{
						if (burstDllName != null)
						{
							var burstDll = Path.GetFullPath(Path.Combine(dllDirectory, "..\\Burst\\" + burstDllName));
							if (!File.Exists(burstDll))
							{
								throw new FileNotFoundException("Cannot find Burst library", burstDll);
							}
							Debug($"Load Burst library from {burstDll}");
							if (BurstRuntime.LoadAdditionalLibrary(burstDll))
							{
								YaOptGlobal.IsBurstAvailable = true;
							}
							else
							{
								YaOptMod.Error($"Failed to load Burst library from {burstDll}. " +
											   "Any features that require Burst will not work.");
							}
						}
					}
					catch (Exception e)
					{
						Error($"Error when load Burst library.\n {e}");
					}
				}
			}
			else
			{
				Warning($"Some functions only work on 64Bit OS, current OS: {Environment.OSVersion}");
			}
			YaOptGlobal.IsLibraryLoaded = true;
			Settings.ValidateOptions(true);
			YaOptSubMod.InitAll(SubMods);

#if DEBUG
			HarmonyLib.Harmony.DEBUG = true;
#endif
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
