using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CleaningMinigame : MonoBehaviour
{
    public const string TutorialCompleteKey = "CTD_CleaningTutorialComplete";

    [Header("Equipment")]
    public CleaningTarget[] targets;
    public GameObject manualCleaningGroup;
    public GameObject quickCleaningGroup;

    [Header("UI")]
    public TMP_Text instructionText;
    public TMP_Text explanationText;
    public Button quickCleanButton;
    public Button continueButton;

    public event Action Completed;

    private bool hasCompleted;
    private bool hasSelectedTool;
    private CleaningToolType selectedTool;

    private void Awake()
    {
        if (quickCleanButton != null)
        {
            quickCleanButton.onClick.AddListener(QuickClean);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Continue);
        }
    }

    public void Begin()
    {
        hasCompleted = false;
        hasSelectedTool = false;

        foreach (CleaningTarget target in targets)
        {
            target.ResetTarget();
        }

        bool tutorialComplete = PlayerPrefs.GetInt(TutorialCompleteKey, 0) == 1;
        manualCleaningGroup.SetActive(!tutorialComplete);
        quickCleaningGroup.SetActive(tutorialComplete);
        continueButton.gameObject.SetActive(false);

        instructionText.text = tutorialComplete
            ? "Equipment check: press CLEAN ALL to prepare the sampling kit."
            : "Click a tool, then click equipment — or drag the tool across it.";

        explanationText.text = "Clean equipment prevents DNA left by an earlier sample from changing our results.";
    }

    public void ApplyTool(CleaningToolType toolType, Vector2 screenPosition, float deltaTime)
    {
        if (hasCompleted)
        {
            return;
        }

        bool touchedTarget = false;

        foreach (CleaningTarget target in targets)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(target.targetRect, screenPosition))
            {
                continue;
            }

            touchedTarget = true;

            if (toolType == CleaningToolType.DecontaminationSolution)
            {
                target.ApplyCleaning(deltaTime);
                instructionText.text = $"Cleaning {target.displayName} removes leftover DNA.";
            }
            else if (target.ApplyRinse(deltaTime))
            {
                instructionText.text = $"Rinsing {target.displayName} removes the cleaning solution.";
            }
            else if (!target.IsCleaned)
            {
                instructionText.text = $"Clean {target.displayName} before rinsing it.";
            }
        }

        if (!touchedTarget)
        {
            instructionText.text = "Move the tool across one of the three pieces of equipment.";
        }

        CheckCompletion();
    }

    public void SelectTool(CleaningToolType toolType)
    {
        if (hasCompleted)
        {
            return;
        }

        selectedTool = toolType;
        hasSelectedTool = true;
        instructionText.text = toolType == CleaningToolType.DecontaminationSolution
            ? "Cleaning solution selected. Click an item or drag the tool across it."
            : "Sterile water selected. Click an already-cleaned item to rinse it.";
    }

    public void HandleTargetClick(CleaningTarget target)
    {
        if (hasCompleted)
        {
            return;
        }

        if (!hasSelectedTool)
        {
            instructionText.text = "Choose CLEANING SOLUTION or STERILE WATER first.";
            return;
        }

        if (selectedTool == CleaningToolType.DecontaminationSolution)
        {
            target.ApplyCleaning(target.cleanSeconds);
            instructionText.text = $"{target.displayName} cleaned. Select sterile water to rinse it.";
        }
        else if (target.ApplyRinse(target.rinseSeconds))
        {
            instructionText.text = $"{target.displayName} rinsed and ready. ✓";
        }
        else if (!target.IsCleaned)
        {
            instructionText.text = $"Clean {target.displayName} before rinsing it.";
        }

        CheckCompletion();
    }

    private void QuickClean()
    {
        foreach (CleaningTarget target in targets)
        {
            target.CompleteImmediately();
        }

        instructionText.text = "All equipment is clean and ready. ✓";
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        foreach (CleaningTarget target in targets)
        {
            if (!target.IsComplete)
            {
                return;
            }
        }

        if (hasCompleted)
        {
            return;
        }

        hasCompleted = true;
        PlayerPrefs.SetInt(TutorialCompleteKey, 1);
        PlayerPrefs.Save();
        instructionText.text = "Sampling equipment prepared successfully. ✓";
        continueButton.gameObject.SetActive(true);
    }

    private void Continue()
    {
        if (hasCompleted)
        {
            Completed?.Invoke();
        }
    }
}
