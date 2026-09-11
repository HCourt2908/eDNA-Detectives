using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private float comparisonNotebookScrollOffset;
        private bool ComparisonWorkspaceVisible => state != null && state.Phase == InvestigationPhase.Observe && !ObserveArrivalActive;
        private bool ComparisonNotebookVisible => ComparisonWorkspaceVisible && !observeMapOpen;

        private void SaveComparisonNotebookScroll()
        {
            if (!hasRenderedPhase) return;
            ScrollRect scroll = FindActiveScrollRect(contentRoot, "Comparison Notebook Scroll")
                ?? FindActiveScrollRect(comparisonBriefingOverlay, "Comparison Notebook Scroll");
            if (scroll != null) comparisonNotebookScrollOffset = Mathf.Max(0f, scroll.content.anchoredPosition.y);
        }
        private void RestoreComparisonNotebookScroll()
        {
            ScrollRect scroll = FindActiveScrollRect(contentRoot, "Comparison Notebook Scroll");
            if (scroll == null) return;
            Canvas.ForceUpdateCanvases();
            float maxOffset = Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height);
            Vector2 position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(comparisonNotebookScrollOffset, 0f, maxOffset);
            scroll.StopMovement();
            scroll.content.anchoredPosition = position;
        }

        private void SetComparisonReferenceView(bool seamount)
        {
            if (ComparisonBriefingActive) return;
            notebookDrawerOpen = false;
            notebookIntroductionVisible = false;
            observeMapOpen = seamount;
            navigationRevealTarget = "Toggle Comparison View";
            navigationRevealAtTop = false;
            RefreshPresentationOnly();
        }

        private void ReturnToComparisonSeamount() => SetComparisonReferenceView(true);

        private void CreateComparisonViewSwitch(RectTransform panel)
        {
            if (ComparisonBriefingActive) return;
            Button button = CreateButton("Toggle Comparison View", panel, observeMapOpen ? "Notebook view" : "Seamount view",
                ButtonVisualStyle.PaperChoice, () => SetComparisonReferenceView(!observeMapOpen), out Text label);
            label.fontSize = 13;
            button.GetComponent<LayoutElement>().ignoreLayout = true;
            Anchor(button.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -168f, -52f, -12f, -8f);
        }

        private void RenderComparisonSeamount(Transform parent)
        {
            RectTransform panel = CreatePanel("Comparison Seamount", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            Text title = CreateText("Comparison Seamount Title", panel, "SEAMOUNT", 16, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(title.rectTransform, 0f, 1f, 1f, 1f, 14f, -52f, -175f, -8f);
            CreateComparisonViewSwitch(panel);
            RectTransform map = CreatePanel("Comparison Seamount Content", panel, Color.clear, 0f);
            Anchor(map, 0f, 0f, 1f, 1f, 12f, 12f, -12f, -62f);
            RenderSurveyLens(map);
        }

        private void RenderComparisonNotebook(Transform parent)
        {
            RectTransform paper = CreatePanel("Comparison Notebook", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            CreateNotebookBinding(paper);
            Text title = CreateText("Comparison Notebook Title", paper, "MY NOTEBOOK", 16, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(title.rectTransform, 0f, 1f, 1f, 1f, 42f, -52f, -175f, -8f);
            CreateComparisonViewSwitch(paper);

            RectTransform entries = CreateNotebookEntryScroll(paper);
            RectTransform scrollRoot = FindNamedRect(paper, "Notebook Entry Scroll");
            scrollRoot.name = "Comparison Notebook Scroll";
            Anchor(scrollRoot, 0f, 0f, 1f, 1f, 40f, 12f, -14f, -62f);
            entries.GetComponent<VerticalLayoutGroup>().spacing = 8f;

            var current = RecordedSurveySpecies(SurveyEra.Current);
            var historical = RecordedSurveySpecies(SurveyEra.Historical);
            RectTransform dates = new GameObject("Comparison Notebook Dates", typeof(RectTransform), typeof(InvestigationResponsiveSplitLayout)).GetComponent<RectTransform>();
            dates.SetParent(entries, false);
            dates.GetComponent<InvestigationResponsiveSplitLayout>().Configure(.5f, 10f, 440f,
                30f + historical.Count * 40f, 30f + current.Count * 40f);
            RenderComparisonNotebookDate(dates, SurveyEra.Historical);
            RenderComparisonNotebookDate(dates, SurveyEra.Current);
            // The classified tokens show findings; keep these pages as visual survey records.
        }

        private void RenderComparisonNotebookDate(Transform parent, SurveyEra era)
        {
            RectTransform page = CreatePanel(era == SurveyEra.Current ? "Comparison Today Notes" : "Comparison Historical Notes", parent, Color.clear, 0f);
            VerticalLayoutGroup layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            Text date = CreateText(era == SurveyEra.Current ? "Today Survey Notes Title" : "Historical Survey Notes Title", page,
                era == SurveyEra.Current ? "TODAY" : "20 YEARS AGO", 14, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(date.rectTransform, 30f, 0f);
            foreach (var species in RecordedSurveySpecies(era))
            {
                RectTransform row = CreateSurveyRecordRow(page, species, era, true);
                FindNamedRect(row, "Today Notebook Result").GetComponent<Text>().text = "Detected";
                if (selectedComparisonSpecies == species.SpeciesId)
                {
                    InvestigationBorderGraphic border = CreateGraphic<InvestigationBorderGraphic>("Selected Record Border", row);
                    border.Configure(InvestigationTheme.SmallRadius, 1.5f);
                    border.color = InvestigationTheme.PaperSelectedBorder;
                    border.raycastTarget = false;
                    Stretch(border.rectTransform, 1f, 1f, -1f, -1f);
                }
            }
        }
    }
}
