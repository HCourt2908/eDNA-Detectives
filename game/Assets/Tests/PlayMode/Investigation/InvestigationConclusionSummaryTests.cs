using TMPro;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;
using static EDNA.Investigation.Tests.InvestigationWorkbenchTestActions;
using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationConclusionSummaryTests
    {
        private static TextMeshProUGUI Speech => GameObject.Find("Report Edna Speech").GetComponent<TextMeshProUGUI>();
        private static Button Find(string name) => GameObject.Find(name)?.GetComponent<Button>();

        [UnityTest]
        public IEnumerator ConclusionSummary_ReplayDoesNotSilentlyChangeTheSelectedCause()
        {
            yield return LoadCurrent(); EnterCurrentModels(); OpenCurrentSummary(); yield return null;
            Assert.That(CurrentState.ProvisionalThreatId, Is.EqualTo("longline"));
            CurrentPress("Scenario Compare Again"); CurrentPress("Replay Scenario plastic"); CurrentPress("Finish Scenario Animation");
            CurrentPress("Return To Scenario Report"); yield return null; yield return null;
            Assert.That(CurrentState.ProvisionalThreatId, Is.EqualTo("longline"));
            Assert.That(GameObject.Find("Scenario Chosen Explanation").GetComponent<TextMeshProUGUI>().text, Does.Contain("Long-line fishing"));
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(6));
            Assert.That(CurrentState.ConfirmationReviewed, Is.False);
            Assert.That(CurrentState.FinalSubmissionAttemptCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ConclusionSummary_TextAndControlsFitAcrossCanvasWidths()
        {
            yield return LoadCurrent(); CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.ConclusionReady);
            var canvas = CurrentView.GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            bool enabled = scaler.enabled; float original = canvas.scaleFactor;
            try
            {
                scaler.enabled = false;
                foreach (float width in new[] { 720f, 1000f, 1280f })
                {
                    canvas.scaleFactor = Screen.width / width; yield return null; yield return null; Canvas.ForceUpdateCanvases();
                    foreach (string panel in new[] { "Scenario Ending Panel", "Scenario EDNA Dock" })
                        foreach (TextMeshProUGUI text in GameObject.Find(panel).GetComponentsInChildren<TextMeshProUGUI>())
                            Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name);
                    Assert.That(CurrentButton("Complete Scenario Investigation").interactable, Is.True);
                    Assert.That(CurrentView.ContentRoot.GetComponentInParent<ScrollRect>().vertical, Is.False);
                }
            }
            finally { canvas.scaleFactor = original; scaler.enabled = enabled; }
        }


    }
}
