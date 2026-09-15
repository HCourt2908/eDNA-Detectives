using TMPro;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;
namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationRevisedTrawlingTests
    {
        static Transform Row(string model, string species) => GameObject.Find("Scenario Result " + model).transform.Find("Scenario Result Species " + species);
        static int Count(string model, string species) => Row(model, species).GetComponentsInChildren<Image>()
            .Count(i => i.name.StartsWith("Result Specimen ") && i.color.a > .99f);
        static IEnumerator OpenModels()
        {
            yield return LoadCurrent(); EnterCurrentModels(); yield return null; yield return null; SkipCurrentGuide();
        }
        [UnityTest] public IEnumerator KrillIsRecordedAsMoreWithoutAReferencePlaceholder()
        {
            yield return LoadCurrent();
            InvestigationWorkbenchTestActions.BeginObserveQuestions(); yield return null;
            Assert.That(GameObject.Find("Reference Species krill"), Is.Null);
            Assert.That(GameObject.Find("Compare Species krill"), Is.Not.Null);
            foreach (string era in new[] { "Historical", "Today" })
            {
                var row = GameObject.Find(era + " Notebook Row krill");
                Assert.That(row, Is.Not.Null);
                int pictures = row.GetComponentsInChildren<Image>().Count(i => i.name == "Survey Specimen"
                    || i.name == "Group Member Left" || i.name == "Group Member Right");
                Assert.That(pictures, Is.EqualTo(era == "Historical" ? 1 : 3));
            }
            CurrentPress("Compare Species krill"); CurrentPress("Compare Change Same");
            Assert.That(CurrentState.HasDiscoveredObservation("E09_KRILL_WIDER_DETECTION"), Is.False);
            CurrentPress("Compare Change More");
            Assert.That(CurrentState.HasDiscoveredObservation("E09_KRILL_WIDER_DETECTION"), Is.True);
            EnterCurrentModels();
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(6));
            Assert.That(GameObject.Find("Scenario Observed krill"), Is.Not.Null);
        }
        [UnityTest] public IEnumerator Survey_StableProducerUsesSamePicturesAndSameAnswer()
        {
            yield return LoadCurrent(); InvestigationWorkbenchTestActions.BeginObserveQuestions(); yield return null;
            foreach (string era in new[] { "Historical", "Today" })
            {
                var row = GameObject.Find(era + " Notebook Row phytoplankton");
                Assert.That(row.GetComponentsInChildren<Image>().Count(i => i.name == "Survey Specimen"), Is.EqualTo(1));
            }
            CurrentPress("Compare Species phytoplankton"); CurrentPress("Compare Change Fewer");
            Assert.That(CurrentState.HasDiscoveredObservation("E05_PHYTOPLANKTON_STABLE"), Is.False);
            CurrentPress("Compare Change Same");
            Assert.That(CurrentState.HasDiscoveredObservation("E05_PHYTOPLANKTON_STABLE"), Is.True);
            Assert.That(GameObject.Find("Compare Species tree_bubblegum_coral"), Is.Not.Null);
        }
        [UnityTest] public IEnumerator Trawling_HasCoralLossAndDirectCatchWhileTheProducerStaysVisible()
        {
            yield return OpenModels();
            Assert.That(Row("bottom_trawling", "tree_bubblegum_coral"), Is.Not.Null);
            Assert.That(Row("bottom_trawling", "krill"), Is.Null);
            CurrentPress("Run Scenario bottom_trawling");
            Assert.That(Count("bottom_trawling", "tree_bubblegum_coral"), Is.EqualTo(3));
            yield return new WaitForSecondsRealtime(5.8f);
            Assert.That(Count("bottom_trawling", "tree_bubblegum_coral"), Is.Zero);
            Assert.That(Count("bottom_trawling", "phytoplankton"), Is.EqualTo(3));
            CurrentPress("Finish Scenario Animation"); yield return null; yield return null; SkipCurrentGuide();
            Assert.That(Count("bottom_trawling", "shark"), Is.EqualTo(1));
            Assert.That(Count("bottom_trawling", "tuna"), Is.EqualTo(1));
            Assert.That(Count("bottom_trawling", "atlantic_herring"), Is.EqualTo(5));
            CurrentPress("Detail Cause bottom_trawling");
            string body = GameObject.Find("Scenario Detail Body").GetComponent<TextMeshProUGUI>().text;
            Assert.That(body, Does.Contain("dead organic matter sinks").And.Contain("Coral spawn").And.Not.Contain("Herring eats coral"));
            CurrentPress("Close Scenario Details");
            CurrentPress("Detail Prediction bottom_trawling tree_bubblegum_coral");
            body = GameObject.Find("Scenario Detail Body").GetComponent<TextMeshProUGUI>().text;
            Assert.That(body, Does.StartWith("Not present\n\n").And.Contain("Coral not detected today"));
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(6));
            Assert.That(Object.FindObjectsByType<InvestigationBorderGraphic>().Count(b => b.name == "Scenario Detail Highlight"), Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator BothReviewsDoNotTurnTheContradictoryModelIntoTheConclusion()
        {
            foreach (string first in new[] { "longline", "bottom_trawling" })
            {
                yield return OpenModels(); PlayAllModels(); CurrentPress("Choose Scenario " + first); yield return null;
                Assert.That(GameObject.Find("Scenario Finding Summary model").transform.Find("Summary Finding").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Other explanation"));
                Assert.That(CurrentButton("Complete Scenario Investigation").interactable, Is.False);
                ReviewViaModels(first == "longline" ? "bottom_trawling" : "longline"); yield return null;
                Assert.That(GameObject.Find("Scenario Finding Summary model").transform.Find("Summary Finding").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Other explanation"));
                Assert.That(CurrentButton("Complete Scenario Investigation").interactable, Is.True);
                CurrentPress("Complete Scenario Investigation"); yield return null;
                var result = InvestigationSessionBridge.LastResult;
                Assert.That(result.selectedHypothesisId, Is.EqualTo("bottom_trawling"));
                Assert.That(result.compatibleHypothesisIds, Is.EquivalentTo(new[] { "bottom_trawling" }));
                Assert.That(result.reviewedHypothesisIds, Is.EquivalentTo(new[] { "longline", "bottom_trawling" }));
                Assert.That(result.alternativeHypothesisIds, Is.Empty);
                Assert.That(result.evidenceIds, Does.Contain("E04_CORAL_NONDETECTION"));
                Assert.That(GameObject.Find("Scenario Closed Summary").GetComponent<TextMeshProUGUI>().text, Does.Contain("do not match"));
            }
        }
    }
}
