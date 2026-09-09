using System;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationConclusionEvaluator
    {
        // The two-act game records a model comparison, not a field-confirmed
        // report. Keep the legacy ROV report requirements separate and intact.
        public bool CanRecordModelConclusion(InvestigationCaseDefinition caseDefinition, InvestigationState state, string selectedThreatId)
        {
            if (caseDefinition == null || state == null || state.Phase != InvestigationPhase.Report
                || state.ConclusionStatus == InvestigationConclusionStatus.Correct
                || string.IsNullOrEmpty(state.ProvisionalThreatId) || caseDefinition.FindThreat(selectedThreatId) == null
                || !InvestigationObserveEvaluator.IsComplete(caseDefinition, state)
                || !EvaluateReadiness(caseDefinition, state).RequiredObjectivesComplete) return false;
            foreach (var threat in caseDefinition.Threats) if (!state.HasTriedThreat(threat.ThreatId)) return false;
            return true;
        }

        public InvestigationConclusionResult EvaluateModelConclusion(InvestigationCaseDefinition caseDefinition, InvestigationState state, string selectedThreatId)
        {
            if (!CanRecordModelConclusion(caseDefinition, state, selectedThreatId))
                return new InvestigationConclusionResult(InvestigationConclusionStatus.InsufficientEvidence,
                    "Record the survey findings and compare all three models before recording a conclusion.");
            if (!string.Equals(selectedThreatId, caseDefinition.CorrectThreatId, StringComparison.Ordinal))
                return new InvestigationConclusionResult(InvestigationConclusionStatus.Incorrect,
                    "This model does not explain the whole pattern. Compare the stable species as well as the changes.");
            return new InvestigationConclusionResult(InvestigationConclusionStatus.Correct,
                "Conclusion recorded from your survey and model comparisons. This is the best fit among the tested models, not proof of cause.");
        }

        public InvestigationReadiness EvaluateReadiness(
            InvestigationCaseDefinition caseDefinition,
            InvestigationState state)
        {
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));
            if (state == null) throw new ArgumentNullException(nameof(state));

            bool provisionalObjectivesComplete = true;
            string missingProvisionalObjectiveId = string.Empty;
            string missingProvisionalEvidenceId = string.Empty;
            bool requiredObjectivesComplete = true;
            string missingObjectiveId = string.Empty;
            string missingEvidenceId = string.Empty;
            if (caseDefinition.InvestigationObjectives.Count > 0)
            {
                for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
                {
                    InvestigationObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                    if (objective == null || !objective.Required || state.HasCompletedObjective(objective.ObjectiveId)) continue;
                    requiredObjectivesComplete = false;
                    InvestigationObservationDefinition evidence = caseDefinition.FindObservation(objective.RequiredEvidenceId);
                    string unavailableEvidenceId = !state.HasDiscoveredObservation(objective.RequiredEvidenceId)
                        && evidence != null
                        && (evidence.UnlockStage == EvidenceUnlockStage.Observe
                            || evidence.UnlockStage == EvidenceUnlockStage.Always)
                        ? objective.RequiredEvidenceId
                        : string.Empty;
                    if (string.IsNullOrEmpty(missingObjectiveId))
                    {
                        missingObjectiveId = objective.ObjectiveId;
                        missingEvidenceId = unavailableEvidenceId;
                    }
                    if (string.IsNullOrEmpty(missingProvisionalObjectiveId))
                    {
                        provisionalObjectivesComplete = false;
                        missingProvisionalObjectiveId = objective.ObjectiveId;
                        missingProvisionalEvidenceId = unavailableEvidenceId;
                    }
                }
            }
            else
            {
                // Defensive compatibility for direct domain callers that bypass
                // validation. The runtime Controller rejects objective-less cases
                // through InvestigationCaseValidator before this path is reachable.
                for (int index = 0; index < caseDefinition.RequiredComparedThreatIds.Count; index++)
                {
                    string threatId = caseDefinition.RequiredComparedThreatIds[index];
                    if (!state.HasTriedThreat(threatId)
                        || state.AcceptedComparisonCountForThreat(threatId) < caseDefinition.RequiredComparisonsPerThreat)
                    {
                        requiredObjectivesComplete = false;
                        provisionalObjectivesComplete = false;
                        break;
                    }
                }
            }

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
            bool evidenceCategoriesComplete = true;
            EvidenceCategory missingEvidenceCategory = EvidenceCategory.General;
            for (int requirementIndex = 0; requirementIndex < caseDefinition.EvidenceCategoryRequirements.Count; requirementIndex++)
            {
                InvestigationEvidenceCategoryRequirement requirement = caseDefinition.EvidenceCategoryRequirements[requirementIndex];
                if (requirement == null) continue;
                int selectedInCategory = 0;
                for (int evidenceIndex = 0; evidenceIndex < state.SelectedReportEvidenceIds.Count; evidenceIndex++)
                {
                    InvestigationObservationDefinition observation = caseDefinition.FindObservation(state.SelectedReportEvidenceIds[evidenceIndex]);
                    if (observation != null && observation.Category == requirement.Category) selectedInCategory++;
                }
                if (selectedInCategory >= requirement.MinimumCount) continue;
                evidenceCategoriesComplete = false;
                missingEvidenceCategory = requirement.Category;
                break;
            }
            bool reasoningComplete = string.Equals(
                state.SelectedReasoningId,
                caseDefinition.RequiredReasoningId,
                StringComparison.Ordinal);
            bool limitationComplete = !string.IsNullOrEmpty(state.SelectedLimitationId)
                && caseDefinition.FindLimitation(state.SelectedLimitationId) != null;

            return new InvestigationReadiness(
                provisionalObjectivesComplete,
                missingProvisionalObjectiveId,
                missingProvisionalEvidenceId,
                requiredObjectivesComplete,
                missingObjectiveId,
                missingEvidenceId,
                provisionalSubmitted,
                confirmationReviewed,
                finalCauseSelected,
                evidenceComplete,
                confirmationEvidenceIncluded,
                evidenceCategoriesComplete,
                missingEvidenceCategory,
                reasoningComplete,
                limitationComplete);
        }

        public InvestigationConclusionResult EvaluateFinal(
            InvestigationCaseDefinition caseDefinition,
            InvestigationState state,
            string selectedThreatId)
        {
            InvestigationReadiness readiness = EvaluateReadiness(caseDefinition, state);
            if (!readiness.CanSubmitFinal)
            {
                return new InvestigationConclusionResult(
                    InvestigationConclusionStatus.InsufficientEvidence,
                    BuildReadinessFeedback(caseDefinition, readiness));
            }

            if (caseDefinition.FindThreat(selectedThreatId) == null)
            {
                return new InvestigationConclusionResult(
                    InvestigationConclusionStatus.InsufficientEvidence,
                    "Choose one of the available causes before submitting the final report.");
            }

            if (!string.Equals(selectedThreatId, caseDefinition.CorrectThreatId, StringComparison.Ordinal))
            {
                return new InvestigationConclusionResult(
                    InvestigationConclusionStatus.Incorrect,
                    "This cause does not explain the complete pattern. Compare the shared shark–tuna–krill prediction, then use the stable benthic evidence and intact seafloor to distinguish bottom trawling from long-line fishing.");
            }

            string success = string.IsNullOrWhiteSpace(caseDefinition.SuccessFeedback)
                ? "Case solved. The report connects observations, model predictions, confirmation evidence and a scientific limitation."
                : caseDefinition.SuccessFeedback;
            return new InvestigationConclusionResult(InvestigationConclusionStatus.Correct, success);
        }

        private static string BuildReadinessFeedback(
            InvestigationCaseDefinition caseDefinition,
            InvestigationReadiness readiness)
        {
            if (!readiness.ProvisionalSubmitted)
                return "Submit a provisional explanation before reviewing confirmation evidence.";
            if (!readiness.ConfirmationReviewed)
                return "Review the ROV follow-up before finalising the report.";
            if (!readiness.RequiredObjectivesComplete)
            {
                if (!string.IsNullOrEmpty(readiness.MissingEvidenceId))
                {
                    InvestigationObservationDefinition observation = caseDefinition.FindObservation(readiness.MissingEvidenceId);
                    string subject = observation == null ? readiness.MissingEvidenceId : observation.DisplayName;
                    return $"Return to Observe and record the missing evidence: {subject}.";
                }
                InvestigationObjectiveDefinition objective = caseDefinition.FindObjective(readiness.MissingObjectiveId);
                return objective == null
                    ? "Return to Simulate and complete the follow-up comparison."
                    : $"Return to Simulate and complete this follow-up question: {objective.QuestionPrompt}";
            }
            if (!readiness.FinalCauseSelected)
                return "Choose a final cause after reviewing the ROV evidence.";
            if (!readiness.ReasoningComplete)
                return "Explain the shark–tuna–krill food-web cascade.";
            if (!readiness.EvidenceComplete)
                return $"Select at least {caseDefinition.MinimumReportEvidence} observations for the evidence section.";
            if (!readiness.ConfirmationEvidenceIncluded)
                return "Include at least one ROV confirmation observation in the report.";
            if (!readiness.EvidenceCategoriesComplete)
                return $"Add more {EvidenceCategoryLabel(readiness.MissingEvidenceCategory)} evidence to the report.";
            if (!readiness.LimitationComplete)
                return "Record one scientific limitation, such as non-detection not proving complete absence.";
            return "The report is not ready yet.";
        }

        private static string EvidenceCategoryLabel(EvidenceCategory category)
        {
            switch (category)
            {
                case EvidenceCategory.FoodWeb: return "food-web";
                case EvidenceCategory.Benthic: return "benthic";
                case EvidenceCategory.Confirmation: return "ROV confirmation";
                case EvidenceCategory.Environmental: return "environmental";
                case EvidenceCategory.Alternative: return "alternative-cause";
                default: return "required";
            }
        }
    }
}
