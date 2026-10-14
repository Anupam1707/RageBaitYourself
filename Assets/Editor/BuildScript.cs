#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

public class BuildScript
{
    private static string[] GetScenes()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
    }

    [MenuItem("Build/Build All Platforms")]
    public static void BuildAll()
    {
        string[] scenes = GetScenes();
        string rootDir = Directory.GetCurrentDirectory();
        string outputDir = Path.Combine(rootDir, "FinalBuilds");

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        Debug.Log("🚀 Starting Automated Multi-Platform Build Pipeline...");

        // 1. macOS Universal (Intel + Apple Silicon)
        BuildForPlatform(scenes, Path.Combine(outputDir, "RageBaitYourself-0.1-macos-universal.app"), BuildTarget.StandaloneOSX);

        // 2. Windows 64-bit
        BuildForPlatform(scenes, Path.Combine(outputDir, "RageBaitYourself-0.1-win-x64", "RageBaitYourself.exe"), BuildTarget.StandaloneWindows64);

        // 3. Linux 64-bit
        BuildForPlatform(scenes, Path.Combine(outputDir, "RageBaitYourself-0.1-linux-x86_64", "RageBaitYourself.x86_64"), BuildTarget.StandaloneLinux64);

        Debug.Log("🎉 All platforms built successfully!");
    }

    [MenuItem("Build/Build macOS Only")]
    public static void BuildMacOS()
    {
        string rootDir = Directory.GetCurrentDirectory();
        string outputDir = Path.Combine(rootDir, "FinalBuilds");
        BuildForPlatform(GetScenes(), Path.Combine(outputDir, "RageBaitYourself-0.1-macos-universal.app"), BuildTarget.StandaloneOSX);
    }

    private static void BuildForPlatform(string[] scenes, string outputPath, BuildTarget target)
    {
        Debug.Log($"📦 Building for {target} -> {outputPath}...");
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = outputPath;
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"✅ {target} Build Succeeded! Size: {summary.totalSize} bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"❌ {target} Build Failed with {summary.totalErrors} errors!");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
