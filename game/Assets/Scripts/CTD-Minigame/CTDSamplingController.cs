using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CTDSamplingController : MonoBehaviour
{
    [Header("Depth")]
    public float maximumDepth = 1010f;
    public float ascentSpeed = 65f;
    [Min(1f)] public float targetTolerance = 45f;
    public int[] targetDepths = { 820, 500, 180 };
    public string[] targetZones = { "Deep", "Midwater", "Surface" };

    [Header("UI")]
    public SamplingCockpitView cockpit;
    public Image oceanBackground;
    public RectTransform depthGauge;
    public RectTransform depthMarker;
    public RectTransform targetBand;
    public TMP_Text phaseText;
    public TMP_Text depthText;
    public TMP_Text sensorText;
    public TMP_Text targetText;
    public TMP_Text feedbackText;
    public TMP_Text tutorialHintText;
    public Button closeBottleButton;
    public Image[] bottleImages;
    public TMP_Text[] bottleStatusTexts;
    public SamplingOceanBackground oceanVisuals;
    public SamplingBiologyLayer biologyLayer;

    public event Action<CTDSampleRecord[]> SamplingCompleted;

    private CTDSampleRecord[] samples;

    private float currentDepth;
    private int currentTargetIndex;
    private bool running;
    private bool finishStarted;
    private bool wasInsideTarget;
    private string selectedLocation;

    private void Awake()
    {
        closeBottleButton.onClick.AddListener(CloseBottle);
    }

    private void Update()
    {
        if (!running)
        {
            return;
        }

        currentDepth -= ascentSpeed * Time.deltaTime;
        HandleTargetApproach();

        currentDepth = Mathf.Clamp(currentDepth, 0f, maximumDepth);
        RefreshDisplay();
    }

    public void Begin(string location)
    {
        StopAllCoroutines();
        samples = new CTDSampleRecord[targetDepths.Length];
        if (cockpit != null) cockpit.ResetIndicators();
        selectedLocation = location;
        // Stage 3.0 already shows deployment. Stage 3.1 begins at the
        // maximum depth and immediately runs the uninterrupted upcast.
        currentDepth = maximumDepth;
        currentTargetIndex = 0;
        running = true;
        finishStarted = false;
        wasInsideTarget = false;

        phaseText.text = "UPCAST — collect water as the CTD rises";
        feedbackText.text = "Bottle 01 is open. Watch for the deep-water target.";
        tutorialHintText.transform.parent.gameObject.SetActive(false);
        closeBottleButton.interactable = true;

        if (biologyLayer != null)
        {
            biologyLayer.Begin(currentDepth, maximumDepth);
        }

        for (int index = 0; index < samples.Length; index++)
        {
            bottleImages[index].color = new Color(0.74f, 0.86f, 0.91f);
            bottleStatusTexts[index].text = $"Bottle {index + 1:00}: OPEN";
            bottleStatusTexts[index].color = Color.white;
            bottleStatusTexts[index].gameObject.SetActive(false);
            samples[index] = null;
        }

        UpdateTargetBand();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        running = false;
        StopAllCoroutines();
        if (cockpit != null) cockpit.SetCue(false);
    }

    private void HandleTargetApproach()
    {
        if (currentTargetIndex >= targetDepths.Length)
        {
            closeBottleButton.interactable = false;
            return;
        }

        // The CTD keeps moving during the upcast. Once the lower edge of a
        // window passes, that bottle is permanently marked as missed and the
        // next target becomes active. A while loop handles a large frame step
        // without ever reopening an old target.
        while (currentTargetIndex < targetDepths.Length &&
               currentDepth < targetDepths[currentTargetIndex] - targetTolerance)
        {
            RegisterMissedTarget(currentTargetIndex);
            currentTargetIndex++;
            wasInsideTarget = false;
        }

        if (currentTargetIndex >= targetDepths.Length)
        {
            FinishSampling();
            return;
        }

        float targetDepth = targetDepths[currentTargetIndex];
        bool insideTarget = Mathf.Abs(currentDepth - targetDepth) <= targetTolerance;
        closeBottleButton.interactable = running;

        if (insideTarget && !wasInsideTarget)
        {
            feedbackText.text = $"Target window active — close Bottle {currentTargetIndex + 1:00} now.";
        }
        wasInsideTarget = insideTarget;
        UpdateTargetBand();
    }

    private void CloseBottle()
    {
        if (!running || currentTargetIndex >= targetDepths.Length)
        {
            return;
        }

        int targetDepth = targetDepths[currentTargetIndex];
        float difference = Mathf.Abs(currentDepth - targetDepth);

        if (difference > targetTolerance)
        {
            feedbackText.text = currentDepth > targetDepth
                ? "Too deep — wait for the target window."
                : "Too shallow — that target window has passed.";
            return;
        }

        string bottleId = $"Bottle {currentTargetIndex + 1:00}";
        string quality = difference <= targetTolerance * 0.42f ? "Excellent" : "Good";

        samples[currentTargetIndex] = new CTDSampleRecord
        {
            bottleId = bottleId,
            location = selectedLocation,
            zone = targetZones[currentTargetIndex],
            targetDepth = targetDepth,
            actualDepth = Mathf.RoundToInt(currentDepth),
            quality = quality
        };

        if (cockpit != null) cockpit.MarkCollected(currentTargetIndex);
        else bottleImages[currentTargetIndex].color = new Color(0.25f, 0.92f, 0.62f);
        bottleStatusTexts[currentTargetIndex].gameObject.SetActive(true);
        bottleStatusTexts[currentTargetIndex].text = $"{bottleId}: CLOSED  ✓";
        bottleStatusTexts[currentTargetIndex].color = new Color(0.30f, 1f, 0.70f);
        feedbackText.text = $"{bottleId} captured {targetZones[currentTargetIndex].ToLower()} water at {Mathf.RoundToInt(currentDepth)} m.";

        currentTargetIndex++;
        wasInsideTarget = false;
        tutorialHintText.transform.parent.gameObject.SetActive(false);
        if (cockpit != null) cockpit.SetCue(false);

        if (currentTargetIndex >= targetDepths.Length)
        {
            FinishSampling();
        }
        else
        {
            UpdateTargetBand();
        }
        RefreshDisplay();
    }

    private void RegisterMissedTarget(int index)
    {
        if (index < 0 || index >= targetDepths.Length || samples[index] != null)
        {
            return;
        }

        string bottleId = $"Bottle {index + 1:00}";
        samples[index] = new CTDSampleRecord
        {
            bottleId = bottleId,
            location = selectedLocation,
            zone = targetZones[index],
            targetDepth = targetDepths[index],
            actualDepth = Mathf.RoundToInt(currentDepth),
            quality = "Missed window"
        };

        if (cockpit != null) cockpit.MarkFailed(index);
        else bottleImages[index].color = new Color(0.92f, 0.22f, 0.24f);
        bottleStatusTexts[index].gameObject.SetActive(true);
        bottleStatusTexts[index].text = $"{bottleId}: MISSED";
        bottleStatusTexts[index].color = new Color(1f, 0.34f, 0.34f);
        feedbackText.text = $"{bottleId} missed — continue rising to the next target window.";
    }

    private void FinishSampling()
    {
        if (finishStarted)
        {
            return;
        }

        finishStarted = true;
        running = false;
        closeBottleButton.interactable = false;
        targetBand.gameObject.SetActive(false);
        targetText.text = $"All {samples.Length} target windows processed";
        phaseText.text = "UPCAST COMPLETE";
        tutorialHintText.transform.parent.gameObject.SetActive(false);
        if (cockpit != null) cockpit.SetCue(false);
        StartCoroutine(FinishAfterDelay());
    }

    private IEnumerator FinishAfterDelay()
    {
        yield return new WaitForSeconds(1.4f);
        SamplingCompleted?.Invoke(samples);
    }

    private void UpdateTargetBand()
    {
        if (currentTargetIndex >= targetDepths.Length)
        {
            targetBand.gameObject.SetActive(false);
            return;
        }

        targetBand.gameObject.SetActive(true);
        float targetDepth = targetDepths[currentTargetIndex];
        float normalized = targetDepth / maximumDepth;
        float gaugeHeight = depthGauge.rect.height;
        float bandHeight = Mathf.Max(36f, (targetTolerance * 2f / maximumDepth) * gaugeHeight);
        targetBand.sizeDelta = new Vector2(targetBand.sizeDelta.x, bandHeight);
        targetBand.anchoredPosition = new Vector2(0f, -normalized * gaugeHeight);

        targetText.text = $"Next: close Bottle {currentTargetIndex + 1:00} in {targetZones[currentTargetIndex]} water ({targetDepths[currentTargetIndex]} m)";
    }

    private void RefreshDisplay()
    {
        float normalized = currentDepth / maximumDepth;
        float gaugeHeight = depthGauge.rect.height;
        Vector2 markerPosition = depthMarker.anchoredPosition;
        markerPosition.y = -normalized * gaugeHeight;
        depthMarker.anchoredPosition = markerPosition;
        if (cockpit != null) cockpit.ShowDepth(currentDepth, maximumDepth);

        depthText.text = $"{Mathf.RoundToInt(currentDepth)} m";
        float temperature = Mathf.Lerp(22f, 3.5f, normalized);
        float salinity = Mathf.Lerp(34.1f, 35.0f, normalized);
        sensorText.text = $"Temperature  {temperature:0.0} °C\nSalinity         {salinity:0.0} PSU";
        if (oceanVisuals != null)
        {
            oceanVisuals.SetDepth(currentDepth, maximumDepth);
        }
        else if (oceanBackground != null)
        {
            oceanBackground.color = Color.white;
        }

        if (biologyLayer != null)
        {
            biologyLayer.SetDepth(currentDepth, maximumDepth);
        }

        if (currentTargetIndex < targetDepths.Length)
        {
            bool insideTarget = Mathf.Abs(currentDepth - targetDepths[currentTargetIndex]) <= targetTolerance;
            if (cockpit != null) cockpit.SetCue(running && insideTarget);
            else targetBand.GetComponent<Image>().color = insideTarget
                ? new Color(0.26f, 1f, 0.58f, 0.72f)
                : new Color(0.22f, 0.80f, 0.95f, 0.34f);
        }
    }
}
