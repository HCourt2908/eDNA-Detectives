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
    public sealed class InvestigationNavigationTests
    {
        private static Button Find(string name) => GameObject.Find(name)?.GetComponent<Button>();
        private static void Click(string name)
        {
            Button button = Find(name);
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
        }
        private static IEnumerator Load(InvestigationQaCheckpoint checkpoint)
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene");
            yield return null;
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(checkpoint);
            yield return null;
        }
        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }
        [UnityTest]
        public IEnumerator Navigation_NotebookReturnRestoresTheActiveEdnaTask()
        {
            yield return Load(InvestigationQaCheckpoint.SimulateStart);
            Click("Threat longline"); Click("Edna Continue"); Click("Prediction shark");
            Click("Observation E01_SHARK_NONDETECTION");
            Assert.That(Find("Edna Continue"),Is.Not.Null);
            Click("Edna Open Notebook"); yield return null;
            Click("Close Notebook Drawer"); yield return null;
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.CompletedObjectiveCount,Is.EqualTo(1));
            Assert.That(GameObject.Find("Edna Speech Text"),Is.Not.Null,
                "Closing the Notebook should resume EDNA and her next action.");
        }
        [UnityTest]
        public IEnumerator Navigation_ReportCompareActionShowsTheCurrentCause()
        {
            yield return Load(InvestigationQaCheckpoint.FinalReportReady);
            Canvas canvas=Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>();
            CanvasScaler scaler=canvas.GetComponent<CanvasScaler>();
            bool wasEnabled=scaler.enabled; float originalScale=canvas.scaleFactor;
            try
            {
                scaler.enabled=false; canvas.scaleFactor=Screen.width/1000f;
                yield return null; yield return null;
                Click("Edit Report Cause"); Click("Cancel Report Cause");
                Click("Toggle Notebook Drawer"); Click("Toggle Hypothesis Summary");
                yield return null; yield return null;
                ScrollRect notebook=GameObject.Find("Notebook Drawer Scroll").GetComponent<ScrollRect>();
                Assert.That(notebook.content.rect.height,Is.GreaterThan(notebook.viewport.rect.height));
                notebook.verticalNormalizedPosition=0f;
                yield return null;
                Click("Close Notebook Drawer"); yield return null;
                Click("Review Report Comparisons"); yield return null; yield return null;
                notebook=GameObject.Find("Notebook Drawer Scroll").GetComponent<ScrollRect>();
                Rect card=Bounds(Find("Hypothesis Card longline").GetComponent<RectTransform>());
                Rect viewport=Bounds(notebook.viewport);
                Assert.That(card.yMin,Is.GreaterThanOrEqualTo(viewport.yMin),"Review comparisons should show its comparison controls.");
                Assert.That(card.yMax,Is.LessThanOrEqualTo(viewport.yMax),"The current cause must be visible when opening its comparisons.");
                Assert.That(Find("Revisit Comparison longline shark"), Is.Not.Null, "The current cause should be expanded.");
                Assert.That(Find("Revisit Comparison plastic mussel"), Is.Null);
                Rect firstRecord=Bounds(Find("Revisit Comparison longline shark").GetComponent<RectTransform>());
                Assert.That(firstRecord.yMin, Is.GreaterThanOrEqualTo(viewport.yMin));
                Assert.That(firstRecord.yMax, Is.LessThanOrEqualTo(viewport.yMax));
            }
            finally { canvas.scaleFactor=originalScale; scaler.enabled=wasEnabled; }
        }
        [UnityTest]
        public IEnumerator Navigation_NarrowNextCheckRevealsTheEvidenceChoices()
        {
            yield return Load(InvestigationQaCheckpoint.SimulateStart);
            Canvas canvas=Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>();
            CanvasScaler scaler=canvas.GetComponent<CanvasScaler>();
            bool wasEnabled=scaler.enabled; float originalScale=canvas.scaleFactor;
            try
            {
                scaler.enabled=false; canvas.scaleFactor=Screen.width/900f;
                yield return null; yield return null;
                Click("Threat longline"); Click("Edna Continue"); yield return null;
                Click("Talk To Edna");
                Click("Edna Continue"); yield return null; yield return null;
                Assert.That(GameObject.Find("Edna Speech Text").GetComponent<Text>().text,Does.Contain("WHAT WE FOUND"));
                ScrollRect page=GameObject.Find("Investigation Content").GetComponent<ScrollRect>();
                Rect choices=Bounds(GameObject.Find("Observation Selection").GetComponent<RectTransform>());
                Rect viewport=Bounds(page.viewport);
                Assert.That(choices.yMin, Is.GreaterThanOrEqualTo(viewport.yMin), "The evidence choices should be fully visible after the guided action.");
                Assert.That(choices.yMax, Is.LessThanOrEqualTo(viewport.yMax));
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.CompletedObjectiveCount, Is.Zero, "Navigation must not submit an answer.");
            }
            finally { canvas.scaleFactor=originalScale; scaler.enabled=wasEnabled; }
        }
        [UnityTest]
        public IEnumerator Navigation_NotebookPreservesTheReplyAndRespectsDismissal()
        {
            yield return Load(InvestigationQaCheckpoint.SimulateStart);
            Click("Threat longline"); Click("Edna Continue"); Click("Prediction shark");
            Click("Observation E01_SHARK_NONDETECTION"); Click("Edna Why");
            string explanation=GameObject.Find("Edna Speech Text").GetComponent<Text>().text;
            Click("Toggle Notebook Drawer"); yield return null;
            Assert.That(GameObject.Find("Edna Speech Text"), Is.Null);
            Click("Notebook Drawer Scrim"); yield return null;
            Assert.That(GameObject.Find("Edna Speech Text").GetComponent<Text>().text, Is.EqualTo(explanation));
            Assert.That(Find("Edna Continue"), Is.Not.Null);
            Click("Dismiss Edna");
            Click("Toggle Notebook Drawer"); yield return null;
            Click("Close Notebook Drawer"); yield return null;
            Assert.That(GameObject.Find("Edna Speech Text"), Is.Null, "A previously dismissed conversation must stay dismissed.");
        }

        [UnityTest]
        public IEnumerator Navigation_OrdinaryNotebookOpeningKeepsTheReadingPosition()
        {
            yield return Load(InvestigationQaCheckpoint.FinalReportReady);
            Click("Toggle Notebook Drawer"); Click("Toggle Hypothesis Summary");
            yield return null; yield return null;
            Click("Close Notebook Drawer"); Click("Toggle Notebook Drawer");
            yield return null; yield return null;
            ScrollRect notebook=GameObject.Find("Notebook Drawer Scroll").GetComponent<ScrollRect>();
            Assert.That(notebook.content.rect.height, Is.GreaterThan(notebook.viewport.rect.height));
            notebook.verticalNormalizedPosition=.25f;
            yield return null;
            Click("Close Notebook Drawer"); yield return null;
            Click("Toggle Notebook Drawer"); yield return null; yield return null;
            notebook=GameObject.Find("Notebook Drawer Scroll").GetComponent<ScrollRect>();
            Assert.That(notebook.verticalNormalizedPosition, Is.EqualTo(.25f).Within(.01f));
        }
    }
}
