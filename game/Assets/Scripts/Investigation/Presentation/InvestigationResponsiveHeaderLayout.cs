using System;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class InvestigationResponsiveHeaderLayout : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private RectTransform eyebrow;
        [SerializeField] private RectTransform progressRoot;
        [SerializeField, Min(480f)] private float compactBreakpoint = 760f;

        private string accessibleTitle = string.Empty;
        private bool applyingLayout;

        public bool IsCompact { get; private set; }
        public string AccessibleTitle => accessibleTitle;

        public void ConfigureReferences(Text title, RectTransform eyebrowRect, RectTransform progress)
        {
            titleText = title;
            eyebrow = eyebrowRect;
            progressRoot = progress;
            if (string.IsNullOrEmpty(accessibleTitle) && titleText != null)
            {
                accessibleTitle = titleText.text;
            }
            ApplyNow();
        }

        public void SetTitle(string title)
        {
            accessibleTitle = title ?? string.Empty;
            ApplyNow();
        }

        public void ApplyNow()
        {
            if (applyingLayout) return;
            applyingLayout = true;
            try
            {
                float width = ((RectTransform)transform).rect.width;
                IsCompact = width > 0f && width < compactBreakpoint;
                float titleBoundary = IsCompact ? 0.46f : 0.58f;
                float progressStart = IsCompact ? 0.46f : 0.50f;
                float motionWidth = IsCompact ? 104f : 136f;

                if (titleText != null)
                {
                    titleText.fontSize = IsCompact ? 18 : 21;
                    titleText.text = IsCompact ? CompactTitle(accessibleTitle) : accessibleTitle;
                    SetOffsets(
                        titleText.rectTransform,
                        new Vector2(0f, 0f),
                        new Vector2(titleBoundary, 0.72f),
                        IsCompact ? 14f : 18f,
                        6f,
                        -4f,
                        -2f);
                }

                SetOffsets(
                    eyebrow,
                    new Vector2(0f, 0.72f),
                    new Vector2(titleBoundary, 1f),
                    IsCompact ? 14f : 18f,
                    0f,
                    -4f,
                    -6f);
                SetOffsets(
                    progressRoot,
                    new Vector2(progressStart, 0.12f),
                    new Vector2(1f, 0.86f),
                    IsCompact ? 4f : 8f,
                    0f,
                    -(motionWidth + 18f),
                    0f);

                ConfigureMetricWidths();
                ConfigureMotionButton(motionWidth);
            }
            finally
            {
                applyingLayout = false;
            }
        }

        private void ConfigureMetricWidths()
        {
            if (progressRoot == null) return;
            LayoutElement[] metrics = progressRoot.GetComponentsInChildren<LayoutElement>(true);
            for (int index = 0; index < metrics.Length; index++)
            {
                metrics[index].minWidth = IsCompact ? 36f : 58f;
                metrics[index].preferredWidth = IsCompact ? 48f : 72f;
            }
        }

        private void ConfigureMotionButton(float width)
        {
            Transform motion = transform.Find("Motion Preference");
            if (motion == null) return;
            RectTransform motionRect = motion.GetComponent<RectTransform>();
            motionRect.sizeDelta = new Vector2(width, 44f);
            motionRect.anchoredPosition = new Vector2(IsCompact ? -8f : -12f, 0f);

            InvestigationButtonView button = motion.GetComponent<InvestigationButtonView>();
            if (button == null) return;
            string visibleLabel = IsCompact
                ? InvestigationMotionSettings.ReducedMotion ? "REDUCED" : "MOTION"
                : button.Label;
            button.SetVisualLabel(visibleLabel);
        }

        private static string CompactTitle(string title)
        {
            const string prefix = "eDNA DETECTIVES  /  ";
            return !string.IsNullOrEmpty(title) && title.StartsWith(prefix, StringComparison.Ordinal)
                ? $"eDNA  /  {title.Substring(prefix.Length)}"
                : title;
        }

        private void OnEnable()
        {
            ApplyNow();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) ApplyNow();
        }

        private void OnTransformChildrenChanged()
        {
            if (isActiveAndEnabled) ApplyNow();
        }

        private static void SetOffsets(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float left,
            float bottom,
            float right,
            float top)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }
    }
}
