using TMPro;
using System.Collections;
using System.Linq;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationFigmaFoodChainTests
    {
        private static int Specimens(Transform root) => root.GetComponentsInChildren<Image>(true)
            .Count(i => i.name == "Survey Specimen" || i.name == "Group Member Left" || i.name == "Group Member Right");

        [UnityTest]
        public IEnumerator FigmaSurvey_RecordsBothDatesWithActualFewerAndMoreGroups()
        {
            yield return LoadCurrent();
            InvestigationWorkbenchTestActions.BeginObserveQuestions();
            yield return null;
            Assert.That(GameObject.Find("Compare Species sea_star"), Is.Null);
            Assert.That(GameObject.Find("Compare Species mussel"), Is.Null);
            Assert.That(Specimens(GameObject.Find("Historical Notebook Row atlantic_herring").transform), Is.EqualTo(1));
            Assert.That(Specimens(GameObject.Find("Today Notebook Row atlantic_herring").transform), Is.EqualTo(3));
            Assert.That(Specimens(GameObject.Find("Historical Notebook Row phytoplankton").transform), Is.EqualTo(1));
            Assert.That(Specimens(GameObject.Find("Today Notebook Row phytoplankton").transform), Is.EqualTo(1));
            Assert.That(Specimens(GameObject.Find("Historical Notebook Row tree_bubblegum_coral").transform), Is.EqualTo(1));
            Assert.That(GameObject.Find("Today Notebook Row tree_bubblegum_coral"), Is.Null);
            CurrentPress("Compare Species atlantic_herring"); CurrentPress("Compare Change Same");
            Assert.That(CurrentState.HasDiscoveredObservation("E03_HERRING_WIDER_DETECTION"), Is.False);
            CurrentPress("Compare Change More");
            Assert.That(CurrentState.HasDiscoveredObservation("E03_HERRING_WIDER_DETECTION"), Is.True);
            CurrentPress("Compare Species tree_bubblegum_coral"); CurrentPress("Compare Change NotDetected");
            Assert.That(CurrentState.HasDiscoveredObservation("E04_CORAL_NONDETECTION"), Is.True);
        }

        [UnityTest]
        public IEnumerator FigmaConclusion_ExportsOnlyTheMatchingModelRegardlessOfReviewOrder()
        {
            foreach (string id in new[] { "longline", "bottom_trawling" })
            {
                yield return LoadCurrent(); EnterCurrentModels(); PlayAllModels();
                CurrentPress("Choose Scenario " + id); yield return null;
                Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Report));
                string words = string.Join(" ", GameObject.Find("Scenario Ending Panel").GetComponentsInChildren<TextMeshProUGUI>().Select(t => t.text));
                Assert.That(words, Does.Contain("Best fit").And.Contain("Other explanation"));
                Assert.That(words, Does.Not.Contain("independent evidence"));
                Assert.That(GameObject.Find("Scenario Chosen Explanation").GetComponent<TextMeshProUGUI>().text,
                    Does.Contain(id == "bottom_trawling" ? "MAIN EXPLANATION" : "ALTERNATIVE CHECKED"));
                Assert.That(words, Does.Not.Contain("sea star").And.Not.Contain("mussel"));
                InvestigationCurrentFlowTestActions.ReviewRemainingExplanations(); CurrentPress("Complete Scenario Investigation"); yield return null;
                var result = InvestigationSessionBridge.LastResult;
                Assert.That(result.completed, Is.True);
                Assert.That(result.selectedHypothesisId, Is.EqualTo("bottom_trawling"));
                Assert.That(result.compatibleHypothesisIds, Is.EquivalentTo(new[] { "bottom_trawling" }));
                Assert.That(result.primaryHypothesisId, Is.EqualTo("bottom_trawling"));
                Assert.That(result.alternativeHypothesisIds, Is.Empty);
                Assert.That(GameObject.Find("Scenario Closed Summary").GetComponent<TextMeshProUGUI>().text,
                    Does.Contain("Best fit").And.Contain("Long-line fishing was checked").And.Not.Contain("independent evidence"));
                Assert.That(result.evidenceIds.Count, Is.EqualTo(6));
                Assert.That(CurrentState.ConfirmationReviewed, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator FigmaNotebook_ReferenceFoodWebsDoNotInventSurveyEvidence()
        {
            yield return LoadCurrent(); EnterCurrentModels();
            var records = CurrentState.DiscoveredObservationIds.ToArray();
            CurrentPress("Toggle Notebook Drawer"); yield return null;
            CurrentNotebookScroll().verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            CurrentPress("Notebook Food Web Reference"); yield return null;
            foreach (string id in new[] { "reference_main", "manta_branch", "deep_branch", "benthic_branch" })
                Assert.That(GameObject.Find("Food Web Reference " + id), Is.Not.Null, id);
            Assert.That(CurrentState.DiscoveredObservationIds, Is.EquivalentTo(records));
            Assert.That(CurrentState.TriedThreatIds, Is.Empty);
            CurrentPress("Notebook Food Web Reference"); CurrentPress("Close Notebook Drawer");
            Assert.That(CurrentView.ContentRoot.GetComponentInParent<ScrollRect>().vertical, Is.False);
        }
    }
}
