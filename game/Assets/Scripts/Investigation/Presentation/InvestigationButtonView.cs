using System;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public enum InvestigationButtonStyle
    {
        Primary,
        Browse,
        Navigation
    }

    [DisallowMultipleComponent]
    public sealed class InvestigationButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text labelText;

        [Header("Primary action")]
        [SerializeField] private Color primaryBackground = new Color32(23, 104, 115, 255);
        [SerializeField] private Color primaryText = Color.white;

        [Header("Previous / next browsing")]
        [SerializeField] private Color browseBackground = new Color32(14, 48, 65, 255);
        [SerializeField] private Color browseText = new Color32(169, 201, 207, 255);

        [Header("Section navigation")]
        [SerializeField] private Color navigationBackground = new Color32(24, 75, 91, 255);
        [SerializeField] private Color navigationText = Color.white;

        public string Label => labelText == null ? string.Empty : labelText.text;
        public InvestigationButtonStyle CurrentStyle { get; private set; }
        public Color BackgroundColor => background == null ? Color.clear : background.color;
        public Color LabelColor => labelText == null ? Color.clear : labelText.color;

        public void ConfigureReferences(Button buttonReference, Image backgroundReference, Text labelReference)
        {
            button = buttonReference;
            background = backgroundReference;
            labelText = labelReference;
        }

        public void Bind(
            string label,
            Action action,
            InvestigationButtonStyle style,
            bool isInteractable = true)
        {
            CurrentStyle = style;
            if (labelText != null)
            {
                labelText.text = label;
                labelText.color = GetTextColor(style);
            }

            if (background != null)
            {
                background.color = GetBackgroundColor(style);
            }

            if (button == null)
            {
                return;
            }

            button.interactable = isInteractable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
        }

        private Color GetBackgroundColor(InvestigationButtonStyle style)
        {
            switch (style)
            {
                case InvestigationButtonStyle.Browse:
                    return browseBackground;
                case InvestigationButtonStyle.Navigation:
                    return navigationBackground;
                default:
                    return primaryBackground;
            }
        }

        private Color GetTextColor(InvestigationButtonStyle style)
        {
            switch (style)
            {
                case InvestigationButtonStyle.Browse:
                    return browseText;
                case InvestigationButtonStyle.Navigation:
                    return navigationText;
                default:
                    return primaryText;
            }
        }
    }
}
