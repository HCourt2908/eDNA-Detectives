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
            bool caseClosed = state.ConclusionStatus == InvestigationConclusionStatus.Correct;
            Text heading = CreateHeading(caseClosed ? "Case closed" : "Close the case with EDNA",
                caseClosed ? "Your report connects the survey, models and ROV findings."
                : "Look at the clues · Weigh our explanation · Send our report");
            if (!caseClosed)
            {
                if (!state.ConfirmationReviewed && !string.IsNullOrEmpty(rovScanId)) RenderWorkbenchRovScan();
                else RenderReportConversation();
                if (state.ConfirmationReviewed && reportConversationRound == ReportConversationRound.Clues)
                    RenderConfirmationPanel();
            }
            if (caseClosed || (state.ConfirmationReviewed && reportConversationRound == ReportConversationRound.Review)) RenderReportPaper();
            if (caseClosed)
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
            Button restartButton = CreateButton("Restart Case", footerLeft, "Restart case", ButtonVisualStyle.Danger, RequestRestartConfirmation, out _);
            ConfigureReportFooterButton(restartButton, 100f);
            CreateNotebookDrawerButton(footerRight);
        }

        private void RenderConfirmationPanel()
        {
            RectTransform panel = CreatePanel("ROV Confirmation", contentRoot, InvestigationTheme.SurfaceQuiet, InvestigationTheme.SmallRadius);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text title = CreateText("ROV Title", panel, state.ConfirmationReviewed ? "The ROV found…" : "ROV field notes are ready", 20, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(title);
            Text detail = CreateText("ROV Detail", panel, state.ConfirmationReviewed
                ? "Fishing line adds a physical clue; the intact seafloor adds context to your Sea star finding."
                : "Open the findings to see what the camera recorded. Your model comparisons are already saved.", 14, FontStyle.Bold, InvestigationTheme.TextSecondary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(detail);
            RectTransform row = new GameObject("ROV Evidence", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            row.SetParent(panel, false);
            row.GetComponent<InvestigationResponsiveGridLayout>().Configure(2, 1, 190f, 12f);
            string first = reportRovFocus == "E08_SEAFLOOR_INTACT" ? "E08_SEAFLOOR_INTACT" : "E07_FISHING_LINE";
            string second = first == "E07_FISHING_LINE" ? "E08_SEAFLOOR_INTACT" : "E07_FISHING_LINE";
            CreateConfirmationItem(row, first, first == "E07_FISHING_LINE" ? InvestigationEvidenceIconLibrary.FishingLine : InvestigationEvidenceIconLibrary.Seafloor);
            CreateConfirmationItem(row, second, second == "E07_FISHING_LINE" ? InvestigationEvidenceIconLibrary.FishingLine : InvestigationEvidenceIconLibrary.Seafloor);
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
                14, FontStyle.Bold, InvestigationTheme.TextSecondary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            Anchor(details.rectTransform, 0f, 0f, 1f, 1f, 20f, 42f, -20f, -94f);
            Text added = CreateText("Notebook Added", item, "ADDED TO NOTEBOOK · ROV", 11,
                FontStyle.Bold, InvestigationTheme.Success, TextAnchor.MiddleLeft, InvestigationTheme.DataFont);
            Anchor(added.rectTransform, 0f, 0f, 1f, 0f, 20f, 14f, -20f, 34f);
        }

        private IEnumerator AnimateRovEvidenceReveal()
        {
            bool seafloorFirst = reportRovFocus == "E08_SEAFLOOR_INTACT";
            RectTransform fishingLine = FindNamedRect(contentRoot, seafloorFirst ? "Confirmation E08_SEAFLOOR_INTACT" : "Confirmation E07_FISHING_LINE");
            RectTransform seafloor = FindNamedRect(contentRoot, seafloorFirst ? "Confirmation E07_FISHING_LINE" : "Confirmation E08_SEAFLOOR_INTACT");
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

            string reportTitle = state.ConclusionStatus == InvestigationConclusionStatus.Correct ? "Survey report" : "Your report";
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

            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct)
            {
                CreateReportOutcome(paper);
                CreateCaseClosedSummary(paper);
                return;
            }

            RenderReportReview(paper);
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
                    feedbackColor = InvestigationTheme.ReportGuide;
                    feedbackIcon = InvestigationStatusIconLibrary.Question;
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

            if (reportKeyClue != ReportKeyClue.None)
                CreateDebriefRow(summary, "YOUR KEY CLUE", ReportKeyClueLabel(reportKeyClue),
                    ReportKeyClueResponse(), InvestigationTheme.Primary);
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
            Text label = CreateText("Section Heading", section, question, 16, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
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
