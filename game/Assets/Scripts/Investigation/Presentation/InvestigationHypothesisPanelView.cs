using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationHypothesisPanelView : MonoBehaviour
    {
        private static readonly Color Unexplored = InvestigationTheme.TextMuted;
        private static readonly Color Plausible = InvestigationTheme.Primary;
        private static readonly Color Supported = InvestigationTheme.Success;
        private static readonly Color Contradicted = InvestigationTheme.Warning;

        [SerializeField] private RectTransform selectorRoot;
        [SerializeField] private Text counterText;
        [SerializeField] private Text hypothesisTitleText;
        [SerializeField] private Text hypothesisDescriptionText;
        [SerializeField] private Image statusBadgeImage;
        [SerializeField] private Text statusText;
        [SerializeField] private Text hypothesisMetadataText;
        [SerializeField] private Text selectedIndicatorText;
        [SerializeField] private Text findingTitleText;
        [SerializeField] private Text findingDescriptionText;
        [SerializeField] private Text findingMetadataText;
        [SerializeField] private Text helperText;
        [SerializeField] private Image supportWellImage;
        [SerializeField] private Text supportWellText;
        [SerializeField] private Image challengeWellImage;
        [SerializeField] private Text challengeWellText;

        public string StatusLabel => statusText == null ? string.Empty : statusText.text;
        public Color StatusColor => statusBadgeImage == null ? Color.clear : statusBadgeImage.color;
        public string HypothesisMetadata => hypothesisMetadataText == null ? string.Empty : hypothesisMetadataText.text;
        public RectTransform SelectorRoot => selectorRoot;

        public void ConfigureReferences(
            Text counterReference,
            Text hypothesisTitleReference,
            Text hypothesisDescriptionReference,
            Image statusBadgeReference,
            Text statusReference,
            Text hypothesisMetadataReference,
            Text selectedIndicatorReference,
            Text findingTitleReference,
            Text findingDescriptionReference,
            Text findingMetadataReference,
            Text helperReference,
            RectTransform selectorReference,
            Image supportWellImageReference = null,
            Text supportWellReference = null,
            Image challengeWellImageReference = null,
            Text challengeWellReference = null)
        {
            counterText = counterReference;
            hypothesisTitleText = hypothesisTitleReference;
            hypothesisDescriptionText = hypothesisDescriptionReference;
            statusBadgeImage = statusBadgeReference;
            statusText = statusReference;
            hypothesisMetadataText = hypothesisMetadataReference;
            selectedIndicatorText = selectedIndicatorReference;
            findingTitleText = findingTitleReference;
            findingDescriptionText = findingDescriptionReference;
            findingMetadataText = findingMetadataReference;
            helperText = helperReference;
            selectorRoot = selectorReference;
            supportWellImage = supportWellImageReference;
            supportWellText = supportWellReference;
            challengeWellImage = challengeWellImageReference;
            challengeWellText = challengeWellReference;
        }

        public void Bind(
            int hypothesisNumber,
            int hypothesisCount,
            string hypothesisTitle,
            string hypothesisDescription,
            HypothesisStatus status,
            int supportCount,
            int oppositionCount,
            string requiredPatterns,
            string minimumConfidence,
            bool selectedForConclusion,
            string findingTitle,
            string findingDescription,
            string findingMetadata,
            string currentAssignment = "None")
        {
            counterText.text = $"THEORY {hypothesisNumber} OF {hypothesisCount}";
            hypothesisTitleText.text = hypothesisTitle;
            hypothesisDescriptionText.text = hypothesisDescription;
            statusText.text = InvestigationDisplayNames.HypothesisStatus(status).ToUpperInvariant();
            statusBadgeImage.color = GetStatusColor(status);
            hypothesisMetadataText.text =
                $"Assigned support: {supportCount}    Assigned challenge: {oppositionCount}\n" +
                $"Evidence needed: {requiredPatterns}\n" +
                $"Minimum confidence: {minimumConfidence}";
            selectedIndicatorText.gameObject.SetActive(selectedForConclusion);
            findingTitleText.text = findingTitle;
            findingDescriptionText.text = findingDescription;
            findingMetadataText.text = findingMetadata;
            findingMetadataText.gameObject.SetActive(!string.IsNullOrWhiteSpace(findingMetadata));
            helperText.text = "Assigning a finding records whether it supports or challenges this explanation. It never changes the raw eDNA result.";
            bool assignedSupport = string.Equals(currentAssignment, "Support", System.StringComparison.OrdinalIgnoreCase);
            bool assignedChallenge = string.Equals(currentAssignment, "Challenge", System.StringComparison.OrdinalIgnoreCase);
            if (supportWellText != null)
            {
                supportWellText.text = assignedSupport
                    ? $"SUPPORT  ·  {supportCount}\nSELECTED FINDING LINKED HERE"
                    : $"SUPPORT  ·  {supportCount}\nEvidence that strengthens this theory";
            }
            if (challengeWellText != null)
            {
                challengeWellText.text = assignedChallenge
                    ? $"CHALLENGE  ·  {oppositionCount}\nSELECTED FINDING LINKED HERE"
                    : $"CHALLENGE  ·  {oppositionCount}\nEvidence that tests or weakens it";
            }
            if (supportWellImage != null)
            {
                supportWellImage.color = assignedSupport
                    ? InvestigationTheme.SurfaceSuccess
                    : InvestigationTheme.WithAlpha(InvestigationTheme.SurfaceSuccess, 0.72f);
            }
            if (challengeWellImage != null)
            {
                challengeWellImage.color = assignedChallenge
                    ? InvestigationTheme.SurfaceWarning
                    : InvestigationTheme.WithAlpha(InvestigationTheme.SurfaceWarning, 0.72f);
            }
        }

        private static Color GetStatusColor(HypothesisStatus status)
        {
            switch (status)
            {
                case HypothesisStatus.Plausible: return Plausible;
                case HypothesisStatus.Supported: return Supported;
                case HypothesisStatus.Contradicted: return Contradicted;
                default: return Unexplored;
            }
        }
    }
}
