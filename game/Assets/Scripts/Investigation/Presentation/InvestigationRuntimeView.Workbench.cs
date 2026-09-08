using System;
using System.Collections.Generic;
using EDNA.Investigation.Domain;
using EDNA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private float surveyLensValue = .5f;
        private bool surveyLensExplored;
        private string observeQuestionFeedback = string.Empty;
        private string workbenchFeedback = string.Empty;
        private readonly HashSet<string> workbenchLinks = new HashSet<string>(StringComparer.Ordinal);
        private string workbenchLinkSource = string.Empty;
        private bool workbenchFoodWebReady;
        private bool workbenchInspectPrediction;
        private readonly HashSet<string> restingExperiments = new HashSet<string>(StringComparer.Ordinal);
        private string workbenchEvidence = string.Empty;

        private void ResetWorkbench()
        {
            surveyLensValue = .5f; surveyLensExplored = false; observeQuestionFeedback = string.Empty; workbenchFeedback = string.Empty;
            workbenchLinks.Clear(); workbenchLinkSource = string.Empty; workbenchFoodWebReady = false;
            workbenchInspectPrediction = false; restingExperiments.Clear(); workbenchEvidence = string.Empty;
            rovScanId = string.Empty; rovScanProgress = 0f; rovScanRegions = 0; rovScanFound = false;
            reportCrossCheck = ReportKeyClue.None; reportStagedClue = ReportKeyClue.None;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void PrepareWorkbenchQa(InvestigationQaCheckpoint checkpoint)
        {
            if (checkpoint == InvestigationQaCheckpoint.Start || checkpoint == InvestigationQaCheckpoint.FirstFinding) return;
            foreach (FoodWebEdgeDefinition edge in caseDefinition.FoodWebEdges)
                if (edge.NetworkId == caseDefinition.SimulationFoodWebId)
                    workbenchLinks.Add(edge.PredatorSpeciesId + ">" + edge.PreySpeciesId);
            workbenchFoodWebReady = true;
        }
#endif

        private Slider CreateWorkbenchSlider(string name, Transform parent, float value, Action<float> changed)
        {
            RectTransform track = CreatePanel(name, parent, InvestigationTheme.SurfaceRaised, InvestigationTheme.SmallRadius);
            AddLayout(track, 44f, 1f);
            Slider slider = track.gameObject.AddComponent<Slider>();
            track.GetComponent<Image>().raycastTarget = true;
            slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
            RectTransform line = CreatePanel("Track", track, InvestigationTheme.Primary, 3f);
            Anchor(line, 0f, .5f, 1f, .5f, 16f, -2f, -16f, 2f);
            RectTransform area = CreatePanel("Handle Area", track, Color.clear, 0f);
            Stretch(area, 20f, 0f, -20f, 0f);
            RectTransform handle = CreatePanel("Handle", area, InvestigationTheme.Paper, 10f);
            handle.sizeDelta = new Vector2(36f, -8f);
            handle.GetComponent<Image>().raycastTarget = true;
            slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => changed?.Invoke(v));
            return slider;
        }

        private void RenderSurveyLens(RectTransform parent)
        {
            RectTransform maps = CreatePanel("Survey Lens Maps", parent, Color.clear, 0f);
            Anchor(maps, 0f, 0f, 1f, 1f, 0f, 54f, 0f, 0f);
            CreateSurveyMap(maps, SurveyEra.Historical);
            RectTransform historical = FindNamedRect(maps, "Historical Seamount");
            Stretch(historical, 0f, 0f, 0f, 0f);
            CreateSurveyMap(maps, SurveyEra.Current);
            RectTransform current = FindNamedRect(maps, "Current Seamount");
            Stretch(current, 0f, 0f, 0f, 0f);
            // Both surveys occupy the same coordinates; the lens clips without
            // scaling either map or changing its species hit areas.
            // RectMask2D clips descendants, not the Graphic on its own object.
            // Put the opaque cover inside the mask so it reveals the baseline too.
            current.GetComponent<Image>().color = Color.clear;
            RectTransform currentWater = CreatePanel("Current Survey Water", current, InvestigationTheme.MapSurface, InvestigationTheme.CardRadius);
            Stretch(currentWater, 0f, 0f, 0f, 0f);
            currentWater.SetAsFirstSibling();
            RectMask2D mask = current.gameObject.AddComponent<RectMask2D>();
            RectTransform divider = CreatePanel("Survey Lens Divider", maps, InvestigationTheme.Paper, 0f);
            divider.GetComponent<Image>().raycastTarget = false;
            RectTransform controls = CreatePanel("Survey Lens Controls", parent, Color.clear, 0f);
            Anchor(controls, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 48f);
            Slider slider = CreateWorkbenchSlider("Survey Time Lens", controls, surveyLensValue, v => surveyLensValue = v);
            Stretch(slider.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            maps.gameObject.AddComponent<InvestigationSurveyLens>().Configure(mask, divider, slider);
            current.gameObject.AddComponent<InvestigationLensRaycastFilter>().Configure(slider);
            historical.gameObject.AddComponent<InvestigationLensRaycastFilter>().Configure(slider, true);
            FindNamedRect(current, "Survey Title").gameObject.SetActive(false);
            Text today = CreateText("Lens Today Label", maps, "Today", 18, FontStyle.Bold, InvestigationTheme.Primary,
                TextAnchor.UpperRight, InvestigationTheme.DisplayFont);
            Anchor(today.rectTransform, .7f, .88f, 1f, 1f, 0f, 0f, -14f, -10f);
            today.gameObject.SetActive(surveyLensValue < .99f);
            Text historyTitle = FindNamedRect(historical, "Survey Title").GetComponent<Text>();
            historyTitle.gameObject.SetActive(surveyLensValue > .01f);
            slider.onValueChanged.AddListener(v =>
            {
                today.gameObject.SetActive(v < .99f);
                historyTitle.gameObject.SetActive(v > .01f);
            });
            if (!surveyLensExplored)
            {
                AddChoiceBorderCue("Survey Lens Handle Cue", slider.handleRect);
                RectTransform cue = FindNamedRect(slider.handleRect, "Survey Lens Handle Cue");
                Stretch(cue, -5f, -5f, 5f, 5f);
                float initialValue = surveyLensValue;
                slider.onValueChanged.AddListener(v =>
                {
                    if (Mathf.Abs(v - initialValue) < .04f) return;
                    surveyLensExplored = true;
                    cue.gameObject.SetActive(false);
                });
            }
            RectTransform handleArea = FindNamedRect(slider.transform, "Handle Area");
            Stretch(handleArea, 20f, 18f, -20f, -2f);
            Anchor(FindNamedRect(slider.transform, "Track"), 0f, .66f, 1f, .66f, 16f, -2f, -16f, 2f);
            Text caption = CreateText("Survey Lens Caption", slider.transform, "Today  ←  slide to compare  →  20 years ago", 12, FontStyle.Bold,
                InvestigationTheme.TextPrimary, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Anchor(caption.rectTransform, 0f, 0f, 1f, 0f, 42f, 0f, -42f, 18f);
            caption.raycastTarget = false;
        }

        private enum ObserveAnswer { NotDetected, MoreSites, FewerSites, Stable }

        private InvestigationObservationDefinition ObserveQuestion
        {
            get
            {
                if (caseDefinition == null || state == null) return null;
                foreach (InvestigationObservationDefinition finding in caseDefinition.Observations)
                    if (InvestigationObserveEvaluator.IsInitialFinding(finding)
                        && !state.HasDiscoveredObservation(finding.EvidenceId)) return finding;
                return null;
            }
        }

        private static ObserveAnswer AnswerForFinding(InvestigationObservationDefinition finding)
        {
            switch (finding.ClaimType)
            {
                case ObservationClaimType.NotDetected: return ObserveAnswer.NotDetected;
                case ObservationClaimType.ChangedDepthOrDistribution:
                case ObservationClaimType.NewDetection: return ObserveAnswer.MoreSites;
                default: return ObserveAnswer.Stable;
            }
        }

        private static string ObserveAnswerLabel(ObserveAnswer answer)
        {
            switch (answer)
            {
                case ObserveAnswer.NotDetected: return "Not detected today";
                case ObserveAnswer.MoreSites: return "Detected at more sites";
                case ObserveAnswer.FewerSites: return "Detected at fewer sites";
                default: return "About the same";
            }
        }

        private RectTransform RenderObserveQuestion(Transform parent)
        {
            InvestigationObservationDefinition finding = ObserveQuestion;
            if (finding == null) return null;
            EnsureEdnaArtwork();
            RectTransform panel = CreatePanel("Observe Question", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12); layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            RectTransform header = CreatePanel("Observe Edna Header", panel, Color.clear, 0f);
            AddLayout(header, 74f, 0f);
            Image portrait = CreateStatusIcon("Edna Introduction Portrait", header, ednaPortrait, Color.white);
            Anchor(portrait.rectTransform, 1f, 0f, 1f, 1f, -66f, 0f, 0f, 4f);
            Text name = CreateText("Edna Name", header, $"EDNA · {CountInitialFindings() + 1}/{InvestigationObserveEvaluator.RequiredCount(caseDefinition)}", 13,
                FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            Anchor(name.rectTransform, 0f, .6f, 1f, 1f, 0f, 0f, -70f, 0f);
            Text locator = CreateText("Edna Intro Locator", header, "Find EDNA at the top right.", 14,
                FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(locator.rectTransform, 0f, 0f, 1f, .62f, 0f, 0f, -70f, 0f);
            string species = caseDefinition.FindSpecies(finding.RelatedSpeciesId)?.GameplayName ?? finding.DisplayName;
            Text question = CreateText("Observe Question Text", panel, $"What changed for {species} in today's survey?", 19,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(question);
            Text instruction = CreateText("Observe Question Instruction", panel, "Slide to compare both surveys, then choose.", 14,
                FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(instruction);
            // Shuffle once per question/session, preserving order through retries and notebook visits.
            var answers = new List<ObserveAnswer>((ObserveAnswer[])Enum.GetValues(typeof(ObserveAnswer)));
            int seed = unchecked((int)InvestigationSpeciesMapLayout.StableOrder(observeLayoutSessionSeed, finding.EvidenceId, DepthBand.Shallow, false));
            var random = new System.Random(seed);
            for (int i = answers.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1); ObserveAnswer swap = answers[i]; answers[i] = answers[j]; answers[j] = swap;
            }
            RectTransform choices = new GameObject("Observe Answer Choices", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            choices.SetParent(panel, false);
            choices.GetComponent<InvestigationResponsiveGridLayout>().Configure(2, 2, 2, 60f, 8f);
            foreach (ObserveAnswer answer in answers)
            {
                Button choice = CreateButton("Observe Answer " + answer, choices, ObserveAnswerLabel(answer), ButtonVisualStyle.PaperChoice,
                    () => AnswerObserveQuestion(finding.EvidenceId, answer), out Text label);
                ConfigureWrappingChoice(choice, label);
            }
            Text feedback = CreateText("Observe Question Feedback", panel,
                string.IsNullOrEmpty(observeQuestionFeedback) ? "Symbols show survey detections, not animal counts." : observeQuestionFeedback,
                14, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(feedback);
            if (CountInitialFindings() > 0)
            {
                RectTransform footer = CreatePanel("Observe Notebook Tools", panel, Color.clear, 0f);
                AddLayout(footer, 50f, 0f);
                Button notebook = CreateNotebookDrawerButton(footer);
                Anchor(notebook.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f, 0f, 0f, 62f, 0f);
                Text note = CreateText("Observe Notebook Tip", footer,
                    notebookHasBeenOpened ? "Your findings are saved here." : "Open your notebook to revisit a finding and its evidence.",
                    14, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
                Stretch(note.rectTransform, 70f, 0f, 0f, 0f);
            }
            return panel;
        }

        private void AnswerObserveQuestion(string evidenceId, ObserveAnswer answer)
        {
            InvestigationObservationDefinition finding = ObserveQuestion;
            // A queued click from a previous question must not answer the next one.
            if (finding == null || finding.EvidenceId != evidenceId) return;
            if (answer != AnswerForFinding(finding))
            {
                observeQuestionFeedback = "Take another look: slide all the way to each end. Compare the highlighted organism's survey symbols.";
                RefreshPresentationOnly(); return;
            }
            observeQuestionFeedback = "Recorded: " + finding.DisplayName.TrimEnd('.') + "."
                + (finding.ClaimType == ObservationClaimType.NotDetected ? " Not detected does not mean gone." : string.Empty);
            lastRecordedObservationId = finding.EvidenceId;
            pendingTappedSpeciesId = string.Empty;
            discoverObservation?.Invoke(finding.EvidenceId);
        }

        private void AddWorkbenchDrag(Button button, string kind, string id, string label)
        {
            button.gameObject.AddComponent<InvestigationWorkbenchDrag>().Configure(kind, id, label);
        }

        private void ConnectWorkbenchSpecies(string from, string to)
        {
            if (from == to) { workbenchLinkSource = from; return; }
            string predator = caseDefinition.FindSpecies(from)?.CanonicalSpeciesId;
            string prey = caseDefinition.FindSpecies(to)?.CanonicalSpeciesId;
            bool allowed = false;
            foreach (FoodWebEdgeDefinition edge in caseDefinition.FoodWebEdges)
                if (edge.NetworkId == caseDefinition.SimulationFoodWebId && edge.PredatorSpeciesId == predator && edge.PreySpeciesId == prey) allowed = true;
            if (!allowed)
            {
                workbenchFeedback = "Check the diet notes. Connect the animal that eats to the animal it feeds on.";
                workbenchLinkSource = string.Empty; RefreshPresentationOnly(); return;
            }
            workbenchLinks.Add(predator + ">" + prey);
            workbenchLinkSource = string.Empty;
            int required = 0;
            foreach (FoodWebEdgeDefinition edge in caseDefinition.FoodWebEdges)
                if (edge.NetworkId == caseDefinition.SimulationFoodWebId) required++;
            workbenchFoodWebReady = workbenchLinks.Count == required;
            workbenchFeedback = workbenchFoodWebReady ? "Your food web is connected. Now place a cause on the model and see what changes." : "Connection added. Find the next feeding relationship.";
            RefreshPresentationOnly();
        }

        private void RenderFoodWebAssembly()
        {
            CreateHeading("Build your investigation model", "Connect who eats whom, then try a disturbance. You can drag a card onto its prey, or select two cards.");
            RectTransform paper = CreatePanel("Food Web Assembly", contentRoot, InvestigationTheme.SurfaceQuiet, InvestigationTheme.CardRadius);
            VerticalLayoutGroup layout = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16); layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            Text status = CreateText("Food Web Assembly Status", paper, string.IsNullOrEmpty(workbenchFeedback)
                ? "Start with the predator. Arrows mean EATS. Use the diet notes on each card." : workbenchFeedback,
                16, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(status);
            RectTransform row = new GameObject("Food Web Cards", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            row.SetParent(paper, false); row.GetComponent<InvestigationResponsiveGridLayout>().Configure(3, 3, 1, 172f, 12f);
            foreach (string id in caseDefinition.FoodWebChainSpeciesIds)
            {
                InvestigationSpeciesDefinition species = caseDefinition.FindSpecies(id);
                Button node = CreateButton("Connect Species " + id, row, string.Empty, ButtonVisualStyle.Choice, () =>
                {
                    if (string.IsNullOrEmpty(workbenchLinkSource)) { workbenchLinkSource = id; workbenchFeedback = "Now choose what " + species.GameplayName + " eats."; RefreshPresentationOnly(); }
                    else ConnectWorkbenchSpecies(workbenchLinkSource, id);
                }, out Text hidden);
                hidden.gameObject.SetActive(false);
                if (workbenchLinkSource == id) node.targetGraphic.color = InvestigationTheme.SurfaceRaised;
                RectTransform art = CreateSpeciesArtwork("Card Artwork", node.transform, species, InvestigationTheme.Primary);
                Anchor(art, 0f, .45f, 1f, 1f, 20f, 0f, -20f, -8f);
                Text name = CreateText("Species Name", node.transform, species.GameplayName, 20, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleCenter, InvestigationTheme.DisplayFont);
                Anchor(name.rectTransform, 0f, .25f, 1f, .48f, 8f, 0f, -8f, 0f);
                Text diet = CreateText("Diet Note", node.transform, species.Description, 12, FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.UpperCenter, InvestigationTheme.BodyFont);
                Anchor(diet.rectTransform, 0f, 0f, 1f, .25f, 10f, 5f, -10f, 0f);
                AddWorkbenchDrag(node, "food-web", id, species.GameplayName);
                node.gameObject.AddComponent<InvestigationWorkbenchDrop>().Configure("food-web", source => ConnectWorkbenchSpecies(source, id));
            }
            foreach (string link in workbenchLinks)
            {
                string[] ids = link.Split('>');
                CreateButton("Remove Food Web Link " + link, paper,
                    caseDefinition.FindSpecies(ids[0]).GameplayName + "  → eats →  " + caseDefinition.FindSpecies(ids[1]).GameplayName + "   ×",
                    ButtonVisualStyle.Tertiary, () => { workbenchLinks.Remove(link); workbenchFeedback = string.Empty; RefreshPresentationOnly(); }, out _);
            }
            int requiredLinks = 0;
            foreach (FoodWebEdgeDefinition edge in caseDefinition.FoodWebEdges)
                if (edge.NetworkId == caseDefinition.SimulationFoodWebId) requiredLinks++;
            RectTransform footer = CreatePanel("Food Web Tools", paper, Color.clear, 0f);
            HorizontalLayoutGroup tools = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            tools.spacing = 12f; tools.childAlignment = TextAnchor.MiddleRight;
            tools.childControlWidth = tools.childControlHeight = true;
            tools.childForceExpandWidth = tools.childForceExpandHeight = false;
            if (workbenchLinks.Count == requiredLinks)
            {
                Button use = CreateButton("Use Food Web", footer, "Use this food web", ButtonVisualStyle.Primary,
                    () => { workbenchFoodWebReady = true; RefreshPresentationOnly(); }, out _);
                use.GetComponent<LayoutElement>().flexibleWidth = 1f;
            }
            CreateNotebookDrawerButton(footer);
        }

        private void ApplyWorkbenchExperiment(string threatId)
        {
            if (!workbenchFoodWebReady || caseDefinition.FindThreat(threatId) == null) return;
            selectedThreatId = threatId; selectedPredictionSpeciesId = string.Empty; selectedObservationId = string.Empty;
            workbenchInspectPrediction = false; workbenchEvidence = string.Empty;
            restingExperiments.Remove(threatId); animatedThreatIds.Remove(threatId);
            runThreat?.Invoke(threatId);
        }

        private void ResetWorkbenchExperiment()
        {
            restingExperiments.Add(selectedThreatId);
            selectedPredictionSpeciesId = string.Empty; selectedObservationId = string.Empty;
            workbenchInspectPrediction = false;
            RefreshPresentationOnly();
        }

        private void RenderWorkbenchBaseline(RectTransform parent)
        {
            RectTransform row = CreatePanel("Workbench Baseline", parent, InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
            AddLayout(row, 146f, 1f);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8); layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            int nodeIndex = 0;
            foreach (string id in caseDefinition.FoodWebChainSpeciesIds)
            {
                InvestigationSpeciesDefinition species = caseDefinition.FindSpecies(id);
                if (nodeIndex++ > 0)
                {
                    Text arrow = CreateText("Baseline Feeding Link", row, "→", 24, FontStyle.Bold, InvestigationTheme.Primary,
                        TextAnchor.MiddleCenter, InvestigationTheme.DisplayFont);
                    LayoutElement width = AddLayout(arrow.rectTransform, 130f, 0f);
                    width.minWidth = width.preferredWidth = 24f;
                }
                RectTransform node = CreatePanel("Baseline " + id, row, Color.clear, 0f);
                RectTransform art = CreateSpeciesArtwork("Baseline Artwork", node, species, InvestigationTheme.Primary);
                Anchor(art, 0f, .4f, 1f, 1f, 12f, 0f, -12f, -6f);
                Text stateLabel = CreateText("Baseline Label", node, species.GameplayName + "\nStable", 14, FontStyle.Bold,
                    InvestigationTheme.TextPrimary, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Anchor(stateLabel.rectTransform, 0f, 0f, 1f, .4f, 4f, 0f, -4f, 0f);
            }
            if (caseDefinition.FindThreat(selectedThreatId) != null)
            {
                Button apply = CreateButton("Run Selected Model", parent, "Apply to model", ButtonVisualStyle.Primary,
                    () => ApplyWorkbenchExperiment(selectedThreatId), out _);
                AddLayout(apply.GetComponent<RectTransform>(), 44f, 1f);
                if (state.Difficulty == InvestigationDifficulty.Easy && EdnaCuesVisible)
                {
                    InvestigationGuideArrowGraphic arrow = AddActionArrow("Run Model Arrow", apply.transform);
                    Anchor(arrow.rectTransform, 1f, .5f, 1f, .5f, -34f, -14f, -8f, 14f);
                }
            }
            else
            {
                Text caption = CreateText("Baseline Caption", parent, "Your reconstructed food web · model starts stable", 12,
                    FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
                ConfigureContentDrivenText(caption);
            }
        }

        private void CompareWorkbenchPattern(string pattern)
        {
            if (pattern != "food-web" && pattern != "benthic" && pattern != "indicator") return;
            if (state.FindSimulation(selectedThreatId) == null || restingExperiments.Contains(selectedThreatId)) return;
            var ids = pattern == "food-web" ? new[] { "shark", "tuna", "krill" }
                : pattern == "benthic" ? new[] { "sea_star" } : new[] { "mussel" };
            // One pattern card represents several recorded findings. Each still
            // goes through the same scientific comparison rules.
            foreach (string id in ids)
            {
                InvestigationObservationDefinition finding = FindObserveObservationForSpecies(id);
                if (finding == null || !state.HasDiscoveredObservation(finding.EvidenceId)) continue;
                selectedPredictionSpeciesId = id; selectedPredictionTargetKind = PredictionTargetKind.Species;
                selectedObservationId = finding.EvidenceId;
                compareEvidence?.Invoke(selectedThreatId, PredictionTargetKind.Species, id, finding.EvidenceId);
            }
            workbenchEvidence = string.Empty; workbenchInspectPrediction = false;
            RefreshPresentationOnly();
        }

        private void RenderWorkbenchEvidenceTray(RectTransform parent, SimulationResult simulation)
        {
            Text heading = CreateText("Workbench Evidence Heading", parent, "YOUR EVIDENCE TABLE", 16, FontStyle.Bold, InvestigationTheme.Primary, TextAnchor.MiddleLeft, InvestigationTheme.DataFont);
            ConfigureContentDrivenText(heading);
            Text detail = CreateText("Workbench Evidence Instructions", parent, "Place a recorded pattern on this experiment. Watch which predictions it supports or challenges.", 14, FontStyle.Normal, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(detail);
            string[] patterns = { "food-web", "benthic", "indicator" };
            string[] labels = { "Food-web pattern · shark / tuna / krill", "Benthic check · stable sea star", "Pollution check · stable mussel" };
            for (int index = 0; index < patterns.Length; index++)
            {
                string pattern = patterns[index]; string title = labels[index];
                Button card = CreateButton("Evidence Pattern " + pattern, parent, title, ButtonVisualStyle.Choice,
                    () => { workbenchEvidence = pattern; RefreshPresentationOnly(); }, out Text text);
                ConfigureWrappingChoice(card, text);
                if (workbenchEvidence == pattern) card.targetGraphic.color = InvestigationTheme.SurfaceRaised;
                AddWorkbenchDrag(card, "evidence-pattern", pattern, title);
            }
            Button test = CreateButton("Test Evidence Pattern", parent, "Place selected pattern on the model", ButtonVisualStyle.Primary,
                () => CompareWorkbenchPattern(workbenchEvidence), out Text label);
            ConfigureWrappingChoice(test, label);
            test.interactable = !string.IsNullOrEmpty(workbenchEvidence);
            test.gameObject.AddComponent<InvestigationWorkbenchDrop>().Configure("evidence-pattern", CompareWorkbenchPattern);
            InvestigationHypothesisSummary summary = new InvestigationHypothesisSummary(caseDefinition, state, selectedThreatId);
            Text result = CreateText("Workbench Comparison Result", parent, $"Saved checks: {summary.SupportCount} support · {summary.ChallengeCount} challenge · {summary.OpenCount} open",
                13, FontStyle.Bold, InvestigationTheme.TextSecondary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(result);
            Button reset = CreateButton("Reset Workbench Experiment", parent, "Remove disturbance · show baseline", ButtonVisualStyle.Tertiary, ResetWorkbenchExperiment, out _);
            ConfigureCompactNavigationButton(reset, 250f);
        }
    }
}
