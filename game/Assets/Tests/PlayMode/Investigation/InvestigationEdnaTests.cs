using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;
using static EDNA.Investigation.Tests.InvestigationWorkbenchTestActions;
using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationEdnaTests
    {
        private static Button Button(string name) => GameObject.Find(name)?.GetComponent<Button>();
        private static Text Speech => GameObject.Find("Edna Speech Text")?.GetComponent<Text>();

        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene");
            yield return null;
            BeginObserveQuestions();
            yield return null;
        }


        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator Edna_FixedObserveGuideHasIntegratedPortraitWithoutRedundantDock()
        {
            yield return Load();
            Assert.That(Button("Talk To Edna"), Is.Null);
            Assert.That(Button("Dismiss Edna"), Is.Null);
            Assert.That(GameObject.Find("Edna Intro Locator"), Is.Null);
            Image introduction = GameObject.Find("Edna Introduction Portrait").GetComponent<Image>();
            Assert.That(introduction.transform.parent.name, Is.EqualTo("Observe Comparison Edna"));
            Assert.That(Bounds(introduction.rectTransform).center.x, Is.GreaterThan(Bounds(GameObject.Find("Observe Comparison Board").GetComponent<RectTransform>()).center.x));
            Assert.That(Button("Edna Next Step"), Is.Null);
            Assert.That(Button("Edna Why"), Is.Null);
            Assert.That(Button("Compare Species shark"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Edna_ComparisonInstructionsFitBesideThePortrait()
        {
            yield return Load();
            Canvas.ForceUpdateCanvases();
            Text message = GameObject.Find("Observe Comparison Instruction").GetComponent<Text>();
            Rect portrait = Bounds(GameObject.Find("Edna Introduction Portrait").GetComponent<RectTransform>());
            Assert.That(Bounds(message.rectTransform).xMax, Is.LessThan(portrait.xMin));
            Assert.That(message.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(message.preferredHeight));
            Assert.That(Button("Edna Next Step"), Is.Null);
            Assert.That(Button("Edna Why"), Is.Null);
            Assert.That(GameObject.Find("Observe Answer Choices"), Is.Null);
        }

        [UnityTest]
        public IEnumerator Edna_StaysVisibleWhileNotebookPreservesPlaybackProgress()
        {
            yield return LoadCurrent(); EnterCurrentModels(); CurrentPress("Run Scenario longline");
            CurrentPress("Toggle Notebook Drawer"); yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
            Assert.That(CurrentButton("Talk To Edna").interactable, Is.False);
            Assert.That(CurrentState.HasTriedThreat("longline"), Is.True);
            CurrentPress("Close Notebook Drawer"); yield return null;
            Assert.That(CurrentButton("Talk To Edna").interactable, Is.True);
            CurrentPress("Finish Scenario Animation"); yield return null;
            Assert.That(GameObject.Find("Metrics").GetComponent<Text>().text, Does.Contain("1/3"));
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(5));
        }

    }
}
