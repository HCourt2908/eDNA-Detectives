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
    public sealed class InvestigationScenarioBriefingTests
    {
        private static void Press(string name)
        {
            var b = GameObject.Find(name)?.GetComponent<Button>(); Assert.That(b, Is.Not.Null, name); Assert.That(b.interactable, Is.True, name); b.onClick.Invoke();
        }
        private static IEnumerator Start()
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null;
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            InvestigationWorkbenchTestActions.EnterSimulate("Stage Simulate"); Press("Finish Scenario Animation"); yield return null; yield return null;
        }
        private static IEnumerator Next() { Press("Scenario Briefing Next"); yield return null; yield return null; }
        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners); return new Rect(corners[0], corners[2] - corners[0]);
        }
        private static void AssertSpotlight(string target)
        {
            Assert.That(GameObject.Find("Scenario Briefing Focus " + target).transform.parent.name, Is.EqualTo("Scenario Briefing Overlay"));
            Assert.That(GameObject.Find("Scenario Briefing Dimmer").GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(GameObject.Find("Edna Conversation"), Is.Null);
            if (target == "Scenario Result longline")
                foreach (Image graphic in GameObject.Find(target).GetComponentsInChildren<Image>())
                    if (graphic.name == "Result Specimen 0") Assert.That(graphic.color.a, Is.GreaterThan(.99f), "The spotlight must preserve every species' main symbol");
        }
        [UnityTest] public IEnumerator Guide_ExplainsSurveyAndPlayButtonsWithoutChangingProgress()
        {
            yield return Start(); var controller = Object.FindAnyObjectByType<InvestigationController>();
            AssertSpotlight("Scenario Survey Target");
            Assert.That(GameObject.Find("Scenario Briefing Speaker").GetComponent<Text>().text, Does.Contain("1/2"));
            var old = GameObject.Find("Scenario Briefing Next").GetComponent<Button>().onClick;
            Rect before = Bounds(GameObject.Find("Scenario Results").GetComponent<RectTransform>());
            yield return Next(); AssertSpotlight("Scenario Results");
            Rect after = Bounds(GameObject.Find("Scenario Results").GetComponent<RectTransform>());
            Assert.That(Vector2.Distance(before.center, after.center), Is.LessThan(.5f), "Guide must not move the workbench");
            Assert.That(Vector2.Distance(before.size, after.size), Is.LessThan(.5f));
            Rect focused = Bounds(GameObject.Find("Scenario Briefing Focus Scenario Results").GetComponent<RectTransform>());
            Assert.That(Vector2.Distance(after.center, focused.center), Is.LessThan(.5f), "Spotlight must stay aligned with the real model");
            old.Invoke(); Assert.That(GameObject.Find("Scenario Briefing Speaker").GetComponent<Text>().text, Does.Contain("2/2"));
            AssertSpotlight("Scenario Results");
            var cause = GameObject.Find("Run Scenario longline").GetComponent<Button>();
            var pointer = new PointerEventData(EventSystem.current) { position = cause.transform.position };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.Not.EqualTo(cause.gameObject), "Guide must block the live controls behind it");
            yield return Next();
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Null);
            Assert.That(GameObject.Find("Scenario Results").transform.parent.name, Is.EqualTo("Scenario Investigation"));
            var visibility = GameObject.Find("Scenario Results").GetComponent<CanvasGroup>();
            Assert.That(visibility == null ? 1f : visibility.alpha, Is.EqualTo(1f));
            Assert.That(controller.State.TriedThreatIds, Is.Empty);
            Assert.That(controller.State.ComparisonRecords.Count, Is.Zero);
            Press("Difficulty Toggle"); yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Null);
            Press("Run Scenario longline"); yield return null;
            Assert.That(controller.State.HasTriedThreat("longline"), Is.True);
        }
        [UnityTest] public IEnumerator Guide_ResultComparisonAndMismatchGetContextualSpotlights()
        {
            yield return Start(); Press("Scenario Briefing Skip");
            Press("Run Scenario longline"); Press("Finish Scenario Animation"); yield return null; yield return null;
            AssertSpotlight("Scenario Result longline"); yield return Next();
            Press("Run Scenario plastic"); Press("Finish Scenario Animation"); yield return null;
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Null);
            Press("Run Scenario bottom_trawling"); Press("Finish Scenario Animation"); yield return null; yield return null;
            AssertSpotlight("Scenario Results"); yield return Next();
            Press("Choose Scenario bottom_trawling"); yield return null; yield return null;
            AssertSpotlight("Scenario Observed sea_star");
            Assert.That(GameObject.Find("Scenario Briefing Message").GetComponent<Text>().text, Does.Contain("Sea star"));
            yield return Next(); Press("Choose Scenario longline"); yield return null;
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Null);
        }
        [UnityTest] public IEnumerator Guide_ManualHelpPausesAndResumesThePrediction()
        {
            yield return Start(); Press("Scenario Briefing Skip"); Press("Run Scenario longline");
            yield return new WaitForSecondsRealtime(.2f); Press("Talk To Edna"); yield return null; yield return null;
            AssertSpotlight("Scenario Result longline");
            string value = GameObject.Find("Scenario Result longline").transform.Find("Scenario Result Species tuna/Result Prediction").GetComponent<Text>().text;
            yield return new WaitForSecondsRealtime(7.5f);
            Assert.That(GameObject.Find("Scenario Result longline").transform.Find("Scenario Result Species tuna/Result Prediction").GetComponent<Text>().text, Is.EqualTo(value));
            Assert.That(GameObject.Find("Metrics").GetComponent<Text>().text, Does.Contain("0/3"));
            yield return Next(); Press("Finish Scenario Animation"); yield return null;
            Assert.That(GameObject.Find("Metrics").GetComponent<Text>().text, Does.Contain("1/3"));
        }
        [UnityTest] public IEnumerator Guide_CompactLayoutReceivesPointerInputAndCleansUpOnObserve()
        {
            yield return Start();
            var view = Object.FindAnyObjectByType<InvestigationRuntimeView>(); var canvas = view.GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            bool enabled = scaler.enabled; float scale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false; canvas.scaleFactor = Screen.width / 720f;
                yield return null; yield return null;
                for (int i = 0; i < 2; i++)
                {
                    Canvas.ForceUpdateCanvases();
                    Rect screen = Bounds(GameObject.Find("Scenario Briefing Overlay").GetComponent<RectTransform>());
                    Rect speech = Bounds(GameObject.Find("Scenario EDNA Dock").GetComponent<RectTransform>());
                    Rect light = Bounds(GameObject.Find("Scenario Briefing Spotlight").GetComponent<RectTransform>());
                    Assert.That(speech.xMin, Is.GreaterThanOrEqualTo(screen.xMin)); Assert.That(speech.xMax, Is.LessThanOrEqualTo(screen.xMax));
                    Assert.That(speech.yMin, Is.GreaterThanOrEqualTo(screen.yMin)); Assert.That(speech.yMax, Is.LessThanOrEqualTo(screen.yMax));
                    foreach (Text text in GameObject.Find("Scenario EDNA Dock").GetComponentsInChildren<Text>())
                        Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name);
                    var b = GameObject.Find("Scenario Briefing Next").GetComponent<Button>();
                    var pointer = new PointerEventData(EventSystem.current) { position = b.transform.position };
                    var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                    Assert.That(hits, Is.Not.Empty); Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(b.gameObject));
                    ExecuteEvents.Execute(b.gameObject, pointer, ExecuteEvents.pointerClickHandler); yield return null; yield return null;
                }
                Press("Talk To Edna"); yield return null; yield return null;
                Press("Stage Observe"); yield return null;
                Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Null);
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            }
            finally { canvas.scaleFactor = scale; scaler.enabled = enabled; }
        }
    }
}
