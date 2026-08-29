using System;
using System.Collections.Generic;
using EDNA.Core;

namespace EDNA.Investigation.V2.Domain
{
    public sealed class InvestigationV2State
    {
        private readonly List<string> discoveredObservationIds = new List<string>();
        private readonly List<string> triedThreatIds = new List<string>();
        private readonly List<SimulationResult> simulationResults = new List<SimulationResult>();
        private readonly List<PredictionComparisonRecord> comparisonRecords = new List<PredictionComparisonRecord>();
        private readonly List<string> selectedReportEvidenceIds = new List<string>();

        public InvestigationV2Phase Phase { get; internal set; } = InvestigationV2Phase.Observe;
        public InvestigationV2Difficulty Difficulty { get; internal set; } = InvestigationV2Difficulty.Easy;
        public string ActiveThreatId { get; internal set; } = string.Empty;
        public string ProvisionalThreatId { get; internal set; } = string.Empty;
        public bool ConfirmationReviewed { get; internal set; }
        public string FinalThreatId { get; internal set; } = string.Empty;
        public string SelectedReasoningId { get; internal set; } = string.Empty;
        public string SelectedLimitationId { get; internal set; } = string.Empty;
        public InvestigationV2ConclusionStatus ConclusionStatus { get; internal set; } = InvestigationV2ConclusionStatus.NotSubmitted;
        public int MisstepCount { get; internal set; }
        public int FinalSubmissionAttemptCount { get; internal set; }
        public string SurveyId { get; private set; } = string.Empty;
        public string SurveyDisplayName { get; private set; } = "Survey";
        public string SiteId { get; private set; } = string.Empty;
        public string SiteDisplayName { get; private set; } = "Survey site";
        public string ProcessedSampleSummary { get; private set; } = "Processed eDNA survey results";

        public IReadOnlyList<string> DiscoveredObservationIds => discoveredObservationIds;
        public IReadOnlyList<string> TriedThreatIds => triedThreatIds;
        public IReadOnlyList<SimulationResult> SimulationResults => simulationResults;
        public IReadOnlyList<PredictionComparisonRecord> ComparisonRecords => comparisonRecords;
        public IReadOnlyList<string> SelectedReportEvidenceIds => selectedReportEvidenceIds;

        public bool HasDiscoveredObservation(string evidenceId) => Contains(discoveredObservationIds, evidenceId);
        public bool HasTriedThreat(string threatId) => Contains(triedThreatIds, threatId);
        public bool HasSelectedEvidence(string evidenceId) => Contains(selectedReportEvidenceIds, evidenceId);

        public int AcceptedComparisonCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < comparisonRecords.Count; index++)
                {
                    if (comparisonRecords[index].CountsTowardProgress) count++;
                }
                return count;
            }
        }

        public int AcceptedComparisonCountForThreat(string threatId)
        {
            int count = 0;
            for (int index = 0; index < comparisonRecords.Count; index++)
            {
                PredictionComparisonRecord record = comparisonRecords[index];
                if (record.CountsTowardProgress && string.Equals(record.ThreatId, threatId, StringComparison.Ordinal)) count++;
            }
            return count;
        }

        public SimulationResult FindSimulation(string threatId)
        {
            for (int index = 0; index < simulationResults.Count; index++)
            {
                SimulationResult result = simulationResults[index];
                if (string.Equals(result.ThreatId, threatId, StringComparison.Ordinal)) return result;
            }
            return null;
        }

        public PredictionComparisonRecord FindComparison(string threatId, string speciesId)
        {
            return FindComparison(threatId, PredictionTargetKind.Species, speciesId);
        }

        public PredictionComparisonRecord FindComparison(
            string threatId,
            PredictionTargetKind targetKind,
            string targetId)
        {
            for (int index = 0; index < comparisonRecords.Count; index++)
            {
                PredictionComparisonRecord record = comparisonRecords[index];
                if (string.Equals(record.ThreatId, threatId, StringComparison.Ordinal)
                    && record.TargetKind == targetKind
                    && string.Equals(record.TargetId, targetId, StringComparison.Ordinal)) return record;
            }
            return null;
        }

        public bool HasCompletedObjective(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId)) return false;
            for (int index = 0; index < comparisonRecords.Count; index++)
            {
                PredictionComparisonRecord record = comparisonRecords[index];
                if (record.CompletesObjective
                    && string.Equals(record.ObjectiveId, objectiveId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public int CompletedObjectiveCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < comparisonRecords.Count; index++)
                    if (comparisonRecords[index].CompletesObjective) count++;
                return count;
            }
        }

        internal void DiscoverObservation(string evidenceId)
        {
            AddUnique(discoveredObservationIds, evidenceId);
        }

        internal void ApplySurveyContext(InvestigationSurveyContextData context)
        {
            if (context == null) return;
            SurveyId = Prefer(context.surveyId, SurveyId);
            SurveyDisplayName = Prefer(context.surveyDisplayName, SurveyDisplayName);
            SiteId = Prefer(context.siteId, SiteId);
            SiteDisplayName = Prefer(context.siteDisplayName, SiteDisplayName);
            ProcessedSampleSummary = Prefer(context.processedSampleSummary, ProcessedSampleSummary);
        }

        internal void RecordSimulation(SimulationResult result)
        {
            if (result == null) return;
            AddUnique(triedThreatIds, result.ThreatId);
            for (int index = simulationResults.Count - 1; index >= 0; index--)
            {
                if (string.Equals(simulationResults[index].ThreatId, result.ThreatId, StringComparison.Ordinal))
                {
                    simulationResults.RemoveAt(index);
                }
            }
            simulationResults.Add(result);
            ActiveThreatId = result.ThreatId;
        }

        internal bool RecordComparison(PredictionComparisonRecord record)
        {
            if (record == null) return false;
            for (int index = comparisonRecords.Count - 1; index >= 0; index--)
            {
                PredictionComparisonRecord existing = comparisonRecords[index];
                if (string.Equals(existing.ThreatId, record.ThreatId, StringComparison.Ordinal)
                    && existing.TargetKind == record.TargetKind
                    && string.Equals(existing.TargetId, record.TargetId, StringComparison.Ordinal))
                {
                    // A decisive accepted judgement is committed. A scientifically
                    // acceptable NotEnoughEvidence remains revisable and never locks.
                    if (existing.LocksComparison) return false;
                    comparisonRecords.RemoveAt(index);
                }
            }
            comparisonRecords.Add(record);
            if (!record.IsAccepted) MisstepCount++;
            return true;
        }

        internal void RecordFinalSubmission(InvestigationV2ConclusionStatus status)
        {
            if (status != InvestigationV2ConclusionStatus.Correct
                && status != InvestigationV2ConclusionStatus.Incorrect)
            {
                return;
            }

            FinalSubmissionAttemptCount++;
            if (status == InvestigationV2ConclusionStatus.Incorrect) MisstepCount++;
        }

        internal void SetEvidenceSelected(string evidenceId, bool selected)
        {
            if (selected) AddUnique(selectedReportEvidenceIds, evidenceId);
            else Remove(selectedReportEvidenceIds, evidenceId);
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], value, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (!string.IsNullOrEmpty(value) && !Contains(values, value)) values.Add(value);
        }

        private static void Remove(List<string> values, string value)
        {
            for (int index = values.Count - 1; index >= 0; index--)
            {
                if (string.Equals(values[index], value, StringComparison.Ordinal)) values.RemoveAt(index);
            }
        }

        private static string Prefer(string candidate, string fallback)
        {
            return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate.Trim();
        }
    }
}
