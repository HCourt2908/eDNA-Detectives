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
            ConclusionReadiness readiness = EvaluateReadiness(caseDefinition, state);
            if (string.IsNullOrEmpty(state.SelectedHypothesisId))
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "Select a hypothesis before submitting a conclusion.");
            }

            HypothesisDefinition selected = readiness.SelectedHypothesis;
            if (selected == null)
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "The selected hypothesis is not available in this case.");
            }

            if (!readiness.HasSupportedHypothesis)
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "The current evidence does not fully support this hypothesis yet.");
            }

            if (!readiness.HasRequiredFollowUpSample)
            {
                return new ConclusionResult(
                    ConclusionStatus.InsufficientEvidence,
                    "Plan and complete at least one follow-up sample before concluding.");
            }

            if (!readiness.HasRequiredOpposingEvidence)
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

        public ConclusionReadiness EvaluateReadiness(
            InvestigationCaseDefinition caseDefinition,
            InvestigationState state)
        {
            HypothesisDefinition selected = caseDefinition.FindHypothesis(state.SelectedHypothesisId);
            HypothesisEvaluation evaluation = selected == null
                ? null
                : hypothesisEvaluator.Evaluate(selected, state);
            bool hasFollowUp = !caseDefinition.RequireFollowUpSample || state.CompletedSampleCount > 0;
            bool hasOpposition = caseDefinition.RequiredOpposingEvidence == 0
                || (evaluation != null
                    && evaluation.OpposingEvidenceCount >= caseDefinition.RequiredOpposingEvidence);
            return new ConclusionReadiness(selected, evaluation, hasFollowUp, hasOpposition);
        }
    }
}
