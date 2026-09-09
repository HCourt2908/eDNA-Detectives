using System;
using System.Collections;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private readonly Dictionary<string, List<GameObject>> speciesPairHighlights = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
        private RectTransform speciesTooltipOwner;
        private bool speciesTooltipPinned;
        private Coroutine speciesTooltipDismiss;
        private string lastRecordedObservationId = string.Empty;
        private InvestigationSeamountBackdrop seamountBackdrop;
        private bool observeNotebookVisible;

        private enum NotebookEvidenceStatus
        {
            New,
            Used,
            OpenQuestion,
            Report
        }

        private void RenderObserve()
        {
            if (!ObserveArrivalActive) { RenderObserveComparison(); return; }
            string processedSummary = string.IsNullOrWhiteSpace(state.ProcessedSampleSummary)
                ? "Processed survey results"
                : state.ProcessedSampleSummary;
            Text observeHeading = CreateHeading(
                "What changed on this seamount?",
                $"{processedSummary}. Begin with today, then look back 20 years.");

            RectTransform split = new GameObject("Observe Split", typeof(RectTransform), typeof(InvestigationResponsiveSplitLayout)).GetComponent<RectTransform>();
            split.SetParent(contentRoot, false);
            InvestigationResponsiveSplitLayout splitLayout = split.GetComponent<InvestigationResponsiveSplitLayout>();
            splitLayout.padding = new RectOffset(0, 0, 0, 0);
            float availableHeight = contentPanel == null ? 480f : contentPanel.rect.height;
            float surveyHeight = Mathf.Clamp(availableHeight - 50f, 360f, 430f);
            splitLayout.Configure(0.68f, 14f, 930f, surveyHeight, surveyHeight);

            RectTransform comparison = CreatePanel("Survey Comparison", split, new Color(0f, 0f, 0f, 0f), 0f);
            RenderSurveyLens(comparison);
            if (ObserveArrivalActive)
            {
                RectTransform arrival = observeArrivalStage == ObserveArrivalStage.Welcome
                    ? RenderObserveWelcome(split) : observeArrivalStage == ObserveArrivalStage.HistoricalPreview
                        ? RenderHistoryRecordingPrompt(split) : RenderObserveRecording(split);
                Canvas.ForceUpdateCanvases();
                bool compactRecording = observeArrivalStage != ObserveArrivalStage.Welcome && GetComponent<RectTransform>().rect.width < 930f;
                if (compactRecording) surveyHeight = Mathf.Clamp(availableHeight - 50f, 260f, 430f);
                splitLayout.Configure(compactRecording ? .58f : .68f, 14f, observeArrivalStage == ObserveArrivalStage.Welcome ? 930f : 300f, surveyHeight,
                    Mathf.Max(surveyHeight, LayoutUtility.GetPreferredHeight(arrival)));
                return;
            }
        }

        private IEnumerator RevealObserveNotebook(RectTransform notebook)
        {
            CanvasGroup group = EnsureCanvasGroup(notebook);
            float elapsed = 0f;
            const float duration = .28f;
            while (elapsed < duration)
            {
                if (group == null) yield break;
                group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (group != null) group.alpha = 1f;
        }

        private RectTransform CreateNotebookEntryScroll(RectTransform notebook)
        {
            RectTransform scrollRoot = CreatePanel("Notebook Entry Scroll", notebook, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(scrollRoot, 0f, 0f, 1f, 1f, 40f, 68f, -12f, -58f);
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;

            RectTransform viewport = CreatePanel("Notebook Entry Viewport", scrollRoot, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            RectTransform entries = new GameObject(
                "Notebook Entry Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            entries.SetParent(viewport, false);
            entries.anchorMin = new Vector2(0f, 1f);
            entries.anchorMax = new Vector2(1f, 1f);
            entries.pivot = new Vector2(0.5f, 1f);
            entries.anchoredPosition = Vector2.zero;
            entries.sizeDelta = Vector2.zero;
            VerticalLayoutGroup entriesLayout = entries.GetComponent<VerticalLayoutGroup>();
            entriesLayout.padding = new RectOffset(0, 0, 2, 8);
            entriesLayout.spacing = 0f;
            entriesLayout.childControlWidth = true;
            entriesLayout.childControlHeight = true;
            entriesLayout.childForceExpandWidth = true;
            entriesLayout.childForceExpandHeight = false;
            entries.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = entries;

            RectTransform scrollbarRect = CreatePanel("Notebook Scrollbar", scrollRoot, new Color32(179, 204, 218, 110), 5f);
            Anchor(scrollbarRect, 1f, 0f, 1f, 1f, -8f, 3f, 0f, -3f);
            scrollbarRect.GetComponent<Image>().raycastTarget = true;
            Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            RectTransform slidingArea = new GameObject("Sliding Area", typeof(RectTransform)).GetComponent<RectTransform>();
            slidingArea.SetParent(scrollbarRect, false);
            Stretch(slidingArea, 1f, 1f, -1f, -1f);
            RectTransform handle = CreatePanel("Handle", slidingArea, new Color32(77, 111, 132, 230), 4f);
            Stretch(handle, 0f, 0f, 0f, 0f);
            handle.GetComponent<Image>().raycastTarget = true;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 6f;
            return entries;
        }

        private void CreateSurveyMap(Transform parent, SurveyEra era)
        {
            bool historical = era == SurveyEra.Historical;
            RectTransform map = CreatePanel(
                historical ? "Historical Seamount" : "Current Seamount",
                parent,
                historical ? InvestigationTheme.MapSurfaceHistorical : InvestigationTheme.MapSurface,
                InvestigationTheme.CardRadius);

            Text title = CreateText(
                "Survey Title",
                map,
                historical ? "20 years ago" : "Today",
                18,
                FontStyle.Bold,
                historical ? InvestigationTheme.TextPrimary : InvestigationTheme.Primary,
                TextAnchor.UpperLeft,
                InvestigationTheme.DisplayFont);
            Anchor(title.rectTransform, 0f, 1f, 1f, 1f, 14f, -44f, -12f, -10f);

            RectTransform plotArea = CreatePanel("Seamount Plot Area", map, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(plotArea, 0.08f, 0.08f, 0.92f, 0.86f, 0f, 0f, 0f, 0f);
            RectTransform visualClip = CreatePanel("Seamount Visual Clip", plotArea, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(visualClip, 0f, 0f, 0f, 0f);
            visualClip.gameObject.AddComponent<RectMask2D>();
            CreateSeamountVisual(visualClip);
            CreateDepthLabel(map, "SHALLOW", 0.66f);
            CreateDepthLabel(map, "MID", 0.38f);
            CreateDepthLabel(map, "DEEP", 0.10f);

            CreateSpeciesMarkers(plotArea, era);
            RectTransform artwork = FindNamedRect(visualClip, "Seamount Backdrop") ?? FindNamedRect(visualClip, "Seamount Sprite");
            if (artwork != null)
            {
                var pins = new List<RectTransform>();
                foreach (InvestigationSpeciesDefinition species in GetVisibleObserveSpecies())
                {
                    if (!InvestigationSpeciesMapLayout.IsBenthic(species)) continue;
                    RectTransform pin = FindNamedRect(plotArea, (historical ? "Historical Species Marker " : "Species Marker ") + species.SpeciesId);
                    if (pin != null) pins.Add(pin);
                }
                plotArea.gameObject.AddComponent<InvestigationBenthicAlignment>().Configure(plotArea, artwork, pins.ToArray());
            }
        }

        private void CreateSpeciesMarkers(RectTransform plotArea, SurveyEra era)
        {
            List<InvestigationSpeciesDefinition> visibleSpecies = GetVisibleObserveSpecies();
            string layoutSeed = $"{caseDefinition.CaseId}|{state.SurveyId}|{state.SiteId}|{observeLayoutSessionSeed}";
            DepthBand[] depthBands = { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep };
            for (int depthIndex = 0; depthIndex < depthBands.Length; depthIndex++)
            {
                DepthBand depthBand = depthBands[depthIndex];
                CreateSpeciesMarkerGroup(plotArea, visibleSpecies, layoutSeed, era, depthBand, false);
                CreateSpeciesMarkerGroup(plotArea, visibleSpecies, layoutSeed, era, depthBand, true);
            }
        }

        private void CreateSpeciesMarkerGroup(
            RectTransform plotArea,
            IReadOnlyList<InvestigationSpeciesDefinition> visibleSpecies,
            string layoutSeed,
            SurveyEra era,
            DepthBand depthBand,
            bool benthic)
        {
            List<InvestigationSpeciesDefinition> group = new List<InvestigationSpeciesDefinition>();
            for (int index = 0; index < visibleSpecies.Count; index++)
            {
                InvestigationSpeciesDefinition species = visibleSpecies[index];
                if ((caseDefinition.IsCaseSpecies(species.SpeciesId) || FindSurveyRecord(species.SpeciesId, era) != null)
                    && ResolveSurveyDepthBand(species, era) == depthBand
                    && InvestigationSpeciesMapLayout.IsBenthic(species) == benthic)
                {
                    group.Add(species);
                }
            }
            group.Sort((left, right) => InvestigationSpeciesMapLayout
                .StableOrder(layoutSeed, left.SpeciesId, depthBand, benthic)
                .CompareTo(InvestigationSpeciesMapLayout.StableOrder(layoutSeed, right.SpeciesId, depthBand, benthic)));

            for (int index = 0; index < group.Count; index++)
            {
                InvestigationSpeciesDefinition species = group[index];
                InvestigationSpeciesMapPlacement placement = InvestigationSpeciesMapLayout.Calculate(
                    layoutSeed,
                    species.SpeciesId,
                    depthBand,
                    benthic,
                    SurveyEra.Historical,
                    index,
                    group.Count);
                if (ShouldDisplaySpeciesInEra(species, era)) CreateSpeciesMarker(plotArea, species, era, placement);
            }
        }

        private List<InvestigationSpeciesDefinition> GetVisibleObserveSpecies()
        {
            List<InvestigationSpeciesDefinition> visible = new List<InvestigationSpeciesDefinition>();
            HashSet<string> includedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.Species.Count; index++)
            {
                InvestigationSpeciesDefinition species = caseDefinition.Species[index];
                if (species != null
                    && includedIds.Add(species.SpeciesId)
                    && (state.SurveySpeciesIds.Count == 0 || state.HasSurveySpecies(species.SpeciesId)))
                {
                    visible.Add(species);
                }
            }
            for (int index = 0; index < caseDefinition.SpeciesCatalog.Count; index++)
            {
                InvestigationSpeciesDefinition species = caseDefinition.SpeciesCatalog[index];
                if (species != null
                    && includedIds.Add(species.SpeciesId)
                    && state.HasSurveySpecies(species.SpeciesId))
                {
                    visible.Add(species);
                }
            }
            return visible;
        }

        private bool ShouldDisplaySpeciesInEra(InvestigationSpeciesDefinition species, SurveyEra era)
        {
            if (species == null) return false;
            if (era == SurveyEra.Current && ResolveSurveySummary(species, era)?.Detection == SpeciesDetectionState.NotDetected) return false;
            if (caseDefinition.IsCaseSpecies(species.SpeciesId)) return true;
            return FindSurveyRecord(species.SpeciesId, era) != null;
        }

        private DepthBand ResolveSurveyDepthBand(InvestigationSpeciesDefinition species, SurveyEra era)
        {
            InvestigationSurveySummary summary = ResolveSurveySummary(species, era);
            return summary == null ? InvestigationSpeciesMapLayout.ResolveDepthBand(species) : summary.Depth;
        }

        private InvestigationSurveySummary ResolveSurveySummary(InvestigationSpeciesDefinition species, SurveyEra era)
        {
            return InvestigationSurveyEvaluator.Resolve(caseDefinition, state, species,
                era == SurveyEra.Historical ? SurveyTimepoint.Historical : SurveyTimepoint.Current);
        }

        private InvestigationSurveySpeciesRecord FindSurveyRecord(string speciesId, SurveyEra era)
        {
            if (string.IsNullOrEmpty(speciesId)) return null;
            return state.FindSurveySpeciesRecord(
                speciesId,
                era == SurveyEra.Historical ? SurveyTimepoint.Historical : SurveyTimepoint.Current);
        }

        private void CreateSeamountVisual(RectTransform plotArea)
        {
            if (seamountBackdrop == null)
                seamountBackdrop = GetComponent<InvestigationSeamountBackdrop>() ?? gameObject.AddComponent<InvestigationSeamountBackdrop>();
            if (seamountBackdrop.TryInitialise())
            {
                RawImage mountain = CreateGraphic<RawImage>("Seamount Backdrop", plotArea);
                mountain.texture = seamountBackdrop.NaturalTexture;
                mountain.material = seamountBackdrop.RenderMaterial;
                mountain.raycastTarget = false;
                mountain.rectTransform.anchorMin = new Vector2(0f, 0f);
                mountain.rectTransform.anchorMax = new Vector2(1f, 0f);
                mountain.rectTransform.pivot = new Vector2(0.5f, 0f);
                mountain.rectTransform.anchoredPosition = Vector2.zero;
                mountain.rectTransform.sizeDelta = Vector2.zero;
                AspectRatioFitter aspect = mountain.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspect.aspectRatio = mountain.texture.width / (float)mountain.texture.height;
                return;
            }
            if (seamountSprite != null)
            {
                Image mountain = CreateGraphic<Image>("Seamount Sprite", plotArea);
                mountain.sprite = seamountSprite;
                mountain.preserveAspect = true;
                mountain.raycastTarget = false;
                mountain.color = Color.white;
                mountain.rectTransform.anchorMin = new Vector2(0f, 0f);
                mountain.rectTransform.anchorMax = new Vector2(1f, 0f);
                mountain.rectTransform.pivot = new Vector2(0.5f, 0f);
                mountain.rectTransform.anchoredPosition = Vector2.zero;
                mountain.rectTransform.sizeDelta = Vector2.zero;
                AspectRatioFitter aspect = mountain.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspect.aspectRatio = seamountSprite.rect.width / seamountSprite.rect.height;
            }
            else
            {
                InvestigationSeamountGraphic fallback = CreateGraphic<InvestigationSeamountGraphic>("Seamount Silhouette Fallback", plotArea);
                Stretch(fallback.rectTransform, 0f, 0f, 0f, 0f);
                fallback.color = Color.white;
            }

            InvestigationSeamountFogGraphic fog = CreateGraphic<InvestigationSeamountFogGraphic>("Seamount Foot Fog", plotArea);
            Anchor(fog.rectTransform, 0f, 0f, 1f, 0.24f, -8f, -4f, 8f, 0f);
            Color fogColor = InvestigationTheme.Primary;
            fogColor.a = 0.55f;
            fog.color = fogColor;
        }

        private void CreateDepthLabel(RectTransform map, string value, float normalizedY)
        {
            Text label = CreateText($"Depth {value}", map, value, 11, FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.UpperLeft, InvestigationTheme.DataFont);
            label.rectTransform.anchorMin = new Vector2(0f, normalizedY + 0.08f);
            label.rectTransform.anchorMax = new Vector2(1f, normalizedY + 0.08f);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.offsetMin = new Vector2(12f, -32f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);
            RectTransform line = CreatePanel($"Depth Line {value}", map, InvestigationTheme.BorderSoft, 0f);
            line.anchorMin = new Vector2(0.03f, normalizedY);
            line.anchorMax = new Vector2(0.97f, normalizedY);
            line.offsetMin = new Vector2(0f, -1f);
            line.offsetMax = new Vector2(0f, 1f);
        }

        private void CreateSpeciesMarker(
            RectTransform map,
            InvestigationSpeciesDefinition species,
            SurveyEra era,
            InvestigationSpeciesMapPlacement placement)
        {
            InvestigationObservationDefinition observation = FindObserveObservationForSpecies(species.SpeciesId);
            bool historical = era == SurveyEra.Historical;
            InvestigationSurveySummary survey = ResolveSurveySummary(species, era);
            bool notDetected = survey?.Detection == SpeciesDetectionState.NotDetected;
            bool anomaly = !historical
                && ((observation != null && observation.ClaimType != ObservationClaimType.MatchesBaseline) || notDetected);

            RectTransform rect = null;
            Button marker = CreateButton(
                historical ? $"Historical Species Marker {species.SpeciesId}" : $"Species Marker {species.SpeciesId}",
                map,
                string.Empty,
                ButtonVisualStyle.Choice,
                () => ActivateSpeciesMarker(rect, species, era, observation),
                out Text emptyLabel);
            emptyLabel.gameObject.SetActive(false);
            marker.interactable = true;
            rect = marker.GetComponent<RectTransform>();
            Vector2 comparisonPosition = placement.Anchor;
            rect.anchorMin = comparisonPosition;
            rect.anchorMax = comparisonPosition;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = placement.MarkerSize;
            rect.anchoredPosition = Vector2.zero;
            LayoutElement markerLayout = marker.GetComponent<LayoutElement>();
            markerLayout.ignoreLayout = true;

            Image background = marker.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0f);
            Outline hitOutline = marker.GetComponent<Outline>();
            if (hitOutline != null) hitOutline.effectColor = new Color(0f, 0f, 0f, 0f);
            RectTransform artwork = CreateSurveyArtwork("Species Artwork", marker.transform, species, era);
            Anchor(artwork, 0f, 0f, 1f, 1f, 10f, 6f, -10f, -4f);

            if (notDetected)
            {
                InvestigationMissingSignalGraphic missing = CreateGraphic<InvestigationMissingSignalGraphic>("Missing Signal", marker.transform);
                missing.color = InvestigationTheme.TextMuted;
                Anchor(missing.rectTransform, 0.12f, 0.08f, 0.88f, 0.96f, 0f, 0f, 0f, -2f);
            }

            InvestigationBorderGraphic focusBorder = CreateGraphic<InvestigationBorderGraphic>("Paired Species Focus", marker.transform);
            focusBorder.Configure(InvestigationTheme.SmallRadius, 1.5f);
            focusBorder.color = InvestigationTheme.Primary;
            focusBorder.raycastTarget = false;
            RectTransform pairFocus = focusBorder.rectTransform;
            Stretch(pairFocus, 2f, 2f, -2f, -2f);
            pairFocus.SetAsFirstSibling();
            pairFocus.gameObject.SetActive(!ObserveArrivalActive && HighlightedObserveSpecies == species.SpeciesId);
            if (!speciesPairHighlights.TryGetValue(species.SpeciesId, out List<GameObject> highlights))
            {
                highlights = new List<GameObject>();
                speciesPairHighlights.Add(species.SpeciesId, highlights);
            }
            highlights.Add(pairFocus.gameObject);
            if (!historical && observation != null && state.HasDiscoveredObservation(observation.EvidenceId))
            {
                Image recorded = CreateStatusIcon("Recorded Finding", marker.transform, InvestigationStatusIconLibrary.Check, InvestigationTheme.Success);
                Anchor(recorded.rectTransform, 1f, 1f, 1f, 1f, -23f, -23f, -3f, -3f);
            }

            InvestigationHoverTooltipTrigger tooltipTrigger = marker.gameObject.AddComponent<InvestigationHoverTooltipTrigger>();
            tooltipTrigger.Configure(
                1f,
                () => ShowSpeciesTooltip(rect, species, era),
                () => HideSpeciesTooltipFor(rect));

            if (string.Equals(pendingTappedSpeciesId, species.SpeciesId, StringComparison.Ordinal)
                && pendingTappedSpeciesHistorical == historical)
            {
                pendingTappedSpeciesMarker = rect;
                pendingTappedSpecies = species;
            }
        }

        private static void CreateMapGroupMember(Transform parent, Sprite sprite, string name, float minX, float minY, float maxX, float maxY)
        {
            Image member = CreateGraphic<Image>(name, parent);
            member.sprite = sprite;
            member.preserveAspect = true;
            member.raycastTarget = false;
            member.color = new Color(1f, 1f, 1f, 0.72f);
            Anchor(member.rectTransform, minX, minY, maxX, maxY, 0f, 0f, 0f, 0f);
        }

        private void ActivateSpeciesMarker(
            RectTransform marker,
            InvestigationSpeciesDefinition species,
            SurveyEra era,
            InvestigationObservationDefinition observation)
        {
            if (era == SurveyEra.Historical) ShowReferenceSurveyNotice();
            ShowSpeciesTooltip(marker, species, era, true);
        }

        private void ShowPendingTappedSpeciesTooltip()
        {
            if (string.IsNullOrEmpty(pendingTappedSpeciesId)) return;
            RectTransform marker = pendingTappedSpeciesMarker;
            InvestigationSpeciesDefinition species = pendingTappedSpecies;
            pendingTappedSpeciesId = string.Empty;
            pendingTappedSpeciesMarker = null;
            pendingTappedSpecies = null;
            ShowSpeciesTooltip(marker, species, pendingTappedSpeciesHistorical ? SurveyEra.Historical : SurveyEra.Current, true);
        }

        private int CountVisibleNotebookObservations()
        {
            int count = 0;
            for (int index = 0; index < state.DiscoveredObservationIds.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.FindObservation(state.DiscoveredObservationIds[index]);
                if (observation != null && observation.Source != ObservationSource.Methodology) count++;
            }
            return count;
        }

        private void RenderNotebookEntries(Transform parent)
        {
            int shown = 0;
            shown += RenderNotebookGroup(parent, "FOOD-WEB PATTERN", observation => observation.Category == EvidenceCategory.FoodWeb);
            shown += RenderNotebookGroup(
                parent,
                "STABLE CONTROLS",
                observation => observation.UnlockStage == EvidenceUnlockStage.Observe
                    && (observation.Category == EvidenceCategory.Benthic || observation.Category == EvidenceCategory.Alternative));
            shown += RenderNotebookGroup(
                parent,
                "FOLLOW-UP CLUES",
                observation => observation.Category != EvidenceCategory.FoodWeb
                    && !(observation.UnlockStage == EvidenceUnlockStage.Observe
                        && (observation.Category == EvidenceCategory.Benthic || observation.Category == EvidenceCategory.Alternative)));
            if (shown == 0)
            {
                Text empty = CreateText("Notebook Empty", parent, "Your recorded findings will appear here.", 14, FontStyle.Normal, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                AddLayout(empty.rectTransform, 92f, 1f);
            }
        }

        private int RenderNotebookGroup(
            Transform parent,
            string groupName,
            Func<InvestigationObservationDefinition, bool> belongsToGroup)
        {
            bool hasEntries = false;
            for (int index = 0; index < state.DiscoveredObservationIds.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.FindObservation(state.DiscoveredObservationIds[index]);
                if (observation != null && observation.Source != ObservationSource.Methodology && belongsToGroup(observation))
                {
                    hasEntries = true;
                    break;
                }
            }
            if (!hasEntries) return 0;

            Text heading = CreateText($"Notebook Group {groupName}", parent, groupName, 10, FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.LowerLeft, InvestigationTheme.DataFont);
            AddLayout(heading.rectTransform, 22f, 1f);
            int shown = 0;
            for (int index = 0; index < state.DiscoveredObservationIds.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.FindObservation(state.DiscoveredObservationIds[index]);
                if (observation == null || observation.Source == ObservationSource.Methodology || !belongsToGroup(observation)) continue;
                CreateNotebookEntry(parent, observation);
                shown++;
            }
            return shown;
        }

        private void CreateNotebookEntry(Transform parent, InvestigationObservationDefinition observation)
        {
            bool relatedToSelection = state.Phase == InvestigationPhase.Simulate
                && !string.IsNullOrEmpty(selectedPredictionSpeciesId)
                && IsDirectObservationForSelectedTarget(observation);
            RectTransform item = CreatePanel(
                $"Notebook {observation.EvidenceId}",
                parent,
                relatedToSelection ? new Color32(217, 236, 243, 170) : new Color(0f, 0f, 0f, 0f),
                relatedToSelection ? 8f : 0f);
            AddLayout(item, 58f, 1f);
            if (relatedToSelection)
                EnsureOutline(item.gameObject, InvestigationTheme.PaperSelectedBorder, new Vector2(1f, -1f));
            Color evidenceColor = NotebookStateColor(observation.ClaimType);
            RectTransform bullet = CreatePanel("Evidence Bullet", item, evidenceColor, 4f);
            Anchor(bullet, 0f, 0.56f, 0f, 0.56f, 4f, -6f, 16f, 6f);
            InvestigationSpeciesDefinition species = caseDefinition.FindSpecies(observation.RelatedSpeciesId);
            string stateLabel = NotebookStateLabel(observation.ClaimType);
            string stateColor = ColorUtility.ToHtmlStringRGB(evidenceColor);
            Text title = CreateText(
                "Observation",
                item,
                $"{species?.GameplayName ?? observation.DisplayName} <b><color=#{stateColor}>{stateLabel}</color></b>",
                14,
                FontStyle.Normal,
                InvestigationTheme.PaperInk,
                TextAnchor.LowerLeft,
                InvestigationTheme.DisplayFont);
            title.supportRichText = true;
            Anchor(title.rectTransform, 0f, 0.43f, 1f, 1f, 24f, 0f, -76f, 0f);
            Text source = CreateText("Source", item, $"{ObservationSourceDisplayName(observation.Source)} · {observation.Confidence} confidence", 10, FontStyle.Normal, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.DataFont);
            Anchor(source.rectTransform, 0f, 0f, 1f, 0.46f, 24f, 0f, -76f, 0f);

            NotebookEvidenceStatus status = NotebookStatusFor(observation.EvidenceId);
            Color badgeColor = NotebookStatusColor(status);
            RectTransform statusBadge = CreatePanel("Evidence Status", item, badgeColor, 7f);
            Anchor(statusBadge, 1f, 0.5f, 1f, 0.5f, -68f, -12f, -8f, 12f);
            Text statusText = CreateText(
                "Evidence Status Text",
                statusBadge,
                NotebookStatusLabel(status),
                9,
                FontStyle.Bold,
                status == NotebookEvidenceStatus.Report ? Color.white : InvestigationTheme.PaperInk,
                TextAnchor.MiddleCenter,
                InvestigationTheme.DataFont);
            Stretch(statusText.rectTransform, 4f, 1f, -4f, -1f);
            RectTransform paperLine = CreatePanel("Paper Line", item, InvestigationTheme.PaperRule, 0f);
            Anchor(paperLine, 0.04f, 0f, 0.96f, 0f, 0f, 0f, 0f, 2f);
        }

        private NotebookEvidenceStatus NotebookStatusFor(string evidenceId)
        {
            if (state.HasSelectedEvidence(evidenceId)) return NotebookEvidenceStatus.Report;

            bool used = false;
            bool openQuestion = false;
            for (int index = 0; index < state.ComparisonRecords.Count; index++)
            {
                PredictionComparisonRecord record = state.ComparisonRecords[index];
                if (!string.Equals(record.EvidenceId, evidenceId, StringComparison.Ordinal)) continue;
                if (record.LocksComparison) used = true;
                else if (record.IsAccepted && record.Judgement == ComparisonJudgement.NotEnoughEvidence)
                    openQuestion = true;
            }
            if (used) return NotebookEvidenceStatus.Used;
            if (openQuestion) return NotebookEvidenceStatus.OpenQuestion;
            return NotebookEvidenceStatus.New;
        }

        private static string NotebookStatusLabel(NotebookEvidenceStatus status)
        {
            switch (status)
            {
                case NotebookEvidenceStatus.Used: return "USED";
                case NotebookEvidenceStatus.OpenQuestion: return "OPEN";
                case NotebookEvidenceStatus.Report: return "REPORT";
                default: return "NEW";
            }
        }

        private static Color NotebookStatusColor(NotebookEvidenceStatus status)
        {
            switch (status)
            {
                case NotebookEvidenceStatus.Used: return InvestigationTheme.PaperSelected;
                case NotebookEvidenceStatus.OpenQuestion: return InvestigationTheme.PaperRule;
                case NotebookEvidenceStatus.Report: return InvestigationTheme.PaperSelectedBorder;
                default: return new Color32(231, 229, 213, 255);
            }
        }

        private static string ObservationSourceDisplayName(ObservationSource source)
        {
            switch (source)
            {
                case ObservationSource.EDNA: return "eDNA";
                case ObservationSource.CTDLog: return "CTD sensor";
                case ObservationSource.ROV: return "ROV";
                case ObservationSource.Methodology: return "Method note";
                default: return "Survey";
            }
        }

        private void ShowSpeciesTooltip(
            RectTransform marker,
            InvestigationSpeciesDefinition species,
            SurveyEra era = SurveyEra.Current,
            bool pinned = false)
        {
            if (marker == null || species == null) return;
            // A delayed hover callback must not replace a card pinned by a tap.
            if (!pinned && speciesTooltipPinned && speciesTooltipOwner == marker) return;
            HideSpeciesTooltip();
            speciesTooltipOwner = marker;
            speciesTooltipPinned = pinned;
            SetSpeciesPairHighlight(species.SpeciesId);

            RectTransform overlay = GetComponent<RectTransform>();
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, marker.TransformPoint(marker.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screenPoint, null, out Vector2 localPoint);
            bool placeRight = screenPoint.x < Screen.width * 0.55f;

            speciesTooltip = CreatePanel("Species Facts Tooltip", overlay, InvestigationTheme.Deep, InvestigationTheme.SmallRadius);
            speciesTooltip.GetComponent<Image>().raycastTarget = pinned;
            speciesTooltip.anchorMin = Vector2.one * 0.5f;
            speciesTooltip.anchorMax = Vector2.one * 0.5f;
            speciesTooltip.pivot = new Vector2(placeRight ? 0f : 1f, 0.5f);
            speciesTooltip.sizeDelta = new Vector2(Mathf.Min(390f, overlay.rect.width - 24f), 258f);
            Vector2 tooltipPosition = localPoint + new Vector2(placeRight ? 64f : -64f, 0f);
            float width = speciesTooltip.sizeDelta.x;
            float height = speciesTooltip.sizeDelta.y;
            tooltipPosition.x = placeRight
                ? Mathf.Clamp(tooltipPosition.x, overlay.rect.xMin + 12f, overlay.rect.xMax - width - 12f)
                : Mathf.Clamp(tooltipPosition.x, overlay.rect.xMin + width + 12f, overlay.rect.xMax - 12f);
            tooltipPosition.y = Mathf.Clamp(
                tooltipPosition.y,
                overlay.rect.yMin + height * 0.5f + 12f,
                overlay.rect.yMax - height * 0.5f - 12f);
            speciesTooltip.anchoredPosition = tooltipPosition;
            speciesTooltip.SetAsLastSibling();

            Outline outline = speciesTooltip.gameObject.AddComponent<Outline>();
            outline.effectColor = InvestigationTheme.Primary;
            outline.effectDistance = new Vector2(1f, -1f);

            Text eyebrow = CreateText("Tooltip Eyebrow", speciesTooltip, "SPECIES FACTS", 11, FontStyle.Bold, InvestigationTheme.Primary, TextAnchor.UpperLeft, InvestigationTheme.DataFont);
            Anchor(eyebrow.rectTransform, 0f, 0.80f, 1f, 1f, 16f, 0f, -12f, -12f);
            Button close = CreateButton("Close Species Facts", speciesTooltip, "Close", ButtonVisualStyle.Tertiary, HideSpeciesTooltip, out Text closeLabel);
            close.GetComponent<LayoutElement>().ignoreLayout = true;
            closeLabel.fontSize = 12;
            Anchor(close.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -72f, -42f, -8f, -4f);
            Text title = CreateText("Tooltip Title", speciesTooltip, species.DisplayName, 19, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.DisplayFont);
            Anchor(title.rectTransform, 0f, 0.61f, 1f, 0.84f, 16f, 0f, -12f, 0f);
            Text description = CreateText("Tooltip Description", speciesTooltip, species.Description, 13, FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            Anchor(description.rectTransform, 0f, 0.39f, 1f, 0.63f, 16f, 0f, -12f, -2f);
            Text details = CreateText(
                "Tooltip Details",
                speciesTooltip,
                BuildSpeciesTooltipDetails(species, era),
                11,
                FontStyle.Normal,
                InvestigationTheme.TextMuted,
                TextAnchor.UpperLeft,
                InvestigationTheme.DataFont);
            Anchor(details.rectTransform, 0f, 0f, 1f, 0.40f, 16f, 10f, -12f, 0f);
            if (pinned) speciesTooltipDismiss = StartCoroutine(DismissTappedSpeciesTooltip(speciesTooltip));
        }

        private IEnumerator DismissTappedSpeciesTooltip(RectTransform shownTooltip)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            speciesTooltipDismiss = null;
            if (speciesTooltip == shownTooltip) HideSpeciesTooltip();
        }

        private string BuildSpeciesTooltipDetails(InvestigationSpeciesDefinition species, SurveyEra era)
        {
            InvestigationSurveySummary summary = ResolveSurveySummary(species, era);
            if (summary == null) return "No record was supplied for this survey era.";
            string status = era == SurveyEra.Historical ? "Historical reference · read only"
                : summary.Observation == null ? "Supplementary survey · not a required case finding"
                : state.HasDiscoveredObservation(summary.Observation.EvidenceId) ? "RECORDED IN NOTEBOOK" : "Compare this record on the notebook table";
            string source = string.IsNullOrEmpty(summary.Confidence) ? summary.Source : $"{summary.Source} · {summary.Confidence}";
            return $"{summary.Result}\n{source}\nDEPTH  {summary.Depth}\n{status}";
        }

        private void SetSpeciesPairHighlight(string speciesId)
        {
            foreach (KeyValuePair<string, List<GameObject>> pair in speciesPairHighlights)
                foreach (GameObject highlight in pair.Value)
                    if (highlight != null) highlight.SetActive(pair.Key == speciesId);
        }

        private void HideSpeciesTooltip()
        {
            if (speciesTooltipDismiss != null)
            {
                StopCoroutine(speciesTooltipDismiss);
                speciesTooltipDismiss = null;
            }
            speciesTooltipOwner = null;
            speciesTooltipPinned = false;
            SetSpeciesPairHighlight(ObserveArrivalActive ? null : HighlightedObserveSpecies);
            if (speciesTooltip == null) return;
            GameObject tooltipObject = speciesTooltip.gameObject;
            speciesTooltip = null;
            tooltipObject.SetActive(false);
            if (Application.isPlaying) Destroy(tooltipObject);
            else DestroyImmediate(tooltipObject);
        }

        private void HideSpeciesTooltipFor(RectTransform marker)
        {
            if (speciesTooltipOwner == marker && !speciesTooltipPinned) HideSpeciesTooltip();
        }

        private InvestigationObservationDefinition FindObserveObservationForSpecies(string speciesId)
        {
            return InvestigationSurveyEvaluator.FindCaseObservation(caseDefinition, speciesId);
        }

        private static string JoinDepths(InvestigationSpeciesDefinition species)
        {
            if (species.PreferredDepths.Count == 0) return "unknown";
            string value = string.Empty;
            for (int index = 0; index < species.PreferredDepths.Count; index++)
            {
                if (index > 0) value += ", ";
                value += species.PreferredDepths[index].ToString();
            }
            return value;
        }

        private static string ConciseObservationLabel(InvestigationObservationDefinition observation)
        {
            switch (observation.ClaimType)
            {
                case ObservationClaimType.NotDetected: return "Not detected";
                case ObservationClaimType.NewDetection: return "New detection";
                case ObservationClaimType.ChangedDepthOrDistribution: return "More sites";
                case ObservationClaimType.MatchesBaseline: return "Same as before";
                case ObservationClaimType.ResultWarning: return "Result warning";
                default: return observation.DisplayName;
            }
        }

        private static string NotebookStateLabel(ObservationClaimType claimType)
        {
            switch (claimType)
            {
                case ObservationClaimType.NotDetected: return "Not detected";
                case ObservationClaimType.NewDetection: return "New detection";
                case ObservationClaimType.ChangedDepthOrDistribution: return "Detected at more sites";
                case ObservationClaimType.MatchesBaseline: return "Stable";
                case ObservationClaimType.ResultWarning: return "Result warning";
                case ObservationClaimType.EnvironmentalReading: return "Historical range";
                case ObservationClaimType.PhysicalObservation: return "Physical evidence";
                default: return "Observation";
            }
        }

        private static Color NotebookStateColor(ObservationClaimType claimType)
        {
            switch (claimType)
            {
                case ObservationClaimType.NotDetected: return InvestigationTheme.PaperInk;
                case ObservationClaimType.ChangedDepthOrDistribution: return InvestigationTheme.PaperSelectedBorder;
                case ObservationClaimType.MatchesBaseline: return InvestigationTheme.PaperMuted;
                case ObservationClaimType.NewDetection: return new Color32(27, 109, 138, 255);
                case ObservationClaimType.EnvironmentalReading: return new Color32(27, 109, 138, 255);
                case ObservationClaimType.PhysicalObservation: return new Color32(28, 105, 99, 255);
                default: return InvestigationTheme.PaperMuted;
            }
        }

    }
}
