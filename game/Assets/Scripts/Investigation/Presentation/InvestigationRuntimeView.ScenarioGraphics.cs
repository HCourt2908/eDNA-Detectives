using System;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private Action bindScenarioPlayback;
        private void RenderScenarioObservedPattern(Transform parent)
        {
            RectTransform paper = CreatePanel("Scenario Survey Target", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            AddLayout(paper, 104f, 0f);
            CreateNotebookBinding(paper);
            EnsureCanvasGroup(paper).alpha = ScenarioRecordsRevealing ? 0f : 1f;
            Text heading = CreateText("Scenario Survey Target Title", paper, "FROM MY NOTEBOOK · 20 YEARS AGO → TODAY", 12,
                FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(heading.rectTransform, 0f, 1f, 1f, 1f, 32f, -24f, -12f, -4f);
            var ids = ScenarioSpecies();
            for (int i = 0; i < ids.Count; i++)
            {
                var species = caseDefinition.FindSpecies(ids[i]);
                var finding = FindObserveObservationForSpecies(ids[i]);
                RectTransform tile = CreatePanel("Scenario Observed " + ids[i], paper, Color.clear, 0f);
                Anchor(tile, i / (float)ids.Count, 0f, (i + 1) / (float)ids.Count, 1f, 8f, 5f, -8f, -25f);
                Text name = CreateText("Observed Species", tile, ids[i] == "mussel" ? "Mussel" : species.GameplayName, 13,
                    FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperCenter, InvestigationTheme.BodyFont);
                Anchor(name.rectTransform, 0f, .75f, 1f, 1f, 0f, 0f, 0f, 0f);
                RectTransform past = CreateStorySymbols("Observed Past Artwork", tile, species, SurveyEra.Historical);
                Anchor(past, .03f, .24f, .44f, .76f, 0f, 0f, 0f, 0f);
                Text arrow = CreateText("Observed Time Arrow", tile, "→", 14, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Anchor(arrow.rectTransform, .43f, .24f, .57f, .76f, 0f, 0f, 0f, 0f);
                bool missing = ResolveSurveySummary(species, SurveyEra.Current)?.Detection == SpeciesDetectionState.NotDetected;
                if (missing)
                {
                    Text absent = CreateText("Observed Today Absence", tile, "No\nsignal", 11, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                    Anchor(absent.rectTransform, .57f, .24f, .97f, .76f, 0f, 0f, 0f, 0f);
                }
                else
                {
                    RectTransform today = CreateStorySymbols("Observed Today Artwork", tile, species, SurveyEra.Current);
                    Anchor(today, .57f, .24f, .97f, .76f, 0f, 0f, 0f, 0f);
                }
                Text result = CreateText("Observed Change", tile, missing ? "Not detected" : finding?.ClaimType == ObservationClaimType.ChangedDepthOrDistribution ? "More sites" : "Stable", 12,
                    FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.LowerCenter, InvestigationTheme.BodyFont);
                Anchor(result.rectTransform, 0f, 0f, 1f, .25f, 0f, 0f, 0f, 0f);
                if (scenarioConflictSpecies == ids[i])
                {
                    var outline = CreateGraphic<InvestigationBorderGraphic>("Scenario Conflicting Record", tile);
                    outline.Configure(6f, 2f); outline.color = InvestigationTheme.Accent;
                    Stretch(outline.rectTransform, -2f, -2f, 2f, 2f);
                }
            }
        }

        private void RenderScenarioScene(Transform parent)
        {
            bool building = scenarioStage == ScenarioStage.BuildingChain;
            SimulationResult simulation = building ? null : state.FindSimulation(scenarioActiveId);
            RectTransform hero = CreatePanel("Scenario Model", parent, InvestigationTheme.Deep, InvestigationTheme.CardRadius);
            AddLayout(hero, 220f, 0f);
            string title = building ? "Our records form a simple food-chain model" : simulation == null ? "Each trial starts from this same stable baseline"
                : "MODEL PREDICTION · " + caseDefinition.FindThreat(scenarioActiveId).DisplayName;
            Text heading = CreateText("Scenario Model Title", hero, title, 15, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(heading.rectTransform, 0f, 1f, .76f, 1f, 12f, -26f, -8f, -4f);
            RectTransform timeline = CreatePanel("Scenario Playback Track", hero, InvestigationTheme.SurfaceRaised, 4f);
            Anchor(timeline, .78f, 1f, 1f, 1f, 0f, -20f, -16f, -10f);
            RectTransform progressFill = CreatePanel("Scenario Playback Progress", timeline, InvestigationTheme.Primary, 4f);
            Stretch(progressFill, 0f, 0f, 0f, 0f);
            RectTransform plot = CreatePanel("Scenario Seamount", hero, Color.clear, 0f);
            Anchor(plot, 0f, 0f, 1f, 1f, 8f, 4f, -8f, -28f);
            RectTransform mountain = CreatePanel("Scenario Terrain", plot, Color.clear, 0f);
            Anchor(mountain, .17f, 0f, .83f, .92f, 0f, 0f, 0f, 0f);
            CreateSeamountVisual(mountain);
            // A trial must not appear different merely because the decorative
            // A/B/C terrain cycle advanced. Leave Observe's shared material alone.
            RawImage terrain = mountain.GetComponentInChildren<RawImage>();
            if (terrain != null) terrain.gameObject.AddComponent<InvestigationScenarioTerrain>().Freeze(terrain);
            var actors = new List<ScenarioActor>();
            var links = new List<CanvasGroup>();
            for (int i = 0; i < caseDefinition.FoodWebChainSpeciesIds.Count; i++)
            {
                string id = caseDefinition.FoodWebChainSpeciesIds[i];
                actors.Add(CreateScenarioActor(plot, id, simulation?.FindPrediction(id)?.PredictedState ?? PredictionState.Stable,
                    .05f + i * .32f, .50f, .31f + i * .32f, 1f));
                if (i < caseDefinition.FoodWebChainSpeciesIds.Count - 1)
                {
                    Text arrow = CreateText("Scenario Feeding Link", plot, "→\neats", 12, FontStyle.Bold, InvestigationTheme.Primary, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                    Anchor(arrow.rectTransform, .30f + i * .32f, .63f, .37f + i * .32f, .93f, 0f, 0f, 0f, 0f);
                    links.Add(EnsureCanvasGroup(arrow.rectTransform));
                }
            }
            for (int i = 0; i < caseDefinition.BenthicIndicatorSpeciesIds.Count; i++)
            {
                string id = caseDefinition.BenthicIndicatorSpeciesIds[i];
                actors.Add(CreateScenarioActor(plot, id, simulation?.FindPrediction(id)?.PredictedState ?? PredictionState.Stable,
                    .20f + i * .34f, 0f, .46f + i * .34f, .47f));
            }
            Canvas.ForceUpdateCanvases();
            if (building || scenarioStage == ScenarioStage.PlayingCause)
            {
                int run = scenarioRun;
                var session = scenarioSession;
                double began = scenarioStarted;
                bindScenarioPlayback = () =>
                {
                    RectTransform notebook = building ? FindNamedRect(ednaConversation, "Toggle Notebook Drawer") : null;
                    hero.gameObject.AddComponent<InvestigationScenarioPlayback>().Configure(began,
                        building ? ChainArrivalSeconds : ScenarioSeconds,
                        progress =>
                        {
                            SampleScenarioScene(actors, links, building, progress, notebook);
                            progressFill.anchorMax = new Vector2(progress, 1f);
                        },
                        () => { if (run == scenarioRun && ReferenceEquals(session, state)) FinishScenarioAnimation(); },
                        () => ScenarioPlaybackTime);
                };
            }
            else SampleScenarioScene(actors, links, false, 1f);
        }

    }
}
