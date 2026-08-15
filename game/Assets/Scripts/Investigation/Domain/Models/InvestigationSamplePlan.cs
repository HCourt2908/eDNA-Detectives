using System;
using EDNA.Core;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [Serializable]
    public sealed class InvestigationSamplePlan
    {
        [SerializeField] private SampleRequest request;
        [SerializeField] private int roundIndex;
        [SerializeField] private string relatedHypothesisId = string.Empty;
        [SerializeField] private string reasonEvidenceId = string.Empty;

        public InvestigationSamplePlan(
            SampleRequest request,
            int roundIndex,
            string relatedHypothesisId,
            string reasonEvidenceId)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.roundIndex = roundIndex;
            this.relatedHypothesisId = relatedHypothesisId ?? string.Empty;
            this.reasonEvidenceId = reasonEvidenceId ?? string.Empty;
        }

        public SampleRequest Request => request;
        public int RoundIndex => roundIndex;
        public string RelatedHypothesisId => relatedHypothesisId;
        public string ReasonEvidenceId => reasonEvidenceId;
    }
}
