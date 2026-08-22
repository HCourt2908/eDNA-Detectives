using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace EDNA.Investigation.V2.Editor
{
    public static class InvestigationV2BuildValidator
    {
        public const string DefaultOutputPath = "/private/tmp/edna-investigation-v2-webgl";
        public const string MacOsOutputPath = "/private/tmp/edna-investigation-v2-macos.app";

        public static void BuildWebGlFromCommandLine()
        {
            List<string> scenes = GetEnabledScenes();
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = DefaultOutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL validation build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            }
            UnityEngine.Debug.Log($"Investigation V2 WebGL validation build succeeded at {DefaultOutputPath}");
        }

        public static void BuildMacOsFromCommandLine()
        {
            List<string> scenes = new List<string> { "Assets/Scenes/InvestigationSceneV2.unity" };
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = MacOsOutputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"macOS validation build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            }
            UnityEngine.Debug.Log($"Investigation V2 macOS validation build succeeded at {MacOsOutputPath}");
        }

        private static List<string> GetEnabledScenes()
        {
            List<string> scenes = new List<string>();
            EditorBuildSettingsScene[] configuredScenes = EditorBuildSettings.scenes;
            for (int index = 0; index < configuredScenes.Length; index++)
            {
                if (configuredScenes[index].enabled) scenes.Add(configuredScenes[index].path);
            }
            if (scenes.Count == 0) throw new InvalidOperationException("No enabled scenes are available for validation.");
            return scenes;
        }
    }
}
