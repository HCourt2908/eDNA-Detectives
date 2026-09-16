using TMPro;
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
            public TextMeshProUGUI Result;
            public Image[] Units;
            public Image[] FoodParticles;
            public Image Halo;
            public InvestigationBorderGraphic Border;
            public PredictionState Prediction;
        }
        private static int ScenarioPopulationCount(PredictionState state) => state == PredictionState.Absent ? 0 : state == PredictionState.Increase ? 5 : state == PredictionState.Decrease ? 1 : 3;
        private static readonly Vector2[] ScenarioCardUnitSlots = {
            new Vector2(.5f, .5f), new Vector2(.3f, .5f), new Vector2(.7f, .5f), new Vector2(.1f, .5f), new Vector2(.9f, .5f)
        };

        private void SampleScenarioPopulations(List<ScenarioActor> actors, float progress)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                ScenarioActor actor = actors[i];
                // Hold the baseline, then reveal each species response. The last part
                // of the playback holds the completed pattern before advancing.
                float begin = .10f + i * (.54f / Mathf.Max(1, actors.Count - 1));
                float t = Mathf.InverseLerp(begin, begin + .24f, progress);
                if (actor.FoodParticles != null)
                    for (int particle = 0; particle < actor.FoodParticles.Length; particle++)
                    {
                        float fall = Mathf.Repeat(progress * 3f + particle / 3f, 1f);
                        var mote = actor.FoodParticles[particle];
                        float x = .2f + particle * .3f;
                        mote.rectTransform.anchorMin = mote.rectTransform.anchorMax = new Vector2(x, 1f - fall);
                        mote.color = new Color(.75f, .95f, .85f, progress > 0f && progress < 1f ? Mathf.Sin(fall * Mathf.PI) * .55f : 0f);
                    }
                Color tint = actor.Prediction == PredictionState.Decrease || actor.Prediction == PredictionState.Absent ? InvestigationTheme.Accent : InvestigationTheme.Primary;
                float pulse = t > 0f && t < 1f ? Mathf.Sin(t * Mathf.PI) * (.65f + .35f * Mathf.Sin(t * Mathf.PI * 4f)) : 0f;
                actor.Halo.color = new Color(tint.r, tint.g, tint.b, pulse * .20f);
                actor.Border.color = new Color(tint.r, tint.g, tint.b, pulse * .95f);
                actor.Result.text = t <= 0f ? "— Stable" : (PredictionStateSymbol(actor.Prediction) + " " + PredictionLabel(actor.Prediction)).Trim();
                actor.Result.color = t <= 0f ? InvestigationTheme.TextPrimary : PredictionStateColor(actor.Prediction);
                for (int unit = 0; unit < actor.Units.Length; unit++)
                {
                    Image image = actor.Units[unit];
                    Vector2 slot = ScenarioCardUnitSlots[unit];
                    Vector2 offset = Vector2.zero;
                    float alpha = unit < 3 ? 1f : 0f;
                    float stagger = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp((unit % 2) * .18f, .82f + (unit % 2) * .18f, t));
                    if (actor.Prediction == PredictionState.Increase && unit >= 3)
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
                    else if (actor.Prediction == PredictionState.Absent && unit < 3)
                    {
                        alpha = 1f - stagger;
                        offset.y = -actor.Art.rect.height * .08f * stagger;
                    }
                    else if (actor.Prediction == PredictionState.DepthShift && unit < 3)
                        offset.y = -actor.Art.rect.height * .18f * stagger;
                    // A small swim during playback catches the eye without using
                    // size or opacity as the meaning of 'more' and 'less'.
                    if (progress > 0f && progress < 1f)
                        offset.y += Mathf.Sin(progress * Mathf.PI * 6f + unit) * 2.5f * Mathf.Sin(progress * Mathf.PI);
                    // Keep symbols inside the row directly. Nested per-row
                    // clip masks can retain stale clips after UI snapshots.
                    float width = Mathf.Max(0f, actor.Art.rect.width), height = Mathf.Max(0f, actor.Art.rect.height);
                    offset.x = Mathf.Clamp(offset.x, (.095f - slot.x) * width, (.905f - slot.x) * width);
                    offset.y = Mathf.Clamp(offset.y, -.08f * height, .08f * height);
                    float arrival = actor.Prediction == PredictionState.Increase && unit >= 3 ? .65f + .35f * stagger + .12f * Mathf.Sin(stagger * Mathf.PI) : 1f;
                    image.rectTransform.localScale = Vector3.one * arrival;
                    image.rectTransform.anchoredPosition = offset;
                    image.color = new Color(1f, 1f, 1f, alpha * (actor.Prediction == PredictionState.Unknown ? Mathf.Lerp(1f, .25f, t) : 1f));
                }
            }
        }
    }
}
