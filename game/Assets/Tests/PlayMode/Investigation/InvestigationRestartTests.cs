using TMPro;
using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationRestartTests
    {
        [UnityTest]
        public IEnumerator HeaderRestart_IsReachableAboveGuides_AndCancelPreservesProgress()
        {
            yield return LoadCurrent();
            Assert.That(GameObject.Find("Difficulty Toggle"), Is.Null);
            Assert.That(GameObject.Find("Motion Toggle"), Is.Null);
            Assert.That(CurrentButton("Restart Case").GetComponentInChildren<TextMeshProUGUI>().text, Is.Empty);
            Assert.That(CurrentButton("Restart Case").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            CurrentPointerClick(CurrentButton("Restart Case"));
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Cancel Restart Case"));
            CurrentPointerClick(CurrentButton("Cancel Restart Case"));
            Assert.That(CurrentState.DiscoveredObservationIds, Is.Empty);
            Assert.That(GameObject.Find("Restart Confirmation"), Is.Null);

            RecordAllFindings();
            var original = CurrentState;
            CurrentPointerClick(CurrentButton("Restart Case"));
            yield return null;
            CurrentPointerClick(CurrentButton("Cancel Restart Case"));
            Assert.That(CurrentState, Is.SameAs(original));
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator HeaderRestart_ResetsFromObserveModelsAndConclusion_OnlyAfterConfirmation()
        {
            yield return LoadCurrent();
            foreach (var checkpoint in new[] { InvestigationQaCheckpoint.ObserveReady,
                InvestigationQaCheckpoint.SimulateComplete, InvestigationQaCheckpoint.ConclusionReady })
            {
                CurrentController.ApplyQaCheckpoint(checkpoint);
                yield return null; yield return null;
                var oldState = CurrentState;
                CurrentPointerClick(CurrentButton("Restart Case"));
                yield return null;
                var staleConfirm = CurrentButton("Confirm Restart Case").onClick;
                Assert.That(CurrentState, Is.SameAs(oldState));
                CurrentPointerClick(CurrentButton("Confirm Restart Case"));
                yield return null; yield return null;
                Assert.That(CurrentState, Is.Not.SameAs(oldState));
                Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Observe));
                Assert.That(CurrentState.DiscoveredObservationIds, Is.Empty);
                Assert.That(CurrentState.TriedThreatIds, Is.Empty);
                Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
                Assert.That(GameObject.Find("Restart Confirmation"), Is.Null);
                Assert.That(GameObject.Find("Notebook Drawer"), Is.Null);
                var restarted = CurrentState;
                staleConfirm.Invoke();
                Assert.That(CurrentState, Is.SameAs(restarted));
            }
        }

        [UnityTest]
        public IEnumerator HeaderRestart_PausesPrediction_AndEscapeResumesWithoutCompletingIt()
        {
            yield return LoadCurrent();
            EnterCurrentModels();
            CurrentPress("Run Scenario longline");
            yield return new WaitForSecondsRealtime(.3f);
            CurrentPointerClick(CurrentButton("Restart Case"));
            yield return new WaitForSecondsRealtime(7.5f);
            Assert.That(GameObject.Find("Replay Scenario longline"), Is.Null,
                "Waiting in restart confirmation must not complete the prediction.");
            ExecuteEvents.Execute(CurrentButton("Cancel Restart Case").gameObject,
                new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
            yield return null;
            Assert.That(GameObject.Find("Restart Confirmation"), Is.Null);
            Assert.That(GameObject.Find("Replay Scenario longline"), Is.Null);
            CurrentPress("Finish Scenario Animation");
            Assert.That(GameObject.Find("Replay Scenario longline"), Is.Not.Null);
        }
    }
}
