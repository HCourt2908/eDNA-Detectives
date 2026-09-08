using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum InvestigationGuidanceStep
        {
            ObserveFirstFinding,
            ObserveRemainingFindings,
            ObserveComplete,
            ChooseCause,
            RunModel,
            ChoosePrediction,
            ChooseObservation,
            ReviewEvidence,
            ContinueInvestigation,
            SubmitProvisional,
            ReviewRov,
            CompleteFinalReport,
            CaseClosed
        }

        private readonly struct InvestigationGuidance
        {
            public InvestigationGuidance(
                InvestigationGuidanceStep step,
                string label,
                string message,
                string directionHint = null,
                string actionHint = null)
            {
                Step = step;
                Label = label ?? string.Empty;
                Message = message ?? string.Empty;
                DirectionHint = string.IsNullOrWhiteSpace(directionHint) ? Message : directionHint;
                ActionHint = string.IsNullOrWhiteSpace(actionHint) ? DirectionHint : actionHint;
            }

            public InvestigationGuidanceStep Step { get; }
            public string Label { get; }
            public string Message { get; }
            public string DirectionHint { get; }
            public string ActionHint { get; }

            public string MessageForLevel(int level)
            {
                if (level >= 3) return ActionHint;
                if (level == 2) return DirectionHint;
                return Message;
            }
        }

        private InvestigationGuidance ResolveGuidance()
        {
            if (state == null)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ObserveFirstFinding,
                    "CURRENT TASK",
                    "The case is unavailable.");
            }

            switch (state.Phase)
            {
                case InvestigationPhase.Observe:
                    return ResolveObserveGuidance();
                case InvestigationPhase.Simulate:
                    return ResolveSimulateGuidance();
                case InvestigationPhase.Report:
                    return ResolveReportGuidance();
                default:
                    return new InvestigationGuidance(
                        InvestigationGuidanceStep.ContinueInvestigation,
                        "CURRENT TASK",
                        "Continue the investigation.");
            }
        }

        private InvestigationGuidance ResolveObserveGuidance()
        {
            int findings = CountInitialFindings();
            int remaining = Mathf.Max(0, InvestigationObserveEvaluator.RequiredCount(caseDefinition) - findings);
            if (findings == 0)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ObserveFirstFinding,
                    "OBSERVE",
                    "I'm Edna. Help me find what changed here, and why. Answer my first survey question to begin.",
                    "Slide between the two surveys to compare the highlighted organism.",
                    "Choose an answer beside the survey. I will record the finding in your notebook.");
            }
            if (remaining > 0)
            {
                InvestigationObservationDefinition latest = caseDefinition.FindObservation(lastRecordedObservationId);
                string subject = latest == null ? string.Empty
                    : caseDefinition.FindSpecies(latest.RelatedSpeciesId)?.GameplayName ?? latest.DisplayName;
                string response = latest == null ? "Look for changes and stable results."
                    : latest.ClaimType == ObservationClaimType.MatchesBaseline
                        ? $"{subject} stayed stable. Stable findings help us test causes too."
                        : latest.ClaimType == ObservationClaimType.NotDetected
                            ? $"{subject} wasn't detected. Keep the survey's limits in mind."
                            : latest.DisplayName.TrimEnd('.') + ".";
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ObserveRemainingFindings,
                    "OBSERVE",
                    $"{response} Answer {remaining} more question{(remaining == 1 ? string.Empty : "s")}.",
                    "Stable species are useful comparisons too. Match each organism with the highlighted one in the baseline.",
                    $"Answer Edna's next survey question. You still need {remaining} more notebook entr{(remaining == 1 ? "y" : "ies")}.");
            }
            return new InvestigationGuidance(
                    InvestigationGuidanceStep.ObserveComplete,
                    "OBSERVE COMPLETE",
                    "Your findings are recorded. Let's test what could explain them.");
        }

        private InvestigationGuidance ResolveSimulateGuidance()
        {
            var readiness = new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
            if (!string.IsNullOrEmpty(state.ProvisionalThreatId) && !state.ConfirmationReviewed)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ReviewRov,
                    "NEW CHECK",
                    "Your first idea is saved. Return to the report and review the ROV follow-up.");
            }
            if (state.ConfirmationReviewed && readiness.RequiredObjectivesComplete)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.CompleteFinalReport,
                    "EVIDENCE READY",
                    "The close-match checks are complete. Return to the report and finish your conclusion.");
            }
            if (string.IsNullOrEmpty(state.ProvisionalThreatId) && readiness.CanEnterProvisional)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.SubmitProvisional,
                    "FIRST IDEA",
                    "You have enough comparisons for a first idea. Choose the cause you currently support and write it down.");
            }
            if (string.IsNullOrEmpty(selectedThreatId))
            {
                InvestigationObjectiveDefinition guidedObjective = FindNextGuidedObjective();
                ThreatSimulationDefinition guidedThreat = caseDefinition.FindThreat(guidedObjective?.ThreatId);
                string guidedThreatName = guidedThreat?.DisplayName ?? "a possible cause";
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ChooseCause,
                    "STEP 1 · CHOOSE",
                    "Choose one possible cause to investigate. You can test every cause before deciding.",
                    "We can start with any cause, then compare what its model predicts with our survey.",
                    $"Select {guidedThreatName}, then look for the Run simulation action.");
            }

            ThreatSimulationDefinition threat = caseDefinition.FindThreat(selectedThreatId);
            SimulationResult simulation = restingExperiments.Contains(selectedThreatId) ? null : state.FindSimulation(selectedThreatId);
            if (simulation == null)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.RunModel,
                    "STEP 2 · RUN",
                    $"Apply {threat?.DisplayName ?? "the selected cause"} to your food web to see what it predicts.",
                    "Running a model does not choose the answer; it only reveals a prediction you can test.",
                    "Select Apply to model, or drag the cause card onto your model.");
            }
            if (string.IsNullOrEmpty(selectedPredictionSpeciesId))
            {
                InvestigationObjectiveDefinition guidedObjective = FindNextGuidedObjective();
                string guidedTarget = guidedObjective == null ? "another species" : ObjectiveTargetDisplayName(guidedObjective);
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ChoosePrediction,
                    "STEP 3 · PREDICTION",
                    "Select one prediction in the model that you want to test against the survey.",
                    $"We can use the {guidedTarget} prediction for our next check.",
                    $"Select the {guidedTarget} prediction on the left. This will reveal related notebook evidence.");
            }
            if (string.IsNullOrEmpty(selectedObservationId))
            {
                InvestigationObservationDefinition guidedEvidence = null;
                PredictionComparisonRuleDefinition rule = caseDefinition.FindComparisonRule(selectedThreatId, selectedPredictionTargetKind, selectedPredictionSpeciesId);
                if (rule != null)
                    foreach (ObservationComparisonOptionDefinition option in rule.ObservationOptions)
                    {
                        InvestigationObservationDefinition finding = caseDefinition.FindObservation(option.EvidenceId);
                        if (finding == null || !state.HasDiscoveredObservation(finding.EvidenceId) || !IsDirectObservationForSelectedTarget(finding)) continue;
                        guidedEvidence = finding;
                        break;
                    }
                string evidenceName = guidedEvidence?.DisplayName ?? "an observation about the same subject";
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ChooseObservation,
                    "STEP 3 · EVIDENCE",
                    "Select a finding in WHAT WE FOUND to check it against the chosen prediction.",
                    "Choose a finding about the same species or environmental reading. Its relationship to the model will be checked immediately.",
                    $"Select “{evidenceName}” from WHAT WE FOUND, or open the Notebook to review it first.");
            }

            PredictionComparisonRecord saved = state.FindComparison(
                selectedThreatId,
                selectedPredictionTargetKind,
                selectedPredictionSpeciesId);
            if (saved == null || !saved.IsAccepted)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ReviewEvidence,
                    "CHECK THE FINDING",
                    "Choose a recorded finding that can test this prediction.",
                    "Check that the finding concerns the same species or a related food-web change.",
                    "Select another finding in WHAT WE FOUND to check it immediately.");
            }
            if (!saved.LocksComparison)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ContinueInvestigation,
                    "OPEN QUESTION",
                    "This finding cannot settle the prediction. Choose another finding or test a different prediction.",
                    "An unrelated finding cannot settle this comparison.",
                    "Select a finding about the predicted species in WHAT WE FOUND, or choose another prediction.");
            }

            return new InvestigationGuidance(
                InvestigationGuidanceStep.ContinueInvestigation,
                "COMPARISON SAVED",
                state.Difficulty == InvestigationDifficulty.Easy
                    ? $"Good evidence check. Next: {BuildSimulationGateLabel()}."
                    : "Good evidence check. Continue investigating the remaining predictions.");
        }

        private InvestigationGuidance ResolveReportGuidance()
        {
            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.CaseClosed,
                    "CASE CLOSED",
                    "Your conclusion connects the survey pattern, ecosystem mechanism, follow-up evidence and remaining uncertainty.");
            }

            if (!state.ConfirmationReviewed)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ReviewRov,
                    "NEW FINDINGS",
                    $"You suggested {caseDefinition.FindThreat(state.ProvisionalThreatId)?.DisplayName}. The ROV findings are ready to review.",
                    "These physical observations add to the comparisons you have already completed.",
                    $"Choose {InspectSharkHabitatLabel} or {InspectSeafloorLabel} in our conversation.");
            }
            return new InvestigationGuidance(
                InvestigationGuidanceStep.CompleteFinalReport,
                "YOUR REPORT",
                "Your checked findings are gathered here. Review your explanation and send the report.",
                "The report keeps your first idea. You can revise it using the ROV findings.",
                "Follow our conversation to review the explanation, revisit the clues, or send your report.");
        }

        private void ShowReferenceSurveyNotice()
        {
            statusMessage = "This is the 20-year reference survey. Compare it with today, then answer Edna's question to record a finding.";
            statusTone = InvestigationStatusTone.Notice;
            RenderChrome();
        }

        private void AddUnrecordedFindingCue(Button marker)
        {
            RectTransform cue = new GameObject("Unrecorded Finding Cue", typeof(RectTransform)).GetComponent<RectTransform>();
            cue.SetParent(marker.transform, false);
            Anchor(cue, 0f, 0f, 1f, 1f, -4f, -4f, 4f, 4f);
            RectTransform top = CreatePanel("Guide Border Top", cue, InvestigationTheme.Primary, 1f);
            Anchor(top, 0f, 1f, 1f, 1f, 0f, -3f, 0f, 0f);
            RectTransform bottom = CreatePanel("Guide Border Bottom", cue, InvestigationTheme.Primary, 1f);
            Anchor(bottom, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 3f);
            RectTransform left = CreatePanel("Guide Border Left", cue, InvestigationTheme.Primary, 1f);
            Anchor(left, 0f, 0f, 0f, 1f, 0f, 3f, 3f, -3f);
            RectTransform right = CreatePanel("Guide Border Right", cue, InvestigationTheme.Primary, 1f);
            Anchor(right, 1f, 0f, 1f, 1f, -3f, 3f, 0f, -3f);
            cue.gameObject.AddComponent<InvestigationGuidancePulse>();
        }

        private InvestigationGuideArrowGraphic AddActionArrow(string name, Transform target)
        {
            InvestigationGuideArrowGraphic arrow = CreateGraphic<InvestigationGuideArrowGraphic>(name, target);
            arrow.color = InvestigationTheme.Accent;
            Anchor(arrow.rectTransform, .5f, 1f, .5f, 1f, -18f, 2f, 18f, 40f);
            AddSingleShadow(arrow.gameObject, InvestigationTheme.PrimaryShadow, new Vector2(2f, -2f));
            arrow.gameObject.AddComponent<InvestigationGuidancePulse>();
            return arrow;
        }

        private void AddChoiceBorderCue(string name, Transform target)
        {
            InvestigationBorderGraphic border = CreateGraphic<InvestigationBorderGraphic>(name, target);
            Stretch(border.rectTransform, 1f, 1f, -1f, -1f);
            border.Configure(InvestigationTheme.SmallRadius, 1.5f);
            Color color = InvestigationTheme.Primary;
            color.a = 0.75f;
            border.color = color;
            border.gameObject.AddComponent<InvestigationGuidancePulse>();
        }
    }
}
