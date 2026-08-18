using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CTDSamplingController : MonoBehaviour
{
    [Header("Depth")]
    public float maximumDepth = 1000f;
    public float descentSpeed = 180f;
    public float ascentSpeed = 65f;
    public float targetTolerance = 95f;
    public int[] targetDepths = { 820, 500, 180 };
    public string[] targetZones = { "Deep", "Midwater", "Surface" };

    [Header("UI")]
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

    public event Action<CTDSampleRecord[]> SamplingCompleted;

    private readonly Color surfaceColour = new Color(0.08f, 0.54f, 0.79f);
    private readonly Color deepColour = new Color(0.015f, 0.055f, 0.17f);
    private readonly CTDSampleRecord[] samples = new CTDSampleRecord[3];

    private float currentDepth;
    private int currentTargetIndex;
    private bool descending;
    private bool running;
    private bool tutorialPause;
    private bool firstHintShown;
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

        if (descending)
        {
            currentDepth += descentSpeed * Time.deltaTime;

            if (currentDepth >= maximumDepth)
            {
                currentDepth = maximumDepth;
                descending = false;
                phaseText.text = "UPCAST — collect water as the CTD rises";
                feedbackText.text = "Bottle 01 is open. Watch for the deep-water target.";
                UpdateTargetBand();
            }
        }
        else if (!tutorialPause)
        {
            currentDepth -= ascentSpeed * Time.deltaTime;
            HandleTargetApproach();
        }

        currentDepth = Mathf.Clamp(currentDepth, 0f, maximumDepth);
        RefreshDisplay();
    }

    public void Begin(string location)
    {
        StopAllCoroutines();
        selectedLocation = location;
        currentDepth = 0f;
        currentTargetIndex = 0;
        descending = true;
        running = true;
        tutorialPause = false;
        firstHintShown = false;

        phaseText.text = "DOWNCAST — bottles stay open while water flows through";
        feedbackText.text = "The CTD is recording temperature, salinity, and depth.";
        targetText.text = "Sampling begins during the upcast";
        tutorialHintText.transform.parent.gameObject.SetActive(false);
        closeBottleButton.interactable = false;
        targetBand.gameObject.SetActive(false);

        for (int index = 0; index < bottleImages.Length; index++)
        {
            bottleImages[index].color = new Color(0.74f, 0.86f, 0.91f);
            bottleStatusTexts[index].text = $"Bottle {index + 1:00}: OPEN";
            bottleStatusTexts[index].color = Color.white;
            samples[index] = null;
        }

        RefreshDisplay();
    }

    private void HandleTargetApproach()
    {
        if (currentTargetIndex >= targetDepths.Length)
        {
            return;
        }

        float targetDepth = targetDepths[currentTargetIndex];
        bool insideTarget = Mathf.Abs(currentDepth - targetDepth) <= targetTolerance;
        closeBottleButton.interactable = insideTarget;

        if (insideTarget && currentTargetIndex == 0 && !firstHintShown)
        {
            firstHintShown = true;
            tutorialPause = true;
            tutorialHintText.transform.parent.gameObject.SetActive(true);
            tutorialHintText.text = "The marker is inside the target zone. Press CLOSE BOTTLE now!";
            feedbackText.text = "Tutorial pause: close Bottle 01 to capture deep water.";
        }

        if (currentDepth < targetDepth - targetTolerance)
        {
            currentDepth = targetDepth + targetTolerance * 1.35f;
            feedbackText.text = "The target was missed, so the winch adjusted for another attempt.";
        }
    }

    private void CloseBottle()
    {
        if (!running || descending || currentTargetIndex >= targetDepths.Length)
        {
            return;
        }

        int targetDepth = targetDepths[currentTargetIndex];
        float difference = Mathf.Abs(currentDepth - targetDepth);

        if (difference > targetTolerance)
        {
            feedbackText.text = currentDepth > targetDepth
                ? "Too deep — wait until the marker reaches the target zone."
                : "Too shallow — the target has passed and the winch will adjust.";
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

        bottleImages[currentTargetIndex].color = new Color(0.25f, 0.92f, 0.62f);
        bottleStatusTexts[currentTargetIndex].text = $"{bottleId}: CLOSED  ✓";
        bottleStatusTexts[currentTargetIndex].color = new Color(0.30f, 1f, 0.70f);
        feedbackText.text = $"{bottleId} captured {targetZones[currentTargetIndex].ToLower()} water at {Mathf.RoundToInt(currentDepth)} m.";

        currentTargetIndex++;
        tutorialPause = false;
        tutorialHintText.transform.parent.gameObject.SetActive(false);
        closeBottleButton.interactable = false;

        if (currentTargetIndex >= targetDepths.Length)
        {
            running = false;
            targetBand.gameObject.SetActive(false);
            targetText.text = "All three samples collected";
            phaseText.text = "UPCAST COMPLETE";
            StartCoroutine(FinishAfterDelay());
        }
        else
        {
            UpdateTargetBand();
        }
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
        depthMarker.anchoredPosition = new Vector2(0f, -normalized * gaugeHeight);

        depthText.text = $"{Mathf.RoundToInt(currentDepth)} m";
        float temperature = Mathf.Lerp(22f, 3.5f, normalized);
        float salinity = Mathf.Lerp(34.1f, 35.0f, normalized);
        sensorText.text = $"Temperature  {temperature:0.0} °C\nSalinity         {salinity:0.0} PSU";
        oceanBackground.color = Color.Lerp(surfaceColour, deepColour, normalized);

        if (!descending && currentTargetIndex < targetDepths.Length)
        {
            bool insideTarget = Mathf.Abs(currentDepth - targetDepths[currentTargetIndex]) <= targetTolerance;
            targetBand.GetComponent<Image>().color = insideTarget
                ? new Color(0.26f, 1f, 0.58f, 0.72f)
                : new Color(0.22f, 0.80f, 0.95f, 0.34f);
        }
    }
}
