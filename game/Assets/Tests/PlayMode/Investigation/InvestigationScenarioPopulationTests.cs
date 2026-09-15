using TMPro;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationScenarioPopulationTests
    {
        static readonly string[] Species = { "shark", "tuna", "krill", "atlantic_herring", "phytoplankton" };
        static void Press(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
        static Image Unit(string species, int i) => GameObject.Find("Scenario Result " + Object.FindAnyObjectByType<InvestigationController>().State.ActiveThreatId).transform.Find("Scenario Result Species " + species + "/Result Population " + species + "/Result Specimen " + i).GetComponent<Image>();
        static void AssertPopulation(string species, int expected)
        {
            int count = 0;
            for (int i = 0; i < 5; i++) if (Unit(species, i).color.a > .99f) count++;
            Assert.That(count, Is.EqualTo(expected), species + " must communicate its prediction using visible organisms");
        }
        static IEnumerator Start()
        {
            InvestigationSessionBridge.Clear(); yield return SceneManager.LoadSceneAsync("InvestigationScene"); yield return null;
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            InvestigationWorkbenchTestActions.EnterSimulate("Stage Simulate"); Press("Finish Scenario Animation"); yield return null; yield return null;
            Press("Scenario Briefing Skip"); yield return null;
        }
        [UnityTest] public IEnumerator Population_ShowsDeparturesAndArrivalsThenHoldsTheResult()
        {
            yield return Start(); Press("Run Scenario longline");
            foreach (string species in Species) AssertPopulation(species, 3);
            yield return new WaitForSecondsRealtime(1.8f);
            Assert.That(Unit("shark", 1).color.a, Is.InRange(.01f, .99f));
            Assert.That(Mathf.Abs(Unit("shark", 1).rectTransform.anchoredPosition.x), Is.GreaterThan(10f));
            Assert.That(GameObject.Find("Scenario Result longline").transform.Find("Scenario Result Species shark/Scenario Population Glow").GetComponent<Image>().color.a, Is.GreaterThan(0f));
            yield return new WaitForSecondsRealtime(4.65f);
            AssertPopulation("shark", 1); AssertPopulation("tuna", 5); AssertPopulation("krill", 5);
            AssertPopulation("atlantic_herring", 1); AssertPopulation("phytoplankton", 3);
            Assert.That(GameObject.Find("Metrics").GetComponent<TextMeshProUGUI>().text, Does.Contain("0/3"), "Final pattern stays visible before the trial completes");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(GameObject.Find("Metrics").GetComponent<TextMeshProUGUI>().text, Does.Contain("1/3"));
            Press("Scenario Briefing Next"); Press("Run Scenario plastic");
            foreach (string species in Species) AssertPopulation(species, 3);
            Press("Finish Scenario Animation");
            AssertPopulation("shark", 0); AssertPopulation("tuna", 3); AssertPopulation("krill", 1); AssertPopulation("phytoplankton", 0);
            Assert.That(Unit("shark", 0).color.a, Is.EqualTo(.25f).Within(.01f));
        }
        [UnityTest] public IEnumerator Population_ComparisonCardsKeepTheSameVisualCounts()
        {
            yield return Start();
            foreach (string id in new[] { "plastic", "longline", "bottom_trawling" }) { Press("Run Scenario " + id); Press("Finish Scenario Animation"); }
            int[][] expected = { new[] {0,3,1,0,0}, new[] {1,5,5,1,3}, new[] {1,1,0,5,3} };
            string[] causes = { "plastic", "longline", "bottom_trawling" };
            for (int cause = 0; cause < causes.Length; cause++)
            {
                Transform card = GameObject.Find("Scenario Result " + causes[cause]).transform;
                for (int species = 0; species < Species.Length; species++)
                {
                    string rowId = causes[cause] == "bottom_trawling" && Species[species] == "krill" ? "tree_bubblegum_coral" : Species[species];
                    Transform population = card.Find("Scenario Result Species " + rowId + "/Result Population " + rowId);
                    int visible = 0; foreach (var image in population.GetComponentsInChildren<Image>()) if (image.name.StartsWith("Result Specimen ") && image.color.a > .99f) visible++;
                    Assert.That(visible, Is.EqualTo(expected[cause][species]), causes[cause] + " " + Species[species]);
                }
            }
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.ComparisonRecords.Count, Is.Zero);
        }
    }
}
