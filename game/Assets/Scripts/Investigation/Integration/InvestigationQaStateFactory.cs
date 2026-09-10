#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using EDNA.Investigation.Domain;

namespace EDNA.Investigation
{
    public enum InvestigationQaCheckpoint
    {
        Start = 0,
        ObserveReady = 1,
        SimulateComplete = 2,
        ConclusionReady = 3,
        FirstFinding = 5,
        SimulateStart = 6,
        CaseClosed = 8
    }

    public static class InvestigationQaStateFactory
    {
        public static InvestigationState Create(
            InvestigationCaseDefinition caseDefinition,
            InvestigationQaCheckpoint checkpoint)
        {
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));
            if (!Enum.IsDefined(typeof(InvestigationQaCheckpoint), checkpoint)) throw new ArgumentOutOfRangeException(nameof(checkpoint));
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();
            if (checkpoint == InvestigationQaCheckpoint.Start) return state;
            foreach (var finding in caseDefinition.Observations)
            {
                if (!InvestigationObserveEvaluator.IsInitialFinding(finding)) continue;
                Discover(updater, state, finding.EvidenceId);
                if (checkpoint == InvestigationQaCheckpoint.FirstFinding) return state;
            }
            if (checkpoint == InvestigationQaCheckpoint.ObserveReady) return state;
            if (checkpoint == InvestigationQaCheckpoint.SimulateStart)
            {
                Require(updater.TrySetPhase(state, InvestigationPhase.Simulate, out string feedback), feedback);
                return state;
            }

            foreach (var threat in caseDefinition.Threats)
            {
                Run(updater, state, threat.ThreatId);
            }
            foreach (var objective in caseDefinition.InvestigationObjectives)
                if (objective.Required) Compare(updater, state, objective.ThreatId, objective.TargetKind,
                    objective.TargetId, objective.RequiredEvidenceId, objective.RequiredJudgement);
            if (checkpoint == InvestigationQaCheckpoint.SimulateComplete) return state;

            foreach (string id in caseDefinition.SupportedModelThreatIds)
                Require(updater.TryReviewModelExplanation(state, id, out string reviewFeedback), reviewFeedback);
            Require(updater.TryReviewModelExplanation(state, caseDefinition.PrimaryModelThreatId, out string provisionalFeedback), provisionalFeedback);
            if (checkpoint == InvestigationQaCheckpoint.ConclusionReady) return state;
            if (checkpoint == InvestigationQaCheckpoint.CaseClosed)
            {
                var modelResult = updater.SubmitModelConclusion(state, caseDefinition.PrimaryModelThreatId);
                Require(modelResult.Status == InvestigationConclusionStatus.Correct, modelResult.Feedback);
                return state;
            }

            return state;
        }

        private static void Discover(InvestigationStateUpdater updater, InvestigationState state, string evidenceId)
        {
            Require(updater.TryDiscoverObservation(state, evidenceId, out string feedback), feedback);
        }

        private static void Run(InvestigationStateUpdater updater, InvestigationState state, string threatId)
        {
            Require(updater.TryRunThreat(state, threatId, out SimulationResult _, out string feedback), feedback);
        }

        private static void Compare(
            InvestigationStateUpdater updater,
            InvestigationState state,
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            PredictionComparisonRecord record = updater.CompareEvidence(state, threatId, targetKind, targetId, evidenceId);
            Require(record.Judgement == judgement, "The selected evidence resolved to an unexpected relationship.");
            Require(record.CompletesObjective, record.Feedback);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(string.IsNullOrEmpty(message) ? "QA checkpoint could not build a valid state." : message);
        }
    }
}
#endif
