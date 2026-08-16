using EDNA.Investigation.Domain;
using UnityEngine;

namespace EDNA.Investigation
{
    public sealed class InvestigationController : MonoBehaviour
    {
        private InvestigationCaseDefinition caseDefinition;
        private InvestigationStateUpdater stateUpdater;
        private MockSampleResultProvider sampleResultProvider;
        private InvestigationRuntimeView view;
        private InvestigationState state;

        public InvestigationState State => state;

        public void Initialize(
            InvestigationCaseDefinition definition,
            InvestigationRuntimeView runtimeView)
        {
            caseDefinition = definition;
            view = runtimeView;

            if (caseDefinition == null)
            {
                view.ShowFatalError("The Investigation demo case is missing.");
                return;
            }

            InvestigationCaseValidator validator = new InvestigationCaseValidator();
            var validationErrors = validator.Validate(caseDefinition);
            if (validationErrors.Count > 0)
            {
                view.ShowFatalError("The Investigation demo case is invalid:\n- " + string.Join("\n- ", validationErrors));
                return;
            }

            stateUpdater = new InvestigationStateUpdater(caseDefinition);
            sampleResultProvider = new MockSampleResultProvider(caseDefinition);
            view.Bind(
                caseDefinition,
                HandleSelectHypothesis,
                HandleAssignEvidence,
                HandleIdentifyAnomaly,
                HandleRequestSample,
                HandleSubmitConclusion,
                RestartCase);
            RestartCase();
        }

        private void RestartCase()
        {
            if (stateUpdater == null)
            {
                return;
            }

            state = stateUpdater.CreateInitialState();
            view.Refresh(state, "Case loaded. Review the historical records and current results.");
        }

        private void HandleSelectHypothesis(string hypothesisId)
        {
            if (stateUpdater.TrySelectHypothesis(state, hypothesisId, out string error))
            {
                view.Refresh(state, "Working hypothesis selected.", InvestigationStatusTone.Success);
            }
            else
            {
                view.Refresh(state, error, InvestigationStatusTone.Warning);
            }
        }

        private void HandleAssignEvidence(
            string evidenceId,
            string hypothesisId,
            EvidenceAssignmentKind assignmentKind)
        {
            if (stateUpdater.TryAssignEvidence(
                    state,
                    evidenceId,
                    hypothesisId,
                    assignmentKind,
                    out string error))
            {
                HypothesisEvaluation evaluation = stateUpdater.EvaluateHypothesis(state, hypothesisId);
                view.Refresh(
                    state,
                    $"Evidence assigned. Hypothesis status: {evaluation.Status}.",
                    InvestigationStatusTone.Success);
            }
            else
            {
                view.Refresh(state, error, InvestigationStatusTone.Warning);
            }
        }

        private void HandleIdentifyAnomaly(string evidenceId, AnomalyClaimType claimType)
        {
            bool identified = stateUpdater.TryIdentifyAnomaly(state, evidenceId, claimType, out string feedback);
            view.Refresh(
                state,
                feedback,
                identified ? InvestigationStatusTone.Success : InvestigationStatusTone.Warning);
        }

        private void HandleRequestSample(
            string siteId,
            EDNA.Core.DepthBand depthBand,
            string hypothesisId,
            string reasonEvidenceId)
        {
            if (!stateUpdater.TryPlanSample(
                    state,
                    siteId,
                    depthBand,
                    hypothesisId,
                    reasonEvidenceId,
                    out InvestigationSamplePlan plan,
                    out string error))
            {
                view.Refresh(state, error, InvestigationStatusTone.Warning);
                return;
            }

            var result = sampleResultProvider.CreateResult(plan);
            if (!stateUpdater.ApplyResult(state, result, out error))
            {
                stateUpdater.TryCancelPlannedSample(state, plan.Request.requestId, out _);
                view.Refresh(state, error, InvestigationStatusTone.Warning);
                return;
            }

            view.Refresh(
                state,
                $"Mock sample complete: {result.detectedSpeciesIds.Count} species detected at {depthBand} depth.",
                InvestigationStatusTone.Success);
        }

        private void HandleSubmitConclusion()
        {
            ConclusionResult result = stateUpdater.SubmitConclusion(state);
            string classificationReview = state.MisclassificationCount == 0
                ? "No incorrect classifications were recorded."
                : $"Classification review: {state.MisclassificationCount} incorrect option(s) were ruled out during the investigation.";
            view.Refresh(
                state,
                $"{result.Feedback} {classificationReview}",
                result.Status == ConclusionStatus.Correct
                    ? InvestigationStatusTone.Success
                    : InvestigationStatusTone.Warning);
        }
    }
}
