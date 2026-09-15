using System;
using System.Collections;
using EDNA.Core;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationCompletionSignalTests
    {
        [Test]
        public void CompletionSignal_StoresTheResultAndIgnoresIncompleteOrRepeatedResults()
        {
            InvestigationSessionBridge.Clear();
            int signals = 0;
            Action<InvestigationGameResult> onComplete = result =>
            {
                signals++;
                Assert.That(InvestigationSessionBridge.IsComplete, Is.True);
                Assert.That(InvestigationSessionBridge.LastResult, Is.SameAs(result));
            };
            InvestigationSessionBridge.GameCompleted += onComplete;
            try
            {
                InvestigationSessionBridge.PublishResult(new InvestigationGameResult { completed = false });
                Assert.That(signals, Is.Zero);
                Assert.That(InvestigationSessionBridge.IsComplete, Is.False);
                var result = new InvestigationGameResult { completed = true, correct = true };
                InvestigationSessionBridge.PublishResult(result);
                InvestigationSessionBridge.PublishResult(result);
                Assert.That(signals, Is.EqualTo(1));
                // A screen enabled after the event can still read completion.
                Assert.That(InvestigationSessionBridge.LastResult.completed, Is.True);
                InvestigationSessionBridge.ClearResult();
                Assert.That(InvestigationSessionBridge.IsComplete, Is.False);
                InvestigationSessionBridge.PublishResult(new InvestigationGameResult { completed = true });
                Assert.That(signals, Is.EqualTo(2));
            }
            finally
            {
                InvestigationSessionBridge.GameCompleted -= onComplete;
                InvestigationSessionBridge.Clear();
            }
        }

        [UnityTest]
        public IEnumerator PlayerCompletion_EmitsAfterTheEndingIsReadyAndCanBeReplayed()
        {
            yield return LoadCurrent();
            int signals = 0;
            Action<InvestigationGameResult> onComplete = result =>
            {
                signals++;
                Assert.That(result.completed, Is.True);
                Assert.That(CurrentState.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
                Assert.That(GameObject.Find("Scenario Case Closed"), Is.Not.Null);
            };
            InvestigationSessionBridge.GameCompleted += onComplete;
            try
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    EnterCurrentModels(); OpenCurrentSummary();
                    Assert.That(signals, Is.EqualTo(attempt));
                    var click = CurrentButton("Complete Scenario Investigation").onClick;
                    click.Invoke(); click.Invoke();
                    Assert.That(signals, Is.EqualTo(attempt + 1));
                    Assert.That(InvestigationSessionBridge.IsComplete, Is.True);
                    CurrentController.RestartInvestigation();
                    Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Observe));
                    Assert.That(CurrentState.DiscoveredObservationIds, Is.Empty);
                    Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
                    Assert.That(InvestigationSessionBridge.IsComplete, Is.False);
                    yield return null;
                }
                CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.CaseClosed);
                Assert.That(signals, Is.EqualTo(3));
            }
            finally { InvestigationSessionBridge.GameCompleted -= onComplete; }
        }
    }
}
