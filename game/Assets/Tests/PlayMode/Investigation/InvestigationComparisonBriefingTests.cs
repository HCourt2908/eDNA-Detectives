using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationComparisonBriefingTests
    {
        private static void Press(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
        private static string Speaker => GameObject.Find("Comparison Briefing Speaker").GetComponent<Text>().text;
        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear();yield return SceneManager.LoadSceneAsync("InvestigationScene");yield return null;yield return null;
            InvestigationWorkbenchTestActions.BeginObserveQuestions(false);yield return null;yield return new WaitForSecondsRealtime(.25f);
        }
        private static float Alpha(string name)
        {
            CanvasGroup group=GameObject.Find(name).GetComponent<CanvasGroup>();return group==null?1f:group.alpha;
        }
        private static Rect Bounds(RectTransform rect)
        {
            var points=new Vector3[4];rect.GetWorldCorners(points);return new Rect(points[0],points[2]-points[0]);
        }
        [UnityTest] public IEnumerator Briefing_RevealsNotebookSpeciesAndChangesOnlyAfterNext()
        {
            yield return Load();var controller=Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(Speaker,Does.Contain("1/3"));
            Assert.That(GameObject.Find("Comparison Notebook").transform.parent.name,Is.EqualTo("Comparison Briefing Overlay"));
            Assert.That(Alpha("Comparison Species Page"),Is.Zero);
            Assert.That(Alpha("Comparison Changes Page"),Is.Zero);
            Assert.That(GameObject.Find("Toggle Comparison View"),Is.Null);
            Assert.That(GameObject.Find("Comparison Briefing Dimmer").GetComponent<Image>().raycastTarget,Is.True);
            Press("Compare Species tuna");Press("Compare Change More");
            Assert.That(controller.State.DiscoveredObservationIds,Is.Empty,"Introduction must not record an answer.");
            var oldNext=GameObject.Find("Comparison Briefing Next").GetComponent<Button>().onClick;
            Press("Comparison Briefing Next");yield return null;
            Assert.That(Speaker,Does.Contain("2/3"));
            Assert.That(GameObject.Find("Comparison Species Page").transform.parent.name,Is.EqualTo("Comparison Briefing Overlay"));
            Assert.That(Alpha("Comparison Species Page"),Is.EqualTo(1f));
            Assert.That(Alpha("Comparison Changes Page"),Is.Zero);
            oldNext.Invoke();Assert.That(Speaker,Does.Contain("2/3"),"A queued click from the old dialogue must not skip a step.");
            Press("Comparison Briefing Next");yield return null;
            Assert.That(Speaker,Does.Contain("3/3"));
            Assert.That(GameObject.Find("Comparison Changes Page").transform.parent.name,Is.EqualTo("Comparison Briefing Overlay"));
            Assert.That(GameObject.Find("Comparison Briefing Next").GetComponentInChildren<Text>().text,Does.Contain("Start comparing"));
            Press("Comparison Briefing Next");yield return null;
            Assert.That(GameObject.Find("Comparison Briefing Overlay"),Is.Null);
            Assert.That(GameObject.Find("Comparison Notebook").transform.parent.name,Is.EqualTo("Species Sorting Columns"));
            Assert.That(Alpha("Comparison Species Page"),Is.EqualTo(1f));
            Assert.That(Alpha("Comparison Changes Page"),Is.EqualTo(1f));
            Assert.That(GameObject.Find("Investigation Content").GetComponent<ScrollRect>().enabled,Is.True);
            InvestigationCurrentFlowTestActions.ToggleGuidanceForTests();Press("Toggle Comparison View");Press("Toggle Comparison View");
            Assert.That(GameObject.Find("Comparison Briefing Overlay"),Is.Null,"Returning to the reference or changing mode must not replay the briefing.");
            Press("Compare Species tuna");Press("Compare Change More");
            Assert.That(controller.State.HasDiscoveredObservation("E02_TUNA_WIDER_DETECTION"),Is.True);
        }
        [UnityTest] public IEnumerator Briefing_CompactLayoutKeepsTheSpotlightAndNextVisibleAcrossRefreshes()
        {
            yield return Load();var view=Object.FindAnyObjectByType<InvestigationRuntimeView>();
            var canvas=view.GetComponent<Canvas>();var scaler=canvas.GetComponent<CanvasScaler>();
            float scale=canvas.scaleFactor;bool enabled=scaler.enabled;
            try
            {
                scaler.enabled=false;canvas.scaleFactor=Screen.width/720f;
                yield return null;yield return null;yield return new WaitForSecondsRealtime(.25f);
                for(int step=1;step<=3;step++)
                {
                    Canvas.ForceUpdateCanvases();
                    Assert.That(Speaker,Does.Contain(step+"/3"));
                    var overlay=GameObject.Find("Comparison Briefing Overlay").GetComponent<RectTransform>();
                    Rect screen=Bounds(overlay);
                    Rect speech=Bounds(GameObject.Find("Comparison Briefing Speech").GetComponent<RectTransform>());
                    Rect spotlight=Bounds(GameObject.Find("Comparison Briefing Spotlight").GetComponent<RectTransform>());
                    Assert.That(speech.xMin,Is.GreaterThanOrEqualTo(screen.xMin));
                    Assert.That(speech.xMax,Is.LessThanOrEqualTo(screen.xMax));
                    Assert.That(speech.yMin,Is.GreaterThanOrEqualTo(screen.yMin));
                    Assert.That(spotlight.yMin,Is.GreaterThan(speech.yMax));
                    foreach(Text text in GameObject.Find("Comparison Briefing Speech").GetComponentsInChildren<Text>())
                        Assert.That(text.rectTransform.rect.height+1f,Is.GreaterThanOrEqualTo(text.preferredHeight),text.name);
                    var button=GameObject.Find("Comparison Briefing Next").GetComponent<Button>();
                    var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,button.transform.position)};
                    var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                    Assert.That(hits,Is.Not.Empty);
                    Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(button.gameObject));
                    if(step==2)
                    {
                        view.gameObject.SetActive(false);yield return null;view.gameObject.SetActive(true);yield return null;
                        Assert.That(Speaker,Does.Contain("2/3"));
                    }
                    Press("Comparison Briefing Next");yield return null;yield return new WaitForSecondsRealtime(.25f);
                }
                Assert.That(GameObject.Find("Comparison Briefing Overlay"),Is.Null);
            }
            finally{canvas.scaleFactor=scale;scaler.enabled=enabled;}
        }
        [UnityTest] public IEnumerator Briefing_ReducedMotionAndRestartKeepExplicitProgression()
        {
            bool previous=InvestigationMotionSettings.ReducedMotion;InvestigationMotionSettings.SetReducedMotionForTests(true);
            try
            {
                yield return Load();Assert.That(Speaker,Does.Contain("1/3"));
                Assert.That(Alpha("Comparison Briefing Overlay"),Is.EqualTo(1f));
                Press("Comparison Briefing Next");Press("Comparison Briefing Next");Press("Comparison Briefing Next");
                var controller=Object.FindAnyObjectByType<InvestigationController>();
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start);
                InvestigationWorkbenchTestActions.BeginObserveQuestions(false);yield return null;
                Assert.That(Speaker,Does.Contain("1/3"));
                Assert.That(controller.State.DiscoveredObservationIds,Is.Empty);
            }
            finally{InvestigationMotionSettings.SetReducedMotionForTests(previous);}
        }
    }
}
