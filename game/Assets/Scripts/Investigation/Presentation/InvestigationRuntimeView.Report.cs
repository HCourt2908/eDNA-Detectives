using System;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private void RenderReport()
        {
            ThreatSimulationDefinition provisional = caseDefinition.FindThreat(state.ProvisionalThreatId);
            bool caseClosed = state.ConclusionStatus == InvestigationConclusionStatus.Correct;
            InvestigationReadiness readiness = new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
            bool needsFollowUpComparison = state.ConfirmationReviewed && !readiness.RequiredObjectivesComplete;
            CreateHeading(
                caseClosed
                    ? "Case closed"
                    : needsFollowUpComparison
                        ? "Re-test the close match"
                        : state.ConfirmationReviewed ? "Finish your report for OceanX" : "Write your first idea",
                caseClosed
                    ? "Review how the survey, ecosystem model and follow-up evidence support your conclusion."
                    : needsFollowUpComparison
                        ? "Use the intact seafloor clue to compare Sea star under both fishing models, then return to the report."
                    : state.ConfirmationReviewed
                    ? "Use the ROV observations to re-check your first idea, then complete every report section."
                    : $"Your provisional explanation is {provisional?.DisplayName ?? "not selected"}. The same ROV follow-up appears for every provisional choice.");

            RenderConfirmationPanel();
            RenderReportPaper();

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

            if (!state.ConfirmationReviewed)
            {
                Button review = CreateButton("Review ROV Follow-up", footerRight, "Review ROV", ButtonVisualStyle.Primary, () => reviewConfirmation?.Invoke(), out _);
                ConfigureReportFooterButton(review, 104f);
            }
            else
            {
                if (!readiness.RequiredObjectivesComplete)
                {
                    Button retest = CreateButton("Re-test Fishing Models", footerRight, "Re-test models →", ButtonVisualStyle.Primary, () => setPhase?.Invoke(InvestigationPhase.Simulate), out _);
                    ConfigureReportFooterButton(retest, 126f);
                }
                else
                {
                    Button submit = CreateButton(
                        "Submit Final Report",
                        footerRight,
                        readiness.CanSubmitFinal ? "Send report" : "Check my report",
                        ButtonVisualStyle.Primary,
                        () => submitFinal?.Invoke(),
                        out _);
                    submit.interactable = true;
                    ConfigureReportFooterButton(submit, readiness.CanSubmitFinal ? 104f : 116f);
                }
            }
        }

        private void RenderConfirmationPanel()
        {
            RectTransform confirmation = CreateSection(
                "ROV Confirmation",
                contentRoot,
                InvestigationTheme.SurfaceQuiet,
                state.ConfirmationReviewed ? 154f : 96f,
                InvestigationTheme.SmallRadius);
            EnsureOutline(
                confirmation.gameObject,
                state.ConfirmationReviewed ? InvestigationTheme.Success : InvestigationTheme.Primary,
                new Vector2(2f, -2f));
            Text title = CreateText(
                "ROV Title",
                confirmation,
                state.ConfirmationReviewed ? "ROV follow-up reviewed" : "ROV follow-up sealed",
                19,
                FontStyle.Bold,
                InvestigationTheme.TextPrimary,
                TextAnchor.MiddleLeft,
                InvestigationTheme.DisplayFont);
            float summaryBottom = state.ConfirmationReviewed ? 0.58f : 0f;
            Anchor(title.rectTransform, 0f, summaryBottom, 0.31f, 1f, 16f, 4f, -4f, -4f);
            Text detail = CreateText(
                "ROV Detail",
                confirmation,
                state.ConfirmationReviewed
                    ? "The follow-up found the same two observations. Use them to re-test the two fishing models before finalising the report."
                    : "Submit a provisional explanation first. Reviewing the ROV footage will not change based on which cause you selected.",
                14,
                FontStyle.Normal,
                InvestigationTheme.TextSecondary,
                TextAnchor.MiddleLeft,
                InvestigationTheme.BodyFont);
            Anchor(detail.rectTransform, 0.31f, summaryBottom, 1f, 1f, 4f, 4f, -12f, -4f);

            if (!state.ConfirmationReviewed) return;

            RectTransform evidenceRow = CreatePanel("ROV Evidence", confirmation, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(evidenceRow, 0f, 0f, 1f, 0.57f, 12f, 8f, -12f, 0f);
            HorizontalLayoutGroup layout = evidenceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            CreateConfirmationItem(evidenceRow, "E07_FISHING_LINE", InvestigationEvidenceIconLibrary.FishingLine);
            CreateConfirmationItem(evidenceRow, "E08_SEAFLOOR_INTACT", InvestigationEvidenceIconLibrary.Seafloor);
        }

        private void CreateConfirmationItem(Transform parent, string evidenceId, Sprite iconSprite)
        {
            InvestigationObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
            if (observation == null) return;
            RectTransform item = CreatePanel($"Confirmation {evidenceId}", parent, InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
            Image icon = CreateStatusIcon("Confirmation Icon", item, iconSprite, InvestigationTheme.Primary);
            Anchor(icon.rectTransform, 0f, 0f, 0.18f, 1f, 10f, 10f, -4f, -10f);
            Text label = CreateText("Observation", item, observation.DisplayName, 14, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
            Anchor(label.rectTransform, 0.18f, 0f, 1f, 1f, 4f, 4f, -8f, -4f);
        }

        private void RenderReportPaper()
        {
            RectTransform paper = CreatePanel("Survey Report Paper", contentRoot, new Color32(241, 245, 241, 255), InvestigationTheme.CardRadius);
            EnsureOutline(paper.gameObject, new Color32(179, 204, 218, 100), new Vector2(1f, -1f));
            VerticalLayoutGroup layout = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 18, 18);
            layout.spacing = 12f;
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

            Text title = CreateText("Report Title", reportHeader, "Survey report", 25, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
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

            CreateReportOutcome(paper);
            if (state.ConclusionStatus == InvestigationConclusionStatus.Correct)
            {
                CreateCaseClosedSummary(paper);
                return;
            }

            RectTransform columns = new GameObject("Report Columns", typeof(RectTransform), typeof(InvestigationResponsiveSplitLayout)).GetComponent<RectTransform>();
            columns.SetParent(paper, false);
            InvestigationResponsiveSplitLayout split = columns.GetComponent<InvestigationResponsiveSplitLayout>();
            split.padding = new RectOffset(0, 0, 0, 0);
            split.Configure(0.5f, 12f, 900f, 390f, 390f);

            RectTransform leftColumn = CreateReportColumn("Report Cause And Reasoning", columns);
            RectTransform rightColumn = CreateReportColumn("Report Evidence And Limitation", columns);
            CreateReportCauseSection(leftColumn);
            CreateReportReasoningSection(leftColumn);
            CreateReportEvidenceSection(rightColumn);
            CreateReportLimitationSection(rightColumn);
            FitReportColumns(split, columns, leftColumn, rightColumn);
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
                    : statusMessage,
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
            EnsureOutline(summary.gameObject, InvestigationTheme.PaperSelectedBorder, new Vector2(2f, -2f));
            VerticalLayoutGroup layout = summary.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text title = CreateText(
                "Case Closed Title",
                summary,
                $"CASE CLOSED · {finalThreat?.DisplayName ?? "Best-supported cause recorded"}",
                22,
                FontStyle.Bold,
                InvestigationTheme.PaperInk,
                TextAnchor.MiddleLeft,
                InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(title);

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
            RectTransform row = CreatePanel($"Debrief {heading}", parent, new Color32(247, 250, 251, 255), InvestigationTheme.SmallRadius);
            EnsureOutline(row.gameObject, accent, new Vector2(2f, -2f));
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

        private static RectTransform CreateReportColumn(string name, Transform parent)
        {
            RectTransform column = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0f), 0f);
            VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        private void CreateReportCauseSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Cause Section", "What do I think happened here?");
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
            layout.Configure(4, 2, 66f, 8f);
            for (int index = 0; index < caseDefinition.Threats.Count; index++)
            {
                ThreatSimulationDefinition threat = caseDefinition.Threats[index];
                if (threat == null) continue;
                Button button = CreateButton(
                    $"Final Cause {threat.ThreatId}",
                    grid,
                    threat.DisplayName,
                    ButtonVisualStyle.PaperChoice,
                    () => setFinalThreat?.Invoke(threat.ThreatId),
                    out _);
                button.interactable = state.ConfirmationReviewed;
                StylePaperChoice(button, string.Equals(state.FinalThreatId, threat.ThreatId, StringComparison.Ordinal));
            }
        }

        private void CreateReportEvidenceSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Evidence Section", "What did I find that shows this?");
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
                    () => setReportEvidence?.Invoke(observation.EvidenceId, !state.HasSelectedEvidence(observation.EvidenceId)),
                    out Text label);
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
            RectTransform section = CreatePaperSlot(parent, "Report Reasoning Section", "How did that cause these changes?");
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
                () => setReasoning?.Invoke(reasoningId),
                out Text choiceLabel);
            ConfigureWrappingChoice(button, choiceLabel);
            StylePaperChoice(button, string.Equals(state.SelectedReasoningId, reasoningId, StringComparison.Ordinal));
        }

        private void CreateReportLimitationSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "Report Limitation Section", "What am I still not sure about?");
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
                    () => setLimitation?.Invoke(limitation.LimitationId),
                    out Text choiceLabel);
                ConfigureWrappingChoice(button, choiceLabel);
                StylePaperChoice(button, string.Equals(state.SelectedLimitationId, limitation.LimitationId, StringComparison.Ordinal));
            }
        }

        private RectTransform CreatePaperSlot(Transform parent, string sectionName, string question)
        {
            RectTransform section = CreatePanel(sectionName, parent, new Color32(255, 255, 255, 148), InvestigationTheme.SmallRadius);
            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text label = CreateText("Question", section, question, 16, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(label);
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
            InvestigationContentHeightLayoutElement contentHeight =
                button.gameObject.AddComponent<InvestigationContentHeightLayoutElement>();
            contentHeight.Configure(label, 52f, 20f, 16f);
        }

        private static void FitReportColumns(
            InvestigationResponsiveSplitLayout split,
            RectTransform columns,
            RectTransform leftColumn,
            RectTransform rightColumn)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(columns);
            LayoutRebuilder.ForceRebuildLayoutImmediate(leftColumn);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rightColumn);
            float leftHeight = Mathf.Max(220f, LayoutUtility.GetPreferredHeight(leftColumn));
            float rightHeight = Mathf.Max(220f, LayoutUtility.GetPreferredHeight(rightColumn));
            split.Configure(0.5f, 12f, 900f, leftHeight, rightHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(columns);
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
