using TMPro;
using System;
using System.Collections.Generic;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum SurveyChange { More, Fewer, NotDetected, Same }
        private string selectedComparisonSpecies = string.Empty;
        private bool observeMapOpen;
        private string HighlightedObserveSpecies => !string.IsNullOrEmpty(selectedComparisonSpecies) ? selectedComparisonSpecies : ObserveQuestion?.RelatedSpeciesId;

        private bool HasGroupedSurveyArtwork(InvestigationSpeciesDefinition species, SurveyEra era)
        {
            var finding = FindObserveObservationForSpecies(species.SpeciesId);
            return species.Icon != null && finding != null
                && (era == SurveyEra.Current && FindingChange(finding) == SurveyChange.More
                    || era == SurveyEra.Historical && FindingChange(finding) == SurveyChange.Fewer);
        }

        // The same composition is used on the map, in the flying record, and on
        // both notebook pages. Repeated symbols represent detection patterns, not a census.
        private RectTransform CreateSurveyArtwork(string name, Transform parent, InvestigationSpeciesDefinition species, SurveyEra era)
        {
            RectTransform root = CreatePanel(name, parent, Color.clear, 0f);
            bool group = HasGroupedSurveyArtwork(species, era);
            bool absent = ResolveSurveySummary(species, era)?.Detection == EDNA.Core.SpeciesDetectionState.NotDetected;
            RectTransform specimen = CreateSpeciesArtwork("Survey Specimen", root, species, InvestigationTheme.Primary, absent);
            if (group)
            {
                Anchor(specimen, .3f, .12f, .7f, .96f, 0f, 0f, 0f, 0f);
                CreateMapGroupMember(root, species.Icon, "Group Member Left", .02f, .18f, .38f, .80f);
                CreateMapGroupMember(root, species.Icon, "Group Member Right", .62f, .20f, .98f, .82f);
            }
            else Stretch(specimen, 0f, 0f, 0f, 0f);
            return root;
        }

        private List<InvestigationObservationDefinition> ComparisonFindings()
        {
            var findings = new List<InvestigationObservationDefinition>();
            foreach (var finding in caseDefinition.Observations)
                if (InvestigationObserveEvaluator.IsInitialFinding(finding)) findings.Add(finding);
            return findings;
        }

        private static string SurveyChangeLabel(SurveyChange change)
        {
            switch (change)
            {
                case SurveyChange.More: return "More";
                case SurveyChange.Fewer: return "Fewer";
                case SurveyChange.NotDetected: return "Not detected";
                default: return "Same";
            }
        }

        private static SurveyChange FindingChange(InvestigationObservationDefinition finding)
        {
            switch (finding.ClaimType)
            {
                case ObservationClaimType.NotDetected: return SurveyChange.NotDetected;
                case ObservationClaimType.ReducedDetection: return SurveyChange.Fewer;
                case ObservationClaimType.ChangedDepthOrDistribution:
                case ObservationClaimType.NewDetection: return SurveyChange.More;
                default: return SurveyChange.Same;
            }
        }

        private void SelectComparisonSpecies(string speciesId)
        {
            if (ComparisonBriefingActive) return;
            var finding = FindObserveObservationForSpecies(speciesId);
            if (finding == null || state.HasDiscoveredObservation(finding.EvidenceId)) return;
            selectedComparisonSpecies = speciesId;
            observeComparisonFeedback = "Choose a change for " + caseDefinition.FindSpecies(speciesId).GameplayName + ". Your notebook has both surveys.";
            RefreshPresentationOnly();
        }

        private void SortComparisonSpecies(string speciesId, SurveyChange change)
        {
            if (ComparisonBriefingActive) return;
            if (string.IsNullOrEmpty(speciesId))
            {
                observeComparisonFeedback = "Select a species first, or drag one into a change.";
                RefreshPresentationOnly(); return;
            }
            var finding = FindObserveObservationForSpecies(speciesId);
            if (finding == null || state.HasDiscoveredObservation(finding.EvidenceId)) return;
            if (FindingChange(finding) != change)
            {
                selectedComparisonSpecies = speciesId;
                observeComparisonFeedback = "Take another look in your notebook. Compare this species in both surveys, then try again.";
                RefreshPresentationOnly(); return;
            }
            selectedComparisonSpecies = string.Empty;
            observeComparisonFeedback = "Recorded: " + caseDefinition.FindSpecies(speciesId).GameplayName + " → " + SurveyChangeLabel(change) + ". Saved in your notebook.";
            lastRecordedObservationId = finding.EvidenceId;
            pendingTappedSpeciesId = string.Empty;
            discoverObservation?.Invoke(finding.EvidenceId);
        }

        private RectTransform CreateComparisonColumn(Transform parent, string objectName, string title)
        {
            RectTransform panel = CreatePanel(objectName, parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8); layout.spacing = 5f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            TextMeshProUGUI label = CreateText(objectName + " Title", panel, title, 16, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(label.rectTransform, 24f, 0f);
            return panel;
        }

        private void RenderObserveComparison()
        {
            RectTransform board = CreatePanel("Observe Comparison Board", contentRoot, Color.clear, 0f);
            VerticalLayoutGroup layout = board.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            RectTransform edna = CreatePanel("Observe Comparison Edna", board, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            AddLayout(edna, 84f, 0f);
            EnsureEdnaArtwork();
            Image portrait = CreateStatusIcon("Edna Introduction Portrait", edna, ednaPortrait, Color.white);
            Anchor(portrait.rectTransform, 0f, 0f, 0f, 1f, 4f, 0f, 78f, 2f);
            string heading = ObserveSummaryVisible ? "OUR FINDINGS" : "FIND WHAT CHANGED";
            TextMeshProUGUI title = CreateText("Edna Name", edna, $"EDNA · {heading} · {CountInitialFindings()}/{InvestigationObserveEvaluator.RequiredCount(caseDefinition)}", 13,
                FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(title.rectTransform, 0f, .57f, 1f, 1f, 92f, 0f, -14f, -4f);
            TextMeshProUGUI message = CreateText("Observe Comparison Instruction", edna,
                InvestigationObserveEvaluator.IsComplete(caseDefinition, state)
                    ? (ObserveSummaryVisible ? "Here is the change across 20 years. Keep this picture in your notebook as we investigate the cause."
                        : "All species compared! Click 'Summarise our findings' below Species to make our picture.")
                    : ComparisonActionInstruction, 14,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(message.rectTransform, 0f, 0f, 1f, .57f, 92f, 4f, -14f, 0f);
            if (ObserveComplete && ObserveSummaryVisible) { RenderObserveSummary(board); return; }
            var findings = ComparisonFindings();
            RectTransform columns = new GameObject("Species Sorting Columns", typeof(RectTransform), typeof(InvestigationComparisonLayout)).GetComponent<RectTransform>();
            columns.SetParent(board, false);
            var split = columns.GetComponent<InvestigationComparisonLayout>();
            RectTransform speciesPage = CreateComparisonColumn(columns, "Comparison Species Page", "1 · SPECIES");
            TextMeshProUGUI speciesHint = CreateText("Select Species Instruction", speciesPage,
                InvestigationObserveEvaluator.IsComplete(caseDefinition, state) ? "All compared"
                    : string.IsNullOrEmpty(selectedComparisonSpecies) ? "Click one to select" : "Selected: " + ComparisonSelectedName,
                12, FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(speciesHint);
            int remaining = 0;
            foreach (var finding in findings)
            {
                if (state.HasDiscoveredObservation(finding.EvidenceId)) continue;
                CreateComparisonSpeciesCard(speciesPage, finding);
                remaining++;
            }
            if (remaining == 0)
            {
                TextMeshProUGUI done = CreateText("Species Sorting Complete", speciesPage, "All species compared.\nYour choices are saved.", 17,
                    FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                ConfigureContentDrivenText(done);
                if (ObserveComplete) RenderComparisonTools(speciesPage);
            }
            RectTransform changes = CreateComparisonColumn(columns, "Comparison Changes Page", "2 · COMPARE");
            TextMeshProUGUI changeHint = CreateText("Choose Change Instruction", changes,
                InvestigationObserveEvaluator.IsComplete(caseDefinition, state) ? "All changes recorded"
                    : string.IsNullOrEmpty(selectedComparisonSpecies) ? "Choose a species first" : "Now click its change", 12,
                FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(changeHint);
            RectTransform grid = new GameObject("Comparison Change Zones", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(changes, false);
            grid.GetComponent<InvestigationResponsiveGridLayout>().Configure(2, 2, 2, 96f, 8f);
            foreach (SurveyChange change in Enum.GetValues(typeof(SurveyChange))) CreateSurveyChangeZone(grid, change, findings);
            if (observeMapOpen) RenderComparisonSeamount(columns);
            else RenderComparisonNotebook(columns);
            split.Configure(ObserveComplete ? 236f : Mathf.Max(160f, 63f + remaining * 53f), 268f, Mathf.Clamp(contentPanel.rect.height - 184f, 330f, 480f));
            TextMeshProUGUI feedback = CreateText("Observe Comparison Feedback", board, string.IsNullOrEmpty(observeComparisonFeedback)
                ? "Drag a species into a change, or select the species and then the change." : observeComparisonFeedback,
                14, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(feedback);
            feedback.gameObject.SetActive(!ObserveComplete);

            ApplyComparisonBriefingVisibility(edna, speciesPage, changes, feedback);
        }

        private void CreateComparisonSpeciesCard(Transform parent, InvestigationObservationDefinition finding)
        {
            var species = caseDefinition.FindSpecies(finding.RelatedSpeciesId);
            if (species == null)
            {
                TextMeshProUGUI note = CreateText("Additional Comparison Record", parent, finding.DisplayName, 14, FontStyle.Bold,
                    InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                ConfigureContentDrivenText(note); return;
            }
            string id = species.SpeciesId;
            Button card = CreateButton("Compare Species " + id, parent, string.Empty, ButtonVisualStyle.PaperChoice,
                () => SelectComparisonSpecies(id), out TextMeshProUGUI hidden);
            hidden.gameObject.SetActive(false);
            card.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
            card.GetComponent<LayoutElement>().minHeight = card.GetComponent<LayoutElement>().preferredHeight = 48f;
            // This is a species token, not a survey record: dated detection images
            // remain in the notebook, so the player must make the comparison.
            RectTransform art = CreateSpeciesArtwork("Comparison Species Artwork", card.transform, species, InvestigationTheme.Primary);
            Anchor(art, 0f, 0f, 0f, 1f, 6f, 4f, 62f, -4f);
            TextMeshProUGUI name = CreateText("Comparison Species Name", card.transform, species.GameplayName, 15,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(name.rectTransform, 0f, 0f, 1f, 1f, 70f, 4f, -6f, -4f);
            if (selectedComparisonSpecies == id) card.targetGraphic.color = InvestigationTheme.PaperSelected;
            if (!ComparisonBriefingActive && string.IsNullOrEmpty(selectedComparisonSpecies) && state.Difficulty == InvestigationDifficulty.Easy)
                AddChoiceBorderCue("Pick Species Cue", card.transform);
            card.gameObject.AddComponent<InvestigationWorkbenchDrag>().Configure("survey-species", id, species.GameplayName,
                target => Stretch(CreateSpeciesArtwork("Dragged Species Token", target, species, InvestigationTheme.Primary), 8f, 4f, -8f, -4f));
        }

        private void CreateSurveyChangeZone(Transform parent, SurveyChange change, List<InvestigationObservationDefinition> findings)
        {
            Button zone = CreateButton("Compare Change " + change, parent, string.Empty, ButtonVisualStyle.PaperChoice,
                () => SortComparisonSpecies(selectedComparisonSpecies, change), out TextMeshProUGUI hidden);
            hidden.gameObject.SetActive(false);
            zone.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
            zone.interactable = !InvestigationObserveEvaluator.IsComplete(caseDefinition, state);
            if (!ComparisonBriefingActive && !string.IsNullOrEmpty(selectedComparisonSpecies) && zone.interactable && state.Difficulty == InvestigationDifficulty.Easy)
                AddChoiceBorderCue("Choose Change Cue", zone.transform);
            zone.gameObject.AddComponent<InvestigationWorkbenchDrop>().Configure("survey-species", id => SortComparisonSpecies(id, change));
            TextMeshProUGUI title = CreateText("Comparison Change Label", zone.transform, SurveyChangeLabel(change), 14, FontStyle.Bold,
                InvestigationTheme.PaperInk, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            var sorted = new List<InvestigationSpeciesDefinition>();
            foreach (var finding in findings)
                if (state.HasDiscoveredObservation(finding.EvidenceId) && FindingChange(finding) == change)
                {
                    var species = caseDefinition.FindSpecies(finding.RelatedSpeciesId);
                    if (species != null) sorted.Add(species);
                }
            if (sorted.Count == 0) Stretch(title.rectTransform, 8f, 8f, -8f, -8f);
            else
            {
                Anchor(title.rectTransform, 0f, .64f, 1f, 1f, 6f, 0f, -6f, -4f);
                for (int i = 0; i < sorted.Count; i++)
                {
                    var species = sorted[i];
                    RectTransform token = CreatePanel("Sorted Species " + species.SpeciesId, zone.transform, Color.clear, 0f);
                    Anchor(token, i / (float)sorted.Count, 0f, (i + 1f) / sorted.Count, .66f, 4f, 5f, -4f, 0f);
                    RectTransform art = CreateSpeciesArtwork("Sorted Species Artwork", token, species, InvestigationTheme.Primary);
                    Anchor(art, .15f, .27f, .85f, 1f, 0f, 0f, 0f, -2f);
                    TextMeshProUGUI name = CreateText("Sorted Species Name", token, species.GameplayName, 11,
                        FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                    Anchor(name.rectTransform, 0f, 0f, 1f, .30f, 0f, 0f, 0f, 0f);
                }
                zone.targetGraphic.color = InvestigationTheme.PaperSelected;
            }
            ColorBlock colors = zone.colors; colors.disabledColor = Color.white; zone.colors = colors;
        }

        private string ComparisonSelectedName => caseDefinition.FindSpecies(selectedComparisonSpecies)?.GameplayName ?? "Species";

        private string ComparisonActionInstruction => string.IsNullOrEmpty(selectedComparisonSpecies)
            ? "Click a species to start. Check its two surveys on the right, then choose a change."
            : ComparisonSelectedName
                + " selected. Check 20 years ago → Today on the right, then click its change. You can also drag the card.";

        private void RenderComparisonTools(Transform parent)
        {
            if (!ObserveComplete) return;
            Button summarize = CreateButton("Summarize Findings", parent,
                ObserveSummaryTransitioning ? "Bringing our findings together…" : "Summarise our findings →", ButtonVisualStyle.Primary,
                BeginObserveSummary, out TextMeshProUGUI label);
            label.fontSize = 14;
            ConfigureWrappingChoice(summarize, label);
            LayoutElement size = summarize.GetComponent<LayoutElement>();
            size.minWidth = 0f;
            size.preferredWidth = -1f;
            size.flexibleWidth = 1f;
            size.minHeight = size.preferredHeight = 64f;
            summarize.interactable = !ObserveSummaryTransitioning;
            if (summarize.interactable) AddChoiceBorderCue("Summarise Findings Cue", summarize.transform);
        }
    }
}
