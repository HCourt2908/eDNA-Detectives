using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum CTDGameState
{
    Intro,
    Cleaning,
    Planning,
    Launching,
    Sampling,
    Recovering,
    Complete
}

public class CTDGameManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject introPanel;
    public GameObject cleaningPanel;
    public GameObject planningPanel;
    public GameObject launchPanel;
    public GameObject samplingPanel;
    public GameObject recoveryPanel;
    public GameObject completePanel;

    [Header("Flow")]
    public Button beginButton;
    public CleaningMinigame cleaningMinigame;
    public Button[] locationButtons;
    public TMP_Text selectedLocationText;
    public Button deployButton;
    public CTDSamplingController samplingController;

    [Header("Transitions")]
    public RectTransform launchRosette;
    public RectTransform recoveryRosette;
    public TMP_Text transitionStatusText;
    public TMP_Text completionSummaryText;
    public Button replayButton;
    public Button continueButton;
    public string dnaSceneName = "Petri-Dish-Game";

    public CTDGameState CurrentState { get; private set; }

    private readonly GameObject[] panels = new GameObject[7];
    private string selectedLocation = "Station A";
    private CTDSampleRecord[] completedSamples;

    private void Awake()
    {
        panels[0] = introPanel;
        panels[1] = cleaningPanel;
        panels[2] = planningPanel;
        panels[3] = launchPanel;
        panels[4] = samplingPanel;
        panels[5] = recoveryPanel;
        panels[6] = completePanel;

        beginButton.onClick.AddListener(BeginMission);
        cleaningMinigame.Completed += OpenPlanning;
        deployButton.onClick.AddListener(BeginLaunch);
        samplingController.SamplingCompleted += BeginRecovery;
        replayButton.onClick.AddListener(Replay);
        continueButton.onClick.AddListener(ContinueToDNA);

        for (int index = 0; index < locationButtons.Length; index++)
        {
            int capturedIndex = index;
            locationButtons[index].onClick.AddListener(() => SelectLocation(capturedIndex));
        }
    }

    private void Start()
    {
        ShowPanel(introPanel);
        CurrentState = CTDGameState.Intro;
    }

    private void BeginMission()
    {
        CurrentState = CTDGameState.Cleaning;
        ShowPanel(cleaningPanel);
        cleaningMinigame.Begin();
    }

    private void OpenPlanning()
    {
        CurrentState = CTDGameState.Planning;
        ShowPanel(planningPanel);
        SelectLocation(0);
    }

    private void SelectLocation(int index)
    {
        selectedLocation = $"Station {(char)('A' + index)}";
        selectedLocationText.text = $"Selected: {selectedLocation}\nTargets: Deep 820 m  •  Midwater 500 m  •  Surface 180 m";

        for (int buttonIndex = 0; buttonIndex < locationButtons.Length; buttonIndex++)
        {
            ColorBlock colours = locationButtons[buttonIndex].colors;
            colours.normalColor = buttonIndex == index
                ? new Color(0.18f, 0.76f, 0.72f)
                : new Color(0.12f, 0.28f, 0.40f);
            locationButtons[buttonIndex].colors = colours;
        }
    }

    private void BeginLaunch()
    {
        CurrentState = CTDGameState.Launching;
        ShowPanel(launchPanel);
        StartCoroutine(PlayLaunch());
    }

    private IEnumerator PlayLaunch()
    {
        Vector2 start = new Vector2(0f, 210f);
        Vector2 end = new Vector2(0f, -220f);
        launchRosette.anchoredPosition = start;
        transitionStatusText.text = "Deck crew secured — lowering the CTD rosette";

        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            launchRosette.anchoredPosition = Vector2.Lerp(start, end, progress);
            yield return null;
        }

        CurrentState = CTDGameState.Sampling;
        ShowPanel(samplingPanel);
        samplingController.Begin(selectedLocation);
    }

    private void BeginRecovery(CTDSampleRecord[] samples)
    {
        completedSamples = samples;
        CurrentState = CTDGameState.Recovering;
        ShowPanel(recoveryPanel);
        StartCoroutine(PlayRecovery());
    }

    private IEnumerator PlayRecovery()
    {
        Vector2 start = new Vector2(0f, -220f);
        Vector2 end = new Vector2(0f, 210f);
        recoveryRosette.anchoredPosition = start;

        float duration = 2.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            recoveryRosette.anchoredPosition = Vector2.Lerp(start, end, progress);
            yield return null;
        }

        ShowCompletion();
    }

    private void ShowCompletion()
    {
        CurrentState = CTDGameState.Complete;
        ShowPanel(completePanel);

        StringBuilder summary = new StringBuilder();
        summary.AppendLine("CTD CAST COMPLETE  ✓\n");

        foreach (CTDSampleRecord sample in completedSamples)
        {
            summary.AppendLine($"{sample.bottleId}  •  {sample.location}  •  {sample.zone}");
            summary.AppendLine($"Collected at {sample.actualDepth} m  •  Quality: {sample.quality}\n");
        }

        summary.Append("The water samples are ready for eDNA analysis.");
        completionSummaryText.text = summary.ToString();
    }

    private void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ContinueToDNA()
    {
        if (Application.CanStreamedLevelBeLoaded(dnaSceneName))
        {
            SceneManager.LoadScene(dnaSceneName);
        }
        else
        {
            completionSummaryText.text += "\n\nDNA scene is not in Build Settings yet.";
            continueButton.interactable = false;
        }
    }

    private void ShowPanel(GameObject panelToShow)
    {
        foreach (GameObject panel in panels)
        {
            panel.SetActive(panel == panelToShow);
        }
    }
}
