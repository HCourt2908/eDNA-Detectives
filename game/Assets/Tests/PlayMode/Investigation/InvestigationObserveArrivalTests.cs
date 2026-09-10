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
    public sealed class InvestigationObserveArrivalTests
    {
        private static void Press(string name)
        {
            Button button = GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name); Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
        }
        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null; yield return null;
        }
        [UnityTest]
        public IEnumerator HistoryGuide_SpotlightsAnInteractiveSliderAndKeepsItsPositionOnContinue()
        {
            yield return Load();
            InvestigationWorkbenchTestActions.BeginTodayRecording();
            Press("Skip Today Recording Animation");
            Press("Compare With History"); yield return null; yield return null;
            Assert.That(GameObject.Find("History Lens Briefing Overlay"), Is.Not.Null);
            Assert.That(GameObject.Find("Survey Lens Controls").transform.parent.name, Is.EqualTo("History Lens Briefing Overlay"));
            Assert.That(GameObject.Find("Survey Lens Handle Cue"), Is.Not.Null);
            Slider lens = GameObject.Find("Survey Time Lens").GetComponent<Slider>();
            Assert.That(lens.interactable, Is.True);
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, lens.handleRect.position);
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject), Is.EqualTo(lens.gameObject));
            ExecuteEvents.Execute(lens.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            var corners = new Vector3[4]; lens.handleRect.parent.GetComponent<RectTransform>().GetWorldCorners(corners);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            ExecuteEvents.Execute(lens.gameObject, pointer, ExecuteEvents.dragHandler);
            Assert.That(lens.value, Is.GreaterThan(.98f));
            Press("Start Recording History");
            Assert.That(GameObject.Find("Today Record In Flight"), Is.Null, "Covered recording action waits for the guide to close.");
            Press("History Lens Briefing Next"); yield return null;
            Assert.That(GameObject.Find("History Lens Briefing Overlay"), Is.Null);
            lens = GameObject.Find("Survey Time Lens").GetComponent<Slider>();
            Assert.That(lens.value, Is.GreaterThan(.98f));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(lens.gameObject));
            InvestigationCurrentFlowTestActions.ToggleGuidanceForTests(); yield return null;
            Assert.That(GameObject.Find("History Lens Briefing Overlay"), Is.Null, "A settings refresh must not replay the guide.");
            Assert.That(GameObject.Find("Start Recording History").GetComponent<Button>().interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator HistoricalRecords_KeepTheLensFreeWithoutChangingSavedRecordsOrCompletingComparison()
        {
            yield return Load();
            InvestigationWorkbenchTestActions.BeginTodayRecording();
            Press("Skip Today Recording Animation");
            Press("Compare With History"); Press("History Lens Briefing Next");
            GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = 1f;
            Press("Start Recording History");
            Assert.That(GameObject.Find("Survey Time Lens").GetComponent<Slider>().interactable, Is.False, "Lock only while the pictures are being collected.");
            Press("Skip History Recording Animation"); yield return null;
            Slider lens = GameObject.Find("Survey Time Lens").GetComponent<Slider>();
            Assert.That(lens.interactable, Is.True);
            lens.value = 0f; yield return null;
            Assert.That(GameObject.Find("Lens Today Label").activeInHierarchy, Is.True);
            Assert.That(GameObject.Find("Historical Notebook Row shark"), Is.Not.Null);
            Assert.That(GameObject.Find("Today Recording Status").GetComponent<Text>().text, Does.Contain("20 years ago"));
            lens.value = .4f;
            InvestigationCurrentFlowTestActions.ToggleGuidanceForTests(); yield return null;
            lens = GameObject.Find("Survey Time Lens").GetComponent<Slider>();
            Assert.That(lens.interactable, Is.True);
            Assert.That(lens.value, Is.EqualTo(.4f).Within(.001f));
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Is.Empty);
            Assert.That(GameObject.Find("Compare Recorded Surveys").GetComponent<Button>().interactable, Is.True);
            Press("Compare Recorded Surveys"); yield return null;
            Assert.That(GameObject.Find("Comparison Briefing Overlay"), Is.Not.Null);
            Assert.That(GameObject.Find("Historical Notebook Row shark"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Arrival_StartsTodayAndFliesRecordsBeforeOfferingComparisons()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            Slider lens = GameObject.Find("Survey Time Lens").GetComponent<Slider>();
            Assert.That(lens.value, Is.Zero); Assert.That(lens.interactable, Is.False);
            Assert.That(GameObject.Find("Observe Answer Choices"), Is.Null);
            Assert.That(GameObject.Find("Toggle Notebook Drawer"), Is.Null);
            InvestigationWorkbenchTestActions.BeginTodayRecording(); yield return null; yield return null;
            Assert.That(GameObject.Find("Today Notebook Row shark"), Is.Null);
            Assert.That(GameObject.Find("Today Notebook Row krill"), Is.Not.Null);
            Assert.That(GameObject.Find("Today Negative Survey Record"), Is.Null);
            GameObject flight = GameObject.Find("Today Record In Flight");
            Assert.That(flight, Is.Not.Null);
            Assert.That(flight.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Vector3 a = flight.transform.position;
            yield return new WaitForSecondsRealtime(.16f);
            Assert.That(Vector3.Distance(a, flight.transform.position), Is.GreaterThan(5f));
            float deadline = Time.realtimeSinceStartup + 6f;
            while (GameObject.Find("Compare With History") == null && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(GameObject.Find("Observe Answer Choices"), Is.Null);
                Assert.That(GameObject.Find("Species Marker shark"), Is.Null);
                Assert.That(GameObject.Find("Species Marker krill"), Is.Not.Null);
                yield return null;
            }
            Assert.That(GameObject.Find("Today Record In Flight"), Is.Null);
            Assert.That(GameObject.Find("Compare With History"), Is.Not.Null);
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty, "Saving current survey records must not answer the historical comparison questions.");
            Assert.That(GameObject.Find("Today Notebook Row shark"), Is.Null);
            Assert.That(GameObject.Find("Today Notebook Row krill"), Is.Not.Null);
            Assert.That(GameObject.Find("Today Recording Status").GetComponent<Text>().text, Does.Contain("4/4"));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Compare With History"));
            Press("Compare With History"); Press("History Lens Briefing Next"); yield return null;
            Assert.That(GameObject.Find("Observe Answer Choices"), Is.Null);
            lens = GameObject.Find("Survey Time Lens").GetComponent<Slider>();
            Assert.That(lens.value, Is.Zero); Assert.That(lens.interactable, Is.True);
            Button history = GameObject.Find("Start Recording History").GetComponent<Button>();
            Assert.That(history.interactable, Is.False);
            history.onClick.Invoke();
            Assert.That(GameObject.Find("Today Record In Flight"), Is.Null);
            lens.value = .5f;
            Assert.That(history.interactable, Is.False);
            lens.value = 1f;
            Assert.That(history.interactable, Is.True);
            Assert.That(GameObject.Find("Historical Notebook Row shark"), Is.Null, "Historical records require another explicit recording action.");
            Press("Start Recording History"); yield return null; yield return null;
            Assert.That(GameObject.Find("Today Record In Flight"), Is.Not.Null);
            deadline = Time.realtimeSinceStartup + 6f;
            while (GameObject.Find("Compare Recorded Surveys") == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(GameObject.Find("Historical Notebook Row shark").transform.Find("Today Notebook Result").GetComponent<Text>().text, Is.EqualTo("Detected 20 years ago"));
            Assert.That(GameObject.Find("Historical Notebook Row krill"), Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Survey Time Lens"));
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Press("Compare Recorded Surveys");
            InvestigationWorkbenchTestActions.BeginObserveQuestions(); yield return null;
            Assert.That(GameObject.Find("Observe Comparison Board"), Is.Not.Null);
            Assert.That(GameObject.Find("Observe Answer Choices"), Is.Null);
            Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null);
            Assert.That(GameObject.Find("Today Survey Notes Title"), Is.Not.Null);
            Assert.That(GameObject.Find("Historical Survey Notes Title"), Is.Not.Null);
            Assert.That(GameObject.Find("Today Notebook Row shark"), Is.Null);
            Assert.That(GameObject.Find("Historical Notebook Row shark"), Is.Not.Null);

            InvestigationWorkbenchTestActions.Record("Species Marker shark");
            Assert.That(controller.State.DiscoveredObservationIds.Count, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator Arrival_RefreshSkipAndRestartDoNotLeaveFlyingArtifactsOrDuplicateFindings()
        {
            yield return Load(); InvestigationWorkbenchTestActions.BeginTodayRecording();
            yield return new WaitForSecondsRealtime(.9f);
            InvestigationCurrentFlowTestActions.ToggleGuidanceForTests(); yield return null; yield return null;
            int flights = 0;
            foreach (RectTransform rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude))
                if (rect.name == "Today Record In Flight") flights++;
            Assert.That(flights, Is.EqualTo(1));
            Press("Skip Today Recording Animation"); yield return null;
            Assert.That(GameObject.Find("Today Record In Flight"), Is.Null);
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start); yield return null;
            Assert.That(GameObject.Find("Arrival Briefing Next"), Is.Not.Null);
            Assert.That(GameObject.Find("Today Negative Survey Record"), Is.Null);
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            InvestigationWorkbenchTestActions.BeginTodayRecording(); yield return null; yield return null;
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start); yield return null;
            Assert.That(GameObject.Find("Today Record In Flight"), Is.Null);
            Assert.That(GameObject.Find("Arrival Briefing Next"), Is.Not.Null);
        }
        [UnityTest]
        public IEnumerator Arrival_ReducedMotionStillRecordsAndRequiresAnExplicitComparisonStep()
        {
            bool previous = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotionForTests(true);
            try
            {
                yield return Load(); InvestigationWorkbenchTestActions.BeginTodayRecording(); yield return null;
                Assert.That(GameObject.Find("Today Record In Flight"), Is.Null);
                Assert.That(GameObject.Find("Compare With History"), Is.Not.Null);
                Assert.That(GameObject.Find("Observe Answer Choices"), Is.Null);
                Press("Compare With History"); Press("History Lens Briefing Next");
                Assert.That(GameObject.Find("Observe Answer Choices"), Is.Null);
                GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = 1f;
                Press("Start Recording History");
                Assert.That(GameObject.Find("Today Record In Flight"), Is.Null);
                Assert.That(GameObject.Find("Compare Recorded Surveys"), Is.Not.Null);
                Press("Compare Recorded Surveys");
                Assert.That(GameObject.Find("Observe Comparison Board"), Is.Not.Null);
            }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(previous); }
        }
    }
}
