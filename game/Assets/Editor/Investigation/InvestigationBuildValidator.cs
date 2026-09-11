using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace EDNA.Investigation.Editor
{
    public static class InvestigationBuildValidator
    {
        private const string ScenePath = "Assets/Scenes/InvestigationScene.unity";
        public const string DefaultOutputPath = "/private/tmp/edna-investigation-webgl";
        public const string MacOsOutputPath = "/private/tmp/edna-investigation-macos.app";

        public static void BuildWebGlFromCommandLine()
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = DefaultOutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL validation build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            }
            UnityEngine.Debug.Log($"Investigation WebGL validation build succeeded at {DefaultOutputPath}");
        }

        public static void BuildMacOsFromCommandLine()
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = MacOsOutputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"macOS validation build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            }
            UnityEngine.Debug.Log($"Investigation macOS validation build succeeded at {MacOsOutputPath}");
        }

    }
}
