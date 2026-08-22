using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.V2.Domain;
using UnityEngine;

namespace EDNA.Investigation.V2
{
    public enum InvestigationV2StatusTone
    {
        Guide,
        Success,
        Warning
    }

    [DisallowMultipleComponent]
    public sealed class InvestigationV2Controller : MonoBehaviour
    {
        private InvestigationV2CaseDefinition caseDefinition;
        private InvestigationV2StateUpdater updater;
        private InvestigationV2RuntimeView view;
        private InvestigationV2State state;

        public InvestigationV2State State => state;

        public void Initialize(InvestigationV2CaseDefinition definition, InvestigationV2RuntimeView runtimeView)
        {
            caseDefinition = definition;
            view = runtimeView;
            if (view == null)
            {
                Debug.LogError("Investigation V2 runtime view is missing.");
                return;
            }
            if (caseDefinition == null)
            {
                view.ShowFatalError("The Investigation V2 case is missing.");
                return;
            }

            var errors = new InvestigationV2CaseValidator().Validate(caseDefinition);
            if (errors.Count > 0)
            {
                view.ShowFatalError("The Investigation V2 case is invalid:\n- " + string.Join("\n- ", errors));
                return;
            }

            updater = new InvestigationV2StateUpdater(caseDefinition);
            view.Bind(
                caseDefinition,
                HandleSetPhase,
                HandleSetDifficulty,
                HandleDiscoverObservation,
                HandleRunThreat,
                HandleCompare,
                HandleSubmitProvisional,
                HandleReviewConfirmation,
                HandleSetFinalThreat,
                HandleSetReportEvidence,
                HandleSetReasoning,
                HandleSetLimitation,
                HandleSubmitFinal,
                HandleRestart,
                HandleSetReducedMotion);
            HandleRestart();
        }

        private void HandleRestart()
        {
            state = updater.CreateInitialState();
            if (InvestigationV2SessionBridge.PendingInput != null)
            {
                updater.TryApplyExternalInput(state, InvestigationV2SessionBridge.PendingInput, out _);
            }
            view.ResetPresentationState();
            view.Refresh(state, "Start with today's survey and record every unusual species pattern.", InvestigationV2StatusTone.Guide);
        }

        private void HandleSetPhase(InvestigationV2Phase phase)
        {
            if (updater.TrySetPhase(state, phase, out string feedback))
            {
                view.Refresh(state, string.IsNullOrEmpty(feedback) ? GetPhaseGuide(phase) : feedback, InvestigationV2StatusTone.Guide);
            }
            else
            {
                view.Refresh(state, feedback, InvestigationV2StatusTone.Warning);
            }
        }

        private void HandleSetDifficulty(InvestigationV2Difficulty difficulty)
        {
            updater.SetDifficulty(state, difficulty);
            view.Refresh(state, difficulty == InvestigationV2Difficulty.Easy
                ? "Easy guidance enabled: relevant observations are highlighted and feedback is detailed."
                : "Hard guidance enabled: candidate observations remain available without relevance highlighting.", InvestigationV2StatusTone.Guide);
        }

        private void HandleDiscoverObservation(string evidenceId)
        {
            bool success = updater.TryDiscoverObservation(state, evidenceId, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationV2StatusTone.Success : InvestigationV2StatusTone.Warning);
        }

        private void HandleRunThreat(string threatId)
        {
            ThreatSimulationDefinition threat = caseDefinition.FindThreat(threatId);
            if (threat == null)
            {
                view.Refresh(state, "Choose an available cause before running the model.", InvestigationV2StatusTone.Warning);
                return;
            }
            bool success = updater.TryRunThreat(state, threatId, out _, out string feedback);
            view.Refresh(
                state,
                success
                    ? $"Model complete: {threat.DisplayName}. Select one prediction and one related observation."
                    : feedback,
                success ? InvestigationV2StatusTone.Success : InvestigationV2StatusTone.Warning);
        }

