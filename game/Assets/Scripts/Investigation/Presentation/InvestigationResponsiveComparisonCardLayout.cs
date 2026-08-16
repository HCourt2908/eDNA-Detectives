using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(LayoutElement))]
    public sealed class InvestigationResponsiveComparisonCardLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform portrait;
        [SerializeField] private RectTransform speciesName;
        [SerializeField] private RectTransform historicalData;
        [SerializeField] private RectTransform currentData;
        [SerializeField] private RectTransform characteristics;
        [SerializeField] private Text characteristicsText;
        [SerializeField] private RectTransform findingState;
        [SerializeField] private RectTransform characteristicsBackground;
        [SerializeField] private RectTransform actionBackground;
        [SerializeField] private RectTransform timelineDivider;
        [SerializeField, Min(320f)] private float compactBreakpoint = 680f;
        [SerializeField, Min(186f)] private float wideHeight = 186f;
        [SerializeField, Min(240f)] private float compactHeight = 280f;

        private bool applyingLayout;

        public bool IsCompact { get; private set; }
        public float CurrentHeight { get; private set; }

        public void ConfigureReferences(
            RectTransform portraitRect,
            RectTransform nameRect,
            RectTransform historicalRect,
            RectTransform currentRect,
            RectTransform traitsRect,
            Text traitsText,
            RectTransform findingRect,
            RectTransform traitsBackgroundRect,
            RectTransform actionBackgroundRect,
            RectTransform dividerRect)
        {
            portrait = portraitRect;
            speciesName = nameRect;
            historicalData = historicalRect;
            currentData = currentRect;
            characteristics = traitsRect;
            characteristicsText = traitsText;
            findingState = findingRect;
            characteristicsBackground = traitsBackgroundRect;
            actionBackground = actionBackgroundRect;
            timelineDivider = dividerRect;
        }

        public void ApplyNow()
        {
            if (applyingLayout) return;
            applyingLayout = true;
            try
            {
                float width = ((RectTransform)transform).rect.width;
                IsCompact = width > 0f && width < compactBreakpoint;
                CurrentHeight = IsCompact ? compactHeight : wideHeight;
                LayoutElement layout = GetComponent<LayoutElement>();
                layout.minHeight = CurrentHeight;
                layout.preferredHeight = CurrentHeight;

                if (IsCompact)
                {
                    ApplyCompactLayout();
                }
                else
                {
                    ApplyWideLayout();
                }
            }
            finally
            {
                applyingLayout = false;
            }
        }

        private void ApplyWideLayout()
        {
            SetOffsets(portrait, new Vector2(0f, 0f), new Vector2(0f, 1f), 14f, 14f, 170f, -14f);
            SetOffsets(speciesName, new Vector2(0f, 0.72f), new Vector2(0.81f, 1f), 188f, 0f, -14f, -8f);
            SetOffsets(historicalData, new Vector2(0f, 0.36f), new Vector2(0.48f, 0.72f), 188f, 4f, -12f, -3f);
            SetOffsets(currentData, new Vector2(0.48f, 0.36f), new Vector2(0.81f, 0.72f), 12f, 4f, -14f, -3f);
            SetOffsets(characteristics, new Vector2(0f, 0f), new Vector2(0.81f, 0.34f), 198f, 8f, -16f, -7f);
            SetOffsets(characteristicsBackground, new Vector2(0f, 0f), new Vector2(0.81f, 0.34f), 188f, 6f, -14f, -6f);
            SetOffsets(findingState, new Vector2(0.82f, 0.1f), new Vector2(0.985f, 0.9f), 0f, 0f, 0f, 0f);
            SetOffsets(actionBackground, new Vector2(0.82f, 0.1f), new Vector2(0.985f, 0.9f), 0f, 0f, 0f, 0f);
            SetOffsets(timelineDivider, new Vector2(0.48f, 0.39f), new Vector2(0.48f, 0.68f), -1f, 0f, 1f, 0f);
            if (characteristicsText != null) characteristicsText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void ApplyCompactLayout()
        {
            SetOffsets(portrait, new Vector2(0f, 1f), new Vector2(0f, 1f), 14f, -112f, 126f, -14f);
            SetOffsets(speciesName, new Vector2(0f, 1f), Vector2.one, 142f, -50f, -14f, -14f);
            SetOffsets(findingState, new Vector2(0f, 1f), Vector2.one, 142f, -112f, -14f, -58f);
            SetOffsets(actionBackground, new Vector2(0f, 1f), Vector2.one, 142f, -112f, -14f, -58f);
            SetOffsets(historicalData, new Vector2(0f, 1f), new Vector2(0.5f, 1f), 14f, -180f, -8f, -124f);
            SetOffsets(currentData, new Vector2(0.5f, 1f), Vector2.one, 8f, -180f, -14f, -124f);
            SetOffsets(characteristics, new Vector2(0f, 1f), Vector2.one, 24f, -266f, -24f, -190f);
            SetOffsets(characteristicsBackground, new Vector2(0f, 1f), Vector2.one, 14f, -268f, -14f, -188f);
            SetOffsets(timelineDivider, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), -1f, -180f, 1f, -124f);
            if (characteristicsText != null) characteristicsText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void OnEnable()
        {
            ApplyNow();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                ApplyNow();
            }
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
