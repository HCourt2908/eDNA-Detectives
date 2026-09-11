using System.Collections;
using System.Linq;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;
namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationScenarioDetailsTests
    {
        static IEnumerator Start()
        {
            yield return LoadCurrent(); EnterCurrentModels(); yield return null; yield return null; SkipCurrentGuide(); yield return null; yield return null;
            EventSystem.current.SetSelectedGameObject(null);
        }
        static Button Detail(string threat, string species, bool prediction = true)
        {
            var card = GameObject.Find("Scenario Result " + threat).transform;
            var row = CurrentChild(card, "Scenario Result Species " + species);
            return CurrentChild(row, prediction ? "Result Prediction" : "Result Species Name").GetComponentInChildren<Button>();
        }
        static void Enter(Button b) => ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
        static void Leave(Button b) => ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
        static string Body => GameObject.Find("Scenario Detail Body").GetComponent<Text>().text;
        static string Tuna => Detail("longline", "tuna").GetComponentInParent<Text>().text;

        [UnityTest] public IEnumerator Details_HoverIsOptionalNonBlockingAndDoesNotRevealUnplayedResults()
        {
            yield return Start();
            var cause = CurrentButton("Detail Cause longline");
            Enter(cause); Leave(cause); yield return new WaitForSecondsRealtime(.6f);
            Assert.That(GameObject.Find("Scenario Detail Card"), Is.Null);
            Enter(cause); yield return new WaitForSecondsRealtime(.6f);
            Assert.That(Body, Does.Contain("assuming fewer sharks").And.Not.Contain("main explanation"));
            Assert.That(CurrentState.TriedThreatIds, Is.Empty);
            var play = CurrentButton("Run Scenario longline");
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentPointerHit(play.GetComponent<RectTransform>())), Is.EqualTo(play.gameObject));
            Leave(cause);
            var prediction = Detail("longline", "tuna"); Enter(prediction); yield return new WaitForSecondsRealtime(.6f);
            Assert.That(Body, Does.StartWith("Baseline").And.Not.Contain("↑ Increase"));
            Assert.That(Object.FindObjectsByType<InvestigationBorderGraphic>().Count(b => b.name == "Scenario Detail Highlight"), Is.EqualTo(4));
            Leave(prediction); yield return null;
            Assert.That(GameObject.Find("Scenario Detail Card"), Is.Null);
            Assert.That(Object.FindObjectsByType<InvestigationBorderGraphic>().Count(b => b.name == "Scenario Detail Highlight"), Is.Zero);
            var species = Detail("longline", "tuna", false);
            ExecuteEvents.Execute(species.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.selectHandler);
            Assert.That(Body, Does.Contain("Thunnus thynnus"));
        }

        [UnityTest] public IEnumerator Details_PinningPausesAndCloseResumesWithoutMovingTheWorkbench()
        {
            yield return Start(); CurrentPress("Run Scenario longline"); yield return new WaitForSecondsRealtime(.2f);
            var root = GameObject.Find("Scenario Results").GetComponent<RectTransform>(); var position = root.position; var size = root.rect.size;
            var prediction = Detail("longline", "tuna");
            CurrentPointerClick(prediction); yield return null;
            Assert.That(GameObject.Find("Scenario Detail Status").GetComponent<Text>().text, Does.StartWith("Paused"));
            string before = Tuna;
            yield return new WaitForSecondsRealtime(2f);
            Assert.That(Tuna, Is.EqualTo(before));
            Assert.That(root.position, Is.EqualTo(position)); Assert.That(root.rect.size, Is.EqualTo(size));
            var close = CurrentButton("Close Scenario Details");
            ExecuteEvents.Execute(close.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
            yield return null;
            Assert.That(GameObject.Find("Scenario Detail Card"), Is.Null);
            Assert.That(Tuna, Is.EqualTo(before));
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(Tuna, Does.Contain("Increase"));
            prediction = Detail("longline", "tuna"); Enter(prediction); yield return new WaitForSecondsRealtime(.6f);
            Assert.That(Body, Does.Contain("feeding pressure").And.Contain("Your survey").And.Contain("Shark ↓ → Tuna ↑"));
            Assert.That(CurrentState.ReviewedModelThreatIds, Is.Empty);
        }

        [UnityTest] public IEnumerator Details_RestartTakesOverThePauseAndUnknownStaysDistinctFromAbsence()
        {
            yield return Start(); CurrentPress("Run Scenario longline"); yield return new WaitForSecondsRealtime(.2f);
            CurrentPointerClick(Detail("longline", "tuna")); yield return new WaitForSecondsRealtime(.8f);
            string before = Tuna;
            CurrentPointerClick(CurrentButton("Restart Case")); yield return new WaitForSecondsRealtime(1f);
            Assert.That(GameObject.Find("Scenario Detail Card"), Is.Null);
            Assert.That(Tuna, Is.EqualTo(before));
            CurrentPress("Cancel Restart Case"); yield return null;
            Assert.That(Tuna, Is.EqualTo(before));
            CurrentPress("Finish Scenario Animation"); CurrentPress("Run Scenario plastic"); CurrentPress("Finish Scenario Animation");
            yield return null; yield return null; SkipCurrentGuide();
            Enter(Detail("plastic", "atlantic_herring")); yield return new WaitForSecondsRealtime(.6f);
            Assert.That(Body, Does.StartWith("? Unknown").And.Contain("does not mean stable, absent"));
            CurrentPress("Toggle Notebook Drawer"); yield return null;
            Assert.That(GameObject.Find("Scenario Detail Card"), Is.Null);
        }

        [UnityTest] public IEnumerator Details_FixedCardFitsACompactWindowWithoutStartingATrial()
        {
            yield return Start();
            var canvas = CurrentView.GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            bool enabled = scaler.enabled; float scale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false; canvas.scaleFactor = Screen.width / 720f;
                yield return null; yield return null;
                CurrentPointerClick(Detail("longline", "phytoplankton", false)); yield return null;
                Canvas.ForceUpdateCanvases();
                var root = canvas.GetComponent<RectTransform>();
                var card = GameObject.Find("Scenario Detail Card").GetComponent<RectTransform>();
                var corners = new Vector3[4]; card.GetWorldCorners(corners);
                foreach (var point in corners)
                {
                    var local = root.InverseTransformPoint(point);
                    Assert.That(local.x, Is.InRange(root.rect.xMin, root.rect.xMax));
                    Assert.That(local.y, Is.InRange(root.rect.yMin, root.rect.yMax));
                }
                Assert.That(GameObject.Find("Scenario Detail Title").GetComponent<Text>().preferredHeight, Is.LessThanOrEqualTo(53f));
                Assert.That(CurrentState.TriedThreatIds, Is.Empty);
                Assert.That(CurrentView.ContentRoot.GetComponentInParent<ScrollRect>().vertical, Is.False);
                CurrentPointerClick(CurrentButton("Close Scenario Details")); yield return null;
                Assert.That(GameObject.Find("Scenario Detail Card"), Is.Null);
            }
            finally { canvas.scaleFactor = scale; scaler.enabled = enabled; }
        }

        [UnityTest] public IEnumerator Ending_RequiresBothDistinctReviewsAndOffersTheOtherDirectly()
        {
            foreach (string first in new[] { "longline", "bottom_trawling" })
            {
                yield return Start(); PlayAllModels(); CurrentPress("Choose Scenario " + first); yield return null;
                string second = first == "longline" ? "bottom_trawling" : "longline";
                Assert.That(CurrentState.ReviewedModelThreatIds, Is.EquivalentTo(new[] { first }));
                Assert.That(CurrentButton("Complete Scenario Investigation").interactable, Is.False);
                CurrentButton("Complete Scenario Investigation").onClick.Invoke();
                Assert.That(CurrentState.FinalSubmissionAttemptCount, Is.Zero);
                Assert.That(GameObject.Find("Scenario Briefing Message").GetComponent<Text>().text,
                    Does.Contain(second == "longline" ? "Long-line fishing" : "Bottom trawling"));
                CurrentPress("Review Conclusion " + first); yield return null;
                Assert.That(CurrentButton("Complete Scenario Investigation").interactable, Is.False);
                CurrentPress("Review Conclusion " + second); yield return null;
                Assert.That(CurrentState.ReviewedModelThreatIds, Is.EquivalentTo(new[] { first, second }));
                Assert.That(CurrentButton("Complete Scenario Investigation").interactable, Is.True);
                CurrentPress("Complete Scenario Investigation"); yield return null;
                Assert.That(InvestigationSessionBridge.LastResult.reviewedHypothesisIds, Is.EquivalentTo(new[] { first, second }));
                Assert.That(InvestigationSessionBridge.LastResult.selectedHypothesisId, Is.EqualTo(second));
                Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(5));
                CurrentPress("Restart Completed Case"); yield return null;
                Assert.That(CurrentState.ReviewedModelThreatIds, Is.Empty);
            }
        }
    }
}
