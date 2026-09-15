using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EDNA.Investigation.Tests
{
    // Player actions for the current two-act contract. No state injection is used
    // by the full-route helpers; QA checkpoints are explicit at individual tests.
    internal static class InvestigationCurrentFlowTestActions
    {
        public static InvestigationController CurrentController => Object.FindAnyObjectByType<InvestigationController>();
        public static InvestigationRuntimeView CurrentView => Object.FindAnyObjectByType<InvestigationRuntimeView>();
        public static InvestigationState CurrentState => CurrentController.State;
        // Guidance variants remain an internal regression seam, not a player control.
        public static void ToggleGuidanceForTests()
        {
            CurrentController.SendMessage("HandleSetDifficulty",
                CurrentState.Difficulty == InvestigationDifficulty.Easy ? InvestigationDifficulty.Hard : InvestigationDifficulty.Easy,
                SendMessageOptions.RequireReceiver);
        }
        public static IEnumerator LoadCurrent()
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene");
            yield return null; yield return null;
        }
        public static Button CurrentButton(string name)
        {
            var button = GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name); return button;
        }
        public static void CurrentPress(string name)
        {
            var button = CurrentButton(name); Assert.That(button.interactable, Is.True, name); button.onClick.Invoke();
        }
        public static void SkipCurrentGuide()
        {
            if (GameObject.Find("Scenario Briefing Skip") != null) CurrentPress("Scenario Briefing Skip");
        }
        public static void RecordAllFindings()
        {
            foreach (string species in new[] { "shark", "tuna", "krill", "atlantic_herring", "phytoplankton" })
                InvestigationWorkbenchTestActions.Record("Species Marker " + species);
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(5));
        }
        public static void EnterCurrentModels()
        {
            RecordAllFindings();
            InvestigationWorkbenchTestActions.EnterSimulate("Stage Simulate");
            Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            if (GameObject.Find("Finish Scenario Animation") != null) CurrentPress("Finish Scenario Animation");
            SkipCurrentGuide();
        }
        public static void PlayAllModels()
        {
            foreach (string id in new[] { "plastic", "longline", "bottom_trawling" })
            {
                CurrentPress(GameObject.Find("Run Scenario " + id) != null ? "Run Scenario " + id : "Replay Scenario " + id);
                if (GameObject.Find("Finish Scenario Animation") != null) CurrentPress("Finish Scenario Animation");
            }
        }
        public static void ReviewViaModels(string id)
        {
            if (CurrentState.Phase == InvestigationPhase.Report) CurrentPress("Scenario Compare Again");
            CurrentPress("Choose Scenario " + id);
        }
        public static void ReviewRemainingExplanations()
        {
            string selected = CurrentState.ProvisionalThreatId;
            foreach (string id in new[] { "longline", "bottom_trawling" })
                if (!CurrentState.HasReviewedModel(id)) ReviewViaModels(id);
            if (!string.IsNullOrEmpty(selected) && CurrentState.ProvisionalThreatId != selected)
                ReviewViaModels(selected);
        }
        public static void OpenCurrentSummary()
        {
            PlayAllModels(); CurrentPress("Choose Scenario longline"); ReviewRemainingExplanations();
            Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Report));
        }
        public static Transform CurrentChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            Assert.Fail(name + " is missing from " + parent.name); return null;
        }
        public static ScrollRect CurrentNotebookScroll() => CurrentChild(GameObject.Find("Notebook Drawer").transform, "Notebook Drawer Scroll").GetComponent<ScrollRect>();
        public static GameObject CurrentPointerHit(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty); return hits[0].gameObject;
        }
        public static void CurrentPointerClick(Button button)
        {
            var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentPointerHit(button.GetComponent<RectTransform>()));
            Assert.That(target, Is.EqualTo(button.gameObject), button.name);
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }
    }
}
