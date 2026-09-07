using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private bool provisionalReviewOpen;
        private string provisionalCandidateId = string.Empty;
        private RectTransform provisionalReviewRoot;
        private float provisionalReviewScrollPosition = 1f;

        public void OpenProvisionalReview()
        {
            if (state == null
                || state.Phase != InvestigationPhase.Simulate
                || !string.IsNullOrEmpty(state.ProvisionalThreatId)
                || !new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state).CanEnterProvisional) return;
            provisionalCandidateId = selectedThreatId;
            provisionalReviewOpen = true;
            provisionalReviewScrollPosition = 1f;
            notebookDrawerOpen = false;
            notebookFocusTargetAfterRender = $"Provisional Cause {provisionalCandidateId}";
            RefreshPresentationOnly();
        }

        private void RemoveProvisionalReview()
        {
            if (provisionalReviewRoot == null) return;
            ScrollRect scroll = provisionalReviewRoot.GetComponent<ScrollRect>();
            if (scroll != null) provisionalReviewScrollPosition = scroll.verticalNormalizedPosition;
            provisionalReviewRoot.gameObject.SetActive(false);
            Destroy(provisionalReviewRoot.gameObject);
            provisionalReviewRoot = null;
        }

        private void RenderProvisionalReview()
        {
            if (!provisionalReviewOpen) return;
            if (state == null || state.Phase != InvestigationPhase.Simulate || !string.IsNullOrEmpty(state.ProvisionalThreatId))
            {
                provisionalReviewOpen = false;
                return;
            }
            CanvasGroup group = contentRoot.GetComponent<CanvasGroup>();
            if (group == null) group = contentRoot.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            provisionalReviewRoot = CreatePanel("Provisional Review Overlay", contentPanel, new Color(0f, 0.04f, 0.08f, 0.92f), 0f);
            Stretch(provisionalReviewRoot, 0f, 0f, 0f, 0f);
            provisionalReviewRoot.GetComponent<Image>().raycastTarget = true;
            provisionalReviewRoot.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = provisionalReviewRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.viewport = provisionalReviewRoot;
            RectTransform paper = CreatePanel("Provisional Review", provisionalReviewRoot, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            paper.anchorMin = new Vector2(0.06f, 1f);
            paper.anchorMax = new Vector2(0.94f, 1f);
            paper.pivot = new Vector2(0.5f, 1f);
            paper.anchoredPosition = new Vector2(0f, -12f);
            paper.sizeDelta = Vector2.zero;
            paper.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = paper;
            VerticalLayoutGroup layout = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text title = CreateText("Provisional Review Title", paper, "Which explanation do you currently support?", 20,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.DisplayFont);
            ConfigureContentDrivenText(title);
            Text detail = CreateText("Provisional Review Detail", paper,
                "This saves your first idea. You can choose a different final cause after reviewing the same ROV follow-up.",
                14, FontStyle.Normal, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(detail);
            RectTransform choices = new GameObject("Provisional Cause Choices", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            choices.SetParent(paper, false);
            choices.GetComponent<InvestigationResponsiveGridLayout>().Configure(2, 2, 52f, 8f);
            foreach (ThreatSimulationDefinition threat in caseDefinition.Threats)
            {
                Button choice = CreateButton($"Provisional Cause {threat.ThreatId}", choices, threat.DisplayName,
                    ButtonVisualStyle.PaperChoice, () =>
                    {
                        provisionalCandidateId = threat.ThreatId;
                        RefreshPresentationOnly();
                    }, out _);
                StylePaperChoice(choice, provisionalCandidateId == threat.ThreatId);
                choice.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
            }
            ThreatSimulationDefinition candidate = caseDefinition.FindThreat(provisionalCandidateId);
            InvestigationHypothesisSummary summary = new InvestigationHypothesisSummary(caseDefinition, state, provisionalCandidateId);
            Text selected = CreateText("Provisional Selected Summary", paper,
                $"Your first idea: {candidate?.DisplayName ?? "Choose an explanation"}\nYour checks: {summary.SupportCount} support · {summary.ChallengeCount} challenge · {summary.OpenCount} open",
                15, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(selected);
            Button confirm = CreateButton("Confirm Provisional Idea", paper, "Save this first idea", ButtonVisualStyle.PaperPrimary,
                () => { provisionalReviewOpen = false; submitProvisional?.Invoke(provisionalCandidateId); }, out _);
            confirm.interactable = candidate != null;
            Button cancel = CreateButton("Cancel Provisional Idea", paper, "Back to comparisons", ButtonVisualStyle.PaperChoice,
                () =>
                {
                    provisionalReviewOpen = false;
                    notebookFocusTargetAfterRender = "Write Provisional Report";
                    RefreshPresentationOnly();
                }, out _);
            AddLayout(confirm.GetComponent<RectTransform>(), 48f, 1f);
            AddLayout(cancel.GetComponent<RectTransform>(), 44f, 1f);
            confirm.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
            cancel.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = provisionalReviewScrollPosition;
            provisionalReviewRoot.SetAsLastSibling();
        }
    }
}
