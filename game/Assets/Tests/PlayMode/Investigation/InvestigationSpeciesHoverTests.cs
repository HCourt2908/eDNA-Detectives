using TMPro;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationSpeciesHoverTests
    {
        private static IEnumerator OpenMap()
        {
            yield return LoadCurrent();
            InvestigationWorkbenchTestActions.BeginObserveQuestions();
            CurrentPress("Toggle Comparison View"); yield return null;
            EventSystem.current.SetSelectedGameObject(null);
        }

        [UnityTest] public IEnumerator Hover_PreviewsBothSurveysPromptlyWithoutRecordingOrBlockingControls()
        {
            yield return OpenMap();
            foreach (bool historical in new[] { false, true })
            {
                GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = historical ? 1f : 0f;
                var marker = CurrentButton((historical ? "Historical Species Marker " : "Species Marker ") + "tuna");
                var hit = CurrentPointerHit(marker.GetComponent<RectTransform>());
                Assert.That(ExecuteEvents.GetEventHandler<IPointerEnterHandler>(hit), Is.EqualTo(marker.gameObject));
                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(marker.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(GameObject.Find("Tooltip Title").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Atlantic Bluefin Tuna"));
                Assert.That(GameObject.Find("Close Species Facts"), Is.Null);
                foreach (var graphic in GameObject.Find("Species Facts Tooltip").GetComponentsInChildren<Graphic>())
                    Assert.That(graphic.raycastTarget, Is.False, "Hover previews must not steal map or slider input.");
                Assert.That(CurrentState.DiscoveredObservationIds, Is.Empty);
                ExecuteEvents.Execute(marker.gameObject, pointer, ExecuteEvents.pointerExitHandler);
                yield return null;
                Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            }
        }

        [UnityTest] public IEnumerator Hover_QuickPassDoesNotFlashAndClickKeepsDetailsWhileThePointerRemains()
        {
            yield return OpenMap();
            GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = 0f;
            var marker = CurrentButton("Species Marker tuna");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(marker.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(marker.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            ExecuteEvents.Execute(marker.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(marker.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(GameObject.Find("Close Species Facts"), Is.Not.Null);
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(CurrentState.DiscoveredObservationIds, Is.Empty);
            ExecuteEvents.Execute(marker.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            yield return null; yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
        }
    }
}
