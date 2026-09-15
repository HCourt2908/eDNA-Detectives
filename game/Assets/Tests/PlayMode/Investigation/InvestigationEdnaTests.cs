using TMPro;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;
using static EDNA.Investigation.Tests.InvestigationWorkbenchTestActions;
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
    public sealed class InvestigationEdnaTests
    {
        private static Button Button(string name) => GameObject.Find(name)?.GetComponent<Button>();
        private static TextMeshProUGUI Speech => GameObject.Find("Edna Speech TextMeshProUGUI")?.GetComponent<TextMeshProUGUI>();

        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene");
            yield return null;
            BeginObserveQuestions();
            yield return null;
        }


        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator Edna_FixedObserveGuideHasIntegratedPortraitWithoutRedundantDock()
        {
            yield return Load();
            Assert.That(Button("Talk To Edna"), Is.Null);
            Assert.That(Button("Dismiss Edna"), Is.Null);
            Assert.That(GameObject.Find("Edna Intro Locator"), Is.Null);
            Image introduction = GameObject.Find("Edna Introduction Portrait").GetComponent<Image>();
            Assert.That(introduction.transform.parent.name, Is.EqualTo("Observe Comparison Edna"));
            Assert.That(Bounds(introduction.rectTransform).center.x, Is.LessThan(Bounds(GameObject.Find("Observe Comparison Board").GetComponent<RectTransform>()).center.x));
            Assert.That(Button("Edna Next Step"), Is.Null);
            Assert.That(Button("Edna Why"), Is.Null);
            Assert.That(Button("Compare Species shark"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Edna_ComparisonInstructionsFitBesideThePortrait()
        {
            yield return Load();
            Canvas.ForceUpdateCanvases();
            TextMeshProUGUI message = GameObject.Find("Observe Comparison Instruction").GetComponent<TextMeshProUGUI>();
            Rect portrait = Bounds(GameObject.Find("Edna Introduction Portrait").GetComponent<RectTransform>());
            Assert.That(Bounds(message.rectTransform).xMin, Is.GreaterThan(portrait.xMax));
            Assert.That(message.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(message.preferredHeight));
            Assert.That(Button("Edna Next Step"), Is.Null);
            Assert.That(Button("Edna Why"), Is.Null);
            Assert.That(GameObject.Find("Observe Answer Choices"), Is.Null);
        }

        [UnityTest]
        public IEnumerator Edna_NewRightFacingPosesSwitchWithGuidanceWithoutMovingTheWorkbench()
        {
            yield return LoadCurrent();
            Sprite neutral = Resources.Load<Sprite>("Investigation/Edna/edna");
            Sprite speaking = Resources.Load<Sprite>("Investigation/Edna/edna-speaking");
            Assert.That(neutral, Is.Not.Null); Assert.That(speaking, Is.Not.Null);
            Image arrival = GameObject.Find("Arrival Briefing Portrait").GetComponent<Image>();
            Assert.That(arrival.sprite, Is.SameAs(speaking));
            Assert.That(arrival.raycastTarget, Is.False);
            Assert.That(arrival.rectTransform.localScale.x, Is.GreaterThan(0f), "Keep the supplied right-facing artwork unmirrored.");
            BeginObserveQuestions(); yield return null;
            Assert.That(GameObject.Find("Edna Introduction Portrait").GetComponent<Image>().sprite, Is.SameAs(neutral));
            EnterCurrentModels(); yield return null; yield return null;
            SkipCurrentGuide(); yield return null;
            var person = GameObject.Find("Scenario Briefing Portrait").GetComponent<Image>();
            Assert.That(person.sprite, Is.SameAs(neutral));
            Rect before = Bounds(GameObject.Find("Scenario Results").GetComponent<RectTransform>());
            CurrentPress("Talk To Edna"); yield return null; yield return null;
            person = GameObject.Find("Scenario Briefing Portrait").GetComponent<Image>();
            Assert.That(person.sprite, Is.SameAs(speaking));
            Assert.That(Bounds(person.rectTransform).xMax,
                Is.LessThan(Bounds(GameObject.Find("Scenario Briefing Message").GetComponent<RectTransform>()).xMin));
            Rect after = Bounds(GameObject.Find("Scenario Results").GetComponent<RectTransform>());
            Assert.That(Vector2.Distance(before.center, after.center), Is.LessThan(.5f));
            Assert.That(Vector2.Distance(before.size, after.size), Is.LessThan(.5f));
            CurrentPress("Scenario Briefing Skip"); yield return null;
            Assert.That(GameObject.Find("Scenario Briefing Portrait").GetComponent<Image>().sprite, Is.SameAs(neutral));
            Assert.That(CurrentState.TriedThreatIds, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Edna_CompactRecordingAvatarUsesTheNewNeutralFace()
        {
            yield return LoadCurrent(); BeginTodayRecording(); yield return null; yield return null;
            var avatar = GameObject.Find("Edna Introduction Portrait").GetComponent<Image>().sprite;
            var neutral = Resources.Load<Sprite>("Investigation/Edna/edna");
            Assert.That(avatar, Is.Not.Null);
            Assert.That(avatar.texture, Is.SameAs(neutral.texture));
            Assert.That(avatar.rect.height, Is.LessThan(neutral.rect.height));
            Assert.That(avatar.rect.yMax, Is.EqualTo(neutral.rect.yMax).Within(.1f));
        }

        [UnityTest]
        public IEnumerator Edna_StaysVisibleWhileNotebookPreservesPlaybackProgress()
        {
            yield return LoadCurrent(); EnterCurrentModels(); CurrentPress("Run Scenario longline");
            CurrentPress("Toggle Notebook Drawer"); yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
            Assert.That(CurrentButton("Talk To Edna").interactable, Is.False);
            Assert.That(CurrentState.HasTriedThreat("longline"), Is.True);
            CurrentPress("Close Notebook Drawer"); yield return null;
            Assert.That(CurrentButton("Talk To Edna").interactable, Is.True);
            CurrentPress("Finish Scenario Animation"); yield return null;
            Assert.That(GameObject.Find("Metrics").GetComponent<TextMeshProUGUI>().text, Does.Contain("1/3"));
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(6));
        }

    }
}
