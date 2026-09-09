using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationEdnaVisibilityTests
    {
        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene");
            yield return null;
        }

        private static void Press(string name)
        {
            Button button = GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
        }

        private static void AssertEmbeddedGuide()
        {
            Assert.That(GameObject.Find("Talk To Edna"), Is.Null);
            Assert.That(GameObject.Find("Dismiss Edna"), Is.Null);
            Assert.That(GameObject.Find("Edna Intro Locator"), Is.Null);
        }

        [UnityTest]
        public IEnumerator EmbeddedGuides_NeverShowAnInactiveReopenControl()
        {
            yield return Load();
            AssertEmbeddedGuide();
            InvestigationWorkbenchTestActions.BeginTodayRecording();
            AssertEmbeddedGuide();
            InvestigationWorkbenchTestActions.BeginObserveQuestions(false);
            for (int step = 0; step < 3; step++)
            {
                AssertEmbeddedGuide();
                Press("Comparison Briefing Next");
            }
            AssertEmbeddedGuide();
            Press("Toggle Comparison View");
            AssertEmbeddedGuide();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            foreach (var checkpoint in new[] { InvestigationQaCheckpoint.ReportReady, InvestigationQaCheckpoint.ReportQuestions })
            {
                controller.ApplyQaCheckpoint(checkpoint);
                yield return null;
                AssertEmbeddedGuide();
            }
        }

        [UnityTest]
        public IEnumerator PersistentGuide_HighlightAndNotebookKeepUsefulControls()
        {
            yield return LoadCurrent(); EnterCurrentModels(); yield return null; yield return null; SkipCurrentGuide();
            CurrentPress("Talk To Edna"); yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Not.Null);
            Assert.That(GameObject.Find("Dismiss Edna"), Is.Null);
            CurrentPress("Toggle Notebook Drawer"); yield return null;
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Null);
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
            CurrentPress("Close Notebook Drawer"); yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Not.Null);
            CurrentPress("Scenario Briefing Next"); yield return null;
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Null);
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ClosedCase_NotebookRemainsAvailableAndDockDoesNotLeakIntoRestart()
        {
            yield return LoadCurrent(); CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.CaseClosed);
            yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
            Assert.That(CurrentButton("Scenario Compare Again").interactable, Is.False);
            CurrentPress("Toggle Notebook Drawer"); yield return null;
            Assert.That(GameObject.Find("Notebook Survey Story"), Is.Not.Null);
            CurrentPress("Close Notebook Drawer"); CurrentPress("Restart Completed Case"); yield return null;
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Null);
            Assert.That(GameObject.Find("Arrival Briefing Next"), Is.Not.Null);
        }
    }
}
