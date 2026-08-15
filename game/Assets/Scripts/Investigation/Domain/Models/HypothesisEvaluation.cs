namespace EDNA.Investigation.Domain
{
    public sealed class HypothesisEvaluation
    {
        public HypothesisEvaluation(
            string hypothesisId,
            HypothesisStatus status,
            int supportingEvidenceCount,
            int opposingEvidenceCount,
            string explanation)
        {
            HypothesisId = hypothesisId;
            Status = status;
            SupportingEvidenceCount = supportingEvidenceCount;
            OpposingEvidenceCount = opposingEvidenceCount;
            Explanation = explanation;
        }

        public string HypothesisId { get; }
        public HypothesisStatus Status { get; }
        public int SupportingEvidenceCount { get; }
        public int OpposingEvidenceCount { get; }
        public string Explanation { get; }
    }
}
