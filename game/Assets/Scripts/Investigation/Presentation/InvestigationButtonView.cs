using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public enum InvestigationButtonStyle
    {
        Primary,
        Browse,
        Navigation,
        Support,
        Challenge,
        Commit,
        Destructive
    }

    [DisallowMultipleComponent]
    public sealed class InvestigationButtonView : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text labelText;
        [SerializeField] private Outline outline;
        [SerializeField] private Image activeIndicator;
        [SerializeField] private InvestigationGlyphGraphic glyphGraphic;

        [Header("Primary action")]
        [SerializeField] private Color primaryBackground = new Color32(18, 72, 83, 255);
        [SerializeField] private Color primaryText = new Color32(242, 247, 248, 255);

        [Header("Previous / next browsing")]
        [SerializeField] private Color browseBackground = new Color32(13, 43, 58, 255);
        [SerializeField] private Color browseText = new Color32(177, 207, 213, 255);

        [Header("Section navigation")]
        [SerializeField] private Color navigationBackground = new Color32(13, 43, 58, 255);
        [SerializeField] private Color navigationText = new Color32(177, 207, 213, 255);

        [Header("State-changing commit")]
        [SerializeField] private Color commitBackground = new Color32(50, 204, 209, 255);
        [SerializeField] private Color commitText = new Color32(7, 25, 38, 255);

        [Header("Evidence assignment")]
        [SerializeField] private Color supportBackground = new Color32(16, 61, 51, 255);
        [SerializeField] private Color supportText = new Color32(154, 236, 190, 255);
        [SerializeField] private Color challengeBackground = new Color32(61, 47, 27, 255);
        [SerializeField] private Color challengeText = new Color32(255, 208, 128, 255);

        [Header("Destructive action")]
        [SerializeField] private Color destructiveBackground = new Color32(13, 43, 58, 0);
        [SerializeField] private Color destructiveText = new Color32(255, 190, 90, 255);
        [SerializeField] private Color destructiveOutline = new Color32(255, 190, 90, 255);

        public string Label => labelText == null ? string.Empty : labelText.text;
        public InvestigationButtonStyle CurrentStyle { get; private set; }
        public Color BackgroundColor => background == null ? Color.clear : background.color;
        public Color LabelColor => labelText == null ? Color.clear : labelText.color;
        public bool IsCurrentNavigation { get; private set; }
        public bool IsCompletedNavigation { get; private set; }

        private bool hasFocus;

        public void ConfigureReferences(
            Button buttonReference,
            Image backgroundReference,
            Text labelReference,
            Outline outlineReference = null,
            Image activeIndicatorReference = null,
            InvestigationGlyphGraphic glyphReference = null)
        {
            button = buttonReference;
            background = backgroundReference;
            labelText = labelReference;
            outline = outlineReference;
            activeIndicator = activeIndicatorReference;
            glyphGraphic = glyphReference;
        }

        public void Bind(
            string label,
            Action action,
            InvestigationButtonStyle style,
            bool isInteractable = true)
        {
            CurrentStyle = style;
            IsCurrentNavigation = false;
            IsCompletedNavigation = false;
            if (labelText != null)
            {
                labelText.text = label;
                labelText.color = GetTextColor(style);
                ConfigureLabelLayout(style);
            }

            if (background != null)
            {
                background.color = GetBackgroundColor(style);
            }

            if (glyphGraphic != null)
            {
                glyphGraphic.SetGlyph(InvestigationGlyph.None);
                glyphGraphic.gameObject.SetActive(false);
            }

            if (activeIndicator != null) activeIndicator.gameObject.SetActive(false);

            if (button == null)
            {
                return;
            }

            button.interactable = isInteractable;
            ConfigureButtonColours(style, isInteractable);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
            RefreshOutline();
        }

        public void SetNavigationState(bool isCurrent, bool isCompleted, InvestigationGlyph glyph)
        {
            if (CurrentStyle != InvestigationButtonStyle.Navigation) return;
            IsCurrentNavigation = isCurrent;
            IsCompletedNavigation = isCompleted;

            if (background != null)
            {
                background.color = isCurrent
                    ? InvestigationTheme.SurfaceSelected
                    : isCompleted
                        ? InvestigationTheme.SurfaceRaised
                        : navigationBackground;
            }

            if (labelText != null)
            {
                labelText.color = isCurrent
                    ? InvestigationTheme.Sand
                    : isCompleted
                        ? InvestigationTheme.TextPrimary
                        : navigationText;
            }

            if (glyphGraphic != null)
            {
                glyphGraphic.gameObject.SetActive(glyph != InvestigationGlyph.None);
                glyphGraphic.color = isCurrent
                    ? InvestigationTheme.Primary
                    : isCompleted
                        ? InvestigationTheme.Success
                        : InvestigationTheme.TextMuted;
                glyphGraphic.SetGlyph(glyph);
            }

            if (activeIndicator != null)
            {
                activeIndicator.gameObject.SetActive(isCurrent || isCompleted);
                activeIndicator.color = isCurrent ? InvestigationTheme.Primary : InvestigationTheme.Success;
            }

            ConfigureButtonColours(CurrentStyle, button != null && button.interactable);
            RefreshOutline();
        }

        public void OnSelect(BaseEventData eventData)
        {
            hasFocus = true;
            RefreshOutline();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hasFocus = false;
            RefreshOutline();
        }

        private void ConfigureLabelLayout(InvestigationButtonStyle style)
        {
            if (labelText == null) return;
            RectTransform rect = labelText.rectTransform;
            if (style == InvestigationButtonStyle.Navigation)
            {
                labelText.alignment = TextAnchor.MiddleLeft;
                rect.offsetMin = new Vector2(46f, 4f);
                rect.offsetMax = new Vector2(-10f, -4f);
            }
            else
            {
                labelText.alignment = TextAnchor.MiddleCenter;
                rect.offsetMin = new Vector2(10f, 4f);
                rect.offsetMax = new Vector2(-10f, -4f);
            }
        }

        private void ConfigureButtonColours(InvestigationButtonStyle style, bool isInteractable)
        {
            if (button == null) return;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.86f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.84f, 1f);
            colors.selectedColor = new Color(0.86f, 1f, 1f, 1f);
            colors.disabledColor = new Color(0.52f, 0.58f, 0.60f, 0.72f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = InvestigationTheme.MotionFast;
            button.colors = colors;
            if (labelText != null && !isInteractable)
            {
                labelText.color = InvestigationTheme.WithAlpha(labelText.color, 0.48f);
            }
        }

        private void RefreshOutline()
        {
            if (outline == null) return;
            bool destructive = CurrentStyle == InvestigationButtonStyle.Destructive;
            outline.enabled = destructive || hasFocus || IsCurrentNavigation;
            outline.effectColor = destructive
                ? destructiveOutline
                : hasFocus
                    ? InvestigationTheme.Sand
                    : InvestigationTheme.Primary;
            outline.effectDistance = hasFocus ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }

        private Color GetBackgroundColor(InvestigationButtonStyle style)
        {
            switch (style)
            {
                case InvestigationButtonStyle.Browse:
                    return browseBackground;
                case InvestigationButtonStyle.Navigation:
                    return navigationBackground;
                case InvestigationButtonStyle.Support:
                    return supportBackground;
                case InvestigationButtonStyle.Challenge:
                    return challengeBackground;
                case InvestigationButtonStyle.Commit:
                    return commitBackground;
                case InvestigationButtonStyle.Destructive:
                    return destructiveBackground;
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
                case InvestigationButtonStyle.Support:
                    return supportText;
                case InvestigationButtonStyle.Challenge:
                    return challengeText;
                case InvestigationButtonStyle.Commit:
                    return commitText;
                case InvestigationButtonStyle.Destructive:
                    return destructiveText;
                default:
                    return primaryText;
            }
        }
    }
}
