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

            SimulationResult simulation = selectedThreat != null ? state.FindSimulation(selectedThreat.ThreatId) : null;
            const float modelPanelHeight = 293f;
            const float modelColumnHeight = 385f;
            float pairingHeight = simulation == null ? 0f : CalculateSimulationPairingHeight(simulation);
            float comparisonHeight = simulation == null ? 318f : 154f + pairingHeight;
            float workspaceHeight = Mathf.Max(435f, 118f + comparisonHeight);
            RectTransform workspace = new GameObject("Simulate Workspace", typeof(RectTransform), typeof(InvestigationV2ResponsiveSplitLayout)).GetComponent<RectTransform>();
            workspace.SetParent(contentRoot, false);
            InvestigationV2ResponsiveSplitLayout workspaceLayout = workspace.GetComponent<InvestigationV2ResponsiveSplitLayout>();
            workspaceLayout.padding = new RectOffset(0, 0, 0, 0);
            workspaceLayout.Configure(0.5f, 12f, 960f, workspaceHeight, workspaceHeight);

            RectTransform leftColumn = CreatePanel("Simulate Left Column", workspace, new Color(0f, 0f, 0f, 0f), 0f);
            VerticalLayoutGroup leftLayout = leftColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            leftLayout.padding = new RectOffset(0, 0, 0, 0);
            leftLayout.spacing = 8f;
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = true;
            leftLayout.childForceExpandWidth = true;
            leftLayout.childForceExpandHeight = false;
            RenderSimulateIntroduction(leftColumn);

            RectTransform modelColumn = CreatePanel("Simulation Models", leftColumn, InvestigationV2Theme.SurfaceQuiet, InvestigationV2Theme.SmallRadius);
            AddLayout(modelColumn, modelColumnHeight, 1f);
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

            RectTransform modelPanel = CreateSection("Model Workspace", modelColumn, InvestigationV2Theme.Deep, modelPanelHeight, InvestigationV2Theme.SmallRadius);
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

            RectTransform rightColumn = CreatePanel("Simulate Right Column", workspace, new Color(0f, 0f, 0f, 0f), 0f);
            VerticalLayoutGroup rightLayout = rightColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            rightLayout.padding = new RectOffset(0, 0, 0, 0);
            rightLayout.spacing = 8f;
            rightLayout.childControlWidth = true;
            rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = false;

            RectTransform questions = CreatePanel("Case Questions", rightColumn, new Color32(8, 36, 54, 225), InvestigationV2Theme.SmallRadius);
            AddLayout(questions, 110f, 1f);
            PopulateCaseQuestions(questions);

            RectTransform comparisonColumn = CreatePanel("Comparison Workspace", rightColumn, InvestigationV2Theme.SurfaceQuiet, InvestigationV2Theme.SmallRadius);
            AddLayout(comparisonColumn, comparisonHeight, 1f);
            VerticalLayoutGroup comparisonLayout = comparisonColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            comparisonLayout.padding = new RectOffset(10, 10, 10, 4);
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
                RenderSimulationVisualization(modelPanel, simulation, modelPanelHeight);
                RenderSimulationComparison(comparisonColumn, simulation, pairingHeight);
            }

            RenderSimulationNavigation(comparisonColumn, simulation);
        }

        private void RenderSimulateIntroduction(Transform parent)
        {
            RectTransform introduction = CreatePanel("Simulate Introduction", parent, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(introduction, 42f, 1f);
            HorizontalLayoutGroup layout = introduction.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            Text title = CreateText("Simulate Title", introduction, "Test an explanation", 18, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.minWidth = Mathf.Clamp(title.preferredWidth + 6f, 145f, 320f);
            titleLayout.preferredWidth = titleLayout.minWidth;
            titleLayout.preferredHeight = 40f;

            RectTransform divider = CreatePanel("Simulate Heading Divider", introduction, InvestigationV2Theme.Primary, 0f);
            LayoutElement dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
            dividerLayout.minWidth = 2f;
            dividerLayout.preferredWidth = 2f;
            dividerLayout.minHeight = 22f;
            dividerLayout.preferredHeight = 22f;

            Text description = CreateText("Simulate Description", introduction, "Run one scenario and compare its food-web prediction with the real survey.", 12, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.MiddleLeft, InvestigationV2Theme.BodyFont);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement descriptionLayout = description.gameObject.AddComponent<LayoutElement>();
            descriptionLayout.minWidth = 220f;
            descriptionLayout.preferredHeight = 40f;
            descriptionLayout.flexibleWidth = 1f;
        }

        private float CalculateSimulationPairingHeight(SimulationResult simulation)
        {
            if (simulation == null || string.IsNullOrEmpty(selectedPredictionSpeciesId)) return 118f;
            PredictionComparisonRuleDefinition rule = caseDefinition.FindComparisonRule(
                simulation.ThreatId,
                selectedPredictionTargetKind,
                selectedPredictionSpeciesId);
            if (rule == null) return 128f;

            int maximum = state.Difficulty == InvestigationV2Difficulty.Easy ? 3 : 4;
            int visible = 0;
            for (int index = 0; index < rule.ObservationOptions.Count && visible < maximum; index++)
            {
                ObservationComparisonOptionDefinition option = rule.ObservationOptions[index];
                if (option != null && state.HasDiscoveredObservation(option.EvidenceId)) visible++;
            }

            float observationContentHeight = visible > 0 ? 20f + visible * 47f : 76f;
            return Mathf.Clamp(Mathf.Max(128f, observationContentHeight), 118f, 220f);
        }

        private void RenderSimulationNavigation(RectTransform parent, SimulationResult simulation)
        {
            RectTransform navigation = CreatePanel("Simulation Navigation", parent, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(navigation, 28f, 1f);
            HorizontalLayoutGroup navigationLayout = navigation.gameObject.AddComponent<HorizontalLayoutGroup>();
            navigationLayout.padding = new RectOffset(0, 0, 0, 0);
            navigationLayout.spacing = 4f;
            navigationLayout.childAlignment = TextAnchor.LowerCenter;
            navigationLayout.childControlWidth = true;
            navigationLayout.childControlHeight = false;
            navigationLayout.childForceExpandWidth = false;
            navigationLayout.childForceExpandHeight = false;

            InvestigationV2Readiness readiness = new InvestigationV2ConclusionEvaluator().EvaluateReadiness(caseDefinition, state);
            string backLabel = string.IsNullOrEmpty(readiness.MissingEvidenceId)
                ? "← Back to notebook"
                : $"← Record {MissingEvidenceSubject(readiness.MissingEvidenceId)} in Observe";
            Button back = CreateButton("Back To Observe", navigation, backLabel, ButtonVisualStyle.Tertiary, () => setPhase?.Invoke(InvestigationV2Phase.Observe), out _);
            ConfigureCompactNavigationButton(back, string.IsNullOrEmpty(readiness.MissingEvidenceId) ? 124f : 210f);

            if (readiness.CanEnterProvisional)
            {
                Button report = CreateButton("Write Provisional Report", navigation, "Write first idea →", ButtonVisualStyle.Primary, () => submitProvisional?.Invoke(selectedThreatId), out _);
                ConfigureCompactNavigationButton(report, 148f);
            }
            else
            {
                RectTransform gate = CreatePanel("Report Gate Hint", navigation, new Color32(14, 51, 72, 225), 10f);
                gate.sizeDelta = new Vector2(310f, 28f);
                LayoutElement gateLayout = gate.gameObject.AddComponent<LayoutElement>();
                gateLayout.minWidth = 210f;
                gateLayout.preferredWidth = 310f;
                gateLayout.flexibleWidth = 1f;
                gateLayout.minHeight = 28f;
                gateLayout.preferredHeight = 28f;
                AddPanelAccent(gate, InvestigationV2Theme.Primary, 2f);
                Text guidance = CreateText(
                    "Comparison Gate",
                    gate,
                    simulation == null
                        ? "TO REPORT · Run a model, then compare its predictions with evidence"
                        : $"TO REPORT · {BuildSimulationGateLabel()}",
                    10,
                    FontStyle.Bold,
                    InvestigationV2Theme.TextSecondary,
                    TextAnchor.MiddleCenter,
                    InvestigationV2Theme.DataFont);
                Stretch(guidance.rectTransform, 8f, 1f, -8f, -1f);
            }
        }

        private void PopulateCaseQuestions(RectTransform panel)
        {
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text heading = CreateText("Case Questions Heading", panel, "CASE QUESTIONS", 11, FontStyle.Bold, InvestigationV2Theme.Primary, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            AddLayout(heading.rectTransform, 16f, 1f);

            RectTransform grid = new GameObject("Case Question Grid", typeof(RectTransform), typeof(InvestigationV2ResponsiveGridLayout)).GetComponent<RectTransform>();
            grid.SetParent(panel, false);
            InvestigationV2ResponsiveGridLayout gridLayout = grid.GetComponent<InvestigationV2ResponsiveGridLayout>();
            gridLayout.padding = new RectOffset(0, 0, 0, 0);
            gridLayout.Configure(2, 2, 2, 24f, 2f);

            HashSet<string> shownQuestionIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
            {
                InvestigationV2ObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                if (objective == null || !objective.Required || !shownQuestionIds.Add(objective.QuestionId)) continue;
                bool complete = IsQuestionComplete(objective.QuestionId);
                RectTransform row = CreatePanel($"Case Question {objective.QuestionId}", grid, new Color(0f, 0f, 0f, 0f), 0f);
                Image icon = CreateStatusIcon(
                    "Question Status",
                    row,
                    complete ? InvestigationV2StatusIconLibrary.Check : InvestigationV2StatusIconLibrary.Question,
                    complete ? InvestigationV2Theme.Success : InvestigationV2Theme.Unknown);
                Anchor(icon.rectTransform, 0f, 0f, 0f, 1f, 0f, 3f, 16f, -3f);
                Text prompt = CreateText("Question Prompt", row, objective.QuestionPrompt, 13, FontStyle.Bold, complete ? InvestigationV2Theme.TextPrimary : InvestigationV2Theme.TextSecondary, TextAnchor.MiddleLeft, InvestigationV2Theme.BodyFont);
                prompt.verticalOverflow = VerticalWrapMode.Overflow;
                Anchor(prompt.rectTransform, 0f, 0f, 1f, 1f, 20f, 0f, 0f, 0f);
            }
        }

        private bool IsQuestionComplete(string questionId)
        {
            bool found = false;
            for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
            {
                InvestigationV2ObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                if (objective == null || !objective.Required || !string.Equals(objective.QuestionId, questionId, StringComparison.Ordinal)) continue;
                found = true;
                if (!state.HasCompletedObjective(objective.ObjectiveId)) return false;
            }
            return found;
        }

        private string ThreatInvestigationStatus(string threatId)
        {
            if (!state.HasTriedThreat(threatId)) return "NOT TESTED";
            bool hasRequired = false;
            for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
            {
                InvestigationV2ObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                if (objective == null || !objective.Required || !string.Equals(objective.ThreatId, threatId, StringComparison.Ordinal)) continue;
                hasRequired = true;
                if (!state.HasCompletedObjective(objective.ObjectiveId)) return "CHECK EVIDENCE";
            }
            if (!hasRequired) return "MODEL RUN";
            return string.Equals(threatId, caseDefinition.CorrectThreatId, StringComparison.Ordinal)
                ? "STILL POSSIBLE"
                : "CHALLENGED";
        }

        private string MissingEvidenceSubject(string evidenceId)
        {
            InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(evidenceId);
            if (observation == null) return "missing evidence";
            InvestigationV2SpeciesDefinition species = caseDefinition.FindSpecies(observation.RelatedSpeciesId);
            return species?.DisplayName ?? observation.DisplayName;
        }

        private static void ConfigureCompactNavigationButton(Button button, float width)
        {
            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = 28f;
            layout.preferredHeight = 28f;
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 28f);
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 11;
                Stretch(label.rectTransform, 6f, 2f, -6f, -2f);
            }

            Image rootImage = button.GetComponent<Image>();
            if (rootImage == null || rootImage.color.a <= 0.01f) return;
            Color surfaceColor = rootImage.color;
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            Shadow rootShadow = button.GetComponent<Shadow>();
            if (rootShadow != null) rootShadow.enabled = false;
            RectTransform surface = CreatePanel("Compact Navigation Surface", button.transform, surfaceColor, 11f);
            Stretch(surface, 0f, 0f, 0f, 0f);
            surface.SetAsFirstSibling();
            AddSingleShadow(surface.gameObject, InvestigationV2Theme.PrimaryShadow, new Vector2(2f, -2f));
            button.targetGraphic = surface.GetComponent<Image>();
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
                    selectedPredictionTargetKind = PredictionTargetKind.Species;
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
            Anchor(title.rectTransform, 0.30f, 0.24f, 0.88f, 1f, 2f, 0f, -2f, 0f);
            Text status = CreateText("Threat Status", button.transform, ThreatInvestigationStatus(threat.ThreatId), 10, FontStyle.Bold, InvestigationV2Theme.TextMuted, TextAnchor.UpperLeft, InvestigationV2Theme.DataFont);
            Anchor(status.rectTransform, 0.30f, 0f, 0.88f, 0.32f, 2f, 2f, -2f, 0f);
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

        private void RenderSimulationVisualization(RectTransform parent, SimulationResult simulation, float modelHeight)
        {
            RectTransform environment = CreatePanel("Environmental Predictions", parent, InvestigationV2Theme.Surface, InvestigationV2Theme.SmallRadius);
            AddLayout(environment, 44f, 1f);
            HorizontalLayoutGroup environmentLayout = environment.gameObject.AddComponent<HorizontalLayoutGroup>();
            environmentLayout.padding = new RectOffset(4, 4, 4, 4);
            environmentLayout.spacing = 4f;
            environmentLayout.childControlWidth = true;
            environmentLayout.childControlHeight = true;
            environmentLayout.childForceExpandWidth = true;
            environmentLayout.childForceExpandHeight = false;
            CreateModelIndicatorChip(
                environment,
                "TEMP",
                CompactIndicatorValue(simulation.TemperaturePrediction),
                InvestigationV2Theme.Primary,
                simulation,
                PredictionTargetKind.Temperature,
                "temperature");
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
            List<PredictionAnimationTarget> benthicTargets = new List<PredictionAnimationTarget>();
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

            float benthicHeight = Mathf.Clamp(modelHeight - 206f, 44f, 104f);
            float benthicNodeHeight = Mathf.Clamp(benthicHeight - 26f, 44f, 78f);
            RectTransform indicators = CreatePanel("Reference Indicators", parent, InvestigationV2Theme.SurfaceQuiet, InvestigationV2Theme.SmallRadius);
            AddLayout(indicators, benthicHeight, 1f);
            HorizontalLayoutGroup indicatorLayout = indicators.gameObject.AddComponent<HorizontalLayoutGroup>();
            indicatorLayout.padding = new RectOffset(8, 8, 20, 6);
            indicatorLayout.spacing = 8f;
            indicatorLayout.childAlignment = TextAnchor.MiddleCenter;
            indicatorLayout.childControlWidth = true;
            indicatorLayout.childControlHeight = true;
            indicatorLayout.childForceExpandWidth = true;
            indicatorLayout.childForceExpandHeight = false;
            Text indicatorLabel = CreateText("Indicator Heading", indicators, "BENTHIC CHECK · SELECT A SPECIES", 11, FontStyle.Bold, InvestigationV2Theme.Primary, TextAnchor.UpperLeft, InvestigationV2Theme.DataFont);
            Anchor(indicatorLabel.rectTransform, 0f, 1f, 1f, 1f, 10f, -18f, -8f, -2f);
            indicatorLabel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            string[] indicatorIds = { "sea_star", "mussel" };
            for (int index = 0; index < indicatorIds.Length; index++)
            {
                SimulationPrediction prediction = simulation.FindPrediction(indicatorIds[index]);
                InvestigationV2SpeciesDefinition species = caseDefinition.FindSpecies(indicatorIds[index]);
                if (species == null || prediction == null) continue;
                Button node = CreatePredictionButton(indicators, species, prediction, 220f, benthicNodeHeight, true);
                LayoutElement nodeLayout = node.GetComponent<LayoutElement>();
                nodeLayout.minWidth = 180f;
                nodeLayout.preferredWidth = 0f;
                nodeLayout.flexibleWidth = 1f;
                benthicTargets.Add(CreatePredictionAnimationTarget(node, prediction));
            }

            if (!animatedThreatIds.Contains(simulation.ThreatId))
            {
                animatedThreatIds.Add(simulation.ThreatId);
                if (!InvestigationV2MotionSettings.ReducedMotion)
                {
                    StartCoroutine(AnimateSimulationSequence(foodWebTargets, benthicTargets));
                }
            }
        }

        private void CreateModelIndicatorChip(
            Transform parent,
            string label,
            string value,
            Color accentColor,
            SimulationResult simulation = null,
            PredictionTargetKind targetKind = PredictionTargetKind.Species,
            string targetId = "")
        {
            bool selectable = simulation != null
                && caseDefinition.FindComparisonRule(simulation.ThreatId, targetKind, targetId) != null;
            RectTransform chip;
            bool selected = false;
            if (selectable)
            {
                Button button = CreateButton(
                    $"Prediction Target {targetKind} {targetId}",
                    parent,
                    string.Empty,
                    ButtonVisualStyle.Choice,
                    () =>
                    {
                        selectedPredictionTargetKind = targetKind;
                        selectedPredictionSpeciesId = targetId;
                        PredictionComparisonRecord saved = state.FindComparison(simulation.ThreatId, targetKind, targetId);
                        selectedObservationId = saved != null && saved.LocksComparison ? saved.EvidenceId : string.Empty;
                        RefreshPresentationOnly();
                    },
                    out Text hiddenLabel);
                hiddenLabel.gameObject.SetActive(false);
                chip = button.GetComponent<RectTransform>();
                LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
                buttonLayout.minWidth = 0f;
                buttonLayout.preferredWidth = 0f;
                buttonLayout.flexibleWidth = 1f;
                buttonLayout.minHeight = 36f;
                buttonLayout.preferredHeight = 36f;
                buttonLayout.flexibleHeight = 0f;
                selected = selectedPredictionTargetKind == targetKind
                    && string.Equals(selectedPredictionSpeciesId, targetId, StringComparison.Ordinal);
                button.GetComponent<Image>().color = selected ? InvestigationV2Theme.SurfaceRaised : new Color32(8, 36, 54, 215);
            }
            else
            {
                chip = CreatePanel($"Model Indicator {label}", parent, new Color32(8, 36, 54, 215), 8f);
            }
            EnsureOutline(chip.gameObject, accentColor, selected ? new Vector2(3f, -3f) : new Vector2(2f, -2f));
            LayoutElement chipLayout = chip.GetComponent<LayoutElement>();
            if (chipLayout == null) chipLayout = chip.gameObject.AddComponent<LayoutElement>();
            chipLayout.minWidth = 0f;
            chipLayout.preferredWidth = 0f;
            chipLayout.flexibleWidth = 1f;
            chipLayout.minHeight = 36f;
            chipLayout.preferredHeight = 36f;
            chipLayout.flexibleHeight = 0f;
            Text title = CreateText("Indicator Label", chip, label, 12, FontStyle.Bold, accentColor, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            Anchor(title.rectTransform, 0f, 0.55f, 1f, 1f, 7f, 0f, -4f, 0f);
            Text detail = CreateText("Indicator Value", chip, value, 14, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleLeft, InvestigationV2Theme.BodyFont);
            Anchor(detail.rectTransform, 0f, 0f, 1f, 0.58f, 7f, 0f, -4f, 0f);
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

        private void RenderSimulationComparison(RectTransform parent, SimulationResult simulation, float pairingHeight)
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

            RectTransform pairing = new GameObject("Prediction Observation Pairing", typeof(RectTransform), typeof(InvestigationV2ResponsiveSplitLayout)).GetComponent<RectTransform>();
            pairing.SetParent(parent, false);
            InvestigationV2ResponsiveSplitLayout split = pairing.GetComponent<InvestigationV2ResponsiveSplitLayout>();
            split.padding = new RectOffset(0, 0, 0, 0);
            split.Configure(0.5f, 8f, 520f, pairingHeight, pairingHeight);

            RectTransform predictionPanel = CreatePanel("Prediction Selection", pairing, new Color32(13, 55, 76, 255), InvestigationV2Theme.SmallRadius);
            EnsureOutline(predictionPanel.gameObject, InvestigationV2Theme.Primary, new Vector2(2f, -2f));
            RenderSelectedPredictionPanel(predictionPanel, simulation);
            RectTransform observationPanel = CreatePanel("Observation Selection", pairing, InvestigationV2Theme.Surface, InvestigationV2Theme.SmallRadius);
            EnsureOutline(observationPanel.gameObject, InvestigationV2Theme.Accent, new Vector2(2f, -2f));
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
                    selectedPredictionTargetKind = PredictionTargetKind.Species;
                    PredictionComparisonRecord saved = state.FindComparison(selectedThreatId, PredictionTargetKind.Species, prediction.SpeciesId);
                    selectedObservationId = saved != null && saved.LocksComparison ? saved.EvidenceId : string.Empty;
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
            bool selected = selectedPredictionTargetKind == PredictionTargetKind.Species
                && string.Equals(selectedPredictionSpeciesId, prediction.SpeciesId, StringComparison.Ordinal);
            button.GetComponent<Image>().color = selected ? InvestigationV2Theme.SurfaceRaised : InvestigationV2Theme.Surface;
            Color stateColor = PredictionStateColor(prediction.PredictedState);
            PredictionComparisonRecord comparisonRecord = state.FindComparison(selectedThreatId, PredictionTargetKind.Species, prediction.SpeciesId);
            bool comparisonLocked = comparisonRecord != null && comparisonRecord.LocksComparison;
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
            layout.padding = new RectOffset(10, 10, 2, 2);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text heading = CreateText("Heading", panel, "THE MODEL SAYS", 13, FontStyle.Bold, InvestigationV2Theme.TextMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            AddLayout(heading.rectTransform, 16f, 1f);
            if (string.IsNullOrEmpty(selectedPredictionSpeciesId))
            {
                Text prompt = CreateText("Prompt", panel, "Select one species prediction above.", 15, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
                AddLayout(prompt.rectTransform, 48f, 1f);
                return;
            }
            SimulationPrediction selected = selectedPredictionTargetKind == PredictionTargetKind.Species
                ? simulation.FindPrediction(selectedPredictionSpeciesId)
                : null;
            InvestigationV2SpeciesDefinition species = selectedPredictionTargetKind == PredictionTargetKind.Species
                ? caseDefinition.FindSpecies(selectedPredictionSpeciesId)
                : null;
            string predictionText;
            if (selectedPredictionTargetKind == PredictionTargetKind.Temperature)
            {
                predictionText = $"Temperature pattern\n{simulation.TemperaturePrediction}";
            }
            else
            {
                predictionText = selected == null || species == null
                    ? "Prediction unavailable."
                    : $"{species.DisplayName}: {PredictionLabel(selected.PredictedState)}\n{selected.Rationale}";
            }
            Text prediction = CreateText(
                "Selected Prediction",
                panel,
                predictionText,
                16,
                FontStyle.Bold,
                InvestigationV2Theme.TextPrimary,
                TextAnchor.UpperLeft,
                InvestigationV2Theme.BodyFont);
            AddLayout(prediction.rectTransform, 84f, 1f);
        }

        private void RenderCandidateObservations(RectTransform panel, SimulationResult simulation)
        {
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text heading = CreateText("Heading", panel, "WHAT WE FOUND", 13, FontStyle.Bold, InvestigationV2Theme.TextMuted, TextAnchor.MiddleLeft, InvestigationV2Theme.DataFont);
            AddLayout(heading.rectTransform, 16f, 1f);
            if (string.IsNullOrEmpty(selectedPredictionSpeciesId))
            {
                Text prompt = CreateText("Prompt", panel, "Candidate observations appear after you choose a prediction.", 15, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
                AddLayout(prompt.rectTransform, 48f, 1f);
                return;
            }
            PredictionComparisonRuleDefinition rule = caseDefinition.FindComparisonRule(
                simulation.ThreatId,
                selectedPredictionTargetKind,
                selectedPredictionSpeciesId);
            if (rule == null) return;
            PredictionComparisonRecord lockedComparison = state.FindComparison(
                simulation.ThreatId,
                selectedPredictionTargetKind,
                selectedPredictionSpeciesId);
            bool comparisonLocked = lockedComparison != null && lockedComparison.LocksComparison;
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
                    SimulationObservationLabel(observation),
                    ButtonVisualStyle.Choice,
                    () =>
                    {
                        selectedObservationId = observation.EvidenceId;
                        RefreshPresentationOnly();
                    },
                    out Text label);
                label.alignment = TextAnchor.MiddleLeft;
                label.supportRichText = true;
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
                    && IsDirectObservationForSelectedTarget(observation))
                {
                    EnsureOutline(button.gameObject, InvestigationV2Theme.BorderStrong, new Vector2(1f, -1f));
                }
                shown++;
            }
            if (shown == 0)
            {
                Text none = CreateText("No Candidates", panel, "Return to Observe and record more evidence for this prediction.", 14, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperLeft, InvestigationV2Theme.BodyFont);
                AddLayout(none.rectTransform, 48f, 1f);
            }
        }

        private static string SimulationObservationLabel(InvestigationV2ObservationDefinition observation)
        {
            if (observation == null || string.IsNullOrEmpty(observation.DisplayName)) return string.Empty;
            string status = NotebookStateLabel(observation.ClaimType);
            int statusIndex = observation.DisplayName.IndexOf(status, StringComparison.OrdinalIgnoreCase);
            if (statusIndex < 0) return observation.DisplayName;

            string originalStatus = observation.DisplayName.Substring(statusIndex, status.Length);
            string color = ColorUtility.ToHtmlStringRGB(SimulationObservationStateColor(observation.ClaimType));
            return observation.DisplayName.Substring(0, statusIndex)
                + $"<b><color=#{color}>{originalStatus}</color></b>"
                + observation.DisplayName.Substring(statusIndex + status.Length);
        }

        private static Color SimulationObservationStateColor(ObservationClaimType claimType)
        {
            switch (claimType)
            {
                case ObservationClaimType.NotDetected: return InvestigationV2Theme.Danger;
                case ObservationClaimType.ChangedDepthOrDistribution: return InvestigationV2Theme.Accent;
                case ObservationClaimType.MatchesBaseline: return InvestigationV2Theme.Success;
                case ObservationClaimType.NewDetection: return InvestigationV2Theme.Primary;
                case ObservationClaimType.ResultWarning: return InvestigationV2Theme.Focus;
                case ObservationClaimType.EnvironmentalReading: return InvestigationV2Theme.Primary;
                default: return InvestigationV2Theme.TextSecondary;
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
                compare?.Invoke(
                    selectedThreatId,
                    selectedPredictionTargetKind,
                    selectedPredictionSpeciesId,
                    selectedObservationId,
                    judgement);
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
                : state.FindComparison(selectedThreatId, selectedPredictionTargetKind, selectedPredictionSpeciesId);
            button.interactable = saved == null || !saved.LocksComparison;
        }

        private bool IsDirectObservationForSelectedTarget(InvestigationV2ObservationDefinition observation)
        {
            if (observation == null) return false;
            if (selectedPredictionTargetKind == PredictionTargetKind.Temperature)
                return observation.Source == ObservationSource.CTDLog;
            return selectedPredictionTargetKind == PredictionTargetKind.Species
                && string.Equals(observation.RelatedSpeciesId, selectedPredictionSpeciesId, StringComparison.Ordinal);
        }

        private string BuildSimulationGateLabel()
        {
            if (caseDefinition.InvestigationObjectives.Count > 0)
            {
                for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
                {
                    InvestigationV2ObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                    if (objective == null || !objective.Required || state.HasCompletedObjective(objective.ObjectiveId)) continue;
                    InvestigationV2ObservationDefinition requiredEvidence = caseDefinition.FindObservation(objective.RequiredEvidenceId);
                    if (!state.HasDiscoveredObservation(objective.RequiredEvidenceId)
                        && requiredEvidence != null
                        && (requiredEvidence.UnlockStage == EvidenceUnlockStage.Observe
                            || requiredEvidence.UnlockStage == EvidenceUnlockStage.Always))
                    {
                        return $"Record {MissingEvidenceSubject(objective.RequiredEvidenceId)} in Observe";
                    }
                    ThreatSimulationDefinition threat = caseDefinition.FindThreat(objective.ThreatId);
                    if (!state.HasTriedThreat(objective.ThreatId))
                        return $"Run {threat?.DisplayName ?? objective.ThreatId}";
                    return ObjectiveActionLabel(objective, threat);
                }
                return "Complete the required investigation questions";
            }
            int totalRemaining = caseDefinition.MinimumCompletedComparisons - state.AcceptedComparisonCount;
            return totalRemaining > 0 ? $"Next: {totalRemaining} more comparison{(totalRemaining == 1 ? string.Empty : "s")}" : "Complete the required comparisons";
        }

        private string ObjectiveActionLabel(
            InvestigationV2ObjectiveDefinition objective,
            ThreatSimulationDefinition threat)
        {
            string target;
            if (objective.TargetKind == PredictionTargetKind.Temperature) target = "temperature";
            else target = caseDefinition.FindSpecies(objective.TargetId)?.DisplayName ?? objective.TargetId;
            return $"Compare {target} for {threat?.DisplayName ?? objective.ThreatId}";
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
            IReadOnlyList<PredictionAnimationTarget> benthicTargets)
        {
            for (int index = 0; index < foodWebTargets.Count; index++)
            {
                PreparePredictionAnimationTarget(foodWebTargets[index]);
            }
            for (int index = 0; index < benthicTargets.Count; index++)
            {
                PreparePredictionAnimationTarget(benthicTargets[index]);
            }

            yield return new WaitForSecondsRealtime(0.56f);
            for (int index = 0; index < foodWebTargets.Count; index++)
            {
                PredictionAnimationTarget target = foodWebTargets[index];
                yield return AnimatePredictionTarget(target, 1.04f);
                yield return new WaitForSecondsRealtime(0.32f);
            }

            for (int index = 0; index < benthicTargets.Count; index++)
            {
                BeginPredictionAnimationTarget(benthicTargets[index]);
            }
            float benthicElapsed = 0f;
            const float benthicDuration = 1.04f;
            while (benthicElapsed < benthicDuration)
            {
                benthicElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(benthicElapsed / benthicDuration);
                float eased = t * t * (3f - 2f * t);
                for (int index = 0; index < benthicTargets.Count; index++)
                {
                    ApplyPredictionAnimationTarget(benthicTargets[index], eased, t);
                }
                yield return null;
            }
            for (int index = 0; index < benthicTargets.Count; index++)
            {
                CompletePredictionAnimationTarget(benthicTargets[index]);
            }
        }

        private IEnumerator AnimatePredictionTarget(PredictionAnimationTarget target, float duration)
        {
            BeginPredictionAnimationTarget(target);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                ApplyPredictionAnimationTarget(target, eased, t);
                yield return null;
            }
            CompletePredictionAnimationTarget(target);
        }

        private static void PreparePredictionAnimationTarget(PredictionAnimationTarget target)
        {
            if (target == null) return;
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

        private static void BeginPredictionAnimationTarget(PredictionAnimationTarget target)
        {
            if (target?.StateLabel == null) return;
            target.StateLabel.text = $"Stable → {PredictionLabel(target.State)}";
            target.StateLabel.color = target.StateColor;
        }

        private static void ApplyPredictionAnimationTarget(PredictionAnimationTarget target, float eased, float rawT)
        {
            if (target == null) return;
            if (target.Artwork != null)
            {
                float scale = Mathf.Lerp(1f, FinalArtworkScale(target.State), eased);
                if (target.State == PredictionState.Increase)
                {
                    scale *= 1f + Mathf.Sin(rawT * Mathf.PI) * 0.08f;
                }
                target.Artwork.localScale = Vector3.one * scale;
            }
            if (target.ArtworkGroup != null)
                target.ArtworkGroup.alpha = Mathf.Lerp(1f, FinalArtworkAlpha(target.State), eased);
            if (target.IndicatorGroup != null) target.IndicatorGroup.alpha = eased;
            float startCrowdAlpha = target.State == PredictionState.Decrease ? 0.58f : 0f;
            float finalCrowdAlpha = target.State == PredictionState.Increase ? 0.58f : 0f;
            SetCrowdAlpha(target.CrowdMembers, Mathf.Lerp(startCrowdAlpha, finalCrowdAlpha, eased));
        }

        private static void CompletePredictionAnimationTarget(PredictionAnimationTarget target)
        {
            if (target == null) return;
            if (target.Artwork != null) target.Artwork.localScale = Vector3.one * FinalArtworkScale(target.State);
            if (target.ArtworkGroup != null) target.ArtworkGroup.alpha = FinalArtworkAlpha(target.State);
            if (target.IndicatorGroup != null) target.IndicatorGroup.alpha = 1f;
            SetCrowdAlpha(target.CrowdMembers, target.State == PredictionState.Increase ? 0.58f : 0f);
            if (target.StateLabel != null) target.StateLabel.text = PredictionLabel(target.State);
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
