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
    public sealed class InvestigationSurveyStoryTests
    {
        private static void Press(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null;
        }

        [UnityTest] public IEnumerator Opening_IntroducesOnlyTheCurrentStepAndStartsRecordingExplicitly()
        {
            yield return Load();
            Assert.That(GameObject.Find("Arrival Briefing Speaker").GetComponent<Text>().text, Does.Contain("1/2"));
            Assert.That(GameObject.Find("Survey Comparison").transform.parent.name, Is.EqualTo("Arrival Briefing Overlay"));
            Assert.That(GameObject.Find("Observe Welcome").GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(GameObject.Find("Start Recording Today"), Is.Null, "The next action lives in EDNA, not in a duplicate prompt.");
            Assert.That(GameObject.Find("Today Record In Flight"), Is.Null, "Covered controls cannot advance the tutorial.");
            var stale = GameObject.Find("Arrival Briefing Next").GetComponent<Button>().onClick;
            Press("Arrival Briefing Next"); stale.Invoke();
            Assert.That(GameObject.Find("Arrival Briefing Speaker").GetComponent<Text>().text, Does.Contain("2/2"));
            Assert.That(GameObject.Find("Observe Welcome").transform.parent.name, Is.EqualTo("Arrival Briefing Overlay"));
            Press("Arrival Briefing Next"); yield return null; yield return null;
            Assert.That(GameObject.Find("Arrival Briefing Overlay"), Is.Null);
            Assert.That(GameObject.Find("Today Record In Flight"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Is.Empty);
        }

        [UnityTest] public IEnumerator SummaryAction_SitsInsideSpeciesAndRemainsReadableAndClickableOnNarrowScreens()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            var canvas = Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>(); bool enabled = scaler.enabled; float scale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false;
                foreach (float width in new[] { 1280f, 720f })
                {
                    canvas.scaleFactor = Screen.width / width;
                    yield return null; yield return null; Canvas.ForceUpdateCanvases();
                    var button = GameObject.Find("Summarize Findings").GetComponent<Button>();
                    Assert.That(button.transform.parent.name, Is.EqualTo("Comparison Species Page"));
                    Assert.That(GameObject.Find("Observe Comparison Instruction").GetComponent<Text>().text,
                        Does.Contain("below Species"));
                    Assert.That(GameObject.Find("Summarise Findings Cue"), Is.Not.Null);
                    var panel = button.transform.parent.GetComponent<RectTransform>();
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, button.transform);
                    Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(panel.rect.yMin - 1f));
                    Assert.That(bounds.max.x, Is.LessThanOrEqualTo(panel.rect.xMax + 1f));
                    foreach (Text text in panel.GetComponentsInChildren<Text>())
                        Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name);
                    var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                        { position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) };
                    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                    Assert.That(hits, Is.Not.Empty);
                    Assert.That(UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject),
                        Is.EqualTo(button.gameObject));
                }
            }
            finally { canvas.scaleFactor = scale; scaler.enabled = enabled; }
        }

        [UnityTest] public IEnumerator CompletedSorting_WaitsForSummarizeThenAnimatesWithoutSavingOrChangingPhase()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            Assert.That(GameObject.Find("Observe Survey Story"), Is.Null);
            Assert.That(GameObject.Find("Comparison Changes Page"), Is.Not.Null);
            Assert.That(GameObject.Find("Sorted Species tuna"), Is.Not.Null);
            InvestigationCurrentFlowTestActions.ToggleGuidanceForTests(); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(GameObject.Find("Observe Survey Story"), Is.Null);
            var stale = GameObject.Find("Summarize Findings").GetComponent<Button>().onClick;
            Press("Summarize Findings"); stale.Invoke();
            Assert.That(GameObject.Find("Summarize Findings").GetComponent<Button>().interactable, Is.False);
            yield return new WaitForSecondsRealtime(.12f);
            float alpha = GameObject.Find("Species Sorting Columns").GetComponent<CanvasGroup>().alpha;
            Assert.That(alpha, Is.InRange(.01f, .99f));
            yield return new WaitForSecondsRealtime(.75f);
            Assert.That(GameObject.Find("Observe Survey Story"), Is.Not.Null);
            Assert.That(GameObject.Find("Comparison Changes Page"), Is.Null);
            Assert.That(GameObject.Find("Continue To Simulate").GetComponent<Button>().interactable, Is.True);
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Continue To Simulate"));
            Assert.That(GameObject.Find("Survey Summary In Flight"), Is.Null);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(controller.State.DiscoveredObservationIds.Count, Is.EqualTo(5));
            var tuna = GameObject.Find("Story Finding tuna").transform;
            Assert.That(tuna.Find("Story Before").childCount, Is.EqualTo(1));
            Assert.That(tuna.Find("Story After").childCount, Is.EqualTo(3));
            var shark = GameObject.Find("Story Finding shark").transform;
            Assert.That(shark.Find("Story Before").childCount, Is.EqualTo(1));
            Assert.That(shark.Find("Story After").childCount, Is.Zero);
            var star = GameObject.Find("Story Finding atlantic_herring").transform;
            Assert.That(star.Find("Story Before").childCount, Is.EqualTo(3));
            Assert.That(star.Find("Story After").childCount, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator Story_ShowsTrueSurveyPicturesAndSavesBeforeThePhaseTransition()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            InvestigationWorkbenchTestActions.ShowSurveySummary();
            Assert.That(GameObject.Find("Observe Survey Story"), Is.Not.Null);
            Assert.That(GameObject.Find("Story Past Species shark"), Is.Not.Null);
            Assert.That(GameObject.Find("Story Today Species shark"), Is.Null);
            Assert.That(GameObject.Find("Story Today Species krill"), Is.Not.Null);
            Assert.That(GameObject.Find("Story Today Species tuna").transform.Find("Group Member Left"), Is.Not.Null);
            Assert.That(GameObject.Find("Story Past Species tuna").transform.Find("Group Member Left"), Is.Null);
            Assert.That(GameObject.Find("Comparison Species Page"), Is.Null);
            Press("Continue To Simulate"); yield return null; yield return null;
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(GameObject.Find("Survey Summary In Flight"), Is.Not.Null);
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(GameObject.Find("Survey Summary In Flight"), Is.Null);
            Assert.That(GameObject.Find("Observe Summary Status").GetComponent<Text>().text, Does.StartWith("Saved"));
            Press("Summary Notebook Destination");
            Assert.That(GameObject.Find("Notebook Survey Story"), Is.Not.Null);
            Press("Close Notebook Drawer"); Press("Continue To Simulate");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            Press("Toggle Notebook Drawer");
            Assert.That(GameObject.Find("Notebook Survey Story"), Is.Not.Null);
            Assert.That(controller.State.DiscoveredObservationIds.Count, Is.EqualTo(5));
        }

        [UnityTest] public IEnumerator Story_RefreshSkipAndRestartLeaveNoDuplicateAnimation()
        {
            yield return Load();
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            InvestigationWorkbenchTestActions.ShowSurveySummary();
            Press("Stage Simulate"); yield return null; yield return null;
            InvestigationCurrentFlowTestActions.ToggleGuidanceForTests(); yield return null; yield return null;
            int count = 0;
            foreach (RectTransform item in Object.FindObjectsByType<RectTransform>())
                if (item.name == "Survey Summary In Flight" && item.gameObject.activeInHierarchy) count++;
            Assert.That(count, Is.EqualTo(1));
            var view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            view.enabled = false; yield return null;
            Assert.That(GameObject.Find("Survey Summary In Flight"), Is.Null);
            view.enabled = true; yield return null; yield return null;
            Assert.That(GameObject.Find("Survey Summary In Flight"), Is.Not.Null);
            Press("Finish Saving Summary");
            Assert.That(GameObject.Find("Survey Summary In Flight"), Is.Null);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start); yield return null;
            Assert.That(GameObject.Find("Arrival Briefing Next"), Is.Not.Null);
            Assert.That(GameObject.Find("Observe Survey Story"), Is.Null);
        }
    }
}
