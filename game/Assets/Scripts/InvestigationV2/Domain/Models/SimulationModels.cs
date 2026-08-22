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
        {
            ThreatId = threatId ?? string.Empty;
            SpeciesId = speciesId ?? string.Empty;
            EvidenceId = evidenceId ?? string.Empty;
            Judgement = judgement;
            Outcome = outcome;
            Feedback = feedback ?? string.Empty;
        }

        public string ThreatId { get; }
        public string SpeciesId { get; }
        public string EvidenceId { get; }
        public ComparisonJudgement Judgement { get; }
        public ComparisonEvaluationOutcome Outcome { get; }
        public string Feedback { get; }
        public bool IsAccepted => Outcome != ComparisonEvaluationOutcome.Incorrect;
        public bool CountsTowardProgress => IsAccepted && Judgement != ComparisonJudgement.NotEnoughEvidence;
    }

    public sealed class InvestigationV2Readiness
    {
        public InvestigationV2Readiness(
            bool requiredThreatsCompared,
            bool minimumComparisonsComplete,
            bool provisionalSubmitted,
            bool confirmationReviewed,
            bool finalCauseSelected,
            bool evidenceComplete,
            bool confirmationEvidenceIncluded,
            bool reasoningComplete,
            bool limitationComplete)
        {
            RequiredThreatsCompared = requiredThreatsCompared;
            MinimumComparisonsComplete = minimumComparisonsComplete;
            ProvisionalSubmitted = provisionalSubmitted;
            ConfirmationReviewed = confirmationReviewed;
            FinalCauseSelected = finalCauseSelected;
            EvidenceComplete = evidenceComplete;
            ConfirmationEvidenceIncluded = confirmationEvidenceIncluded;
            ReasoningComplete = reasoningComplete;
            LimitationComplete = limitationComplete;
        }

        public bool RequiredThreatsCompared { get; }
        public bool MinimumComparisonsComplete { get; }
        public bool ProvisionalSubmitted { get; }
        public bool ConfirmationReviewed { get; }
        public bool FinalCauseSelected { get; }
        public bool EvidenceComplete { get; }
        public bool ConfirmationEvidenceIncluded { get; }
        public bool ReasoningComplete { get; }
        public bool LimitationComplete { get; }
        public bool CanEnterProvisional => RequiredThreatsCompared && MinimumComparisonsComplete;
        public bool CanSubmitFinal => CanEnterProvisional
            && ProvisionalSubmitted
            && ConfirmationReviewed
            && FinalCauseSelected
            && EvidenceComplete
            && ConfirmationEvidenceIncluded
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
