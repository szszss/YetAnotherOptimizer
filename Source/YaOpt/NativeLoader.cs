using System;
using System.IO;
using System.Reflection;
using Unity.Burst;

namespace YaOpt
{
	public static class NativeLoader
	{
		public static void LoadLibraries(Assembly modAssembly)
		{
			var dllDirectory = Path.GetDirectoryName(modAssembly.Location);
			if (string.IsNullOrEmpty(dllDirectory))
			{
				YaOptMod.Error("Cannot get the location of mod dll. Some functions will not work.");
				return;
			}

			string nativeDllName = null;
			string initerName = null;
			string burstDllName = null;

			if (Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
			{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				{
					nativeDllName = "YaOpt.Native.Win64.dll";
					initerName = "YaOpt.Native.Win64.Initer";
					burstDllName = "yaopt_burst_win64.dll";
				}
				// Future: Add Linux/Mac support here
				// else if (Environment.OSVersion.Platform == PlatformID.Unix) { ... }
			}

			if (nativeDllName != null && initerName != null)
			{
				LoadNativeLibrary(dllDirectory, nativeDllName, initerName);
			}
			if (burstDllName != null)
			{
				LoadBurstLibrary(dllDirectory, burstDllName);
			}
			if (nativeDllName == null || burstDllName == null)
			{
				YaOptMod.Warning($"Some functions only work on 64Bit OS, current OS: {Environment.OSVersion}");
			}
		}

		private static void LoadNativeLibrary(string directory, string dllName, string initerType)
		{
			try
			{
				var dllPath = Path.GetFullPath(Path.Combine(directory, dllName));
				YaOptMod.Debug($"Load Native library from {dllPath}");
				var dll = Assembly.LoadFile(dllPath);
				var type = dll.GetType(initerType, true);
				// ReSharper disable once PossibleNullReferenceException
				type.GetMethod("Init").Invoke(null, null);
				YaOptGlobal.IsNativeAvailable = true;
			}
			catch (Exception e)
			{
				YaOptMod.Error($"Error when load {dllName}. Trampoline will be unavailable\n {e}");
			}
		}

		private static void LoadBurstLibrary(string directory, string dllName)
		{
			try
			{
				var dllPath = Path.GetFullPath(Path.Combine(directory, "..\\Burst", dllName));
				if (!File.Exists(dllPath))
				{
					throw new FileNotFoundException("Cannot find Burst library", dllPath);
				}
				YaOptMod.Debug($"Load Burst library from {dllPath}");
				if (BurstRuntime.LoadAdditionalLibrary(dllPath))
				{
					YaOptGlobal.IsBurstAvailable = true;
				}
				else
				{
					YaOptMod.Error($"Failed to load Burst library from {dllPath}. Any features that require Burst will not work.");
				}
			}
			catch (Exception e)
			{
				YaOptMod.Error($"Error when load Burst library.\n {e}");
			}
		}
	}
}
