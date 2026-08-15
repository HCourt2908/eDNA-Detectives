using System;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class SpeciesComparisonCardView : MonoBehaviour
    {
        private static readonly Color Normal = new Color32(20, 64, 82, 255);
        private static readonly Color Selected = new Color32(31, 126, 136, 255);
        private static readonly Color Identified = new Color32(31, 105, 82, 255);
        private static readonly Color Missing = new Color32(10, 25, 36, 255);
        private static readonly Color PendingText = new Color32(255, 190, 90, 255);
        private static readonly Color SelectedText = new Color32(245, 230, 190, 255);
        private static readonly Color IdentifiedText = new Color32(120, 230, 170, 255);
        private static readonly Color InactiveText = new Color32(169, 201, 207, 255);

        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image portraitBackground;
        [SerializeField] private Text portraitText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text historicalText;
        [SerializeField] private Text currentText;
        [SerializeField] private Text traitsText;
        [SerializeField] private Text findingStateText;

        [Header("Pending classification pulse")]
        [SerializeField] private Color pendingPulseBackground = new Color32(38, 93, 106, 255);
        [SerializeField] private Color pendingPulseText = new Color32(245, 230, 190, 255);
        [SerializeField, Min(0.1f)] private float pendingPulseSpeed = 1.25f;

        private bool pendingAttention;

        public bool IsAwaitingSelection => pendingAttention;

        public void ConfigureReferences(
            Button buttonReference,
            Image backgroundReference,
            Image portraitBackgroundReference,
            Text portraitReference,
            Text nameReference,
            Text historicalReference,
            Text currentReference,
            Text traitsReference,
            Text findingStateReference)
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
            portraitText.text = portraitMarker;
            historicalText.text = historical;
            currentText.text = current;
            traitsText.text = traits;
            findingStateText.text = findingState;

            pendingAttention = isInteractive && !isSelected && !isIdentified;
            background.color = isIdentified ? Identified : isSelected ? Selected : Normal;
            findingStateText.color = isIdentified
                ? IdentifiedText
                : isSelected
                    ? SelectedText
                    : isInteractive
                        ? PendingText
                        : InactiveText;
            portraitBackground.color = isMissing ? Missing : (Color)new Color32(35, 105, 119, 255);
            portraitText.color = isMissing
                ? (Color)new Color32(70, 88, 98, 255)
                : (Color)new Color32(245, 230, 190, 255);
            button.interactable = isInteractive;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke());
        }

        private void Update()
        {
            if (!pendingAttention || background == null || findingStateText == null)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pendingPulseSpeed * Mathf.PI * 2f);
            background.color = Color.Lerp(Normal, pendingPulseBackground, pulse);
            findingStateText.color = Color.Lerp(PendingText, pendingPulseText, pulse);
        }
    }
}
