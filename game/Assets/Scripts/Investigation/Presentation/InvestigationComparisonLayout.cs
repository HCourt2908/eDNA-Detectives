using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    // Compact controls leave most of the desktop width for the open notebook.
    public sealed class InvestigationComparisonLayout : LayoutGroup
    {
        private float speciesHeight = 305f;
        private float changeHeight = 240f;
        private float notebookHeight = 430f;
        private const float Gap = 12f;
        private bool Wide => rectTransform.rect.width - padding.horizontal >= 860f;

        public void Configure(float species, float changes, float notebook)
        {
            speciesHeight = Mathf.Max(100f, species);
            changeHeight = Mathf.Max(100f, changes);
            notebookHeight = Mathf.Max(280f, notebook);
            SetDirty();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(padding.horizontal, -1f, -1f, 0);
        }
        public override void CalculateLayoutInputVertical()
        {
            float height = padding.vertical + (Wide ? Mathf.Max(Mathf.Max(speciesHeight, changeHeight), notebookHeight)
                : Mathf.Max(speciesHeight, changeHeight) + Gap + notebookHeight);
            SetLayoutInputForAxis(height, height, height, 1);
        }
        public override void SetLayoutHorizontal() => Arrange();
        public override void SetLayoutVertical() => Arrange();
        private void Arrange()
        {
            if (rectChildren.Count < 3) return;
            float width = rectTransform.rect.width - padding.horizontal;
            float first = Wide ? Mathf.Clamp(width * .18f, 160f, 230f) : Mathf.Min(220f, (width - Gap) * .38f);
            float second = Wide ? Mathf.Clamp(width * .26f, 220f, 330f) : width - Gap - first;
            Place(0, padding.left, padding.top, first, speciesHeight);
            Place(1, padding.left + first + Gap, padding.top, second, changeHeight);
            if (Wide) Place(2, padding.left + first + second + Gap * 2f, padding.top, width - first - second - Gap * 2f, notebookHeight);
            else Place(2, padding.left, padding.top + Mathf.Max(speciesHeight, changeHeight) + Gap, width, notebookHeight);
        }
        private void Place(int index, float x, float y, float width, float height)
        {
            SetChildAlongAxis(rectChildren[index], 0, x, width);
            SetChildAlongAxis(rectChildren[index], 1, y, height);
        }
    }
}
