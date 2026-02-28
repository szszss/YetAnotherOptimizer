using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;

public static class BuildCommand
{
    private const string OutputDir = "Builds";
    private const string ArtifactsDir = "Artifacts";

    public static void PerformBuild()
    {
        // Get target from command line arguments or default to Windows
        var target = GetBuildTargetFromArgs();
        var buildPath = Path.Combine(OutputDir, target.ToString());
        
        Debug.Log($"Starting build for {target}...");

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" }, // Must have at least one scene
            locationPathName = GetBuildLocation(target, buildPath),
            target = target,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes");
            ExtractArtifacts(target, buildPath);
        }
        else
        {
            Debug.LogError($"Build failed: {summary.result}");
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    Debug.LogError(message.content);
                }
            }
            EditorApplication.Exit(1);
        }
    }

    private static BuildTarget GetBuildTargetFromArgs()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (args.Contains("-buildTarget"))
        {
            var targetStr = args[System.Array.IndexOf(args, "-buildTarget") + 1];
            if (System.Enum.TryParse(targetStr, out BuildTarget target))
                return target;
        }
        return BuildTarget.StandaloneWindows64;
    }

    private static string GetBuildLocation(BuildTarget target, string path)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows64:
                return Path.Combine(path, "YaOpt.exe");
            case BuildTarget.StandaloneLinux64:
                return Path.Combine(path, "YaOpt.x86_64");
            case BuildTarget.StandaloneOSX:
                return Path.Combine(path, "YaOpt.app");
            default:
                return Path.Combine(path, "YaOpt");
        }
    }

    private static void ExtractArtifacts(BuildTarget target, string buildPath)
    {
        var artifactsPath = Path.Combine(OutputDir, ArtifactsDir, target.ToString());
        Directory.CreateDirectory(artifactsPath);

        string burstLibName = "";
        string burstSourcePath = "";
        string managedDllName = "YaOpt.Unity.dll"; // Assumes asmdef name
        string managedSourcePath = "";

        switch (target)
        {
            case BuildTarget.StandaloneWindows64:
                burstLibName = "lib_burst_generated.dll";
                burstSourcePath = Path.Combine(buildPath, "YaOpt_Data", "Plugins", "x86_64", burstLibName);
                managedSourcePath = Path.Combine(buildPath, "YaOpt_Data", "Managed", managedDllName);
                break;
            case BuildTarget.StandaloneLinux64:
                burstLibName = "lib_burst_generated.so";
                burstSourcePath = Path.Combine(buildPath, "YaOpt_Data", "Plugins", "x86_64", burstLibName);
                managedSourcePath = Path.Combine(buildPath, "YaOpt_Data", "Managed", managedDllName);
                break;
            case BuildTarget.StandaloneOSX:
                burstLibName = "lib_burst_generated.bundle";
                // macOS bundle structure is different
                burstSourcePath = Path.Combine(buildPath, "YaOpt.app", "Contents", "Plugins", burstLibName); 
                managedSourcePath = Path.Combine(buildPath, "YaOpt.app", "Contents", "Resources", "Data", "Managed", managedDllName);
                break;
        }

        // Copy Burst Library
        if (File.Exists(burstSourcePath))
        {
            Debug.Log($"Found Burst library: {burstSourcePath}");
            File.Copy(burstSourcePath, Path.Combine(artifactsPath, burstLibName), true);
        }
        else if (Directory.Exists(burstSourcePath)) // macOS .bundle is a directory
        {
            Debug.Log($"Found Burst bundle: {burstSourcePath}");
            CopyDirectory(burstSourcePath, Path.Combine(artifactsPath, burstLibName));
        }
        else
        {
            Debug.LogWarning($"Burst library not found at: {burstSourcePath}");
        }

        // Copy Managed DLL
        if (File.Exists(managedSourcePath))
        {
            Debug.Log($"Found Managed DLL: {managedSourcePath}");
            File.Copy(managedSourcePath, Path.Combine(artifactsPath, managedDllName), true);
        }
        else
        {
            Debug.LogError($"Managed DLL not found at: {managedSourcePath}");
            // Don't fail the build if managed dll is missing, main build might fail later though
        }

        Debug.Log($"Artifacts extracted to: {artifactsPath}");
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
    }
}
