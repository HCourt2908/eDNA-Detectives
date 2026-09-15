using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private bool IsBloomScenario(string id) => caseDefinition.FindThreat(id)?.GlyphKind == ThreatGlyphKind.AlgalBloom;
        private float ScenarioPopulationProgress(string id, float progress) => IsBloomScenario(id)
            ? Mathf.InverseLerp(.20f, 1f, progress) : progress;

        private RectTransform[] CreateBloomParticles(RectTransform card)
        {
            var particles = new RectTransform[18];
            for (int i = 0; i < particles.Length; i++)
            {
                var particle = CreatePanel("Bloom Particle " + i, card, Color.clear, 6f);
                particle.GetComponent<Image>().raycastTarget = false;
                particle.anchorMin = particle.anchorMax = new Vector2(.12f + (i % 6) * .15f, .3f + (i / 6) * .17f);
                particle.sizeDelta = Vector2.one * (7f + i % 3 * 3f);
                particles[i] = particle;
            }
            return particles;
        }

        private static void SampleBloomParticles(RectTransform[] particles, float progress)
        {
            if (particles == null) return;
            float bloom = Mathf.InverseLerp(0f, .22f, progress) * (1f - Mathf.InverseLerp(.60f, 1f, progress));
            for (int i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                particle.localScale = Vector3.one * Mathf.Lerp(.25f, 1.6f, bloom);
                particle.anchoredPosition = new Vector2(Mathf.Sin(progress * 8f + i) * 12f, progress * 16f);
                particle.GetComponent<Image>().color = new Color(.76f, .78f, .24f, bloom * .32f);
            }
        }
    }
}
