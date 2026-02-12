using System;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers;

namespace YaOpt.Patches.Trampolines
{
	internal abstract class TrampolineInstaller
	{
		private static readonly List<TrampolineInstaller> registeredTrampolines = new List<TrampolineInstaller>();

		private static readonly List<TrampolineInstaller> registeredEarlyTrampolines = new List<TrampolineInstaller>();

		public static bool CanUseTrampoline => YaOptGlobal.IsWindows && YaOptGlobal.IsTrampolineAvailable;

		public bool Installed { protected set; get; }

		public bool Available { protected set; get; }

		protected MethodInfo SourceMethod { set; get; }

		protected MethodInfo TargetMethod { set; get; }

		protected byte[] PrefixCode { set; get; }

		protected AsmHelper.ITrampoline Trampoline { set; get; }

		public static void EarlyInit()
		{
			if (!CanUseTrampoline)
				return;

			registeredEarlyTrampolines.Add(new Verse_ContentFinder_Get());
			InitDo(registeredEarlyTrampolines);
		}

		public static void Init()
		{
			if (!CanUseTrampoline)
				return;

			registeredTrampolines.Add(new Verse_ThingWithComps_GetComp());
			InitDo(registeredTrampolines);
		}

		private static void InitDo(List<TrampolineInstaller> trampolineInstallers)
		{
			foreach (var installer in trampolineInstallers)
			{
				try
				{
					installer.Prepare();
					installer.AfterPrepare();
					installer.Validate();
					installer.Available = true;
				}
				catch (Exception e)
				{
					YaOptMod.Error($"Failed to prepare trampolines installer {installer.GetType().Name}\n{e}");
				}
			}
		}

		public static void EarlyInstallAll()
		{
			if (!CanUseTrampoline)
				return;

			InstallDo(registeredEarlyTrampolines);
		}

		public static void InstallAll()
		{
			if (!CanUseTrampoline)
				return;

			InstallDo(registeredTrampolines);
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
					installer.Installed = false;
					installer.Available = false;
				}
			}
		}

		public static void UninstallAll()
		{
			if (!CanUseTrampoline)
				return;

			foreach (var installer in registeredTrampolines)
			{
				try
				{
					installer.TryUninstall();
				}
				catch (Exception e)
				{
					YaOptMod.Error($"Failed to uninstall trampolines for {installer.GetType().Name}. " +
								   $"The game could be very instable!\n{e}");
					installer.Installed = false;
					installer.Available = false;
				}
			}
		}

		public void TryInstall()
		{
			if (!Installed && Available && ShouldInstall())
			{
				Install();
				Installed = true;
			}
		}

		public void TryUninstall()
		{
			if (Installed)
			{
				Uninstall();
				Installed = false;
			}
		}

		protected abstract void Prepare();

		protected virtual void AfterPrepare()
		{
			if (SourceMethod == null)
				throw new MissingMethodException($"Cannot find source method for {GetType().Name}");
			if (TargetMethod == null)
				throw new MissingMethodException($"Cannot find target method for {GetType().Name}");
			Trampoline = AsmHelper.TrampolineFactory.CreateTrampoline(SourceMethod, TargetMethod, PrefixCode);
		}

		protected abstract bool ShouldInstall();

		protected virtual void Validate()
		{
		}

		protected virtual void Install()
		{
			Trampoline.Install();
		}

		protected virtual void Uninstall()
		{
			Trampoline.Uninstall();
		}
	}
}