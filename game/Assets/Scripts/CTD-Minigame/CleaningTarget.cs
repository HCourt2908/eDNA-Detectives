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
    private readonly Color dirtyEquipmentColor = new Color(0.30f, 0.32f, 0.34f, 1f);

    private void Awake()
    {
        if (minigame == null)
        {
            minigame = GetComponentInParent<CleaningMinigame>();
        }

        if (equipmentImage != null)
        {
            originalEquipmentColor = equipmentImage.color;

            Transform placeholderLabel = equipmentImage.transform.Find("EquipmentName");
            if (placeholderLabel != null)
            {
                placeholderLabel.gameObject.SetActive(false);
            }
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

        if (dirtyOverlay != null)
        {
            dirtyOverlay.gameObject.SetActive(false);
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
            dirtyOverlay.gameObject.SetActive(false);
        }

        if (equipmentImage != null)
        {
            Color cleanColour = originalEquipmentColor == default ? Color.white : originalEquipmentColor;
            equipmentImage.color = Color.Lerp(dirtyEquipmentColor, cleanColour, cleanProgress);
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
                statusText.text = "Use the cleaning sponge";
                statusText.color = Color.white;
            }
        }

    }
}
