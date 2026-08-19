using System;
using System.Collections;
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
    private bool isAutoCleaning;
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
        StopAllCoroutines();
        hasCompleted = false;
        hasSelectedTool = false;
        isAutoCleaning = false;

        foreach (CleaningTarget target in targets)
        {
            target.ResetTarget();
        }

        manualCleaningGroup.SetActive(true);
        quickCleaningGroup.SetActive(true);
        quickCleanButton.interactable = true;
        continueButton.gameObject.SetActive(false);

        instructionText.text = "Click a tool, then click equipment — or drag the tool across it.";

        explanationText.text = "Clean equipment prevents DNA left by an earlier sample from changing our results.";
    }

    public void ApplyTool(CleaningToolType toolType, Vector2 screenPosition, float deltaTime)
    {
        if (hasCompleted || isAutoCleaning)
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
        if (hasCompleted || isAutoCleaning)
        {
            return;
        }

        selectedTool = toolType;
        hasSelectedTool = true;
        instructionText.text = toolType == CleaningToolType.DecontaminationSolution
            ? "Cleaning sponge selected. Click an item or drag the sponge across it."
            : "Sterile water selected. Click an already-cleaned item to rinse it.";
    }

    public void HandleTargetClick(CleaningTarget target)
    {
        if (hasCompleted || isAutoCleaning)
        {
            return;
        }

        if (!hasSelectedTool)
        {
            instructionText.text = "Choose the CLEANING SPONGE or STERILE WATER first.";
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
        if (hasCompleted || isAutoCleaning)
        {
            return;
        }

        StartCoroutine(AutoCleanAndContinue());
    }

    private IEnumerator AutoCleanAndContinue()
    {
        isAutoCleaning = true;
        hasSelectedTool = false;
        quickCleanButton.interactable = false;
        continueButton.gameObject.SetActive(false);

        instructionText.text = "Skip selected — automatically cleaning all three items...";
        yield return AnimateAutomaticStep(true, 0.55f);

        instructionText.text = "Automatically rinsing away the cleaning solution...";
        yield return AnimateAutomaticStep(false, 0.55f);

        foreach (CleaningTarget target in targets)
        {
            target.CompleteImmediately();
        }

        CompleteCleaning(false);
        instructionText.text = "Auto-clean complete. Opening the station map... ✓";
        yield return new WaitForSeconds(0.45f);

        Completed?.Invoke();
    }

    private IEnumerator AnimateAutomaticStep(bool cleaning, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float frameTime = Mathf.Min(Time.deltaTime, duration - elapsed);
            elapsed += frameTime;

            foreach (CleaningTarget target in targets)
            {
                if (cleaning)
                {
                    target.ApplyCleaning(target.cleanSeconds * frameTime / duration);
                }
                else
                {
                    target.ApplyRinse(target.rinseSeconds * frameTime / duration);
                }
            }

            yield return null;
        }

        foreach (CleaningTarget target in targets)
        {
            if (cleaning)
            {
                target.ApplyCleaning(target.cleanSeconds);
            }
            else
            {
                target.ApplyRinse(target.rinseSeconds);
            }
        }
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

        CompleteCleaning(true);
    }

    private void CompleteCleaning(bool showContinueButton)
    {
        hasCompleted = true;
        isAutoCleaning = false;
        PlayerPrefs.SetInt(TutorialCompleteKey, 1);
        PlayerPrefs.Save();
        quickCleaningGroup.SetActive(false);
        instructionText.text = "Sampling equipment prepared successfully. ✓";
        continueButton.gameObject.SetActive(showContinueButton);
    }

    private void Continue()
    {
        if (hasCompleted)
        {
            Completed?.Invoke();
        }
    }
}