        private void HandleCompare(string threatId, string speciesId, string evidenceId, ComparisonJudgement judgement)
        {
            PredictionComparisonRecord record = updater.Compare(state, threatId, speciesId, evidenceId, judgement);
            bool incorrect = record.Outcome == ComparisonEvaluationOutcome.Incorrect;
            view.Refresh(
                state,
                incorrect ? $"Latest attempt: {record.Outcome}. {record.Feedback}" : record.Feedback,
                incorrect
                    ? InvestigationV2StatusTone.Warning
                    : InvestigationV2StatusTone.Success);
        }

        private void HandleSubmitProvisional(string threatId)
        {
            bool success = updater.TrySubmitProvisional(state, threatId, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationV2StatusTone.Success : InvestigationV2StatusTone.Warning);
        }

        private void HandleReviewConfirmation()
        {
            bool success = updater.TryReviewConfirmation(state, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationV2StatusTone.Success : InvestigationV2StatusTone.Warning);
        }

        private void HandleSetFinalThreat(string threatId)
        {
            bool success = updater.TrySetFinalThreat(state, threatId, out string feedback);
            view.Refresh(state, success ? "Final cause updated. Complete the remaining report sections." : feedback, success ? InvestigationV2StatusTone.Guide : InvestigationV2StatusTone.Warning);
        }

        private void HandleSetReportEvidence(string evidenceId, bool selected)
        {
            bool success = updater.TrySetReportEvidence(state, evidenceId, selected, out string feedback);
            view.Refresh(state, success ? "Report evidence updated." : feedback, success ? InvestigationV2StatusTone.Guide : InvestigationV2StatusTone.Warning);
        }

        private void HandleSetReasoning(string reasoningId)
        {
            bool success = updater.TrySetReasoning(state, reasoningId, out string feedback);
            view.Refresh(state, success ? "Reasoning updated." : feedback, success ? InvestigationV2StatusTone.Guide : InvestigationV2StatusTone.Warning);
        }

        private void HandleSetLimitation(string limitationId)
        {
            bool success = updater.TrySetLimitation(state, limitationId, out string feedback);
            view.Refresh(state, success ? "Scientific limitation recorded." : feedback, success ? InvestigationV2StatusTone.Guide : InvestigationV2StatusTone.Warning);
        }

        private void HandleSubmitFinal()
        {
            InvestigationV2ConclusionResult result = updater.SubmitFinal(state);
            InvestigationV2SessionBridge.PublishResult(new InvestigationGameResult
            {
                caseId = caseDefinition.CaseId,
                selectedHypothesisId = state.FinalThreatId,
                correct = result.Status == InvestigationV2ConclusionStatus.Correct,
                evidenceIds = new List<string>(state.SelectedReportEvidenceIds),
                missteps = state.MisstepCount,
                finalSubmissionAttempts = state.FinalSubmissionAttemptCount,
                completed = result.Status == InvestigationV2ConclusionStatus.Correct
            });
            view.Refresh(
                state,
                result.Feedback,
                result.Status == InvestigationV2ConclusionStatus.Correct
                    ? InvestigationV2StatusTone.Success
                    : InvestigationV2StatusTone.Warning);
        }

        private void HandleSetReducedMotion(bool reducedMotion)
        {
            InvestigationV2MotionSettings.SetReducedMotion(reducedMotion);
            view.Refresh(state, reducedMotion ? "Reduced motion enabled." : "Full motion enabled.", InvestigationV2StatusTone.Guide);
        }

        private static string GetPhaseGuide(InvestigationV2Phase phase)
        {
            switch (phase)
            {
                case InvestigationV2Phase.Observe: return "Compare the baseline and current survey, then record unusual results.";
                case InvestigationV2Phase.Simulate: return "Run the overlapping causes and compare model predictions with your observations.";
                case InvestigationV2Phase.Report: return "Record a provisional explanation, review ROV confirmation, then complete the final report.";
                default: return string.Empty;
            }
        }
    }
}
