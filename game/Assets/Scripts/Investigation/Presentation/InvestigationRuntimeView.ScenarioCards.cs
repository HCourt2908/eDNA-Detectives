using System.Collections.Generic;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private void RenderScenarioAlternatives(Transform parent)
        {
            Text heading = CreateText("Scenario Comparison Heading", parent,
                EveryScenarioViewed ? "Which predictions fit our survey?" : "Play each prediction · Compare it with our survey", 18,
                FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(heading.rectTransform, 26f, 0f);
            RectTransform cards = new GameObject("Scenario Results", typeof(RectTransform), typeof(InvestigationResponsiveGridLayout)).GetComponent<RectTransform>();
            cards.SetParent(parent, false); cards.GetComponent<InvestigationResponsiveGridLayout>().Configure(3, 3, 1, 300f, 10f);
            foreach (var cause in caseDefinition.Threats)
            {
                string id = cause.ThreatId;
                bool playing = scenarioStage == ScenarioStage.PlayingCause && scenarioActiveId == id;
                bool complete = viewedScenarios.Contains(id);
                var simulation = playing || complete ? state.FindSimulation(id) : null;
                RectTransform card = CreatePanel("Scenario Result " + id, cards, InvestigationTheme.SurfaceQuiet, InvestigationTheme.CardRadius);
                RectTransform header = CreatePanel("Scenario Card Header " + id, card, playing ? InvestigationTheme.SurfaceRaised : InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
                Anchor(header, 0f, 1f, 1f, 1f, 8f, -49f, -8f, -5f);
                RectTransform icon = CreateThreatArtwork("Scenario Cause Icon " + id, header, cause);
                Anchor(icon, 0f, 0f, 0f, 1f, 5f, 4f, 40f, -4f);
                Text title = CreateText("Scenario Card Title " + id, header, cause.DisplayName, 14,
                    FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
                Anchor(title.rectTransform, 0f, 0f, 1f, 1f, 47f, 2f, -94f, -2f);
                Button play = CreateButton((complete ? "Replay Scenario " : "Run Scenario ") + id, header,
                    playing ? "Playing…" : complete ? "Replay" : "Play", ButtonVisualStyle.Primary, () => StartScenario(id), out Text playLabel);
                play.GetComponent<LayoutElement>().ignoreLayout = true; playLabel.fontSize = 13;
                Anchor(play.GetComponent<RectTransform>(), 1f, .5f, 1f, .5f, -88f, -18f, -4f, 18f);
                play.interactable = !playing && !ScenarioCaseClosed;
                RectTransform track = CreatePanel("Scenario Card Track " + id, card, InvestigationTheme.SurfaceRaised, 2f);
                Anchor(track, 0f, 1f, 1f, 1f, 12f, -57f, -12f, -53f);
                RectTransform fill = CreatePanel("Scenario Card Progress " + id, track, InvestigationTheme.Primary, 2f);
                Anchor(fill, 0f, 0f, complete && !playing ? 1f : 0f, 1f, 0f, 0f, 0f, 0f);
                var actors = new List<ScenarioActor>(); var links = new List<CanvasGroup>();
                var ids = ScenarioSpecies();
                for (int i = 0; i < ids.Count; i++)
                {
                    var species = caseDefinition.FindSpecies(ids[i]);
                    PredictionState prediction = simulation?.FindPrediction(ids[i])?.PredictedState ?? PredictionState.Stable;
                    RectTransform row = CreatePanel("Scenario Result Species " + ids[i], card, Color.clear, 0f);
                    Anchor(row, 0f, 1f, 1f, 1f, 12f, -100f - i * 36f, -12f, -64f - i * 36f);
                    RectTransform halo = CreatePanel("Scenario Population Glow", row, Color.clear, 6f); Stretch(halo, 0f, 0f, 0f, 0f);
                    Text name = CreateText("Result Species Name", row, species.GameplayName, 13,
                        FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
                    Anchor(name.rectTransform, 0f, 0f, .28f, 1f, 2f, 0f, -2f, 0f);
                    RectTransform population = CreatePanel("Result Population " + ids[i], row, Color.clear, 0f);
                    Anchor(population, .29f, 0f, .68f, 1f, 0f, 1f, 0f, -1f);
                    var units = new Image[5];
                    for (int unit = 0; unit < units.Length; unit++)
                    {
                        units[unit] = CreateStatusIcon("Result Specimen " + unit, population, species.Icon, unit < 3 ? Color.white : Color.clear);
                        float x = ScenarioCardUnitSlots[unit].x;
                        Anchor(units[unit].rectTransform, x - .095f, .08f, x + .095f, .92f, 0f, 0f, 0f, 0f);
                    }
                    Text value = CreateText("Result Prediction", row, "Baseline", 13, FontStyle.Bold,
                        InvestigationTheme.TextSecondary, TextAnchor.MiddleRight, InvestigationTheme.BodyFont);
                    Anchor(value.rectTransform, .69f, 0f, 1f, 1f, 0f, 0f, -2f, 0f);
                    var border = CreateGraphic<InvestigationBorderGraphic>("Scenario Population Pulse", row);
                    border.Configure(6f, 1.5f); border.color = Color.clear; border.raycastTarget = false; Stretch(border.rectTransform, 0f, 0f, 0f, 0f);
                    actors.Add(new ScenarioActor { Art = population, Result = value, Units = units, Halo = halo.GetComponent<Image>(), Border = border, Prediction = prediction, IsCardRow = true });
                }
                SampleScenarioScene(actors, links, false, complete && !playing ? 1f : 0f);
                if (!playing && !complete) foreach (var actor in actors) actor.Result.text = "Baseline";
                if (playing)
                {
                    int run = scenarioRun; var session = state; double began = scenarioStarted;
                    bindScenarioPlayback += () => card.gameObject.AddComponent<InvestigationScenarioPlayback>().Configure(began, ScenarioSeconds,
                        progress => { SampleScenarioScene(actors, links, false, progress); fill.anchorMax = new Vector2(progress, 1f); },
                        () => { if (run == scenarioRun && ReferenceEquals(session, state) && scenarioActiveId == id) FinishScenarioAnimation(); }, () => ScenarioPlaybackTime);
                }
                else
                {
                    // Initialize settled rows after the responsive layout has
                    // real dimensions, just like the animated rows.
                    bindScenarioPlayback += () =>
                    {
                        SampleScenarioScene(actors, links, false, complete ? 1f : 0f);
                        if (!complete) foreach (var actor in actors) actor.Result.text = "Baseline";
                        foreach (var actor in actors) foreach (var unit in actor.Units) unit.SetAllDirty();
                    };
                }
                Button choose = CreateButton("Choose Scenario " + id, card, "Review this explanation", ButtonVisualStyle.Primary,
                    () => ChooseScenarioExplanation(id), out Text label);
                choose.GetComponent<LayoutElement>().ignoreLayout = true; label.fontSize = 14;
                Anchor(choose.GetComponent<RectTransform>(), 0f, 0f, 1f, 0f, 8f, 8f, -8f, 48f);
                choose.interactable = EveryScenarioViewed && scenarioStage == ScenarioStage.Comparing && !ScenarioCaseClosed;
            }
        }
    }
}
