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
        private const string OpenRovFindingsLabel = "Open ROV findings";
        private const string CompareFishingModelsLabel = "Compare fishing models";
        private enum ReportGuidanceSection
        {
            None,
            Cause,
            Reasoning,
            Evidence,
            Limitation,
            Review
        }

        private ReportGuidanceSection reportDiagnosticSection;
        private bool reportFeedbackFocusPending;
        private string reportDiagnosticMessage = string.Empty;

        public void RequestReportFeedbackFocus(string feedback)
        {
            reportDiagnosticMessage = feedback;
            reportFeedbackFocusPending = true;
        }

        private void RenderReport()
        {
            ThreatSimulationDefinition provisional = caseDefinition.FindThreat(state.ProvisionalThreatId);
            bool caseClosed = state.ConclusionStatus == InvestigationConclusionStatus.Correct;
            InvestigationReadiness readiness = new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
            bool needsFollowUpComparison = state.ConfirmationReviewed && !readiness.RequiredObjectivesComplete;
            Text reportHeading = CreateHeading(
                caseClosed
                    ? "Case closed"
                    : needsFollowUpComparison
                        ? "Re-test the close match"
                        : state.ConfirmationReviewed ? "Explain your findings" : "Your first idea is saved",
                caseClosed
                    ? "Review how the survey, ecosystem model and follow-up evidence support your conclusion."
                    : needsFollowUpComparison
                        ? "Use the intact seafloor clue to compare Sea star under both fishing models, then return to the report."
                    : state.ConfirmationReviewed
                    ? "Answer one question at a time, then review your report with Edna."
                    : $"Your provisional explanation is {provisional?.DisplayName ?? "not selected"}. The same ROV follow-up appears for every provisional choice.");
            bool writingReport = CanWriteFinalReport();
            if (!writingReport) RenderEdnaGuide(reportHeading.transform.parent);
            if (!caseClosed && !writingReport) RenderConfirmationPanel();
            else RenderReportPaper();

            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct)
            {
                Button again = CreateButton("Restart Completed Case", footerRight, "Investigate again", ButtonVisualStyle.Primary, () => restart?.Invoke(), out _);
                ConfigureReportFooterButton(again, 116f);
                return;
            }

            if (restartConfirmationPending)
            {
                Button cancel = CreateButton("Cancel Restart Case", footerLeft, "Cancel", ButtonVisualStyle.Tertiary, CancelRestartConfirmation, out _);
                ConfigureReportFooterButton(cancel, 80f);
                Button confirm = CreateButton("Confirm Restart Case", footerRight, "Confirm restart", ButtonVisualStyle.Danger, ConfirmRestart, out _);
                ConfigureReportFooterButton(confirm, 116f);
                return;
            }

            Button back = CreateButton("Back To Simulator", footerLeft, "← Back to simulator", ButtonVisualStyle.Tertiary, () => setPhase?.Invoke(InvestigationPhase.Simulate), out _);
            ConfigureReportFooterButton(back, 126f);
            Button restartButton = CreateButton("Restart Case", footerLeft, "Restart case", ButtonVisualStyle.Danger, RequestRestartConfirmation, out _);
            ConfigureReportFooterButton(restartButton, 100f);

            CreateNotebookDrawerButton(footerRight);
            if (state.ConfirmationReviewed && readiness.RequiredObjectivesComplete)
            {
                if (reportDialogueSection == ReportGuidanceSection.Review)
                {
                    Button submit = CreateButton("Submit Final Report", footerRight,
                        readiness.CanSubmitFinal ? "Send report" : "Check report",
                        ButtonVisualStyle.Primary, () => submitFinal?.Invoke(), out _);
                    ConfigureReportFooterButton(submit, readiness.CanSubmitFinal ? 104f : 116f);
                }
                else
                {
                    Button next = CreateButton("Continue Report Dialogue", footerRight,
                        reportDialogueSection == ReportGuidanceSection.Limitation ? "Review report →" : "Continue →",
                        ButtonVisualStyle.Primary, ContinueReportDialogue, out _);
                    ConfigureReportFooterButton(next, reportDialogueSection == ReportGuidanceSection.Limitation ? 132f : 104f);
                }
            }
        }

        private void RenderConfirmationPanel()
        {
            RectTransform confirmation = CreatePanel("ROV Confirmation", contentRoot,
                InvestigationTheme.SurfaceQuiet, InvestigationTheme.SmallRadius);
            EnsureOutline(confirmation.gameObject,
                state.ConfirmationReviewed ? InvestigationTheme.Success : InvestigationTheme.Primary, new Vector2(1f, -1f));
            VerticalLayoutGroup layout = confirmation.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 12f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text title = CreateText("ROV Title", confirmation,
                state.ConfirmationReviewed ? "New ROV evidence received" : "ROV field notes are ready",
                20, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(title);
            Text detail = CreateText("ROV Detail", confirmation,
                state.ConfirmationReviewed
                    ? "Two findings were added to your notebook. Check them against the two fishing models."
                    : "Your first idea is saved. Open these two findings to see what the ROV recorded.",
                14, FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(detail);

            RectTransform evidenceRow = new GameObject("ROV Evidence", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            evidenceRow.SetParent(confirmation, false);
            evidenceRow.GetComponent<InvestigationResponsiveGridLayout>().Configure(2, 1, 190f, 12f);
            if (state.ConfirmationReviewed)
            {
                CreateConfirmationItem(evidenceRow, "E07_FISHING_LINE", InvestigationEvidenceIconLibrary.FishingLine);
                CreateConfirmationItem(evidenceRow, "E08_SEAFLOOR_INTACT", InvestigationEvidenceIconLibrary.Seafloor);
            }
            else
            {
                for (int index = 1; index <= 2; index++)
                {
                    RectTransform sealedNote = CreatePanel($"Sealed ROV Finding {index}", evidenceRow,
                        InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
                    Image icon = CreateStatusIcon("Sealed Finding Icon", sealedNote, InvestigationStatusIconLibrary.Question, InvestigationTheme.Primary);
                    Anchor(icon.rectTransform, 0f, 0.5f, 0f, 0.5f, 20f, -28f, 76f, 28f);
                    Text note = CreateText("Sealed Finding Title", sealedNote, $"ROV finding {index:00}",
                        19, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.LowerLeft, InvestigationTheme.DisplayFont);
                    Anchor(note.rectTransform, 0f, 0.5f, 1f, 0.82f, 96f, 2f, -18f, 0f);
                    Text ready = CreateText("Sealed Finding Status", sealedNote, "Ready to review",
                        14, FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                    Anchor(ready.rectTransform, 0f, 0.18f, 1f, 0.5f, 96f, 0f, -18f, -6f);
                }
            }

            RectTransform actions = CreatePanel("ROV Actions", confirmation, Color.clear, 0f);
            AddLayout(actions, 48f, 1f);
            HorizontalLayoutGroup actionLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionLayout.childAlignment = TextAnchor.MiddleRight;
            actionLayout.childControlWidth = actionLayout.childControlHeight = false;
            actionLayout.childForceExpandWidth = actionLayout.childForceExpandHeight = false;
            Button action = state.ConfirmationReviewed
                ? CreateButton("Re-test Fishing Models", actions, CompareFishingModelsLabel + " →", ButtonVisualStyle.Primary,
                    () => setPhase?.Invoke(InvestigationPhase.Simulate), out _)
                : CreateButton("Review ROV Follow-up", actions, OpenRovFindingsLabel, ButtonVisualStyle.Primary,
                    () => reviewConfirmation?.Invoke(), out _);
            action.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 48f);
            action.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
        }

        private void CreateConfirmationItem(Transform parent, string evidenceId, Sprite iconSprite)
        {
            InvestigationObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
            if (observation == null) return;
            RectTransform item = CreatePanel($"Confirmation {evidenceId}", parent, InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
            Image icon = CreateStatusIcon("Confirmation Icon", item, iconSprite, InvestigationTheme.Primary);
            Anchor(icon.rectTransform, 0f, 1f, 0f, 1f, 20f, -80f, 76f, -24f);
            Text label = CreateText("Observation", item, observation.DisplayName,
                18, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
            Anchor(label.rectTransform, 0f, 1f, 1f, 1f, 96f, -84f, -18f, -16f);
            Text details = CreateText("Confirmation Detail", item, observation.Detail,
                14, FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            Anchor(details.rectTransform, 0f, 0f, 1f, 1f, 20f, 42f, -20f, -94f);
            Text added = CreateText("Notebook Added", item, "ADDED TO NOTEBOOK · ROV", 11,
                FontStyle.Bold, InvestigationTheme.Success, TextAnchor.MiddleLeft, InvestigationTheme.DataFont);
            Anchor(added.rectTransform, 0f, 0f, 1f, 0f, 20f, 14f, -20f, 34f);
        }

        private IEnumerator AnimateRovEvidenceReveal()
        {
            RectTransform fishingLine = FindNamedRect(contentRoot, "Confirmation E07_FISHING_LINE");
            RectTransform seafloor = FindNamedRect(contentRoot, "Confirmation E08_SEAFLOOR_INTACT");
            if (fishingLine == null || seafloor == null) yield break;

            CanvasGroup first = EnsureCanvasGroup(fishingLine);
            CanvasGroup second = EnsureCanvasGroup(seafloor);
            first.alpha = 0f;
            second.alpha = 0f;
            fishingLine.localScale = Vector3.one * 0.96f;
            seafloor.localScale = Vector3.one * 0.96f;

            yield return AnimateRovEvidenceCard(fishingLine, first, 0.34f);
            yield return new WaitForSecondsRealtime(0.12f);
            yield return AnimateRovEvidenceCard(seafloor, second, 0.34f);
        }

        private static IEnumerator AnimateRovEvidenceCard(RectTransform card, CanvasGroup group, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (card == null || group == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                group.alpha = eased;
                card.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
                yield return null;
            }
            if (card == null || group == null) yield break;
            group.alpha = 1f;
            card.localScale = Vector3.one;
        }

        private static CanvasGroup EnsureCanvasGroup(RectTransform rect)
        {
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            return group == null ? rect.gameObject.AddComponent<CanvasGroup>() : group;
        }

        private static RectTransform FindNamedRect(Transform root, string objectName)
        {
            if (root == null) return null;
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            for (int index = 0; index < rects.Length; index++)
            {
                if (rects[index].gameObject.activeInHierarchy && rects[index].name == objectName) return rects[index];
            }
            return null;
        }

        private void RenderReportPaper()
        {
            RectTransform paper = CreatePanel("Survey Report Paper", contentRoot, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            VerticalLayoutGroup layout = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 18, 18);
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform reportHeader = CreatePanel("Report Header Row", paper, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(reportHeader, 44f, 1f);
            HorizontalLayoutGroup headerLayout = reportHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 12f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            string reportTitle = state.ConclusionStatus == InvestigationConclusionStatus.Correct ? "Survey report"
                : reportDialogueSection == ReportGuidanceSection.Review ? "Review report" : "Your report";
            Text title = CreateText("Report Title", reportHeader, reportTitle, 25, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.minWidth = 170f;
            titleLayout.preferredWidth = 190f;
            string attemptText = state.FinalSubmissionAttemptCount == 0
                ? string.Empty
                : $" · Revision {state.FinalSubmissionAttemptCount}";
            string compactSite = CompactReportMetadataValue(state.SiteDisplayName, 15, "Survey site");
            string compactSurvey = CompactReportMetadataValue(state.SurveyDisplayName, 13, "Survey");
            Text metadata = CreateText("Report Metadata", reportHeader, $"Researcher: You · Site: {compactSite} · {compactSurvey}{attemptText}", 13, FontStyle.Normal, InvestigationTheme.PaperMuted, TextAnchor.MiddleRight, InvestigationTheme.DataFont);
            metadata.horizontalOverflow = HorizontalWrapMode.Overflow;
            metadata.verticalOverflow = VerticalWrapMode.Overflow;
            metadata.resizeTextForBestFit = true;
            metadata.resizeTextMinSize = 10;
            metadata.resizeTextMaxSize = 13;
            LayoutElement metadataLayout = metadata.gameObject.AddComponent<LayoutElement>();
            metadataLayout.minWidth = 260f;
            metadataLayout.flexibleWidth = 1f;
            RectTransform rule = CreatePanel("Report Header Rule", paper, InvestigationTheme.PaperRule, 0f);
            AddLayout(rule, 1f, 1f);

            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct || reportDialogueSection == ReportGuidanceSection.Review)
                CreateReportOutcome(paper);
            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct)
            {
                CreateCaseClosedSummary(paper);
                return;
            }

            RenderReportDialogue(paper);
        }

        private static string CompactReportMetadataValue(string value, int maximumCharacters, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            if (normalized.Length <= maximumCharacters) return normalized;
            return normalized.Substring(0, Mathf.Max(1, maximumCharacters - 1)).TrimEnd() + "…";
        }

        private void CreateReportOutcome(Transform parent)
        {
            if (state.ConclusionStatus == InvestigationConclusionStatus.NotSubmitted) return;

            Color feedbackColor;
            Sprite feedbackIcon;
            switch (state.ConclusionStatus)
            {
                case InvestigationConclusionStatus.Correct:
                    feedbackColor = InvestigationTheme.ReportSuccess;
                    feedbackIcon = InvestigationStatusIconLibrary.Check;
                    break;
                case InvestigationConclusionStatus.InsufficientEvidence:
                    feedbackColor = InvestigationTheme.ReportGuide;
                    feedbackIcon = InvestigationStatusIconLibrary.Question;
                    break;
                default:
                    feedbackColor = InvestigationTheme.ReportError;
                    feedbackIcon = InvestigationStatusIconLibrary.Cross;
                    break;
            }

            RectTransform feedback = CreatePanel("Report Outcome", parent, feedbackColor, InvestigationTheme.SmallRadius);
            HorizontalLayoutGroup feedbackLayout = feedback.gameObject.AddComponent<HorizontalLayoutGroup>();
            feedbackLayout.padding = new RectOffset(14, 14, 10, 10);
            feedbackLayout.spacing = 12f;
            feedbackLayout.childAlignment = TextAnchor.MiddleLeft;
            feedbackLayout.childControlWidth = true;
            feedbackLayout.childControlHeight = true;
            feedbackLayout.childForceExpandWidth = false;
            feedbackLayout.childForceExpandHeight = false;
            Image icon = CreateStatusIcon("Outcome Icon", feedback, feedbackIcon, InvestigationTheme.PaperInk);
            LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = 28f;
            iconLayout.preferredWidth = 28f;
            iconLayout.minHeight = 28f;
            iconLayout.preferredHeight = 28f;
            Text outcome = CreateText(
                "Outcome Text",
                feedback,
                state.ConclusionStatus == InvestigationConclusionStatus.Correct
                    ? "Report accepted. Your evidence supports a complete explanation."
                    : string.IsNullOrEmpty(reportDiagnosticMessage) ? statusMessage : reportDiagnosticMessage,
                15,
                FontStyle.Bold,
                InvestigationTheme.PaperInk,
                TextAnchor.MiddleLeft,
                InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(outcome);
            LayoutElement outcomeLayout = outcome.gameObject.AddComponent<LayoutElement>();
            outcomeLayout.minWidth = 0f;
            outcomeLayout.flexibleWidth = 1f;
        }

        private void CreateCaseClosedSummary(Transform parent)
        {
            ThreatSimulationDefinition finalThreat = caseDefinition.FindThreat(state.FinalThreatId);
            InvestigationReasoningDefinition reasoning = caseDefinition.FindReasoning(state.SelectedReasoningId);
            InvestigationLimitationDefinition limitation = caseDefinition.FindLimitation(state.SelectedLimitationId);

            RectTransform summary = CreatePanel("Case Closed Summary", parent, InvestigationTheme.PaperRaised, InvestigationTheme.SmallRadius);
            EnsureOutline(summary.gameObject, InvestigationTheme.PaperRule, new Vector2(1f, -1f));
            VerticalLayoutGroup layout = summary.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform headingRow = CreatePanel("Case Closed Heading", summary, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(headingRow, 76f, 1f);
            HorizontalLayoutGroup headingLayout = headingRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            headingLayout.spacing = 18f;
            headingLayout.childControlWidth = true;
            headingLayout.childControlHeight = true;
            headingLayout.childForceExpandHeight = false;
            headingLayout.childForceExpandWidth = false;
            Text title = CreateText(
                "Case Closed Title",
                headingRow,
                finalThreat?.DisplayName ?? "Best-supported cause recorded",
                22,
                FontStyle.Bold,
                InvestigationTheme.PaperInk,
                TextAnchor.MiddleLeft,
                InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(title);
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.minWidth = 160f;
            titleLayout.flexibleWidth = 1f;
            titleLayout.preferredHeight = 66f;
            CreateCaseClosedStamp(headingRow);

            Text explanation = CreateText(
                "Case Closed Explanation",
                summary,
                string.IsNullOrWhiteSpace(caseDefinition.SuccessFeedback)
                    ? "The report connects the ecosystem model to several independent kinds of evidence."
                    : caseDefinition.SuccessFeedback,
                14,
                FontStyle.Normal,
                InvestigationTheme.PaperInk,
                TextAnchor.MiddleLeft,
                InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(explanation);

            CreateDebriefRow(
                summary,
                "FOOD-WEB MECHANISM",
                reasoning?.DisplayName ?? "Ecosystem mechanism recorded",
                reasoning?.Explanation ?? string.Empty,
                InvestigationTheme.Success);
            CreateDebriefRow(
                summary,
                "BENTHIC CHECK",
                FindEvidenceSummary(EvidenceCategory.Benthic, "Benthic evidence was included"),
                "This is the key check that separates selective fishing from a cause that damages the seafloor.",
                InvestigationTheme.Primary);
            CreateDebriefRow(
                summary,
                "ROV FOLLOW-UP",
                BuildConfirmationSummary(),
                "Physical observations strengthen the explanation developed from eDNA and the ecosystem model.",
                InvestigationTheme.Accent);
            CreateDebriefRow(
                summary,
                "SCIENTIFIC CAUTION",
                limitation?.DisplayName ?? "Uncertainty recorded",
                limitation?.Explanation ?? string.Empty,
                InvestigationTheme.Focus);
        }

        private static void CreateDebriefRow(
            Transform parent,
            string heading,
            string statement,
            string explanation,
            Color accent)
        {
            RectTransform row = CreatePanel($"Debrief {heading}", parent, new Color(0f, 0f, 0f, 0f), 0f);
            RectTransform rule = CreatePanel("Debrief Rule", row, InvestigationTheme.PaperRule, 0f);
            rule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Anchor(rule, 0f, 1f, 1f, 1f, 0f, -1f, 0f, 0f);
            VerticalLayoutGroup layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 12, 8, 8);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text label = CreateText("Debrief Heading", row, heading, 11, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleLeft, InvestigationTheme.DataFont);
            ConfigureContentDrivenText(label);
            Text value = CreateText("Debrief Statement", row, statement, 16, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(value);
            if (string.IsNullOrWhiteSpace(explanation)) return;
            Text detail = CreateText("Debrief Explanation", row, explanation, 12, FontStyle.Normal, InvestigationTheme.PaperMuted, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(detail);
        }

        private string FindEvidenceSummary(EvidenceCategory category, string fallback)
        {
            for (int index = 0; index < state.SelectedReportEvidenceIds.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.FindObservation(state.SelectedReportEvidenceIds[index]);
                if (observation != null && observation.Category == category) return observation.DisplayName;
            }
            return fallback;
        }

        private string BuildConfirmationSummary()
        {
            string result = string.Empty;
            for (int index = 0; index < caseDefinition.ConfirmationEvidenceIds.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.FindObservation(caseDefinition.ConfirmationEvidenceIds[index]);
                if (observation == null || !state.HasDiscoveredObservation(observation.EvidenceId)) continue;
                if (!string.IsNullOrEmpty(result)) result += " · ";
                result += observation.DisplayName;
            }
            return string.IsNullOrEmpty(result) ? "ROV follow-up reviewed" : result;
        }

        private void CreateReportCauseSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Cause Section", "Which cause best explains your findings?");
            ThreatSimulationDefinition provisional = caseDefinition.FindThreat(state.ProvisionalThreatId);
            Text provisionalNote = CreateText(
                "Provisional Reminder",
                section,
                state.ConfirmationReviewed
                    ? $"Your first idea was {provisional?.DisplayName ?? "not recorded"}. Use the ROV evidence, then choose your final cause below."
                    : $"Your first idea is {provisional?.DisplayName ?? "not recorded"}. Review the sealed ROV follow-up before choosing a final cause.",
                13,
                FontStyle.Bold,
                InvestigationTheme.PaperMuted,
                TextAnchor.MiddleLeft,
                InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(provisionalNote);
            RectTransform grid = new GameObject("Cause Choices", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(section, false);
            InvestigationResponsiveGridLayout layout = grid.GetComponent<InvestigationResponsiveGridLayout>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.Configure(Mathf.Min(3, caseDefinition.Threats.Count), 1, 66f, 8f);
            for (int index = 0; index < caseDefinition.Threats.Count; index++)
            {
                ThreatSimulationDefinition threat = caseDefinition.Threats[index];
                if (threat == null) continue;
                Button button = CreateButton(
                    $"Final Cause {threat.ThreatId}",
                    grid,
                    threat.DisplayName,
                    ButtonVisualStyle.PaperChoice,
                    () => AnswerReportQuestion(ReportGuidanceSection.Cause, () => setFinalThreat?.Invoke(threat.ThreatId)),
                    out _);
                button.interactable = state.ConfirmationReviewed;
                button.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
                StylePaperChoice(button, string.Equals(state.FinalThreatId, threat.ThreatId, StringComparison.Ordinal));
            }
        }

        private void CreateReportEvidenceSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Evidence Section", "Which findings support your explanation?");
            int confirmationSelected = 0;
            for (int index = 0; index < caseDefinition.ConfirmationEvidenceIds.Count; index++)
            {
                if (state.HasSelectedEvidence(caseDefinition.ConfirmationEvidenceIds[index])) confirmationSelected++;
            }
            bool evidenceMinimumMet = state.SelectedReportEvidenceIds.Count >= caseDefinition.MinimumReportEvidence;
            bool confirmationMinimumMet = confirmationSelected >= caseDefinition.MinimumConfirmationEvidenceInReport;
            bool categoriesComplete = true;
            string categoryProgress = string.Empty;
            for (int requirementIndex = 0; requirementIndex < caseDefinition.EvidenceCategoryRequirements.Count; requirementIndex++)
            {
                InvestigationEvidenceCategoryRequirement requirement = caseDefinition.EvidenceCategoryRequirements[requirementIndex];
                if (requirement == null) continue;
                int selectedCount = CountSelectedEvidenceInCategory(requirement.Category);
                if (selectedCount < requirement.MinimumCount) categoriesComplete = false;
                categoryProgress += $"  ·  {EvidenceCategoryShortLabel(requirement.Category)} {selectedCount} / {requirement.MinimumCount}";
            }
            Text progress = CreateText(
                "Evidence Progress",
                section,
                $"Selected {state.SelectedReportEvidenceIds.Count} / {caseDefinition.MinimumReportEvidence}{categoryProgress}",
                12,
                FontStyle.Bold,
                evidenceMinimumMet && confirmationMinimumMet && categoriesComplete
                    ? InvestigationTheme.PaperSelectedBorder
                    : InvestigationTheme.PaperMuted,
                TextAnchor.MiddleLeft,
                InvestigationTheme.DataFont);
            ConfigureContentDrivenText(progress);
            RectTransform grid = new GameObject("Evidence Choices", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(section, false);
            InvestigationResponsiveGridLayout layout = grid.GetComponent<InvestigationResponsiveGridLayout>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.Configure(3, 2, 72f, 8f);
            for (int index = 0; index < state.DiscoveredObservationIds.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.FindObservation(state.DiscoveredObservationIds[index]);
                if (observation == null || observation.Source == ObservationSource.Methodology) continue;
                bool selected = state.HasSelectedEvidence(observation.EvidenceId);
                Button button = CreateButton(
                    $"Report Evidence {observation.EvidenceId}",
                    grid,
                    observation.DisplayName,
                    ButtonVisualStyle.PaperChoice,
                    () =>
                    {
                        reportDialogueReply = string.Empty;
                        setReportEvidence?.Invoke(observation.EvidenceId, !state.HasSelectedEvidence(observation.EvidenceId));
                    },
                    out Text label);
                button.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(42f, label.rectTransform.offsetMin.y);
                Image icon = CreateStatusIcon(
                    "Evidence Icon",
                    button.transform,
                    InvestigationEvidenceIconLibrary.ForObservation(observation),
                    selected ? InvestigationTheme.PaperSelectedBorder : InvestigationTheme.PaperMuted);
                Anchor(icon.rectTransform, 0f, 0f, 0f, 1f, 10f, 14f, 36f, -14f);
                StylePaperChoice(button, selected);
            }
        }

        private int CountSelectedEvidenceInCategory(EvidenceCategory category)
        {
            int count = 0;
            for (int index = 0; index < state.SelectedReportEvidenceIds.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.FindObservation(state.SelectedReportEvidenceIds[index]);
                if (observation != null && observation.Category == category) count++;
            }
            return count;
        }

        private static string EvidenceCategoryShortLabel(EvidenceCategory category)
        {
            switch (category)
            {
                case EvidenceCategory.FoodWeb: return "FOOD WEB";
                case EvidenceCategory.Benthic: return "BENTHIC";
                case EvidenceCategory.Confirmation: return "ROV";
                case EvidenceCategory.Environmental: return "ENV";
                case EvidenceCategory.Alternative: return "ALTERNATIVE";
                default: return "EVIDENCE";
            }
        }

        private void CreateReportReasoningSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Reasoning Section", "How did that cause the food-web changes?");
            RectTransform row = CreatePanel("Reasoning Choices", section, new Color(0f, 0f, 0f, 0f), 0f);
            VerticalLayoutGroup layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            for (int index = 0; index < caseDefinition.ReasoningOptions.Count; index++)
            {
                InvestigationReasoningDefinition reasoning = caseDefinition.ReasoningOptions[index];
                if (reasoning != null) CreateReasoningChoice(row, reasoning.ReasoningId, reasoning.DisplayName);
            }
        }

        private void CreateReasoningChoice(Transform parent, string reasoningId, string label)
        {
            Button button = CreateButton(
                $"Reasoning {reasoningId}",
                parent,
                label,
                ButtonVisualStyle.PaperChoice,
                () => AnswerReportQuestion(ReportGuidanceSection.Reasoning, () => setReasoning?.Invoke(reasoningId)),
                out Text choiceLabel);
            ConfigureWrappingChoice(button, choiceLabel);
            StylePaperChoice(button, string.Equals(state.SelectedReasoningId, reasoningId, StringComparison.Ordinal));
        }

        private void CreateReportLimitationSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Limitation Section", "What can your evidence still not tell us?");
            RectTransform row = CreatePanel("Limitation Choices", section, new Color(0f, 0f, 0f, 0f), 0f);
            VerticalLayoutGroup layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            for (int index = 0; index < caseDefinition.Limitations.Count; index++)
            {
                InvestigationLimitationDefinition limitation = caseDefinition.Limitations[index];
                if (limitation == null) continue;
                Button button = CreateButton(
                    $"Limitation {limitation.LimitationId}",
                    row,
                    limitation.DisplayName,
                    ButtonVisualStyle.PaperChoice,
                    () => AnswerReportQuestion(ReportGuidanceSection.Limitation, () => setLimitation?.Invoke(limitation.LimitationId)),
                    out Text choiceLabel);
                ConfigureWrappingChoice(button, choiceLabel);
                StylePaperChoice(button, string.Equals(state.SelectedLimitationId, limitation.LimitationId, StringComparison.Ordinal));
            }
        }

        private ReportGuidanceSection NextReportSection()
        {
            if (state == null) return ReportGuidanceSection.None;
            InvestigationReadiness readiness = new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
            if (!state.ConfirmationReviewed || !readiness.RequiredObjectivesComplete)
                return ReportGuidanceSection.None;
            if (!readiness.FinalCauseSelected) return ReportGuidanceSection.Cause;
            if (!readiness.ReasoningComplete) return ReportGuidanceSection.Reasoning;
            if (!readiness.EvidenceComplete
                || !readiness.EvidenceCategoriesComplete
                || !readiness.ConfirmationEvidenceIncluded)
            {
                return ReportGuidanceSection.Evidence;
            }
            if (!readiness.LimitationComplete) return ReportGuidanceSection.Limitation;
            return ReportGuidanceSection.None;
        }

        private void UpdateReportDiagnostic()
        {
            reportDiagnosticSection = state != null && state.Phase == InvestigationPhase.Report
                && (state.ConclusionStatus == InvestigationConclusionStatus.InsufficientEvidence
                    || state.ConclusionStatus == InvestigationConclusionStatus.Incorrect)
                ? state.ConclusionStatus == InvestigationConclusionStatus.Incorrect ? ReportGuidanceSection.Cause : NextReportSection()
                : ReportGuidanceSection.None;
        }

        private static string ReportSectionName(ReportGuidanceSection section)
        {
            return section == ReportGuidanceSection.Reasoning ? "Report Reasoning Section"
                : section == ReportGuidanceSection.Evidence ? "Report Evidence Section"
                : section == ReportGuidanceSection.Limitation ? "Report Limitation Section" : "Report Cause Section";
        }

        private IEnumerator FocusReportDiagnosticNextFrame()
        {
            yield return null;
            if (state == null || state.Phase != InvestigationPhase.Report || reportDiagnosticSection == ReportGuidanceSection.None) yield break;
            RectTransform section = FindNamedRect(contentRoot, ReportSectionName(reportDiagnosticSection));
            if (section == null) yield break;
            Canvas.ForceUpdateCanvases();
            float scrollableHeight = contentScroll.content.rect.height - contentScroll.viewport.rect.height;
            if (scrollableHeight > 0f)
            {
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(contentScroll.viewport, section);
                contentScroll.StopMovement();
                contentScroll.verticalNormalizedPosition = Mathf.Clamp01(contentScroll.verticalNormalizedPosition
                    + (bounds.max.y - contentScroll.viewport.rect.yMax + 12f) / scrollableHeight);
            }
            Button target = FindFirstInteractableButton(section);
            if (target != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(target.gameObject);
            }
        }

        private RectTransform CreatePaperSlot(Transform parent, string sectionName, string question)
        {
            RectTransform section = CreatePanel(sectionName, parent, InvestigationTheme.PaperRaised, InvestigationTheme.SmallRadius);
            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text label = CreateText("Question", section, question, 16, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(label);
            if (reportDiagnosticSection != ReportGuidanceSection.None && sectionName == ReportSectionName(reportDiagnosticSection))
            {
                EnsureOutline(section.gameObject, InvestigationTheme.Focus, new Vector2(2f, -2f));
                Text feedback = CreateText("Report Section Feedback", section, reportDiagnosticMessage,
                    13, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                ConfigureContentDrivenText(feedback);
            }
            return section;
        }

        private static void ConfigureContentDrivenText(Text text)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void ConfigureWrappingChoice(Button button, Text label)
        {
            ConfigureContentDrivenText(label);
            button.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
            InvestigationContentHeightLayoutElement contentHeight =
                button.gameObject.AddComponent<InvestigationContentHeightLayoutElement>();
            contentHeight.Configure(label, 52f, 20f, 16f);
        }

        private static void ConfigureReportFooterButton(Button button, float width)
        {
            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = 30f;
            layout.preferredHeight = 30f;
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 30f);
            Text label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            label.fontSize = 11;
            Stretch(label.rectTransform, 6f, 2f, -6f, -2f);
        }

        private void RequestRestartConfirmation()
        {
            notebookDrawerOpen = false;
            restartConfirmationPending = true;
            statusMessage = "Restarting clears the progress in this case. Confirm restart or cancel.";
            statusTone = InvestigationStatusTone.Warning;
            RefreshPresentationOnly();
        }

        private void CancelRestartConfirmation()
        {
            restartConfirmationPending = false;
            statusMessage = string.Empty;
            statusTone = InvestigationStatusTone.Guide;
            RefreshPresentationOnly();
        }

        private void ConfirmRestart()
        {
            restartConfirmationPending = false;
            restart?.Invoke();
        }

        private static void StylePaperChoice(Button button, bool selected)
        {
            Image border = button.GetComponent<Image>();
            border.color = selected ? InvestigationTheme.PaperSelectedBorder : InvestigationTheme.PaperBorder;
            Transform faceTransform = button.transform.Find("Paper Choice Face");
            Image face = faceTransform == null ? null : faceTransform.GetComponent<Image>();
            if (face != null) face.color = selected ? InvestigationTheme.PaperSelected : InvestigationTheme.PaperRaised;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = InvestigationTheme.PaperInk;
                text.rectTransform.offsetMax = new Vector2(selected ? -36f : -10f, text.rectTransform.offsetMax.y);
            }
            Transform evidenceIcon = button.transform.Find("Evidence Icon");
            if (evidenceIcon != null)
            {
                evidenceIcon.GetComponent<Image>().color = selected
                    ? InvestigationTheme.PaperSelectedBorder
                    : InvestigationTheme.PaperMuted;
            }
            if (selected)
            {
                Image check = CreateStatusIcon(
                    "Selected Check",
                    button.transform,
                    InvestigationStatusIconLibrary.Check,
                    InvestigationTheme.PaperSelectedBorder);
                Anchor(check.rectTransform, 1f, 0.5f, 1f, 0.5f, -32f, -11f, -10f, 11f);
            }
        }
    }
}
