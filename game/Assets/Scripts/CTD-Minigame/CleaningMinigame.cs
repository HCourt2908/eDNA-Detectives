using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CleaningMinigame : MonoBehaviour
{
    [Header("Equipment")]
    public CleaningTarget[] targets;
    public GameObject manualCleaningGroup;
    public GameObject quickCleaningGroup;
    public CleaningTool spongeTool;
    public CleaningTool hoseTool;

    [Header("UI")]
    public TMP_Text instructionText;
    public TMP_Text explanationText;
    // Kept for the legacy scene builder; the stage no longer auto-cleans.
    public Button quickCleanButton;
    public TMP_Text bottleStatusText;
    public Button rinseButton;
    public Button skipButton;
    public Button continueButton;

    [Header("Foam completion")]
    [Range(0.5f, 1f)] public float foamCoverageToRinse = 0.8f;
    [Range(0f, 0.3f)] public float foamCoverageToClean = 0.08f;

    public event Action Completed;

    private static int cleaningStageEntries;
    private bool hasCompleted;
    private bool rinseEnabled;
    private CleaningToolType selectedTool = CleaningToolType.DecontaminationSolution;

    private void Awake()
    {
        rinseButton.onClick.AddListener(SelectRinse);
        skipButton.onClick.AddListener(SkipCleaning);
        continueButton.onClick.AddListener(Continue);
    }

    public void Begin()
    {
        hasCompleted = false;
        rinseEnabled = false;
        selectedTool = CleaningToolType.DecontaminationSolution;
        cleaningStageEntries++;

        foreach (CleaningTarget target in targets) target.ResetTarget();
        manualCleaningGroup.SetActive(true);
        quickCleaningGroup.SetActive(false);
        if (spongeTool != null) spongeTool.gameObject.SetActive(true);
        if (hoseTool != null) hoseTool.gameObject.SetActive(false);
        rinseButton.gameObject.SetActive(true);
        rinseButton.interactable = false;
        skipButton.gameObject.SetActive(cleaningStageEntries > 1);
        continueButton.gameObject.SetActive(false);
        instructionText.text = "Step 1: Drag the sponge hand across the Niskin bottle.";
        explanationText.text = "Scrub the bottle until foam covers most of its surface.";
        bottleStatusText.text = "KEEP SCRUBBING THE BOTTLE";
        bottleStatusText.color = Color.white;
    }

    public void ApplyTool(CleaningToolType toolType, Vector2 screenPosition, float deltaTime)
    {
        if (hasCompleted) return;
        foreach (CleaningTarget target in targets)
        {
            if (!target.IsScreenPointOnBottle(screenPosition)) continue;
            if (toolType == CleaningToolType.DecontaminationSolution)
            {
                if (rinseEnabled) continue;
                target.ApplyFoamAt(screenPosition, deltaTime);
                UpdateFoamState(target);
            }
            else if (!rinseEnabled)
            {
                instructionText.text = "Keep scrubbing the bottle.";
            }
            else
            {
                target.RinseAt(screenPosition, deltaTime);
                UpdateRinseState(target);
            }
        }
    }

    public void SelectTool(CleaningToolType toolType)
    {
        if (hasCompleted) return;
        if (toolType == CleaningToolType.SterileWater && !rinseEnabled)
        {
            instructionText.text = "Keep scrubbing the bottle.";
            return;
        }
        selectedTool = toolType;
    }

    public bool IsToolActive(CleaningToolType toolType) => selectedTool == toolType;
    public bool IsRinsing => selectedTool == CleaningToolType.SterileWater;
    public CleaningToolType ActiveTool => selectedTool;

    private void UpdateFoamState(CleaningTarget target)
    {
        if (target.FoamCoverage < foamCoverageToRinse)
        {
            bottleStatusText.text = "KEEP SCRUBBING THE BOTTLE";
            return;
        }
        rinseEnabled = true;
        if (spongeTool != null) spongeTool.gameObject.SetActive(false);
        if (hoseTool != null) hoseTool.gameObject.SetActive(true);
        selectedTool = CleaningToolType.SterileWater;
        rinseButton.interactable = true;
        bottleStatusText.text = "READY TO RINSE";
        bottleStatusText.color = new Color(1f, 0.9f, 0.2f);
        instructionText.text = "Step 2: Drag the mirrored hose hand across the bottle to rinse.";
        explanationText.text = "Use sterile water to remove the foam from the bottle.";
    }

    private void SelectRinse()
    {
        if (!rinseEnabled)
        {
            instructionText.text = "Keep scrubbing the bottle.";
            return;
        }
        selectedTool = CleaningToolType.SterileWater;
        if (hoseTool != null) hoseTool.gameObject.SetActive(true);
        bottleStatusText.text = "RINSE THE FOAM AWAY";
        bottleStatusText.color = new Color(0.35f, 0.85f, 1f);
    }

    private void UpdateRinseState(CleaningTarget target)
    {
        if (target.FoamCoverage > foamCoverageToClean) return;
        hasCompleted = true;
        bottleStatusText.text = "BOTTLE CLEAN";
        bottleStatusText.color = new Color(0.35f, 1f, 0.7f);
        instructionText.text = "Bottle clean. Continue to deploy the rosette.";
        rinseButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(true);
    }

    private void SkipCleaning()
    {
        if (hasCompleted) return;
        foreach (CleaningTarget target in targets) target.ClearFoam();
        hasCompleted = true;
        if (spongeTool != null) spongeTool.gameObject.SetActive(false);
        if (hoseTool != null) hoseTool.gameObject.SetActive(false);
        bottleStatusText.text = "BOTTLE CLEAN";
        bottleStatusText.color = new Color(0.35f, 1f, 0.7f);
        instructionText.text = "Cleaning skipped for this repeat attempt.";
        rinseButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(true);
    }

    private void Continue()
    {
        if (hasCompleted) Completed?.Invoke();
    }
}
