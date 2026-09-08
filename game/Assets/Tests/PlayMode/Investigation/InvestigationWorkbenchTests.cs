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
    public sealed class InvestigationWorkbenchTests
    {
        private static Button Button(string name) => GameObject.Find(name)?.GetComponent<Button>();
        private static void Press(string name) { var b = Button(name); Assert.That(b, Is.Not.Null, name); Assert.That(b.interactable, Is.True, name); b.onClick.Invoke(); }
        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null; yield return null;
        }
        private static void Drop(string from, string to)
        {
            GameObject source = GameObject.Find(from), target = GameObject.Find(to);
            Assert.That(source, Is.Not.Null, from); Assert.That(target, Is.Not.Null, to);
            var data = new PointerEventData(EventSystem.current) { pointerDrag = source, position = new Vector2(100f,100f) };
            ExecuteEvents.Execute(source, data, ExecuteEvents.beginDragHandler);
            Assert.That(GameObject.Find("Workbench Drag Preview"), Is.Not.Null);
            ExecuteEvents.Execute(target, data, ExecuteEvents.dropHandler);
            if (source != null) ExecuteEvents.Execute(source, data, ExecuteEvents.endDragHandler);
            Assert.That(GameObject.Find("Workbench Drag Preview"), Is.Null);
        }
        [UnityTest]
        public IEnumerator Workbench_ObserveQuestionsRecordAnswersAndKeepMissingSpeciesOffToday()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(Button("Species Marker shark"), Is.Null);
            Assert.That(Button("Species Marker krill"), Is.Null);
            Assert.That(Button("Historical Species Marker shark"), Is.Not.Null);
            Assert.That(Button("Historical Species Marker krill"), Is.Not.Null);
            Assert.That(GameObject.Find("Missing Signal"), Is.Null);
            Assert.That(GameObject.Find("Current Survey Water").GetComponent<Image>().color.a, Is.EqualTo(1f), "Today must cover historical organisms, not show ghosts through a transparent layer.");
            Assert.That(GameObject.Find("Edna Intro Locator").GetComponent<Text>().text, Is.EqualTo("Find EDNA at the top right."));
            foreach (float value in new[] { 0f, 1f })
            {
                GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = value;
                yield return null; Canvas.ForceUpdateCanvases();
                string name = value == 0f ? "Species Marker tuna" : "Historical Species Marker tuna";
                var rect = Button(name).GetComponent<RectTransform>();
                var pointer = new PointerEventData(EventSystem.current) { position = rect.TransformPoint(rect.rect.center) };
                var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(Button(name).gameObject));
            }
            GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = 0f;
            yield return null; Canvas.ForceUpdateCanvases();
            RectTransform absent = Button("Historical Species Marker shark").GetComponent<RectTransform>();
            var absentPointer = new PointerEventData(EventSystem.current) { position = absent.TransformPoint(absent.rect.center) };
            var absentHits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(absentPointer, absentHits);
            foreach (var hit in absentHits)
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject), Is.Not.EqualTo(absent.gameObject), "Blank TODAY must not click an invisible historical shark.");
            var cover = GameObject.Find("Current Survey Water").GetComponent<Image>();
            Assert.That(cover.canvasRenderer.cull, Is.False);
            GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = 1f;
            yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(cover.canvasRenderer.cull, Is.True, "Historical terrain must be revealed by clipping the current water cover.");
            var previousAnswer = Button("Observe Answer NotDetected").onClick;
            string order = AnswerOrder();
            Press("Observe Answer Stable");
            Assert.That(AnswerOrder(), Is.EqualTo(order), "Retrying must not reshuffle choices.");
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Assert.That(GameObject.Find("Observe Question Feedback").GetComponent<Text>().text, Does.Contain("another look"));
            foreach (string id in new[] { "shark", "tuna", "krill", "sea_star", "mussel" })
            {
                InvestigationWorkbenchTestActions.Record("Species Marker " + id);
                yield return null;
                if (id == "shark")
                {
                    previousAnswer.Invoke();
                    Assert.That(controller.State.DiscoveredObservationIds.Count, Is.EqualTo(1), "A stale click must not answer the next question.");
                    string tunaOrder = AnswerOrder();
                    Assert.That(Button("Toggle Notebook Drawer"), Is.Not.Null);
                    Press("Toggle Notebook Drawer"); yield return null;
                    Assert.That(GameObject.Find("Notebook Drawer"), Is.Not.Null);
                    Assert.That(GameObject.Find("Notebook E01_SHARK_NONDETECTION"), Is.Not.Null);
                    Press("Close Notebook Drawer"); yield return null;
                    Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Does.Contain("Tuna"));
                    Assert.That(AnswerOrder(), Is.EqualTo(tunaOrder));
                }
            }
            Assert.That(controller.State.DiscoveredObservationIds.Count, Is.EqualTo(5));
            Assert.That(GameObject.Find("Observe Question"), Is.Null);
            Assert.That(Button("Continue To Simulate"), Is.Not.Null);
            Assert.That(controller.State.MisstepCount, Is.Zero);
        }
        private static string AnswerOrder()
        {
            string order = string.Empty;
            foreach (Transform child in GameObject.Find("Observe Answer Choices").transform) order += child.name + "|";
            return order;
        }
        [UnityTest]
        public IEnumerator Workbench_LensSliderReceivesRealPointerInput()
        {
            yield return Load();
            Slider slider = GameObject.Find("Survey Time Lens").GetComponent<Slider>();
            RectTransform rect = slider.GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = rect.TransformPoint(new Vector2(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, .8f), rect.rect.center.y)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject);
            Assert.That(handler, Is.EqualTo(slider.gameObject));
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(slider.value, Is.GreaterThan(.7f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Workbench_ConnectionsArePlayableAndInvalidLinksDoNotUnlockExperiments()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            foreach(string id in new[] { "shark", "tuna", "krill", "sea_star", "mussel" })
                InvestigationWorkbenchTestActions.Record("Species Marker " + id);
            Press("Continue To Simulate"); yield return null;
            Assert.That(GameObject.Find("Food Web Assembly"), Is.Not.Null);
            Canvas.ForceUpdateCanvases();
            Assert.That(Button("Toggle Notebook Drawer").GetComponent<RectTransform>().rect.width, Is.EqualTo(62f).Within(.5f), "The notebook icon must not stretch across the food-web panel.");
            Drop("Connect Species krill", "Connect Species shark");
            Assert.That(Button("Run Selected Model"), Is.Null);
            Assert.That(controller.State.TriedThreatIds, Is.Empty);
            Drop("Connect Species shark", "Connect Species tuna");
            Assert.That(GameObject.Find("Food Web Assembly"), Is.Not.Null);
            Drop("Connect Species tuna", "Connect Species krill");
            yield return null;
            Assert.That(GameObject.Find("Food Web Assembly"), Is.Null);
            Assert.That(Button("Threat longline"), Is.Not.Null);
            Assert.That(controller.State.TriedThreatIds, Is.Empty);
        }
        [UnityTest]
        public IEnumerator Workbench_InterventionAndPatternDropsCompleteTheInvestigationAndCanBeUndone()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart); yield return null;
            Drop("Threat longline", "Model Workspace"); yield return null;
            Assert.That(controller.State.HasTriedThreat("longline"), Is.True);
            Drop("Evidence Pattern food-web", "Test Evidence Pattern");
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(3));
            Drop("Evidence Pattern benthic", "Test Evidence Pattern");
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(4));
            Press("Reset Workbench Experiment"); yield return null;
            Assert.That(GameObject.Find("Workbench Baseline"), Is.Not.Null);
            Assert.That(GameObject.Find("Baseline shark").GetComponentInChildren<Text>().text, Does.Contain("Stable"));
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(4));
            Press("Run Selected Model"); yield return null;
            Assert.That(GameObject.Find("Workbench Baseline"), Is.Null);
            Drop("Threat plastic", "Model Workspace"); yield return null;
            Drop("Evidence Pattern indicator", "Test Evidence Pattern");
            Drop("Threat bottom_trawling", "Model Workspace"); yield return null;
            Drop("Evidence Pattern food-web", "Test Evidence Pattern");
            Drop("Evidence Pattern benthic", "Test Evidence Pattern");
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(7));
            Assert.That(controller.State.MisstepCount, Is.Zero);
            Assert.That(Button("Write Provisional Report"), Is.Not.Null);
        }
        [UnityTest]
        public IEnumerator Workbench_RovRequiresScanningBeforeRecordingAndReportNeedsTwoDistinctClues()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
            Press("Review ROV Seafloor"); yield return null;
            Assert.That(controller.State.ConfirmationReviewed, Is.False);
            Assert.That(Button("Pin ROV Finding").interactable, Is.False);
            var sweep = GameObject.Find("ROV Camera Sweep").GetComponent<Slider>();
            sweep.value = .15f; Assert.That(Button("Pin ROV Finding").interactable, Is.False);
            sweep.value = .5f; Assert.That(Button("Pin ROV Finding").interactable, Is.False);
            sweep.value = .85f; Assert.That(Button("Pin ROV Finding").interactable, Is.True);
            Press("Pin ROV Finding"); yield return null;
            Assert.That(controller.State.ConfirmationReviewed, Is.True);
            Press("Discuss Explanation"); yield return null;
            Assert.That(Button("Discuss Connected Clues").interactable, Is.False);
            Drop("Report Key Clue FoodWeb", "Argument Slot main");
            Drop("Report Key Clue FoodWeb", "Argument Slot cross");
            Assert.That(Button("Discuss Connected Clues").interactable, Is.False);
            Drop("Report Key Clue Seafloor", "Argument Slot cross");
            Assert.That(Button("Discuss Connected Clues").interactable, Is.True);
            Press("Discuss Connected Clues");
            Press("Keep Report Explanation");
            Assert.That(GameObject.Find("Report Highlighted Clue"), Is.Not.Null);
            Assert.That(GameObject.Find("Report Cross Check").GetComponent<Text>().text, Does.Contain("seafloor"));
            Press("Submit Final Report"); yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
        }
    }
}
