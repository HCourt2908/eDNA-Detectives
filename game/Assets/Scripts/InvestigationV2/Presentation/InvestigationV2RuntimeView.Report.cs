using System;
using EDNA.Investigation.V2.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    public sealed partial class InvestigationV2RuntimeView
    {
        private void RenderReport()
        {
            ThreatSimulationDefinition provisional = caseDefinition.FindThreat(state.ProvisionalThreatId);
            bool caseClosed = state.ConclusionStatus == InvestigationV2ConclusionStatus.Correct;
            CreateHeading(
                caseClosed ? "Case closed" : state.ConfirmationReviewed ? "Finish your report for OceanX" : "Write your first idea",
                caseClosed
                    ? "Review how the survey, ecosystem model and follow-up evidence support your conclusion."
                    : state.ConfirmationReviewed
                    ? "Use the ROV observations to re-check your first idea, then complete every report section."
                    : $"Your provisional explanation is {provisional?.DisplayName ?? "not selected"}. The same ROV follow-up appears for every provisional choice.");

            RenderConfirmationPanel();
            RenderReportPaper();

            if (state.ConclusionStatus == InvestigationV2ConclusionStatus.Correct)
            {
                Button again = CreateButton("Restart Completed Case", footerRight, "Investigate again", ButtonVisualStyle.Primary, () => restart?.Invoke(), out _);
                ConfigureReportFooterButton(again, 116f);
                return;
            }

            if (restartConfirmationPending)
            {
                Button cancel = CreateButton("Cancel Restart V2 Case", footerLeft, "Cancel", ButtonVisualStyle.Tertiary, CancelRestartConfirmation, out _);
                ConfigureReportFooterButton(cancel, 80f);
                Button confirm = CreateButton("Confirm Restart V2 Case", footerRight, "Confirm restart", ButtonVisualStyle.Danger, ConfirmRestart, out _);
                ConfigureReportFooterButton(confirm, 116f);
                return;
            }

            Button back = CreateButton("Back To Simulator", footerLeft, "← Back to simulator", ButtonVisualStyle.Tertiary, () => setPhase?.Invoke(InvestigationV2Phase.Simulate), out _);
            ConfigureReportFooterButton(back, 126f);
            Button restartButton = CreateButton("Restart V2 Case", footerLeft, "Restart case", ButtonVisualStyle.Danger, RequestRestartConfirmation, out _);
            ConfigureReportFooterButton(restartButton, 100f);

            if (!state.ConfirmationReviewed)
            {
                Button review = CreateButton("Review ROV Follow-up", footerRight, "Review ROV", ButtonVisualStyle.Primary, () => reviewConfirmation?.Invoke(), out _);
                ConfigureReportFooterButton(review, 104f);
            }
            else
            {
                InvestigationV2Readiness readiness = new InvestigationV2ConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
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

        private void RenderConfirmationPanel()
        {
            RectTransform confirmation = CreateSection(
                "ROV Confirmation",
                contentRoot,
                InvestigationV2Theme.SurfaceQuiet,
                state.ConfirmationReviewed ? 154f : 96f,
                InvestigationV2Theme.SmallRadius);
            EnsureOutline(
                confirmation.gameObject,
                state.ConfirmationReviewed ? InvestigationV2Theme.Success : InvestigationV2Theme.Primary,
                new Vector2(2f, -2f));
            Text title = CreateText(
                "ROV Title",
                confirmation,
                state.ConfirmationReviewed ? "ROV follow-up reviewed" : "ROV follow-up sealed",
                19,
                FontStyle.Bold,
                InvestigationV2Theme.TextPrimary,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.DisplayFont);
            float summaryBottom = state.ConfirmationReviewed ? 0.58f : 0f;
            Anchor(title.rectTransform, 0f, summaryBottom, 0.31f, 1f, 16f, 4f, -4f, -4f);
            Text detail = CreateText(
                "ROV Detail",
                confirmation,
                state.ConfirmationReviewed
                    ? "The follow-up found the same two observations regardless of your provisional explanation."
                    : "Submit a provisional explanation first. Reviewing the ROV footage will not change based on which cause you selected.",
                14,
                FontStyle.Normal,
                InvestigationV2Theme.TextSecondary,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.BodyFont);
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
            CreateConfirmationItem(evidenceRow, "E07_FISHING_LINE", InvestigationV2EvidenceIconLibrary.FishingLine);
            CreateConfirmationItem(evidenceRow, "E08_SEAFLOOR_INTACT", InvestigationV2EvidenceIconLibrary.Seafloor);
        }

        private void CreateConfirmationItem(Transform parent, string evidenceId, Sprite iconSprite)
        {
            InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
            if (observation == null) return;
            RectTransform item = CreatePanel($"Confirmation {evidenceId}", parent, InvestigationV2Theme.Surface, InvestigationV2Theme.SmallRadius);
            Image icon = CreateStatusIcon("Confirmation Icon", item, iconSprite, InvestigationV2Theme.Primary);
            Anchor(icon.rectTransform, 0f, 0f, 0.18f, 1f, 10f, 10f, -4f, -10f);
            Text label = CreateText("Observation", item, observation.DisplayName, 14, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
            Anchor(label.rectTransform, 0.18f, 0f, 1f, 1f, 4f, 4f, -8f, -4f);
        }

        private void RenderReportPaper()
        {
            RectTransform paper = CreatePanel("Survey Report Paper", contentRoot, new Color32(241, 245, 241, 255), InvestigationV2Theme.CardRadius);
            EnsureOutline(paper.gameObject, new Color32(179, 204, 218, 100), new Vector2(1f, -1f));
            VerticalLayoutGroup layout = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 18, 18);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text title = CreateText("Report Title", paper, "Survey report", 25, FontStyle.Bold, InvestigationV2Theme.PaperInk, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
            AddLayout(title.rectTransform, 44f, 1f);
            string attemptText = state.FinalSubmissionAttemptCount == 0
                ? string.Empty
                : $" · Revision {state.FinalSubmissionAttemptCount}";
            Text metadata = CreateText("Report Metadata", paper, $"Researcher: You · Site: {state.SiteDisplayName} · {state.SurveyDisplayName}{attemptText}", 13, FontStyle.Normal, InvestigationV2Theme.PaperMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            AddLayout(metadata.rectTransform, 28f, 1f);

            CreateReportOutcome(paper);
            if (state.ConclusionStatus == InvestigationV2ConclusionStatus.Correct)
            {
                CreateCaseClosedSummary(paper);
                return;
            }

            RectTransform columns = new GameObject("Report Columns", typeof(RectTransform), typeof(InvestigationV2ResponsiveSplitLayout)).GetComponent<RectTransform>();
            columns.SetParent(paper, false);
            InvestigationV2ResponsiveSplitLayout split = columns.GetComponent<InvestigationV2ResponsiveSplitLayout>();
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

        private void CreateReportOutcome(Transform parent)
        {
            if (state.ConclusionStatus == InvestigationV2ConclusionStatus.NotSubmitted) return;

            Color feedbackColor;
            Sprite feedbackIcon;
            switch (state.ConclusionStatus)
            {
                case InvestigationV2ConclusionStatus.Correct:
                    feedbackColor = InvestigationV2Theme.ReportSuccess;
                    feedbackIcon = InvestigationV2StatusIconLibrary.Check;
                    break;
                case InvestigationV2ConclusionStatus.InsufficientEvidence:
                    feedbackColor = InvestigationV2Theme.ReportGuide;
                    feedbackIcon = InvestigationV2StatusIconLibrary.Question;
                    break;
                default:
                    feedbackColor = InvestigationV2Theme.ReportError;
                    feedbackIcon = InvestigationV2StatusIconLibrary.Cross;
                    break;
            }

            RectTransform feedback = CreatePanel("Report Outcome", parent, feedbackColor, InvestigationV2Theme.SmallRadius);
            HorizontalLayoutGroup feedbackLayout = feedback.gameObject.AddComponent<HorizontalLayoutGroup>();
            feedbackLayout.padding = new RectOffset(14, 14, 10, 10);
            feedbackLayout.spacing = 12f;
            feedbackLayout.childAlignment = TextAnchor.MiddleLeft;
            feedbackLayout.childControlWidth = true;
            feedbackLayout.childControlHeight = true;
            feedbackLayout.childForceExpandWidth = false;
            feedbackLayout.childForceExpandHeight = false;
            Image icon = CreateStatusIcon("Outcome Icon", feedback, feedbackIcon, InvestigationV2Theme.PaperInk);
            LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = 28f;
            iconLayout.preferredWidth = 28f;
            iconLayout.minHeight = 28f;
            iconLayout.preferredHeight = 28f;
            Text outcome = CreateText(
                "Outcome Text",
                feedback,
                state.ConclusionStatus == InvestigationV2ConclusionStatus.Correct
                    ? "Report accepted. Your evidence supports a complete explanation."
                    : statusMessage,
                15,
                FontStyle.Bold,
                InvestigationV2Theme.PaperInk,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.BodyFont);
            ConfigureContentDrivenText(outcome);
            LayoutElement outcomeLayout = outcome.gameObject.AddComponent<LayoutElement>();
            outcomeLayout.minWidth = 0f;
            outcomeLayout.flexibleWidth = 1f;
        }

        private void CreateCaseClosedSummary(Transform parent)
        {
            ThreatSimulationDefinition finalThreat = caseDefinition.FindThreat(state.FinalThreatId);
            InvestigationV2ReasoningDefinition reasoning = caseDefinition.FindReasoning(state.SelectedReasoningId);
            InvestigationV2LimitationDefinition limitation = caseDefinition.FindLimitation(state.SelectedLimitationId);

            RectTransform summary = CreatePanel("Case Closed Summary", parent, InvestigationV2Theme.PaperRaised, InvestigationV2Theme.SmallRadius);
            EnsureOutline(summary.gameObject, InvestigationV2Theme.PaperSelectedBorder, new Vector2(2f, -2f));
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
                InvestigationV2Theme.PaperInk,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.DisplayFont);
            ConfigureContentDrivenText(title);

            Text explanation = CreateText(
                "Case Closed Explanation",
                summary,
                string.IsNullOrWhiteSpace(caseDefinition.SuccessFeedback)
                    ? "The report connects the ecosystem model to several independent kinds of evidence."
                    : caseDefinition.SuccessFeedback,
                14,
                FontStyle.Normal,
                InvestigationV2Theme.PaperInk,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.BodyFont);
            ConfigureContentDrivenText(explanation);

            CreateDebriefRow(
                summary,
                "FOOD-WEB MECHANISM",
                reasoning?.DisplayName ?? "Ecosystem mechanism recorded",
                reasoning?.Explanation ?? string.Empty,
                InvestigationV2Theme.Success);
            CreateDebriefRow(
                summary,
                "BENTHIC CHECK",
                FindEvidenceSummary(EvidenceCategory.Benthic, "Benthic evidence was included"),
                "This is the key check that separates selective fishing from a cause that damages the seafloor.",
                InvestigationV2Theme.Primary);
            CreateDebriefRow(
                summary,
                "ROV FOLLOW-UP",
                BuildConfirmationSummary(),
                "Physical observations strengthen the explanation developed from eDNA and the ecosystem model.",
                InvestigationV2Theme.Accent);
            CreateDebriefRow(
                summary,
                "SCIENTIFIC CAUTION",
                limitation?.DisplayName ?? "Uncertainty recorded",
                limitation?.Explanation ?? string.Empty,
                InvestigationV2Theme.Focus);
        }

        private static void CreateDebriefRow(
            Transform parent,
            string heading,
            string statement,
            string explanation,
            Color accent)
        {
            RectTransform row = CreatePanel($"Debrief {heading}", parent, new Color32(247, 250, 251, 255), InvestigationV2Theme.SmallRadius);
            EnsureOutline(row.gameObject, accent, new Vector2(2f, -2f));
            VerticalLayoutGroup layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 12, 8, 8);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text label = CreateText("Debrief Heading", row, heading, 11, FontStyle.Bold, InvestigationV2Theme.PaperMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            ConfigureContentDrivenText(label);
            Text value = CreateText("Debrief Statement", row, statement, 16, FontStyle.Bold, InvestigationV2Theme.PaperInk, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
            ConfigureContentDrivenText(value);
            if (string.IsNullOrWhiteSpace(explanation)) return;
            Text detail = CreateText("Debrief Explanation", row, explanation, 12, FontStyle.Normal, InvestigationV2Theme.PaperMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.BodyFont);
            ConfigureContentDrivenText(detail);
        }

        private string FindEvidenceSummary(EvidenceCategory category, string fallback)
        {
            for (int index = 0; index < state.SelectedReportEvidenceIds.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(state.SelectedReportEvidenceIds[index]);
                if (observation != null && observation.Category == category) return observation.DisplayName;
            }
            return fallback;
        }

        private string BuildConfirmationSummary()
        {
            string result = string.Empty;
            for (int index = 0; index < caseDefinition.ConfirmationEvidenceIds.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(caseDefinition.ConfirmationEvidenceIds[index]);
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
                InvestigationV2Theme.PaperMuted,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.BodyFont);
            ConfigureContentDrivenText(provisionalNote);
            RectTransform grid = new GameObject("Cause Choices", typeof(RectTransform), typeof(InvestigationV2ResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(section, false);
            InvestigationV2ResponsiveGridLayout layout = grid.GetComponent<InvestigationV2ResponsiveGridLayout>();
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
                InvestigationV2EvidenceCategoryRequirement requirement = caseDefinition.EvidenceCategoryRequirements[requirementIndex];
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
                    ? InvestigationV2Theme.PaperSelectedBorder
                    : InvestigationV2Theme.PaperMuted,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.DataFont);
            ConfigureContentDrivenText(progress);
            RectTransform grid = new GameObject("Evidence Choices", typeof(RectTransform), typeof(InvestigationV2ResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(section, false);
            InvestigationV2ResponsiveGridLayout layout = grid.GetComponent<InvestigationV2ResponsiveGridLayout>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.Configure(3, 2, 72f, 8f);
            for (int index = 0; index < state.DiscoveredObservationIds.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(state.DiscoveredObservationIds[index]);
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
                    InvestigationV2EvidenceIconLibrary.ForObservation(observation),
                    selected ? InvestigationV2Theme.PaperSelectedBorder : InvestigationV2Theme.PaperMuted);
                Anchor(icon.rectTransform, 0f, 0f, 0f, 1f, 10f, 14f, 36f, -14f);
                StylePaperChoice(button, selected);
            }
        }

        private int CountSelectedEvidenceInCategory(EvidenceCategory category)
        {
            int count = 0;
            for (int index = 0; index < state.SelectedReportEvidenceIds.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(state.SelectedReportEvidenceIds[index]);
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
                InvestigationV2ReasoningDefinition reasoning = caseDefinition.ReasoningOptions[index];
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
                InvestigationV2LimitationDefinition limitation = caseDefinition.Limitations[index];
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
            RectTransform section = CreatePanel(sectionName, parent, new Color32(255, 255, 255, 148), InvestigationV2Theme.SmallRadius);
            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text label = CreateText("Question", section, question, 16, FontStyle.Bold, InvestigationV2Theme.PaperInk, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
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
            InvestigationV2ContentHeightLayoutElement contentHeight =
                button.gameObject.AddComponent<InvestigationV2ContentHeightLayoutElement>();
            contentHeight.Configure(label, 52f, 20f, 16f);
        }

        private static void FitReportColumns(
            InvestigationV2ResponsiveSplitLayout split,
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
            statusTone = InvestigationV2StatusTone.Warning;
            RefreshPresentationOnly();
        }

        private void CancelRestartConfirmation()
        {
            restartConfirmationPending = false;
            statusMessage = string.Empty;
            statusTone = InvestigationV2StatusTone.Guide;
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
            border.color = selected ? InvestigationV2Theme.PaperSelectedBorder : InvestigationV2Theme.PaperBorder;
            Transform faceTransform = button.transform.Find("Paper Choice Face");
            Image face = faceTransform == null ? null : faceTransform.GetComponent<Image>();
            if (face != null) face.color = selected ? InvestigationV2Theme.PaperSelected : InvestigationV2Theme.PaperRaised;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = InvestigationV2Theme.PaperInk;
                text.rectTransform.offsetMax = new Vector2(selected ? -36f : -10f, text.rectTransform.offsetMax.y);
            }
            Transform evidenceIcon = button.transform.Find("Evidence Icon");
            if (evidenceIcon != null)
            {
                evidenceIcon.GetComponent<Image>().color = selected
                    ? InvestigationV2Theme.PaperSelectedBorder
                    : InvestigationV2Theme.PaperMuted;
            }
            if (selected)
            {
                Image check = CreateStatusIcon(
                    "Selected Check",
                    button.transform,
                    InvestigationV2StatusIconLibrary.Check,
                    InvestigationV2Theme.PaperSelectedBorder);
                Anchor(check.rectTransform, 1f, 0.5f, 1f, 0.5f, -32f, -11f, -10f, 11f);
            }
        }
    }
}
