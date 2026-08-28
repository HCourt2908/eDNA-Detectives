#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using EDNA.Investigation.V2.Domain;

namespace EDNA.Investigation.V2
{
    public enum InvestigationV2QaCheckpoint
    {
        ObserveReady = 1,
        SimulateComplete = 2,
        ReportReady = 3,
        FinalReportReady = 4
    }

    public static class InvestigationV2QaStateFactory
    {
        public static InvestigationV2State Create(
            InvestigationV2CaseDefinition caseDefinition,
            InvestigationV2QaCheckpoint checkpoint)
        {
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));
            InvestigationV2StateUpdater updater = new InvestigationV2StateUpdater(caseDefinition);
            InvestigationV2State state = updater.CreateInitialState();
            Discover(updater, state, "E01_SHARK_NONDETECTION");
            Discover(updater, state, "E02_TUNA_WIDER_DETECTION");
            Discover(updater, state, "E03_KRILL_NONDETECTION");
            Discover(updater, state, "E04_BENTHIC_STABLE");
            Discover(updater, state, "E06_PLASTIC_INDICATOR_STABLE");
            if (checkpoint == InvestigationV2QaCheckpoint.ObserveReady) return state;

            Run(updater, state, "warming");
            Compare(updater, state, "warming", PredictionTargetKind.Temperature, "temperature", "E05_TEMPERATURE_NORMAL", ComparisonJudgement.Mismatch);
            Run(updater, state, "plastic");
            Compare(updater, state, "plastic", PredictionTargetKind.Species, "mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch);
            Run(updater, state, "longline");
            Compare(updater, state, "longline", PredictionTargetKind.Species, "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match);
            Compare(updater, state, "longline", PredictionTargetKind.Species, "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match);
            Compare(updater, state, "longline", PredictionTargetKind.Species, "krill", "E03_KRILL_NONDETECTION", ComparisonJudgement.Match);
            Compare(updater, state, "longline", PredictionTargetKind.Species, "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match);
            Run(updater, state, "bottom_trawling");
            Compare(updater, state, "bottom_trawling", PredictionTargetKind.Species, "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match);
            Compare(updater, state, "bottom_trawling", PredictionTargetKind.Species, "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch);
            if (checkpoint == InvestigationV2QaCheckpoint.SimulateComplete) return state;

            Require(updater.TrySubmitProvisional(state, "longline", out string provisionalFeedback), provisionalFeedback);
            if (checkpoint == InvestigationV2QaCheckpoint.ReportReady) return state;

            Require(updater.TryReviewConfirmation(state, out string confirmationFeedback), confirmationFeedback);
            Require(updater.TrySetFinalThreat(state, "longline", out string causeFeedback), causeFeedback);
            SelectEvidence(updater, state, "E01_SHARK_NONDETECTION");
            SelectEvidence(updater, state, "E02_TUNA_WIDER_DETECTION");
            SelectEvidence(updater, state, "E04_BENTHIC_STABLE");
            SelectEvidence(updater, state, "E07_FISHING_LINE");
            Require(updater.TrySetReasoning(state, "food_web_cascade", out string reasoningFeedback), reasoningFeedback);
            Require(updater.TrySetLimitation(state, "L01_NONDETECTION_LIMITATION", out string limitationFeedback), limitationFeedback);
            return state;
        }

        private static void Discover(InvestigationV2StateUpdater updater, InvestigationV2State state, string evidenceId)
        {
            Require(updater.TryDiscoverObservation(state, evidenceId, out string feedback), feedback);
        }

        private static void Run(InvestigationV2StateUpdater updater, InvestigationV2State state, string threatId)
        {
            Require(updater.TryRunThreat(state, threatId, out SimulationResult _, out string feedback), feedback);
        }

        private static void Compare(
            InvestigationV2StateUpdater updater,
            InvestigationV2State state,
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            PredictionComparisonRecord record = updater.Compare(state, threatId, targetKind, targetId, evidenceId, judgement);
            Require(record.CompletesObjective, record.Feedback);
        }

        private static void SelectEvidence(InvestigationV2StateUpdater updater, InvestigationV2State state, string evidenceId)
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
