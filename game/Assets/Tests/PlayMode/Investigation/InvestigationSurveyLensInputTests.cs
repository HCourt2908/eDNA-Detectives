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
    public sealed class InvestigationSurveyLensInputTests
    {
        private static Button Button(string name) => GameObject.Find(name)?.GetComponent<Button>();
        private static void Press(string name) { if (name == "Toggle Survey Map" && GameObject.Find(name) == null) name = "Toggle Comparison View"; var b = Button(name); Assert.That(b, Is.Not.Null, name); Assert.That(b.interactable, Is.True, name); b.onClick.Invoke(); }
        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null; yield return null;
            InvestigationWorkbenchTestActions.BeginObserveQuestions();
            yield return null;
        }


        [UnityTest]
        public IEnumerator SurveyLens_SliderReceivesRealPointerInput()
        {
            yield return Load();
            Press("Toggle Survey Map"); yield return null;
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

    }
}
