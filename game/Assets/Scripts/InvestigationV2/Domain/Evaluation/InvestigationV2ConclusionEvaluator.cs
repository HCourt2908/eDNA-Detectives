using System;

namespace EDNA.Investigation.V2.Domain
{
    public sealed class InvestigationV2ConclusionEvaluator
    {
        public InvestigationV2Readiness EvaluateReadiness(
            InvestigationV2CaseDefinition caseDefinition,
            InvestigationV2State state)
        {
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));
            if (state == null) throw new ArgumentNullException(nameof(state));

            bool requiredThreatsCompared = true;
            for (int index = 0; index < caseDefinition.RequiredComparedThreatIds.Count; index++)
            {
                string threatId = caseDefinition.RequiredComparedThreatIds[index];
                if (!state.HasTriedThreat(threatId)
                    || state.AcceptedComparisonCountForThreat(threatId) < caseDefinition.RequiredComparisonsPerThreat)
                {
                    requiredThreatsCompared = false;
                    break;
                }

                InvestigationV2RequiredComparisonSpeciesDefinition requiredSpecies = caseDefinition.FindRequiredComparisonSpecies(threatId);
                if (requiredSpecies == null) continue;
                for (int speciesIndex = 0; speciesIndex < requiredSpecies.RequiredComparisonSpeciesIds.Count; speciesIndex++)
                {
                    PredictionComparisonRecord comparison = state.FindComparison(
                        threatId,
                        requiredSpecies.RequiredComparisonSpeciesIds[speciesIndex]);
                    if (comparison == null || !comparison.CountsTowardProgress)
                    {
                        requiredThreatsCompared = false;
                        break;
                    }
                }
                if (!requiredThreatsCompared) break;
            }

            bool minimumComparisonsComplete = state.AcceptedComparisonCount >= caseDefinition.MinimumCompletedComparisons;
            bool provisionalSubmitted = !string.IsNullOrEmpty(state.ProvisionalThreatId);
            bool confirmationReviewed = state.ConfirmationReviewed;
            bool finalCauseSelected = caseDefinition.FindThreat(state.FinalThreatId) != null;
            bool evidenceComplete = state.SelectedReportEvidenceIds.Count >= caseDefinition.MinimumReportEvidence;
            int confirmationEvidenceCount = 0;
            for (int evidenceIndex = 0; evidenceIndex < caseDefinition.ConfirmationEvidenceIds.Count; evidenceIndex++)
            {
                if (state.HasSelectedEvidence(caseDefinition.ConfirmationEvidenceIds[evidenceIndex])) confirmationEvidenceCount++;
            }
            bool confirmationEvidenceIncluded = confirmationEvidenceCount >= caseDefinition.MinimumConfirmationEvidenceInReport;
            bool reasoningComplete = string.Equals(
                state.SelectedReasoningId,
                caseDefinition.RequiredReasoningId,
                StringComparison.Ordinal);
            bool limitationComplete = !string.IsNullOrEmpty(state.SelectedLimitationId)
                && caseDefinition.FindLimitation(state.SelectedLimitationId) != null;

            return new InvestigationV2Readiness(
                requiredThreatsCompared,
                minimumComparisonsComplete,
                provisionalSubmitted,
                confirmationReviewed,
                finalCauseSelected,
                evidenceComplete,
                confirmationEvidenceIncluded,
                reasoningComplete,
                limitationComplete);
        }

        public InvestigationV2ConclusionResult EvaluateFinal(
            InvestigationV2CaseDefinition caseDefinition,
            InvestigationV2State state,
            string selectedThreatId)
        {
            InvestigationV2Readiness readiness = EvaluateReadiness(caseDefinition, state);
            if (!readiness.CanSubmitFinal)
            {
                return new InvestigationV2ConclusionResult(
                    InvestigationV2ConclusionStatus.InsufficientEvidence,
                    BuildReadinessFeedback(readiness));
            }

            if (caseDefinition.FindThreat(selectedThreatId) == null)
            {
                return new InvestigationV2ConclusionResult(
                    InvestigationV2ConclusionStatus.InsufficientEvidence,
                    "Choose one of the available causes before submitting the final report.");
            }

            if (!string.Equals(selectedThreatId, caseDefinition.CorrectThreatId, StringComparison.Ordinal))
            {
                return new InvestigationV2ConclusionResult(
                    InvestigationV2ConclusionStatus.Incorrect,
                    "This cause does not explain the complete pattern. Compare the shared shark–tuna–krill prediction, then use the stable benthic evidence and intact seafloor to distinguish bottom trawling from long-line fishing.");
            }

            string success = string.IsNullOrWhiteSpace(caseDefinition.SuccessFeedback)
                ? "Case solved. The report connects observations, model predictions, confirmation evidence and a scientific limitation."
                : caseDefinition.SuccessFeedback;
            return new InvestigationV2ConclusionResult(InvestigationV2ConclusionStatus.Correct, success);
        }

        private static string BuildReadinessFeedback(InvestigationV2Readiness readiness)
        {
            if (!readiness.RequiredThreatsCompared)
                return "Compare both long-line fishing and bottom trawling, including their shared food-web predictions.";
            if (!readiness.MinimumComparisonsComplete)
                return "Complete more prediction–observation comparisons before writing a report.";
            if (!readiness.ProvisionalSubmitted)
                return "Submit a provisional explanation before reviewing confirmation evidence.";
            if (!readiness.ConfirmationReviewed)
                return "Review the ROV follow-up before finalising the report.";
            if (!readiness.FinalCauseSelected)
                return "Choose a final cause after reviewing the ROV evidence.";
            if (!readiness.EvidenceComplete)
                return "Select at least two observations for the evidence section.";
            if (!readiness.ConfirmationEvidenceIncluded)
                return "Include at least one ROV confirmation observation in the report.";
            if (!readiness.ReasoningComplete)
                return "Explain the shark–tuna–krill food-web cascade.";
            if (!readiness.LimitationComplete)
                return "Record one scientific limitation, such as non-detection not proving complete absence.";
            return "The report is not ready yet.";
        }
    }
}
