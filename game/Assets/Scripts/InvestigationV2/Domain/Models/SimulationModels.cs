using System;
using System.Collections.Generic;

namespace EDNA.Investigation.V2.Domain
{
    public sealed class SimulationPrediction
    {
        public SimulationPrediction(string threatId, string speciesId, PredictionState predictedState, string rationale)
        {
            ThreatId = threatId ?? string.Empty;
            SpeciesId = speciesId ?? string.Empty;
            PredictedState = predictedState;
            Rationale = rationale ?? string.Empty;
        }

        public string ThreatId { get; }
        public string SpeciesId { get; }
        public PredictionState PredictedState { get; }
        public string Rationale { get; }
    }

    public sealed class SimulationResult
    {
        public SimulationResult(
            string threatId,
            IEnumerable<SimulationPrediction> predictions,
            string temperaturePrediction,
            string seafloorPrediction,
            string physicalConfirmation)
        {
            ThreatId = threatId ?? string.Empty;
            Predictions = new List<SimulationPrediction>(predictions ?? Array.Empty<SimulationPrediction>());
            TemperaturePrediction = temperaturePrediction ?? string.Empty;
            SeafloorPrediction = seafloorPrediction ?? string.Empty;
            PhysicalConfirmation = physicalConfirmation ?? string.Empty;
        }

        public string ThreatId { get; }
        public IReadOnlyList<SimulationPrediction> Predictions { get; }
        public string TemperaturePrediction { get; }
        public string SeafloorPrediction { get; }
        public string PhysicalConfirmation { get; }

        public SimulationPrediction FindPrediction(string speciesId)
        {
            for (int index = 0; index < Predictions.Count; index++)
            {
                SimulationPrediction prediction = Predictions[index];
                if (string.Equals(prediction.SpeciesId, speciesId, StringComparison.Ordinal)) return prediction;
            }
            return null;
        }
    }

    public sealed class PredictionComparisonRecord
    {
        public PredictionComparisonRecord(
            string threatId,
            string speciesId,
            string evidenceId,
            ComparisonJudgement judgement,
            ComparisonEvaluationOutcome outcome,
            string feedback)
            : this(
                threatId,
                PredictionTargetKind.Species,
                speciesId,
                evidenceId,
                judgement,
                outcome,
                feedback,
                ComparisonProgressRole.ContextOnly,
                string.Empty)
        {
        }

        public PredictionComparisonRecord(
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement,
            ComparisonEvaluationOutcome outcome,
            string feedback,
            ComparisonProgressRole progressRole,
            string objectiveId)
        {
            ThreatId = threatId ?? string.Empty;
            TargetKind = targetKind;
            TargetId = targetId ?? string.Empty;
            EvidenceId = evidenceId ?? string.Empty;
            Judgement = judgement;
            Outcome = outcome;
            Feedback = feedback ?? string.Empty;
            ProgressRole = progressRole;
            ObjectiveId = objectiveId ?? string.Empty;
        }

        public string ThreatId { get; }
        public PredictionTargetKind TargetKind { get; }
        public string TargetId { get; }
        public string SpeciesId => TargetKind == PredictionTargetKind.Species ? TargetId : string.Empty;
        public string EvidenceId { get; }
        public ComparisonJudgement Judgement { get; }
        public ComparisonEvaluationOutcome Outcome { get; }
        public string Feedback { get; }
        public ComparisonProgressRole ProgressRole { get; }
        public string ObjectiveId { get; }
        public bool IsAccepted => Outcome != ComparisonEvaluationOutcome.Incorrect;
        public bool LocksComparison => IsAccepted && Judgement != ComparisonJudgement.NotEnoughEvidence;
        public bool CompletesObjective => LocksComparison && !string.IsNullOrEmpty(ObjectiveId);
        public bool CountsTowardProgress => LocksComparison;
    }

    public sealed class InvestigationV2Readiness
    {
        public InvestigationV2Readiness(
            bool provisionalObjectivesComplete,
            string missingProvisionalObjectiveId,
            string missingProvisionalEvidenceId,
            bool requiredObjectivesComplete,
            string missingObjectiveId,
            string missingEvidenceId,
            bool provisionalSubmitted,
            bool confirmationReviewed,
            bool finalCauseSelected,
            bool evidenceComplete,
            bool confirmationEvidenceIncluded,
            bool evidenceCategoriesComplete,
            EvidenceCategory missingEvidenceCategory,
            bool reasoningComplete,
            bool limitationComplete)
        {
            ProvisionalObjectivesComplete = provisionalObjectivesComplete;
            MissingProvisionalObjectiveId = missingProvisionalObjectiveId ?? string.Empty;
            MissingProvisionalEvidenceId = missingProvisionalEvidenceId ?? string.Empty;
            RequiredObjectivesComplete = requiredObjectivesComplete;
            MissingObjectiveId = missingObjectiveId ?? string.Empty;
            MissingEvidenceId = missingEvidenceId ?? string.Empty;
            ProvisionalSubmitted = provisionalSubmitted;
            ConfirmationReviewed = confirmationReviewed;
            FinalCauseSelected = finalCauseSelected;
            EvidenceComplete = evidenceComplete;
            ConfirmationEvidenceIncluded = confirmationEvidenceIncluded;
            EvidenceCategoriesComplete = evidenceCategoriesComplete;
            MissingEvidenceCategory = missingEvidenceCategory;
            ReasoningComplete = reasoningComplete;
            LimitationComplete = limitationComplete;
        }

        public bool ProvisionalObjectivesComplete { get; }
        public string MissingProvisionalObjectiveId { get; }
        public string MissingProvisionalEvidenceId { get; }
        public bool RequiredObjectivesComplete { get; }
        public string MissingObjectiveId { get; }
        public string MissingEvidenceId { get; }
        public bool RequiredThreatsCompared => ProvisionalObjectivesComplete;
        public bool MinimumComparisonsComplete => ProvisionalObjectivesComplete;
        public bool ProvisionalSubmitted { get; }
        public bool ConfirmationReviewed { get; }
        public bool FinalCauseSelected { get; }
        public bool EvidenceComplete { get; }
        public bool ConfirmationEvidenceIncluded { get; }
        public bool EvidenceCategoriesComplete { get; }
        public EvidenceCategory MissingEvidenceCategory { get; }
        public bool ReasoningComplete { get; }
        public bool LimitationComplete { get; }
        public bool CanEnterProvisional => ProvisionalObjectivesComplete;
        public bool CanSubmitFinal => RequiredObjectivesComplete
            && ProvisionalSubmitted
            && ConfirmationReviewed
            && FinalCauseSelected
            && EvidenceComplete
            && ConfirmationEvidenceIncluded
            && EvidenceCategoriesComplete
            && ReasoningComplete
            && LimitationComplete;
    }

    public sealed class InvestigationV2ConclusionResult
    {
        public InvestigationV2ConclusionResult(InvestigationV2ConclusionStatus status, string feedback)
        {
            Status = status;
            Feedback = feedback ?? string.Empty;
        }

        public InvestigationV2ConclusionStatus Status { get; }
        public string Feedback { get; }
    }
}
