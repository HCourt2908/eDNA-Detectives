using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class SpeciesComparisonCardView : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private static readonly Color Normal = InvestigationTheme.SurfaceRaised;
        private static readonly Color Selected = InvestigationTheme.SurfaceSelected;
        private static readonly Color Identified = InvestigationTheme.SurfaceSuccess;
        private static readonly Color Missing = InvestigationTheme.BackgroundDeep;
        private static readonly Color PendingText = InvestigationTheme.Warning;
        private static readonly Color SelectedText = InvestigationTheme.Sand;
        private static readonly Color IdentifiedText = InvestigationTheme.Success;
        private static readonly Color InactiveText = InvestigationTheme.TextSecondary;
        private static readonly Color PendingAction = InvestigationTheme.Surface;
        private static readonly Color SelectedAction = InvestigationTheme.SurfaceWarning;
        private static readonly Color IdentifiedAction = InvestigationTheme.SurfaceSuccess;

        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image portraitBackground;
        [SerializeField] private Text portraitText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text historicalText;
        [SerializeField] private Text currentText;
        [SerializeField] private Text traitsText;
        [SerializeField] private Image traitsBackground;
        [SerializeField] private Image actionBackground;
        [SerializeField] private Text findingStateText;
        [SerializeField] private Outline stateOutline;
        [SerializeField] private InvestigationGlyphGraphic portraitGlyph;
        [SerializeField] private InvestigationAttentionPulse attentionPulse;

        [Header("Pending classification pulse")]
        [SerializeField] private Color pendingPulseBackground = new Color32(38, 93, 106, 255);
        [SerializeField] private Color pendingPulseText = InvestigationTheme.Sand;
        [SerializeField, Min(0.1f)] private float pendingPulseSpeed = 0.75f;

        private bool pendingAttention;
        private bool selected;
        private bool identified;
        private bool hasFocus;

        public bool IsAwaitingSelection => pendingAttention;
        public bool IsSelected => selected;
        public Color ActionBackgroundColor => actionBackground == null ? Color.clear : actionBackground.color;
        public string TraitsLabel => traitsText == null ? string.Empty : traitsText.text;

        public void ConfigureReferences(
            Button buttonReference,
            Image backgroundReference,
            Image portraitBackgroundReference,
            Text portraitReference,
            Text nameReference,
            Text historicalReference,
            Text currentReference,
            Text traitsReference,
            Text findingStateReference,
            Image traitsBackgroundReference = null,
            Outline outlineReference = null,
            InvestigationGlyphGraphic portraitGlyphReference = null,
            InvestigationAttentionPulse attentionPulseReference = null)
        {
            button = buttonReference;
            background = backgroundReference;
            portraitBackground = portraitBackgroundReference;
            portraitText = portraitReference;
            nameText = nameReference;
            historicalText = historicalReference;
            currentText = currentReference;
            traitsText = traitsReference;
            findingStateText = findingStateReference;
            traitsBackground = traitsBackgroundReference;
            stateOutline = outlineReference;
            portraitGlyph = portraitGlyphReference;
            attentionPulse = attentionPulseReference;
        }

        public void Bind(
            string displayName,
            string portraitMarker,
            string historical,
            string current,
            string traits,
            string findingState,
            bool isMissing,
            bool isSelected,
            bool isIdentified,
            bool isInteractive,
            Action onSelected)
        {
            nameText.text = displayName;
            bool isWarning = portraitMarker == "!";
            portraitText.text = isWarning
                ? "LAB ALERT"
                : isMissing
                    ? "NOT DETECTED"
                    : portraitMarker;
            historicalText.text = historical;
            currentText.text = current;
            traitsText.text = traits;
            findingStateText.text = findingState;

            selected = isSelected;
            identified = isIdentified;
            pendingAttention = isInteractive && !selected && !identified;
            background.color = isIdentified ? Identified : isSelected ? Selected : Normal;
            findingStateText.color = isIdentified
                ? IdentifiedText
                : isSelected
                    ? SelectedText
                    : isInteractive
                        ? PendingText
                        : InactiveText;
            if (actionBackground != null)
            {
                actionBackground.color = isIdentified
                    ? IdentifiedAction
                    : isSelected
                        ? SelectedAction
                        : PendingAction;
            }
            if (traitsBackground != null)
            {
                traitsBackground.color = InvestigationTheme.WithAlpha(InvestigationTheme.BackgroundDeep, 0.62f);
            }
            portraitBackground.color = isMissing ? Missing : InvestigationTheme.SurfaceInteractive;
            portraitText.color = isMissing
                ? InvestigationTheme.TextMuted
                : isWarning
                    ? InvestigationTheme.Warning
                    : InvestigationTheme.Sand;
            if (portraitGlyph != null)
            {
                portraitGlyph.SetGlyph(isWarning
                    ? InvestigationGlyph.Warning
                    : isMissing
                        ? InvestigationGlyph.Fish
                        : InvestigationGlyph.Dna);
                portraitGlyph.color = portraitText.color;
            }
            button.interactable = isInteractive;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke());
            if (attentionPulse != null)
            {
                attentionPulse.Configure(
                    findingStateText,
                    actionBackground,
                    findingStateText.color,
                    pendingPulseText,
                    actionBackground == null ? PendingAction : actionBackground.color,
                    pendingPulseBackground,
                    pendingPulseSpeed);
                attentionPulse.SetPulsing(pendingAttention && !InvestigationMotionSettings.ReducedMotion);
            }
            UpdateOutline();
        }

        public void OnSelect(BaseEventData eventData)
        {
            hasFocus = true;
            UpdateOutline();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hasFocus = false;
            UpdateOutline();
        }

        private void UpdateOutline()
        {
            if (stateOutline == null) return;
            stateOutline.enabled = true;
            stateOutline.useGraphicAlpha = false;
            stateOutline.effectDistance = hasFocus ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            stateOutline.effectColor = hasFocus
                ? InvestigationTheme.Sand
                : identified
                    ? InvestigationTheme.Success
                    : selected
                        ? InvestigationTheme.Primary
                        : pendingAttention
                            ? InvestigationTheme.WithAlpha(InvestigationTheme.Warning, 0.55f)
                            : InvestigationTheme.Border;
        }
    }
}
