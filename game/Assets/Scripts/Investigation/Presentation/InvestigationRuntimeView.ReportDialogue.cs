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
        private ReportGuidanceSection reportDialogueSection;
        private bool reportReturnToReview;
        private bool reportDialogueFocusPending;
        private int reportHintLevel = 1;
        private string reportDialogueReply = string.Empty;

        private bool CanWriteFinalReport()
        {
            return state != null && state.Phase == InvestigationPhase.Report
                && state.ConclusionStatus != InvestigationConclusionStatus.Correct
                && state.ConfirmationReviewed
                && new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state).RequiredObjectivesComplete;
        }

        private void PrepareReportDialogue()
        {
            if (!CanWriteFinalReport()) return;
            if (reportFeedbackFocusPending && reportDiagnosticSection != ReportGuidanceSection.None)
            {
                reportReturnToReview |= reportDialogueSection == ReportGuidanceSection.Review;
                SetReportDialogueSection(reportDiagnosticSection, string.Empty);
            }
            else if (reportDialogueSection == ReportGuidanceSection.None)
            {
                ReportGuidanceSection first = NextReportSection();
                SetReportDialogueSection(first == ReportGuidanceSection.None ? ReportGuidanceSection.Review : first, string.Empty);
            }
        }

        private void SetReportDialogueSection(ReportGuidanceSection section, string reply)
        {
            if (reportDialogueSection != section) reportHintLevel = 1;
            // Advancing or locating an incomplete answer must reveal the question.
            notebookDrawerOpen = false;
            notebookFocusTargetAfterRender = string.Empty;
            reportDialogueSection = section;
            reportDialogueReply = reply;
            reportDialogueFocusPending = true;
        }

        private bool IsReportAnswerComplete(ReportGuidanceSection section)
        {
            InvestigationReadiness readiness = new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
            switch (section)
            {
                case ReportGuidanceSection.Cause: return readiness.FinalCauseSelected;
                case ReportGuidanceSection.Reasoning: return readiness.ReasoningComplete;
                case ReportGuidanceSection.Evidence:
                    return readiness.EvidenceComplete && readiness.ConfirmationEvidenceIncluded && readiness.EvidenceCategoriesComplete;
                case ReportGuidanceSection.Limitation: return readiness.LimitationComplete;
                default: return readiness.CanSubmitFinal;
            }
        }

        private void AnswerReportQuestion(ReportGuidanceSection section, Action choose)
        {
            if (!CanWriteFinalReport() || reportDialogueSection != section) return;
            reportDialogueReply = string.Empty;
            choose();
            if (!CanWriteFinalReport()) return;
            ContinueReportDialogue();
        }

        private void ContinueReportDialogue()
        {
            if (!CanWriteFinalReport() || reportDialogueSection == ReportGuidanceSection.Review) return;
            if (!IsReportAnswerComplete(reportDialogueSection))
            {
                // Reuse the domain's diagnostic check; incomplete drafts do not
                // count as submissions or publish a result.
                submitFinal?.Invoke();
                return;
            }

            string reply = ReportAnswerAcknowledgement(reportDialogueSection);
            ReportGuidanceSection next = reportReturnToReview
                ? ReportGuidanceSection.Review
                : (ReportGuidanceSection)((int)reportDialogueSection + 1);
            reportReturnToReview = false;
            SetReportDialogueSection(next, reply);
            RefreshPresentationOnly();
        }

        private string ReportAnswerAcknowledgement(ReportGuidanceSection section)
        {
            switch (section)
            {
                case ReportGuidanceSection.Cause:
                    return $"You chose {caseDefinition.FindThreat(state.FinalThreatId)?.DisplayName}. That is your explanation to test.";
                case ReportGuidanceSection.Reasoning:
                    return "Your food-web explanation is recorded.";
                case ReportGuidanceSection.Evidence:
                    return $"You selected {state.SelectedReportEvidenceIds.Count} supporting findings, including the ROV follow-up.";
                case ReportGuidanceSection.Limitation:
                    return "You have included a scientific limitation. Your draft is ready to review.";
                default: return string.Empty;
            }
        }

        private void PreviousReportQuestion()
        {
            if (!CanWriteFinalReport()) return;
            ReportGuidanceSection previous = reportReturnToReview
                ? ReportGuidanceSection.Review
                : (ReportGuidanceSection)Mathf.Max((int)ReportGuidanceSection.Cause, (int)reportDialogueSection - 1);
            reportReturnToReview = false;
            SetReportDialogueSection(previous, "Your previous answers are still saved. You can keep or change them.");
            RefreshPresentationOnly();
        }

        private void EditReportAnswer(ReportGuidanceSection section)
        {
            if (!CanWriteFinalReport()) return;
            reportReturnToReview = true;
            SetReportDialogueSection(section, "Let's revisit this part of your explanation.");
            RefreshPresentationOnly();
        }

        private void RenderReportDialogue(Transform paper)
        {
            RectTransform speaker = CreatePanel("Report Dialogue Speaker", paper, Color.clear, 0f);
            HorizontalLayoutGroup row = speaker.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            Image icon = CreateStatusIcon("Report Edna Icon", speaker, InvestigationScenarioIconLibrary.Investigate, InvestigationTheme.PaperSelectedBorder);
            LayoutElement iconSize = AddLayout(icon.rectTransform, 28f, 0f);
            iconSize.minWidth = iconSize.preferredWidth = 28f;
            Text progress = CreateText("Report Dialogue Progress", speaker,
                reportDialogueSection == ReportGuidanceSection.Review
                    ? "EDNA · REVIEW YOUR REPORT"
                    : $"EDNA · QUESTION {(int)reportDialogueSection} OF 4",
                12, FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.DataFont);
            LayoutElement progressSize = AddLayout(progress.rectTransform, 44f, 1f);
            progressSize.minWidth = 130f;
            if (reportDialogueSection != ReportGuidanceSection.Cause || reportReturnToReview)
            {
                Button back = CreateButton("Previous Report Question", speaker,
                    reportReturnToReview ? "Back to review" : "Previous", ButtonVisualStyle.PaperChoice, PreviousReportQuestion, out _);
                SetDialogueControlSize(back, reportReturnToReview ? 120f : 88f);
            }
            if (state.Difficulty == InvestigationDifficulty.Easy && reportDialogueSection != ReportGuidanceSection.Review)
            {
                Button help = CreateButton("Increase Edna Hint", speaker, reportHintLevel < 3 ? "More help" : "Less help",
                    ButtonVisualStyle.PaperChoice, () =>
                    {
                        reportHintLevel = reportHintLevel < 3 ? reportHintLevel + 1 : 1;
                        RefreshPresentationOnly();
                    }, out Text helpLabel);
                SetDialogueControlSize(help, 104f);
                StyleGuideControl(help, helpLabel, true, true);
            }

            Text reply = CreateText("Edna Prompt", paper, ReportDialoguePrompt(), 15, FontStyle.Normal,
                InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(reply);
            switch (reportDialogueSection)
            {
                case ReportGuidanceSection.Cause: CreateReportCauseSection(paper); break;
                case ReportGuidanceSection.Reasoning: CreateReportReasoningSection(paper); break;
                case ReportGuidanceSection.Evidence: CreateReportEvidenceSection(paper); break;
                case ReportGuidanceSection.Limitation: CreateReportLimitationSection(paper); break;
                case ReportGuidanceSection.Review: RenderReportReview(paper); break;
            }
        }

        private static void SetDialogueControlSize(Button button, float width)
        {
            LayoutElement size = button.GetComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = width;
            size.minHeight = size.preferredHeight = 44f;
            button.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
        }

        private string ReportDialoguePrompt()
        {
            if (state.ConclusionStatus == InvestigationConclusionStatus.Incorrect)
                return "Let's reconsider the explanation using the evidence you collected.";
            if (reportDiagnosticSection == reportDialogueSection && !string.IsNullOrEmpty(reportDiagnosticMessage))
                return "This answer needs another look. Your other answers are still saved.";
            if (reportDialogueSection == ReportGuidanceSection.Review)
                return "Review your explanation below. Edit any answer, then send the report when you are ready.";
            int helpLevel = state.Difficulty == InvestigationDifficulty.Easy ? reportHintLevel : 1;
            if (helpLevel > 1)
            {
                switch (reportDialogueSection)
                {
                    case ReportGuidanceSection.Cause:
                        return helpLevel == 2 ? "Compare each cause with both the food-web pattern and the ROV findings."
                            : "Open your Notebook if needed. Choose the cause that explains the full set of findings.";
                    case ReportGuidanceSection.Reasoning:
                        return helpLevel == 2 ? "Think about what happens to prey when a predator becomes less common."
                            : "Connect all three changes: fewer sharks, more tuna, and fewer krill.";
                    case ReportGuidanceSection.Evidence:
                        return $"Select at least {caseDefinition.MinimumReportEvidence} findings. Use the category counts below to include food-web, benthic and ROV evidence.";
                    case ReportGuidanceSection.Limitation:
                        return "Consider what the survey can show, and what it cannot prove about abundance or the cause of change.";
                }
            }
            if (!string.IsNullOrEmpty(reportDialogueReply)) return reportDialogueReply;
            switch (reportDialogueSection)
            {
                case ReportGuidanceSection.Cause: return "I'm Edna. Let's build your explanation one answer at a time.";
                case ReportGuidanceSection.Reasoning: return "Tell me how your proposed cause connects the food-web changes.";
                case ReportGuidanceSection.Evidence: return "Choose your evidence, then continue when your selection is ready.";
                case ReportGuidanceSection.Limitation: return "A good scientific report also says what remains uncertain.";
                default: return string.Empty;
            }
        }

        private void RenderReportReview(Transform paper)
        {
            RectTransform review = CreatePanel("Report Review", paper, Color.clear, 0f);
            VerticalLayoutGroup layout = review.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            CreateReportReviewRow(review, ReportGuidanceSection.Cause, "Your explanation",
                caseDefinition.FindThreat(state.FinalThreatId)?.DisplayName ?? "No cause selected");
            CreateReportReviewRow(review, ReportGuidanceSection.Reasoning, "How it happened",
                caseDefinition.FindReasoning(state.SelectedReasoningId)?.DisplayName ?? "No explanation selected");
            var findings = new List<string>();
            foreach (string id in state.SelectedReportEvidenceIds)
            {
                InvestigationObservationDefinition observation = caseDefinition.FindObservation(id);
                if (observation != null) findings.Add("• " + observation.DisplayName);
            }
            CreateReportReviewRow(review, ReportGuidanceSection.Evidence, "Your supporting evidence",
                findings.Count == 0 ? "No findings selected" : string.Join("\n", findings));
            CreateReportReviewRow(review, ReportGuidanceSection.Limitation, "What remains uncertain",
                caseDefinition.FindLimitation(state.SelectedLimitationId)?.DisplayName ?? "No limitation selected");
        }

        private void CreateReportReviewRow(Transform parent, ReportGuidanceSection section, string heading, string answer)
        {
            RectTransform row = CreatePanel($"Report Review {section}", parent, InvestigationTheme.PaperRaised, InvestigationTheme.SmallRadius);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            RectTransform textColumn = CreatePanel("Answer", row, Color.clear, 0f);
            textColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childControlWidth = textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;
            Text title = CreateText("Answer Heading", textColumn, heading, 12, FontStyle.Bold,
                InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.DataFont);
            ConfigureContentDrivenText(title);
            Text value = CreateText("Answer Text", textColumn, answer, 16, FontStyle.Normal,
                InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(value);
            Button edit = CreateButton($"Edit Report {section}", row, "Edit", ButtonVisualStyle.PaperChoice,
                () => EditReportAnswer(section), out _);
            SetDialogueControlSize(edit, 72f);
        }

        private IEnumerator FocusReportDialogueNextFrame()
        {
            yield return null;
            if (!CanWriteFinalReport() || notebookDrawerOpen) yield break;
            Button target = reportDialogueSection == ReportGuidanceSection.Review
                ? FindInteractableButton("Submit Final Report")
                : FindFirstInteractableButton(FindNamedRect(contentRoot, ReportSectionName(reportDialogueSection)));
            if (target == null || EventSystem.current == null) yield break;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}
