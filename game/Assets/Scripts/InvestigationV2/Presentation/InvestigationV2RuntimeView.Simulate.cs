using System;
using System.Collections;
using System.Collections.Generic;
using EDNA.Investigation.V2.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    public sealed partial class InvestigationV2RuntimeView
    {
        private sealed class PredictionAnimationTarget
        {
            public RectTransform Artwork;
            public CanvasGroup ArtworkGroup;
            public Text StateLabel;
            public Text Indicator;
            public CanvasGroup IndicatorGroup;
            public Image[] CrowdMembers;
            public PredictionState State;
            public Color StateColor;
        }

        private void RenderSimulate()
        {
            if (string.IsNullOrEmpty(selectedThreatId)) selectedThreatId = FindDefaultThreatId();
            ThreatSimulationDefinition selectedThreat = caseDefinition.FindThreat(selectedThreatId);
            if (selectedThreat == null && caseDefinition.Threats.Count > 0)
            {
                selectedThreat = caseDefinition.Threats[0];
                selectedThreatId = selectedThreat.ThreatId;
            }

            CreateHeading(
                "Test an explanation",
                "Run one scenario and compare its food-web prediction with the real survey.");

            SimulationResult simulation = selectedThreat != null ? state.FindSimulation(selectedThreat.ThreatId) : null;
            float availableHeight = contentPanel == null ? 540f : contentPanel.rect.height;
            float workspaceHeight = Mathf.Clamp(availableHeight - 50f, 342f, 496f);
            RectTransform workspace = new GameObject("Simulate Workspace", typeof(RectTransform), typeof(InvestigationV2ResponsiveSplitLayout)).GetComponent<RectTransform>();
            workspace.SetParent(contentRoot, false);
            InvestigationV2ResponsiveSplitLayout workspaceLayout = workspace.GetComponent<InvestigationV2ResponsiveSplitLayout>();
            workspaceLayout.padding = new RectOffset(0, 0, 0, 0);
            workspaceLayout.Configure(0.5f, 12f, 960f, workspaceHeight, workspaceHeight);

            RectTransform modelColumn = CreatePanel("Simulation Models", workspace, InvestigationV2Theme.SurfaceQuiet, InvestigationV2Theme.SmallRadius);
            VerticalLayoutGroup modelColumnLayout = modelColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            modelColumnLayout.padding = new RectOffset(8, 8, 8, 8);
            modelColumnLayout.spacing = 8f;
            modelColumnLayout.childControlWidth = true;
            modelColumnLayout.childControlHeight = true;
            modelColumnLayout.childForceExpandWidth = true;
            modelColumnLayout.childForceExpandHeight = false;

            RectTransform threatGrid = CreatePanel("Threat Choices", modelColumn, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(threatGrid, 68f, 1f);
            HorizontalLayoutGroup threatLayout = threatGrid.gameObject.AddComponent<HorizontalLayoutGroup>();
            threatLayout.spacing = 6f;
            threatLayout.childAlignment = TextAnchor.MiddleCenter;
            threatLayout.childControlWidth = true;
            threatLayout.childControlHeight = true;
            threatLayout.childForceExpandWidth = true;
            threatLayout.childForceExpandHeight = true;
            for (int index = 0; index < caseDefinition.Threats.Count; index++)
            {
                ThreatSimulationDefinition threat = caseDefinition.Threats[index];
                if (threat != null) CreateThreatButton(threatGrid, threat);
            }

            float modelHeight = Mathf.Max(250f, workspaceHeight - 92f);
            RectTransform modelPanel = CreateSection("Model Workspace", modelColumn, InvestigationV2Theme.Deep, modelHeight, InvestigationV2Theme.SmallRadius);
            VerticalLayoutGroup modelLayout = modelPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            modelLayout.padding = new RectOffset(8, 8, 8, 8);
            modelLayout.spacing = 6f;
            modelLayout.childControlWidth = true;
            modelLayout.childControlHeight = true;
            modelLayout.childForceExpandWidth = true;
            modelLayout.childForceExpandHeight = false;

            Text modelTitle = CreateText(
                "Model Title",
                modelPanel,
                selectedThreat == null ? "Choose a cause" : $"{selectedThreat.DisplayName} · model prediction",
                16,
                FontStyle.Bold,
                InvestigationV2Theme.TextPrimary,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.DisplayFont);
            AddLayout(modelTitle.rectTransform, 24f, 1f);

            RectTransform comparisonColumn = CreatePanel("Comparison Workspace", workspace, InvestigationV2Theme.SurfaceQuiet, InvestigationV2Theme.SmallRadius);
            VerticalLayoutGroup comparisonLayout = comparisonColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            comparisonLayout.padding = new RectOffset(10, 10, 10, 10);
            comparisonLayout.spacing = 8f;
            comparisonLayout.childControlWidth = true;
            comparisonLayout.childControlHeight = true;
            comparisonLayout.childForceExpandWidth = true;
            comparisonLayout.childForceExpandHeight = false;

            if (simulation == null)
            {
                RenderModelPreview(modelPanel, selectedThreat);
                RenderComparisonPlaceholder(comparisonColumn);
            }
            else
            {
                RenderSimulationVisualization(modelPanel, simulation);
                RenderSimulationComparison(comparisonColumn, simulation, workspaceHeight);
            }

            Button back = CreateButton("Back To Observe", footerLeft, "← Back to notebook", ButtonVisualStyle.Tertiary, () => setPhase?.Invoke(InvestigationV2Phase.Observe), out _);
            ConfigureCompactFooterButton(back, 160f);

            InvestigationV2Readiness readiness = new InvestigationV2ConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
            if (readiness.CanEnterProvisional)
            {
                Button report = CreateButton("Write Provisional Report", footerRight, "Write my first idea →", ButtonVisualStyle.Primary, () => submitProvisional?.Invoke(selectedThreatId), out _);
                ConfigureCompactFooterButton(report, 210f);
            }
            else
            {
                RectTransform gate = CreatePanel("Report Gate Hint", footerRight, new Color32(14, 51, 72, 225), 10f);
                gate.sizeDelta = new Vector2(520f, 30f);
                LayoutElement gateLayout = gate.gameObject.AddComponent<LayoutElement>();
                gateLayout.minWidth = 420f;
                gateLayout.preferredWidth = 520f;
                gateLayout.minHeight = 30f;
                gateLayout.preferredHeight = 30f;
                AddPanelAccent(gate, InvestigationV2Theme.Primary, 2f);
                Text guidance = CreateText(
                    "Comparison Gate",
                    gate,
                    simulation == null
                        ? "TO REPORT · Run a model, then compare its predictions with evidence"
                        : $"TO REPORT · {BuildSimulationGateLabel()}",
                    12,
                    FontStyle.Bold,
                    InvestigationV2Theme.TextSecondary,
                    TextAnchor.MiddleCenter,
                    InvestigationV2Theme.BodyFont);
                Stretch(guidance.rectTransform, 10f, 2f, -10f, -2f);
            }
        }

        private static void ConfigureCompactFooterButton(Button button, float width)
        {
            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = 28f;
            layout.preferredHeight = 30f;
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 30f);
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.fontSize = 12;
        }

        private void RenderModelPreview(RectTransform parent, ThreatSimulationDefinition threat)
        {
            RectTransform preview = CreatePanel("Scenario Preview", parent, InvestigationV2Theme.Surface, InvestigationV2Theme.SmallRadius);
            LayoutElement previewLayout = AddLayout(preview, 156f, 1f);
            previewLayout.flexibleHeight = 1f;

            RectTransform artwork = CreateThreatArtwork("Scenario Artwork", preview, threat);
            Anchor(artwork, 0f, 0f, 0.16f, 1f, 14f, 16f, -2f, -16f);

            Text summary = CreateText(
                "Scenario Summary",
                preview,
                threat == null ? "Select one of the four candidate causes." : threat.Summary,
                15,
                FontStyle.Normal,
                InvestigationV2Theme.TextSecondary,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.BodyFont);
            Anchor(summary.rectTransform, 0.17f, 0f, 0.73f, 1f, 8f, 12f, -12f, -12f);

            Button run = CreateButton(
                "Run Selected Model",
                preview,
                "Run simulation",
                ButtonVisualStyle.Primary,
                () => runThreat?.Invoke(selectedThreatId),
                out _);
            Anchor(run.GetComponent<RectTransform>(), 0.75f, 0.22f, 1f, 0.78f, 4f, 0f, -14f, 0f);
            run.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        private void CreateThreatButton(RectTransform parent, ThreatSimulationDefinition threat)
        {
            Button button = CreateButton(
                $"Threat {threat.ThreatId}",
                parent,
                string.Empty,
                ButtonVisualStyle.Choice,
                () =>
                {
                    selectedThreatId = threat.ThreatId;
                    selectedPredictionSpeciesId = string.Empty;
                    selectedObservationId = string.Empty;
                    RefreshPresentationOnly();
                },
                out Text hiddenLabel);
            hiddenLabel.gameObject.SetActive(false);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.minHeight = 68f;
            layout.preferredHeight = 68f;
            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            Image background = button.GetComponent<Image>();
            background.color = string.Equals(selectedThreatId, threat.ThreatId, StringComparison.Ordinal)
                ? new Color32(70, 48, 48, 255)
                : InvestigationV2Theme.SurfaceQuiet;
            if (string.Equals(selectedThreatId, threat.ThreatId, StringComparison.Ordinal))
            {
                EnsureOutline(button.gameObject, InvestigationV2Theme.Accent, new Vector2(2f, -2f));
            }

            RectTransform icon = CreateThreatArtwork("Threat Artwork", button.transform, threat);
            Anchor(icon, 0f, 0.08f, 0.30f, 1f, 6f, 6f, -2f, -6f);
            Text title = CreateText("Threat Name", button.transform, threat.DisplayName, 12, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
            Anchor(title.rectTransform, 0.30f, 0f, 0.88f, 1f, 2f, 0f, -2f, 0f);
            if (state.HasTriedThreat(threat.ThreatId))
            {
                Image tried = CreateStatusIcon("Threat Tried", button.transform, InvestigationV2StatusIconLibrary.Check, InvestigationV2Theme.Success);
                Anchor(tried.rectTransform, 0.88f, 0.58f, 1f, 0.94f, 0f, 0f, -5f, 0f);
            }
        }

        private void RenderComparisonPlaceholder(RectTransform parent)
        {
            Text heading = CreateText(
                "Prediction Versus Survey",
                parent,
                "PREDICTION VS SURVEY",
                18,
                FontStyle.Bold,
                InvestigationV2Theme.TextPrimary,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.DisplayFont);
            AddLayout(heading.rectTransform, 30f, 1f);

            RectTransform placeholder = CreatePanel("Comparison Placeholder", parent, InvestigationV2Theme.Surface, InvestigationV2Theme.SmallRadius);
            LayoutElement placeholderLayout = AddLayout(placeholder, 230f, 1f);
            placeholderLayout.flexibleHeight = 1f;
            Text title = CreateText("Placeholder Title", placeholder, "Run the selected model", 20, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleCenter, InvestigationV2Theme.DisplayFont);
            Anchor(title.rectTransform, 0.08f, 0.46f, 0.92f, 0.66f, 0f, 0f, 0f, 0f);
            Text detail = CreateText("Placeholder Detail", placeholder, "Its food-web prediction will appear on the left. Then compare one species prediction with the survey evidence here.", 14, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperCenter, InvestigationV2Theme.BodyFont);
            Anchor(detail.rectTransform, 0.12f, 0.20f, 0.88f, 0.47f, 0f, 0f, 0f, 0f);
        }

        private void RenderSimulationVisualization(RectTransform parent, SimulationResult simulation)
        {
            RectTransform environment = CreatePanel("Environmental Predictions", parent, InvestigationV2Theme.Surface, InvestigationV2Theme.SmallRadius);
            AddLayout(environment, 44f, 1f);
            HorizontalLayoutGroup environmentLayout = environment.gameObject.AddComponent<HorizontalLayoutGroup>();
            environmentLayout.padding = new RectOffset(4, 4, 4, 4);
            environmentLayout.spacing = 4f;
            environmentLayout.childControlWidth = true;
            environmentLayout.childControlHeight = true;
            environmentLayout.childForceExpandWidth = true;
            environmentLayout.childForceExpandHeight = true;
            CreateModelIndicatorChip(environment, "TEMP", CompactIndicatorValue(simulation.TemperaturePrediction), InvestigationV2Theme.Primary);
            CreateModelIndicatorChip(environment, "SEAFLOOR", CompactIndicatorValue(simulation.SeafloorPrediction), InvestigationV2Theme.Success);
            CreateModelIndicatorChip(environment, "LOOK FOR", CompactIndicatorValue(simulation.PhysicalConfirmation), InvestigationV2Theme.Accent);

            RectTransform chain = CreatePanel("Food Web Prediction", parent, InvestigationV2Theme.SurfaceQuiet, InvestigationV2Theme.SmallRadius);
            AddLayout(chain, 104f, 1f);
            Text chainLabel = CreateText("Food Web Heading", chain, "FOOD WEB · SELECT A SPECIES", 11, FontStyle.Bold, InvestigationV2Theme.Primary, TextAnchor.UpperLeft, InvestigationV2Theme.DataFont);
            Anchor(chainLabel.rectTransform, 0f, 1f, 1f, 1f, 10f, -18f, -8f, -2f);
            chainLabel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            HorizontalLayoutGroup chainLayout = chain.gameObject.AddComponent<HorizontalLayoutGroup>();
            chainLayout.padding = new RectOffset(8, 8, 20, 6);
            chainLayout.spacing = 6f;
            chainLayout.childAlignment = TextAnchor.MiddleCenter;
            chainLayout.childControlWidth = false;
            chainLayout.childControlHeight = false;
            chainLayout.childForceExpandWidth = false;
            chainLayout.childForceExpandHeight = false;

            List<PredictionAnimationTarget> foodWebTargets = new List<PredictionAnimationTarget>();
            List<RectTransform> indicatorNodes = new List<RectTransform>();
            string[] foodWebSpeciesIds = { "shark", "tuna", "krill" };
            for (int index = 0; index < foodWebSpeciesIds.Length; index++)
            {
                string speciesId = foodWebSpeciesIds[index];
                SimulationPrediction prediction = simulation.FindPrediction(speciesId);
                InvestigationV2SpeciesDefinition species = caseDefinition.FindSpecies(speciesId);
                if (species == null || prediction == null) continue;
                Button node = CreatePredictionButton(chain, species, prediction, 164f, 78f, true);
                foodWebTargets.Add(CreatePredictionAnimationTarget(node, prediction));
                if (index < foodWebSpeciesIds.Length - 1) CreateFoodWebArrow(chain, 24f, 78f, 26);
            }

            RectTransform indicators = CreatePanel("Reference Indicators", parent, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(indicators, 44f, 1f);
            HorizontalLayoutGroup indicatorLayout = indicators.gameObject.AddComponent<HorizontalLayoutGroup>();
            indicatorLayout.spacing = 6f;
            indicatorLayout.childAlignment = TextAnchor.MiddleCenter;
            indicatorLayout.childControlWidth = false;
            indicatorLayout.childControlHeight = false;
            indicatorLayout.childForceExpandWidth = false;
            indicatorLayout.childForceExpandHeight = false;
            Text indicatorLabel = CreateText("Indicator Heading", indicators, "BENTHIC\nCHECK", 10, FontStyle.Bold, InvestigationV2Theme.TextMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            indicatorLabel.rectTransform.sizeDelta = new Vector2(92f, 44f);
            LayoutElement indicatorLabelLayout = indicatorLabel.gameObject.AddComponent<LayoutElement>();
            indicatorLabelLayout.minWidth = 92f;
            indicatorLabelLayout.preferredWidth = 92f;
            indicatorLabelLayout.minHeight = 44f;
            indicatorLabelLayout.preferredHeight = 44f;
            string[] indicatorIds = { "sea_star", "mussel" };
            for (int index = 0; index < indicatorIds.Length; index++)
            {
                SimulationPrediction prediction = simulation.FindPrediction(indicatorIds[index]);
                InvestigationV2SpeciesDefinition species = caseDefinition.FindSpecies(indicatorIds[index]);
                if (species == null || prediction == null) continue;
                Button node = CreatePredictionButton(indicators, species, prediction, 220f, 44f, true);
                indicatorNodes.Add(node.GetComponent<RectTransform>());
            }

            if (!animatedThreatIds.Contains(simulation.ThreatId))
            {
                animatedThreatIds.Add(simulation.ThreatId);
                if (!InvestigationV2MotionSettings.ReducedMotion)
                {
                    StartCoroutine(AnimateSimulationSequence(foodWebTargets, indicatorNodes));
                }
            }
        }

        private void CreateModelIndicatorChip(Transform parent, string label, string value, Color accentColor)
        {
            RectTransform chip = CreatePanel($"Model Indicator {label}", parent, new Color32(8, 36, 54, 215), 8f);
            EnsureOutline(chip.gameObject, InvestigationV2Theme.BorderSoft, new Vector2(1f, -1f));
            RectTransform accent = CreatePanel("Indicator Accent", chip, accentColor, 1f);
            Anchor(accent, 0f, 0.18f, 0f, 0.82f, 0f, 0f, 3f, 0f);
            Text title = CreateText("Indicator Label", chip, label, 10, FontStyle.Bold, accentColor, TextAnchor.LowerLeft, InvestigationV2Theme.DataFont);
            Anchor(title.rectTransform, 0f, 0.52f, 1f, 1f, 7f, 0f, -4f, 0f);
            Text detail = CreateText("Indicator Value", chip, value, 11, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
            Anchor(detail.rectTransform, 0f, 0f, 1f, 0.52f, 7f, 0f, -4f, 0f);
        }

        private static string CompactIndicatorValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Unknown";
            string lower = value.ToLowerInvariant();
            if (lower.Contains("historical range") || lower.Contains("remain normal")) return "Historical range";
            if (lower.Contains("range exceeded") || lower.Contains("exceeded")) return "Range exceeded";
            if (lower.Contains("disturbed") || lower.Contains("damaged"))
                return lower.Contains("trawl") ? "Trawl marks / damage" : "Disturbed / damaged";
            if (lower.Contains("intact")) return "Intact";
            if (lower.Contains("plastic") || lower.Contains("contamination")) return "Plastic / contamination";
            if (lower.Contains("no fishing gear") || lower.Contains("not required")) return "No gear required";
            if (lower.Contains("fishing gear")) return "Fishing gear";
            return value.Length <= 24 ? value.TrimEnd('.') : value.Substring(0, 23).TrimEnd() + "…";
        }

        private void RenderSimulationComparison(RectTransform parent, SimulationResult simulation, float workspaceHeight)
        {
            Text comparisonHeading = CreateText(
                "Prediction Versus Survey",
                parent,
                "PREDICTION VS SURVEY",
                18,
                FontStyle.Bold,
                InvestigationV2Theme.TextPrimary,
                TextAnchor.MiddleLeft,
                InvestigationV2Theme.DisplayFont);
            AddLayout(comparisonHeading.rectTransform, 30f, 1f);

            float pairingHeight = Mathf.Clamp(workspaceHeight - 124f, 218f, 346f);
            RectTransform pairing = new GameObject("Prediction Observation Pairing", typeof(RectTransform), typeof(InvestigationV2ResponsiveSplitLayout)).GetComponent<RectTransform>();
            pairing.SetParent(parent, false);
            InvestigationV2ResponsiveSplitLayout split = pairing.GetComponent<InvestigationV2ResponsiveSplitLayout>();
            split.padding = new RectOffset(0, 0, 0, 0);
            split.Configure(0.5f, 8f, 520f, pairingHeight, pairingHeight);

            RectTransform predictionPanel = CreatePanel("Prediction Selection", pairing, new Color32(13, 55, 76, 255), InvestigationV2Theme.SmallRadius);
            AddPanelAccent(predictionPanel, InvestigationV2Theme.Primary);
            RenderSelectedPredictionPanel(predictionPanel, simulation);
            RectTransform observationPanel = CreatePanel("Observation Selection", pairing, InvestigationV2Theme.Surface, InvestigationV2Theme.SmallRadius);
            AddPanelAccent(observationPanel, InvestigationV2Theme.Accent);
            RenderCandidateObservations(observationPanel, simulation);

            RectTransform judgementRow = CreatePanel("Judgement Row", parent, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(judgementRow, 58f, 1f);
            HorizontalLayoutGroup judgementLayout = judgementRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            judgementLayout.spacing = 8f;
            judgementLayout.childAlignment = TextAnchor.MiddleCenter;
            judgementLayout.childControlWidth = false;
            judgementLayout.childControlHeight = true;
            judgementLayout.childForceExpandWidth = false;
            judgementLayout.childForceExpandHeight = true;
            CreateJudgementButton(judgementRow, "It matches", ComparisonJudgement.Match, InvestigationV2StatusIconLibrary.Check, InvestigationV2Theme.Success, 168f);
            CreateJudgementButton(judgementRow, "It doesn't match", ComparisonJudgement.Mismatch, InvestigationV2StatusIconLibrary.Cross, InvestigationV2Theme.Danger, 168f);
            CreateJudgementButton(judgementRow, "Not enough evidence", ComparisonJudgement.NotEnoughEvidence, InvestigationV2StatusIconLibrary.Question, InvestigationV2Theme.Unknown, 184f);
        }

        private void CreateFoodWebArrow(Transform parent, float width = 52f, float height = 158f, int fontSize = 42)
        {
            Text arrow = CreateText(
                "Food Web Arrow",
                parent,
                "→",
                fontSize,
                FontStyle.Bold,
                InvestigationV2Theme.Primary,
                TextAnchor.MiddleCenter,
                InvestigationV2Theme.DisplayFont);
            arrow.horizontalOverflow = HorizontalWrapMode.Overflow;
            arrow.verticalOverflow = VerticalWrapMode.Overflow;
            arrow.rectTransform.sizeDelta = new Vector2(width, height);
            LayoutElement layout = arrow.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private Button CreatePredictionButton(
            RectTransform parent,
            InvestigationV2SpeciesDefinition species,
            SimulationPrediction prediction,
            float width,
            float height,
            bool compact)
        {
            Button button = CreateButton(
                $"Prediction {prediction.SpeciesId}",
                parent,
                string.Empty,
                ButtonVisualStyle.Choice,
                () =>
                {
                    selectedPredictionSpeciesId = prediction.SpeciesId;
                    PredictionComparisonRecord saved = state.FindComparison(selectedThreatId, prediction.SpeciesId);
                    selectedObservationId = saved != null && saved.CountsTowardProgress ? saved.EvidenceId : string.Empty;
                    RefreshPresentationOnly();
                },
                out Text hidden);
            hidden.gameObject.SetActive(false);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            layout.minWidth = width;
            layout.preferredWidth = width;
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            bool selected = string.Equals(selectedPredictionSpeciesId, prediction.SpeciesId, StringComparison.Ordinal);
            button.GetComponent<Image>().color = selected ? InvestigationV2Theme.SurfaceRaised : InvestigationV2Theme.Surface;
            Color stateColor = PredictionStateColor(prediction.PredictedState);
            PredictionComparisonRecord comparisonRecord = state.FindComparison(selectedThreatId, prediction.SpeciesId);
            bool comparisonLocked = comparisonRecord != null && comparisonRecord.CountsTowardProgress;
            EnsureOutline(
                button.gameObject,
                comparisonLocked ? InvestigationV2Theme.Success : selected ? InvestigationV2Theme.Primary : stateColor,
                comparisonLocked || selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f));

            RectTransform artwork = CreateSpeciesArtwork("Species Artwork", button.transform, species, stateColor);
            if (compact)
            {
                Anchor(artwork, 0f, 0f, 0.34f, 1f, 12f, 9f, -4f, -9f);
                Text title = CreateText("Name", button.transform, species.DisplayName, 14, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.LowerLeft, InvestigationV2Theme.DisplayFont);
                Anchor(title.rectTransform, 0.35f, 0.44f, 1f, 1f, 8f, 0f, -8f, -6f);
                Text stateLabel = CreateText("Prediction", button.transform, PredictionLabel(prediction.PredictedState), 13, FontStyle.Bold, stateColor, TextAnchor.UpperLeft, InvestigationV2Theme.DataFont);
                Anchor(stateLabel.rectTransform, 0.35f, 0f, 1f, 0.46f, 8f, 5f, -8f, 0f);
            }
            else
            {
                Anchor(artwork, 0f, 0.36f, 1f, 1f, 34f, 0f, -34f, -8f);
                Text title = CreateText("Name", button.transform, species.DisplayName, 15, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleCenter, InvestigationV2Theme.DisplayFont);
                Anchor(title.rectTransform, 0f, 0.18f, 1f, 0.42f, 4f, 0f, -4f, 0f);
                Text stateLabel = CreateText("Prediction", button.transform, PredictionLabel(prediction.PredictedState), 13, FontStyle.Bold, stateColor, TextAnchor.UpperCenter, InvestigationV2Theme.DataFont);
                Anchor(stateLabel.rectTransform, 0f, 0f, 1f, 0.20f, 4f, 0f, -4f, 0f);
            }

            CanvasGroup artworkGroup = artwork.gameObject.AddComponent<CanvasGroup>();
            artwork.localScale = Vector3.one * FinalArtworkScale(prediction.PredictedState);
            artworkGroup.alpha = FinalArtworkAlpha(prediction.PredictedState);

            Text changeIndicator = CreateText(
                "Change Indicator",
                button.transform,
                PredictionStateSymbol(prediction.PredictedState),
                compact ? 20 : 30,
                FontStyle.Bold,
                stateColor,
                TextAnchor.MiddleCenter,
                InvestigationV2Theme.DisplayFont);
            if (compact) Anchor(changeIndicator.rectTransform, 0.83f, 0.42f, 1f, 1f, 0f, 0f, -8f, -4f);
            else Anchor(changeIndicator.rectTransform, 0.75f, 0.62f, 1f, 1f, 0f, 0f, -10f, -6f);

            if ((prediction.PredictedState == PredictionState.Increase || prediction.PredictedState == PredictionState.Decrease)
                && species.Icon != null)
            {
                float finalCrowdAlpha = prediction.PredictedState == PredictionState.Increase ? 0.58f : 0f;
                if (compact)
                {
                    CreateCrowdMember(button.transform, species.Icon, "Crowd Member 1", 0.00f, 0.50f, 0.14f, 0.80f, finalCrowdAlpha);
                    CreateCrowdMember(button.transform, species.Icon, "Crowd Member 2", 0.10f, 0.64f, 0.24f, 0.94f, finalCrowdAlpha);
                    CreateCrowdMember(button.transform, species.Icon, "Crowd Member 3", 0.20f, 0.48f, 0.34f, 0.78f, finalCrowdAlpha);
                }
                else
                {
                    CreateCrowdMember(button.transform, species.Icon, "Crowd Member 1", 0.02f, 0.46f, 0.24f, 0.78f, finalCrowdAlpha);
                    CreateCrowdMember(button.transform, species.Icon, "Crowd Member 2", 0.10f, 0.64f, 0.32f, 0.96f, finalCrowdAlpha);
                    CreateCrowdMember(button.transform, species.Icon, "Crowd Member 3", 0.70f, 0.50f, 0.92f, 0.82f, finalCrowdAlpha);
                }
            }
            if (comparisonLocked)
            {
                Image locked = CreateStatusIcon("Comparison Locked", button.transform, InvestigationV2StatusIconLibrary.Check, InvestigationV2Theme.Success);
                Anchor(locked.rectTransform, 0.84f, 0.66f, 1f, 1f, 0f, 0f, -6f, -5f);
            }
            return button;
        }

        private void CreateCrowdMember(Transform parent, Sprite sprite, string name, float minX, float minY, float maxX, float maxY, float alpha)
        {
            Image member = CreateGraphic<Image>(name, parent);
            member.sprite = sprite;
            member.preserveAspect = true;
            member.raycastTarget = false;
            member.color = new Color(1f, 1f, 1f, alpha);
            Anchor(member.rectTransform, minX, minY, maxX, maxY, 0f, 0f, 0f, 0f);
        }

        private void RenderSelectedPredictionPanel(RectTransform panel, SimulationResult simulation)
        {
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text heading = CreateText("Heading", panel, "THE MODEL SAYS", 13, FontStyle.Bold, InvestigationV2Theme.TextMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            AddLayout(heading.rectTransform, 24f, 1f);
            if (string.IsNullOrEmpty(selectedPredictionSpeciesId))
            {
                Text prompt = CreateText("Prompt", panel, "Select one species prediction above.", 15, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
                AddLayout(prompt.rectTransform, 70f, 1f);
                return;
            }
            SimulationPrediction selected = simulation.FindPrediction(selectedPredictionSpeciesId);
            InvestigationV2SpeciesDefinition species = caseDefinition.FindSpecies(selectedPredictionSpeciesId);
            Text prediction = CreateText(
                "Selected Prediction",
                panel,
                selected == null || species == null
                    ? "Prediction unavailable."
                    : $"{species.DisplayName}: {PredictionLabel(selected.PredictedState)}\n{selected.Rationale}",
                16,
                FontStyle.Bold,
                InvestigationV2Theme.TextPrimary,
                TextAnchor.UpperLeft,
                InvestigationV2Theme.BodyFont);
            AddLayout(prediction.rectTransform, 128f, 1f);
        }

        private void RenderCandidateObservations(RectTransform panel, SimulationResult simulation)
        {
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text heading = CreateText("Heading", panel, "WHAT WE FOUND", 13, FontStyle.Bold, InvestigationV2Theme.TextMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            AddLayout(heading.rectTransform, 24f, 1f);
            if (string.IsNullOrEmpty(selectedPredictionSpeciesId))
            {
                Text prompt = CreateText("Prompt", panel, "Candidate observations appear after you choose a prediction.", 15, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
                AddLayout(prompt.rectTransform, 70f, 1f);
                return;
            }
            PredictionComparisonRuleDefinition rule = caseDefinition.FindComparisonRule(simulation.ThreatId, selectedPredictionSpeciesId);
            if (rule == null) return;
            PredictionComparisonRecord lockedComparison = state.FindComparison(simulation.ThreatId, selectedPredictionSpeciesId);
            bool comparisonLocked = lockedComparison != null && lockedComparison.CountsTowardProgress;
            int maxCandidates = state.Difficulty == InvestigationV2Difficulty.Easy ? 3 : 4;
            int shown = 0;
            for (int index = 0; index < rule.ObservationOptions.Count && shown < maxCandidates; index++)
            {
                ObservationComparisonOptionDefinition option = rule.ObservationOptions[index];
                InvestigationV2ObservationDefinition observation = option == null ? null : caseDefinition.FindObservation(option.EvidenceId);
                if (observation == null || !state.HasDiscoveredObservation(observation.EvidenceId)) continue;
                Button button = CreateButton(
                    $"Observation {observation.EvidenceId}",
                    panel,
                    observation.DisplayName,
                    ButtonVisualStyle.Choice,
                    () =>
                    {
                        selectedObservationId = observation.EvidenceId;
                        RefreshPresentationOnly();
                    },
                    out Text label);
                label.alignment = TextAnchor.MiddleLeft;
                button.GetComponent<LayoutElement>().preferredHeight = 44f;
                button.GetComponent<LayoutElement>().minHeight = 44f;
                button.interactable = !comparisonLocked;
                if (string.Equals(selectedObservationId, observation.EvidenceId, StringComparison.Ordinal))
                {
                    button.GetComponent<Image>().color = InvestigationV2Theme.SurfaceRaised;
                    EnsureOutline(button.gameObject, InvestigationV2Theme.Primary, new Vector2(2f, -2f));
                    label.rectTransform.offsetMax = new Vector2(-30f, label.rectTransform.offsetMax.y);
                    Image check = CreateStatusIcon("Selected Evidence Check", button.transform, InvestigationV2StatusIconLibrary.Check, InvestigationV2Theme.Primary);
                    Anchor(check.rectTransform, 0.88f, 0.18f, 1f, 0.82f, 0f, 0f, -8f, 0f);
                }
                else if (state.Difficulty == InvestigationV2Difficulty.Easy
                    && string.Equals(observation.RelatedSpeciesId, selectedPredictionSpeciesId, StringComparison.Ordinal))
                {
                    EnsureOutline(button.gameObject, InvestigationV2Theme.BorderStrong, new Vector2(1f, -1f));
                }
                shown++;
            }
            if (shown == 0)
            {
                Text none = CreateText("No Candidates", panel, "Return to Observe and record more evidence for this prediction.", 14, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
                AddLayout(none.rectTransform, 68f, 1f);
            }
        }

        private void CreateJudgementButton(Transform parent, string label, ComparisonJudgement judgement, Sprite iconSprite, Color color, float preferredWidth)
        {
            Button button = CreateButton($"Judge {judgement}", parent, label, ButtonVisualStyle.Secondary, () =>
            {
                if (string.IsNullOrEmpty(selectedPredictionSpeciesId) || string.IsNullOrEmpty(selectedObservationId))
                {
                    statusMessage = "Choose one prediction and one observation before judging the connection.";
                    statusTone = InvestigationV2StatusTone.Warning;
                    RefreshPresentationOnly();
                    return;
                }
                compare?.Invoke(selectedThreatId, selectedPredictionSpeciesId, selectedObservationId, judgement);
            }, out Text text);
            text.rectTransform.offsetMin = new Vector2(38f, 4f);
            EnsureOutline(button.gameObject, color, new Vector2(2f, -2f));
            Image glyph = CreateStatusIcon("State Shape", button.transform, iconSprite, color);
            Anchor(glyph.rectTransform, 0f, 0f, 0f, 1f, 9f, 9f, 37f, -9f);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.minWidth = preferredWidth;
            PredictionComparisonRecord saved = string.IsNullOrEmpty(selectedPredictionSpeciesId)
                ? null
                : state.FindComparison(selectedThreatId, selectedPredictionSpeciesId);
            button.interactable = saved == null || !saved.CountsTowardProgress;
        }

        private string BuildSimulationGateLabel()
        {
            for (int index = 0; index < caseDefinition.RequiredComparedThreatIds.Count; index++)
            {
                string requiredThreatId = caseDefinition.RequiredComparedThreatIds[index];
                if (!state.HasTriedThreat(requiredThreatId))
                {
                    ThreatSimulationDefinition threat = caseDefinition.FindThreat(requiredThreatId);
                    return threat == null ? "Next: run a required model" : $"Next: run {threat.DisplayName}";
                }
                InvestigationV2RequiredComparisonSpeciesDefinition requiredSpecies = caseDefinition.FindRequiredComparisonSpecies(requiredThreatId);
                if (requiredSpecies != null)
                {
                    for (int speciesIndex = 0; speciesIndex < requiredSpecies.RequiredComparisonSpeciesIds.Count; speciesIndex++)
                    {
                        string speciesId = requiredSpecies.RequiredComparisonSpeciesIds[speciesIndex];
                        PredictionComparisonRecord requiredRecord = state.FindComparison(requiredThreatId, speciesId);
                        if (requiredRecord == null || !requiredRecord.CountsTowardProgress)
                        {
                            ThreatSimulationDefinition threat = caseDefinition.FindThreat(requiredThreatId);
                            InvestigationV2SpeciesDefinition species = caseDefinition.FindSpecies(speciesId);
                            return $"Next: compare {species?.DisplayName ?? speciesId} for {threat?.DisplayName ?? requiredThreatId}";
                        }
                    }
                }
                int remaining = caseDefinition.RequiredComparisonsPerThreat - state.AcceptedComparisonCountForThreat(requiredThreatId);
                if (remaining > 0)
                {
                    ThreatSimulationDefinition threat = caseDefinition.FindThreat(requiredThreatId);
                    return $"Next: {remaining} comparison{(remaining == 1 ? string.Empty : "s")} for {threat?.DisplayName ?? requiredThreatId}";
                }
            }
            int totalRemaining = caseDefinition.MinimumCompletedComparisons - state.AcceptedComparisonCount;
            return totalRemaining > 0 ? $"Next: {totalRemaining} more comparison{(totalRemaining == 1 ? string.Empty : "s")}" : "Complete the required comparisons";
        }

        private string FindDefaultThreatId()
        {
            if (!string.IsNullOrEmpty(state.ActiveThreatId) && caseDefinition.FindThreat(state.ActiveThreatId) != null) return state.ActiveThreatId;
            for (int index = 0; index < caseDefinition.Threats.Count; index++)
            {
                ThreatSimulationDefinition threat = caseDefinition.Threats[index];
                if (threat != null && threat.GlyphKind == ThreatGlyphKind.LongLine) return threat.ThreatId;
            }
            return caseDefinition.Threats.Count > 0 ? caseDefinition.Threats[0].ThreatId : string.Empty;
        }

        private PredictionAnimationTarget CreatePredictionAnimationTarget(Button node, SimulationPrediction prediction)
        {
            RectTransform artwork = node.transform.Find("Species Artwork") as RectTransform;
            Text stateLabel = node.transform.Find("Prediction")?.GetComponent<Text>();
            Text indicator = node.transform.Find("Change Indicator")?.GetComponent<Text>();
            return new PredictionAnimationTarget
            {
                Artwork = artwork,
                ArtworkGroup = artwork == null ? null : artwork.GetComponent<CanvasGroup>(),
                StateLabel = stateLabel,
                Indicator = indicator,
                IndicatorGroup = indicator == null ? null : indicator.gameObject.AddComponent<CanvasGroup>(),
                CrowdMembers = new[]
                {
                    node.transform.Find("Crowd Member 1")?.GetComponent<Image>(),
                    node.transform.Find("Crowd Member 2")?.GetComponent<Image>(),
                    node.transform.Find("Crowd Member 3")?.GetComponent<Image>()
                },
                State = prediction.PredictedState,
                StateColor = PredictionStateColor(prediction.PredictedState)
            };
        }

        private IEnumerator AnimateSimulationSequence(
            IReadOnlyList<PredictionAnimationTarget> foodWebTargets,
            IReadOnlyList<RectTransform> indicatorNodes)
        {
            for (int index = 0; index < foodWebTargets.Count; index++)
            {
                PredictionAnimationTarget target = foodWebTargets[index];
                if (target.Artwork != null) target.Artwork.localScale = Vector3.one;
                if (target.ArtworkGroup != null) target.ArtworkGroup.alpha = 1f;
                if (target.StateLabel != null)
                {
                    target.StateLabel.text = "Stable";
                    target.StateLabel.color = InvestigationV2Theme.Unknown;
                }
                if (target.IndicatorGroup != null) target.IndicatorGroup.alpha = 0f;
                SetCrowdAlpha(target.CrowdMembers, target.State == PredictionState.Decrease ? 0.58f : 0f);
            }

            yield return new WaitForSecondsRealtime(0.56f);
            for (int index = 0; index < foodWebTargets.Count; index++)
            {
                PredictionAnimationTarget target = foodWebTargets[index];
                if (target.StateLabel != null)
                {
                    target.StateLabel.text = $"Stable → {PredictionLabel(target.State)}";
                    target.StateLabel.color = target.StateColor;
                }

                float elapsed = 0f;
                const float duration = 1.04f;
                float startCrowdAlpha = target.State == PredictionState.Decrease ? 0.58f : 0f;
                float finalCrowdAlpha = target.State == PredictionState.Increase ? 0.58f : 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = t * t * (3f - 2f * t);
                    if (target.Artwork != null)
                    {
                        float scale = Mathf.Lerp(1f, FinalArtworkScale(target.State), eased);
                        if (target.State == PredictionState.Increase)
                        {
                            scale *= 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
                        }
                        target.Artwork.localScale = Vector3.one * scale;
                    }
                    if (target.ArtworkGroup != null)
                        target.ArtworkGroup.alpha = Mathf.Lerp(1f, FinalArtworkAlpha(target.State), eased);
                    if (target.IndicatorGroup != null) target.IndicatorGroup.alpha = eased;
                    SetCrowdAlpha(target.CrowdMembers, Mathf.Lerp(startCrowdAlpha, finalCrowdAlpha, eased));
                    yield return null;
                }

                if (target.Artwork != null) target.Artwork.localScale = Vector3.one * FinalArtworkScale(target.State);
                if (target.ArtworkGroup != null) target.ArtworkGroup.alpha = FinalArtworkAlpha(target.State);
                if (target.IndicatorGroup != null) target.IndicatorGroup.alpha = 1f;
                SetCrowdAlpha(target.CrowdMembers, finalCrowdAlpha);
                if (target.StateLabel != null) target.StateLabel.text = PredictionLabel(target.State);
                yield return new WaitForSecondsRealtime(0.32f);
            }

            for (int index = 0; index < indicatorNodes.Count; index++)
            {
                RectTransform node = indicatorNodes[index];
                if (node == null) continue;
                CanvasGroup group = node.GetComponent<CanvasGroup>();
                if (group == null) group = node.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0.35f;
                node.localScale = Vector3.one * 0.94f;
                float elapsed = 0f;
                const float duration = 0.36f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    group.alpha = Mathf.Lerp(0.35f, 1f, t);
                    node.localScale = Vector3.Lerp(Vector3.one * 0.94f, Vector3.one, t);
                    yield return null;
                }
                group.alpha = 1f;
                node.localScale = Vector3.one;
            }
        }

        private static void SetCrowdAlpha(IReadOnlyList<Image> members, float alpha)
        {
            if (members == null) return;
            for (int index = 0; index < members.Count; index++)
            {
                Image member = members[index];
                if (member == null) continue;
                Color color = member.color;
                color.a = alpha;
                member.color = color;
            }
        }

        private static Color PredictionStateColor(PredictionState state)
        {
            if (state == PredictionState.Increase) return InvestigationV2Theme.Success;
            if (state == PredictionState.Decrease) return InvestigationV2Theme.Danger;
            return InvestigationV2Theme.Unknown;
        }

        private static string PredictionStateSymbol(PredictionState state)
        {
            switch (state)
            {
                case PredictionState.Increase: return "↑";
                case PredictionState.Decrease: return "↓";
                case PredictionState.DepthShift: return "↕";
                case PredictionState.Unknown: return "?";
                default: return "—";
            }
        }

        private static float FinalArtworkScale(PredictionState state)
        {
            if (state == PredictionState.Increase) return 1.10f;
            return 1f;
        }

        private static float FinalArtworkAlpha(PredictionState state)
        {
            if (state == PredictionState.Unknown) return 0.68f;
            return 1f;
        }
    }
}
