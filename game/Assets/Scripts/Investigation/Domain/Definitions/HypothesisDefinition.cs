using System.Collections.Generic;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation/Hypothesis", fileName = "Hypothesis_")]
    public sealed class HypothesisDefinition : ScriptableObject
    {
        [SerializeField] private string hypothesisId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 6)] private string explanation = string.Empty;
        [SerializeField] private List<string> requiredEvidenceTags = new List<string>();
        [SerializeField] private List<string> contradictingEvidenceTags = new List<string>();
        [SerializeField] private EvidenceConfidence minimumConfidence = EvidenceConfidence.Medium;
        [SerializeField, Min(1)] private int minimumSupportingEvidence = 2;
        [SerializeField, TextArea(2, 5)] private string feedbackText = string.Empty;

        public string HypothesisId => hypothesisId;
        public string DisplayName => displayName;
        public string Explanation => explanation;
        public IReadOnlyList<string> RequiredEvidenceTags => requiredEvidenceTags;
        public IReadOnlyList<string> ContradictingEvidenceTags => contradictingEvidenceTags;
        public EvidenceConfidence MinimumConfidence => minimumConfidence;
        public int MinimumSupportingEvidence => Mathf.Max(1, minimumSupportingEvidence);
        public string FeedbackText => feedbackText;
    }
}
