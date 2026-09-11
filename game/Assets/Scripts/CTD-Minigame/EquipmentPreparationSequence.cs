using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the preparation/loading page. It references scene UI only; its layout is
/// intentionally authored in the Canvas and can be adjusted without editing code.
/// </summary>
public class EquipmentPreparationSequence : MonoBehaviour
{
    [Header("Scene objects")]
    public TMP_Text statusText;
    public TMP_Text tipText;
    public Image progressFill;
    public Button readyButton;

    [Header("Timing")]
    [Min(0.1f)] public float preparationDuration = 4f;

    public event Action ReadyPressed;

    private readonly string[] tips =
    {
        "Clean sampling equipment prevents contamination from earlier water samples.",
        "A CTD records conductivity, temperature, and depth throughout a cast.",
        "Niskin bottles seal at target depths to preserve an accurate water sample."
    };

    private void Awake()
    {
        readyButton.onClick.AddListener(() => ReadyPressed?.Invoke());
    }

    public void Begin(bool manualCleaningWillFollow)
    {
        StopAllCoroutines();
        readyButton.gameObject.SetActive(false);
        progressFill.fillAmount = 0f;
        statusText.text = "PREPARING YOUR EQUIPMENT";
        StartCoroutine(Prepare(manualCleaningWillFollow));
    }

    private IEnumerator Prepare(bool manualCleaningWillFollow)
    {
        float elapsed = 0f;
        int shownTip = -1;

        while (elapsed < preparationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / preparationDuration);
            progressFill.fillAmount = progress;

            int tipIndex = Mathf.Min(tips.Length - 1, Mathf.FloorToInt(progress * tips.Length));
            if (tipIndex != shownTip)
            {
                shownTip = tipIndex;
                tipText.text = tips[shownTip];
            }

            yield return null;
        }

        progressFill.fillAmount = 1f;
        statusText.text = manualCleaningWillFollow
            ? "EQUIPMENT CHECK READY"
            : "EQUIPMENT READY";
        tipText.text = manualCleaningWillFollow
            ? "One Niskin bottle still needs your first manual decontamination check."
            : "All equipment was cleaned automatically during preparation.";
        readyButton.gameObject.SetActive(true);
    }
}
