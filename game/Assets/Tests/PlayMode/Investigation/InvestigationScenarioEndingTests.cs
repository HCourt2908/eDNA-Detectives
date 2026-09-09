using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationScenarioEndingTests
    {
        static InvestigationController Controller => Object.FindAnyObjectByType<InvestigationController>();
        static void Press(string name)
        {
            var button = GameObject.Find(name)?.GetComponent<Button>(); Assert.That(button, Is.Not.Null, name);
            Assert.That(button.interactable, Is.True, name); button.onClick.Invoke();
        }
        static IEnumerator Start()
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null;
            Controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            InvestigationWorkbenchTestActions.EnterSimulate("Stage Simulate"); Press("Finish Scenario Animation");
            foreach (string id in new[] { "plastic", "longline", "bottom_trawling" }) { Press("Run Scenario " + id); Press("Finish Scenario Animation"); }
            Press("Choose Scenario longline"); yield return null; yield return null;
        }
        static Rect Bounds(RectTransform rect)
        {
            var points = new Vector3[4]; rect.GetWorldCorners(points); return new Rect(points[0], points[2] - points[0]);
        }
        static void AssertFits()
        {
            var view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            var scroll = view.ContentRoot.GetComponentInParent<ScrollRect>();
            Assert.That(scroll.vertical, Is.False);
            Assert.That(scroll.content.rect.height, Is.LessThanOrEqualTo(scroll.viewport.rect.height + 1f));
            Rect viewport = Bounds(GameObject.Find("Scenario Workspace").GetComponent<RectTransform>());
            Rect workbench = Bounds(GameObject.Find("Scenario Investigation").GetComponent<RectTransform>());
            Assert.That(workbench.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - 1f));
            Assert.That(workbench.xMax, Is.LessThanOrEqualTo(viewport.xMax + 1f));
            Assert.That(workbench.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1f));
            Assert.That(workbench.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1f));
            Assert.That(GameObject.Find("Scenario Notebook Prompt"), Is.Null);
        }
        [UnityTest] public IEnumerator Ending_UsesOnlyGatheredFindingsAndRequiresOneExplicitConclusion()
        {
            yield return Start(); AssertFits();
            Assert.That(GameObject.Find("Stage Report"), Is.Null);
            Assert.That(GameObject.Find("Survey Report Paper"), Is.Null);
            Assert.That(GameObject.Find("Scenario ROV Frame"), Is.Null);
            Assert.That(GameObject.Find("Begin Scenario ROV"), Is.Null);
            Assert.That(GameObject.Find("Scenario Finding Summary survey"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenario Finding Summary controls"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenario Finding Summary model"), Is.Not.Null);
            Assert.That(Controller.State.ConfirmationReviewed, Is.False);
            Assert.That(Controller.State.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);
            Assert.That(Controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.NotSubmitted));
            foreach (Text text in GameObject.Find("Scenario Investigation").GetComponentsInChildren<Text>())
            {
                Assert.That(text.text, Does.Not.Contain("ROV"));
                Assert.That(text.text, Does.Not.Contain("Fishing line"));
                Assert.That(text.text, Does.Not.Contain("seabed"));
            }
            Assert.That(GameObject.Find("Scenario Conclusion Limit").GetComponent<Text>().text, Does.Contain("does not prove"));
            var button = GameObject.Find("Complete Scenario Investigation").GetComponent<Button>();
            Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("Record conclusion"));
            var submit = button.onClick;
            Press("Complete Scenario Investigation"); yield return null; yield return null;
            Assert.That(Controller.State.ConfirmationReviewed, Is.False);
            Assert.That(Controller.State.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);
            Assert.That(Controller.State.HasDiscoveredObservation("E08_SEAFLOOR_INTACT"), Is.False);
            Assert.That(Controller.State.DiscoveredObservationIds.Count, Is.EqualTo(5));
            Assert.That(InvestigationSessionBridge.LastResult.evidenceIds.Count, Is.EqualTo(5));
            Assert.That(InvestigationSessionBridge.LastResult.evidenceIds, Does.Not.Contain("E07_FISHING_LINE"));
            Assert.That(InvestigationSessionBridge.LastResult.evidenceIds, Does.Not.Contain("E08_SEAFLOOR_INTACT"));
            Assert.That(Controller.State.FinalSubmissionAttemptCount, Is.EqualTo(1));
            submit.Invoke(); Assert.That(Controller.State.FinalSubmissionAttemptCount, Is.EqualTo(1));
            Assert.That(Controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(GameObject.Find("Scenario Case Closed").GetComponent<Text>().text, Is.EqualTo("CONCLUSION RECORDED")); AssertFits();
            Press("Stage Observe"); Press("Stage Simulate"); yield return null;
            Assert.That(GameObject.Find("Scenario Case Closed"), Is.Not.Null);
        }
        [UnityTest] public IEnumerator Ending_RecomparisonDoesNotAddEvidenceOrSubmitPrematurely()
        {
            yield return Start(); int comparisons = Controller.State.ComparisonRecords.Count;
            Press("Scenario Compare Again"); yield return null;
            Assert.That(GameObject.Find("Scenario Results"), Is.Not.Null);
            Assert.That(Controller.State.ConfirmationReviewed, Is.False);
            Press("Replay Scenario plastic"); Press("Finish Scenario Animation");
            Press("Choose Scenario longline"); yield return null; yield return null;
            Assert.That(Controller.State.ComparisonRecords.Count, Is.EqualTo(comparisons));
            Assert.That(Controller.State.DiscoveredObservationIds.Count, Is.EqualTo(5));
            Assert.That(Controller.State.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(GameObject.Find("Complete Scenario Investigation").GetComponent<Button>().interactable, Is.True);
            Press("Complete Scenario Investigation"); yield return null;
            Assert.That(Controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
        }
        [UnityTest] public IEnumerator Ending_MainWorkspaceFitsOnACompactCanvasInEveryStep()
        {
            yield return Start();
            var canvas = Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            bool enabled = scaler.enabled; float scale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false; canvas.scaleFactor = Screen.width / 720f; yield return null; yield return null; AssertFits();
                Press("Scenario Compare Again"); yield return null; yield return null; AssertFits();
                Press("Replay Scenario longline"); yield return null; yield return null; AssertFits();
                Press("Finish Scenario Animation"); yield return null; yield return null;
                Press("Return To Scenario Report"); yield return null; yield return null; AssertFits();
                foreach (Text text in GameObject.Find("Scenario EDNA Dock").GetComponentsInChildren<Text>())
                    Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name);
            }
            finally { canvas.scaleFactor = scale; scaler.enabled = enabled; }
        }
        [UnityTest] public IEnumerator Ending_QaCheckpointCanFinishAndRestartWithoutTheOldReportPage()
        {
            yield return Start(); Controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady); yield return null; yield return null;
            Assert.That(GameObject.Find("Complete Scenario Investigation").GetComponent<Button>().interactable, Is.True);
            Press("Complete Scenario Investigation"); yield return null;
            Press("Restart Completed Case"); yield return null;
            Assert.That(Controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Null);
            Assert.That(Controller.State.ConfirmationReviewed, Is.False);
        }
    }
}
