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
    public sealed class InvestigationScenarioCardsTests
    {
        static void Press(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
        static IEnumerator Start()
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null;
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            InvestigationWorkbenchTestActions.EnterSimulate("Stage Simulate"); Press("Finish Scenario Animation"); yield return null; yield return null;
            Press("Scenario Briefing Skip"); yield return null; yield return null;
        }
        [UnityTest] public IEnumerator Cards_OnlyDedicatedPlayButtonsStartPlaybackWithoutChangingLayout()
        {
            yield return Start(); var controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(GameObject.Find("Scenario Model"), Is.Null); Assert.That(GameObject.Find("Scenario Choices"), Is.Null);
            foreach (string id in new[] { "plastic", "longline", "bottom_trawling" })
            {
                Assert.That(GameObject.Find("Scenario Cause Icon " + id), Is.Not.Null);
                Assert.That(GameObject.Find("Scenario Card Header " + id).GetComponent<Button>(), Is.Null);
                Assert.That(GameObject.Find("Scenario Result " + id).GetComponent<Button>(), Is.Null);
                Assert.That(GameObject.Find("Run Scenario " + id).GetComponentInChildren<Text>().text, Is.EqualTo("Play"));
                Assert.That(GameObject.Find("Choose Scenario " + id).GetComponent<Button>().interactable, Is.False);
            }
            var title = GameObject.Find("Scenario Card Title longline");
            ExecuteEvents.ExecuteHierarchy(title, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            Assert.That(controller.State.TriedThreatIds, Is.Empty);
            RectTransform card = GameObject.Find("Scenario Result longline").GetComponent<RectTransform>();
            Vector3 position = card.position; Vector2 size = card.rect.size;
            var button = GameObject.Find("Run Scenario longline").GetComponent<Button>();
            var pointer = new PointerEventData(EventSystem.current) { position = button.transform.position };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(button.gameObject));
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler); yield return null; yield return null;
            card = GameObject.Find("Scenario Result longline").GetComponent<RectTransform>();
            Assert.That(Vector3.Distance(position, card.position), Is.LessThan(.5f)); Assert.That(card.rect.size, Is.EqualTo(size));
            Assert.That(GameObject.Find("Scenario Model"), Is.Null);
            Assert.That(GameObject.Find("Run Scenario longline").GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("Scenario Result plastic").transform.Find("Scenario Result Species tuna/Result Prediction").GetComponent<Text>().text, Is.EqualTo("Baseline"));
            Press("Finish Scenario Animation"); yield return null; yield return null; Press("Scenario Briefing Next");
            Assert.That(GameObject.Find("Replay Scenario longline").GetComponentInChildren<Text>().text, Is.EqualTo("Replay"));
            Press("Replay Scenario longline"); yield return null;
            Assert.That(GameObject.Find("Scenario Result longline").transform.Find("Scenario Result Species tuna/Result Prediction").GetComponent<Text>().text, Does.Contain("Stable"));
        }
        [UnityTest] public IEnumerator Cards_InterruptedPredictionDoesNotRevealUnfinishedResultsOrUnlockChoice()
        {
            yield return Start(); Press("Run Scenario plastic"); yield return new WaitForSecondsRealtime(.2f);
            Press("Run Scenario longline");
            Assert.That(GameObject.Find("Scenario Result plastic").transform.Find("Scenario Result Species mussel/Result Prediction").GetComponent<Text>().text, Is.EqualTo("Baseline"));
            Press("Finish Scenario Animation"); Press("Run Scenario bottom_trawling"); Press("Finish Scenario Animation");
            Assert.That(GameObject.Find("Choose Scenario longline").GetComponent<Button>().interactable, Is.False);
            Press("Run Scenario plastic"); Press("Finish Scenario Animation");
            Assert.That(GameObject.Find("Choose Scenario longline").GetComponent<Button>().interactable, Is.True);
        }
    }
}
