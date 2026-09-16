using TMPro;
using System;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum ScenarioDetailKind { Cause, Species, Prediction, Survey }
        private RectTransform scenarioDetailRoot, scenarioDetailOwner;
        private ScenarioDetailKind scenarioDetailKind;
        private string scenarioDetailThreat, scenarioDetailSpecies, scenarioDetailShown;
        private bool scenarioDetailPinned, scenarioDetailWasRevealed;
        private double scenarioDetailPausedAt = -1d;
        private readonly List<RectTransform> scenarioDetailHighlights = new List<RectTransform>();

        private void AttachScenarioDetail(RectTransform target, ScenarioDetailKind kind, string threat = "", string species = "")
        {
            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic == null) return;
            graphic.raycastTarget = false;
            // Unique focus targets keep the same species/scenario selected when
            // the workbench rebuilds at the end of playback.
            RectTransform hit = CreatePanel(("Detail " + kind + " " + threat + " " + species).Trim() + (target.name.StartsWith("Scenario Cause Icon", StringComparison.Ordinal) ? " Icon" : ""), target, Color.clear, 0f);
            Stretch(hit, 0f, 0f, 0f, 0f); hit.GetComponent<Image>().raycastTarget = true;
            Button button = hit.gameObject.AddComponent<Button>();
            button.targetGraphic = hit.GetComponent<Image>(); button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ShowScenarioDetail(hit, kind, threat, species, true));
            hit.gameObject.AddComponent<InvestigationHoverTooltipTrigger>().Configure(.5f,
                () => ShowScenarioDetail(hit, kind, threat, species, false),
                () => { if (scenarioDetailOwner == hit && !scenarioDetailPinned) CloseScenarioDetail(false); });
        }

        private bool ScenarioPredictionRevealed(string threat, string species)
        {
            if (scenarioStage != ScenarioStage.PlayingCause || scenarioActiveId != threat)
                return viewedScenarios.Contains(threat);
            var ids = ScenarioSpecies(threat);
            int index = ids.IndexOf(species);
            float begin = .10f + index * (.54f / Mathf.Max(1, ids.Count - 1));
            return index >= 0 && ScenarioPopulationProgress(threat, (float)((ScenarioPlaybackTime - scenarioStarted) / ScenarioSeconds)) > begin;
        }

        private string ScenarioDetailTitle(ScenarioDetailKind kind, string threat, string species)
        {
            if (kind == ScenarioDetailKind.Cause) return caseDefinition.FindThreat(threat)?.DisplayName ?? "Scenario";
            return (caseDefinition.FindSpecies(species)?.GameplayName ?? species)
                + (kind == ScenarioDetailKind.Prediction ? " · Model prediction" : kind == ScenarioDetailKind.Survey ? " · Our survey" : " · Species facts");
        }

        private string ScenarioDetailWords(ScenarioDetailKind kind, string threat, string speciesId)
        {
            if (kind == ScenarioDetailKind.Cause)
            {
                switch (threat)
                {
                    case "toxic_algal_bloom": return "Extreme trial. Toxic algae can harm marine life, and some blooms also deplete oxygen as they decay. For comparison, this trial assumes the five model species disappear locally; this is not the outcome of every bloom.\n\nThe phytoplankton row represents Prochlorococcus marinus, not all phytoplankton or the bloom-forming algae. Coral response: unknown.\n\nYour survey has more herring and stable phytoplankton. Compare this prediction with your notebook.";
                    case "longline": return "Long-line fishing uses baited hooks. This trial starts with fewer sharks and follows the food chain. Surface phytoplankton remains stable in the model.\n\nUse Play to see the prediction; compare it with your recorded survey.";
                    case "bottom_trawling": return "This trial models targeted tuna catch, hammerhead bycatch and seabed disturbance. Surface phytoplankton is held stable. The krill response is not specified in this model. Use Play to reveal the responses.\n\nNormal food supply:\n" + ScenarioFoodLinks(threat);
                    default: return "This provisional pollution trial specifies some species responses and leaves others unknown. The team is reviewing this scenario.\n\nUnknown means the model does not provide a prediction; it does not mean stable or absent.";
                }
            }
            var species = caseDefinition.FindSpecies(speciesId);
            if (species == null) return string.Empty;
            if (kind == ScenarioDetailKind.Species)
                return species.DisplayName + "\n" + species.ScientificName + "\n\n" + species.Description
                    + (speciesId == "tree_bubblegum_coral" ? "\n\nNormal food supply in this teaching model:\n" + ScenarioFoodLinks("bottom_trawling") : string.Empty);
            var finding = FindObserveObservationForSpecies(speciesId);
            string survey = finding?.DisplayName ?? ResolveSurveySummary(species, SurveyEra.Current)?.Result
                ?? "No observation of this species was collected in this case";
            if (kind == ScenarioDetailKind.Survey)
            {
                var past = ResolveSurveySummary(species, SurveyEra.Historical);
                return "20 years ago: " + (past?.Detection == SpeciesDetectionState.Detected ? "detected" : "not detected")
                    + ".\nToday: " + survey + ".\n\nPictures show relative detection patterns, not population counts. A non-detection does not prove absence.";
            }
            if (!ScenarioPredictionRevealed(threat, speciesId))
                return "Baseline: all trials start from the same reference groups. Play this scenario and watch this row to reveal its prediction.\n\nThe symbols illustrate relative change, not measured population counts.";
            var simulation = state.FindSimulation(threat);
            var prediction = simulation?.FindPrediction(speciesId);
            if (prediction == null || prediction.PredictedState == PredictionState.Unknown)
                return "? Unknown\n\nThis model does not specify a response for this species. Unknown does not mean stable, absent or disproven.\n\nYour survey: " + survey + ".";
            string words = (PredictionStateSymbol(prediction.PredictedState) + " " + PredictionLabel(prediction.PredictedState)).Trim() + "\n\n";
            int i = new List<string>(caseDefinition.FoodWebChainSpeciesIds).IndexOf(speciesId);
            var explicitPrediction = caseDefinition.FindThreat(threat)?.FindPrediction(speciesId);
            if (explicitPrediction != null) words += explicitPrediction.Rationale;
            else if (threat == "plastic")
                words += "This is an explicit assumption of the provisional pollution trial. The team has not supplied a complete food-chain response for it.";
            else if (i == 0)
                words += "This trial starts by assuming fewer sharks. The model then follows the predator–prey links to predict the other species' responses.";
            else if (i > 0)
            {
                string predatorId = caseDefinition.FoodWebChainSpeciesIds[i - 1];
                var predator = caseDefinition.FindSpecies(predatorId);
                var previous = simulation.FindPrediction(predatorId);
                bool less = previous?.PredictedState == PredictionState.Decrease;
                string predatorName = predator.SpeciesId == "shark" ? "sharks" : predator.GameplayName.ToLowerInvariant();
                words += $"In this model, {(less ? "fewer" : "more")} {predatorName} means {(less ? "less" : "more")} feeding pressure on {species.GameplayName.ToLowerInvariant()}, so this group {(less ? "increases" : "decreases")}.\n\n"
                    + predator.GameplayName + " " + PredictionStateSymbol(previous.PredictedState) + " → "
                    + species.GameplayName + " " + PredictionStateSymbol(prediction.PredictedState);
            }
            return words + "\n\nYour survey: " + survey + ".";
        }

        private string ScenarioFoodLinks(string threatId)
        {
            var model = caseDefinition.FindThreat(threatId);
            var lines = new List<string>();
            if (model == null) return string.Empty;
            foreach (var link in model.FoodSupplyLinks)
            {
                string source = caseDefinition.FindSpecies(link.SourceSpeciesId)?.GameplayName;
                string consumer = caseDefinition.FindSpecies(link.ConsumerSpeciesId)?.GameplayName;
                switch (link.Kind)
                {
                    case ScenarioFoodLinkKind.SinkingOrganicMatter:
                        lines.Add(source + " at the surface → dead organic matter sinks → seabed " + consumer.ToLowerInvariant()); break;
                    case ScenarioFoodLinkKind.CoralSpawn:
                        lines.Add(source + " spawn → food for " + consumer.ToLowerInvariant()); break;
                    default: lines.Add(consumer + " eats " + source.ToLowerInvariant()); break;
                }
            }
            return string.Join("\n\n", lines);
        }

        private void ShowScenarioDetail(RectTransform owner, ScenarioDetailKind kind, string threat, string species, bool pinned)
        {
            if (!ScenarioWorkspaceActive || notebookDrawerOpen || ScenarioBriefingActive || restartConfirmationPending
                || owner == null || !owner.gameObject.activeInHierarchy || (!pinned && scenarioDetailPinned)) return;
            if (scenarioDetailPinned) return;
            CloseScenarioDetail(false);
            scenarioDetailOwner = owner; scenarioDetailKind = kind;
            scenarioDetailThreat = threat; scenarioDetailSpecies = species; scenarioDetailPinned = pinned;
            if (pinned && scenarioStage == ScenarioStage.PlayingCause) scenarioDetailPausedAt = Time.unscaledTimeAsDouble;
            scenarioDetailWasRevealed = kind == ScenarioDetailKind.Prediction && ScenarioPredictionRevealed(threat, species);
            scenarioDetailShown = ScenarioDetailWords(kind, threat, species);
            var canvas = GetComponent<Canvas>().rootCanvas.GetComponent<RectTransform>();
            scenarioDetailRoot = CreatePanel("Scenario Detail Overlay", canvas, Color.clear, 0f);
            Stretch(scenarioDetailRoot, 0f, 0f, 0f, 0f);
            var layer = scenarioDetailRoot.gameObject.AddComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 80;
            if (pinned)
            {
                scenarioDetailRoot.gameObject.AddComponent<GraphicRaycaster>();
                scenarioDetailRoot.GetComponent<Image>().raycastTarget = true;
                var scrim = scenarioDetailRoot.gameObject.AddComponent<Button>(); scrim.transition = Selectable.Transition.None;
                scrim.navigation = new Navigation { mode = Navigation.Mode.None };
                scrim.onClick.AddListener(() => CloseScenarioDetail(true));
            }
            RectTransform panel = CreatePanel("Scenario Detail Card", scenarioDetailRoot, InvestigationTheme.Deep, InvestigationTheme.CardRadius);
            panel.GetComponent<Image>().raycastTarget = pinned;
            if (pinned)
            {
                var blocker = panel.gameObject.AddComponent<Button>(); blocker.transition = Selectable.Transition.None;
                blocker.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            float width = Mathf.Min(360f, canvas.rect.width - 24f);
            TextMeshProUGUI title = CreateText("Scenario Detail Title", panel, ScenarioDetailTitle(kind, threat, species), 18,
                FontStyle.Bold, InvestigationTheme.Primary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            Anchor(title.rectTransform, 0f, 1f, 1f, 1f, 16f, -64f, pinned ? -86f : -16f, -12f);
            RectTransform viewport = CreatePanel("Scenario Detail Viewport", panel, Color.clear, 0f);
            Stretch(viewport, 16f, 36f, -16f, -70f); viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = pinned;
            scroll.viewport = viewport; scroll.movementType = ScrollRect.MovementType.Clamped;
            viewport.GetComponent<Image>().raycastTarget = pinned;
            TextMeshProUGUI body = CreateText("Scenario Detail Body", viewport, scenarioDetailShown, 14,
                FontStyle.Normal, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            body.rectTransform.anchorMin = new Vector2(0f, 1f); body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.pivot = new Vector2(.5f, 1f); body.rectTransform.offsetMin = body.rectTransform.offsetMax = Vector2.zero;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = body.rectTransform;
            float bodyHeight = body.GetPreferredValues(body.text, width - 32f, Mathf.Infinity).y;
            float height = Mathf.Min(Mathf.Max(180f, bodyHeight + 112f), canvas.rect.height - 24f);
            Rect source = ScenarioGuideBounds(owner, canvas), screen = canvas.rect;
            float x = source.center.x < screen.center.x ? source.xMax + 12f : source.xMin - width - 12f;
            x = Mathf.Clamp(x, screen.xMin + 12f, screen.xMax - width - 12f);
            float y = Mathf.Clamp(source.center.y - height * .5f, screen.yMin + 12f, screen.yMax - height - 12f);
            PositionScenarioGuide(panel, new Rect(x, y, width, height));
            EnsureOutline(panel.gameObject, InvestigationTheme.Primary, new Vector2(1f, -1f));
            TextMeshProUGUI foot = CreateText("Scenario Detail Status", panel, pinned
                ? (scenarioDetailPausedAt >= 0d ? "Paused · close to continue" : (bodyHeight > height - 106f ? "Scroll for more · Close to return" : "Click outside or Close to return")) : "Click or tap to keep open", 11,
                FontStyle.Bold, InvestigationTheme.Primary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(foot.rectTransform, 0f, 0f, 1f, 0f, 16f, 6f, -16f, 30f);
            if (pinned)
            {
                Button close = CreateButton("Close Scenario Details", panel, "Close", ButtonVisualStyle.Secondary, () => CloseScenarioDetail(true), out TextMeshProUGUI closeLabel);
                closeLabel.fontSize = 13;
                Anchor(close.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -78f, -54f, -10f, -10f);
                close.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = close, selectOnRight = close, selectOnUp = close, selectOnDown = close };
                var trigger = close.gameObject.AddComponent<EventTrigger>();
                var cancel = new EventTrigger.Entry { eventID = EventTriggerType.Cancel };
                cancel.callback.AddListener(_ => CloseScenarioDetail(true)); trigger.triggers.Add(cancel);
                EventSystem.current?.SetSelectedGameObject(close.gameObject);
            }
            HighlightScenarioDetail(species, threat);
        }

        private void HighlightScenarioDetail(string species, string threat)
        {
            var targets = new List<RectTransform>();
            if (!string.IsNullOrEmpty(species))
            {
                foreach (var cause in caseDefinition.Threats)
                    targets.Add(FindNamedRect(FindNamedRect(contentRoot, "Scenario Result " + cause.ThreatId), "Scenario Result Species " + species));
                targets.Add(FindNamedRect(contentRoot, "Scenario Observed " + species));
            }
            else targets.Add(FindNamedRect(contentRoot, "Scenario Card Header " + threat));
            foreach (var target in targets)
            {
                if (target == null) continue;
                var border = CreateGraphic<InvestigationBorderGraphic>("Scenario Detail Highlight", target);
                border.Configure(6f, 1.5f); border.color = InvestigationTheme.Primary; border.raycastTarget = false;
                Stretch(border.rectTransform, 0f, 0f, 0f, 0f); scenarioDetailHighlights.Add(border.rectTransform);
            }
        }

        private void CloseScenarioDetail(bool refresh)
        {
            bool paused = scenarioDetailPausedAt >= 0d;
            if (paused) scenarioStarted += Time.unscaledTimeAsDouble - scenarioDetailPausedAt;
            scenarioDetailPausedAt = -1d; scenarioDetailPinned = false; scenarioDetailOwner = null;
            RemoveEdnaObject(scenarioDetailRoot); scenarioDetailRoot = null;
            foreach (var item in scenarioDetailHighlights) RemoveEdnaObject(item);
            scenarioDetailHighlights.Clear();
            if (refresh && paused) RefreshPresentationOnly();
            if (refresh)
            {
                var focus = FindInteractableButton("Replay Scenario " + scenarioDetailThreat)
                    ?? FindInteractableButton("Run Scenario " + scenarioDetailThreat)
                    ?? FindInteractableButton("Toggle Notebook Drawer");
                if (focus != null) EventSystem.current?.SetSelectedGameObject(focus.gameObject);
            }
        }

        private void Update()
        {
            if (scenarioDetailRoot == null || scenarioDetailPinned) return;
            if (scenarioDetailOwner == null || !scenarioDetailOwner.gameObject.activeInHierarchy) { CloseScenarioDetail(false); return; }
            if (scenarioDetailKind == ScenarioDetailKind.Prediction
                && scenarioDetailWasRevealed != ScenarioPredictionRevealed(scenarioDetailThreat, scenarioDetailSpecies))
                ShowScenarioDetail(scenarioDetailOwner, scenarioDetailKind, scenarioDetailThreat, scenarioDetailSpecies, false);
        }
    }
}
