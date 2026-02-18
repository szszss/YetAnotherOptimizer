using System;
using System.Collections.Generic;
using YaOpt.Helpers.Trampolines;

namespace YaOpt.Patches.Trampolines
{
	internal static class TrampolinePatcher
	{
		private static readonly List<TrampolineInstaller> _registeredTrampolines = new List<TrampolineInstaller>();

		private static readonly List<TrampolineInstaller> _registeredEarlyTrampolines = new List<TrampolineInstaller>();

		public static bool CanUseTrampoline => YaOptGlobal.IsNativeAvailable && YaOptGlobal.IsTrampolineAvailable;

		public static void RegisterTrampolineInstallers()
		{
			if (!CanUseTrampoline)
				return;

			TrampolineFactory.Instance.CreateTrampolineInstallers();
		}

		public static void EarlyInit()
		{
			if (!CanUseTrampoline)
				return;

			_registeredEarlyTrampolines.Add(Verse_ContentFinder_Get.Instance);
			InitDo(_registeredEarlyTrampolines);
		}

		public static void Init()
		{
			if (!CanUseTrampoline)
				return;

			_registeredTrampolines.Add(Verse_ThingWithComps_GetComp.Instance);
			InitDo(_registeredTrampolines);
		}

		public static void EarlyInstallAll()
		{
			if (!CanUseTrampoline)
				return;

			InstallDo(_registeredEarlyTrampolines);
		}

		public static void InstallAll()
		{
			if (!CanUseTrampoline)
				return;

			InstallDo(_registeredTrampolines);
		}

		private static void InitDo(List<TrampolineInstaller> trampolineInstallers)
		{
			foreach (var installer in trampolineInstallers)
			{
				try
				{
					installer.Init();
				}
				catch (Exception e)
				{
					YaOptMod.Error($"Failed to prepare trampolines installer {installer.GetType().Name}\n{e}");
				}
			}
		}

		private static void InstallDo(List<TrampolineInstaller> trampolineInstallers)
		{
			foreach (var installer in trampolineInstallers)
			{
				try
				{
					installer.TryInstall();
				}
				catch (Exception e)
				{
					YaOptMod.Error($"Failed to install trampolines for {installer.GetType().Name}\n{e}");
				}
			}
		}

		public static void UninstallAll()
		{
			if (!CanUseTrampoline)
				return;

			foreach (var installer in _registeredTrampolines)
			{
				try
				{
					installer.TryUninstall();
				}
				catch (Exception e)
				{
					YaOptMod.Error($"Failed to uninstall trampolines for {installer.GetType().Name}. " +
								   $"The game could be very instable!\n{e}");
				}
			}
		}
	}
}