using System.Collections.Generic;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationHypothesisSummary
    {
        public InvestigationHypothesisSummary(InvestigationCaseDefinition definition, InvestigationState state, string threatId)
        {
            var records = new List<PredictionComparisonRecord>();
            foreach (PredictionComparisonRecord record in state.ComparisonRecords)
            {
                if (record.ThreatId != threatId || !state.HasTriedThreat(threatId)
                    || !state.HasDiscoveredObservation(record.EvidenceId)
                    || (record.ProgressRole == ComparisonProgressRole.BenthicDiscriminator && !state.ConfirmationReviewed)) continue;
                if (definition.FindComparisonRule(threatId, record.TargetKind, record.TargetId) == null) continue;
                records.Add(record);
                if (!record.LocksComparison) OpenCount++;
                else if (record.Judgement == ComparisonJudgement.Match) SupportCount++;
                else if (record.Judgement == ComparisonJudgement.Mismatch) ChallengeCount++;
            }
            Records = records;
        }

        public IReadOnlyList<PredictionComparisonRecord> Records { get; }
        public int SupportCount { get; }
        public int ChallengeCount { get; }
        public int OpenCount { get; }
    }
}
