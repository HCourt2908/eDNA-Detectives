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
    private const string VideoResourcePath = "RosetteDeployment/Launch/rosette_entry_ocean_animation";

    [Header("Scene UI")]
    public RawImage animationSurface;
    public TMP_Text readyLabel;
    public Button readyButton;

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

        readyButton = label.GetComponentInParent<Button>();
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
        readyButton.onClick.AddListener(() => ReadyPressed?.Invoke());
    }

    public void Begin()
    {
        ConfigureOnce();
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

        VideoClip clip = Resources.Load<VideoClip>(VideoResourcePath);
        if (clip == null)
        {
            Debug.LogError($"Rosette entry video is missing from Resources/{VideoResourcePath}.");
            ShowReadyButton();
            return;
        }

        videoPlayer.clip = clip;
        videoPlayer.frame = 0;
        if (animationSurface != null)
        {
            animationSurface.texture = null;
            animationSurface.enabled = true;
        }

        videoPlayer.Prepare();
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
