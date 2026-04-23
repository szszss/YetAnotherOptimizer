using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Burst;
using Verse;

namespace YaOpt
{
	public static class NativeLoader
	{
		public const string LOAD_FOLDER = "1.6";

		public static void LoadLibraries(ModContentPack content)
		{
			string nativeDllName = null;
			string initerName = null;
			string burstDllName = null;

			if (Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					nativeDllName = "YaOpt.Native.Win64.dll";
					initerName = "YaOpt.Native.Win64.Initer";
					burstDllName = "yaopt_burst_win64.dll";
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					nativeDllName = "YaOpt.Native.Unix64.dll";
					initerName = "YaOpt.Native.Unix64.Initer";
					burstDllName = "yaopt_burst_linux64.so";
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					burstDllName = "yaopt_burst_osx64.bundle";
				}
			}

			if (nativeDllName != null)
			{
				LoadNativeLibrary(content, nativeDllName, initerName);
			}
			else
			{
				YaOptMod.Warning($"Some functions only work on Windows and Linux. Current OS: {Environment.OSVersion}");
			}
			if (burstDllName != null)
			{
				LoadBurstLibrary(content, burstDllName);
			}
			else
			{
				YaOptMod.Warning($"This system doesn't support Burst. Current OS: {Environment.OSVersion}");
			}
		}

		private static string GetFileFromFolders(List<string> folders, string filename)
		{
			foreach (var folder in folders)
			{
				var path = Path.Combine(folder, filename);
				if (File.Exists(path))
					return path;
			}
			return null;
		}

		private static void LoadNativeLibrary(ModContentPack content, string dllName, string initerType)
		{
			try
			{
				var dllPath = GetFileFromFolders(content.foldersToLoadDescendingOrder,
					Path.Combine("Assemblies", dllName));
				if (string.IsNullOrEmpty(dllPath))
				{
					throw new FileNotFoundException("Cannot find native library", dllPath);
				}
				YaOptMod.Debug($"Load Native library from {dllPath}");
				//var dll = Assembly.LoadFile(dllPath);
				//var type = dll.GetType(initerType, true);
				// ReSharper disable once PossibleNullReferenceException
				//type.GetMethod("Init").Invoke(null, null);
				AccessTools.Method(AccessTools.TypeByName(initerType), "Init").Invoke(null, null);
				YaOptGlobal.IsNativeAvailable = true;
			}
			catch (Exception e)
			{
				YaOptMod.Error($"Error when load {dllName}. Trampoline will be unavailable\n {e}");
			}
		}

		private static void LoadBurstLibrary(ModContentPack content, string dllName)
		{
			try
			{
				var dllPath = GetFileFromFolders(content.foldersToLoadDescendingOrder,
					Path.Combine("Burst", dllName));
				if (string.IsNullOrEmpty(dllPath))
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
				YaOptMod.Error($"Error when load Burst library. " +
							   $"Any features that require Burst will not work.\n {e}");
			}
		}
	}
}
