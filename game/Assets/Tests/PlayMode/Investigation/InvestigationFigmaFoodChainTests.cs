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
            foreach (string id in new[] { "atlantic_herring", "phytoplankton" })
            {
                Assert.That(Specimens(GameObject.Find("Historical Notebook Row " + id).transform), Is.EqualTo(3), id);
                Assert.That(Specimens(GameObject.Find("Today Notebook Row " + id).transform), Is.EqualTo(1), id);
            }
            Assert.That(Specimens(GameObject.Find("Historical Notebook Row krill").transform), Is.EqualTo(1));
            Assert.That(Specimens(GameObject.Find("Today Notebook Row krill").transform), Is.EqualTo(3));
            CurrentPress("Compare Species atlantic_herring"); CurrentPress("Compare Change Same");
            Assert.That(CurrentState.HasDiscoveredObservation("E03_HERRING_FEWER_SITES"), Is.False);
            CurrentPress("Compare Change Fewer");
            Assert.That(CurrentState.HasDiscoveredObservation("E03_HERRING_FEWER_SITES"), Is.True);
            CurrentPress("Compare Species krill"); CurrentPress("Compare Change More");
            Assert.That(CurrentState.HasDiscoveredObservation("E04_KRILL_WIDER_DETECTION"), Is.True);
        }

        [UnityTest]
        public IEnumerator FigmaConclusion_ExportsMainAndAlternativeRegardlessOfTheReviewedModel()
        {
            foreach (string id in new[] { "longline", "bottom_trawling" })
            {
                yield return LoadCurrent(); EnterCurrentModels(); PlayAllModels();
                CurrentPress("Choose Scenario " + id); yield return null;
                Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Report));
                string words = string.Join(" ", GameObject.Find("Scenario Ending Panel").GetComponentsInChildren<TextMeshProUGUI>().Select(t => t.text));
                Assert.That(words, Does.Contain("Main explanation").And.Contain("Possible alternative"));
                Assert.That(words, Does.Not.Contain("independent evidence"));
                Assert.That(GameObject.Find("Scenario Chosen Explanation").GetComponent<TextMeshProUGUI>().text,
                    Does.Contain(id == "longline" ? "MAIN EXPLANATION" : "POSSIBLE ALTERNATIVE"));
                Assert.That(words, Does.Not.Contain("sea star").And.Not.Contain("mussel"));
                InvestigationCurrentFlowTestActions.ReviewRemainingExplanations(); CurrentPress("Complete Scenario Investigation"); yield return null;
                var result = InvestigationSessionBridge.LastResult;
                Assert.That(result.completed, Is.True);
                Assert.That(result.selectedHypothesisId, Is.EqualTo(id));
                Assert.That(result.compatibleHypothesisIds, Is.EquivalentTo(new[] { "longline", "bottom_trawling" }));
                Assert.That(result.primaryHypothesisId, Is.EqualTo("longline"));
                Assert.That(result.alternativeHypothesisIds, Is.EquivalentTo(new[] { "bottom_trawling" }));
                Assert.That(GameObject.Find("Scenario Closed Summary").GetComponent<TextMeshProUGUI>().text,
                    Does.Contain("Main explanation").And.Contain("Possible alternative").And.Not.Contain("independent evidence"));
                Assert.That(result.evidenceIds.Count, Is.EqualTo(5));
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
