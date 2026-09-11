using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum CTDGameState
{
    Map,
    Preparation,
    Cleaning,
    Launching,
    Sampling,
    Recovering,
    Complete
}

/// <summary>
/// Owns the stage sequence. UI composition is deliberately kept in the scene;
/// this component switches screens and runs only gameplay behaviour.
/// </summary>
public class CTDGameManager : MonoBehaviour
{
    [Header("Stage roots authored in the scene")]
    public RosetteMapHub mapHub;
    public GameObject preparationPanel;
    public GameObject cleaningPanel;
    public GameObject launchPanel;
    public GameObject samplingPanel;
    public GameObject recoveryPanel;
    public GameObject completePanel;

    [Header("Stage behaviour")]
    public EquipmentPreparationSequence preparationSequence;
    public CleaningMinigame cleaningMinigame;
    public CTDSamplingController samplingController;

    [Header("Rosette entry animation")]
    public RosetteEntrySequence rosetteEntrySequence;
    public TMP_Text transitionStatusText;

    [Header("Completion")]
    public RectTransform recoveryRosette;
    [Header("Recovery animation")]
    public Vector2 recoveryStartPosition = new Vector2(0f, -220f);
    public Vector2 recoveryEndPosition = new Vector2(0f, 210f);
    [Min(0.1f)] public float recoveryDuration = 2.6f;
    public Vector3 recoveryStartScale = Vector3.one;
    public Vector3 recoveryEndScale = Vector3.one;
    public AnimationCurve recoveryMotion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public TMP_Text completionSummaryText;
    public Button replayButton;
    public Button continueButton;
    public string dnaSceneName = "Petri-Dish-Game";

    public CTDGameState CurrentState { get; private set; }

    private CTDSampleRecord[] completedSamples;
    private int selectedLocationIndex;

    private void Awake()
    {
        mapHub.ReadyToDeploy += BeginPreparation;
        preparationSequence.ReadyPressed += ContinueFromPreparation;
        cleaningMinigame.Completed += BeginLaunch;
        if (rosetteEntrySequence == null)
        {
            rosetteEntrySequence = launchPanel.GetComponent<RosetteEntrySequence>();
            if (rosetteEntrySequence == null)
            {
                rosetteEntrySequence = launchPanel.AddComponent<RosetteEntrySequence>();
            }
        }

        rosetteEntrySequence.Configure(transitionStatusText);
        rosetteEntrySequence.ReadyPressed += BeginSampling;
        samplingController.SamplingCompleted += BeginRecovery;
        replayButton.onClick.AddListener(Replay);
        continueButton.onClick.AddListener(ContinueToDNA);
    }

    private void Start()
    {
        ShowMap();
    }

    private void ShowMap()
    {
        CurrentState = CTDGameState.Map;
        HideAllStagePanels();
        mapHub.Show();
    }

    private void BeginPreparation(int locationIndex)
    {
        selectedLocationIndex = locationIndex;
        CurrentState = CTDGameState.Preparation;
        ShowPanel(preparationPanel);
        preparationSequence.Begin(true);
    }

    private void ContinueFromPreparation()
    {
        CurrentState = CTDGameState.Cleaning;
        ShowPanel(cleaningPanel);
        cleaningMinigame.Begin();
    }

    private void BeginLaunch()
    {
        CurrentState = CTDGameState.Launching;
        ShowPanel(launchPanel);
        rosetteEntrySequence.Begin();
    }

    private void BeginSampling()
    {
        CurrentState = CTDGameState.Sampling;
        ShowPanel(samplingPanel);
        samplingController.Begin($"Waypoint-{selectedLocationIndex + 1:00}");
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
        recoveryRosette.anchoredPosition = recoveryStartPosition;
        recoveryRosette.localScale = recoveryStartScale;
        float elapsed = 0f;

        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float progress = recoveryMotion.Evaluate(Mathf.Clamp01(elapsed / recoveryDuration));
            recoveryRosette.anchoredPosition = Vector2.LerpUnclamped(recoveryStartPosition, recoveryEndPosition, progress);
            recoveryRosette.localScale = Vector3.LerpUnclamped(recoveryStartScale, recoveryEndScale, progress);
            yield return null;
        }

        recoveryRosette.anchoredPosition = recoveryEndPosition;
        recoveryRosette.localScale = recoveryEndScale;

        ShowCompletion();
    }

    private void ShowCompletion()
    {
        CurrentState = CTDGameState.Complete;
        ShowPanel(completePanel);
        StringBuilder summary = new StringBuilder("CTD CAST COMPLETE  ✓\n\n");

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
        HideAllStagePanels();
        panelToShow.SetActive(true);
    }

    private void HideAllStagePanels()
    {
        mapHub.Hide();
        preparationPanel.SetActive(false);
        cleaningPanel.SetActive(false);
        launchPanel.SetActive(false);
        samplingPanel.SetActive(false);
        recoveryPanel.SetActive(false);
        completePanel.SetActive(false);
    }
}
