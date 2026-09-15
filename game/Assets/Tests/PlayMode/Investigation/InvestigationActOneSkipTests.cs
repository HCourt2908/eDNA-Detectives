using TMPro;
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
    public sealed class InvestigationActOneSkipTests
    {
        private static void Press(string name)
        {
            var button = GameObject.Find(name)?.GetComponent<Button>();
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

        [UnityTest] public IEnumerator OpeningSkip_LeavesRecordingExplicitAtEitherIntroductionStep()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            for (int step = 0; step < 2; step++)
            {
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start);
                if (step == 1) Press("Arrival Briefing Next");
                var stale = GameObject.Find("Arrival Briefing Skip").GetComponent<Button>().onClick;
                Press("Arrival Briefing Skip"); stale.Invoke();
                Assert.That(GameObject.Find("Arrival Briefing Overlay"), Is.Null);
                Assert.That(GameObject.Find("Today Record In Flight"), Is.Null);
                Assert.That(GameObject.Find("Today Notebook Row tuna"), Is.Null);
                Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Start Recording Today"));
                InvestigationCurrentFlowTestActions.ToggleGuidanceForTests();
                Assert.That(GameObject.Find("Arrival Briefing Overlay"), Is.Null);
                Press("Start Recording Today"); yield return null; yield return null;
                Assert.That(GameObject.Find("Skip Today Recording Animation"), Is.Not.Null);
                Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            }
        }

        [UnityTest] public IEnumerator HistorySkip_PreservesTheLensAndDoesNotCollectThePast()
        {
            yield return Load();
            InvestigationWorkbenchTestActions.BeginTodayRecording();
            Press("Skip Today Recording Animation"); Press("Compare With History");
            GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = .6f;
            Press("History Lens Briefing Skip");
            Assert.That(GameObject.Find("History Lens Briefing Overlay"), Is.Null);
            var slider = GameObject.Find("Survey Time Lens").GetComponent<Slider>();
            Assert.That(slider.interactable, Is.True);
            Assert.That(slider.value, Is.EqualTo(.6f).Within(.001f));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(slider.gameObject));
            Assert.That(GameObject.Find("Historical Notebook Row shark"), Is.Null);
            Assert.That(GameObject.Find("Start Recording History").GetComponent<Button>().interactable, Is.False);
            slider.value = 1f; Press("Start Recording History"); Press("Skip History Recording Animation");
            Assert.That(GameObject.Find("Historical Notebook Row shark"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Is.Empty);
        }

        [UnityTest] public IEnumerator ComparisonSkip_RevealsTheWorkspaceWithoutAnsweringAnySpecies()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            for (int step = 0; step < 3; step++)
            {
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start);
                InvestigationWorkbenchTestActions.BeginObserveQuestions(false);
                for (int i = 0; i < step; i++) Press("Comparison Briefing Next");
                var stale = GameObject.Find("Comparison Briefing Skip").GetComponent<Button>().onClick;
                Press("Comparison Briefing Skip"); stale.Invoke();
                Assert.That(GameObject.Find("Comparison Briefing Overlay"), Is.Null);
                foreach (string panel in new[] { "Comparison Notebook", "Comparison Species Page", "Comparison Changes Page" })
                {
                    var group = GameObject.Find(panel).GetComponent<CanvasGroup>();
                    Assert.That(group == null || group.alpha == 1f, Is.True, panel);
                }
                Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
                Assert.That(GameObject.Find("Summarize Findings"), Is.Null);
                Press("Compare Species shark"); Press("Compare Change NotDetected");
                Assert.That(controller.State.DiscoveredObservationIds.Count, Is.EqualTo(1));
                InvestigationCurrentFlowTestActions.ToggleGuidanceForTests();
                Assert.That(GameObject.Find("Comparison Briefing Overlay"), Is.Null);
            }
        }

        [UnityTest] public IEnumerator Skip_StaysBesideNextAndAcceptsPointerAndKeyboardOnNarrowScreens()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            var canvas = Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>(); bool enabled = scaler.enabled; float scale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false;
                foreach (float width in new[] { 1280f, 720f })
                {
                    canvas.scaleFactor = Screen.width / width;
                    controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start);
                    yield return null; yield return null; Canvas.ForceUpdateCanvases();
                    var next = GameObject.Find("Arrival Briefing Next").GetComponent<Button>();
                    var skip = GameObject.Find("Arrival Briefing Skip").GetComponent<Button>();
                    Assert.That(next.navigation.selectOnRight, Is.EqualTo(skip));
                    Assert.That(skip.navigation.selectOnLeft, Is.EqualTo(next));
                    var speech = GameObject.Find("Arrival Briefing Speech").GetComponent<RectTransform>();
                    foreach (var button in new[] { next, skip })
                    {
                        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(speech, button.transform);
                        Assert.That(bounds.max.x, Is.LessThanOrEqualTo(speech.rect.xMax + 1f));
                        Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(speech.rect.yMin - 1f));
                        var text = button.GetComponentInChildren<TextMeshProUGUI>();
                        Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight));
                    }
                    var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, skip.transform.position) };
                    var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                    Assert.That(hits, Is.Not.Empty);
                    Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(skip.gameObject));
                    EventSystem.current.SetSelectedGameObject(skip.gameObject);
                    ExecuteEvents.Execute(skip.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                    Assert.That(GameObject.Find("Arrival Briefing Overlay"), Is.Null);
                }
            }
            finally { canvas.scaleFactor = scale; scaler.enabled = enabled; }
        }
    }
}
