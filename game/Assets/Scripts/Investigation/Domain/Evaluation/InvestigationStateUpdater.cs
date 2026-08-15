using System;
using EDNA.Core;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationStateUpdater
    {
        private readonly InvestigationCaseDefinition caseDefinition;
        private readonly EvidenceEvaluator evidenceEvaluator;
        private readonly HypothesisEvaluator hypothesisEvaluator;
        private readonly ConclusionEvaluator conclusionEvaluator;

        public InvestigationStateUpdater(InvestigationCaseDefinition caseDefinition)
        {
            this.caseDefinition = caseDefinition ?? throw new ArgumentNullException(nameof(caseDefinition));
            evidenceEvaluator = new EvidenceEvaluator();
            hypothesisEvaluator = new HypothesisEvaluator();
            conclusionEvaluator = new ConclusionEvaluator(hypothesisEvaluator);
        }

        public InvestigationState CreateInitialState()
        {
            InvestigationState state = new InvestigationState(caseDefinition.FollowUpSampleLimit);
            for (int index = 0; index < caseDefinition.InitialResults.Count; index++)
            {
                state.AddInitialResult(caseDefinition.InitialResults[index]);
            }

            RecalculateEvidence(state);
            return state;
        }

        public bool TryPlanSample(
            InvestigationState state,
            string siteId,
            DepthBand depthBand,
            string relatedHypothesisId,
            string reasonEvidenceId,
            out InvestigationSamplePlan plan,
            out string error)
        {
            plan = null;
            error = string.Empty;

            if (state == null)
            {
                error = "Investigation state is missing.";
                return false;
            }

            if (state.AvailableSampleSlots <= 0)
            {
                error = "No follow-up samples remain.";
                return false;
            }

            SampleSiteDefinition site = caseDefinition.FindSite(siteId);
            if (site == null)
            {
                error = "Choose a valid sample site.";
                return false;
            }

            if (!site.SupportsDepth(depthBand))
            {
                error = $"{site.DisplayName} does not support the selected depth.";
                return false;
            }

            if (!string.IsNullOrEmpty(relatedHypothesisId)
                && caseDefinition.FindHypothesis(relatedHypothesisId) == null)
            {
                error = "Choose a valid hypothesis.";
                return false;
            }

            if (!string.IsNullOrEmpty(reasonEvidenceId) && state.FindEvidence(reasonEvidenceId) == null)
            {
                error = "Choose an available observation as the sampling reason.";
                return false;
            }

            if (!string.IsNullOrEmpty(reasonEvidenceId) && !state.IsEvidenceIdentified(reasonEvidenceId))
            {
                error = "Identify this observation before using it as a sampling reason.";
                return false;
            }

            int nextRound = state.CurrentRound + 1;
            SampleRequest request = new SampleRequest
            {
                requestId = $"request_{nextRound:00}",
                siteId = siteId,
                depthBand = depthBand
            };
            plan = new InvestigationSamplePlan(
                request,
                nextRound,
                relatedHypothesisId,
                reasonEvidenceId);
            state.AddPlan(plan);
            return true;
        }

        public bool ApplyResult(InvestigationState state, EDNAResultData result, out string error)
        {
            error = string.Empty;
            if (state == null)
            {
                error = "Investigation state is missing.";
                return false;
            }

            if (result == null)
            {
                error = "The sample result is missing.";
                return false;
            }

            if (state.ContainsResult(result))
            {
                error = "This sample result has already been added.";
                return false;
            }

            state.AddResult(result);
            state.CompletePlan(result.requestId);
            RecalculateEvidence(state);
            return true;
        }

        public bool TryCancelPlannedSample(
            InvestigationState state,
            string requestId,
            out string error)
        {
            error = string.Empty;
            if (state == null)
            {
                error = "Investigation state is missing.";
                return false;
            }

            if (!state.CancelPendingPlan(requestId))
            {
                error = "No pending sample request matched this cancellation.";
                return false;
            }

            return true;
        }

        public bool TryAssignEvidence(
            InvestigationState state,
            string evidenceId,
            string hypothesisId,
            EvidenceAssignmentKind assignmentKind,
            out string error)
        {
            error = string.Empty;
            if (state == null)
            {
                error = "Investigation state is missing.";
                return false;
            }

            if (state.FindEvidence(evidenceId) == null)
            {
                error = "Choose an available observation.";
                return false;
            }

            if (!state.IsEvidenceIdentified(evidenceId))
            {
                error = "Identify this observation in Compare Data before using it in a hypothesis.";
                return false;
            }

            if (caseDefinition.FindHypothesis(hypothesisId) == null)
            {
                error = "Choose a valid hypothesis.";
                return false;
            }

            state.AssignEvidence(evidenceId, hypothesisId, assignmentKind);
            return true;
        }

        public bool TryIdentifyAnomaly(
            InvestigationState state,
            string evidenceId,
            AnomalyClaimType claimType,
            out string feedback)
        {
            feedback = string.Empty;
            if (state == null)
            {
                feedback = "Investigation state is missing.";
                return false;
            }

            EvidenceRecord evidence = state.FindEvidence(evidenceId);
            if (evidence == null)
            {
                feedback = "Select a comparison card first.";
                return false;
            }

            if (!ClaimMatchesEvidence(claimType, evidence.EvidenceType))
            {
                bool recorded = state.RecordMisclassification(evidenceId, claimType);
                string attemptStatus = recorded
                    ? $"Incorrect classification recorded. Misclassifications: {state.MisclassificationCount}."
                    : "That classification was already ruled out for this card.";
                feedback = $"{attemptStatus} {GetClassificationHint(claimType)} Review the comparison and try another option.";
                return false;
            }

            state.IdentifyEvidence(evidenceId);
            feedback = $"Finding identified: {GetClaimDisplayName(claimType)}.";
            return true;
        }

        public bool TrySelectHypothesis(InvestigationState state, string hypothesisId, out string error)
        {
            error = string.Empty;
            if (state == null)
            {
                error = "Investigation state is missing.";
                return false;
            }

            if (caseDefinition.FindHypothesis(hypothesisId) == null)
            {
                error = "Choose a valid hypothesis.";
                return false;
            }

            state.SelectHypothesis(hypothesisId);
            return true;
        }

        public HypothesisEvaluation EvaluateHypothesis(InvestigationState state, string hypothesisId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            HypothesisDefinition hypothesis = caseDefinition.FindHypothesis(hypothesisId);
            if (hypothesis == null)
            {
                return new HypothesisEvaluation(
                    hypothesisId,
                    HypothesisStatus.Unexplored,
                    0,
                    0,
                    "Hypothesis not found.");
            }

            return hypothesisEvaluator.Evaluate(hypothesis, state);
        }

        public ConclusionResult SubmitConclusion(InvestigationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ConclusionResult result = conclusionEvaluator.Evaluate(caseDefinition, state);
            state.SetConclusionStatus(result.Status);
            return result;
        }

        public void RecalculateEvidence(InvestigationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.ReplaceEvidence(evidenceEvaluator.Evaluate(caseDefinition, state.AllResults));
        }

        private static bool ClaimMatchesEvidence(AnomalyClaimType claimType, EvidenceType evidenceType)
        {
            switch (claimType)
            {
                case AnomalyClaimType.NewArrival:
                    return evidenceType == EvidenceType.NewDetection;
                case AnomalyClaimType.ExpectedButMissing:
                    return evidenceType == EvidenceType.NotDetectedInSample
                        || evidenceType == EvidenceType.RepeatedNonDetection;
                case AnomalyClaimType.DifferentDepth:
                    return evidenceType == EvidenceType.DepthShift;
                case AnomalyClaimType.ResultWarning:
                    return evidenceType == EvidenceType.LowQualityResult
                        || evidenceType == EvidenceType.ContaminationWarning;
                case AnomalyClaimType.MatchesBaseline:
                    return evidenceType == EvidenceType.StableIndicator
                        || evidenceType == EvidenceType.RepeatedDetection;
                default:
                    return false;
            }
        }

        private static string GetClassificationHint(AnomalyClaimType claimType)
        {
            switch (claimType)
            {
                case AnomalyClaimType.NewArrival:
                    return "That does not look like a new arrival. Compare the current detection with the historical record.";
                case AnomalyClaimType.ExpectedButMissing:
                    return "That species is not an expected-but-missing observation in this sample.";
                case AnomalyClaimType.DifferentDepth:
                    return "A depth shift needs evidence from two depths at the same site.";
                case AnomalyClaimType.ResultWarning:
                    return "That card does not show a sample-quality or contamination warning.";
                case AnomalyClaimType.MatchesBaseline:
                    return "That observation does not match the historical baseline.";
                default:
                    return "Compare the current sample with the historical record and try again.";
            }
        }

        private static string GetClaimDisplayName(AnomalyClaimType claimType)
        {
            switch (claimType)
            {
                case AnomalyClaimType.NewArrival:
                    return "New Arrival";
                case AnomalyClaimType.ExpectedButMissing:
                    return "Expected but Missing";
                case AnomalyClaimType.DifferentDepth:
                    return "Different Depth";
                case AnomalyClaimType.ResultWarning:
                    return "Result Warning";
                case AnomalyClaimType.MatchesBaseline:
                    return "Matches Baseline";
                default:
                    return claimType.ToString();
            }
        }
    }
}
