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
        private bool useStandaloneSurvey;

        public InvestigationState State => state;
        private bool HasActiveSession => state != null && updater != null && view != null;

        public void Initialize(InvestigationCaseDefinition definition, InvestigationRuntimeView runtimeView)
        {
            state = null;
            updater = null;
            useStandaloneSurvey = false;
            InvestigationSessionBridge.ClearResult();
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
                HandleCompareEvidence,
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
            if (updater == null || view == null) return;
            InvestigationDifficulty retainedDifficulty = state == null
                ? InvestigationDifficulty.Easy
                : state.Difficulty;
            InvestigationSessionBridge.ClearResult();
            state = null;
            InvestigationState initialState = updater.CreateInitialState();
            updater.SetDifficulty(initialState, retainedDifficulty);
            string openingMessage = "Processed survey ready. Compare it with the historical baseline and record every unusual species pattern.";
            InvestigationStatusTone openingTone = InvestigationStatusTone.Guide;
            if (!useStandaloneSurvey && InvestigationSessionBridge.PendingInput != null)
            {
                if (!updater.TryApplyExternalInput(initialState, InvestigationSessionBridge.PendingInput, out string importFeedback))
                {
                    view.ShowFatalError(importFeedback, HandleStartStandalone);
                    return;
                }
                openingMessage = importFeedback;
                openingTone = InvestigationStatusTone.Notice;
            }
            // Only publish a playable session after its input has been validated.
            state = initialState;
            view.ResetPresentationState();
            view.Refresh(state, openingMessage, openingTone);
        }

        private void HandleStartStandalone()
        {
            useStandaloneSurvey = true;
            HandleRestart();
        }

        private void HandleSetPhase(InvestigationPhase phase)
        {
            if (!HasActiveSession) return;
            bool chooseFirstIdea = phase == InvestigationPhase.Report
                && string.IsNullOrEmpty(state.ProvisionalThreatId)
                && updater.EvaluateReadiness(state).CanEnterProvisional;
            InvestigationPhase nextPhase = chooseFirstIdea ? InvestigationPhase.Simulate : phase;
            if (updater.TrySetPhase(state, nextPhase, out string feedback))
            {
                view.Refresh(state, string.IsNullOrEmpty(feedback) ? GetPhaseGuide(nextPhase) : feedback, InvestigationStatusTone.Guide);
                if (chooseFirstIdea) view.OpenProvisionalReview();
            }
            else
            {
                view.Refresh(state, feedback, InvestigationStatusTone.Warning);
            }
        }

        private void HandleSetDifficulty(InvestigationDifficulty difficulty)
        {
            if (!HasActiveSession) return;
            updater.SetDifficulty(state, difficulty);
            view.Refresh(state, difficulty == InvestigationDifficulty.Easy
                ? "Easy guidance enabled: the next Case Question, related clues and exact next step are highlighted."
                : "Hard guidance enabled: all evidence remains available, but guided targets are hidden.", InvestigationStatusTone.Guide);
        }

        private void HandleDiscoverObservation(string evidenceId)
        {
            if (!HasActiveSession) return;
            bool success = updater.TryDiscoverObservation(state, evidenceId, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleRunThreat(string threatId)
        {
            if (!HasActiveSession) return;
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

        private void HandleCompareEvidence(
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId)
        {
            if (!HasActiveSession) return;
            PredictionComparisonRecord record = updater.CompareEvidence(state, threatId, targetKind, targetId, evidenceId);
            bool incorrect = record.Outcome == ComparisonEvaluationOutcome.Incorrect;
            view.Refresh(
                state,
                incorrect ? $"Latest attempt: {record.Outcome}. {record.Feedback}" : record.Feedback,
                incorrect
                    ? InvestigationStatusTone.Warning
                    : record.LocksComparison ? InvestigationStatusTone.Success : InvestigationStatusTone.Guide);
        }

        private void HandleSubmitProvisional(string threatId)
        {
            if (!HasActiveSession) return;
            bool success = updater.TrySubmitProvisional(state, threatId, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleReviewConfirmation()
        {
            if (!HasActiveSession) return;
            bool success = updater.TryReviewConfirmation(state, out string feedback);
            view.Refresh(state, feedback, success ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleSetFinalThreat(string threatId)
        {
            if (!HasActiveSession) return;
            bool success = updater.TrySetFinalThreat(state, threatId, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Final cause updated. Complete the remaining report sections." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleSetReportEvidence(string evidenceId, bool selected)
        {
            if (!HasActiveSession) return;
            bool success = updater.TrySetReportEvidence(state, evidenceId, selected, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Report evidence updated." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleSetReasoning(string reasoningId)
        {
            if (!HasActiveSession) return;
            bool success = updater.TrySetReasoning(state, reasoningId, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Reasoning updated." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleSetLimitation(string limitationId)
        {
            if (!HasActiveSession) return;
            bool success = updater.TrySetLimitation(state, limitationId, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Scientific limitation recorded." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleSubmitFinal()
        {
            if (!HasActiveSession) return;
            InvestigationConclusionResult result = updater.SubmitFinal(state);
            if (result.Status != InvestigationConclusionStatus.Correct) view.RequestReportFeedbackFocus(result.Feedback);
            if (result.Status != InvestigationConclusionStatus.InsufficientEvidence)
            {
                PublishInvestigationResult(result.Status);
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

        private void PublishInvestigationResult(InvestigationConclusionStatus status)
        {
            InvestigationSessionBridge.PublishResult(new InvestigationGameResult
            {
                caseId = caseDefinition.CaseId,
                surveyId = state.SurveyId,
                siteId = state.SiteId,
                selectedHypothesisId = state.FinalThreatId,
                correct = status == InvestigationConclusionStatus.Correct,
                evidenceIds = new List<string>(state.SelectedReportEvidenceIds),
                surveySpeciesIds = new List<string>(state.SurveySpeciesIds),
                missteps = state.MisstepCount,
                finalSubmissionAttempts = state.FinalSubmissionAttemptCount,
                completed = status == InvestigationConclusionStatus.Correct
            });
        }

        private void HandleSetReducedMotion(bool reducedMotion)
        {
            InvestigationMotionSettings.SetReducedMotion(reducedMotion);
            if (!HasActiveSession)
            {
                view?.RefreshMotionPreference();
                return;
            }
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
        private bool qaMenuOpen;
        private static readonly InvestigationQaCheckpoint[] QaCheckpoints =
        {
            InvestigationQaCheckpoint.ObserveReady,
            InvestigationQaCheckpoint.SimulateComplete,
            InvestigationQaCheckpoint.FinalReportReady
        };
        private static readonly string[] QaLabels =
        {
            "Observe", "Simulate", "Report"
        };

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool modifier = (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            if (!modifier) return;
            if (keyboard.digit1Key.wasPressedThisFrame) ApplyQaCheckpoint(QaCheckpoints[0]);
            else if (keyboard.digit2Key.wasPressedThisFrame) ApplyQaCheckpoint(QaCheckpoints[1]);
            else if (keyboard.digit3Key.wasPressedThisFrame) ApplyQaCheckpoint(QaCheckpoints[2]);
        }

        public void ApplyQaCheckpoint(InvestigationQaCheckpoint checkpoint)
        {
            if (!HasActiveSession) return;
            InvestigationDifficulty difficulty = state.Difficulty;
            state = InvestigationQaStateFactory.Create(caseDefinition, checkpoint);
            updater.SetDifficulty(state, difficulty);
            InvestigationSessionBridge.ClearResult();
            view.ResetPresentationState();
            if (checkpoint == InvestigationQaCheckpoint.EvidenceReady) view.PrepareQaEvidenceChoice();
            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct) PublishInvestigationResult(state.ConclusionStatus);
            SetQaMenuOpen(false);
            view.Refresh(state, $"QA checkpoint loaded: {checkpoint}.", InvestigationStatusTone.Guide);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !HasActiveSession) return;
            const float height = 26f;
            const float width = 112f;
            float y = Mathf.Max(4f, Screen.height - height - 4f);
            if (GUI.Button(new Rect(8f, y, 86f, height), qaMenuOpen ? "QA tools ▾" : "QA tools ▸")) SetQaMenuOpen(!qaMenuOpen);
            if (!qaMenuOpen) return;
            int columns = Mathf.Clamp(Mathf.FloorToInt((Screen.width - 16f) / (width + 4f)), 1, 3);
            int rows = Mathf.CeilToInt(QaCheckpoints.Length / (float)columns);
            float top = Mathf.Max(4f, y - rows * (height + 4f) - 8f);
            GUI.Box(new Rect(4f, top - 4f, columns * (width + 4f) + 8f, rows * (height + 4f) + 8f), string.Empty);
            for (int index = 0; index < QaCheckpoints.Length; index++)
            {
                Rect rect = new Rect(8f + (index % columns) * (width + 4f), top + (index / columns) * (height + 4f), width, height);
                GUIContent label = new GUIContent(QaLabels[index], $"Ctrl+Shift+{index + 1} · Fill this stage");
                if (GUI.Button(rect, label)) ApplyQaCheckpoint(QaCheckpoints[index]);
            }
        }

        private void SetQaMenuOpen(bool open)
        {
            qaMenuOpen = open;
            if (view != null && view.TryGetComponent(out UnityEngine.UI.GraphicRaycaster raycaster))
                raycaster.enabled = !open;
        }

        private void OnDisable()
        {
            if (qaMenuOpen) SetQaMenuOpen(false);
        }
#endif
    }
}
