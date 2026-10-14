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

    private static void ApplyVersionFromEnv()
    {
        string ver = System.Environment.GetEnvironmentVariable("GAME_VERSION");
        if (!string.IsNullOrEmpty(ver))
        {
            ver = ver.TrimStart('v', 'V');
            PlayerSettings.bundleVersion = ver;
            Debug.Log($"🏷️ Set Unity PlayerSettings.bundleVersion to: {ver}");
        }
    }

    [MenuItem("Build/Build All Platforms")]
    public static void BuildAll()
    {
        ApplyVersionFromEnv();
        string[] scenes = GetScenes();
        string rootDir = Directory.GetCurrentDirectory();
        string outputDir = Path.Combine(rootDir, "FinalBuilds");

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        Debug.Log("🚀 Starting Automated Multi-Platform Build Pipeline...");

        // 1. macOS Universal (Intel x86_64 + Apple Silicon arm64 combined)
        try {
            PlayerSettings.SetArchitecture(UnityEditor.Build.NamedBuildTarget.Standalone, 2);
        } catch {}
        BuildForPlatform(scenes, Path.Combine(outputDir, "RageBait Yourself.app"), BuildTarget.StandaloneOSX);
        EmbedMacIcon(rootDir, outputDir);

        // 2. Windows 64-bit (x64)
        BuildForPlatform(scenes, Path.Combine(outputDir, "RageBaitYourself-win-x64", "RageBaitYourself.exe"), BuildTarget.StandaloneWindows64);

        // 3. Windows 32-bit (x86 / ia32 separate)
        BuildForPlatform(scenes, Path.Combine(outputDir, "RageBaitYourself-win-ia32", "RageBaitYourself.exe"), BuildTarget.StandaloneWindows);

        // 4. Linux 64-bit (x86_64)
        BuildForPlatform(scenes, Path.Combine(outputDir, "RageBaitYourself-linux-x86_64", "RageBaitYourself.x86_64"), BuildTarget.StandaloneLinux64);

        Debug.Log("🎉 All platforms built successfully!");
    }

    [MenuItem("Build/Build macOS Only")]
    public static void BuildMacOS()
    {
        ApplyVersionFromEnv();
        string rootDir = Directory.GetCurrentDirectory();
        string outputDir = Path.Combine(rootDir, "FinalBuilds");
        BuildForPlatform(GetScenes(), Path.Combine(outputDir, "RageBait Yourself.app"), BuildTarget.StandaloneOSX);
        EmbedMacIcon(rootDir, outputDir);
    }

    private static void EmbedMacIcon(string rootDir, string outputDir)
    {
        string iconSrc = Path.Combine(rootDir, "Launcher", "assets", "icon.icns");
        string iconDest = Path.Combine(outputDir, "RageBait Yourself.app", "Contents", "Resources", "PlayerIcon.icns");
        if (File.Exists(iconSrc) && Directory.Exists(Path.GetDirectoryName(iconDest)))
        {
            File.Copy(iconSrc, iconDest, true);
            Debug.Log("🎨 Embedded official game icon into PlayerIcon.icns!");
        }
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
