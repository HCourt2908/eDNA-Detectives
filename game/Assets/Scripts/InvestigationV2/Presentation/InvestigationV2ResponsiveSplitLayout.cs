using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    [DisallowMultipleComponent]
    public sealed class InvestigationV2ResponsiveSplitLayout : LayoutGroup
    {
        [SerializeField, Range(0.5f, 0.85f)] private float primaryRatio = 0.68f;
        [SerializeField, Min(0f)] private float spacing = 14f;
        [SerializeField, Min(300f)] private float breakpoint = 820f;
        [SerializeField, Min(100f)] private float primaryHeight = 420f;
        [SerializeField, Min(80f)] private float secondaryHeight = 190f;

        public void Configure(float ratio, float gap, float wideBreakpoint, float firstHeight, float secondHeight)
        {
            primaryRatio = Mathf.Clamp(ratio, 0.5f, 0.85f);
            spacing = Mathf.Max(0f, gap);
            breakpoint = Mathf.Max(300f, wideBreakpoint);
            primaryHeight = Mathf.Max(100f, firstHeight);
            secondaryHeight = Mathf.Max(80f, secondHeight);
            SetDirty();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(padding.horizontal, -1f, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            bool horizontal = rectTransform.rect.width >= breakpoint;
            float height = horizontal
                ? padding.vertical + Mathf.Max(primaryHeight, secondaryHeight)
                : padding.vertical + primaryHeight + spacing + secondaryHeight;
            SetLayoutInputForAxis(height, height, height, 1);
        }

        public override void SetLayoutHorizontal() => LayoutChildren();
        public override void SetLayoutVertical() => LayoutChildren();

        private void LayoutChildren()
        {
            if (rectChildren.Count == 0) return;
            float width = rectTransform.rect.width - padding.horizontal;
            bool horizontal = rectTransform.rect.width >= breakpoint;
            if (horizontal)
            {
                float firstWidth = (width - spacing) * primaryRatio;
                float secondWidth = Mathf.Max(0f, width - spacing - firstWidth);
                SetChildAlongAxis(rectChildren[0], 0, padding.left, firstWidth);
                SetChildAlongAxis(rectChildren[0], 1, padding.top, primaryHeight);
                if (rectChildren.Count > 1)
                {
                    SetChildAlongAxis(rectChildren[1], 0, padding.left + firstWidth + spacing, secondWidth);
                    SetChildAlongAxis(rectChildren[1], 1, padding.top, secondaryHeight);
                }
            }
            else
            {
                SetChildAlongAxis(rectChildren[0], 0, padding.left, width);
                SetChildAlongAxis(rectChildren[0], 1, padding.top, primaryHeight);
                if (rectChildren.Count > 1)
                {
                    SetChildAlongAxis(rectChildren[1], 0, padding.left, width);
                    SetChildAlongAxis(rectChildren[1], 1, padding.top + primaryHeight + spacing, secondaryHeight);
                }
            }
        }
    }
}
