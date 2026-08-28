using System;

namespace EDNA.Investigation.V2.Domain
{
    public sealed class PredictionComparisonEvaluator
    {
        public PredictionComparisonRecord Evaluate(
            InvestigationV2CaseDefinition caseDefinition,
            string threatId,
            string speciesId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            return Evaluate(
                caseDefinition,
                threatId,
                PredictionTargetKind.Species,
                speciesId,
                evidenceId,
                judgement);
        }

        public PredictionComparisonRecord Evaluate(
            InvestigationV2CaseDefinition caseDefinition,
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));
            PredictionComparisonRuleDefinition rule = caseDefinition.FindComparisonRule(threatId, targetKind, targetId);
            if (rule == null)
            {
                return Incorrect(threatId, targetKind, targetId, evidenceId, judgement, "This model prediction has no comparison rule in the case data.");
            }

            ObservationComparisonOptionDefinition option = rule.FindOption(evidenceId);
            if (option == null)
            {
                return Incorrect(
                    threatId,
                    targetKind,
                    targetId,
                    evidenceId,
                    judgement,
                    "That observation is not a relevant candidate for this prediction. Compare evidence about the same species or a configured food-web relationship.");
            }

            JudgementResolutionDefinition resolution = option.FindResolution(judgement);
            if (resolution == null)
            {
                return Incorrect(threatId, targetKind, targetId, evidenceId, judgement, "This judgement is not supported by the available evidence.");
            }

            InvestigationV2ObjectiveDefinition objective = caseDefinition.FindObjectiveForComparison(
                threatId,
                targetKind,
                targetId,
                evidenceId,
                judgement);

            return new PredictionComparisonRecord(
                threatId,
                targetKind,
                targetId,
                evidenceId,
                judgement,
                resolution.Outcome,
                resolution.Feedback,
                rule.ProgressRole,
                objective?.ObjectiveId);
        }

        private static PredictionComparisonRecord Incorrect(
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement,
            string feedback)
        {
            return new PredictionComparisonRecord(
                threatId,
                targetKind,
                targetId,
                evidenceId,
                judgement,
                ComparisonEvaluationOutcome.Incorrect,
                feedback,
                ComparisonProgressRole.ContextOnly,
                string.Empty);
        }
    }
}
