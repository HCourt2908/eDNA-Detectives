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
            RetestFishingModels,
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

        private bool guidanceCollapsed;
        private int ednaHintLevel = 1;
        private InvestigationGuidanceStep? activeGuidanceStep;
        private string activeGuidanceTask = string.Empty;
        private int lastObservedMisstepCount;

        private void RenderEdnaGuide(Transform headingRow, string descriptionObjectName = "Description")
        {
            InvestigationGuidance guidance = ResolveGuidance();
            string task = state.Phase == InvestigationPhase.Observe
                ? $"Observe|{CountInitialFindings()}"
                : state.Phase == InvestigationPhase.Report
                    ? $"Report|{NextReportSection()}|{state.ConfirmationReviewed}"
                    : $"Simulate|{selectedThreatId}|{selectedPredictionTargetKind}|{selectedPredictionSpeciesId}|{FindNextGuidedObjective()?.ObjectiveId}";
            task += $"|{guidance.Step}";
            if (!activeGuidanceStep.HasValue || activeGuidanceTask != task)
            {
                activeGuidanceStep = guidance.Step;
                activeGuidanceTask = task;
                ednaHintLevel = 1;
            }
            RenderInlineEdnaGuide(headingRow, guidance, descriptionObjectName);
            if (descriptionObjectName == "Simulate Description") LayoutSimulateGuide(headingRow, guidance.Step);
        }

        private void LayoutSimulateGuide(Transform row, InvestigationGuidanceStep step)
        {
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            Anchor(row.Find("Simulate Title").GetComponent<RectTransform>(), 0f, 1f, 0f, 1f, 0f, -28f, 200f, 0f);
            Anchor(row.Find("Simulate Heading Divider").GetComponent<RectTransform>(), 0f, 1f, 0f, 1f, 196f, -26f, 198f, -2f);
            Anchor(row.Find("Edna Icon").GetComponent<RectTransform>(), 0f, 1f, 0f, 1f, 204f, -25f, 228f, -1f);
            Anchor(row.Find($"Edna Identity {step}").GetComponent<RectTransform>(), 0f, 1f, 0f, 1f, 236f, -28f, 352f, 0f);
            Transform help = row.Find("Increase Edna Hint");
            if (help != null) Anchor(help.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -216f, -44f, -112f, 0f);
            Anchor(row.Find("Toggle Edna Guide").GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -104f, -44f, 0f, 0f);
            RectTransform message = row.Find(guidanceCollapsed ? "Edna Prompt Hidden" : "Edna Prompt").GetComponent<RectTransform>();
            Anchor(message, 0f, 0f, 1f, 1f, 0f, 4f, -4f, -48f);
            message.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
        }

        private void RenderInlineEdnaGuide(Transform headingRow, InvestigationGuidance guidance, string descriptionObjectName)
        {
            if (headingRow == null) return;
            Text description = headingRow.Find(descriptionObjectName)?.GetComponent<Text>();
            if (description == null) return;

            description.gameObject.name = guidanceCollapsed ? "Edna Prompt Hidden" : "Edna Prompt";
            int visibleHintLevel = state != null && state.Difficulty == InvestigationDifficulty.Easy
                ? Mathf.Clamp(ednaHintLevel, 1, 3)
                : 1;
            description.text = guidanceCollapsed ? "Current task hidden" : guidance.MessageForLevel(visibleHintLevel);
            description.fontSize = guidanceCollapsed ? 12 : 13;
            description.fontStyle = guidanceCollapsed ? FontStyle.Normal : FontStyle.Bold;
            description.color = guidanceCollapsed ? InvestigationTheme.TextSecondary : InvestigationTheme.TextPrimary;

            Image icon = CreateStatusIcon(
                "Edna Icon",
                headingRow,
                InvestigationScenarioIconLibrary.Investigate,
                InvestigationTheme.Primary);
            LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = 24f;
            iconLayout.preferredWidth = 24f;
            iconLayout.minHeight = 24f;
            iconLayout.preferredHeight = 24f;
            icon.transform.SetSiblingIndex(Mathf.Max(0, description.transform.GetSiblingIndex()));

            Text identity = CreateText(
                $"Edna Identity {guidance.Step}",
                headingRow,
                guidanceCollapsed
                    ? "EDNA"
                    : state != null && state.Difficulty == InvestigationDifficulty.Easy
                        ? $"EDNA · HINT {visibleHintLevel}/3"
                        : $"EDNA · {guidance.Label}",
                10,
                FontStyle.Bold,
                InvestigationTheme.Primary,
                TextAnchor.MiddleLeft,
                InvestigationTheme.DataFont);
            LayoutElement identityLayout = identity.gameObject.AddComponent<LayoutElement>();
            identityLayout.minWidth = guidanceCollapsed ? 46f : 104f;
            identityLayout.preferredWidth = identityLayout.minWidth;
            identityLayout.preferredHeight = 40f;
            identity.transform.SetSiblingIndex(Mathf.Max(0, description.transform.GetSiblingIndex()));

            if (!guidanceCollapsed && state != null && state.Difficulty == InvestigationDifficulty.Easy)
            {
                Button hint = CreateButton(
                    "Increase Edna Hint",
                    headingRow,
                    visibleHintLevel < 3 ? "More help" : "Less help",
                    ButtonVisualStyle.Secondary,
                    () =>
                    {
                        ednaHintLevel = visibleHintLevel < 3 ? visibleHintLevel + 1 : 1;
                        RefreshPresentationOnly();
                    },
                    out Text hintLabel);
                StyleGuideControl(hint, hintLabel, true);
                LayoutElement hintLayout = hint.GetComponent<LayoutElement>();
                hintLayout.minWidth = 104f;
                hintLayout.preferredWidth = 104f;
                hintLayout.minHeight = 40f;
                hintLayout.preferredHeight = 40f;
                hintLayout.flexibleWidth = 0f;
            }

            Button toggle = CreateButton(
                "Toggle Edna Guide",
                headingRow,
                guidanceCollapsed ? "Show task" : "Minimise",
                ButtonVisualStyle.Secondary,
                () =>
                {
                    guidanceCollapsed = !guidanceCollapsed;
                    RefreshPresentationOnly();
                },
                out Text toggleLabel);
            StyleGuideControl(toggle, toggleLabel, false);
            LayoutElement toggleLayout = toggle.GetComponent<LayoutElement>();
            toggleLayout.minWidth = guidanceCollapsed ? 112f : 104f;
            toggleLayout.preferredWidth = toggleLayout.minWidth;
            toggleLayout.minHeight = 40f;
            toggleLayout.preferredHeight = 40f;
            toggleLayout.flexibleWidth = 0f;
        }

        private void StyleGuideControl(Button button, Text label, bool help, bool paper = false)
        {
            button.targetGraphic.color = paper ? InvestigationTheme.PaperSelected : InvestigationTheme.SurfaceRaised;
            EnsureOutline(button.targetGraphic.gameObject,
                paper ? InvestigationTheme.PaperSelectedBorder : InvestigationTheme.Primary, new Vector2(1f, -1f));
            label.fontSize = 13;
            label.fontStyle = FontStyle.Bold;
            label.color = paper ? InvestigationTheme.PaperInk : InvestigationTheme.TextPrimary;
            label.rectTransform.offsetMin = new Vector2(28f, 4f);
            label.rectTransform.offsetMax = new Vector2(-6f, -4f);
            if (help)
            {
                Image icon = CreateStatusIcon("Help Icon", button.transform, InvestigationStatusIconLibrary.Question, label.color);
                Anchor(icon.rectTransform, 0f, 0.5f, 0f, 0.5f, 6f, -9f, 24f, 9f);
            }
            else
            {
                RectTransform horizontal = CreatePanel("Guide Toggle Icon", button.transform, label.color, 0f);
                Anchor(horizontal, 0f, 0.5f, 0f, 0.5f, 8f, -1f, 22f, 1f);
                if (guidanceCollapsed)
                {
                    RectTransform vertical = CreatePanel("Guide Expand Icon", button.transform, label.color, 0f);
                    Anchor(vertical, 0f, 0.5f, 0f, 0.5f, 14f, -7f, 16f, 7f);
                }
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
                    "I'm Edna. Help me find what changed here, and why. Record one finding on TODAY to begin.",
                    "Only organisms on TODAY record findings. Choose any framed species to start.",
                    "Select any framed species on TODAY; its survey finding will be written into your notebook.");
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
                    $"{response} Record {remaining} more finding{(remaining == 1 ? string.Empty : "s")} on TODAY.",
                    "Stable species are useful comparisons too. Match each organism with the highlighted one in the baseline.",
                    $"Select an organism without a recorded check on TODAY. You still need {remaining} more notebook entr{(remaining == 1 ? "y" : "ies")}.");
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
                string guidedThreatName = guidedThreat?.DisplayName ?? "the cause named by the current Case Question";
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ChooseCause,
                    "STEP 1 · CHOOSE",
                    "Choose one possible cause to investigate. You can test every cause before deciding.",
                    "Use the current Case Question to decide which cause to test next.",
                    $"Select {guidedThreatName}, then look for the Run simulation action.");
            }

            ThreatSimulationDefinition threat = caseDefinition.FindThreat(selectedThreatId);
            SimulationResult simulation = state.FindSimulation(selectedThreatId);
            if (simulation == null)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.RunModel,
                    "STEP 2 · RUN",
                    $"Run the {threat?.DisplayName ?? "selected"} model to reveal what it predicts.",
                    "Running a model does not choose the answer; it only reveals a prediction you can test.",
                    "Select Run simulation in the model panel on the left.");
            }
            if (string.IsNullOrEmpty(selectedPredictionSpeciesId))
            {
                InvestigationObjectiveDefinition guidedObjective = FindNextGuidedObjective();
                string guidedTarget = guidedObjective == null ? "the current Case Question" : ObjectiveTargetDisplayName(guidedObjective);
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ChoosePrediction,
                    "STEP 3 · PREDICTION",
                    "Select one prediction in the model that you want to test against the survey.",
                    $"The current Case Question points you towards {guidedTarget}.",
                    $"Select the {guidedTarget} prediction on the left. This will reveal related notebook evidence.");
            }
            if (string.IsNullOrEmpty(selectedObservationId))
            {
                InvestigationObjectiveDefinition guidedObjective = FindNextGuidedObjective();
                InvestigationObservationDefinition guidedEvidence = guidedObjective == null
                    ? null
                    : caseDefinition.FindObservation(guidedObjective.RequiredEvidenceId);
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
                    "An unrelated finding does not complete a Case Question.",
                    "Select a finding about the predicted species in WHAT WE FOUND, or choose another prediction.");
            }

            return new InvestigationGuidance(
                InvestigationGuidanceStep.ContinueInvestigation,
                "COMPARISON SAVED",
                state.Difficulty == InvestigationDifficulty.Easy
                    ? $"Good evidence check. Next: {BuildSimulationGateLabel()}."
                    : "Good evidence check. Continue investigating the remaining Case Questions.");
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

            var readiness = new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
            if (!state.ConfirmationReviewed)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.ReviewRov,
                    "CHECK YOUR IDEA",
                    $"You suggested {caseDefinition.FindThreat(state.ProvisionalThreatId)?.DisplayName}. Review the ROV follow-up to see whether new evidence changes it.",
                    "The follow-up is fixed, so it will not change to agree with your first idea.",
                    $"Select {OpenRovFindingsLabel} in the ROV panel to reveal the two clues.");
            }
            if (!readiness.RequiredObjectivesComplete)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.RetestFishingModels,
                    "NEW EVIDENCE",
                    "The seafloor is intact. Re-test Sea star under both fishing models before writing the final conclusion.",
                    "The two fishing models share the food-web pattern, so the benthic prediction must separate them.",
                    $"Select {CompareFishingModelsLabel}, then check Sea star under both fishing models.");
            }
            if (!readiness.FinalCauseSelected)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.CompleteFinalReport,
                    "FINAL REPORT",
                    "Choose the final cause again using everything you now know.",
                    "Use both ROV clues: fishing line was recorded, while the seafloor remained intact.",
                    "Choose the cause that explains selective shark removal without predicting damaged seafloor.");
            }
            if (!readiness.ReasoningComplete)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.CompleteFinalReport,
                    "FINAL REPORT",
                    "Choose the mechanism that explains how the species changes are connected.",
                    "The mechanism should connect the three food-web observations rather than treat them as separate accidents.",
                    "Look for the sequence: fewer sharks → more tuna → fewer krill.");
            }
            if (!readiness.EvidenceComplete || !readiness.EvidenceCategoriesComplete || !readiness.ConfirmationEvidenceIncluded)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.CompleteFinalReport,
                    "FINAL REPORT",
                    "Build a balanced evidence set: food web, benthic indicator and ROV follow-up.",
                    "The counters show which evidence categories are still missing.",
                    "Choose at least two FOOD WEB findings, one BENTHIC finding and one ROV finding.");
            }
            if (!readiness.LimitationComplete)
            {
                return new InvestigationGuidance(
                    InvestigationGuidanceStep.CompleteFinalReport,
                    "FINAL REPORT",
                    "Record what the investigation still cannot prove with certainty.",
                    "A strong scientific report separates evidence from what the evidence cannot guarantee.",
                    "Choose the limitation that explains why an eDNA non-detection does not prove a species is completely gone.");
            }
            return new InvestigationGuidance(
                InvestigationGuidanceStep.CompleteFinalReport,
                "REPORT READY",
                "Check your completed explanation, then send the report when you are satisfied.");
        }

        private void ShowReferenceSurveyNotice()
        {
            statusMessage = "This is the 20-year reference survey. Record findings by selecting organisms on TODAY.";
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
    }
}
