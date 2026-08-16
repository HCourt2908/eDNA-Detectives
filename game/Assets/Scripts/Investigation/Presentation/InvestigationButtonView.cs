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
        [SerializeField] private Image navigationBadgeBackground;
        [SerializeField] private Text navigationBadgeText;
        [SerializeField] private InvestigationGlyphGraphic navigationCheckGraphic;

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
        [SerializeField] private Color destructiveBackground = new Color32(4, 17, 27, 255);
        [SerializeField] private Color destructiveText = new Color32(242, 125, 111, 255);
        [SerializeField] private Color destructiveOutline = new Color32(242, 125, 111, 255);

        public string Label => semanticLabel;
        public string DisplayLabel => labelText == null ? string.Empty : labelText.text;
        public InvestigationButtonStyle CurrentStyle { get; private set; }
        public Color BackgroundColor => background == null ? Color.clear : background.color;
        public Color LabelColor => labelText == null ? Color.clear : labelText.color;
        public bool IsCurrentNavigation { get; private set; }
        public bool IsCompletedNavigation { get; private set; }

        private bool hasFocus;
        private string semanticLabel = string.Empty;

        public void ConfigureReferences(
            Button buttonReference,
            Image backgroundReference,
            Text labelReference,
            Outline outlineReference = null,
            Image activeIndicatorReference = null,
            InvestigationGlyphGraphic glyphReference = null,
            Image navigationBadgeBackgroundReference = null,
            Text navigationBadgeTextReference = null,
            InvestigationGlyphGraphic navigationCheckReference = null)
        {
            button = buttonReference;
            background = backgroundReference;
            labelText = labelReference;
            outline = outlineReference;
            activeIndicator = activeIndicatorReference;
            glyphGraphic = glyphReference;
            navigationBadgeBackground = navigationBadgeBackgroundReference;
            navigationBadgeText = navigationBadgeTextReference;
            navigationCheckGraphic = navigationCheckReference;
        }

        public void Bind(
            string label,
            Action action,
            InvestigationButtonStyle style,
            bool isInteractable = true)
        {
            CurrentStyle = style;
            semanticLabel = label ?? string.Empty;
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
            SetNavigationBadgeVisible(style == InvestigationButtonStyle.Navigation);

            if (button == null)
            {
                return;
            }

            button.interactable = isInteractable;
            ConfigureButtonColours(style, isInteractable);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
            RefreshOutline();

            if (style == InvestigationButtonStyle.Navigation)
            {
                transform.parent?.GetComponent<InvestigationResponsiveNavigationLayout>()?.ApplyNow();
            }
        }

        public void SetVisualLabel(string visibleLabel, bool minimalNavigationLabel = false)
        {
            if (labelText == null) return;
            labelText.text = visibleLabel ?? string.Empty;
            ConfigureLabelLayout(CurrentStyle, minimalNavigationLabel);
        }

        public void SetNavigationState(int stepNumber, bool isCurrent, bool isCompleted)
        {
            if (CurrentStyle != InvestigationButtonStyle.Navigation) return;
            IsCurrentNavigation = isCurrent;
            IsCompletedNavigation = isCompleted;

            if (background != null)
            {
                background.color = isCurrent
                    ? InvestigationTheme.SurfaceSelected
                    : InvestigationTheme.BackgroundDeep;
            }

            if (labelText != null)
            {
                labelText.color = isCurrent
                    ? InvestigationTheme.TextPrimary
                    : isCompleted
                        ? InvestigationTheme.TextSecondary
                        : InvestigationTheme.TextMuted;
            }

            if (glyphGraphic != null)
            {
                glyphGraphic.gameObject.SetActive(false);
            }

            if (navigationBadgeBackground != null)
            {
                navigationBadgeBackground.gameObject.SetActive(true);
                navigationBadgeBackground.color = isCurrent
                    ? InvestigationTheme.Primary
                    : isCompleted
                        ? InvestigationTheme.SurfaceSuccess
                        : InvestigationTheme.SurfaceInteractive;
            }

            if (navigationBadgeText != null)
            {
                navigationBadgeText.gameObject.SetActive(!isCompleted);
                navigationBadgeText.text = Mathf.Max(1, stepNumber).ToString("00");
                navigationBadgeText.color = isCurrent
                    ? InvestigationTheme.BackgroundDeep
                    : InvestigationTheme.TextSecondary;
            }

            if (navigationCheckGraphic != null)
            {
                navigationCheckGraphic.gameObject.SetActive(isCompleted);
                navigationCheckGraphic.color = InvestigationTheme.Success;
                navigationCheckGraphic.SetGlyph(InvestigationGlyph.Check);
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

        private void ConfigureLabelLayout(InvestigationButtonStyle style, bool minimalNavigationLabel = false)
        {
            if (labelText == null) return;
            RectTransform rect = labelText.rectTransform;
            if (style == InvestigationButtonStyle.Navigation)
            {
                labelText.fontSize = 13;
                labelText.alignment = minimalNavigationLabel ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                rect.offsetMin = minimalNavigationLabel ? new Vector2(34f, 4f) : new Vector2(44f, 4f);
                rect.offsetMax = minimalNavigationLabel ? new Vector2(-4f, -4f) : new Vector2(-6f, -4f);
            }
            else
            {
                labelText.fontSize = 14;
                labelText.alignment = TextAnchor.MiddleCenter;
                rect.offsetMin = new Vector2(10f, 4f);
                rect.offsetMax = new Vector2(-10f, -4f);
            }

            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout == null) return;
            switch (style)
            {
                case InvestigationButtonStyle.Browse:
                    bool compactArrow = labelText.text == "‹" || labelText.text == "›";
                    layout.minWidth = compactArrow ? 44f : 92f;
                    layout.preferredWidth = compactArrow ? 44f : 132f;
                    layout.flexibleWidth = 0f;
                    break;
                case InvestigationButtonStyle.Navigation:
                    layout.minWidth = minimalNavigationLabel ? 48f : 118f;
                    layout.preferredWidth = minimalNavigationLabel ? 56f : 176f;
                    layout.flexibleWidth = 1f;
                    break;
                case InvestigationButtonStyle.Commit:
                    layout.minWidth = 146f;
                    layout.preferredWidth = 220f;
                    layout.flexibleWidth = 0f;
                    break;
                default:
                    layout.minWidth = 116f;
                    layout.preferredWidth = 168f;
                    layout.flexibleWidth = 0f;
                    break;
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
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = InvestigationTheme.MotionFast;
            button.colors = colors;
            if (labelText != null && !isInteractable)
            {
                labelText.color = InvestigationTheme.TextMuted;
            }

            if (background != null && !isInteractable)
            {
                background.color = InvestigationTheme.BackgroundDeep;
            }
        }

        private void RefreshOutline()
        {
            if (outline == null) return;
            bool destructive = CurrentStyle == InvestigationButtonStyle.Destructive;
            outline.enabled = destructive || hasFocus;
            outline.effectColor = destructive
                ? destructiveOutline
                : hasFocus
                    ? InvestigationTheme.Sand
                    : InvestigationTheme.Primary;
            outline.effectDistance = hasFocus ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }

        private void SetNavigationBadgeVisible(bool visible)
        {
            if (navigationBadgeBackground != null) navigationBadgeBackground.gameObject.SetActive(visible);
            if (navigationBadgeText != null) navigationBadgeText.gameObject.SetActive(visible);
            if (navigationCheckGraphic != null) navigationCheckGraphic.gameObject.SetActive(false);
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
