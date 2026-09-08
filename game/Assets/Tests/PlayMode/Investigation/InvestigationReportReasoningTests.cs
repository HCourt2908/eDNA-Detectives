using static EDNA.Investigation.Tests.InvestigationWorkbenchTestActions;
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
    public sealed class InvestigationReportReasoningTests
    {
        private static Text Speech => GameObject.Find("Report Edna Speech").GetComponent<Text>();
        private static Button Find(string name) => GameObject.Find(name)?.GetComponent<Button>();
        private static void Click(string name)
        {
            Button button = Find(name);
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
        }
        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene");
            yield return null;
        }
        private static void BeginDiscussion()
        {
            InspectRov("Review ROV Follow-up");
            Click("Discuss Explanation");
        }

        [UnityTest]
        public IEnumerator ReportReasoning_EveryClueReceivesFeedbackWithoutChangingScoresOrThePlayersCause()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            string[] clues = { "FoodWeb", "Seafloor", "FishingLine" };
            string[] feedback = { "fits both fishing models", "damage predicted by bottom trawling", "does not settle which fishing method" };
            for (int index = 0; index < clues.Length; index++)
            {
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
                Assert.That(controller.State.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);
                foreach (Text text in GameObject.Find("Report Edna Conversation").GetComponentsInChildren<Text>())
                {
                    Assert.That(text.text.ToLowerInvariant(), Does.Not.Contain("fishing line"), "The initial investigation choices must not reveal the result.");
                    Assert.That(text.text.ToLowerInvariant(), Does.Not.Contain("intact"));
                }
                Assert.That(Find("Review ROV Follow-up").GetComponentInChildren<Text>().text, Is.EqualTo("Inspect former shark habitat"));
                BeginDiscussion();
                Assert.That(Find("Keep Report Explanation"), Is.Null);
                Assert.That(Speech.text, Does.Contain("main clue"));
                int comparisons = controller.State.CompletedObjectiveCount;
                int missteps = controller.State.MisstepCount;
                string cause = controller.State.FinalThreatId;
                var evidence = new System.Collections.Generic.List<string>(controller.State.SelectedReportEvidenceIds);
                ConnectCluesToArgument("Report Key Clue " + clues[index]);
                yield return null;
                Assert.That(Speech.text, Does.Contain(feedback[index]));
                Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("2/3"));
                Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(comparisons));
                Assert.That(controller.State.MisstepCount, Is.EqualTo(missteps));
                Assert.That(controller.State.FinalThreatId, Is.EqualTo(cause));
                Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
                CollectionAssert.AreEqual(evidence, controller.State.SelectedReportEvidenceIds);
                string emphasis = GameObject.Find("Report Key Clue Label").GetComponent<Text>().text.Replace("Your key clue: ", string.Empty);
                Click("Keep Report Explanation");
                Assert.That(GameObject.Find("Report Highlighted Clue").GetComponent<Text>().text, Does.Contain(emphasis));
                Click("Submit Final Report");
                yield return null;
                Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct), "All key-clue choices are valid discussion paths.");
                Assert.That(GameObject.Find("Debrief YOUR KEY CLUE").transform.Find("Debrief Statement").GetComponent<Text>().text, Is.EqualTo(emphasis));
            }
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
            BeginDiscussion();
            Assert.That(Find("Report Key Clue FoodWeb"), Is.Not.Null, "Restart/QA reset must clear the previous clue choice.");
        }

        [UnityTest]
        public IEnumerator ReportReasoning_PreservesAndReconsidersTheClueAcrossNotebookAndReportRevision()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
            BeginDiscussion();
            ConnectCluesToArgument("Report Key Clue FoodWeb");
            string response = Speech.text;
            Click("Review Report Comparisons");
            yield return null;
            Click("Revisit Comparison longline shark");
            yield return null;
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            Click("Edna Continue");
            yield return null;
            Assert.That(Speech.text, Is.EqualTo(response));
            Click("Keep Report Explanation");
            Click("Revisit ROV Clues");
            Click("Discuss Explanation");
            Assert.That(Find("Report Key Clue FoodWeb"), Is.Null, "Reviewing the ROV should not force the clue question again.");
            Assert.That(Speech.text, Is.EqualTo(response));
            Click("Reconsider Report Clue");
            ConnectCluesToArgument("Report Key Clue FishingLine");
            Assert.That(Speech.text, Does.Contain("which fishing method"));
            Click("Edit Report Cause");
            Click("Final Cause bottom_trawling");
            Assert.That(GameObject.Find("Report Highlighted Clue").GetComponent<Text>().text, Does.Contain("Fishing line"));
            Click("Submit Final Report");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Incorrect));
            Assert.That(GameObject.Find("Report Key Clue Label").GetComponent<Text>().text, Does.Contain("Fishing line"));
            Click("Reconsider Report Clue");
            ConnectCluesToArgument("Report Key Clue FoodWeb");
            Assert.That(Speech.text, Does.Contain("fits both fishing models"), "An old submission diagnostic must not hide the response to a new clue choice.");
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.EqualTo(1));
            Click("Keep Report Explanation");
            Click("Submit Final Report");
            yield return null;
            Assert.That(Speech.text, Does.Contain("Our finding,"), "A new submission should restore its current diagnostic.");
            Click("Edit Report Cause");
            Click("Final Cause longline");
            Click("Submit Final Report");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(7));
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator ReportReasoning_DiscussionTextAndControlsFitAcrossCanvasWidths()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Canvas canvas = Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            bool originalEnabled = scaler.enabled;
            float originalScale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false;
                foreach (float width in new[] { 1280f, 720f, 620f })
                {
                    canvas.scaleFactor = Screen.width / width;
                    controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
                    yield return null; yield return null;
                    BeginDiscussion();
                    yield return null; yield return null;
                    AssertTextFits();
                    ConnectCluesToArgument("Report Key Clue Seafloor");
                    yield return null; yield return null;
                    AssertTextFits();
                    Click("Reconsider Report Clue");
                    yield return null; yield return null;
                    AssertTextFits();
                    ConnectCluesToArgument("Report Key Clue FoodWeb");
                    Click("Keep Report Explanation");
                    yield return null; yield return null;
                    AssertTextFits();
                }
            }
            finally { canvas.scaleFactor = originalScale; scaler.enabled = originalEnabled; }
        }

        private static void AssertTextFits()
        {
            Canvas.ForceUpdateCanvases();
            foreach (Text text in GameObject.Find("Report Edna Conversation").GetComponentsInChildren<Text>())
                Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name + ": " + text.text);
        }
    }
}
