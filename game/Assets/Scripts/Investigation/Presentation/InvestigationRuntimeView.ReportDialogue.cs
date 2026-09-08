using System;
using System.Collections;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum ReportConversationRound { Clues, Explanation, Review }
        private enum ReportKeyClue { None, FoodWeb, Seafloor, FishingLine }
        private const string InspectSharkHabitatLabel = "Inspect former shark habitat";
        private const string InspectSeafloorLabel = "Inspect the seafloor";
        private ReportConversationRound reportConversationRound;
        private bool reportConversationInitialized;
        private bool reportConversationFocusPending;
        private string reportRovFocus = string.Empty;
        private bool reportCauseEditing;
        private ReportKeyClue reportKeyClue;
        private bool reportChoosingKeyClue;
        private bool reportKeyClueResponseActive;

        private void PrepareReportConversation()
        {
            if (state == null || state.Phase != InvestigationPhase.Report) return;
            if (!reportConversationInitialized)
            {
                reportConversationInitialized = true;
                // Imported/QA reports already contain their evidence and draft.
                reportConversationRound = state.ConfirmationReviewed ? ReportConversationRound.Review : ReportConversationRound.Clues;
                reportRovFocus = state.ConfirmationReviewed ? "E07_FISHING_LINE" : string.Empty;
            }
            if (reportFeedbackFocusPending)
            {
                reportConversationRound = ReportConversationRound.Explanation;
                reportCauseEditing = false;
                reportChoosingKeyClue = false;
                reportKeyClueResponseActive = false;
                reportConversationFocusPending = true;
            }
        }

        private bool CanWriteFinalReport()
        {
            return state != null && state.Phase == InvestigationPhase.Report
                && state.ConclusionStatus != InvestigationConclusionStatus.Correct
                && state.ConfirmationReviewed
                && new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state).RequiredObjectivesComplete;
        }

        private void ShowReportRound(ReportConversationRound round, bool editCause = false)
        {
            if (round != ReportConversationRound.Clues && !state.ConfirmationReviewed) return;
            reportConversationRound = round;
            reportCauseEditing = editCause;
            reportConversationFocusPending = true;
            RefreshPresentationOnly();
        }

        private void InspectRovClue(string evidenceId)
        {
            reportRovFocus = evidenceId;
            reportConversationRound = ReportConversationRound.Clues;
            reportConversationFocusPending = true;
            if (!state.ConfirmationReviewed) reviewConfirmation?.Invoke();
            else RefreshPresentationOnly();
        }

        private void DiscussReportExplanation()
        {
            reportChoosingKeyClue = reportKeyClue == ReportKeyClue.None || reportCrossCheck == ReportKeyClue.None;
            ShowReportRound(ReportConversationRound.Explanation);
        }

        private void ChooseReportKeyClue(ReportKeyClue clue)
        {
            // This records the player's emphasis, not a scored evidence answer.
            reportKeyClue = clue;
            reportChoosingKeyClue = false;
            reportKeyClueResponseActive = true;
            reportConversationFocusPending = true;
            RefreshPresentationOnly();
        }

        private string ReportKeyClueLabel(ReportKeyClue clue)
        {
            switch (clue)
            {
                case ReportKeyClue.FoodWeb: return "Shark–tuna–krill changes";
                case ReportKeyClue.Seafloor: return "Stable sea star and intact seafloor";
                case ReportKeyClue.FishingLine: return "Fishing line from the ROV";
                default: return string.Empty;
            }
        }

        private string ReportKeyClueResponse()
        {
            switch (reportKeyClue)
            {
                case ReportKeyClue.FoodWeb:
                    return "The shark–tuna–krill pattern fits both fishing models. It helps explain the food web, but the sea-star and seafloor clues help tell those models apart.";
                case ReportKeyClue.Seafloor:
                    return "The stable sea star and intact seafloor challenge the damage predicted by bottom trawling. That helps distinguish it from long-line fishing, even though their food-web predictions overlap.";
                case ReportKeyClue.FishingLine:
                    return "Fishing line adds physical evidence of fishing activity. It does not settle which fishing method was involved; weigh it alongside the intact seafloor and our species findings.";
                default: return string.Empty;
            }
        }

        private string ReportConversationMessage()
        {
            if (reportConversationRound == ReportConversationRound.Clues)
            {
                if (!state.ConfirmationReviewed && !string.IsNullOrEmpty(rovScanId))
                    return "Sweep the camera across the inspection frame. When you locate a finding, pin it to collect the ROV field notes.";
                if (!state.ConfirmationReviewed)
                    return "The ROV is back. We have two camera notes to add to your eDNA findings. Which would you like to look at first?";
                return reportRovFocus == "E08_SEAFLOOR_INTACT"
                    ? "The seafloor looks intact, which fits our stable sea-star finding and challenges widespread trawling damage. The other camera note records fishing line. Both clues are now in your notebook."
                    : "The camera found fishing line: a physical clue to fishing activity. It also recorded an intact seafloor. Together with our species comparisons, these clues help us weigh the explanations.";
            }
            string cause = caseDefinition.FindThreat(state.FinalThreatId)?.DisplayName
                ?? caseDefinition.FindThreat(state.ProvisionalThreatId)?.DisplayName ?? "your first idea";
            if (reportConversationRound == ReportConversationRound.Explanation)
            {
                if (reportCauseEditing) return "Which explanation would you like to put in our report? Your findings and comparisons will stay with it.";
                if (reportChoosingKeyClue)
                    return $"Your explanation is {cause}. Place a main clue and a different cross-check on your argument board. Drag cards onto the slots, or select a card then a slot.";
                if (!reportKeyClueResponseActive && !string.IsNullOrEmpty(reportDiagnosticMessage))
                {
                    foreach (PredictionComparisonRecord record in state.ComparisonRecords)
                    {
                        if (record.ThreatId != state.FinalThreatId || !record.LocksComparison || record.Judgement != ComparisonJudgement.Mismatch) continue;
                        InvestigationObservationDefinition finding = caseDefinition.FindObservation(record.EvidenceId);
                        if (finding == null) continue;
                        string cameraContext = finding.Category == EvidenceCategory.Benthic ? " The ROV also found an intact seafloor." : string.Empty;
                        return $"Our finding, {finding.DisplayName}, challenged {cause}.{cameraContext} We can revise the explanation or look at the clues again.";
                    }
                    return reportDiagnosticMessage + " We can revise the explanation or look at the clues again.";
                }
                if (reportKeyClue != ReportKeyClue.None)
                    return ReportKeyClueResponse() + ArgumentCrossCheckResponse() + $" Would you keep {cause}, or change your explanation?";
                InvestigationHypothesisSummary summary = new InvestigationHypothesisSummary(caseDefinition, state, state.FinalThreatId);
                string caution = summary.ChallengeCount > 0
                    ? " Some of our comparisons challenged that model."
                    : " Let's weigh it against the survey and the two ROV clues.";
                return $"Your explanation is {cause}.{caution} Would you like to keep it, change it, or review the comparisons?";
            }
            return $"Our report proposes {cause} and brings together the survey, model checks and ROV clues. It also notes that non-detection does not prove absence. Ready to send, or would you like another look?";
        }

        private void RenderReportConversation()
        {
            RectTransform panel = CreatePanel("Report Edna Conversation", contentRoot, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            HorizontalLayoutGroup row = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(20, 8, 18, 16);
            row.spacing = 16f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.LowerLeft;
            RectTransform words = new GameObject("Report Conversation Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
            words.SetParent(panel, false);
            words.GetComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup column = words.GetComponent<VerticalLayoutGroup>();
            column.spacing = 12f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            string topic = reportConversationRound == ReportConversationRound.Clues ? "LOOK AT THE CLUES"
                : reportConversationRound == ReportConversationRound.Explanation ? "WEIGH OUR EXPLANATION" : "SEND OUR REPORT";
            Text progress = CreateText("Report Dialogue Progress", words, $"EDNA · {(int)reportConversationRound + 1}/3 · {topic}",
                12, FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.DataFont);
            ConfigureContentDrivenText(progress);
            if (reportConversationRound == ReportConversationRound.Explanation && reportKeyClue != ReportKeyClue.None
                && !reportChoosingKeyClue && !reportCauseEditing)
                RenderReportKeyClueSummary(words);
            Text speech = CreateText("Report Edna Speech", words, ReportConversationMessage(), 17, FontStyle.Bold,
                InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(speech);
            if (reportConversationRound == ReportConversationRound.Explanation && reportChoosingKeyClue && !reportCauseEditing)
                RenderArgumentBoard(words);
            RectTransform actions = new GameObject("Report Conversation Actions", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            actions.SetParent(words, false);
            actions.GetComponent<InvestigationResponsiveGridLayout>().Configure(3, 2, 1, 52f, 8f);
            RenderReportConversationActions(actions);
            EnsureEdnaArtwork();
            Image portrait = CreateStatusIcon("Report Edna Portrait", panel, ednaPortrait, Color.white);
            LayoutElement portraitSize = portrait.gameObject.AddComponent<LayoutElement>();
            portraitSize.minWidth = portraitSize.preferredWidth = EdnaCompact ? 84f : 128f;
            portraitSize.minHeight = portraitSize.preferredHeight = EdnaCompact ? 112f : 166f;
        }

        private void RenderReportKeyClueSummary(Transform parent)
        {
            RectTransform row = CreatePanel("Report Key Clue Summary", parent, Color.clear, 0f);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            Text label = CreateText("Report Key Clue Label", row, "Your key clue: " + ReportKeyClueLabel(reportKeyClue),
                13, FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(label);
            LayoutElement labelSize = label.gameObject.AddComponent<LayoutElement>();
            labelSize.minWidth = 0f;
            labelSize.flexibleWidth = 1f;
            Button reconsider = CreateButton("Reconsider Report Clue", row, "Another clue", ButtonVisualStyle.PaperChoice, () =>
            {
                reportChoosingKeyClue = true;
                ShowReportRound(ReportConversationRound.Explanation);
            }, out Text buttonLabel);
            buttonLabel.fontSize = 13;
            LayoutElement size = reconsider.GetComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 116f;
            size.minHeight = size.preferredHeight = 40f;
            size.flexibleWidth = size.flexibleHeight = 0f;
            reconsider.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
        }

        private void ReportReply(Transform parent, string name, string label, Action action, bool primary = false)
        {
            Button button = CreateButton(name, parent, label, ButtonVisualStyle.PaperChoice, () => action?.Invoke(), out Text text);
            text.fontSize = 14;
            if (primary) button.targetGraphic.color = InvestigationTheme.PaperSelected;
            button.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
        }

        private void RenderReportConversationActions(Transform actions)
        {
            switch (reportConversationRound)
            {
                case ReportConversationRound.Clues:
                    if (!state.ConfirmationReviewed && !string.IsNullOrEmpty(rovScanId))
                    {
                        ReportReply(actions, "Change ROV Area", "Choose another inspection area", () => { rovScanId = string.Empty; RefreshPresentationOnly(); });
                    }
                    else if (!state.ConfirmationReviewed)
                    {
                        ReportReply(actions, "Review ROV Follow-up", InspectSharkHabitatLabel, () => BeginWorkbenchRovScan("E07_FISHING_LINE"));
                        ReportReply(actions, "Review ROV Seafloor", InspectSeafloorLabel, () => BeginWorkbenchRovScan("E08_SEAFLOOR_INTACT"));
                    }
                    else
                    {
                        ReportReply(actions, "Discuss Explanation", "Discuss our explanation", DiscussReportExplanation, true);
                        bool lookingAtLine = reportRovFocus == "E07_FISHING_LINE";
                        ReportReply(actions, "Review Other ROV Clue", lookingAtLine ? "Look at the seafloor" : "Look at fishing line",
                            () => InspectRovClue(lookingAtLine ? "E08_SEAFLOOR_INTACT" : "E07_FISHING_LINE"));
                    }
                    break;
                case ReportConversationRound.Explanation:
                    if (reportCauseEditing)
                    {
                        foreach (ThreatSimulationDefinition threat in caseDefinition.Threats)
                        {
                            if (threat == null) continue;
                            ReportReply(actions, $"Final Cause {threat.ThreatId}", threat.DisplayName, () =>
                            {
                                reportCauseEditing = false;
                                reportConversationRound = ReportConversationRound.Review;
                                reportConversationFocusPending = true;
                                setFinalThreat?.Invoke(threat.ThreatId);
                            });
                        }
                        ReportReply(actions, "Cancel Report Cause", "Keep my current idea", () => ShowReportRound(ReportConversationRound.Explanation));
                    }
                    else if (reportChoosingKeyClue)
                    {
                        foreach (ReportKeyClue clue in new[] { ReportKeyClue.FoodWeb, ReportKeyClue.Seafloor, ReportKeyClue.FishingLine })
                        {
                            ReportKeyClue cardClue = clue;
                            Button card = CreateButton($"Report Key Clue {clue}", actions, ReportKeyClueLabel(clue), ButtonVisualStyle.PaperChoice,
                                () => { reportStagedClue = cardClue; RefreshPresentationOnly(); }, out Text cardLabel);
                            cardLabel.fontSize = 13;
                            if (reportStagedClue == clue) card.targetGraphic.color = InvestigationTheme.PaperSelected;
                            AddWorkbenchDrag(card, "report-clue", clue.ToString(), ReportKeyClueLabel(clue));
                        }
                        ReportReply(actions, "Discuss Connected Clues", "Discuss my evidence", () =>
                        {
                            if (reportKeyClue != ReportKeyClue.None && reportCrossCheck != ReportKeyClue.None) ChooseReportKeyClue(reportKeyClue);
                        }, true);
                        FindNamedRect(actions, "Discuss Connected Clues").GetComponent<Button>().interactable = reportKeyClue != ReportKeyClue.None && reportCrossCheck != ReportKeyClue.None;
                        ReportReply(actions, "Review Report Comparisons", "Review comparisons",
                            () => OpenNotebookComparisons(state.FinalThreatId));
                    }
                    else
                    {
                        ReportReply(actions, "Keep Report Explanation", "Keep my explanation", () => ShowReportRound(ReportConversationRound.Review), true);
                        ReportReply(actions, "Edit Report Cause", "Change explanation", () => ShowReportRound(ReportConversationRound.Explanation, true));
                        ReportReply(actions, "Review Report Comparisons", "Review comparisons",
                            () => OpenNotebookComparisons(state.FinalThreatId));
                        if (!string.IsNullOrEmpty(reportDiagnosticMessage))
                            ReportReply(actions, "Revisit ROV Clues", "Look at the clues again", () => ShowReportRound(ReportConversationRound.Clues));
                    }
                    break;
                case ReportConversationRound.Review:
                    if (CanWriteFinalReport())
                        ReportReply(actions, "Submit Final Report", "Send report", () => submitFinal?.Invoke(), true);
                    ReportReply(actions, "Edit Report Cause", "Review explanation", () => ShowReportRound(ReportConversationRound.Explanation, true));
                    ReportReply(actions, "Revisit ROV Clues", "Look at the clues again", () => ShowReportRound(ReportConversationRound.Clues));
                    break;
            }
        }

        private IEnumerator FocusReportConversationNextFrame()
        {
            yield return null;
            RectTransform actions = FindNamedRect(contentRoot, "Report Conversation Actions");
            Button first = actions == null ? null : FindFirstInteractableButton(actions);
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        private void RenderReportReview(Transform paper)
        {
            RectTransform review = CreatePanel("Report Review", paper, Color.clear, 0f);
            VerticalLayoutGroup layout = review.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            RectTransform cause = CreatePaperSlot(review, "Report Cause Section", "Our explanation");
            Text explanation = CreateText("Report Explanation", cause, caseDefinition.FindThreat(state.FinalThreatId)?.DisplayName ?? "Our first idea",
                24, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(explanation);
            if (reportKeyClue != ReportKeyClue.None)
            {
                Text keyClue = CreateText("Report Highlighted Clue", cause, "Key clue you highlighted: " + ReportKeyClueLabel(reportKeyClue),
                    14, FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                ConfigureContentDrivenText(keyClue);
                if (reportCrossCheck != ReportKeyClue.None)
                {
                    Text cross = CreateText("Report Cross Check", cause, "Cross-check: " + ReportKeyClueLabel(reportCrossCheck), 14,
                        FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                    ConfigureContentDrivenText(cross);
                }
            }
            RenderCollectedFindings(review);
            RenderReportFoodWeb(review);
            InvestigationLimitationDefinition limitation = caseDefinition.FindLimitation(state.SelectedLimitationId);
            RectTransform caution = CreatePaperSlot(review, "Report Limitation Section", "What remains uncertain");
            Text note = CreateText("Report Scientific Caution", caution,
                limitation == null ? "Interpret the survey together with its sampling limits." : limitation.DisplayName + ". " + limitation.Explanation,
                13, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(note);
        }

        private void RenderCollectedFindings(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Evidence Section", "Your checked findings");
            RectTransform grid = new GameObject("Report Findings", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(section, false);
            grid.GetComponent<InvestigationResponsiveGridLayout>().Configure(3, 1, 76f, 8f);
            foreach (string evidenceId in state.SelectedReportEvidenceIds)
            {
                InvestigationObservationDefinition finding = caseDefinition.FindObservation(evidenceId);
                if (finding == null) continue;
                RectTransform item = CreatePanel($"Report Finding {evidenceId}", grid, InvestigationTheme.Paper, InvestigationTheme.SmallRadius);
                Image icon = CreateStatusIcon("Finding Icon", item, InvestigationEvidenceIconLibrary.ForObservation(finding), InvestigationTheme.PaperSelectedBorder);
                Anchor(icon.rectTransform, 0f, .5f, 0f, .5f, 12f, -15f, 42f, 15f);
                Text text = CreateText("Finding Text", item, finding.DisplayName, 14, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
                Anchor(text.rectTransform, 0f, 0f, 1f, 1f, 54f, 8f, -12f, -8f);
            }
        }

        private void RenderReportFoodWeb(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Reasoning Section", "How this model connects the species");
            SimulationResult simulation = state.FindSimulation(state.FinalThreatId);
            RectTransform chain = CreatePanel("Report Food Web", section, Color.clear, 0f);
            AddLayout(chain, 130f, 1f);
            HorizontalLayoutGroup layout = chain.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            for (int index = 0; index < caseDefinition.FoodWebChainSpeciesIds.Count; index++)
            {
                string speciesId = caseDefinition.FoodWebChainSpeciesIds[index];
                InvestigationSpeciesDefinition species = caseDefinition.FindSpecies(speciesId);
                if (species == null) continue;
                if (index > 0)
                {
                    Text arrow = CreateText("Report Food Web Arrow", chain, "→", 23, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.DisplayFont);
                    LayoutElement arrowSize = AddLayout(arrow.rectTransform, 116f, 0f);
                    arrowSize.minWidth = arrowSize.preferredWidth = 20f;
                }
                RectTransform node = CreatePanel($"Report Model {speciesId}", chain, Color.clear, 0f);
                AddLayout(node, 126f, 1f).minWidth = 0f;
                Image icon = CreateStatusIcon("Species Artwork", node, species.Icon, Color.white);
                Anchor(icon.rectTransform, 0f, 0f, 1f, 1f, 12f, 53f, -12f, -2f);
                PredictionState prediction = simulation?.FindPrediction(speciesId)?.PredictedState ?? PredictionState.Unknown;
                Text label = CreateText("Report Model Label", node, species.GameplayName + "\n" + PredictionLabel(prediction), 13, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Anchor(label.rectTransform, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 50f);
            }
            Text caption = CreateText("Report Model Caption", section, "Model predictions, interpreted alongside your survey findings; eDNA detection is not a population count.",
                12, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(caption);
        }
    }
}
