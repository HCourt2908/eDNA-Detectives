using System;
using System.Collections;
using System.Collections.Generic;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum EdnaReply { Introduction, NextStep, Explanation }
        private readonly HashSet<string> ednaIntroductions = new HashSet<string>(StringComparer.Ordinal);
        private bool ednaConversationOpen;
        private bool ednaEntrancePending;
        private bool ednaManualHelp;
        private bool ednaOptionsInLine;
        private float ednaDialogueHeight = 108f;
        private float ednaHeaderHeight = 32f;
        private readonly TextGenerator ednaTextMeasure = new TextGenerator();
        private EdnaReply ednaReply;
        private string ednaTask = string.Empty;
        private RectTransform ednaDock;
        private RectTransform ednaConversation;
        private Sprite ednaPortrait;
        private Sprite ednaAvatar;
        private bool EdnaCuesVisible => ednaConversationOpen && !notebookDrawerOpen && !provisionalReviewOpen;
        private bool EdnaInlineIntroduction => state != null && state.Phase == InvestigationPhase.Observe;
        private bool EdnaCompact => GetComponent<RectTransform>().rect.width < 960f;
        private float EdnaPortraitWidth => EdnaCompact ? 106f : 142f;
        // Embedded guides already provide their own actions; only floating speech needs a reopen control.
        private bool EdnaCanCollapse => state != null && (state.Phase == InvestigationPhase.Simulate
            || (state.Phase == InvestigationPhase.Report && state.ConclusionStatus == InvestigationConclusionStatus.Correct));
        private bool EdnaControlsAvailable => EdnaCanCollapse && !notebookDrawerOpen
            && !provisionalReviewOpen && !restartConfirmationPending;
        private bool EdnaFloatingConversation => EdnaControlsAvailable && ednaConversationOpen && !ScenarioWorkspaceActive;
        private bool EdnaTopConversation => EdnaFloatingConversation && state.Phase != InvestigationPhase.Simulate;
        private bool EdnaBottomConversation => EdnaFloatingConversation && state.Phase == InvestigationPhase.Simulate;
        private float EdnaExtraTopSpace => EdnaTopConversation ? Mathf.Max(0f, ednaDialogueHeight - 42f) : 0f;
        private float EdnaExtraBottomSpace => ScenarioWorkspaceActive ? ScenarioDockHeight + 12f : EdnaBottomConversation ? ednaDialogueHeight + 12f : 0f;
        private bool EdnaHasOpenComparison
        {
            get
            {
                if (state == null || state.Phase != InvestigationPhase.Simulate || string.IsNullOrEmpty(selectedObservationId)) return false;
                PredictionComparisonRecord record = state.FindComparison(selectedThreatId, selectedPredictionTargetKind, selectedPredictionSpeciesId);
                return record == null || !record.LocksComparison;
            }
        }
        private bool EdnaCanAskNext => !ScenarioModeActive && string.IsNullOrEmpty(EdnaActionLabel)
            && (ednaManualHelp || EdnaHasOpenComparison || ResolveGuidance().Step == InvestigationGuidanceStep.ChooseObservation) && ednaReply != EdnaReply.NextStep
            && state != null && state.ConclusionStatus != InvestigationConclusionStatus.Correct
            && !string.Equals(state.Difficulty == InvestigationDifficulty.Easy ? ResolveGuidance().ActionHint : ResolveGuidance().Message, EdnaMessage(), StringComparison.Ordinal);
        private bool EdnaCanAskWhy => !ScenarioModeActive && (ednaManualHelp || EdnaHasOpenComparison
            || (state != null && state.Phase == InvestigationPhase.Simulate && !string.IsNullOrEmpty(selectedObservationId))) && ednaReply != EdnaReply.Explanation;
        private string EdnaActionLabel
        {
            get
            {
                if (ScenarioModeActive) return ScenarioEdnaAction;
                if (state == null || state.Phase != InvestigationPhase.Simulate || !workbenchFoodWebReady) return string.Empty;
                InvestigationGuidanceStep step = ResolveGuidance().Step;
                if (!workbenchInspectPrediction && state.FindSimulation(selectedThreatId) != null && !restingExperiments.Contains(selectedThreatId)
                    && (step == InvestigationGuidanceStep.ChoosePrediction || step == InvestigationGuidanceStep.ContinueInvestigation))
                    return !string.IsNullOrEmpty(workbenchEvidence) ? "Test this pattern" : ednaManualHelp && state.Difficulty == InvestigationDifficulty.Easy ? "Guide one check" : string.Empty;
                switch (ResolveGuidance().Step)
                {
                    case InvestigationGuidanceStep.RunModel: return "Apply to model";
                    case InvestigationGuidanceStep.ContinueInvestigation:
                        return !EdnaHasOpenComparison && FindNextGuidedObjective() != null ? "Next check" : string.Empty;
                    case InvestigationGuidanceStep.ChoosePrediction:
                        return state.Difficulty == InvestigationDifficulty.Easy && FindNextGuidedObjective() != null ? "Guide my next check" : string.Empty;
                    case InvestigationGuidanceStep.SubmitProvisional: return "Write first idea";
                    case InvestigationGuidanceStep.ReviewRov:
                    case InvestigationGuidanceStep.CompleteFinalReport: return "Return to report";
                    default: return string.Empty;
                }
            }
        }
        private bool EdnaCanOpenNotebook => !ScenarioModeActive && state != null && state.Phase == InvestigationPhase.Simulate
            && !notebookHasBeenOpened && ResolveGuidance().Step == InvestigationGuidanceStep.ContinueInvestigation
            && state.FindComparison(selectedThreatId, selectedPredictionTargetKind, selectedPredictionSpeciesId)?.LocksComparison == true;
        private float EdnaOptionsWidth
        {
            get
            {
                int count = (EdnaCanAskNext ? 1 : 0) + (EdnaCanAskWhy ? 1 : 0) + (EdnaCanOpenNotebook ? 1 : 0)
                    + (string.IsNullOrEmpty(EdnaActionLabel) ? 0 : 1);
                return (EdnaCanAskNext ? 116f : 0f) + (EdnaCanAskWhy ? 72f : 0f)
                    + (EdnaCanOpenNotebook ? 136f : 0f) + (string.IsNullOrEmpty(EdnaActionLabel) ? 0f : 148f) + Mathf.Max(0, count - 1) * 8f;
            }
        }

        private float MeasureEdnaText(string text, float width)
        {
            return ednaTextMeasure.GetPreferredHeight(text, new TextGenerationSettings
            {
                font = InvestigationTheme.BodyFont, fontSize = 15, fontStyle = FontStyle.Bold,
                color = Color.white, lineSpacing = 1.05f, scaleFactor = 1f,
                textAnchor = TextAnchor.UpperLeft, horizontalOverflow = HorizontalWrapMode.Wrap,
                verticalOverflow = VerticalWrapMode.Overflow, generationExtents = new Vector2(Mathf.Max(80f, width), 0f),
                richText = false, pivot = Vector2.zero
            });
        }

        private void UpdateEdnaLayout()
        {
            float width = contentPanel == null ? GetComponent<RectTransform>().rect.width
                : ((RectTransform)contentPanel.parent).rect.width;
            float contentWidth = width - OuterMargin * 2f - EdnaPortraitWidth - 82f;
            ednaHeaderHeight = state != null && state.Phase == InvestigationPhase.Simulate
                ? Mathf.Max(32f, MeasureEdnaText(EdnaHeading(), contentWidth) + 12f) : 32f;
            float besideWidth = contentWidth - EdnaOptionsWidth - 12f;
            ednaOptionsInLine = EdnaOptionsWidth > 0f && besideWidth >= 260f
                && MeasureEdnaText(EdnaMessage(), besideWidth) <= 46f;
            float messageHeight = MeasureEdnaText(EdnaMessage(), ednaOptionsInLine ? besideWidth : contentWidth);
            ednaDialogueHeight = Mathf.Max(108f, 48f + messageHeight
                + (EdnaOptionsWidth > 0f && !ednaOptionsInLine ? 52f : 0f));
            ednaDialogueHeight += ednaHeaderHeight - 32f;
        }

        private void ResetEdnaConversation()
        {
            ednaIntroductions.Clear();
            ednaTask = string.Empty;
            ednaConversationOpen = false;
            ednaEntrancePending = false;
            ednaReply = EdnaReply.Introduction;
            ednaManualHelp = false;
        }

        private void PrepareEdnaConversation()
        {
            if (state == null) return;
            if (state.Phase == InvestigationPhase.Simulate && string.IsNullOrEmpty(selectedThreatId))
                selectedThreatId = state.ActiveThreatId;
            InvestigationGuidance guidance = ResolveGuidance();
            string task = $"{workbenchFoodWebReady}|{workbenchLinks.Count}|{ObserveQuestion?.EvidenceId}|{state.Phase}|{state.Difficulty}|{guidance.Step}|{CountInitialFindings()}|{selectedThreatId}|{selectedPredictionSpeciesId}|{selectedObservationId}|{state.AcceptedComparisonCount}|{state.ConfirmationReviewed}|{state.ConclusionStatus}|{state.FinalThreatId}";
            if (task != ednaTask)
            {
                bool wasOpen = ednaConversationOpen;
                ednaTask = task;
                ednaReply = EdnaReply.Introduction;
                ednaManualHelp = false;
                ednaConversationOpen = false;
                string introduction = EdnaIntroductionKey(guidance.Step);
                bool firstIntroduction = !string.IsNullOrEmpty(introduction) && ednaIntroductions.Add(introduction);
                if (firstIntroduction || (state.Phase == InvestigationPhase.Simulate
                    && state.ConclusionStatus != InvestigationConclusionStatus.Correct))
                {
                    ednaConversationOpen = true;
                    ednaEntrancePending = !wasOpen;
                }
            }
            UpdateEdnaLayout();
        }

        private string EdnaIntroductionKey(InvestigationGuidanceStep step)
        {
            if (state.Phase == InvestigationPhase.Observe)
            {
                if (CountVisibleNotebookObservations() == 0) return "observe-start";
                if (CountInitialFindings() == 1) return "first-finding";
                if (InvestigationObserveEvaluator.IsComplete(caseDefinition, state)) return "observe-complete";
                return string.Empty;
            }
            if (state.Phase == InvestigationPhase.Report)
                return state.ConclusionStatus == InvestigationConclusionStatus.Correct ? "case-closed"
                    : state.ConfirmationReviewed ? "rov-arrival" : "rov-ready";
            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct) return string.Empty;
            switch (step)
            {
                case InvestigationGuidanceStep.ChooseCause: return "choose-cause";
                case InvestigationGuidanceStep.RunModel: return "run-model";
                case InvestigationGuidanceStep.ChoosePrediction: return "choose-prediction";
                case InvestigationGuidanceStep.ChooseObservation: return "choose-finding";
                case InvestigationGuidanceStep.SubmitProvisional: return "first-explanation";
                case InvestigationGuidanceStep.ContinueInvestigation:
                    if (!string.IsNullOrEmpty(selectedObservationId))
                    {
                        PredictionComparisonRecord record = state.FindComparison(selectedThreatId, selectedPredictionTargetKind, selectedPredictionSpeciesId);
                        if (record == null || !record.LocksComparison) return "open-comparison";
                        if (!notebookHasBeenOpened) return "notebook-introduction";
                    }
                    return state.AcceptedComparisonCount == 1 ? "first-comparison" : string.Empty;
                default: return string.Empty;
            }
        }

        private string EdnaMessage()
        {
            if (ScenarioModeActive) return ScenarioEdnaMessage;
            InvestigationGuidance guidance = ResolveGuidance();
            if (state.Phase == InvestigationPhase.Simulate && !workbenchFoodWebReady)
                return "Your recorded changes are on these cards. Connect who eats whom using the diet hints, then we can test a cause.";
            if (ednaReply == EdnaReply.Explanation) return EdnaExplanation();
            if (ednaReply == EdnaReply.NextStep)
                return state.Difficulty == InvestigationDifficulty.Easy ? guidance.ActionHint : guidance.Message;
            if (EdnaInlineIntroduction) return "Match the saved records on the two notebook pages, then choose how each species changed.";
            if (state.Phase == InvestigationPhase.Observe && CountInitialFindings() == 1)
                return "I've added your first finding to the notebook. Keep looking — the other species can help us understand what changed.";
            if (state.Phase == InvestigationPhase.Report && state.ConfirmationReviewed
                && state.ConclusionStatus != InvestigationConclusionStatus.Correct)
                return "The ROV found fishing line and an intact seafloor. I've brought your findings together below. Keep your explanation, or change it before sending.";
            if (state.Phase == InvestigationPhase.Simulate && guidance.Step == InvestigationGuidanceStep.ChooseCause)
                return "Your food chain is ready. Try any of these three causes on this same table: drag one onto the model, or select it and choose Apply to model.";
            if (state.Phase == InvestigationPhase.Simulate && guidance.Step == InvestigationGuidanceStep.ChoosePrediction)
                return workbenchInspectPrediction ? "Tap any prediction with a glowing border to test it against the survey."
                    : "The blue panels show model predictions; the paper cards keep your survey records. Drag a saved pattern onto the model to check this explanation.";
            if (state.Phase == InvestigationPhase.Simulate && guidance.Step == InvestigationGuidanceStep.ChooseObservation)
                return $"Let's check {caseDefinition.FindSpecies(selectedPredictionSpeciesId)?.GameplayName ?? "this prediction"}. Choose a finding in WHAT WE FOUND; I'll compare it with the model.";
            if (EdnaCanOpenNotebook)
                return EdnaComparisonResult() + " Open your notebook to review our findings, or continue with the next check.";
            if (state.Phase == InvestigationPhase.Simulate && guidance.Step == InvestigationGuidanceStep.ContinueInvestigation && !EdnaHasOpenComparison)
                return EdnaComparisonResult() + (state.Difficulty == InvestigationDifficulty.Easy
                    ? " Next: " + BuildSimulationGateLabel().TrimEnd('.') + "."
                    : " Pick another prediction, or continue to the next check.");
            return guidance.Message;
        }

        private string EdnaComparisonResult()
        {
            PredictionComparisonRecord record = state.FindComparison(selectedThreatId, selectedPredictionTargetKind, selectedPredictionSpeciesId);
            string species = caseDefinition.FindSpecies(selectedPredictionSpeciesId)?.GameplayName ?? "This";
            string relationship = record?.Judgement == ComparisonJudgement.Match ? "supports"
                : record?.Judgement == ComparisonJudgement.Mismatch ? "challenges" : "cannot settle";
            return $"The {species} finding {relationship} this model. Comparison saved.";
        }

        private string EdnaHeading()
        {
            if (ScenarioModeActive) return ScenarioEdnaHeading;
            if (state == null || state.Phase != InvestigationPhase.Simulate) return "EDNA";
            if (!workbenchFoodWebReady) return "EDNA · Build the food web";
            if (string.IsNullOrEmpty(selectedThreatId)) return "EDNA · Choose a cause to test";
            InvestigationObjectiveDefinition objective = FindNextGuidedObjective();
            return objective == null ? "EDNA · Our comparisons are ready"
                : $"EDNA · {objective.QuestionPrompt}";
        }

        private void PerformEdnaAction()
        {
            if (ScenarioModeActive) { FinishScenarioAnimation(); return; }
            InvestigationGuidanceStep actionStep = ResolveGuidance().Step;
            if ((actionStep == InvestigationGuidanceStep.ChoosePrediction || actionStep == InvestigationGuidanceStep.ContinueInvestigation)
                && !workbenchInspectPrediction && !string.IsNullOrEmpty(workbenchEvidence) && state.FindSimulation(selectedThreatId) != null
                && !restingExperiments.Contains(selectedThreatId))
            {
                CompareWorkbenchPattern(workbenchEvidence);
                return;
            }
            switch (ResolveGuidance().Step)
            {
                case InvestigationGuidanceStep.RunModel:
                    ApplyWorkbenchExperiment(selectedThreatId);
                    return;
                case InvestigationGuidanceStep.SubmitProvisional:
                    OpenProvisionalReview();
                    return;
                case InvestigationGuidanceStep.ReviewRov:
                case InvestigationGuidanceStep.CompleteFinalReport:
                    setPhase?.Invoke(InvestigationPhase.Report);
                    return;
                default:
                    InvestigationObjectiveDefinition next = FindNextGuidedObjective();
                    if (next == null) return;
                    selectedThreatId = next.ThreatId;
                    workbenchInspectPrediction = true;
                    restingExperiments.Remove(next.ThreatId);
                    selectedPredictionTargetKind = next.TargetKind;
                    selectedPredictionSpeciesId = state.FindSimulation(next.ThreatId) != null
                        && state.Difficulty == InvestigationDifficulty.Easy ? next.TargetId : string.Empty;
                    selectedObservationId = string.Empty;
                    navigationRevealTarget = string.IsNullOrEmpty(selectedPredictionSpeciesId)
                        ? "Model Workspace" : "Observation Selection";
                    navigationRevealAtTop = false;
                    RefreshPresentationOnly();
                    return;
            }
        }

        private string EdnaExplanation()
        {
            if (state.Phase == InvestigationPhase.Observe)
                return "Changes matter, but stable species help us too. eDNA shows traces of organisms; a non-detection doesn't prove a species is gone.";
            if (state.Phase == InvestigationPhase.Report)
                return "The camera adds physical evidence to our eDNA survey. Fishing line and seafloor condition help us check the explanation from another angle.";
            if (!string.IsNullOrEmpty(selectedPredictionSpeciesId))
            {
                PredictionComparisonRecord record = state.FindComparison(selectedThreatId, selectedPredictionTargetKind, selectedPredictionSpeciesId);
                if (record != null && !string.IsNullOrEmpty(selectedObservationId)) return record.Feedback;
                return "A useful comparison tests the same species. Look at what the model predicts, then choose a finding about that organism.";
            }
            return "Different causes can predict similar food-web changes. Comparing their other predictions helps us tell the explanations apart.";
        }

        private void TalkToEdna()
        {
            if (ScenarioModeActive) { OpenScenarioBriefing(); return; }
            if (!EdnaControlsAvailable || ednaConversationOpen) return;
            ednaConversationOpen = true;
            ednaManualHelp = true;
            ednaReply = EdnaReply.Introduction;
            ednaEntrancePending = true;
            RefreshEdnaPresentation();
        }

        private void AskEdna(EdnaReply reply)
        {
            ednaManualHelp = true;
            ednaReply = reply;
            ednaConversationOpen = true;
            RefreshEdnaPresentation();
        }

        private void DismissEdna()
        {
            if (!EdnaFloatingConversation) return;
            ednaConversationOpen = false;
            ednaManualHelp = false;
            RefreshEdnaPresentation();
        }

        private void RefreshEdnaPresentation()
        {
            float offset = contentRoot == null ? 0f : contentRoot.anchoredPosition.y;
            RefreshPresentationOnly();
            if (state == null || state.Phase != InvestigationPhase.Simulate || contentScroll == null) return;
            // A shorter viewport reveals less of the same page; it must not move
            // the current comparison by preserving a percentage of a new height.
            float maximum = Mathf.Max(0f, contentRoot.rect.height - contentScroll.viewport.rect.height);
            contentScroll.StopMovement();
            Vector2 position = contentRoot.anchoredPosition;
            position.y = Mathf.Clamp(offset, 0f, maximum);
            contentRoot.anchoredPosition = position;
        }

        private void RemoveEdnaPresentation()
        {
            RemoveEdnaObject(ednaDock);
            RemoveEdnaObject(ednaConversation);
            ednaDock = null;
            ednaConversation = null;
        }

        private static void RemoveEdnaObject(RectTransform item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(item.gameObject);
            else DestroyImmediate(item.gameObject);
        }

        private void EnsureEdnaArtwork()
        {
            if (ednaPortrait != null) return;
            ednaPortrait = Resources.Load<Sprite>("Investigation/Edna/edna");
            if (ednaPortrait == null) return;
            ednaAvatar = Resources.Load<Sprite>("Investigation/Edna/edna-avatar");
        }

        private void RenderEdnaPresentation()
        {
            if (ScenarioWorkspaceActive) { RenderScenarioEdnaDock(); return; }
            if (contentPanel == null || !EdnaControlsAvailable) return;
            EnsureEdnaArtwork();
            if (!ednaConversationOpen || ScenarioModeActive)
            {
                Button dock = CreateButton("Talk To Edna", stageRoot.parent, "EDNA", ButtonVisualStyle.Tertiary, TalkToEdna, out Text label);
                ednaDock = dock.GetComponent<RectTransform>();
                Anchor(ednaDock, 1f, 0f, 1f, 0f, -136f, 2f, -12f, 46f);
                dock.GetComponent<LayoutElement>().ignoreLayout = true;
                dock.targetGraphic.color = InvestigationTheme.SurfaceRaised;
                label.fontSize = 13;
                label.rectTransform.offsetMin = new Vector2(56f, 4f);
                label.rectTransform.offsetMax = new Vector2(-6f, -4f);
                Image avatar = CreateStatusIcon("Edna Avatar", dock.transform, ednaAvatar ?? ednaPortrait, Color.white);
                Anchor(avatar.rectTransform, 0f, 0f, 0f, 1f, 5f, 2f, 53f, -2f);
            }
            if (EdnaFloatingConversation)
            {
                ednaConversation = CreatePanel("Edna Conversation", contentPanel.parent, Color.clear, 0f);
                if (EdnaBottomConversation)
                    Anchor(ednaConversation, 0f, 0f, 1f, 0f, OuterMargin + 2f, OuterMargin,
                        -OuterMargin - 4f, OuterMargin + ednaDialogueHeight);
                else
                    Anchor(ednaConversation, 0f, 1f, 1f, 1f, OuterMargin + 2f, -ContentTopInset - ednaDialogueHeight,
                        -OuterMargin - 4f, -ContentTopInset);
                RectTransform bubble = CreateEdnaSpeech(ednaConversation);
                Stretch(bubble, 0f, 0f, 0f, 0f);
                if (EdnaTopConversation) HideCoveredHeading();
                if (ednaEntrancePending && !InvestigationMotionSettings.ReducedMotion)
                    StartCoroutine(AnimateEdnaEntrance(ednaConversation));
            }
            ednaEntrancePending = false;
        }

        private void HideCoveredHeading()
        {
            RectTransform heading = state.Phase == InvestigationPhase.Simulate
                ? FindNamedRect(contentRoot, "Simulate Introduction") : contentRoot.childCount > 0 ? contentRoot.GetChild(0) as RectTransform : null;
            if (heading != null) EnsureCanvasGroup(heading).alpha = 0f;
        }

        private RectTransform CreateEdnaSpeech(Transform parent)
        {
            RectTransform bubble = CreatePanel("Edna Speech", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            bubble.GetComponent<Image>().raycastTarget = true;
            AddSingleShadow(bubble.gameObject, InvestigationTheme.PaperShadow, new Vector2(0f, -3f));
            Text name = CreateText("Edna Name", bubble, EdnaHeading(), 12, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(name.rectTransform, 0f, 1f, 1f, 1f, 16f, -ednaHeaderHeight + 2f, -EdnaPortraitWidth - 66f, -8f);
            Button close = CreateButton("Dismiss Edna", bubble, "×", ButtonVisualStyle.PaperChoice, DismissEdna, out _);
            close.GetComponent<LayoutElement>().ignoreLayout = true;
            float closeRight = EdnaPortraitWidth + 6f;
            Anchor(close.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -closeRight - 44f, -48f, -closeRight, -4f);
            Text message = CreateText("Edna Speech Text", bubble, EdnaMessage(), 15, FontStyle.Bold,
                InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            RectTransform options = CreatePanel("Edna Reply Options", bubble, Color.clear, 0f);
            float optionsWidth = EdnaOptionsWidth;
            float x = 0f;
            if (!string.IsNullOrEmpty(EdnaActionLabel))
            {
                Button action = CreateButton("Edna Continue", options, EdnaActionLabel, ButtonVisualStyle.PaperChoice, PerformEdnaAction, out Text actionLabel);
                actionLabel.fontSize = 13;
                action.targetGraphic.color = InvestigationTheme.PaperSelected;
                action.GetComponent<LayoutElement>().ignoreLayout = true;
                Anchor(action.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f, x, 0f, x + 148f, 0f);
                x += 156f;
            }
            if (EdnaCanAskNext)
            {
                CreateEdnaReplyButton(options, "Edna Next Step", "What next?", EdnaReply.NextStep, x, 116f);
                x += 124f;
            }
            if (EdnaCanAskWhy)
            {
                CreateEdnaReplyButton(options, "Edna Why", "Why?", EdnaReply.Explanation, x, 72f);
                x += 80f;
            }
            if (EdnaCanOpenNotebook)
            {
                Button openNotebook = CreateButton("Edna Open Notebook", options, "Open notebook",
                    ButtonVisualStyle.PaperChoice, ToggleNotebookDrawer, out Text openLabel);
                openLabel.fontSize = 13;
                openNotebook.targetGraphic.color = InvestigationTheme.PaperSelected;
                openNotebook.GetComponent<LayoutElement>().ignoreLayout = true;
                Anchor(openNotebook.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f, x, 0f, x + 136f, 0f);
            }
            options.gameObject.SetActive(optionsWidth > 0f);
            Image portrait = CreateStatusIcon("Edna Portrait", bubble, ednaPortrait, Color.white);
            float rightInset = EdnaPortraitWidth + 66f;
            Anchor(portrait.rectTransform, 1f, 0f, 1f, 1f, -EdnaPortraitWidth, 0f, -2f, 2f);
            if (ednaOptionsInLine)
            {
                Anchor(message.rectTransform, 0f, 0f, 1f, 1f, 16f, 12f, -rightInset - optionsWidth - 12f, -ednaHeaderHeight);
                Anchor(options, 1f, .5f, 1f, .5f, -rightInset - optionsWidth, -32f, -rightInset, 12f);
            }
            else
            {
                Anchor(message.rectTransform, 0f, 0f, 1f, 1f, 16f, optionsWidth > 0f ? 64f : 12f, -rightInset, -ednaHeaderHeight);
                Anchor(options, 0f, 0f, 0f, 0f, 16f, 10f, 16f + optionsWidth, 54f);
            }
            return bubble;
        }

        private void CreateEdnaReplyButton(Transform parent, string objectName, string label, EdnaReply reply, float x, float width)
        {
            Button button = CreateButton(objectName, parent, label, ButtonVisualStyle.PaperChoice, () => AskEdna(reply), out Text text);
            text.fontSize = 13;
            button.GetComponent<LayoutElement>().ignoreLayout = true;
            Anchor(button.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f, x, 0f, x + width, 0f);
        }

        private IEnumerator AnimateEdnaEntrance(RectTransform target)
        {
            CanvasGroup group = EnsureCanvasGroup(target);
            for (float elapsed = 0f; elapsed < .18f; elapsed += Time.unscaledDeltaTime)
            {
                if (target == null || group == null) yield break;
                group.alpha = Mathf.Clamp01(elapsed / .18f);
                yield return null;
            }
            if (group != null) group.alpha = 1f;
        }

        private void OnDestroy() => ((IDisposable)ednaTextMeasure).Dispose();
    }
}
