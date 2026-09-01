using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace EDNA.Investigation
{
    public enum InvestigationStatusTone
    {
        Guide,
        Notice,
        Success,
        Warning
    }

    [DisallowMultipleComponent]
    public sealed class InvestigationController : MonoBehaviour
    {
        private InvestigationCaseDefinition caseDefinition;
        private InvestigationStateUpdater updater;
        private InvestigationRuntimeView view;
        private InvestigationState state;

        public InvestigationState State => state;

        public void Initialize(InvestigationCaseDefinition definition, InvestigationRuntimeView runtimeView)
        {
            caseDefinition = definition;
            view = runtimeView;
            if (view == null)
            {
                Debug.LogError("Investigation runtime view is missing.");
                return;
            }
            if (caseDefinition == null)
            {
                view.ShowFatalError("The Investigation case is missing.");
                return;
            }

            var errors = new InvestigationCaseValidator().Validate(caseDefinition);
            if (errors.Count > 0)
            {
                view.ShowFatalError("The Investigation case is invalid:\n- " + string.Join("\n- ", errors));
                return;
            }

            updater = new InvestigationStateUpdater(caseDefinition);
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
            InvestigationDifficulty retainedDifficulty = state == null
                ? InvestigationDifficulty.Easy
                : state.Difficulty;
            InvestigationSessionBridge.ClearResult();
            state = updater.CreateInitialState();
            updater.SetDifficulty(state, retainedDifficulty);
            string openingMessage = "Processed survey ready. Compare it with the historical baseline and record every unusual species pattern.";
            InvestigationStatusTone openingTone = InvestigationStatusTone.Guide;
            if (InvestigationSessionBridge.PendingInput != null)
            {
                if (!updater.TryApplyExternalInput(state, InvestigationSessionBridge.PendingInput, out string importFeedback))
                {
                    view.ResetPresentationState();
                    view.ShowFatalError(importFeedback);
                    return;
                }
                openingMessage = importFeedback;
                openingTone = InvestigationStatusTone.Notice;
            }
            view.ResetPresentationState();
            view.Refresh(state, openingMessage, openingTone);
        }

        private void HandleSetPhase(InvestigationPhase phase)
        {
            if (updater.TrySetPhase(state, phase, out string feedback))
            {
                view.Refresh(state, string.IsNullOrEmpty(feedback) ? GetPhaseGuide(phase) : feedback, InvestigationStatusTone.Guide);
            }
            else
            {
                view.Refresh(state, feedback, InvestigationStatusTone.Warning);
            }
        }

        private void HandleSetDifficulty(InvestigationDifficulty difficulty)
        {
            updater.SetDifficulty(state, difficulty);
            view.Refresh(state, difficulty == InvestigationDifficulty.Easy
                ? "Easy guidance enabled: the next Case Question, related clues and exact next step are highlighted."
                : "Hard guidance enabled: all evidence remains available, but guided targets are hidden.", InvestigationStatusTone.Guide);
        }

        private void HandleDiscoverObservation(string evidenceId)
        {
            bool success = updater.TryDiscoverObservation(state, evidenceId, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleRunThreat(string threatId)
        {
            ThreatSimulationDefinition threat = caseDefinition.FindThreat(threatId);
            if (threat == null)
            {
                view.Refresh(state, "Choose an available cause before running the model.", InvestigationStatusTone.Warning);
                return;
            }
            bool success = updater.TryRunThreat(state, threatId, out _, out string feedback);
            view.Refresh(
                state,
                success
                    ? $"Model complete: {threat.DisplayName}. Select one prediction and one related observation."
                    : feedback,
                success ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleCompare(
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            PredictionComparisonRecord record = updater.Compare(state, threatId, targetKind, targetId, evidenceId, judgement);
            bool incorrect = record.Outcome == ComparisonEvaluationOutcome.Incorrect;
            view.Refresh(
                state,
                incorrect ? $"Latest attempt: {record.Outcome}. {record.Feedback}" : record.Feedback,
                incorrect
                    ? InvestigationStatusTone.Warning
                    : InvestigationStatusTone.Success);
        }

        private void HandleSubmitProvisional(string threatId)
        {
            bool success = updater.TrySubmitProvisional(state, threatId, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleReviewConfirmation()
        {
            bool success = updater.TryReviewConfirmation(state, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleSetFinalThreat(string threatId)
        {
            bool success = updater.TrySetFinalThreat(state, threatId, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Final cause updated. Complete the remaining report sections." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleSetReportEvidence(string evidenceId, bool selected)
        {
            bool success = updater.TrySetReportEvidence(state, evidenceId, selected, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Report evidence updated." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleSetReasoning(string reasoningId)
        {
            bool success = updater.TrySetReasoning(state, reasoningId, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Reasoning updated." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleSetLimitation(string limitationId)
        {
            bool success = updater.TrySetLimitation(state, limitationId, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Scientific limitation recorded." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleSubmitFinal()
        {
            InvestigationConclusionResult result = updater.SubmitFinal(state);
            if (result.Status != InvestigationConclusionStatus.InsufficientEvidence)
            {
                InvestigationSessionBridge.PublishResult(new InvestigationGameResult
                {
                    caseId = caseDefinition.CaseId,
                    surveyId = state.SurveyId,
                    siteId = state.SiteId,
                    selectedHypothesisId = state.FinalThreatId,
                    correct = result.Status == InvestigationConclusionStatus.Correct,
                    evidenceIds = new List<string>(state.SelectedReportEvidenceIds),
                    missteps = state.MisstepCount,
                    finalSubmissionAttempts = state.FinalSubmissionAttemptCount,
                    completed = result.Status == InvestigationConclusionStatus.Correct
                });
            }
            view.Refresh(
                state,
                result.Feedback,
                result.Status == InvestigationConclusionStatus.Correct
                    ? InvestigationStatusTone.Success
                    : result.Status == InvestigationConclusionStatus.InsufficientEvidence
                        ? InvestigationStatusTone.Guide
                        : InvestigationStatusTone.Warning);
        }

        private void HandleSetReducedMotion(bool reducedMotion)
        {
            InvestigationMotionSettings.SetReducedMotion(reducedMotion);
            view.Refresh(state, reducedMotion ? "Reduced motion enabled." : "Full motion enabled.", InvestigationStatusTone.Guide);
        }

        private static string GetPhaseGuide(InvestigationPhase phase)
        {
            switch (phase)
            {
                case InvestigationPhase.Observe: return "Compare the baseline and current survey, then record unusual results.";
                case InvestigationPhase.Simulate: return "Run the overlapping causes and compare model predictions with your observations.";
                case InvestigationPhase.Report: return "Record a provisional explanation, review ROV confirmation, then complete the final report.";
                default: return string.Empty;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool modifier = (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            if (!modifier) return;
            if (keyboard.digit1Key.wasPressedThisFrame) ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            else if (keyboard.digit2Key.wasPressedThisFrame) ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            else if (keyboard.digit3Key.wasPressedThisFrame) ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
            else if (keyboard.digit4Key.wasPressedThisFrame) ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
        }

        public void ApplyQaCheckpoint(InvestigationQaCheckpoint checkpoint)
        {
            state = InvestigationQaStateFactory.Create(caseDefinition, checkpoint);
            InvestigationSessionBridge.ClearResult();
            view.ResetPresentationState();
            view.Refresh(state, $"QA checkpoint loaded: {checkpoint}.", InvestigationStatusTone.Guide);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            const float height = 28f;
            const float width = 106f;
            float y = Mathf.Max(4f, Screen.height - height - 6f);
            GUI.Box(new Rect(4f, y - 2f, width * 4f + 14f, height + 4f), string.Empty);
            if (GUI.Button(new Rect(8f, y, width, height), "QA Observe"))
                ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            if (GUI.Button(new Rect(10f + width, y, width, height), "QA Simulate"))
                ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            if (GUI.Button(new Rect(12f + width * 2f, y, width, height), "QA Report"))
                ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
            if (GUI.Button(new Rect(14f + width * 3f, y, width, height), "QA Final"))
                ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
        }
#endif
    }
}
