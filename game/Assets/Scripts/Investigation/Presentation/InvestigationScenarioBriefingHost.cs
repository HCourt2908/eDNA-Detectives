using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    // Owns an inert visual snapshot, not the live controls. Keeping the real
    // panel in place avoids reparenting conflicts during rebuilds/scene unloads.
    public sealed class InvestigationScenarioBriefingHost : MonoBehaviour
    {
        private Action build;
        private bool pending = true;
        private RectTransform overlay;
        private CanvasGroup sourceVisibility;
        private float sourceAlpha;
        public void Configure(Action create) { build = create; pending = true; }
        public void Own(RectTransform presentation) => overlay = presentation;
        public RectTransform Snapshot(RectTransform source, RectTransform presentation, float? restoreAlpha = null)
        {
            overlay = presentation;
            RectTransform snapshot = CopyVisual(source, presentation, true);
            sourceVisibility = source.GetComponent<CanvasGroup>();
            if (sourceVisibility == null) sourceVisibility = source.gameObject.AddComponent<CanvasGroup>();
            sourceAlpha = restoreAlpha ?? sourceVisibility.alpha;
            sourceVisibility.alpha = 0f;
            return snapshot;
        }
        private static RectTransform CopyVisual(RectTransform source, Transform parent, bool root)
        {
            RectTransform copy = new GameObject((root ? "Scenario Briefing Focus " : "Guide Copy ") + source.name, typeof(RectTransform)).GetComponent<RectTransform>();
            copy.SetParent(parent, false);
            copy.anchorMin = source.anchorMin; copy.anchorMax = source.anchorMax; copy.pivot = source.pivot;
            copy.sizeDelta = source.sizeDelta; copy.anchoredPosition = source.anchoredPosition;
            copy.localRotation = source.localRotation; copy.localScale = source.localScale;
            Graphic original = source.GetComponent<Graphic>();
            Graphic graphic = null;
            if (original is TextMeshProUGUI text)
            {
                TextMeshProUGUI label = copy.gameObject.AddComponent<TextMeshProUGUI>();
                label.font = text.font; label.fontSize = text.fontSize; label.fontStyle = text.fontStyle;
                label.text = text.text; label.alignment = text.alignment; label.lineSpacing = text.lineSpacing;
                label.richText = text.richText; label.textWrappingMode = text.textWrappingMode;
                label.overflowMode = text.overflowMode; label.enableAutoSizing = false;
                label.fontSharedMaterial = text.fontSharedMaterial; graphic = label;
            }
            else if (original is RawImage raw)
            {
                RawImage image = copy.gameObject.AddComponent<RawImage>();
                image.texture = raw.texture; image.uvRect = raw.uvRect; graphic = image;
            }
            else if (original is Image image)
            {
                Image picture = copy.gameObject.AddComponent<Image>();
                picture.sprite = image.sprite; picture.type = image.type; picture.preserveAspect = image.preserveAspect;
                picture.fillCenter = image.fillCenter; picture.fillMethod = image.fillMethod; picture.fillAmount = image.fillAmount;
                picture.fillOrigin = image.fillOrigin; picture.fillClockwise = image.fillClockwise;
                picture.pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier; graphic = picture;
            }
            if (graphic != null)
            {
                // Freshly rebuilt graphics may not have a renderer tint yet.
                // Copy authored colour; an uninitialized tint can hide sprites.
                graphic.color = original.color;
                Selectable control = source.GetComponent<Selectable>();
                if (control != null && !control.IsInteractable()) graphic.color *= control.colors.disabledColor;
                graphic.material = original.material; graphic.raycastTarget = false;
            }
            CanvasGroup originalGroup = source.GetComponent<CanvasGroup>();
            if (originalGroup != null)
            {
                CanvasGroup group = copy.gameObject.AddComponent<CanvasGroup>();
                group.alpha = root ? 1f : originalGroup.alpha; group.blocksRaycasts = false; group.interactable = false;
            }
            if (source.GetComponent<RectMask2D>() != null) copy.gameObject.AddComponent<RectMask2D>();
            foreach (Transform child in source)
                if (child.gameObject.activeSelf && child.GetComponent<TMP_SubMeshUI>() == null && child is RectTransform rect) CopyVisual(rect, copy, false);
            return copy;
        }
        private void OnEnable() => pending = true;
        private void LateUpdate()
        {
            if (!pending || build == null) return;
            pending = false; build();
        }
        private void OnDisable() => ClearPresentation();
        public void ClearPresentation()
        {
            if (sourceVisibility != null) sourceVisibility.alpha = sourceAlpha;
            sourceVisibility = null;
            if (overlay != null) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); }
            overlay = null;
        }
    }
}
