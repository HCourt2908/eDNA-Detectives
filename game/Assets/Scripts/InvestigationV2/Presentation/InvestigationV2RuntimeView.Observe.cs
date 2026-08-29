using System;
using EDNA.Investigation.V2.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    public sealed partial class InvestigationV2RuntimeView
    {
        private void RenderObserve()
        {
            string processedSummary = string.IsNullOrWhiteSpace(state.ProcessedSampleSummary)
                ? "Processed survey results"
                : state.ProcessedSampleSummary;
            CreateHeading(
                "What changed on this seamount?",
                $"{processedSummary}. Compare them with the 20-year baseline and record unusual results on today's survey.");

            RectTransform split = new GameObject("Observe Split", typeof(RectTransform), typeof(InvestigationV2ResponsiveSplitLayout)).GetComponent<RectTransform>();
            split.SetParent(contentRoot, false);
            InvestigationV2ResponsiveSplitLayout splitLayout = split.GetComponent<InvestigationV2ResponsiveSplitLayout>();
            splitLayout.padding = new RectOffset(0, 0, 0, 0);
            float availableHeight = contentPanel == null ? 480f : contentPanel.rect.height;
            float surveyHeight = Mathf.Clamp(availableHeight - 50f, 360f, 430f);
            splitLayout.Configure(0.76f, 14f, 930f, surveyHeight, surveyHeight);

            RectTransform comparison = CreatePanel("Survey Comparison", split, new Color(0f, 0f, 0f, 0f), 0f);
            HorizontalLayoutGroup comparisonLayout = comparison.gameObject.AddComponent<HorizontalLayoutGroup>();
            comparisonLayout.spacing = 10f;
            comparisonLayout.childControlWidth = true;
            comparisonLayout.childControlHeight = true;
            comparisonLayout.childForceExpandWidth = true;
            comparisonLayout.childForceExpandHeight = true;
            CreateSurveyMap(comparison, SurveyEra.Historical);
            CreateSurveyMap(comparison, SurveyEra.Current);

            RectTransform notebook = CreatePanel("Investigation Notebook", split, new Color32(234, 244, 248, 255), InvestigationV2Theme.CardRadius);
            EnsureOutline(notebook.gameObject, new Color32(145, 177, 194, 230), new Vector2(2f, -2f));
            Shadow paperShadow = notebook.gameObject.AddComponent<Shadow>();
            paperShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            paperShadow.effectDistance = new Vector2(4f, -4f);
            paperShadow.useGraphicAlpha = true;
            Text notebookTitle = CreateText(
                "Notebook Title",
                notebook,
                $"MY NOTEBOOK · {CountVisibleNotebookObservations()}",
                18,
                FontStyle.Bold,
                InvestigationV2Theme.PaperMuted,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.DataFont);
            Anchor(notebookTitle.rectTransform, 0f, 0.86f, 1f, 1f, 16f, 0f, -12f, -8f);

            RectTransform notebookEntries = CreateNotebookEntryScroll(notebook);
            RenderNotebookEntries(notebookEntries);

            int remaining = Mathf.Max(0, caseDefinition.MinimumObserveDiscoveries - state.DiscoveredObservationIds.Count);
            string nextLabel = remaining == 0 ? "Try causes →" : $"Record {remaining} more";
            Button next = CreateButton("Continue To Simulate", notebook, nextLabel, ButtonVisualStyle.PaperPrimary, () => setPhase?.Invoke(InvestigationV2Phase.Simulate), out _);
            next.GetComponent<LayoutElement>().ignoreLayout = true;
            Anchor(next.GetComponent<RectTransform>(), 0.25f, 0f, 0.75f, 0f, 0f, 8f, 0f, 58f);
            next.interactable = remaining == 0;
        }

        private RectTransform CreateNotebookEntryScroll(RectTransform notebook)
        {
            RectTransform scrollRoot = CreatePanel("Notebook Entry Scroll", notebook, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(scrollRoot, 0f, 0f, 1f, 1f, 12f, 68f, -12f, -58f);
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
                historical ? InvestigationV2Theme.MapSurfaceHistorical : InvestigationV2Theme.MapSurface,
                InvestigationV2Theme.CardRadius);

            Text title = CreateText(
                "Survey Title",
                map,
                historical ? "20 YEARS AGO" : "TODAY",
                18,
                FontStyle.Bold,
                historical ? InvestigationV2Theme.TextSecondary : InvestigationV2Theme.Primary,
                TextAnchor.UpperLeft,
                InvestigationV2Theme.DisplayFont);
            Anchor(title.rectTransform, 0f, 0.84f, 1f, 1f, 14f, 0f, -12f, -10f);

            RectTransform plotArea = CreatePanel("Seamount Plot Area", map, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(plotArea, 0.08f, 0.08f, 0.92f, 0.86f, 0f, 0f, 0f, 0f);
            RectTransform visualClip = CreatePanel("Seamount Visual Clip", plotArea, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(visualClip, 0f, 0f, 0f, 0f);
            visualClip.gameObject.AddComponent<RectMask2D>();
            CreateSeamountVisual(visualClip);
            CreateDepthLabel(map, "SHALLOW", 0.66f);
            CreateDepthLabel(map, "MID", 0.38f);
            CreateDepthLabel(map, "DEEP", 0.10f);

            for (int speciesIndex = 0; speciesIndex < caseDefinition.Species.Count; speciesIndex++)
            {
                InvestigationV2SpeciesDefinition species = caseDefinition.Species[speciesIndex];
                if (species != null) CreateSpeciesMarker(plotArea, species, era);
            }
        }

        private void CreateSeamountVisual(RectTransform plotArea)
        {
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
                aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                aspect.aspectRatio = seamountSprite.rect.width / seamountSprite.rect.height;
            }
            else
            {
                InvestigationV2SeamountGraphic fallback = CreateGraphic<InvestigationV2SeamountGraphic>("Seamount Silhouette Fallback", plotArea);
                Stretch(fallback.rectTransform, 0f, 0f, 0f, 0f);
                fallback.color = Color.white;
            }

            InvestigationV2SeamountFogGraphic fog = CreateGraphic<InvestigationV2SeamountFogGraphic>("Seamount Foot Fog", plotArea);
            Anchor(fog.rectTransform, 0f, 0f, 1f, 0.24f, -8f, -4f, 8f, 0f);
            fog.color = InvestigationV2Theme.Primary;
        }

        private void CreateDepthLabel(RectTransform map, string value, float normalizedY)
        {
            Text label = CreateText($"Depth {value}", map, value, 12, FontStyle.Bold, InvestigationV2Theme.TextMuted, TextAnchor.UpperLeft, InvestigationV2Theme.DataFont);
            label.rectTransform.anchorMin = new Vector2(0f, normalizedY);
            label.rectTransform.anchorMax = new Vector2(1f, normalizedY);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.offsetMin = new Vector2(12f, -32f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);
            RectTransform line = CreatePanel($"Depth Line {value}", map, new Color32(145, 177, 194, 42), 0f);
            line.anchorMin = new Vector2(0f, normalizedY);
            line.anchorMax = new Vector2(1f, normalizedY);
            line.offsetMin = new Vector2(0f, -1f);
            line.offsetMax = new Vector2(0f, 1f);
        }

        private void CreateSpeciesMarker(RectTransform map, InvestigationV2SpeciesDefinition species, SurveyEra era)
        {
            InvestigationV2ObservationDefinition observation = FindObserveObservationForSpecies(species.SpeciesId);
            bool historical = era == SurveyEra.Historical;
            bool anomaly = !historical && observation != null && observation.ClaimType != ObservationClaimType.MatchesBaseline;
            string detail = historical
                ? "Detected"
                : observation != null ? ConciseObservationLabel(observation) : "No current observation";

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
            Vector2 comparisonPosition = species.MapPosition;
            rect.anchorMin = comparisonPosition;
            rect.anchorMax = comparisonPosition;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = new Vector2(112f, 88f);
            rect.anchoredPosition = Vector2.zero;
            LayoutElement markerLayout = marker.GetComponent<LayoutElement>();
            markerLayout.ignoreLayout = true;

            Image background = marker.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0f);
            Outline hitOutline = marker.GetComponent<Outline>();
            if (hitOutline != null) hitOutline.effectColor = new Color(0f, 0f, 0f, 0f);
            RectTransform artwork = CreateSpeciesArtwork(
                "Species Artwork",
                marker.transform,
                species,
                anomaly ? InvestigationV2Theme.Accent : InvestigationV2Theme.Primary,
                !historical && observation != null && observation.ClaimType == ObservationClaimType.NotDetected);
            bool showGroup = !historical
                && observation != null
                && observation.ClaimType == ObservationClaimType.ChangedDepthOrDistribution
                && species.Icon != null;
            if (showGroup)
            {
                Anchor(artwork, 0.30f, 0.45f, 0.70f, 0.98f, 0f, 0f, 0f, -2f);
                CreateMapGroupMember(marker.transform, species.Icon, "Group Member Left", 0.04f, 0.48f, 0.38f, 0.88f);
                CreateMapGroupMember(marker.transform, species.Icon, "Group Member Right", 0.62f, 0.50f, 0.96f, 0.90f);
            }
            else
            {
                Anchor(artwork, 0f, 0.44f, 1f, 0.98f, 24f, 0f, -24f, -2f);
            }

            if (!historical && observation != null && observation.ClaimType == ObservationClaimType.NotDetected)
            {
                InvestigationV2MissingSignalGraphic missing = CreateGraphic<InvestigationV2MissingSignalGraphic>("Missing Signal", marker.transform);
                missing.color = InvestigationV2Theme.TextMuted;
                Anchor(missing.rectTransform, 0.18f, 0.43f, 0.82f, 0.98f, 0f, 0f, 0f, -2f);
            }

            Text name = CreateText("Species Name", marker.transform, species.DisplayName, 13, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleCenter, InvestigationV2Theme.DisplayFont);
            Anchor(name.rectTransform, 0f, 0.31f, 1f, 0.51f, 6f, 0f, -6f, 0f);
            EnsureOutline(name.gameObject, new Color32(1, 8, 16, 245), new Vector2(1.5f, -1.5f));
            Color stateColor = !historical && observation != null && observation.ClaimType == ObservationClaimType.NotDetected
                ? InvestigationV2Theme.Danger
                : !historical && observation != null && observation.ClaimType == ObservationClaimType.ChangedDepthOrDistribution
                    ? InvestigationV2Theme.Accent
                    : InvestigationV2Theme.TextSecondary;
            Text stateLabel = CreateText("Observation", marker.transform, detail, 12, FontStyle.Bold, stateColor, TextAnchor.UpperCenter, InvestigationV2Theme.BodyFont);
            Anchor(stateLabel.rectTransform, 0f, 0.02f, 1f, 0.31f, 6f, 1f, -6f, 0f);
            EnsureOutline(stateLabel.gameObject, new Color32(1, 8, 16, 245), new Vector2(1.5f, -1.5f));

            InvestigationV2HoverTooltipTrigger tooltipTrigger = marker.gameObject.AddComponent<InvestigationV2HoverTooltipTrigger>();
            tooltipTrigger.Configure(
                1f,
                () => ShowSpeciesTooltip(rect, species),
                HideSpeciesTooltip);

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
            InvestigationV2SpeciesDefinition species,
            SurveyEra era,
            InvestigationV2ObservationDefinition observation)
        {
            bool historical = era == SurveyEra.Historical;
            if (historical || observation == null)
            {
                ShowSpeciesTooltip(marker, species);
                return;
            }

            pendingTappedSpeciesId = species.SpeciesId;
            pendingTappedSpeciesHistorical = false;
            discoverObservation?.Invoke(observation.EvidenceId);
        }

        private void ShowPendingTappedSpeciesTooltip()
        {
            if (string.IsNullOrEmpty(pendingTappedSpeciesId)) return;
            RectTransform marker = pendingTappedSpeciesMarker;
            InvestigationV2SpeciesDefinition species = pendingTappedSpecies;
            pendingTappedSpeciesId = string.Empty;
            pendingTappedSpeciesMarker = null;
            pendingTappedSpecies = null;
            ShowSpeciesTooltip(marker, species);
        }

        private int CountVisibleNotebookObservations()
        {
            int count = 0;
            for (int index = 0; index < state.DiscoveredObservationIds.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(state.DiscoveredObservationIds[index]);
                if (observation != null && observation.Source != ObservationSource.Methodology) count++;
            }
            return count;
        }

        private void RenderNotebookEntries(Transform parent)
        {
            int shown = 0;
            for (int index = 0; index < state.DiscoveredObservationIds.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(state.DiscoveredObservationIds[index]);
                if (observation == null || observation.Source == ObservationSource.Methodology) continue;
                RectTransform item = CreatePanel($"Notebook {observation.EvidenceId}", parent, new Color(0f, 0f, 0f, 0f), 0f);
                AddLayout(item, 58f, 1f);
                Color evidenceColor = NotebookStateColor(observation.ClaimType);
                RectTransform bullet = CreatePanel("Evidence Bullet", item, InvestigationV2Theme.Accent, 6f);
                Anchor(bullet, 0f, 0.56f, 0f, 0.56f, 4f, -6f, 16f, 6f);
                EnsureOutline(bullet.gameObject, new Color32(191, 80, 51, 220), new Vector2(1f, -1f));
                InvestigationV2SpeciesDefinition species = caseDefinition.FindSpecies(observation.RelatedSpeciesId);
                string stateLabel = NotebookStateLabel(observation.ClaimType);
                string stateColor = ColorUtility.ToHtmlStringRGB(evidenceColor);
                Text title = CreateText(
                    "Observation",
                    item,
                    $"{species?.DisplayName ?? observation.DisplayName} <b><color=#{stateColor}>{stateLabel}</color></b>",
                    14,
                    FontStyle.Normal,
                    InvestigationV2Theme.PaperInk,
                    TextAnchor.LowerLeft,
                    InvestigationV2Theme.DisplayFont);
                title.supportRichText = true;
                Anchor(title.rectTransform, 0f, 0.43f, 1f, 1f, 24f, 0f, -8f, 0f);
                Text source = CreateText("Source", item, $"{ObservationSourceDisplayName(observation.Source)} · {observation.Confidence} confidence", 10, FontStyle.Normal, InvestigationV2Theme.PaperMuted, TextAnchor.UpperLeft, InvestigationV2Theme.DataFont);
                Anchor(source.rectTransform, 0f, 0f, 1f, 0.46f, 24f, 0f, -8f, 0f);
                RectTransform paperLine = CreatePanel("Paper Line", item, new Color32(179, 214, 225, 210), 0f);
                Anchor(paperLine, 0.04f, 0f, 0.96f, 0f, 0f, 0f, 0f, 2f);
                shown++;
            }
            if (shown == 0)
            {
                Text empty = CreateText("Notebook Empty", parent, "Select unusual results on the current survey map. Important observations are recorded automatically.", 14, FontStyle.Normal, InvestigationV2Theme.PaperMuted, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
                AddLayout(empty.rectTransform, 92f, 1f);
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

        private void ShowSpeciesTooltip(RectTransform marker, InvestigationV2SpeciesDefinition species)
        {
            if (marker == null || species == null) return;
            HideSpeciesTooltip();

            RectTransform overlay = GetComponent<RectTransform>();
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, marker.TransformPoint(marker.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screenPoint, null, out Vector2 localPoint);
            bool placeRight = screenPoint.x < Screen.width * 0.55f;

            speciesTooltip = CreatePanel("Species Facts Tooltip", overlay, new Color32(27, 82, 115, 252), InvestigationV2Theme.SmallRadius);
            speciesTooltip.anchorMin = Vector2.one * 0.5f;
            speciesTooltip.anchorMax = Vector2.one * 0.5f;
            speciesTooltip.pivot = new Vector2(placeRight ? 0f : 1f, 0.5f);
            speciesTooltip.sizeDelta = new Vector2(360f, 188f);
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
            outline.effectColor = InvestigationV2Theme.Primary;
            outline.effectDistance = new Vector2(2f, -2f);

            Text eyebrow = CreateText("Tooltip Eyebrow", speciesTooltip, "SPECIES FACTS", 11, FontStyle.Bold, InvestigationV2Theme.Primary, TextAnchor.UpperLeft, InvestigationV2Theme.DataFont);
            Anchor(eyebrow.rectTransform, 0f, 0.80f, 1f, 1f, 16f, 0f, -12f, -12f);
            Text title = CreateText("Tooltip Title", speciesTooltip, species.DisplayName, 19, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.UpperLeft, InvestigationV2Theme.DisplayFont);
            Anchor(title.rectTransform, 0f, 0.61f, 1f, 0.84f, 16f, 0f, -12f, 0f);
            Text description = CreateText("Tooltip Description", speciesTooltip, species.Description, 13, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
            Anchor(description.rectTransform, 0f, 0.28f, 1f, 0.63f, 16f, 0f, -12f, -2f);
            Text details = CreateText(
                "Tooltip Details",
                speciesTooltip,
                $"DEPTH  {JoinDepths(species)}\nTEMPERATURE  {species.TemperaturePreference}",
                11,
                FontStyle.Normal,
                InvestigationV2Theme.TextMuted,
                TextAnchor.UpperLeft,
                InvestigationV2Theme.DataFont);
            Anchor(details.rectTransform, 0f, 0f, 1f, 0.30f, 16f, 10f, -12f, 0f);
        }

        private void HideSpeciesTooltip()
        {
            if (speciesTooltip == null) return;
            GameObject tooltipObject = speciesTooltip.gameObject;
            speciesTooltip = null;
            tooltipObject.SetActive(false);
            if (Application.isPlaying) Destroy(tooltipObject);
            else DestroyImmediate(tooltipObject);
        }

        private InvestigationV2ObservationDefinition FindObserveObservationForSpecies(string speciesId)
        {
            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.Observations[index];
                if (observation != null
                    && observation.UnlockStage == EvidenceUnlockStage.Observe
                    && string.Equals(observation.RelatedSpeciesId, speciesId, StringComparison.Ordinal))
                {
                    return observation;
                }
            }
            return null;
        }

        private static string JoinDepths(InvestigationV2SpeciesDefinition species)
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

        private static string ConciseObservationLabel(InvestigationV2ObservationDefinition observation)
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
                case ObservationClaimType.NotDetected: return new Color32(165, 65, 58, 255);
                case ObservationClaimType.ChangedDepthOrDistribution: return new Color32(169, 75, 43, 255);
                case ObservationClaimType.MatchesBaseline: return new Color32(28, 105, 99, 255);
                case ObservationClaimType.NewDetection: return new Color32(27, 109, 138, 255);
                case ObservationClaimType.EnvironmentalReading: return new Color32(27, 109, 138, 255);
                case ObservationClaimType.PhysicalObservation: return new Color32(28, 105, 99, 255);
                default: return InvestigationV2Theme.PaperMuted;
            }
        }

    }
}
