namespace EDNA.Investigation.Domain
{
    public sealed class ConclusionReadiness
    {
        public ConclusionReadiness(
            HypothesisDefinition selectedHypothesis,
            HypothesisEvaluation hypothesisEvaluation,
            bool hasRequiredFollowUpSample,
            bool hasRequiredOpposingEvidence)
        {
            SelectedHypothesis = selectedHypothesis;
            HypothesisEvaluation = hypothesisEvaluation;
            HasRequiredFollowUpSample = hasRequiredFollowUpSample;
            HasRequiredOpposingEvidence = hasRequiredOpposingEvidence;
        }

        public HypothesisDefinition SelectedHypothesis { get; }
        public HypothesisEvaluation HypothesisEvaluation { get; }
        public bool HasSelectedHypothesis => SelectedHypothesis != null;
        public bool HasSupportedHypothesis =>
            HypothesisEvaluation != null && HypothesisEvaluation.Status == HypothesisStatus.Supported;
        public bool HasRequiredFollowUpSample { get; }
        public bool HasRequiredOpposingEvidence { get; }
        public bool CanSubmit =>
            HasSelectedHypothesis
            && HasSupportedHypothesis
            && HasRequiredFollowUpSample
            && HasRequiredOpposingEvidence;
    }
}
