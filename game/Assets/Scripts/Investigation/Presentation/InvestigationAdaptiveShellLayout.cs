using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class InvestigationAdaptiveShellLayout : MonoBehaviour
    {
        [Header("Shell regions")]
        [SerializeField] private RectTransform statusRoot;
        [SerializeField] private Text statusMessage;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform actionsRoot;

        [Header("Action regions")]
        [SerializeField] private RectTransform standardActions;
        [SerializeField] private RectTransform classificationPanel;
        [SerializeField] private InvestigationResponsiveGridLayout classificationChoices;
        [SerializeField] private RectTransform comparisonNavigation;
        [SerializeField] private InvestigationResponsiveGridLayout standardActionGrid;
        [SerializeField] private InvestigationResponsiveGridLayout comparisonNavigationGrid;

        [Header("Sizing")]
        [SerializeField, Min(48f)] private float compactActionsHeight = 56f;
        [SerializeField, Min(96f)] private float comparisonActionsHeight = 138f;
        [SerializeField, Min(48f)] private float minimumStatusHeight = 54f;
        [SerializeField, Min(54f)] private float maximumStatusHeight = 76f;
        [SerializeField, Min(0f)] private float statusTopOffset = 136f;
        [SerializeField, Min(0f)] private float regionGap = 8f;

        private bool comparisonMode;
        private bool applyingLayout;

        public bool IsComparisonMode => comparisonMode;
        public float CurrentActionsHeight { get; private set; }
        public float CurrentStatusHeight { get; private set; }
        public float CurrentContentHeight => contentRoot == null ? 0f : contentRoot.rect.height;

        public void ConfigureReferences(
            RectTransform status,
            Text message,
            RectTransform content,
            RectTransform actions,
            RectTransform standard,
            RectTransform classification,
            InvestigationResponsiveGridLayout choices,
            RectTransform compareNavigation,
            InvestigationResponsiveGridLayout standardGrid,
            InvestigationResponsiveGridLayout compareGrid)
        {
            statusRoot = status;
            statusMessage = message;
            contentRoot = content;
            actionsRoot = actions;
            standardActions = standard;
            classificationPanel = classification;
            classificationChoices = choices;
            comparisonNavigation = compareNavigation;
            standardActionGrid = standardGrid;
            comparisonNavigationGrid = compareGrid;
            ApplyNow();
        }

        public void SetComparisonMode(bool enabled)
        {
            comparisonMode = enabled;
            ApplyNow();
        }

        public void ApplyNow()
        {
            if (applyingLayout || contentRoot == null || actionsRoot == null) return;
            applyingLayout = true;
            try
            {
                CurrentStatusHeight = CalculateStatusHeight();
                if (statusRoot != null)
                {
                    SetTopOffsets(statusRoot, 12f, statusTopOffset, 12f, statusTopOffset + CurrentStatusHeight);
                }

                float actionHeight = comparisonMode
                    ? CalculateComparisonActionsHeight()
                    : CalculateStandardActionsHeight();
                CurrentActionsHeight = actionHeight;

                SetBottomOffsets(actionsRoot, 12f, 8f, 12f, 8f + actionHeight);
                SetStretchOffsets(
                    contentRoot,
                    12f,
                    8f + actionHeight + regionGap,
                    12f,
                    statusTopOffset + CurrentStatusHeight + regionGap);

                if (standardActions != null)
                {
                    SetStretchOffsets(standardActions, 0f, 0f, 0f, 0f);
                }

                if (comparisonMode)
                {
                    ApplyComparisonRegions();
                }
            }
            finally
            {
                applyingLayout = false;
            }
        }

        private float CalculateStatusHeight()
        {
            if (statusMessage == null) return minimumStatusHeight;
            float preferredMessageHeight = Mathf.Max(20f, statusMessage.preferredHeight);
            return Mathf.Clamp(preferredMessageHeight + 34f, minimumStatusHeight, maximumStatusHeight);
        }

        private float CalculateStandardActionsHeight()
        {
            if (standardActionGrid == null) return compactActionsHeight;
            standardActionGrid.ApplyNow();
            return Mathf.Max(compactActionsHeight, standardActionGrid.RequiredHeight);
        }

        private float CalculateComparisonActionsHeight()
        {
            classificationChoices?.ApplyNow();
            comparisonNavigationGrid?.ApplyNow();
            float choicesHeight = classificationChoices == null
                ? 48f
                : Mathf.Max(48f, classificationChoices.RequiredHeight);
            float navigationHeight = comparisonNavigationGrid == null
                ? 56f
                : Mathf.Max(56f, comparisonNavigationGrid.RequiredHeight + 8f);
            float classificationHeight = 22f + choicesHeight + 4f;
            return Mathf.Max(comparisonActionsHeight, navigationHeight + classificationHeight + regionGap);
        }

        private void ApplyComparisonRegions()
        {
            float navigationContentHeight = comparisonNavigationGrid == null
                ? 48f
                : Mathf.Max(48f, comparisonNavigationGrid.RequiredHeight);
            float navigationHeight = navigationContentHeight + 8f;
            if (comparisonNavigation != null)
            {
                SetBottomOffsets(comparisonNavigation, 12f, 4f, 12f, 4f + navigationContentHeight);
            }

            if (classificationPanel != null)
            {
                SetStretchOffsets(classificationPanel, 12f, navigationHeight, 12f, 4f);
            }
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

        private static void SetTopOffsets(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, -bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetBottomOffsets(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, top);
        }

        private static void SetStretchOffsets(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
