using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private SimulationResult DeskSimulation => workbenchFoodWebReady && !restingExperiments.Contains(selectedThreatId)
            ? state.FindSimulation(selectedThreatId) : null;

        private void AddDeskDropTargets(GameObject target)
        {
            target.AddComponent<InvestigationWorkbenchDrop>().Configure("cause", ApplyWorkbenchExperiment);
            target.AddComponent<InvestigationWorkbenchDrop>().Configure("evidence-pattern", CompareWorkbenchPattern);
        }

        private RectTransform CreateDeskColumn(Transform parent, string name, Color color)
        {
            RectTransform panel = CreatePanel(name, parent, color, InvestigationTheme.SmallRadius);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return panel;
        }

        private void RenderFoodChainDesk()
        {
            if (string.IsNullOrEmpty(selectedThreatId)) selectedThreatId = state.ActiveThreatId;
            SimulationResult simulation = DeskSimulation;
            RectTransform workspace = new GameObject("Simulate Workspace", typeof(RectTransform), typeof(InvestigationResponsiveSplitLayout)).GetComponent<RectTransform>();
            workspace.SetParent(contentRoot, false);
            var split = workspace.GetComponent<InvestigationResponsiveSplitLayout>();
            const float leftHeight = 472f;
            split.Configure(.63f, 12f, 960f, leftHeight, leftHeight);
            RectTransform left = CreateDeskColumn(workspace, "Simulate Left Column", Color.clear);
            RenderSimulateIntroduction(left);
            RectTransform models = CreateDeskColumn(left, "Simulation Models", InvestigationTheme.SurfaceQuiet);
            FixSimulationHeight(models, 434f);
            models.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(6, 6, 4, 4);
            RectTransform threats = CreatePanel("Threat Choices", models, Color.clear, 0f);
            FixSimulationHeight(threats, 68f);
            var threatLayout = threats.gameObject.AddComponent<HorizontalLayoutGroup>();
            threatLayout.spacing = 6f; threatLayout.childControlWidth = threatLayout.childControlHeight = true;
            threatLayout.childForceExpandWidth = threatLayout.childForceExpandHeight = true;
            foreach (var threat in caseDefinition.Threats)
            {
                CreateThreatButton(threats, threat);
                Button choice = threats.Find("Threat " + threat.ThreatId).GetComponent<Button>();
                choice.interactable = workbenchFoodWebReady;
                if (workbenchFoodWebReady && state.Difficulty == InvestigationDifficulty.Easy && EdnaCuesVisible)
                {
                    var next = FindNextGuidedObjective();
                    bool needsCause = string.IsNullOrEmpty(selectedThreatId) || (simulation != null && next != null && next.ThreatId != selectedThreatId);
                    GetThreatCheckProgress(threat.ThreatId, out int complete, out int total, out bool waiting);
                    if (needsCause && !waiting && (total == 0 || complete < total))
                    {
                        var arrow = AddActionArrow("Choose Cause Arrow", choice.transform);
                        Anchor(arrow.rectTransform, 1f, .5f, 1f, .5f, -28f, -12f, -6f, 12f);
                    }
                }
            }
            RectTransform model = CreateDeskColumn(models, "Model Workspace", InvestigationTheme.Deep);
            FixSimulationHeight(model, 352f);
            model.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(6, 6, 6, 6);
            model.GetComponent<VerticalLayoutGroup>().spacing = 4f;
            model.GetComponent<Image>().raycastTarget = true;
            AddDeskDropTargets(model.gameObject);
            Text title = CreateText("Model Title", model, !workbenchFoodWebReady ? "Connect your recorded species · arrows mean EATS"
                : simulation == null ? "Your food-chain model · stable baseline" : caseDefinition.FindThreat(simulation.ThreatId).DisplayName + " · model prediction",
                15, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(title.rectTransform, 24f, 0f);
            List<PredictionAnimationTarget> chainTargets = new List<PredictionAnimationTarget>();
            List<PredictionAnimationTarget> controlTargets = new List<PredictionAnimationTarget>();
            RectTransform chainFrame = CreatePanel(workbenchFoodWebReady ? "Food Web Prediction" : "Food Web Assembly", model, Color.clear, 0f);
            AddLayout(chainFrame, 188f, 0f);
            RectTransform chain = CreatePanel(simulation == null ? "Workbench Baseline" : "Food Web Cards", chainFrame, Color.clear, 0f);
            Stretch(chain, 0f, 0f, 0f, 0f);
            var row = chain.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 4f; row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false; row.childForceExpandHeight = true;
            for (int i = 0; i < caseDefinition.FoodWebChainSpeciesIds.Count; i++)
            {
                string id = caseDefinition.FoodWebChainSpeciesIds[i];
                if (i > 0) CreateDeskFeedingLink(chain, caseDefinition.FoodWebChainSpeciesIds[i - 1], id);
                CreateDeskSpeciesCard(chain, id, simulation, chainTargets);
            }
            RectTransform environment = CreatePanel("Environmental Predictions", model, InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
            AddLayout(environment, 40f, 0f);
            var envLayout = environment.gameObject.AddComponent<HorizontalLayoutGroup>();
            envLayout.padding = new RectOffset(4, 4, 2, 2); envLayout.spacing = 6f;
            envLayout.childControlWidth = envLayout.childControlHeight = true; envLayout.childForceExpandWidth = true; envLayout.childForceExpandHeight = false;
            CreateModelIndicatorChip(environment, "SEAFLOOR", simulation == null ? "Baseline" : CompactIndicatorValue(simulation.SeafloorPrediction), InvestigationTheme.Success);
            CreateModelIndicatorChip(environment, "LOOK FOR", simulation == null ? "Apply a cause" : CompactIndicatorValue(simulation.PhysicalConfirmation), InvestigationTheme.Accent);
            RectTransform controls = CreatePanel("Reference Indicators", model, Color.clear, 0f);
            AddLayout(controls, 76f, 0f);
            var controlsLayout = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.spacing = 8f; controlsLayout.childControlWidth = controlsLayout.childControlHeight = true;
            controlsLayout.childForceExpandWidth = controlsLayout.childForceExpandHeight = true;
            foreach (string id in caseDefinition.BenthicIndicatorSpeciesIds) CreateDeskControlCard(controls, id, simulation, controlTargets);
            if (simulation != null && animatedThreatIds.Add(simulation.ThreatId) && !InvestigationMotionSettings.ReducedMotion)
                StartCoroutine(AnimateSimulationSequence(chainTargets, controlTargets));

            RectTransform right = CreateDeskColumn(workspace, "Simulate Right Column", Color.clear);
            RectTransform evidence = CreateDeskColumn(right, "Comparison Workspace", InvestigationTheme.Paper);
            evidence.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
            if (workbenchInspectPrediction && simulation != null)
            {
                evidence.GetComponent<Image>().color = InvestigationTheme.SurfaceQuiet;
                Button table = CreateButton("Return To Evidence Table", evidence, "Back to my recorded patterns", ButtonVisualStyle.Tertiary,
                    () => { workbenchInspectPrediction = false; RefreshPresentationOnly(); }, out _);
                ConfigureCompactNavigationButton(table, 250f);
                RenderSimulationComparison(evidence, simulation, CalculateSimulationPairingHeight(simulation));
            }
            else RenderDeskEvidenceRecords(evidence, simulation);
            RenderSimulationNavigation(evidence, simulation);
            if (!workbenchFoodWebReady) FindNamedRect(evidence, "Edit Food Web").GetComponent<Button>().interactable = false;
            if (!workbenchInspectPrediction || simulation == null)
            {
                foreach (string name in new[] { "Edit Food Web", "Back To Observe" })
                {
                    RectTransform action = FindNamedRect(evidence, name);
                    if (action != null) action.GetComponentInChildren<Text>().color = InvestigationTheme.PaperInk;
                }
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(evidence);
            split.Configure(.63f, 12f, 960f, leftHeight, Mathf.Max(leftHeight, LayoutUtility.GetPreferredHeight(evidence)));
        }

        private void CreateDeskFeedingLink(Transform parent, string predatorId, string preyId)
        {
            string key = caseDefinition.FindSpecies(predatorId).CanonicalSpeciesId + ">" + caseDefinition.FindSpecies(preyId).CanonicalSpeciesId;
            bool linked = workbenchLinks.Contains(key);
            RectTransform link = CreatePanel("Feeding Link " + predatorId + " " + preyId, parent, Color.clear, 0f);
            var size = link.gameObject.AddComponent<LayoutElement>(); size.minWidth = size.preferredWidth = 44f; size.flexibleWidth = 0f;
            Text arrow = CreateText("Food Web Arrow", link, linked ? "→\neats" : "…\neats", 13, FontStyle.Bold,
                linked ? InvestigationTheme.Primary : InvestigationTheme.TextMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Stretch(arrow.rectTransform, -2f, 0f, 2f, 0f);
            if (!workbenchFoodWebReady && linked)
            {
                Button remove = CreateButton("Remove Food Web Link " + key, link, "×", ButtonVisualStyle.Tertiary,
                    () => { workbenchLinks.Remove(key); workbenchLinkSource = string.Empty; RefreshPresentationOnly(); }, out _);
                remove.GetComponent<LayoutElement>().ignoreLayout = true;
                Anchor(remove.GetComponent<RectTransform>(), 0f, 0f, 1f, 0f, 0f, 2f, 0f, 46f);
            }
        }

        private void DrawDeskSurveySnapshot(Transform parent, InvestigationSpeciesDefinition species, SurveyEra era, float minX, float maxX)
        {
            Text date = CreateText("Recorded Date " + era, parent, era == SurveyEra.Historical ? "20 YEARS AGO" : "TODAY", 10,
                FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.DataFont);
            Anchor(date.rectTransform, minX, .80f, maxX, .88f, 4f, 0f, -4f, 0f);
            if (ResolveSurveySummary(species, era)?.Detection == SpeciesDetectionState.NotDetected)
            {
                Text absent = CreateText("Recorded Absence " + era, parent, "Not\ndetected", 11, FontStyle.Bold,
                    InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Anchor(absent.rectTransform, minX, .59f, maxX, .80f, 4f, 0f, -4f, 0f);
            }
            else
            {
                RectTransform art = CreateSurveyArtwork("Recorded Survey " + era, parent, species, era);
                Anchor(art, minX, .59f, maxX, .80f, 5f, 2f, -5f, -2f);
            }
        }

        private void CreateDeskSpeciesCard(Transform parent, string id, SimulationResult simulation, List<PredictionAnimationTarget> targets)
        {
            var species = caseDefinition.FindSpecies(id);
            var finding = FindObserveObservationForSpecies(id);
            RectTransform card = CreatePanel("Case Record " + id, parent, InvestigationTheme.Paper, InvestigationTheme.SmallRadius);
            var width = card.gameObject.AddComponent<LayoutElement>(); width.minWidth = 110f; width.flexibleWidth = 1f;
            card.GetComponent<Image>().raycastTarget = true; AddDeskDropTargets(card.gameObject);
            Text name = CreateText("Recorded Species Name", card, species.GameplayName, 15, FontStyle.Bold, InvestigationTheme.PaperInk,
                TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Anchor(name.rectTransform, 0f, .88f, 1f, 1f, 6f, 0f, -6f, -1f);
            DrawDeskSurveySnapshot(card, species, SurveyEra.Historical, 0f, .5f);
            DrawDeskSurveySnapshot(card, species, SurveyEra.Current, .5f, 1f);
            Text observation = CreateText("Recorded Finding", card, finding?.DisplayName ?? "Survey record", 12, FontStyle.Bold,
                InvestigationTheme.PaperInk, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Anchor(observation.rectTransform, 0f, .42f, 1f, .59f, 7f, 0f, -7f, 0f);
            string diet = "Part of this teaching model";
            foreach (var edge in caseDefinition.FoodWebEdges)
                if (edge.NetworkId == caseDefinition.SimulationFoodWebId && edge.PredatorSpeciesId == species.CanonicalSpeciesId)
                    diet = "Diet hint: " + caseDefinition.FindSpecies(edge.PreySpeciesId).GameplayName;
            if (id == "krill") diet = "Feeds on plankton";
            Text hint = CreateText("Diet Note", card, diet, 11, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Anchor(hint.rectTransform, 0f, .33f, 1f, .42f, 4f, 0f, -4f, 0f);
            RectTransform prediction = CreatePanel("Model Slot " + id, card, InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
            Anchor(prediction, 0f, 0f, 1f, .33f, 4f, 4f, -4f, 0f);
            RenderDeskPrediction(prediction, species, simulation, targets);
            if (!workbenchFoodWebReady)
            {
                Button connect = CreateButton("Connect Species " + id, card, string.Empty, ButtonVisualStyle.Choice, () =>
                {
                    if (string.IsNullOrEmpty(workbenchLinkSource)) { workbenchLinkSource = id; workbenchFeedback = "Now choose what " + species.GameplayName + " eats."; RefreshPresentationOnly(); }
                    else ConnectWorkbenchSpecies(workbenchLinkSource, id);
                }, out Text hidden);
                hidden.gameObject.SetActive(false); connect.targetGraphic.color = Color.clear;
                connect.GetComponent<LayoutElement>().ignoreLayout = true; Stretch(connect.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
                connect.gameObject.AddComponent<InvestigationWorkbenchDrag>().Configure("food-web", id, species.GameplayName,
                    preview => Stretch(CreateSurveyArtwork("Carried Record", preview, species, SurveyEra.Historical), 4f, 4f, -4f, -4f));
                connect.gameObject.AddComponent<InvestigationWorkbenchDrop>().Configure("food-web", from => ConnectWorkbenchSpecies(from, id));
                if (workbenchLinkSource == id) AddChoiceBorderCue("Selected Feeding Source", card);
            }
        }

        private void RenderDeskPrediction(RectTransform parent, InvestigationSpeciesDefinition species, SimulationResult simulation, List<PredictionAnimationTarget> targets)
        {
            var prediction = simulation?.FindPrediction(species.SpeciesId);
            if (prediction == null)
            {
                RectTransform baseline = CreatePanel("Baseline " + species.SpeciesId, parent, Color.clear, 0f);
                Stretch(baseline, 0f, 0f, 0f, 0f);
                Text stateText = CreateText("Baseline Label", baseline, workbenchFoodWebReady ? "MODEL · Stable" : "MODEL · Connect links", 12,
                    FontStyle.Bold, InvestigationTheme.TextSecondary, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Stretch(stateText.rectTransform, 4f, 4f, -4f, -4f);
                return;
            }
            Button node = CreatePredictionButton(parent, species, prediction, 0f, 0f, true);
            node.GetComponent<LayoutElement>().ignoreLayout = true;
            Stretch(node.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            Text name = node.transform.Find("Name").GetComponent<Text>(); name.text = "MODEL"; name.fontSize = 10;
            Anchor(name.rectTransform, .35f, .64f, .82f, 1f, 6f, 0f, -2f, -2f);
            Text stateLabel = node.transform.Find("Prediction").GetComponent<Text>();
            Anchor(stateLabel.rectTransform, .35f, 0f, 1f, .63f, 6f, 3f, -6f, 0f);
            ConfigureContentDrivenText(stateLabel);
            targets.Add(CreatePredictionAnimationTarget(node, prediction));
        }

        private void CreateDeskControlCard(Transform parent, string id, SimulationResult simulation, List<PredictionAnimationTarget> targets)
        {
            var species = caseDefinition.FindSpecies(id);
            RectTransform card = CreatePanel("Control Record " + id, parent, InvestigationTheme.Paper, InvestigationTheme.SmallRadius);
            card.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            card.GetComponent<Image>().raycastTarget = true; AddDeskDropTargets(card.gameObject);
            RectTransform art = CreateSurveyArtwork("Control Survey Artwork", card, species, SurveyEra.Current);
            Anchor(art, 0f, .20f, .20f, 1f, 3f, 0f, 0f, -8f);
            Text label = CreateText("Control Finding", card, (id == "mussel" ? "Mussel" : species.GameplayName) + "\nSurvey: stable", 12,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(label.rectTransform, .20f, .15f, .49f, 1f, 4f, 0f, -3f, -5f);
            Text role = CreateText("Control Role", card, "CONTROL RECORD", 9, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleLeft, InvestigationTheme.DataFont);
            Anchor(role.rectTransform, 0f, 0f, .49f, .22f, 6f, 0f, -3f, 0f);
            RectTransform model = CreatePanel("Control Model Slot", card, InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
            Anchor(model, .50f, 0f, 1f, 1f, 0f, 4f, -4f, -4f);
            RenderDeskPrediction(model, species, simulation, targets);
        }

        private void RenderDeskEvidenceRecords(RectTransform parent, SimulationResult simulation)
        {
            Text heading = CreateText("Workbench Evidence Heading", parent, "YOUR SAVED FINDINGS", 15, FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(heading.rectTransform, 24f, 0f);
            string instruction = !workbenchFoodWebReady ? (string.IsNullOrEmpty(workbenchFeedback) ? "Use the diet hints to connect the cards." : workbenchFeedback)
                : simulation == null ? "Your records stay here while you try a cause." : "Drop a finding pattern onto the model to check it.";
            Text note = CreateText("Workbench Evidence Instructions", parent, instruction, 13, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(note);
            string[] patterns = { "food-web", "benthic", "indicator" };
            foreach (string pattern in patterns)
            {
                bool chain = pattern == "food-web";
                string[] ids = chain ? new[] { "shark", "tuna", "krill" } : new[] { pattern == "benthic" ? "sea_star" : "mussel" };
                Button card = CreateButton("Evidence Pattern " + pattern, parent, string.Empty, ButtonVisualStyle.PaperChoice,
                    () => { workbenchEvidence = pattern; RefreshPresentationOnly(); }, out Text hidden);
                hidden.gameObject.SetActive(false);
                card.GetComponent<LayoutElement>().minHeight = card.GetComponent<LayoutElement>().preferredHeight = chain ? 96f : 60f;
                card.interactable = simulation != null;
                ColorBlock colors = card.colors; colors.disabledColor = Color.white; card.colors = colors;
                if (workbenchEvidence == pattern) card.targetGraphic.color = InvestigationTheme.PaperSelected;
                if (simulation != null) AddWorkbenchDrag(card, "evidence-pattern", pattern, chain ? "Your food-web findings" : caseDefinition.FindSpecies(ids[0]).GameplayName + " survey record");
                if (chain)
                {
                    for (int i = 0; i < ids.Length; i++)
                    {
                        var species = caseDefinition.FindSpecies(ids[i]);
                        var finding = FindObserveObservationForSpecies(ids[i]);
                        RectTransform art = CreateSurveyArtwork("Saved Evidence " + ids[i], card.transform, species,
                            finding.ClaimType == ObservationClaimType.NotDetected ? SurveyEra.Historical : SurveyEra.Current);
                        Anchor(art, i / 3f, .43f, (i + 1) / 3f, 1f, 6f, 0f, -6f, -5f);
                        Text label = CreateText("Saved Evidence Label " + ids[i], card.transform,
                            species.GameplayName + "\n" + (finding.ClaimType == ObservationClaimType.NotDetected ? "Not detected" : "More sites"), 12,
                            FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                        Anchor(label.rectTransform, i / 3f, 0f, (i + 1) / 3f, .43f, 3f, 3f, -3f, 0f);
                    }
                }
                else
                {
                    var species = caseDefinition.FindSpecies(ids[0]);
                    RectTransform art = CreateSurveyArtwork("Saved Evidence " + ids[0], card.transform, species, SurveyEra.Current);
                    Anchor(art, 0f, 0f, .24f, 1f, 6f, 7f, -3f, -7f);
                    Text label = CreateText("Saved Control Finding", card.transform,
                        (pattern == "benthic" ? "Sea star" : "Mussel") + " · stable in the survey", 14, FontStyle.Bold,
                        InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
                    Anchor(label.rectTransform, .25f, 0f, 1f, 1f, 5f, 5f, -8f, -5f);
                }
            }
            if (!workbenchFoodWebReady)
            {
                int required = 0;
                foreach (var edge in caseDefinition.FoodWebEdges) if (edge.NetworkId == caseDefinition.SimulationFoodWebId) required++;
                if (workbenchLinks.Count == required)
                    CreateButton("Use Food Web", parent, "Keep these connections", ButtonVisualStyle.PaperPrimary,
                        () => { workbenchFoodWebReady = true; RefreshPresentationOnly(); }, out _);
                else
                {
                    Text status = CreateText("Food Web Assembly Status", parent, $"CONNECTIONS {workbenchLinks.Count}/{required} · 3-species teaching model", 12,
                        FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                    ConfigureContentDrivenText(status);
                }
            }
            else if (simulation == null && caseDefinition.FindThreat(selectedThreatId) != null)
            {
                Button run = CreateButton("Run Selected Model", parent, "Apply to model", ButtonVisualStyle.PaperPrimary, () => ApplyWorkbenchExperiment(selectedThreatId), out _);
                if (state.Difficulty == InvestigationDifficulty.Easy && EdnaCuesVisible) AddActionArrow("Run Model Arrow", run.transform);
            }
            else if (simulation != null)
            {
                Button test = CreateButton("Test Evidence Pattern", parent, "Check selected pattern", ButtonVisualStyle.PaperPrimary,
                    () => CompareWorkbenchPattern(workbenchEvidence), out Text label);
                ConfigureWrappingChoice(test, label); test.interactable = !string.IsNullOrEmpty(workbenchEvidence);
                test.gameObject.AddComponent<InvestigationWorkbenchDrop>().Configure("evidence-pattern", CompareWorkbenchPattern);
                var summary = new InvestigationHypothesisSummary(caseDefinition, state, selectedThreatId);
                Text result = CreateText("Workbench Comparison Result", parent, $"Saved checks: {summary.SupportCount} support · {summary.ChallengeCount} challenge · {summary.OpenCount} open", 12,
                    FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                ConfigureContentDrivenText(result);
                Button reset = CreateButton("Reset Workbench Experiment", parent, "Remove disturbance · keep my checks", ButtonVisualStyle.PaperChoice, ResetWorkbenchExperiment, out _);
                ConfigureCompactNavigationButton(reset, 290f);
            }
        }
    }
}
