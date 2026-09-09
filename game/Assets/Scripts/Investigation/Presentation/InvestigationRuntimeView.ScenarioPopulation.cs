using System.Collections.Generic;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private sealed class ScenarioActor
        {
            public RectTransform Art;
            public Text Result;
            public Image[] Units;
            public Image Halo;
            public InvestigationBorderGraphic Border;
            public PredictionState Prediction;
            public bool IsCardRow;
        }
        private static int ScenarioPopulationCount(PredictionState state) => state == PredictionState.Increase ? 5 : state == PredictionState.Decrease ? 1 : 3;
        private static readonly Vector2[] ScenarioUnitSlots = {
            new Vector2(.50f, .64f), new Vector2(.18f, .64f), new Vector2(.82f, .64f),
            new Vector2(.34f, .33f), new Vector2(.66f, .33f)
        };

        private static readonly Vector2[] ScenarioCardUnitSlots = {
            new Vector2(.5f, .5f), new Vector2(.3f, .5f), new Vector2(.7f, .5f), new Vector2(.1f, .5f), new Vector2(.9f, .5f)
        };

        private ScenarioActor CreateScenarioActor(Transform parent, string id, PredictionState prediction, float x0, float y0, float x1, float y1)
        {
            var species = caseDefinition.FindSpecies(id);
            RectTransform node = CreatePanel("Scenario Actor " + id, parent, Color.clear, 0f);
            Anchor(node, x0, y0, x1, y1, 0f, 0f, 0f, 0f);
            RectTransform halo = CreatePanel("Scenario Population Glow", node, Color.clear, InvestigationTheme.SmallRadius);
            Stretch(halo, 2f, 1f, -2f, -1f);
            Text name = CreateText("Scenario Actor Name", node, id == "mussel" ? "Mussel · control" : id == "sea_star" ? "Sea star · control" : species.GameplayName, 13,
                FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperCenter, InvestigationTheme.BodyFont);
            Anchor(name.rectTransform, 0f, .74f, 1f, 1f, 0f, 0f, 0f, 0f);
            RectTransform art = CreatePanel("Scenario Actor Artwork", node, Color.clear, 0f);
            Anchor(art, .02f, .23f, .98f, .80f, 0f, 0f, 0f, 0f);
            art.gameObject.AddComponent<RectMask2D>();
            var units = new Image[5];
            for (int i = 0; i < units.Length; i++)
            {
                units[i] = CreateStatusIcon("Scenario Specimen " + i, art, species.Icon, i < 3 ? Color.white : Color.clear);
                Vector2 slot = ScenarioUnitSlots[i];
                Anchor(units[i].rectTransform, slot.x - .14f, slot.y - .32f, slot.x + .14f, slot.y + .32f, 0f, 0f, 0f, 0f);
            }
            Text result = CreateText("Scenario Actor Prediction", node, "— Stable", 14, FontStyle.Bold,
                InvestigationTheme.TextPrimary, TextAnchor.LowerCenter, InvestigationTheme.BodyFont);
            Anchor(result.rectTransform, 0f, 0f, 1f, .23f, 0f, 0f, 0f, 0f);
            var border = CreateGraphic<InvestigationBorderGraphic>("Scenario Population Pulse", node);
            border.Configure(InvestigationTheme.SmallRadius, 2f); border.color = Color.clear; border.raycastTarget = false;
            Stretch(border.rectTransform, 1f, 1f, -1f, -1f);
            return new ScenarioActor { Art = art, Units = units, Halo = halo.GetComponent<Image>(), Border = border, Result = result, Prediction = prediction };
        }

        private void SampleScenarioScene(List<ScenarioActor> actors, List<CanvasGroup> links, bool building, float progress, RectTransform notebook = null)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                ScenarioActor actor = actors[i];
                // Hold the baseline, then show a readable cascade. The last part
                // of the playback holds the completed pattern before advancing.
                float begin = i < 3 ? .12f + i * .17f : .57f + (i - 3) * .04f;
                float t = building ? Mathf.InverseLerp(i * .10f, .48f + i * .10f, progress)
                    : Mathf.InverseLerp(begin, begin + .24f, progress);
                Color tint = actor.Prediction == PredictionState.Decrease ? InvestigationTheme.Accent : InvestigationTheme.Primary;
                float pulse = !building && t > 0f && t < 1f ? Mathf.Sin(t * Mathf.PI) * (.65f + .35f * Mathf.Sin(t * Mathf.PI * 4f)) : 0f;
                actor.Halo.color = new Color(tint.r, tint.g, tint.b, pulse * .20f);
                actor.Border.color = new Color(tint.r, tint.g, tint.b, pulse * .95f);
                actor.Result.text = building || t <= 0f ? "— Stable" : PredictionStateSymbol(actor.Prediction) + " " + PredictionLabel(actor.Prediction);
                actor.Result.color = building || t <= 0f ? InvestigationTheme.TextPrimary : PredictionStateColor(actor.Prediction);
                for (int unit = 0; unit < actor.Units.Length; unit++)
                {
                    Image image = actor.Units[unit];
                    Vector2 slot = actor.IsCardRow ? ScenarioCardUnitSlots[unit] : ScenarioUnitSlots[unit];
                    Vector2 offset = Vector2.zero;
                    float alpha = unit < 3 ? 1f : 0f;
                    float stagger = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp((unit % 2) * .18f, .82f + (unit % 2) * .18f, t));
                    if (building)
                    {
                        Vector2 home = new Vector2((slot.x - .5f) * actor.Art.rect.width, (slot.y - .5f) * actor.Art.rect.height);
                        Vector2 source = notebook == null ? home + Vector2.down * 55f
                            : (Vector2)actor.Art.InverseTransformPoint(notebook.TransformPoint(notebook.rect.center));
                        offset = (source - home) * (1f - stagger) + Vector2.up * Mathf.Sin(stagger * Mathf.PI) * 14f;
                        alpha *= stagger;
                    }
                    else if (actor.Prediction == PredictionState.Increase && unit >= 3)
                    {
                        offset = new Vector2((unit == 3 ? -1f : 1f) * (1f - stagger) * actor.Art.rect.width * .52f,
                            Mathf.Sin(stagger * Mathf.PI) * actor.Art.rect.height * .35f);
                        alpha = stagger;
                    }
                    else if (actor.Prediction == PredictionState.Decrease && unit > 0 && unit < 3)
                    {
                        offset = new Vector2((unit == 1 ? -1f : 1f) * stagger * actor.Art.rect.width * .45f,
                            Mathf.Sin(stagger * Mathf.PI) * actor.Art.rect.height * .24f);
                        alpha = 1f - stagger;
                    }
                    else if (actor.Prediction == PredictionState.DepthShift && unit < 3)
                        offset.y = -actor.Art.rect.height * .18f * stagger;
                    // A small swim during playback catches the eye without using
                    // size or opacity as the meaning of 'more' and 'less'.
                    if (!building && progress > 0f && progress < 1f)
                        offset.y += Mathf.Sin(progress * Mathf.PI * 6f + unit) * 2.5f * Mathf.Sin(progress * Mathf.PI);
                    if (actor.IsCardRow)
                    {
                        // Keep symbols inside the row directly. Nested per-row
                        // clip masks can retain stale clips after UI snapshots.
                        float width = Mathf.Max(0f, actor.Art.rect.width), height = Mathf.Max(0f, actor.Art.rect.height);
                        offset.x = Mathf.Clamp(offset.x, (.095f - slot.x) * width, (.905f - slot.x) * width);
                        offset.y = Mathf.Clamp(offset.y, -.08f * height, .08f * height);
                        float arrival = actor.Prediction == PredictionState.Increase && unit >= 3 ? .65f + .35f * stagger + .12f * Mathf.Sin(stagger * Mathf.PI) : 1f;
                        image.rectTransform.localScale = Vector3.one * arrival;
                    }
                    image.rectTransform.anchoredPosition = offset;
                    image.color = new Color(1f, 1f, 1f, alpha);
                }
            }
            for (int i = 0; i < links.Count; i++) links[i].alpha = building ? Mathf.InverseLerp(.25f + .2f * i, .65f + .2f * i, progress) : 1f;
        }
    }
}
