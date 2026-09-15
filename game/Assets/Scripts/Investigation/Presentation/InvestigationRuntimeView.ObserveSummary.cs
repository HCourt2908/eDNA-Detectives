using TMPro;
using System.Collections;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum ObserveSummaryStep { Sorting, FadingOut, Revealing, Ready }
        private ObserveSummaryStep observeSummaryStep;
        private bool ObserveSummaryVisible => observeSummaryStep >= ObserveSummaryStep.Revealing;
        private bool ObserveSummaryTransitioning => observeSummaryStep == ObserveSummaryStep.FadingOut || observeSummaryStep == ObserveSummaryStep.Revealing;
        private bool observeSummarySaved;
        private bool observeSummarySaving;
        private RectTransform observeSummaryFlight;
        private Coroutine observeSummaryAnimation;
        private bool ObserveComplete => state != null && InvestigationObserveEvaluator.IsComplete(caseDefinition, state);

        private void ResetObserveSummary()
        {
            RemoveObserveSummaryFlight();
            observeSummarySaved = observeSummarySaving = false;
            observeSummaryStep = ObserveSummaryStep.Sorting;
        }

        private void RemoveObserveSummaryFlight()
        {
            if (observeSummaryAnimation != null) StopCoroutine(observeSummaryAnimation);
            observeSummaryAnimation = null;
            if (observeSummaryFlight == null) return;
            observeSummaryFlight.gameObject.SetActive(false);
            Destroy(observeSummaryFlight.gameObject);
            observeSummaryFlight = null;
        }

        private void RequestInvestigationPhase(InvestigationPhase phase)
        {
            if (state.Phase == InvestigationPhase.Observe && phase == InvestigationPhase.Simulate && ObserveComplete && !observeSummarySaved)
            {
                if (observeSummaryStep == ObserveSummaryStep.Sorting) BeginObserveSummary();
                else if (!ObserveSummaryTransitioning) SaveObserveSummary();
                return;
            }
            setPhase?.Invoke(phase);
        }

        private void BeginObserveSummary()
        {
            if (!ObserveComplete || state.Phase != InvestigationPhase.Observe || observeSummaryStep != ObserveSummaryStep.Sorting) return;
            observeSummaryStep = InvestigationMotionSettings.ReducedMotion ? ObserveSummaryStep.Ready : ObserveSummaryStep.FadingOut;
            navigationRevealTarget = "Observe Comparison Board";
            navigationRevealAtTop = true;
            RefreshPresentationOnly();
        }

        private IEnumerator AnimateObserveSummaryTransition()
        {
            yield return null;
            bool leaving = observeSummaryStep == ObserveSummaryStep.FadingOut;
            RectTransform panel = FindNamedRect(contentRoot, leaving ? "Species Sorting Columns" : "Observe Survey Story");
            if (panel != null)
            {
                CanvasGroup group = EnsureCanvasGroup(panel);
                group.interactable = group.blocksRaycasts = false;
                float duration = leaving ? .22f : .42f;
                for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    group.alpha = leaving ? 1f - t : t;
                    if (!leaving) panel.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, t);
                    yield return null;
                }
            }
            observeSummaryStep = leaving ? ObserveSummaryStep.Revealing : ObserveSummaryStep.Ready;
            if (!leaving)
            {
                navigationRevealTarget = "Continue To Simulate";
                navigationRevealAtTop = false;
            }
            RefreshPresentationOnly();
        }

        private void SaveObserveSummary()
        {
            if (!ObserveComplete || state.Phase != InvestigationPhase.Observe || observeSummarySaving || observeSummaryStep != ObserveSummaryStep.Ready) return;
            if (observeSummarySaved) { setPhase?.Invoke(InvestigationPhase.Simulate); return; }
            observeSummarySaving = true;
            if (InvestigationMotionSettings.ReducedMotion) { CompleteObserveSummarySaving(); return; }
            RefreshPresentationOnly();
        }

        private void CompleteObserveSummarySaving()
        {
            if (!observeSummarySaving) return;
            observeSummarySaving = false;
            observeSummarySaved = true;
            RemoveObserveSummaryFlight();
            RefreshPresentationOnly();
        }

        private void RenderObserveSummary(Transform parent)
        {
            RectTransform picture = CreateSurveyStory(parent, "Observe Survey Story");
            AddLayout(picture, Mathf.Clamp(contentPanel.rect.height - 170f, 344f, 440f), 0f);
            RectTransform save = CreatePanel("Observe Summary Save", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            AddLayout(save, 74f, 0f);
            Button notebook = CreateButton("Summary Notebook Destination", save, string.Empty,
                ButtonVisualStyle.PaperChoice, ToggleNotebookDrawer, out TextMeshProUGUI notebookLabel);
            notebookLabel.gameObject.SetActive(false);
            notebook.interactable = observeSummarySaved;
            notebook.GetComponent<LayoutElement>().ignoreLayout = true;
            CreateNotebookButtonArtwork(notebook);
            Anchor(notebook.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f, 12f, 8f, 64f, -8f);
            TextMeshProUGUI status = CreateText("Observe Summary Status", save,
                observeSummarySaving ? "Adding our picture to the notebook…" : observeSummarySaved
                    ? "Saved in your notebook. What could explain these changes?" : "Our findings, in one picture. Keep it for the next investigation.",
                14, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(status.rectTransform, 0f, 0f, 1f, 1f, 76f, 8f, -218f, -8f);
            Button action = CreateButton(observeSummarySaving ? "Finish Saving Summary" : "Continue To Simulate", save,
                observeSummarySaving ? "Finish saving →" : observeSummarySaved ? "Test possible causes →" : "Save summary →",
                ButtonVisualStyle.PaperPrimary, observeSummarySaving ? CompleteObserveSummarySaving : SaveObserveSummary, out TextMeshProUGUI label);
            label.fontSize = 13;
            action.interactable = !ObserveSummaryTransitioning;
            action.GetComponent<LayoutElement>().ignoreLayout = true;
            if (observeSummaryStep == ObserveSummaryStep.Revealing) EnsureCanvasGroup(picture).alpha = 0f;
            Anchor(action.GetComponent<RectTransform>(), 1f, .5f, 1f, .5f, -208f, -24f, -12f, 24f);
        }

        private RectTransform CreateSurveyStory(Transform parent, string objectName)
        {
            RectTransform paper = CreatePanel(objectName, parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            TextMeshProUGUI title = CreateText("Survey Story Title", paper, "OUR SEAMOUNT · WHAT CHANGED?", 16, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(title.rectTransform, 0f, 1f, 1f, 1f, 16f, -36f, -16f, -8f);
            bool notebook = objectName == "Notebook Survey Story";
            float cardsTop = notebook ? 242f : 106f;
            if (notebook)
            {
                // The drawer is too narrow for four shallow species in each of
                // two side-by-side maps. Stack dates without resizing Act 2.
                CreateStoryEra(paper, SurveyEra.Historical, 0f, 1f, cardsTop + 218f, 196f);
                CreateStoryEra(paper, SurveyEra.Current, 0f, 1f, cardsTop + 10f, 196f);
            }
            else
            {
                CreateStoryEra(paper, SurveyEra.Historical, 0f, .49f, cardsTop + 10f);
                CreateStoryEra(paper, SurveyEra.Current, .51f, 1f, cardsTop + 10f);
            }
            var findings = ComparisonFindings();
            findings.RemoveAll(f => !state.HasDiscoveredObservation(f.EvidenceId) || caseDefinition.FindSpecies(f.RelatedSpeciesId) == null);
            for (int i = 0; i < findings.Count; i++)
            {
                var finding = findings[i];
                var species = caseDefinition.FindSpecies(finding.RelatedSpeciesId);
                RectTransform change = CreatePanel("Story Finding " + species.SpeciesId, paper, InvestigationTheme.PaperSelected, 6f);
                int columns = notebook ? 2 : Mathf.Max(1, findings.Count);
                int row = i / columns, column = i % columns;
                float top = cardsTop - row * 72f;
                Anchor(change, column / (float)columns, 0f, (column + 1f) / columns, 0f, 8f, top - 68f, -8f, top);
                RectTransform before = CreateStorySymbols("Story Before", change, species, SurveyEra.Historical);
                Anchor(before, 0f, .40f, .43f, 1f, 5f, 0f, 0f, -5f);
                RectTransform after = CreateStorySymbols("Story After", change, species, SurveyEra.Current);
                Anchor(after, .57f, .40f, 1f, 1f, 0f, 0f, -5f, -5f);
                TextMeshProUGUI arrow = CreateText("Story Time Arrow", change, "→", 14, FontStyle.Bold,
                    InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Anchor(arrow.rectTransform, .42f, .40f, .58f, 1f, 0f, 0f, 0f, -5f);
                TextMeshProUGUI result = CreateText("Story Change", change, SurveyChangeLabel(FindingChange(finding)), 11, FontStyle.Bold,
                    InvestigationTheme.PaperInk, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Anchor(result.rectTransform, 0f, 0f, 1f, .40f, 2f, 2f, -2f, 0f);
            }
            TextMeshProUGUI note = CreateText("Survey Story Key", paper, "Pictures show detection patterns, not population counts.", 11, FontStyle.Bold,
                InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Anchor(note.rectTransform, 0f, 0f, 1f, 0f, 12f, 3f, -12f, 23f);
            return paper;
        }

        private RectTransform CreateStorySymbols(string objectName, Transform parent, InvestigationSpeciesDefinition species, SurveyEra era)
        {
            RectTransform art = CreatePanel(objectName, parent, Color.clear, 0f);
            if (!ShouldDisplaySpeciesInEra(species, era) || ResolveSurveySummary(species, era)?.Detection != SpeciesDetectionState.Detected) return art;
            var finding = FindObserveObservationForSpecies(species.SpeciesId);
            SurveyChange change = finding == null ? SurveyChange.Same : FindingChange(finding);
            // Symbol counts illustrate relative detection, never a literal census.
            int count = change == SurveyChange.More ? (era == SurveyEra.Current ? 3 : 1)
                : change == SurveyChange.Fewer ? (era == SurveyEra.Current ? 1 : 3) : 1;
            for (int i = 0; i < count; i++)
            {
                RectTransform symbol = CreateSpeciesArtwork(count == 1 ? "Survey Specimen"
                    : i == 0 ? "Group Member Left" : i == 1 ? "Survey Specimen" : "Group Member Right",
                    art, species, InvestigationTheme.Primary);
                if (count == 1) Stretch(symbol, 0f, 0f, 0f, 0f);
                else Anchor(symbol, i / 3f, .12f, (i + 1f) / 3f, .92f, 0f, 0f, 0f, 0f);
            }
            return art;
        }

        private void CreateStoryEra(RectTransform parent, SurveyEra era, float left, float right, float bottom, float fixedHeight = 0f)
        {
            string key = era == SurveyEra.Historical ? "Past" : "Today";
            RectTransform sea = CreatePanel("Story " + key, parent, InvestigationTheme.SurfaceRaised, InvestigationTheme.CardRadius);
            if (fixedHeight > 0f) Anchor(sea, left, 0f, right, 0f, 12f, bottom, -12f, bottom + fixedHeight);
            else Anchor(sea, left, 0f, right, 1f, 12f, bottom, -12f, -42f);
            RectTransform terrain = CreatePanel("Story Terrain", sea, Color.clear, 0f);
            Stretch(terrain, 0f, 0f, 0f, -28f);
            CreateSeamountVisual(terrain);
            TextMeshProUGUI date = CreateText("Story Date", sea, era == SurveyEra.Historical ? "20 YEARS AGO" : "TODAY", 14, FontStyle.Bold,
                InvestigationTheme.TextPrimary, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Anchor(date.rectTransform, 0f, 1f, 1f, 1f, 8f, -28f, -8f, -3f);
            RectTransform plot = CreatePanel("Story Depth Plot", sea, Color.clear, 0f);
            Stretch(plot, 8f, 24f, -8f, -28f);
            var speciesList = GetVisibleObserveSpecies();
            foreach (DepthBand band in new[] { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep })
            {
                var group = SurveySpeciesGroup(speciesList, era, band, false);
                group.AddRange(SurveySpeciesGroup(speciesList, era, band, true));
                for (int i = 0; i < group.Count; i++)
                {
                    var species = group[i];
                    if (!ShouldDisplaySpeciesInEra(species, era)
                        || ResolveSurveySummary(species, era)?.Detection != SpeciesDetectionState.Detected) continue;
                    // The compact map uses three readable depth lanes. The
                    // depth and stable order come from the same survey as Act 1.
                    float y = band == DepthBand.Shallow ? .85f : band == DepthBand.Mid ? .45f : .05f;
                    float cell = 1f / Mathf.Max(1, group.Count);
                    float x = (i + .5f) * cell;
                    RectTransform slot = CreatePanel("Story " + key + " Slot " + species.SpeciesId, plot, Color.clear, 0f);
                    Anchor(slot, x - cell * .48f, y, x + cell * .48f, y, 0f, -31f, 0f, 23f);
                    RectTransform art = CreateStorySymbols("Story " + key + " Species " + species.SpeciesId, slot, species, era);
                    Anchor(art, 0f, 0f, 1f, 1f, 2f, 30f, -2f, 0f);
                    TextMeshProUGUI name = CreateText("Story Species Name", slot, species.GameplayName, 10,
                        FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperCenter, InvestigationTheme.BodyFont);
                    Anchor(name.rectTransform, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 30f);
                }
            }
        }

        private void ResumeObserveSummarySaving()
        {
            if (state.Phase != InvestigationPhase.Observe || !isActiveAndEnabled) return;
            if (ObserveSummaryTransitioning)
                observeSummaryAnimation = StartCoroutine(AnimateObserveSummaryTransition());
            else if (observeSummarySaving)
                observeSummaryAnimation = StartCoroutine(AnimateObserveSummarySaving());
        }

        private IEnumerator AnimateObserveSummarySaving()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var source = FindNamedRect(contentRoot, "Observe Survey Story");
            var destination = FindNamedRect(contentRoot, "Summary Notebook Destination");
            if (source == null || destination == null) { CompleteObserveSummarySaving(); yield break; }
            RectTransform canvas = GetComponent<Canvas>().rootCanvas.GetComponent<RectTransform>();
            observeSummaryFlight = Instantiate(source, canvas, false);
            observeSummaryFlight.name = "Survey Summary In Flight";
            var group = EnsureCanvasGroup(observeSummaryFlight); group.blocksRaycasts = false; group.interactable = false;
            Rect start = BriefingBoundsIn(source, canvas);
            Rect end = BriefingBoundsIn(destination, canvas);
            PositionBriefingElement(observeSummaryFlight, start);
            for (float elapsed = 0f; elapsed < .9f; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / .9f);
                observeSummaryFlight.anchoredPosition = Vector2.Lerp(start.center, end.center, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 30f;
                observeSummaryFlight.localScale = Vector3.one * Mathf.Lerp(1f, end.width / start.width, t);
                group.alpha = 1f - t * .3f;
                yield return null;
            }
            CompleteObserveSummarySaving();
        }
    }
}
