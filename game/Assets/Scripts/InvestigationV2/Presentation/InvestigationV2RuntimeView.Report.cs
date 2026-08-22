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
            CreateHeading(
                state.ConfirmationReviewed ? "Finish your report for OceanX" : "Write your first idea",
                state.ConfirmationReviewed
                    ? "Use the ROV observations to re-check your first idea, then complete every report section."
                    : $"Your provisional explanation is {provisional?.DisplayName ?? "not selected"}. The same ROV follow-up appears for every provisional choice.");

            RenderConfirmationPanel();
            RenderReportPaper();

            Button back = CreateButton("Back To Simulator", footerLeft, "← Back to simulator", ButtonVisualStyle.Tertiary, () => setPhase?.Invoke(InvestigationV2Phase.Simulate), out _);
            back.GetComponent<LayoutElement>().preferredWidth = 180f;
            Button restartButton = CreateButton("Restart V2 Case", footerLeft, "Restart case", ButtonVisualStyle.Danger, () => restart?.Invoke(), out _);
            restartButton.GetComponent<LayoutElement>().preferredWidth = 145f;

            if (state.ConclusionStatus == InvestigationV2ConclusionStatus.Correct)
            {
                CreateButton("Restart Completed Case", footerRight, "Investigate again", ButtonVisualStyle.Primary, () => restart?.Invoke(), out _);
            }
            else if (!state.ConfirmationReviewed)
            {
                CreateButton("Review ROV Follow-up", footerRight, "Review ROV follow-up", ButtonVisualStyle.Primary, () => reviewConfirmation?.Invoke(), out _);
            }
            else
            {
                InvestigationV2Readiness readiness = new InvestigationV2ConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
                Button submit = CreateButton(
                    "Submit Final Report",
                    footerRight,
                    readiness.CanSubmitFinal ? "Send my report" : "Complete every report section",
                    ButtonVisualStyle.Primary,
                    () => submitFinal?.Invoke(),
                    out _);
                submit.interactable = readiness.CanSubmitFinal;
            }
        }

        private void RenderConfirmationPanel()
        {
            RectTransform confirmation = CreateSection(
                "ROV Confirmation",
                contentRoot,
                InvestigationV2Theme.SurfaceQuiet,
                state.ConfirmationReviewed ? 180f : 112f,
                InvestigationV2Theme.SmallRadius);
            EnsureOutline(confirmation.gameObject, InvestigationV2Theme.BorderSoft, new Vector2(1f, -1f));
            AddPanelAccent(confirmation, state.ConfirmationReviewed ? InvestigationV2Theme.Success : InvestigationV2Theme.Primary);
            Text title = CreateText(
                "ROV Title",
                confirmation,
                state.ConfirmationReviewed ? "ROV follow-up reviewed" : "ROV follow-up sealed",
                19,
                FontStyle.Bold,
                InvestigationV2Theme.TextPrimary,
                TextAnchor.UpperLeft,
                InvestigationV2Theme.DisplayFont);
            Anchor(title.rectTransform, 0f, 0.64f, 1f, 1f, 16f, 0f, -12f, -12f);
            Text detail = CreateText(
                "ROV Detail",
                confirmation,
                state.ConfirmationReviewed
                    ? "The follow-up found the same two observations regardless of your provisional explanation."
                    : "Submit a provisional explanation first. Reviewing the ROV footage will not change based on which cause you selected.",
                14,
                FontStyle.Normal,
                InvestigationV2Theme.TextSecondary,
                TextAnchor.UpperLeft,
                InvestigationV2Theme.BodyFont);
            Anchor(detail.rectTransform, 0f, state.ConfirmationReviewed ? 0.38f : 0f, 1f, 0.68f, 16f, 0f, -12f, -4f);

            if (!state.ConfirmationReviewed) return;

            RectTransform evidenceRow = CreatePanel("ROV Evidence", confirmation, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(evidenceRow, 0f, 0f, 1f, 0.40f, 12f, 8f, -12f, 0f);
            HorizontalLayoutGroup layout = evidenceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            CreateConfirmationItem(evidenceRow, "E07_FISHING_LINE", InvestigationV2Glyph.FishingLine);
            CreateConfirmationItem(evidenceRow, "E08_SEAFLOOR_INTACT", InvestigationV2Glyph.Seafloor);
        }

        private void CreateConfirmationItem(Transform parent, string evidenceId, InvestigationV2Glyph glyphKind)
        {
            InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
            if (observation == null) return;
            RectTransform item = CreatePanel($"Confirmation {evidenceId}", parent, InvestigationV2Theme.Surface, InvestigationV2Theme.SmallRadius);
            InvestigationV2GlyphGraphic glyph = CreateGraphic<InvestigationV2GlyphGraphic>("Confirmation Icon", item);
            Anchor(glyph.rectTransform, 0f, 0f, 0.20f, 1f, 8f, 8f, -4f, -8f);
            glyph.SetGlyph(glyphKind);
            glyph.color = InvestigationV2Theme.Primary;
            Text label = CreateText("Observation", item, observation.DisplayName, 14, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
            Anchor(label.rectTransform, 0.20f, 0f, 1f, 1f, 4f, 4f, -8f, -4f);
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
                : $" · Final attempts: {state.FinalSubmissionAttemptCount}";
            Text metadata = CreateText("Report Metadata", paper, $"Researcher: You · Site: Seamount A · Survey 12{attemptText}", 13, FontStyle.Normal, InvestigationV2Theme.PaperMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            AddLayout(metadata.rectTransform, 28f, 1f);

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

            if (state.ConclusionStatus != InvestigationV2ConclusionStatus.NotSubmitted)
            {
                Color feedbackColor = state.ConclusionStatus == InvestigationV2ConclusionStatus.Correct
                    ? new Color32(215, 245, 229, 255)
                    : new Color32(255, 229, 225, 255);
                RectTransform feedback = CreatePanel("Report Outcome", paper, feedbackColor, InvestigationV2Theme.SmallRadius);
                AddLayout(feedback, 74f, 1f);
                Text outcome = CreateText(
                    "Outcome Text",
                    feedback,
                    statusMessage,
                    15,
                    FontStyle.Bold,
                    InvestigationV2Theme.PaperInk,
                    TextAnchor.MiddleLeft,
                    InvestigationV2Theme.BodyFont);
                Stretch(outcome.rectTransform, 14f, 8f, -14f, -8f);
            }
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
            RectTransform section = CreatePaperSlot(parent, "What do I think happened here?", 198f);
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
            AddLayout(provisionalNote.rectTransform, 36f, 1f);
            RectTransform grid = new GameObject("Cause Choices", typeof(RectTransform), typeof(InvestigationV2ResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(section, false);
            AddLayout(grid, 94f, 1f);
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
            RectTransform section = CreatePaperSlot(parent, "What did I find that shows this?", 236f);
            RectTransform grid = new GameObject("Evidence Choices", typeof(RectTransform), typeof(InvestigationV2ResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(section, false);
            AddLayout(grid, 176f, 1f);
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
                StylePaperChoice(button, selected);
            }
        }

        private void CreateReportReasoningSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "How did that cause these changes?", 144f);
            RectTransform row = CreatePanel("Reasoning Choices", section, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(row, 84f, 1f);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
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
                out _);
            StylePaperChoice(button, string.Equals(state.SelectedReasoningId, reasoningId, StringComparison.Ordinal));
        }

        private void CreateReportLimitationSection(Transform parent)
        {
            RectTransform section = CreatePaperSlot(parent, "What am I still not sure about?", 132f);
            RectTransform row = CreatePanel("Limitation Choices", section, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(row, 72f, 1f);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
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
                    out _);
                StylePaperChoice(button, string.Equals(state.SelectedLimitationId, limitation.LimitationId, StringComparison.Ordinal));
            }
        }

        private RectTransform CreatePaperSlot(Transform parent, string question, float preferredHeight)
        {
            RectTransform section = CreatePanel(question, parent, new Color32(255, 255, 255, 148), InvestigationV2Theme.SmallRadius);
            AddLayout(section, preferredHeight, 1f);
            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text label = CreateText("Question", section, question, 16, FontStyle.Bold, InvestigationV2Theme.PaperInk, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
            AddLayout(label.rectTransform, 36f, 1f);
            return section;
        }

        private static void StylePaperChoice(Button button, bool selected)
        {
            Image image = button.GetComponent<Image>();
            image.color = selected ? new Color32(217, 236, 243, 255) : InvestigationV2Theme.PaperRaised;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) text.color = InvestigationV2Theme.PaperInk;
            Outline outline = button.GetComponent<Outline>();
            if (selected && outline == null) outline = button.gameObject.AddComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = selected ? new Color32(27, 109, 138, 255) : new Color(0f, 0f, 0f, 0f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
        }
    }
}
