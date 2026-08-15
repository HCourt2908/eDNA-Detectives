using System;

namespace EDNA.Investigation.Domain
{
    public sealed class ConclusionEvaluator
    {
        private readonly HypothesisEvaluator hypothesisEvaluator;

        public ConclusionEvaluator(HypothesisEvaluator hypothesisEvaluator)
        {
            this.hypothesisEvaluator = hypothesisEvaluator ?? throw new ArgumentNullException(nameof(hypothesisEvaluator));
        }

        public ConclusionResult Evaluate(InvestigationCaseDefinition caseDefinition, InvestigationState state)
        {
            if (string.IsNullOrEmpty(state.SelectedHypothesisId))
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "Select a hypothesis before submitting a conclusion.");
            }

            HypothesisDefinition selected = caseDefinition.FindHypothesis(state.SelectedHypothesisId);
            if (selected == null)
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "The selected hypothesis is not available in this case.");
            }

            HypothesisEvaluation evaluation = hypothesisEvaluator.Evaluate(selected, state);
            if (evaluation.Status != HypothesisStatus.Supported)
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "The current evidence does not fully support this hypothesis yet.");
            }

            if (caseDefinition.RequireFollowUpSample && state.CompletedSampleCount == 0)
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "Plan and complete at least one follow-up sample before concluding.");
            }

            if (evaluation.OpposingEvidenceCount < caseDefinition.RequiredOpposingEvidence)
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "Consider at least one uncertainty or opposing observation before concluding.");
            }

            if (!string.Equals(
                    selected.HypothesisId,
                    caseDefinition.CorrectHypothesisId,
                    StringComparison.Ordinal))
            {
                return new ConclusionResult(
                    ConclusionStatus.Incorrect,
                    "This conclusion does not explain all of the case evidence. Keep investigating.");
            }

            string feedback = string.IsNullOrEmpty(caseDefinition.SuccessFeedback)
                ? "Conclusion supported: the evidence explains the observed ecosystem change."
                : caseDefinition.SuccessFeedback;
            return new ConclusionResult(ConclusionStatus.Correct, feedback);
        }
    }
}
