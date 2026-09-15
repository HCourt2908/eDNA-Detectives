using TMPro;
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
    public sealed class InvestigationObserveComparisonTests
    {
        private static void Press(string name)
        {
            var button=GameObject.Find(name)?.GetComponent<Button>(); Assert.That(button, Is.Not.Null, name);
            Assert.That(button.interactable, Is.True, name); button.onClick.Invoke();
        }
        private static IEnumerator Load(bool compare=true)
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null; yield return null;
            if(compare) InvestigationWorkbenchTestActions.BeginObserveQuestions(); yield return null;
        }
        private static int Specimens(GameObject target)
        {
            Assert.That(target, Is.Not.Null); int count=0;
            foreach(var image in target.GetComponentsInChildren<Image>()) if(image.sprite!=null && image.color.a>0f) count++;
            return count;
        }
        private static void Drop(string source, string target)
        {
            var from=GameObject.Find(source); var to=GameObject.Find(target);
            Assert.That(from, Is.Not.Null, source); Assert.That(to, Is.Not.Null, target);
            var pointer=new PointerEventData(EventSystem.current){pointerDrag=from,position=new Vector2(400,300)};
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.beginDragHandler);
            Assert.That(GameObject.Find("Workbench Drag Preview"), Is.Not.Null);
            ExecuteEvents.Execute(to,pointer,ExecuteEvents.dropHandler);
            if(from!=null) ExecuteEvents.Execute(from,pointer,ExecuteEvents.endDragHandler);
            Assert.That(GameObject.Find("Workbench Drag Preview"), Is.Null);
        }
        [UnityTest] public IEnumerator Comparison_SpeciesMustBeSortedByTheNotebookEvidence()
        {
            yield return Load(); var controller=Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(GameObject.Find("Observe Comparison Board"), Is.Not.Null);
            Assert.That(GameObject.Find("Historical Record Page"), Is.Null);
            Assert.That(GameObject.Find("Today Record Page"), Is.Null);
            Press("Compare Change Fewer");
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Drop("Compare Species shark","Compare Change More");
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Assert.That(GameObject.Find("Compare Species shark"), Is.Not.Null);
            Assert.That(GameObject.Find("Observe Comparison Feedback").GetComponent<TextMeshProUGUI>().text, Does.Contain("notebook"));
            Assert.That(GameObject.Find("Observe Comparison Feedback").GetComponent<TextMeshProUGUI>().text, Does.Not.Contain("Not detected"));
            Drop("Compare Species tuna","Compare Change More");
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Drop("Compare Species shark","Compare Change Fewer");
            Drop("Compare Species phytoplankton","Compare Change Same");
            Drop("Compare Species tuna","Compare Change Fewer");
            Assert.That(GameObject.Find("Compare Species tuna"), Is.Null);
            Assert.That(GameObject.Find("Sorted Species tuna").transform.parent.name, Is.EqualTo("Compare Change Fewer"));
            Drop("Compare Species tree_bubblegum_coral","Compare Change NotDetected");
            Press("Compare Species atlantic_herring"); Press("Compare Change More");
            InvestigationWorkbenchTestActions.Record("Species Marker krill");
            Assert.That(controller.State.DiscoveredObservationIds.Count, Is.EqualTo(6));
            Assert.That(controller.State.MisstepCount, Is.Zero);
            Assert.That(GameObject.Find("Summarize Findings"), Is.Not.Null);
            Assert.That(GameObject.Find("Observe Survey Story"), Is.Null);
            Assert.That(GameObject.Find("Comparison Changes Page"), Is.Not.Null);
            InvestigationWorkbenchTestActions.EnterSimulate("Continue To Simulate");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
        }
        [UnityTest] public IEnumerator Comparison_GroupedDetectionsSurviveTheMapFlightAndBothNotebookPages()
        {
            yield return Load(false);
            Assert.That(Specimens(GameObject.Find("Species Marker tuna").transform.Find("Species Artwork").gameObject), Is.EqualTo(1));
            Assert.That(Specimens(GameObject.Find("Historical Species Marker tuna").transform.Find("Species Artwork").gameObject), Is.EqualTo(3));
            InvestigationWorkbenchTestActions.BeginTodayRecording();
            float deadline=Time.realtimeSinceStartup+4f; bool groupFlew=false;
            while(GameObject.Find("Compare With History")==null && Time.realtimeSinceStartup<deadline)
            {
                var flight=GameObject.Find("Flying Survey Species");
                if(flight!=null && Specimens(flight)==3) groupFlew=true;
                yield return null;
            }
            Assert.That(groupFlew, Is.True);
            Assert.That(Specimens(GameObject.Find("Today Notebook Icon tuna")), Is.EqualTo(1));
            InvestigationWorkbenchTestActions.BeginObserveQuestions(); yield return null;
            Assert.That(Specimens(GameObject.Find("Compare Species tuna").transform.Find("Comparison Species Artwork").gameObject), Is.EqualTo(1), "The sorting card is a neutral species token; the dated group stays in the notebook.");
            var from=GameObject.Find("Compare Species tuna");
            var pointer=new PointerEventData(EventSystem.current){pointerDrag=from,position=new Vector2(400,300)};
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.beginDragHandler);
            Assert.That(Specimens(GameObject.Find("Dragged Species Token")), Is.EqualTo(1));
            ExecuteEvents.Execute(from,pointer,ExecuteEvents.endDragHandler);
            Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null);
            Assert.That(Specimens(GameObject.Find("Today Notebook Icon tuna")), Is.EqualTo(1));
            Assert.That(Specimens(GameObject.Find("Historical Notebook Icon tuna")), Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator Comparison_NotebookStaysOpenAndPreservesReadingPositionWhenReturningFromSeamount()
        {
            yield return Load();
            Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null);
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Null);
            Assert.That(GameObject.Find("Notebook Drawer Scrim"), Is.Null);
            Assert.That(GameObject.Find("Toggle Notebook Drawer"), Is.Null);
            foreach(string id in new[]{"shark","tree_bubblegum_coral","atlantic_herring","tuna","krill"}) InvestigationWorkbenchTestActions.Record("Species Marker "+id);
            yield return null; Canvas.ForceUpdateCanvases();
            ScrollRect scroll=GameObject.Find("Comparison Notebook Scroll").GetComponent<ScrollRect>();
            // With prose removed the notes may fit without scrolling.
            float offset=Mathf.Min(30f, Mathf.Max(0f,scroll.content.rect.height-scroll.viewport.rect.height));
            scroll.content.anchoredPosition=new Vector2(scroll.content.anchoredPosition.x,offset);
            Press("Compare Species phytoplankton"); yield return null;
            Assert.That(GameObject.Find("Comparison Notebook Scroll").GetComponent<ScrollRect>().content.anchoredPosition.y, Is.EqualTo(offset).Within(.5f));
            Press("Toggle Comparison View"); yield return null;
            Assert.That(GameObject.Find("Comparison Notebook"), Is.Null);
            Assert.That(GameObject.Find("Compare Species phytoplankton"), Is.Not.Null);
            GameObject.Find("Survey Time Lens").GetComponent<Slider>().value=.5f;
            foreach(string marker in new[]{"Species Marker phytoplankton","Historical Species Marker phytoplankton"})
                Assert.That(GameObject.Find(marker).transform.Find("Paired Species Focus").gameObject.activeSelf, Is.True);
            Press("Toggle Comparison View"); yield return null;
            Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null);
            Assert.That(GameObject.Find("Comparison Notebook Scroll").GetComponent<ScrollRect>().content.anchoredPosition.y, Is.EqualTo(offset).Within(.5f));
            Assert.That(GameObject.Find("Compare Species phytoplankton").GetComponent<Button>().targetGraphic.color, Is.EqualTo((Color)InvestigationTheme.PaperSelected));
            Press("Compare Change Same");
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds.Count, Is.EqualTo(6));
        }

        [UnityTest] public IEnumerator Comparison_CompactControlsLeaveSpaceForTheNotebookAndStayClickable()
        {
            yield return Load();
            var canvas=Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>();
            var scaler=canvas.GetComponent<CanvasScaler>(); float scale=canvas.scaleFactor; bool enabled=scaler.enabled;
            try
            {
                scaler.enabled=false; canvas.scaleFactor=Screen.width/1280f;
                yield return null; yield return null; Canvas.ForceUpdateCanvases();
                var species=GameObject.Find("Comparison Species Page").GetComponent<RectTransform>();
                var changes=GameObject.Find("Comparison Changes Page").GetComponent<RectTransform>();
                var notebook=GameObject.Find("Comparison Notebook").GetComponent<RectTransform>();
                Assert.That(species.rect.width, Is.LessThanOrEqualTo(230f));
                Assert.That(changes.rect.width, Is.LessThanOrEqualTo(330f));
                Assert.That(notebook.rect.width, Is.GreaterThan(changes.rect.width));
                Assert.That(notebook.position.x, Is.GreaterThan(changes.position.x));
                foreach(string name in new[]{"Compare Species tuna","Compare Change Fewer"})
                {
                    var rect=GameObject.Find(name).GetComponent<RectTransform>();
                    var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
                    var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                    Assert.That(hits,Is.Not.Empty,name);
                    Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(rect.gameObject),name);
                }
                Assert.That(GameObject.Find("Compare Change Fewer").GetComponent<RectTransform>().rect.height, Is.LessThanOrEqualTo(100f));
                Drop("Compare Species tuna","Compare Change Fewer");
                Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null);
                canvas.scaleFactor=Screen.width/900f; yield return null; yield return null;
                Drop("Compare Species atlantic_herring","Compare Change Same");Drop("Compare Species phytoplankton","Compare Change Same");
                yield return null;Canvas.ForceUpdateCanvases();
                foreach(TextMeshProUGUI text in GameObject.Find("Observe Comparison Board").GetComponentsInChildren<TextMeshProUGUI>())
                    Assert.That(text.rectTransform.rect.height+1f,Is.GreaterThanOrEqualTo(text.preferredHeight),text.name+": "+text.text);
            }
            finally{canvas.scaleFactor=scale;scaler.enabled=enabled;}
        }
        [UnityTest] public IEnumerator Comparison_ViewSwitchOnlyReplacesTheReferencePaneAndGuidanceFollowsSelection()
        {
            yield return Load();
            var controller=Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(GameObject.Find("Comparison Historical Notes").transform.GetSiblingIndex(), Is.LessThan(GameObject.Find("Comparison Today Notes").transform.GetSiblingIndex()));
            Assert.That(GameObject.Find("Toggle Comparison View").GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("Seamount view"));
            Assert.That(GameObject.Find("Select Species Instruction").GetComponent<TextMeshProUGUI>().text, Does.Contain("Click one"));
            Assert.That(GameObject.Find("Comparison Species Page").GetComponentsInChildren<InvestigationGuidancePulse>().Length, Is.EqualTo(6));
            Assert.That(GameObject.Find("Comparison Changes Page").GetComponentsInChildren<InvestigationGuidancePulse>(), Is.Empty);
            Press("Compare Species tuna"); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(GameObject.Find("Observe Comparison Instruction").GetComponent<TextMeshProUGUI>().text, Does.Contain("Tuna selected"));
            Assert.That(GameObject.Find("Choose Change Instruction").GetComponent<TextMeshProUGUI>().text, Does.Contain("click its change"));
            Assert.That(GameObject.Find("Comparison Species Page").GetComponentsInChildren<InvestigationGuidancePulse>(), Is.Empty);
            Assert.That(GameObject.Find("Comparison Changes Page").GetComponentsInChildren<InvestigationGuidancePulse>().Length, Is.EqualTo(4));
            Assert.That(GameObject.Find("Historical Notebook Row tuna").transform.Find("Selected Record Border"), Is.Not.Null);
            Assert.That(GameObject.Find("Today Notebook Row tuna").transform.Find("Selected Record Border"), Is.Not.Null);
            Vector3 speciesPosition=GameObject.Find("Compare Species tuna").transform.position;
            Vector3 changePosition=GameObject.Find("Compare Change Fewer").transform.position;
            Vector2 changeSize=GameObject.Find("Compare Change Fewer").GetComponent<RectTransform>().rect.size;
            Press("Toggle Comparison View"); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(GameObject.Find("Comparison Seamount"), Is.Not.Null);
            Assert.That(GameObject.Find("Observe Comparison Board"), Is.Not.Null);
            Assert.That(GameObject.Find("Toggle Comparison View").GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("Notebook view"));
            Assert.That(Vector3.Distance(speciesPosition,GameObject.Find("Compare Species tuna").transform.position), Is.LessThan(.5f));
            Assert.That(Vector3.Distance(changePosition,GameObject.Find("Compare Change Fewer").transform.position), Is.LessThan(.5f));
            Assert.That(GameObject.Find("Compare Change Fewer").GetComponent<RectTransform>().rect.size, Is.EqualTo(changeSize));
            Press("Compare Change Fewer"); yield return null;
            Assert.That(controller.State.HasDiscoveredObservation("E02_TUNA_FEWER_SITES"), Is.True);
            Assert.That(GameObject.Find("Comparison Seamount"), Is.Not.Null, "Sorting must remain playable with the map open.");
            Press("Toggle Comparison View"); yield return null;
            Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null);
            Assert.That(GameObject.Find("Notebook E02_TUNA_FEWER_SITES"), Is.Null, "The reference pane no longer repeats prose findings.");
            Assert.That(GameObject.Find("Today Notebook Icon tuna"), Is.Not.Null);
        }

    }
}
