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
            "CASE FILES",
            "COMPARE DATA",
            "BUILD HYPOTHESIS",
            "PLAN SAMPLE",
            "CONCLUSION"
        };

        private static readonly string[] CompactLabels =
        {
            "CASE",
            "COMPARE",
            "THEORY",
            "SAMPLE",
            "RESULT"
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
                    ? string.Empty
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
