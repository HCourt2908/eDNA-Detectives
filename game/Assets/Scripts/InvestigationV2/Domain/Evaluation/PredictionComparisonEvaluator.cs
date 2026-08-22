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
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));
            PredictionComparisonRuleDefinition rule = caseDefinition.FindComparisonRule(threatId, speciesId);
            if (rule == null)
            {
                return Incorrect(threatId, speciesId, evidenceId, judgement, "This model prediction has no comparison rule in the case data.");
            }

            ObservationComparisonOptionDefinition option = rule.FindOption(evidenceId);
            if (option == null)
            {
                return Incorrect(
                    threatId,
                    speciesId,
                    evidenceId,
                    judgement,
                    "That observation is not a relevant candidate for this prediction. Compare evidence about the same species or a configured food-web relationship.");
            }

            JudgementResolutionDefinition resolution = option.FindResolution(judgement);
            if (resolution == null)
            {
                return Incorrect(threatId, speciesId, evidenceId, judgement, "This judgement is not supported by the available evidence.");
            }

            return new PredictionComparisonRecord(
                threatId,
                speciesId,
                evidenceId,
                judgement,
                resolution.Outcome,
                resolution.Feedback);
        }

        private static PredictionComparisonRecord Incorrect(
            string threatId,
            string speciesId,
            string evidenceId,
            ComparisonJudgement judgement,
            string feedback)
        {
            return new PredictionComparisonRecord(
                threatId,
                speciesId,
                evidenceId,
                judgement,
                ComparisonEvaluationOutcome.Incorrect,
                feedback);
        }
    }
}
