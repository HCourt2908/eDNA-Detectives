using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays the completed Rosette-entry animation at its authored frame rate, holds
/// on the final frame, and exposes a Ready action for the sampling minigame.
/// Visual positioning remains in the LaunchPanel scene hierarchy.
/// </summary>
public class RosetteEntrySequence : MonoBehaviour
{
    private const string StreamingVideoPath = "RosetteDeployment/Launch/rosette_entry_ocean_animation.mp4";

    [Header("Scene UI")]
    public SamplingCockpitView cockpit;
    public RawImage animationSurface;
    public TMP_Text readyLabel;
    public Button readyButton;
    [Header("WebGL video")]
    [Tooltip("The path is relative to Assets/StreamingAssets and is served beside the WebGL build.")]
    public string streamingVideoPath = StreamingVideoPath;
    [Tooltip("Use the browser-safe URL source. Keep enabled for WebGL builds.")]
    public bool useStreamingUrl = true;

    public event Action ReadyPressed;

    private VideoPlayer videoPlayer;
    private Image panelImage;
    private bool configured;

    public void Configure(TMP_Text label)
    {
        readyLabel = label;
        if (animationSurface == null)
        {
            Transform surfaceTransform = transform.Find("AnimationSurface");
            if (surfaceTransform != null)
            {
                animationSurface = surfaceTransform.GetComponent<RawImage>();
            }
        }

        // The manager configures this sequence while LaunchPanel is inactive.
        // Include inactive parents so the listener targets the visible button.
        readyButton = label.GetComponentInParent<Button>(true);
        if (readyButton == null)
        {
            readyButton = label.gameObject.AddComponent<Button>();
            readyButton.targetGraphic = label;
        }

        ConfigureOnce();
    }

    private void Awake()
    {
        ConfigureOnce();
    }

    private void ConfigureOnce()
    {
        if (configured || readyButton == null)
        {
            return;
        }

        configured = true;
        panelImage = GetComponent<Image>();
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.loopPointReached += HandleAnimationCompleted;
        videoPlayer.errorReceived += HandleVideoError;
        readyButton.onClick.AddListener(() => ReadyPressed?.Invoke());
    }

    public void Begin()
    {
        ConfigureOnce();
        if (cockpit != null) cockpit.ResetIndicators();
        HideLegacyOverlay("Sea");
        HideLegacyOverlay("Deck");
        HideLegacyOverlay("Title");
        HideLegacyOverlay("Cable");
        HideLegacyOverlay("LaunchRosette");

        readyLabel.text = "READY";
        readyButton.gameObject.SetActive(false);

        if (panelImage != null)
        {
            panelImage.enabled = false;
        }

        if (useStreamingUrl)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.clip = null;
            videoPlayer.url = BuildStreamingVideoUrl();
            videoPlayer.Prepare();
            return;
        }

        Debug.LogWarning("Rosette entry video URL playback is disabled; enable Use Streaming Url for WebGL builds.");
        ShowReadyButton();
    }

    private string BuildStreamingVideoUrl()
    {
        string relativePath = string.IsNullOrWhiteSpace(streamingVideoPath)
            ? StreamingVideoPath
            : streamingVideoPath.TrimStart('/', '\\');
        return Application.streamingAssetsPath.TrimEnd('/', '\\') + "/" + relativePath.Replace('\\', '/');
    }

    private void HandleVideoError(VideoPlayer failedPlayer, string message)
    {
        Debug.LogError($"Rosette entry video could not be loaded from '{failedPlayer.url}': {message}");
        if (animationSurface != null)
        {
            animationSurface.texture = null;
        }
        ShowReadyButton();
    }

    private void HandlePrepared(VideoPlayer preparedPlayer)
    {
        if (animationSurface != null)
        {
            animationSurface.texture = preparedPlayer.texture;
        }

        preparedPlayer.Play();
    }

    private void HandleAnimationCompleted(VideoPlayer completedPlayer)
    {
        // Do not call Stop(): leaving the player paused at the end preserves the
        // completed animation's final frame behind the Ready button.
        completedPlayer.Pause();
        ShowReadyButton();
    }

    private void ShowReadyButton()
    {
        readyButton.gameObject.SetActive(true);
        readyLabel.text = "READY";
    }

    private void HideLegacyOverlay(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }
}
