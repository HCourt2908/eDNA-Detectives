using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    /// <summary>
    /// Lets a wrapping button report the height required by its label after the
    /// horizontal layout pass has assigned the available width.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationV2ContentHeightLayoutElement : MonoBehaviour, ILayoutElement
    {
        [SerializeField] private Text content;
        [SerializeField, Min(0f)] private float minimumHeight = 52f;
        [SerializeField, Min(0f)] private float horizontalPadding = 20f;
        [SerializeField, Min(0f)] private float verticalPadding = 16f;

        public void Configure(Text contentText, float minHeight, float horizontalInset, float verticalInset)
        {
            content = contentText;
            minimumHeight = Mathf.Max(0f, minHeight);
            horizontalPadding = Mathf.Max(0f, horizontalInset);
            verticalPadding = Mathf.Max(0f, verticalInset);
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => minimumHeight;

        public float preferredHeight
        {
            get
            {
                if (content == null) return minimumHeight;
                RectTransform rect = transform as RectTransform;
                float availableWidth = Mathf.Max(1f, (rect == null ? 0f : rect.rect.width) - horizontalPadding);
                TextGenerationSettings settings = content.GetGenerationSettings(new Vector2(availableWidth, 0f));
                float textHeight = content.cachedTextGeneratorForLayout.GetPreferredHeight(content.text, settings)
                    / content.pixelsPerUnit;
                return Mathf.Ceil(Mathf.Max(minimumHeight, textHeight + verticalPadding));
            }
        }

        public float flexibleHeight => 0f;
        public int layoutPriority => 2;
    }
}
