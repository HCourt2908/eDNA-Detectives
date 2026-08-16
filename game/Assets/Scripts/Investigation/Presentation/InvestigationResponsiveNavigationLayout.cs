using UnityEngine;

namespace EDNA.Investigation
{
    public enum InvestigationNavigationLabelMode
    {
        Full,
        Compact,
        Minimal
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class InvestigationResponsiveNavigationLayout : MonoBehaviour
    {
        private static readonly string[] FullLabels =
        {
            "1  CASE FILES",
            "2  COMPARE DATA",
            "3  BUILD HYPOTHESIS",
            "4  PLAN SAMPLE",
            "5  CONCLUSION"
        };

        private static readonly string[] CompactLabels =
        {
            "1  CASE",
            "2  COMPARE",
            "3  THEORY",
            "4  SAMPLE",
            "5  RESULT"
        };

        [SerializeField, Min(320f)] private float compactBreakpoint = 760f;
        [SerializeField, Min(240f)] private float minimalBreakpoint = 520f;

        public InvestigationNavigationLabelMode CurrentMode { get; private set; }

        public void ApplyNow()
        {
            float width = ((RectTransform)transform).rect.width;
            CurrentMode = width > 0f && width < minimalBreakpoint
                ? InvestigationNavigationLabelMode.Minimal
                : width > 0f && width < compactBreakpoint
                    ? InvestigationNavigationLabelMode.Compact
                    : InvestigationNavigationLabelMode.Full;

            int navigationIndex = 0;
            for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
            {
                InvestigationButtonView button = transform.GetChild(childIndex).GetComponent<InvestigationButtonView>();
                if (button == null || button.CurrentStyle != InvestigationButtonStyle.Navigation) continue;
                if (navigationIndex >= FullLabels.Length) break;

                string visibleLabel = CurrentMode == InvestigationNavigationLabelMode.Minimal
                    ? (navigationIndex + 1).ToString()
                    : CurrentMode == InvestigationNavigationLabelMode.Compact
                        ? CompactLabels[navigationIndex]
                        : FullLabels[navigationIndex];
                button.SetVisualLabel(visibleLabel, CurrentMode == InvestigationNavigationLabelMode.Minimal);
                navigationIndex++;
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

        private void OnTransformChildrenChanged()
        {
            if (isActiveAndEnabled)
            {
                ApplyNow();
            }
        }
    }
}
