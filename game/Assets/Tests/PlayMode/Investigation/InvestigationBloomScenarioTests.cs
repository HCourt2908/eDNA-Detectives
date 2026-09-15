using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationBloomScenarioTests
    {
        private const string Bloom = "toxic_algal_bloom";
        private static IEnumerator Open()
        {
            yield return LoadCurrent(); EnterCurrentModels(); yield return null; yield return null; SkipCurrentGuide();
        }
        private static int Count(string species) => GameObject.Find("Scenario Result " + Bloom).transform
            .Find("Scenario Result Species " + species).GetComponentsInChildren<Image>()
            .Count(i => i.name.StartsWith("Result Specimen ") && i.color.a > .99f);

        [UnityTest]
        public IEnumerator FourthCardCanBeSkippedWithoutBlockingCompletion()
        {
            yield return Open();
            Assert.That(GameObject.Find("Scenario Results").transform.childCount, Is.EqualTo(4));
            Assert.That(GameObject.Find("Optional Scenario Label"), Is.Null);
            Assert.That(CurrentButton("Choose Scenario " + Bloom).interactable, Is.False);
            OpenCurrentSummary(); CurrentPress("Complete Scenario Investigation");
            Assert.That(InvestigationSessionBridge.IsComplete, Is.True);
            Assert.That(CurrentState.HasTriedThreat(Bloom), Is.False);
        }

        [UnityTest]
        public IEnumerator BloomPlaysBeforeRequiredTrialsAndDoesNotInventEvidence()
        {
            yield return Open();
            CurrentPress("Run Scenario " + Bloom);
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(Count("shark"), Is.EqualTo(3));
            Assert.That(GameObject.Find("Scenario Result " + Bloom).GetComponentsInChildren<Image>()
                .Any(i => i.name.StartsWith("Bloom Particle ") && i.color.a > .05f), Is.True);
            CurrentPress("Finish Scenario Animation"); yield return null; yield return null; SkipCurrentGuide();
            foreach (string species in new[] { "shark", "tuna", "atlantic_herring", "krill", "phytoplankton" })
                Assert.That(Count(species), Is.Zero, species);
            CurrentPress("Detail Cause " + Bloom);
            Assert.That(GameObject.Find("Scenario Detail Body").GetComponent<TextMeshProUGUI>().text,
                Does.Contain("not the outcome of every bloom").And.Contain("Prochlorococcus").And.Contain("Coral response: unknown"));
            CurrentPress("Close Scenario Details");
            CurrentPress("Choose Scenario " + Bloom); yield return null;
            Assert.That(CurrentState.ProvisionalThreatId, Is.Empty);
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(6));
            Assert.That(CurrentState.ComparisonRecords.Count(r => r.ThreatId == Bloom), Is.EqualTo(2));
            Assert.That(CurrentState.CompletedObjectiveCount, Is.Zero);
            Assert.That(string.Join(" ", CurrentView.GetComponentsInChildren<TextMeshProUGUI>().Select(t => t.text)),
                Does.Contain("herring increased and phytoplankton stayed stable"));
            SkipCurrentGuide();
            CurrentPress("Replay Scenario " + Bloom);
            Assert.That(Count("phytoplankton"), Is.EqualTo(3));
            CurrentPress("Finish Scenario Animation"); yield return null; yield return null; SkipCurrentGuide();
            OpenCurrentSummary(); CurrentPress("Complete Scenario Investigation");
            Assert.That(InvestigationSessionBridge.LastResult.selectedHypothesisId, Is.EqualTo("bottom_trawling"));
        }

        [UnityTest]
        public IEnumerator CardsReflowFromFourColumnsToTwoWithoutPageScrolling()
        {
            yield return Open();
            var canvas = CurrentView.GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            float original = canvas.scaleFactor; bool enabled = scaler.enabled;
            var root = canvas.GetComponent<RectTransform>();
            RenderMode mode = canvas.renderMode; Vector2 originalSize = root.sizeDelta;
            try
            {
                scaler.enabled = false;
                foreach (int width in new[] { 1280, 900 })
                {
                    // Supply a tall, narrow viewport as well as the wide viewport.
                    // A short landscape window should keep four columns rather
                    // than shrink an unnecessarily tall two-row workbench.
                    canvas.renderMode = RenderMode.WorldSpace;
                    root.sizeDelta = new Vector2(width, width == 1280 ? 720 : 1300);
                    yield return null; yield return null;
                    Canvas.ForceUpdateCanvases();
                    var first = GameObject.Find("Scenario Result plastic").transform;
                    var second = GameObject.Find("Scenario Result longline").transform;
                    var fourth = GameObject.Find("Scenario Result " + Bloom).transform;
                    Assert.That(first.position.y, Is.EqualTo(second.position.y).Within(1f));
                    if (width == 1280) Assert.That(first.position.y, Is.EqualTo(fourth.position.y).Within(1f));
                    else Assert.That(fourth.position.y, Is.LessThan(first.position.y));
                    Assert.That(CurrentView.ContentRoot.GetComponentInParent<ScrollRect>().vertical, Is.False);
                    foreach (var text in GameObject.Find("Scenario Results").GetComponentsInChildren<TextMeshProUGUI>())
                        Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name + ": " + text.text);
                }
            }
            finally { canvas.renderMode = mode; root.sizeDelta = originalSize; canvas.scaleFactor = original; scaler.enabled = enabled; }
        }
    }
}
