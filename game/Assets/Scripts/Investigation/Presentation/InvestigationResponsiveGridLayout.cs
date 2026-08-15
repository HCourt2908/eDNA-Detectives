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

        public void Configure(float minimumWidth)
        {
            minimumCellWidth = Mathf.Max(48f, minimumWidth);
            ApplyNow();
        }

        public void ApplyNow()
        {
            RectTransform rect = (RectTransform)transform;
            GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
            int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.Max(1, grid.constraintCount)
                : 1;
            float gaps = grid.spacing.x * Mathf.Max(0, columns - 1);
            float available = rect.rect.width - grid.padding.horizontal - gaps;
            float width = Mathf.Max(minimumCellWidth, available / columns);
            if (!Mathf.Approximately(grid.cellSize.x, width))
            {
                grid.cellSize = new Vector2(width, grid.cellSize.y);
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
    }
}
