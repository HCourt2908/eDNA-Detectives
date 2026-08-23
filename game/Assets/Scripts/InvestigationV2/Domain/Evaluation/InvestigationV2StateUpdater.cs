using System;
using EDNA.Core;

namespace EDNA.Investigation.V2.Domain
{
    public sealed class InvestigationV2StateUpdater
    {
        private readonly InvestigationV2CaseDefinition caseDefinition;
        private readonly EcosystemSimulatorEvaluator simulatorEvaluator;
        private readonly PredictionComparisonEvaluator comparisonEvaluator;
        private readonly InvestigationV2ConclusionEvaluator conclusionEvaluator;

        public InvestigationV2StateUpdater(InvestigationV2CaseDefinition caseDefinition)
        {
            this.caseDefinition = caseDefinition ?? throw new ArgumentNullException(nameof(caseDefinition));
            simulatorEvaluator = new EcosystemSimulatorEvaluator();
            comparisonEvaluator = new PredictionComparisonEvaluator();
            conclusionEvaluator = new InvestigationV2ConclusionEvaluator();
        }

        public InvestigationV2State CreateInitialState()
        {
            return new InvestigationV2State();
        }

        public bool TryApplyExternalInput(InvestigationV2State state, InvestigationGameInput input, out string feedback)
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

            int applied = 0;
            for (int index = 0; index < input.discoveredObservationIds.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(input.discoveredObservationIds[index]);
                if (observation == null
                    || (observation.UnlockStage != EvidenceUnlockStage.Observe
                        && observation.UnlockStage != EvidenceUnlockStage.Always))
                {
                    continue;
                }
                state.DiscoverObservation(observation.EvidenceId);
                applied++;
            }
            feedback = applied == 0
                ? "External input contained no directly mapped V2 observations; authored demo evidence remains available."
                : $"Imported {applied} observation(s) from the previous mini-games.";
            return true;
        }

        public bool TrySetPhase(InvestigationV2State state, InvestigationV2Phase phase, out string feedback)
        {
            feedback = string.Empty;
            if (state == null)
            {
                feedback = "Investigation state is missing.";
                return false;
            }

            if (phase == InvestigationV2Phase.Report)
            {
                InvestigationV2Readiness readiness = conclusionEvaluator.EvaluateReadiness(caseDefinition, state);
                if (!readiness.CanEnterProvisional)
                {
                    feedback = "Compare the required overlapping causes before entering the report stage.";
                    return false;
                }
            }
            else if (phase == InvestigationV2Phase.Simulate
                && state.DiscoveredObservationIds.Count < caseDefinition.MinimumObserveDiscoveries)
            {
                feedback = $"Record at least {caseDefinition.MinimumObserveDiscoveries} observations before running ecosystem models.";
                return false;
            }

            state.Phase = phase;
            return true;
        }

        public void SetDifficulty(InvestigationV2State state, InvestigationV2Difficulty difficulty)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Difficulty = difficulty;
        }

        public bool TryDiscoverObservation(InvestigationV2State state, string evidenceId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null)
            {
                feedback = "Investigation state is missing.";
                return false;
            }

            InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
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
            InvestigationV2State state,
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
            if (state.DiscoveredObservationIds.Count < caseDefinition.MinimumObserveDiscoveries)
            {
                feedback = $"Record at least {caseDefinition.MinimumObserveDiscoveries} observations before running ecosystem models.";
                return false;
            }
            if (caseDefinition.FindThreat(threatId) == null)
            {
                feedback = "Choose an available cause before running the model.";
                return false;
            }

            result = simulatorEvaluator.Evaluate(caseDefinition, threatId);
            state.RecordSimulation(result);
            state.Phase = InvestigationV2Phase.Simulate;

            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.Observations[index];
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

        public PredictionComparisonRecord Compare(
            InvestigationV2State state,
            string threatId,
            string speciesId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!state.HasTriedThreat(threatId))
            {
                return new PredictionComparisonRecord(
                    threatId,
                    speciesId,
                    evidenceId,
                    judgement,
                    ComparisonEvaluationOutcome.Incorrect,
                    "Run this model before comparing its predictions.");
            }

            if (!state.HasDiscoveredObservation(evidenceId))
            {
                return new PredictionComparisonRecord(
                    threatId,
                    speciesId,
                    evidenceId,
                    judgement,
                    ComparisonEvaluationOutcome.Incorrect,
                    "Discover this observation before using it in a comparison.");
            }

            PredictionComparisonRecord accepted = state.FindComparison(threatId, speciesId);
            if (accepted != null && accepted.CountsTowardProgress)
            {
                return new PredictionComparisonRecord(
                    accepted.ThreatId,
                    accepted.SpeciesId,
                    accepted.EvidenceId,
                    accepted.Judgement,
                    accepted.Outcome,
                    $"Comparison already saved and locked. {accepted.Feedback}");
            }

            PredictionComparisonRecord record = comparisonEvaluator.Evaluate(
                caseDefinition,
                threatId,
                speciesId,
                evidenceId,
                judgement);
            state.RecordComparison(record);
            return record;
        }

        public InvestigationV2Readiness EvaluateReadiness(InvestigationV2State state)
        {
            return conclusionEvaluator.EvaluateReadiness(caseDefinition, state);
        }

        public bool TrySubmitProvisional(InvestigationV2State state, string threatId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null)
            {
                feedback = "Investigation state is missing.";
                return false;
            }

            InvestigationV2Readiness readiness = conclusionEvaluator.EvaluateReadiness(caseDefinition, state);
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
            state.Phase = InvestigationV2Phase.Report;
            feedback = "Provisional explanation recorded. Review the same ROV follow-up before finalising the report.";
            return true;
        }

        public bool TryReviewConfirmation(InvestigationV2State state, out string feedback)
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
            feedback = "ROV follow-up reviewed: fishing line was recorded and the seafloor remains intact.";
            return true;
        }

        public bool TrySetFinalThreat(InvestigationV2State state, string threatId, out string feedback)
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
            state.ConclusionStatus = InvestigationV2ConclusionStatus.NotSubmitted;
            return true;
        }

        public bool TrySetReportEvidence(InvestigationV2State state, string evidenceId, bool selected, out string feedback)
        {
            feedback = string.Empty;
            InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
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
            state.ConclusionStatus = InvestigationV2ConclusionStatus.NotSubmitted;
            return true;
        }

        public bool TrySetReasoning(InvestigationV2State state, string reasoningId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null || caseDefinition.FindReasoning(reasoningId) == null)
            {
                feedback = "Choose an available reasoning statement.";
                return false;
            }
            state.SelectedReasoningId = reasoningId;
            state.ConclusionStatus = InvestigationV2ConclusionStatus.NotSubmitted;
            return true;
        }

        public bool TrySetLimitation(InvestigationV2State state, string limitationId, out string feedback)
        {
            feedback = string.Empty;
            if (state == null || caseDefinition.FindLimitation(limitationId) == null)
            {
                feedback = "Choose an available scientific limitation.";
                return false;
            }
            state.SelectedLimitationId = limitationId;
            state.ConclusionStatus = InvestigationV2ConclusionStatus.NotSubmitted;
            return true;
        }

        public InvestigationV2ConclusionResult SubmitFinal(InvestigationV2State state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            InvestigationV2ConclusionResult result = conclusionEvaluator.EvaluateFinal(
                caseDefinition,
                state,
                state.FinalThreatId);
            state.RecordFinalSubmission(result.Status);
            state.ConclusionStatus = result.Status;
            return result;
        }
    }
}
