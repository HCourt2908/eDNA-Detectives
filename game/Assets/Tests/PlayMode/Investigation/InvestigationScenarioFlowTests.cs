using TMPro;
using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationScenarioFlowTests
    {
        private static void Press(string name)
        {
            if (GameObject.Find(name) == null && name.StartsWith("Run Scenario ")) name = name.Replace("Run Scenario ", "Replay Scenario ");
            Button b = GameObject.Find(name)?.GetComponent<Button>(); Assert.That(b, Is.Not.Null, name);
            Assert.That(b.interactable, Is.True, name); b.onClick.Invoke();
        }
        private static IEnumerator Start()
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null;
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            InvestigationWorkbenchTestActions.EnterSimulate("Stage Simulate"); yield return null; yield return null;
        }
        private static string Actor(string id) => GameObject.Find("Scenario Result " + Object.FindAnyObjectByType<InvestigationController>().State.ActiveThreatId).transform.Find("Scenario Result Species " + id + "/Result Prediction").GetComponent<TextMeshProUGUI>().text;
        private static void TryAll()
        {
            if (GameObject.Find("Finish Scenario Animation") != null) Press("Finish Scenario Animation");
            foreach (string id in new[] { "bottom_trawling", "plastic", "longline" })
            {
                Press("Run Scenario " + id); Press("Finish Scenario Animation");
            }
        }
        [UnityTest] public IEnumerator Scenario_ShowsThreeCardsThenRunsEveryCauseFromTheSameBaseline()
        {
            yield return Start();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(GameObject.Find("Connect Species shark"), Is.Null);
            Assert.That(GameObject.Find("Run Scenario longline"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenario Model"), Is.Null);
            yield return new WaitForSecondsRealtime(3.1f);
            Assert.That(GameObject.Find("Run Scenario longline"), Is.Not.Null);
            Assert.That(controller.State.TriedThreatIds, Is.Empty);
            Press("Run Scenario longline");
            Assert.That(Actor("shark"), Does.Contain("Stable"));
            Assert.That(Actor("tuna"), Does.Contain("Stable"));
            yield return new WaitForSecondsRealtime(7.5f);
            Assert.That(Actor("shark"), Does.Contain("Decrease"));
            Assert.That(Actor("tuna"), Does.Contain("Increase"));
            Press("Run Scenario plastic");
            Assert.That(Actor("shark"), Does.Contain("Stable"));
            Assert.That(Actor("tuna"), Does.Contain("Stable"));
            Assert.That(controller.State.CompletedObjectiveCount, Is.Zero);
        }
        [UnityTest] public IEnumerator Scenario_WholePatternReviewRejectsConflictThenOpensConclusion()
        {
            yield return Start(); var controller = Object.FindAnyObjectByType<InvestigationController>();
            TryAll(); yield return null;
            Assert.That(GameObject.Find("Scenario Results"), Is.Not.Null);
            Assert.That(GameObject.Find("Evidence Pattern food-web"), Is.Null);
            Assert.That(GameObject.Find("Prediction shark"), Is.Null);
            Assert.That(controller.State.CompletedObjectiveCount, Is.Zero);
            Press("Choose Scenario plastic"); yield return null;
            Assert.That(controller.State.ProvisionalThreatId, Is.Empty);
            Assert.That(GameObject.Find("Scenario Briefing Message").GetComponent<TextMeshProUGUI>().text, Does.Contain("Tuna"));
            Assert.That(controller.State.ComparisonRecords.Count, Is.Zero);
            Press("Choose Scenario longline"); yield return null;
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo("longline"));
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(7));
            Assert.That(controller.State.MisstepCount, Is.Zero);
            Assert.That(controller.State.ConfirmationReviewed, Is.False);
            Assert.That(GameObject.Find("Complete Scenario Investigation"), Is.Not.Null);
        }
        [UnityTest] public IEnumerator Scenario_InterruptingAndReplayingDoesNotInventCompletedTrials()
        {
            yield return Start(); Press("Finish Scenario Animation");
            Press("Run Scenario plastic"); yield return new WaitForSecondsRealtime(.2f);
            Press("Run Scenario longline"); Press("Finish Scenario Animation");
            Press("Run Scenario bottom_trawling"); Press("Finish Scenario Animation");
            Assert.That(GameObject.Find("Choose Scenario longline").GetComponent<Button>().interactable, Is.False, "Plastic was interrupted and still needs to finish.");
            Press("Run Scenario plastic"); Press("Finish Scenario Animation"); yield return null;
            Assert.That(GameObject.Find("Scenario Results"), Is.Not.Null);
            Press("Replay Scenario longline");
            Assert.That(Actor("tuna"), Does.Contain("Stable"));
            Press("Finish Scenario Animation");
            Assert.That(GameObject.Find("Scenario Results"), Is.Not.Null);
        }
        [UnityTest] public IEnumerator Scenario_NotebookAndSettingsPreserveSurveyPicturesAndPlaybackProgress()
        {
            yield return Start(); Press("Finish Scenario Animation"); Press("Run Scenario longline");
            var record = GameObject.Find("Scenario Observed tuna").transform.Find("Observed Today Artwork");
            int count = 0; foreach (var img in record.GetComponentsInChildren<Image>()) if (img.sprite != null) count++;
            Assert.That(count, Is.EqualTo(1));
            Press("Toggle Notebook Drawer"); yield return new WaitForSecondsRealtime(.3f);
            Press("Close Notebook Drawer"); InvestigationCurrentFlowTestActions.ToggleGuidanceForTests(); yield return null;
            Press("Finish Scenario Animation");
            Assert.That(Actor("tuna"), Does.Contain("Increase"));
            Assert.That(GameObject.Find("Scenario Observed tree_bubblegum_coral").transform.Find("Observed Today Absence"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenario Observed tuna").transform.Find("Observed Change").GetComponent<TextMeshProUGUI>().text, Does.Contain("Fewer sites"));
        }
        [UnityTest] public IEnumerator Scenario_ChosenExplanationCanFinishTheExistingReport()
        {
            yield return Start(); TryAll(); Press("Choose Scenario longline"); yield return null;
            InvestigationCurrentFlowTestActions.ReviewRemainingExplanations(); Press("Complete Scenario Investigation"); yield return null;
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(controller.State.MisstepCount, Is.Zero);
        }
        [UnityTest] public IEnumerator Scenario_ReturningToObserveKeepsTheTrialAndItsRecords()
        {
            yield return Start(); Press("Finish Scenario Animation"); Press("Run Scenario longline");
            Press("Stage Observe"); yield return null;
            Assert.That(GameObject.Find("Scenario Investigation"), Is.Null);
            Press("Stage Simulate"); yield return null;
            if (GameObject.Find("Finish Scenario Animation") != null) Press("Finish Scenario Animation");
            Assert.That(Actor("tuna"), Does.Contain("Increase"));
            Assert.That(GameObject.Find("Metrics").GetComponent<TextMeshProUGUI>().text, Does.Contain("1/3"));
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds.Count, Is.EqualTo(6));
        }
        [UnityTest] public IEnumerator Scenario_LabelsRemainReadableOnANarrowCanvas()
        {
            yield return Start(); Press("Finish Scenario Animation");
            var canvas = Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>(); bool enabled = scaler.enabled; float scale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false; canvas.scaleFactor = Screen.width / 720f;
                yield return null; yield return null; Canvas.ForceUpdateCanvases();
                foreach (string panel in new[] { "Scenario Survey Target", "Scenario Results" })
                    foreach (TextMeshProUGUI text in GameObject.Find(panel).GetComponentsInChildren<TextMeshProUGUI>())
                        Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name + ": " + text.text);
                TryAll(); yield return null; Canvas.ForceUpdateCanvases();
                foreach (TextMeshProUGUI text in GameObject.Find("Scenario Results").GetComponentsInChildren<TextMeshProUGUI>())
                    Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name + ": " + text.text);
            }
            finally { canvas.scaleFactor = scale; scaler.enabled = enabled; }
        }
        [UnityTest] public IEnumerator Scenario_RestartResetsTrialsAndReducedMotionStillRequiresAChoice()
        {
            bool before = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotionForTests(true);
            try
            {
                yield return Start(); var controller = Object.FindAnyObjectByType<InvestigationController>();
                Assert.That(GameObject.Find("Run Scenario plastic"), Is.Not.Null);
                foreach (string id in new[] { "plastic", "longline", "bottom_trawling" }) Press("Run Scenario " + id);
                Assert.That(GameObject.Find("Scenario Results"), Is.Not.Null);
                Assert.That(controller.State.ProvisionalThreatId, Is.Empty);
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady); InvestigationWorkbenchTestActions.EnterSimulate("Stage Simulate"); yield return null;
                Assert.That(controller.State.TriedThreatIds, Is.Empty);
                Assert.That(GameObject.Find("Scenario Results"), Is.Not.Null);
                Assert.That(GameObject.Find("Choose Scenario longline").GetComponent<Button>().interactable, Is.False);
            }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(before); }
        }
    }
}
