using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FishingSim.EditorTools
{
    public static class BuildTools
    {
        const string OutputPath = "Builds/FishingSimulator/FishingSimulator.exe";

        [MenuItem("Fishing/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "Fishing Simulator";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            Debug.Log("Player settings applied: 1920x1080, fullscreen window.");
        }

        [MenuItem("Fishing/Build Windows")]
        public static void BuildWindows()
        {
            ApplyPlayerSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/FishingLake.unity" },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"Build succeeded: {OutputPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
            else
                Debug.LogError("Build failed: " + report.summary.result);
        }
    }
}
