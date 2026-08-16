using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(GridLayoutGroup))]
    public sealed class InvestigationResponsiveGridLayout : MonoBehaviour
    {
        [SerializeField, Min(48f)] private float minimumCellWidth = 220f;
        [SerializeField, Min(1)] private int maximumColumns = 1;
        [SerializeField] private bool resizeHeightToRows;

        private bool applyingLayout;

        public int CurrentColumns
        {
            get
            {
                GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
                return Mathf.Max(1, grid.constraintCount);
            }
        }

        public float RequiredHeight
        {
            get
            {
                GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
                int columns = CurrentColumns;
                int layoutChildCount = CountLayoutChildren();
                int rows = layoutChildCount == 0 ? 0 : Mathf.CeilToInt((float)layoutChildCount / columns);
                return grid.padding.vertical
                    + rows * grid.cellSize.y
                    + Mathf.Max(0, rows - 1) * Mathf.Max(0f, grid.spacing.y);
            }
        }

        public void Configure(float minimumWidth, int maximumColumnCount = 0, bool adjustHeightToRows = false)
        {
            minimumCellWidth = Mathf.Max(48f, minimumWidth);
            GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
            maximumColumns = maximumColumnCount > 0
                ? maximumColumnCount
                : Mathf.Max(1, grid.constraintCount);
            resizeHeightToRows = adjustHeightToRows;
            ApplyNow();
        }

        public void ApplyNow()
        {
            if (applyingLayout) return;
            applyingLayout = true;
            try
            {
                RectTransform rect = (RectTransform)transform;
                GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
                int preferredColumns = Mathf.Max(1, maximumColumns);
                if (rect.rect.width <= 0f)
                {
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = preferredColumns;
                    return;
                }

                float spacing = Mathf.Max(0f, grid.spacing.x);
                float contentWidth = Mathf.Max(0f, rect.rect.width - grid.padding.horizontal);
                float columnPitch = minimumCellWidth + spacing;
                int fittingColumns = columnPitch <= 0f
                    ? preferredColumns
                    : Mathf.FloorToInt((contentWidth + spacing) / columnPitch);
                int columns = Mathf.Clamp(fittingColumns, 1, preferredColumns);
                float gaps = spacing * Mathf.Max(0, columns - 1);
                float width = Mathf.Max(0f, (contentWidth - gaps) / columns);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columns;
                if (!Mathf.Approximately(grid.cellSize.x, width))
                {
                    grid.cellSize = new Vector2(width, grid.cellSize.y);
                }

                if (resizeHeightToRows)
                {
                    ResizeHeight(grid, columns);
                }
            }
            finally
            {
                applyingLayout = false;
            }
        }

        private void ResizeHeight(GridLayoutGroup grid, int columns)
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null) return;

            int layoutChildCount = CountLayoutChildren();
            int rows = layoutChildCount == 0 ? 0 : Mathf.CeilToInt((float)layoutChildCount / columns);
            float preferredHeight = grid.padding.vertical
                + rows * grid.cellSize.y
                + Mathf.Max(0, rows - 1) * grid.spacing.y;
            layoutElement.minHeight = preferredHeight;
            layoutElement.preferredHeight = preferredHeight;
        }

        private int CountLayoutChildren()
        {
            int layoutChildCount = 0;
            for (int index = 0; index < transform.childCount; index++)
            {
                GameObject child = transform.GetChild(index).gameObject;
                LayoutElement childLayout = child.GetComponent<LayoutElement>();
                if (child.activeSelf && (childLayout == null || !childLayout.ignoreLayout))
                {
                    layoutChildCount++;
                }
            }

            return layoutChildCount;
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
