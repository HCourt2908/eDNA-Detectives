using System;
using EDNA.Core;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationStateUpdater
    {
        private readonly InvestigationCaseDefinition caseDefinition;
        private readonly EcosystemSimulatorEvaluator simulatorEvaluator;
        private readonly PredictionComparisonEvaluator comparisonEvaluator;
        private readonly InvestigationConclusionEvaluator conclusionEvaluator;
        private readonly InvestigationCaseRosterBuilder rosterBuilder;

        public InvestigationStateUpdater(InvestigationCaseDefinition caseDefinition)
        {
            this.caseDefinition = caseDefinition ?? throw new ArgumentNullException(nameof(caseDefinition));
            simulatorEvaluator = new EcosystemSimulatorEvaluator();
            comparisonEvaluator = new PredictionComparisonEvaluator();
            conclusionEvaluator = new InvestigationConclusionEvaluator();
            rosterBuilder = new InvestigationCaseRosterBuilder();
        }

        public InvestigationState CreateInitialState()
        {
            InvestigationState state = new InvestigationState();
            state.ApplySurveyContext(caseDefinition.SurveyContext);
            InitializeSurveySpecies(state);
            return state;
        }

        public bool TryApplyExternalInput(InvestigationState state, InvestigationGameInput input, out string feedback)
        {
            feedback = string.Empty;
            if (state == null || input == null)
            {
                feedback = "External investigation input is missing.";
                return false;
            }
            if (!string.IsNullOrEmpty(input.caseId)
                && !string.Equals(input.caseId, caseDefinition.CaseId, StringComparison.Ordinal))
            {
                feedback = $"External input targets case {input.caseId}, not {caseDefinition.CaseId}.";
                return false;
            }

            InvestigationCaseRoster roster = rosterBuilder.Build(caseDefinition, input);
            if (!InvestigationSurveyEvaluator.ValidateInput(caseDefinition, input, roster, out feedback)) return false;
            state.ApplySurveyContext(input.surveyContext);
            int applied = 0;
            int importedSpecies = state.ApplySurveyRoster(roster);
            int surveyRecordCount = roster.SurveyRecords.Count;
            if (input.discoveredObservationIds != null)
            {
                for (int index = 0; index < input.discoveredObservationIds.Count; index++)
                {
                    if (TryImportMappedObservation(state, input.discoveredObservationIds[index], null)) applied++;
                }
            }
            applied += ImportExternalObservationList(state, input.environmentalObservations, ObservationSource.CTDLog);
            applied += ImportExternalObservationList(state, input.physicalObservations, ObservationSource.ROV);

            bool hasContext = HasSurveyContext(input.surveyContext);
            feedback = applied > 0 || importedSpecies > 0 || surveyRecordCount > 0
                ? $"Imported {applied} observation(s), {surveyRecordCount} species survey result(s), and {importedSpecies} additional survey species from the previous mini-games."
                : hasContext
                    ? "Imported the survey context. No case observations were mapped, so authored demo evidence remains available."
                    : "External input contained no directly mapped observations; authored demo evidence remains available.";
            return true;
        }

        private void InitializeSurveySpecies(InvestigationState state)
        {
            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.Observations[index];
                if (observation != null
                    && observation.UnlockStage == EvidenceUnlockStage.Observe
                    && caseDefinition.FindSpecies(observation.RelatedSpeciesId) != null)
                {
                    state.IncludeSurveySpecies(observation.RelatedSpeciesId);
                }
            }
            if (state.SurveySpeciesIds.Count > 0) return;
            for (int index = 0; index < caseDefinition.Species.Count; index++)
            {
                InvestigationSpeciesDefinition species = caseDefinition.Species[index];
                if (species != null) state.IncludeSurveySpecies(species.SpeciesId);
            }
        }

        private int ImportExternalObservationList(
            InvestigationState state,
            System.Collections.Generic.IReadOnlyList<InvestigationExternalObservationData> observations,
            ObservationSource expectedSource)
        {
            if (observations == null) return 0;
            int imported = 0;
            for (int index = 0; index < observations.Count; index++)
            {
                InvestigationExternalObservationData external = observations[index];
                if (external != null && TryImportMappedObservation(state, external.observationId, expectedSource)) imported++;
            }
            return imported;
        }

        private bool TryImportMappedObservation(
            InvestigationState state,
            string evidenceId,
            ObservationSource? expectedSource)
        {
            InvestigationObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
            if (observation == null
                || (expectedSource.HasValue && observation.Source != expectedSource.Value)
                || (observation.UnlockStage != EvidenceUnlockStage.Observe
                    && observation.UnlockStage != EvidenceUnlockStage.Always)
                || state.HasDiscoveredObservation(observation.EvidenceId))
            {
                return false;
            }

            state.DiscoverObservation(observation.EvidenceId);
            return true;
        }

        private static bool HasSurveyContext(InvestigationSurveyContextData context)
        {
            return context != null
                && (!string.IsNullOrWhiteSpace(context.surveyId)
                    || !string.IsNullOrWhiteSpace(context.surveyDisplayName)
                    || !string.IsNullOrWhiteSpace(context.siteId)
                    || !string.IsNullOrWhiteSpace(context.siteDisplayName)
                    || !string.IsNullOrWhiteSpace(context.processedSampleSummary));
        }

        public bool TrySetPhase(InvestigationState state, InvestigationPhase phase, out string feedback)
        {
            feedback = string.Empty;
            if (state == null)
            {
                feedback = "Investigation state is missing.";
                return false;
            }

            if (phase == InvestigationPhase.Report)
            {
                InvestigationReadiness readiness = conclusionEvaluator.EvaluateReadiness(caseDefinition, state);
                if (!readiness.CanEnterProvisional)
                {
                    feedback = "Compare the required overlapping causes before entering the report stage.";
                    return false;
                }
                if (string.IsNullOrEmpty(state.ProvisionalThreatId))
                {
                    feedback = "Save a first idea before entering the report stage.";
                    return false;
                }
            }
            else if (phase == InvestigationPhase.Simulate
                && !InvestigationObserveEvaluator.IsComplete(caseDefinition, state))
            {
                feedback = $"Record at least {InvestigationObserveEvaluator.RequiredCount(caseDefinition)} observations before running ecosystem models.";
                return false;
            }

            state.Phase = phase;
            return true;
        }

        public void SetDifficulty(InvestigationState state, InvestigationDifficulty difficulty)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Difficulty = difficulty;
        }

        public bool TryDiscoverObservation(InvestigationState state, string evidenceId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null)
            {
                feedback = "Investigation state is missing.";
                return false;
            }

            InvestigationObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
            if (observation == null)
            {
                feedback = "Observation is not part of this case.";
                return false;
            }

            if (observation.UnlockStage != EvidenceUnlockStage.Observe
                && observation.UnlockStage != EvidenceUnlockStage.Always)
            {
                feedback = "This observation is not available during the Observe stage.";
                return false;
            }

            state.DiscoverObservation(evidenceId);
            feedback = $"Recorded: {observation.DisplayName}";
            return true;
        }

        public bool TryRunThreat(
            InvestigationState state,
            string threatId,
            out SimulationResult result,
            out string feedback)
        {
            result = null;
            feedback = string.Empty;
            if (state == null)
            {
                feedback = "Investigation state is missing.";
                return false;
            }
            if (!InvestigationObserveEvaluator.IsComplete(caseDefinition, state))
            {
                feedback = $"Record at least {InvestigationObserveEvaluator.RequiredCount(caseDefinition)} observations before running ecosystem models.";
                return false;
            }
            if (caseDefinition.FindThreat(threatId) == null)
            {
                feedback = "Choose an available cause before running the model.";
                return false;
            }

            result = simulatorEvaluator.Evaluate(caseDefinition, threatId);
            state.RecordSimulation(result);
            state.Phase = InvestigationPhase.Simulate;

            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.Observations[index];
                if (observation != null
                    && observation.UnlockStage == EvidenceUnlockStage.OnThreatRun
                    && string.Equals(observation.UnlockThreatId, threatId, StringComparison.Ordinal))
                {
                    state.DiscoverObservation(observation.EvidenceId);
                }
            }

            feedback = "Model complete.";
            return true;
        }

        public PredictionComparisonRecord CompareEvidence(
            InvestigationState state,
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId)
        {
            ObservationComparisonOptionDefinition option = caseDefinition
                .FindComparisonRule(threatId, targetKind, targetId)?.FindOption(evidenceId);
            JudgementResolutionDefinition match = option?.FindResolution(ComparisonJudgement.Match);
            JudgementResolutionDefinition mismatch = option?.FindResolution(ComparisonJudgement.Mismatch);
            bool supports = match != null && match.Outcome != ComparisonEvaluationOutcome.Incorrect;
            bool challenges = mismatch != null && mismatch.Outcome != ComparisonEvaluationOutcome.Incorrect;
            // Selecting evidence determines its authored relationship. Ambiguous
            // or unrelated evidence stays open instead of completing a comparison.
            ComparisonJudgement judgement = supports != challenges
                ? supports ? ComparisonJudgement.Match : ComparisonJudgement.Mismatch
                : ComparisonJudgement.NotEnoughEvidence;
            return Compare(state, threatId, targetKind, targetId, evidenceId, judgement);
        }

        public PredictionComparisonRecord Compare(
            InvestigationState state,
            string threatId,
            string speciesId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            return Compare(
                state,
                threatId,
                PredictionTargetKind.Species,
                speciesId,
                evidenceId,
                judgement);
        }

        public PredictionComparisonRecord Compare(
            InvestigationState state,
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!state.HasTriedThreat(threatId))
            {
                return new PredictionComparisonRecord(
                    threatId,
                    targetKind,
                    targetId,
                    evidenceId,
                    judgement,
                    ComparisonEvaluationOutcome.Incorrect,
                    "Run this model before comparing its predictions.",
                    ComparisonProgressRole.ContextOnly,
                    string.Empty);
            }

            if (!state.HasDiscoveredObservation(evidenceId))
            {
                return new PredictionComparisonRecord(
                    threatId,
                    targetKind,
                    targetId,
                    evidenceId,
                    judgement,
                    ComparisonEvaluationOutcome.Incorrect,
                    "Discover this observation before using it in a comparison.",
                    ComparisonProgressRole.ContextOnly,
                    string.Empty);
            }

            PredictionComparisonRecord accepted = state.FindComparison(threatId, targetKind, targetId);
            if (accepted != null && accepted.LocksComparison)
            {
                return new PredictionComparisonRecord(
                    accepted.ThreatId,
                    accepted.TargetKind,
                    accepted.TargetId,
                    accepted.EvidenceId,
                    accepted.Judgement,
                    accepted.Outcome,
                    $"Comparison already saved and locked. {accepted.Feedback}",
                    accepted.ProgressRole,
                    accepted.ObjectiveId);
            }

            PredictionComparisonRecord record = comparisonEvaluator.Evaluate(
                caseDefinition,
                threatId,
                targetKind,
                targetId,
                evidenceId,
                judgement);
            state.RecordComparison(record);
            return record;
        }

        public InvestigationReadiness EvaluateReadiness(InvestigationState state)
        {
            return conclusionEvaluator.EvaluateReadiness(caseDefinition, state);
        }

        public bool TrySubmitProvisional(InvestigationState state, string threatId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null)
            {
                feedback = "Investigation state is missing.";
                return false;
            }

            InvestigationReadiness readiness = conclusionEvaluator.EvaluateReadiness(caseDefinition, state);
            if (!readiness.CanEnterProvisional)
            {
                feedback = !readiness.RequiredThreatsCompared
                    ? "Compare long-line fishing and bottom trawling before writing a first idea."
                    : "Complete more accepted comparisons before writing a first idea.";
                return false;
            }

            if (caseDefinition.FindThreat(threatId) == null)
            {
                feedback = "Choose a valid provisional cause.";
                return false;
            }

            state.ProvisionalThreatId = threatId;
            state.FinalThreatId = string.Empty;
            state.Phase = InvestigationPhase.Report;
            feedback = "Provisional explanation recorded. Review the same ROV follow-up before finalising the report.";
            return true;
        }

        public bool TryReviewConfirmation(InvestigationState state, out string feedback)
        {
            feedback = string.Empty;
            if (state == null || string.IsNullOrEmpty(state.ProvisionalThreatId))
            {
                feedback = "Submit a provisional explanation before reviewing ROV evidence.";
                return false;
            }

            for (int index = 0; index < caseDefinition.ConfirmationEvidenceIds.Count; index++)
            {
                state.DiscoverObservation(caseDefinition.ConfirmationEvidenceIds[index]);
            }
            state.ConfirmationReviewed = true;
            PrepareReportDraft(state);
            feedback = "ROV follow-up reviewed: fishing line was recorded and the seafloor remains intact.";
            return true;
        }

        private void PrepareReportDraft(InvestigationState state)
        {
            if (!conclusionEvaluator.EvaluateReadiness(caseDefinition, state).RequiredObjectivesComplete) return;
            // Carry forward the player's explanation, including an unsupported one.
            // The report records work already done; it never chooses the answer.
            if (string.IsNullOrEmpty(state.FinalThreatId)) state.FinalThreatId = state.ProvisionalThreatId;
            foreach (PredictionComparisonRecord record in state.ComparisonRecords)
            {
                if (record.LocksComparison && state.HasDiscoveredObservation(record.EvidenceId))
                    state.SetEvidenceSelected(record.EvidenceId, true);
            }
            foreach (string evidenceId in caseDefinition.ConfirmationEvidenceIds)
            {
                if (state.HasDiscoveredObservation(evidenceId)) state.SetEvidenceSelected(evidenceId, true);
            }
            state.SelectedReasoningId = caseDefinition.RequiredReasoningId;
            if (string.IsNullOrEmpty(state.SelectedLimitationId) && caseDefinition.Limitations.Count > 0)
                state.SelectedLimitationId = caseDefinition.Limitations[0].LimitationId;
        }

        public bool TrySetFinalThreat(InvestigationState state, string threatId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null || caseDefinition.FindThreat(threatId) == null)
            {
                feedback = "Choose a valid final cause.";
                return false;
            }
            if (!state.ConfirmationReviewed)
            {
                feedback = "Review the ROV follow-up before choosing the final cause.";
                return false;
            }
            state.FinalThreatId = threatId;
            state.ConclusionStatus = InvestigationConclusionStatus.NotSubmitted;
            return true;
        }

        public bool TrySetReportEvidence(InvestigationState state, string evidenceId, bool selected, out string feedback)
        {
            feedback = string.Empty;
            InvestigationObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
            if (state == null || observation == null || observation.Source == ObservationSource.Methodology)
            {
                feedback = "Choose a discovered observation for the evidence section.";
                return false;
            }
            if (!state.HasDiscoveredObservation(evidenceId))
            {
                feedback = "This observation has not been discovered yet.";
                return false;
            }
            state.SetEvidenceSelected(evidenceId, selected);
            state.ConclusionStatus = InvestigationConclusionStatus.NotSubmitted;
            return true;
        }

        public bool TrySetReasoning(InvestigationState state, string reasoningId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null || caseDefinition.FindReasoning(reasoningId) == null)
            {
                feedback = "Choose an available reasoning statement.";
                return false;
            }
            state.SelectedReasoningId = reasoningId;
            state.ConclusionStatus = InvestigationConclusionStatus.NotSubmitted;
            return true;
        }

        public bool TrySetLimitation(InvestigationState state, string limitationId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null || caseDefinition.FindLimitation(limitationId) == null)
            {
                feedback = "Choose an available scientific limitation.";
                return false;
            }
            state.SelectedLimitationId = limitationId;
            state.ConclusionStatus = InvestigationConclusionStatus.NotSubmitted;
            return true;
        }

        public InvestigationConclusionResult SubmitFinal(InvestigationState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            InvestigationConclusionResult result = conclusionEvaluator.EvaluateFinal(
                caseDefinition,
                state,
                state.FinalThreatId);
            state.RecordFinalSubmission(result.Status);
            state.ConclusionStatus = result.Status;
            return result;
        }
    }
}
