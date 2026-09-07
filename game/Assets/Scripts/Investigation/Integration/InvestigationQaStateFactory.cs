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
        ReportReady = 3,
        FinalReportReady = 4,
        FirstFinding = 5,
        SimulateStart = 6,
        ReportQuestions = 7,
        CaseClosed = 8,
        EvidenceReady = 9
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
            Discover(updater, state, "E01_SHARK_NONDETECTION");
            if (checkpoint == InvestigationQaCheckpoint.FirstFinding) return state;
            Discover(updater, state, "E02_TUNA_WIDER_DETECTION");
            Discover(updater, state, "E03_KRILL_NONDETECTION");
            Discover(updater, state, "E04_BENTHIC_STABLE");
            Discover(updater, state, "E06_PLASTIC_INDICATOR_STABLE");
            if (checkpoint == InvestigationQaCheckpoint.ObserveReady) return state;
            if (checkpoint == InvestigationQaCheckpoint.SimulateStart)
            {
                Require(updater.TrySetPhase(state, InvestigationPhase.Simulate, out string feedback), feedback);
                return state;
            }

            Run(updater, state, "plastic");
            if (checkpoint == InvestigationQaCheckpoint.EvidenceReady) return state;
            Compare(updater, state, "plastic", PredictionTargetKind.Species, "mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch);
            Run(updater, state, "longline");
            Compare(updater, state, "longline", PredictionTargetKind.Species, "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match);
            Compare(updater, state, "longline", PredictionTargetKind.Species, "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match);
            Compare(updater, state, "longline", PredictionTargetKind.Species, "krill", "E03_KRILL_NONDETECTION", ComparisonJudgement.Match);
            Run(updater, state, "bottom_trawling");
            Compare(updater, state, "bottom_trawling", PredictionTargetKind.Species, "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match);
            if (checkpoint == InvestigationQaCheckpoint.SimulateComplete) return state;

            Require(updater.TrySubmitProvisional(state, "longline", out string provisionalFeedback), provisionalFeedback);
            if (checkpoint == InvestigationQaCheckpoint.ReportReady) return state;

            Require(updater.TryReviewConfirmation(state, out string confirmationFeedback), confirmationFeedback);
            Require(updater.TrySetPhase(state, InvestigationPhase.Simulate, out string simulateFeedback), simulateFeedback);
            Compare(updater, state, "longline", PredictionTargetKind.Species, "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match);
            Compare(updater, state, "bottom_trawling", PredictionTargetKind.Species, "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch);
            Require(updater.TrySetPhase(state, InvestigationPhase.Report, out string reportFeedback), reportFeedback);
            if (checkpoint == InvestigationQaCheckpoint.ReportQuestions) return state;
            Require(updater.TrySetFinalThreat(state, "longline", out string causeFeedback), causeFeedback);
            SelectEvidence(updater, state, "E01_SHARK_NONDETECTION");
            SelectEvidence(updater, state, "E02_TUNA_WIDER_DETECTION");
            SelectEvidence(updater, state, "E04_BENTHIC_STABLE");
            SelectEvidence(updater, state, "E07_FISHING_LINE");
            Require(updater.TrySetReasoning(state, "food_web_cascade", out string reasoningFeedback), reasoningFeedback);
            Require(updater.TrySetLimitation(state, "L01_NONDETECTION_LIMITATION", out string limitationFeedback), limitationFeedback);
            if (checkpoint == InvestigationQaCheckpoint.CaseClosed)
            {
                InvestigationConclusionResult result = updater.SubmitFinal(state);
                Require(result.Status == InvestigationConclusionStatus.Correct, result.Feedback);
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

        private static void SelectEvidence(InvestigationStateUpdater updater, InvestigationState state, string evidenceId)
        {
            Require(updater.TrySetReportEvidence(state, evidenceId, true, out string feedback), feedback);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(string.IsNullOrEmpty(message) ? "QA checkpoint could not build a valid state." : message);
        }
    }
}
#endif
