using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Small command-line build check for the browser version of the CTD scene.
/// StreamingAssets are copied beside the generated WebGL files and are
/// intentionally not imported as embedded VideoClip assets.
/// </summary>
public static class CTDWebGLBuildValidator
{
    private const string ScenePath = "Assets/Scenes/CTD-Minigame.unity";
    public const string OutputPath = "/private/tmp/ctd-webgl-video-fix";

    public static void ValidateStreamingAsset()
    {
        string assetPath = Path.Combine(Application.dataPath, "StreamingAssets/RosetteDeployment/Launch/rosette_entry_ocean_animation.mp4");
        if (!File.Exists(assetPath))
            throw new InvalidOperationException($"WebGL streaming video is missing: {assetPath}");

        if (File.Exists(Path.Combine(Application.dataPath, "Resources/RosetteDeployment/Launch/rosette_entry_ocean_animation.mp4")))
            throw new InvalidOperationException("The WebGL video is still embedded under Resources.");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var sequence = UnityEngine.Object.FindAnyObjectByType<RosetteEntrySequence>(FindObjectsInactive.Include);
        if (sequence == null || !sequence.useStreamingUrl || string.IsNullOrWhiteSpace(sequence.streamingVideoPath))
            throw new InvalidOperationException("Rosette entry sequence is not configured for StreamingAssets URL playback.");

        UnityEngine.Debug.Log($"CTD_WEBGL_VIDEO_VALIDATION_OK: {sequence.streamingVideoPath}");
    }

    public static void Build()
    {
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"CTD WebGL validation build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
        }

        UnityEngine.Debug.Log($"CTD_WEBGL_BUILD_OK: {OutputPath}");
    }
}
