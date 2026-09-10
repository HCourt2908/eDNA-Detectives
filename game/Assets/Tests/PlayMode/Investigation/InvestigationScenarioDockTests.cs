using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationScenarioDockTests
    {
        static void Press(string name)
        {
            var button = GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name); Assert.That(button.interactable, Is.True, name); button.onClick.Invoke();
        }
        static IEnumerator Start()
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null;
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            InvestigationWorkbenchTestActions.EnterSimulate("Stage Simulate"); yield return null; yield return null;
        }
        static Rect Bounds(string name)
        {
            var points = new Vector3[4]; GameObject.Find(name).GetComponent<RectTransform>().GetWorldCorners(points);
            return new Rect(points[0], points[2] - points[0]);
        }
        [UnityTest] public IEnumerator Dock_UnfoldsNotebookRecordsBeforeBuildingAndStaysVisible()
        {
            yield return Start();
            Assert.That(GameObject.Find("Scenario Findings In Flight"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenario Survey Target").GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(GameObject.Find("Scenario Tools"), Is.Null);
            var book = GameObject.Find("Toggle Notebook Drawer");
            Assert.That(book.transform.parent.name, Is.EqualTo("Scenario EDNA Tools"));
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(GameObject.Find("Scenario Findings In Flight"), Is.Null);
            var visibleSheet = GameObject.Find("Scenario Survey Target");
            Assert.That(visibleSheet.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(GameObject.Find("Scenario Survey Target Title").GetComponent<Text>().text, Does.Contain("NOTEBOOK"));
            if (GameObject.Find("Finish Scenario Animation") != null) Press("Finish Scenario Animation"); yield return null; yield return null; Press("Scenario Briefing Skip");
            yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenario Briefing Overlay"), Is.Null);
            Rect model = Bounds("Scenario Results");
            Press("Toggle Notebook Drawer"); yield return null; yield return null;
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
            Press("Toggle Notebook Drawer"); yield return null; yield return null;
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Null);
            Assert.That(Vector2.Distance(model.center, Bounds("Scenario Results").center), Is.LessThan(.5f));
        }
        [UnityTest] public IEnumerator Dock_NotebookAndConclusionStayBesideEdnaWithoutWorkbenchScrolling()
        {
            yield return Start(); Press("Finish Scenario Animation");
            foreach (string id in new[] { "plastic", "longline", "bottom_trawling" }) { Press("Run Scenario " + id); Press("Finish Scenario Animation"); }
            Press("Choose Scenario longline"); yield return null; Press("Stage Simulate"); yield return null; yield return null;
            var dock = GameObject.Find("Scenario EDNA Dock").transform;
            Assert.That(GameObject.Find("Complete Scenario Investigation").transform.IsChildOf(dock), Is.True);
            Assert.That(GameObject.Find("Toggle Notebook Drawer").transform.IsChildOf(dock), Is.True);
            Rect book = Bounds("Toggle Notebook Drawer"), rov = Bounds("Complete Scenario Investigation"), person = Bounds("Scenario Briefing Portrait");
            Assert.That(book.xMax, Is.LessThan(rov.xMin)); Assert.That(person.xMax, Is.LessThan(book.xMin));
            var scroll = Object.FindAnyObjectByType<InvestigationRuntimeView>().ContentRoot.parent.parent.GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.vertical, Is.False);
            Assert.That(scroll.content.rect.height, Is.LessThanOrEqualTo(scroll.viewport.rect.height + 1f));
            yield return null;
            Assert.That(Vector2.Distance(rov.center, Bounds("Complete Scenario Investigation").center), Is.LessThan(.1f));
            Press("Complete Scenario Investigation"); yield return null;
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenario Case Closed"), Is.Not.Null);
        }
    }
}
