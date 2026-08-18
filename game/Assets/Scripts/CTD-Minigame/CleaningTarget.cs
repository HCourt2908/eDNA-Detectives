using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CleaningTarget : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    public string displayName;

    [Header("UI")]
    public RectTransform targetRect;
    public Image equipmentImage;
    public Image dirtyOverlay;
    public Image cleanProgressFill;
    public Image rinseProgressFill;
    public TMP_Text statusText;
    public CleaningMinigame minigame;

    [Header("Tuning")]
    [Range(0.1f, 3f)] public float cleanSeconds = 1.25f;
    [Range(0.1f, 3f)] public float rinseSeconds = 0.9f;

    public bool IsComplete => cleanProgress >= 1f && rinseProgress >= 1f;
    public bool IsCleaned => cleanProgress >= 1f;

    private float cleanProgress;
    private float rinseProgress;
    private Color originalEquipmentColor;

    private void Awake()
    {
        if (minigame == null)
        {
            minigame = GetComponentInParent<CleaningMinigame>();
        }

        if (equipmentImage != null)
        {
            originalEquipmentColor = equipmentImage.color;
        }

        ResetTarget();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (minigame != null)
        {
            minigame.HandleTargetClick(this);
        }
    }

    public void ResetTarget()
    {
        cleanProgress = 0f;
        rinseProgress = 0f;

        if (equipmentImage != null)
        {
            equipmentImage.color = originalEquipmentColor == default
                ? new Color(0.72f, 0.78f, 0.82f)
                : originalEquipmentColor;
        }

        if (dirtyOverlay != null)
        {
            dirtyOverlay.color = new Color(0.40f, 0.25f, 0.12f, 0.72f);
        }

        RefreshUI();
    }

    public void ApplyCleaning(float deltaTime)
    {
        if (IsComplete)
        {
            return;
        }

        cleanProgress = Mathf.Clamp01(cleanProgress + deltaTime / cleanSeconds);
        RefreshUI();
    }

    public bool ApplyRinse(float deltaTime)
    {
        if (!IsCleaned || IsComplete)
        {
            return false;
        }

        rinseProgress = Mathf.Clamp01(rinseProgress + deltaTime / rinseSeconds);
        RefreshUI();
        return true;
    }

    public void CompleteImmediately()
    {
        cleanProgress = 1f;
        rinseProgress = 1f;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (cleanProgressFill != null)
        {
            cleanProgressFill.fillAmount = cleanProgress;
        }

        if (rinseProgressFill != null)
        {
            rinseProgressFill.fillAmount = rinseProgress;
        }

        if (dirtyOverlay != null)
        {
            Color dirty = dirtyOverlay.color;
            dirty.a = Mathf.Lerp(0.72f, 0.08f, cleanProgress);
            dirtyOverlay.color = dirty;
        }

        if (statusText != null)
        {
            if (IsComplete)
            {
                statusText.text = "Ready  ✓";
                statusText.color = new Color(0.30f, 0.95f, 0.64f);
            }
            else if (IsCleaned)
            {
                statusText.text = "Rinse with sterile water";
                statusText.color = new Color(0.42f, 0.82f, 1f);
            }
            else
            {
                statusText.text = "Apply cleaning solution";
                statusText.color = Color.white;
            }
        }

        if (IsComplete && equipmentImage != null)
        {
            equipmentImage.color = new Color(0.68f, 1f, 0.90f);
        }
    }
}
