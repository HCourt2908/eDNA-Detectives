using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;

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
                HandleDiscoverObservation,
                HandleRunThreat,
                HandleCompareEvidence,
                HandleSubmitProvisional,
                HandleSetFinalThreat,
                HandleRestart,
                HandleRecordModelConclusion);
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
                if (chooseFirstIdea)
                    view.Refresh(state, "Review a completed model card to choose your explanation.", InvestigationStatusTone.Notice);
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
                    ? $"Model running: {threat.DisplayName}. Watch its groups change, then compare the completed predictions."
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
            bool success = updater.TryReviewModelExplanation(state, threatId, out string feedback);
            view.Refresh(state, success ? "Review your survey findings and record your best-fitting explanation." : feedback, success ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleSetFinalThreat(string threatId)
        {
            if (!HasActiveSession) return;
            bool success = updater.TrySetFinalThreat(state, threatId, out string feedback);
            if (success) InvestigationSessionBridge.ClearResult();
            view.Refresh(state, success ? "Explanation updated. Review your findings and send the report." : feedback, success ? InvestigationStatusTone.Guide : InvestigationStatusTone.Warning);
        }

        private void HandleRecordModelConclusion()
        {
            if (!HasActiveSession || state.ConclusionStatus == InvestigationConclusionStatus.Correct) return;
            string chosen = string.IsNullOrEmpty(state.FinalThreatId) ? state.ProvisionalThreatId : state.FinalThreatId;
            InvestigationConclusionResult result = updater.SubmitModelConclusion(state, chosen);
            if (result.Status != InvestigationConclusionStatus.InsufficientEvidence) PublishInvestigationResult(result.Status);
            view.Refresh(state, result.Feedback, result.Status == InvestigationConclusionStatus.Correct
                ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void PublishInvestigationResult(InvestigationConclusionStatus status)
        {
            var alternatives = new List<string>();
            foreach (string id in caseDefinition.SupportedModelThreatIds)
                if (id != caseDefinition.PrimaryModelThreatId) alternatives.Add(id);
            InvestigationSessionBridge.PublishResult(new InvestigationGameResult
            {
                caseId = caseDefinition.CaseId,
                surveyId = state.SurveyId,
                siteId = state.SiteId,
                selectedHypothesisId = state.FinalThreatId,
                reviewedHypothesisIds = new List<string>(state.ReviewedModelThreatIds),
                compatibleHypothesisIds = caseDefinition.SupportedModelThreatIds.Count > 0
                    ? new List<string>(caseDefinition.SupportedModelThreatIds) : new List<string> { caseDefinition.CorrectThreatId },
                primaryHypothesisId = caseDefinition.PrimaryModelThreatId,
                alternativeHypothesisIds = alternatives,
                correct = status == InvestigationConclusionStatus.Correct,
                evidenceIds = new List<string>(state.SelectedReportEvidenceIds),
                surveySpeciesIds = new List<string>(state.SurveySpeciesIds),
                missteps = state.MisstepCount,
                finalSubmissionAttempts = state.FinalSubmissionAttemptCount,
                completed = status == InvestigationConclusionStatus.Correct
            });
        }

        private static string GetPhaseGuide(InvestigationPhase phase)
        {
            switch (phase)
            {
                case InvestigationPhase.Observe: return "Compare the baseline and current survey, then record unusual results.";
                case InvestigationPhase.Simulate: return "Run the overlapping causes and compare model predictions with your observations.";
                case InvestigationPhase.Report: return "Summarise your findings and record the best-fitting model.";
                default: return string.Empty;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Automated test setup only: no player-facing menu or checkpoint shortcuts.
        public void ApplyQaCheckpoint(InvestigationQaCheckpoint checkpoint)
        {
            if (!HasActiveSession) return;
            InvestigationDifficulty difficulty = state.Difficulty;
            state = InvestigationQaStateFactory.Create(caseDefinition, checkpoint);
            updater.SetDifficulty(state, difficulty);
            InvestigationSessionBridge.ClearResult();
            view.ResetPresentationState();
            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct) PublishInvestigationResult(state.ConclusionStatus);
            view.Refresh(state, $"QA checkpoint loaded: {checkpoint}.", InvestigationStatusTone.Guide);
        }

#endif
    }
}
