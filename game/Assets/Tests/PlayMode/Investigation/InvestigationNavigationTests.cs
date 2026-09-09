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
    public sealed class InvestigationNavigationTests
    {
        private static Button Find(string name) => GameObject.Find(name)?.GetComponent<Button>();

        [UnityTest]
        public IEnumerator Navigation_NotebookReturnsToTheSameSpotlightStep()
        {
            yield return LoadCurrent(); EnterCurrentModels(); yield return null; yield return null; SkipCurrentGuide();
            CurrentPress("Talk To Edna"); yield return null; yield return null;
            string message = GameObject.Find("Scenario Briefing Message").GetComponent<Text>().text;
            int discoveries = CurrentState.DiscoveredObservationIds.Count;
            CurrentPress("Toggle Notebook Drawer"); yield return null; CurrentPress("Close Notebook Drawer");
            yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario Briefing Message").GetComponent<Text>().text, Is.EqualTo(message));
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(discoveries));
            var book = CurrentButton("Toggle Notebook Drawer");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(book.gameObject));
            ExecuteEvents.Execute(book.gameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right, moveVector = Vector2.right }, ExecuteEvents.moveHandler);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(GameObject.Find("Scenario Briefing Next")));
        }

        [UnityTest]
        public IEnumerator Navigation_NotebookReopensAtItsSavedPicturePosition()
        {
            yield return LoadCurrent(); EnterCurrentModels(); yield return null; yield return null; SkipCurrentGuide();
            var canvas = CurrentView.GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            bool enabled = scaler.enabled; float scale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false; canvas.scaleFactor = Screen.width / 720f;
                yield return null; yield return null;
                CurrentPress("Toggle Notebook Drawer"); yield return null; yield return null;
                var scroll = CurrentNotebookScroll(); Canvas.ForceUpdateCanvases();
                Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
                scroll.verticalNormalizedPosition = .45f; yield return null;
                float position = scroll.verticalNormalizedPosition;
                CurrentPress("Close Notebook Drawer"); yield return null; CurrentPress("Toggle Notebook Drawer"); yield return null; yield return null;
                Assert.That(CurrentNotebookScroll().verticalNormalizedPosition, Is.EqualTo(position).Within(.02f));
            }
            finally { canvas.scaleFactor = scale; scaler.enabled = enabled; }
        }
    }
}
