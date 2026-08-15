using System;
using System.Collections.Generic;
using System.Reflection;
using EDNA.Core;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationDomainTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void InitialEvidence_SeparatesNonDetectionFromLowQualityWarning()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            InvestigationState state = new InvestigationStateUpdater(caseDefinition).CreateInitialState();

            Assert.That(FindEvidence(state, EvidenceType.NotDetectedInSample), Is.Not.Null);
            Assert.That(FindEvidence(state, EvidenceType.LowQualityResult), Is.Not.Null);
            Assert.That(FindEvidence(state, EvidenceType.ContaminationWarning), Is.Not.Null);
            Assert.That(
                FindEvidence(state, EvidenceType.NotDetectedInSample).Confidence,
                Is.EqualTo(EvidenceConfidence.Low),
                "A contaminated result must not turn absence into strong evidence.");
        }

        [Test]
        public void RepeatedReliableNonDetection_UpgradesAbsenceEvidence()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase(initialQuality: SampleQuality.High, includeContamination: false);
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();

            Assert.That(updater.TryPlanSample(state, "summit", DepthBand.Shallow, "warming", string.Empty, out InvestigationSamplePlan plan, out _), Is.True);
            Assert.That(updater.ApplyResult(state, Result(plan, "warm", "stable"), out _), Is.True);

            EvidenceRecord evidence = FindEvidence(state, EvidenceType.RepeatedNonDetection);
            Assert.That(evidence, Is.Not.Null);
            Assert.That(evidence.Confidence, Is.EqualTo(EvidenceConfidence.High));
        }

        [Test]
        public void DeepRedetection_AfterShallowAbsence_CreatesDepthShift()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();

            Assert.That(updater.TryPlanSample(state, "summit", DepthBand.Deep, "warming", string.Empty, out InvestigationSamplePlan plan, out _), Is.True);
            Assert.That(updater.ApplyResult(state, Result(plan, "cold", "stable"), out _), Is.True);

            EvidenceRecord evidence = FindEvidence(state, EvidenceType.DepthShift);
            Assert.That(evidence, Is.Not.Null);
            Assert.That(evidence.Confidence, Is.EqualTo(EvidenceConfidence.High));
        }

        [Test]
        public void DuplicateResult_IsRejectedWithoutChangingState()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();
            Assert.That(updater.TryPlanSample(state, "summit", DepthBand.Deep, "warming", string.Empty, out InvestigationSamplePlan plan, out _), Is.True);
            EDNAResultData result = Result(plan, "cold");

            Assert.That(updater.ApplyResult(state, result, out _), Is.True);
            int countAfterFirstResult = state.AllResults.Count;

            Assert.That(updater.ApplyResult(state, result, out string error), Is.False);
            Assert.That(error, Does.Contain("already"));
            Assert.That(state.AllResults.Count, Is.EqualTo(countAfterFirstResult));
        }

        [Test]
        public void FollowUpSampleLimit_IsEnforcedByStateOwner()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase(followUpLimit: 1);
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();

            Assert.That(updater.TryPlanSample(state, "summit", DepthBand.Deep, "warming", string.Empty, out _, out _), Is.True);
            Assert.That(updater.TryPlanSample(state, "summit", DepthBand.Shallow, "warming", string.Empty, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("No follow-up samples"));
            Assert.That(state.RemainingSamples, Is.EqualTo(1), "Planning reserves a slot but does not consume it.");
            Assert.That(state.AvailableSampleSlots, Is.Zero);
            Assert.That(state.PendingSampleCount, Is.EqualTo(1));
            Assert.That(state.CompletedSampleCount, Is.Zero);
        }

        [Test]
        public void FailedResult_CanReleaseReservationWithoutConsumingSample()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase(followUpLimit: 1);
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();

            Assert.That(updater.TryPlanSample(state, "summit", DepthBand.Deep, "warming", string.Empty, out InvestigationSamplePlan plan, out _), Is.True);
            Assert.That(updater.ApplyResult(state, null, out string resultError), Is.False);
            Assert.That(resultError, Does.Contain("missing"));
            Assert.That(updater.TryCancelPlannedSample(state, plan.Request.requestId, out _), Is.True);

            Assert.That(state.RemainingSamples, Is.EqualTo(1));
            Assert.That(state.AvailableSampleSlots, Is.EqualTo(1));
            Assert.That(state.PendingSampleCount, Is.Zero);
            Assert.That(state.CompletedSampleCount, Is.Zero);
        }

        [Test]
        public void SuccessfulResult_ConsumesReservedSampleAndMarksItCompleted()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase(followUpLimit: 1);
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();

            Assert.That(updater.TryPlanSample(state, "summit", DepthBand.Deep, "warming", string.Empty, out InvestigationSamplePlan plan, out _), Is.True);
            Assert.That(state.RemainingSamples, Is.EqualTo(1));
            Assert.That(updater.ApplyResult(state, Result(plan, "cold", "stable"), out _), Is.True);

            Assert.That(state.RemainingSamples, Is.Zero);
            Assert.That(state.AvailableSampleSlots, Is.Zero);
            Assert.That(state.PendingSampleCount, Is.Zero);
            Assert.That(state.CompletedSampleCount, Is.EqualTo(1));
            Assert.That(state.IsSampleCompleted(plan.Request.requestId), Is.True);
        }

        [Test]
        public void WrongAnomalyClassification_IsRecordedAndRuledOutWithoutUnlockingEvidence()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();
            EvidenceRecord newDetection = FindEvidence(state, EvidenceType.NewDetection);

            Assert.That(
                updater.TryIdentifyAnomaly(
                    state,
                    newDetection.EvidenceId,
                    AnomalyClaimType.MatchesBaseline,
                    out string feedback),
                Is.False);
            Assert.That(feedback, Does.Contain("does not match"));
            Assert.That(state.IsEvidenceIdentified(newDetection.EvidenceId), Is.False);
            Assert.That(state.MisclassificationCount, Is.EqualTo(1));
            Assert.That(
                state.HasRejectedClassification(newDetection.EvidenceId, AnomalyClaimType.MatchesBaseline),
                Is.True);

            Assert.That(
                updater.TryIdentifyAnomaly(
                    state,
                    newDetection.EvidenceId,
                    AnomalyClaimType.MatchesBaseline,
                    out string repeatedFeedback),
                Is.False);
            Assert.That(repeatedFeedback, Does.Contain("already ruled out"));
            Assert.That(state.MisclassificationCount, Is.EqualTo(1), "Repeating the same rejected option must not inflate the review count.");
        }

        [Test]
        public void CorrectAnomalyClassification_UnlocksEvidence()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();
            EvidenceRecord missing = FindEvidence(state, EvidenceType.NotDetectedInSample);

            Assert.That(
                updater.TryIdentifyAnomaly(
                    state,
                    missing.EvidenceId,
                    AnomalyClaimType.ExpectedButMissing,
                    out string feedback),
                Is.True);
            Assert.That(feedback, Does.Contain("Expected but Missing"));
            Assert.That(state.IsEvidenceIdentified(missing.EvidenceId), Is.True);
            Assert.That(state.GetIdentifiedEvidence(), Has.Count.EqualTo(1));
            Assert.That(state.MisclassificationCount, Is.Zero);
        }

        [Test]
        public void UnidentifiedEvidence_CannotBeAssignedToHypothesis()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();
            EvidenceRecord newDetection = FindEvidence(state, EvidenceType.NewDetection);

            Assert.That(
                updater.TryAssignEvidence(
                    state,
                    newDetection.EvidenceId,
                    "warming",
                    EvidenceAssignmentKind.Supports,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain("Identify"));
        }

        [Test]
        public void KeyOpposingEvidence_PreventsSupportedStatus()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase(contradictingTag: EvidenceType.ContaminationWarning.ToString());
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();
            EvidenceRecord newDetection = FindEvidence(state, EvidenceType.NewDetection);
            EvidenceRecord contamination = FindEvidence(state, EvidenceType.ContaminationWarning);

            Identify(updater, state, newDetection, AnomalyClaimType.NewArrival);
            Identify(updater, state, contamination, AnomalyClaimType.ResultWarning);
            Assert.That(updater.TryAssignEvidence(state, newDetection.EvidenceId, "warming", EvidenceAssignmentKind.Supports, out _), Is.True);
            Assert.That(updater.TryAssignEvidence(state, contamination.EvidenceId, "warming", EvidenceAssignmentKind.Opposes, out _), Is.True);

            Assert.That(updater.EvaluateHypothesis(state, "warming").Status, Is.EqualTo(HypothesisStatus.Contradicted));
        }

        [Test]
        public void NewResult_AllowsExistingHypothesisToRecalculateToSupported()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();
            EvidenceRecord newDetection = FindEvidence(state, EvidenceType.NewDetection);
            Identify(updater, state, newDetection, AnomalyClaimType.NewArrival);
            updater.TryAssignEvidence(state, newDetection.EvidenceId, "warming", EvidenceAssignmentKind.Supports, out _);

            Assert.That(updater.EvaluateHypothesis(state, "warming").Status, Is.EqualTo(HypothesisStatus.Plausible));

            updater.TryPlanSample(state, "summit", DepthBand.Deep, "warming", newDetection.EvidenceId, out InvestigationSamplePlan plan, out _);
            updater.ApplyResult(state, Result(plan, "cold", "stable"), out _);
            EvidenceRecord depthShift = FindEvidence(state, EvidenceType.DepthShift);
            Identify(updater, state, depthShift, AnomalyClaimType.DifferentDepth);
            updater.TryAssignEvidence(state, depthShift.EvidenceId, "warming", EvidenceAssignmentKind.Supports, out _);

            Assert.That(updater.EvaluateHypothesis(state, "warming").Status, Is.EqualTo(HypothesisStatus.Supported));
        }

        [Test]
        public void Conclusion_RequiresFollowUpSupportAndUncertainty_ThenSucceeds()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();

            Assert.That(updater.SubmitConclusion(state).Status, Is.EqualTo(ConclusionStatus.InsufficientEvidence));

            EvidenceRecord newDetection = FindEvidence(state, EvidenceType.NewDetection);
            EvidenceRecord uncertainty = FindEvidence(state, EvidenceType.ContaminationWarning);
            Identify(updater, state, newDetection, AnomalyClaimType.NewArrival);
            Identify(updater, state, uncertainty, AnomalyClaimType.ResultWarning);
            updater.TryPlanSample(state, "summit", DepthBand.Deep, "warming", newDetection.EvidenceId, out InvestigationSamplePlan plan, out _);
            updater.ApplyResult(state, Result(plan, "cold", "stable"), out _);
            EvidenceRecord depthShift = FindEvidence(state, EvidenceType.DepthShift);
            Identify(updater, state, depthShift, AnomalyClaimType.DifferentDepth);
            updater.TryAssignEvidence(state, newDetection.EvidenceId, "warming", EvidenceAssignmentKind.Supports, out _);
            updater.TryAssignEvidence(state, depthShift.EvidenceId, "warming", EvidenceAssignmentKind.Supports, out _);
            updater.TryAssignEvidence(state, uncertainty.EvidenceId, "warming", EvidenceAssignmentKind.Opposes, out _);
            updater.TrySelectHypothesis(state, "warming", out _);

            ConclusionResult result = updater.SubmitConclusion(state);
            Assert.That(result.Status, Is.EqualTo(ConclusionStatus.Correct));
            Assert.That(state.ConclusionStatus, Is.EqualTo(ConclusionStatus.Correct));
        }

        [Test]
        public void Conclusion_DoesNotCountAPlannedSampleUntilItsResultArrives()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase(initialQuality: SampleQuality.High, includeContamination: false);
            HypothesisDefinition hypothesis = caseDefinition.FindHypothesis("warming");
            SetField(hypothesis, "requiredEvidenceTags", new List<string> { EvidenceType.NewDetection.ToString() });
            SetField(hypothesis, "minimumSupportingEvidence", 1);
            SetField(hypothesis, "minimumConfidence", EvidenceConfidence.Low);
            SetField(caseDefinition, "requiredOpposingEvidence", 0);

            InvestigationStateUpdater updater = new InvestigationStateUpdater(caseDefinition);
            InvestigationState state = updater.CreateInitialState();
            EvidenceRecord newDetection = FindEvidence(state, EvidenceType.NewDetection);
            Identify(updater, state, newDetection, AnomalyClaimType.NewArrival);
            Assert.That(updater.TryAssignEvidence(state, newDetection.EvidenceId, "warming", EvidenceAssignmentKind.Supports, out _), Is.True);
            Assert.That(updater.TrySelectHypothesis(state, "warming", out _), Is.True);
            Assert.That(updater.EvaluateHypothesis(state, "warming").Status, Is.EqualTo(HypothesisStatus.Supported));
            Assert.That(updater.TryPlanSample(state, "summit", DepthBand.Deep, "warming", newDetection.EvidenceId, out _, out _), Is.True);

            ConclusionResult result = updater.SubmitConclusion(state);

            Assert.That(result.Status, Is.EqualTo(ConclusionStatus.InsufficientEvidence));
            Assert.That(result.Feedback, Does.Contain("complete"));
            Assert.That(state.CompletedSampleCount, Is.Zero);
        }

        [Test]
        public void CaseValidator_FindsUnknownBaselineSpeciesAndDuplicateMockOutcome()
        {
            InvestigationCaseDefinition caseDefinition = CreateCase();
            List<HistoricalRecordDefinition> baseline = GetField<List<HistoricalRecordDefinition>>(caseDefinition, "historicalBaseline");
            baseline.Add(new HistoricalRecordDefinition
            {
                speciesId = "unknown",
                siteId = "summit",
                depthBand = DepthBand.Shallow,
                expectedPresence = true
            });
            List<MockSampleOutcomeDefinition> outcomes = GetField<List<MockSampleOutcomeDefinition>>(caseDefinition, "mockSampleOutcomes");
            outcomes.Add(outcomes[0]);

            List<string> errors = new InvestigationCaseValidator().Validate(caseDefinition);

            Assert.That(errors, Has.Some.Contains("unknown species"));
            Assert.That(errors, Has.Some.Contains("Duplicate mock outcome"));
        }

        private InvestigationCaseDefinition CreateCase(
            int followUpLimit = 2,
            SampleQuality initialQuality = SampleQuality.Low,
            bool includeContamination = true,
            string contradictingTag = "")
        {
            SpeciesDefinition cold = CreateAsset<SpeciesDefinition>();
            SetField(cold, "speciesId", "cold");
            SetField(cold, "displayName", "Cold Fish");
            SetField(cold, "sensitivityTags", new List<string> { "ColdSensitive" });

            SpeciesDefinition warm = CreateAsset<SpeciesDefinition>();
            SetField(warm, "speciesId", "warm");
            SetField(warm, "displayName", "Warm Fish");
            SetField(warm, "sensitivityTags", new List<string> { "WarmWater" });

            SpeciesDefinition stable = CreateAsset<SpeciesDefinition>();
            SetField(stable, "speciesId", "stable");
            SetField(stable, "displayName", "Stable Species");

            SampleSiteDefinition site = CreateAsset<SampleSiteDefinition>();
            SetField(site, "siteId", "summit");
            SetField(site, "displayName", "Summit");
            SetField(site, "availableDepths", new List<DepthBand> { DepthBand.Shallow, DepthBand.Deep });

            HypothesisDefinition hypothesis = CreateAsset<HypothesisDefinition>();
            SetField(hypothesis, "hypothesisId", "warming");
            SetField(hypothesis, "displayName", "Warming Shift");
            SetField(
                hypothesis,
                "requiredEvidenceTags",
                new List<string> { EvidenceType.NewDetection.ToString(), EvidenceType.DepthShift.ToString() });
            SetField(
                hypothesis,
                "contradictingEvidenceTags",
                string.IsNullOrEmpty(contradictingTag)
                    ? new List<string>()
                    : new List<string> { contradictingTag });
            SetField(hypothesis, "minimumConfidence", EvidenceConfidence.Medium);
            SetField(hypothesis, "minimumSupportingEvidence", 2);

            List<EDNAResultFlag> initialFlags = includeContamination
                ? new List<EDNAResultFlag> { EDNAResultFlag.LowQuality, EDNAResultFlag.ContaminationWarning }
                : new List<EDNAResultFlag>();
            EDNAResultData initial = new EDNAResultData
            {
                sampleId = "initial_shallow",
                siteId = "summit",
                depthBand = DepthBand.Shallow,
                roundIndex = 0,
                sampleQuality = initialQuality,
                detectedSpeciesIds = new List<string> { "warm", "stable" },
                resultFlags = initialFlags
            };

            EDNAResultData deepTemplate = new EDNAResultData
            {
                siteId = "summit",
                depthBand = DepthBand.Deep,
                sampleQuality = SampleQuality.High,
                detectedSpeciesIds = new List<string> { "cold", "stable" },
                resultFlags = new List<EDNAResultFlag>()
            };

            InvestigationCaseDefinition caseDefinition = CreateAsset<InvestigationCaseDefinition>();
            SetField(caseDefinition, "caseId", "test_case");
            SetField(caseDefinition, "species", new List<SpeciesDefinition> { cold, warm, stable });
            SetField(caseDefinition, "sampleSites", new List<SampleSiteDefinition> { site });
            SetField(caseDefinition, "hypotheses", new List<HypothesisDefinition> { hypothesis });
            SetField(
                caseDefinition,
                "historicalBaseline",
                new List<HistoricalRecordDefinition>
                {
                    new HistoricalRecordDefinition { speciesId = "cold", siteId = "summit", depthBand = DepthBand.Shallow },
                    new HistoricalRecordDefinition { speciesId = "stable", siteId = "summit", depthBand = DepthBand.Shallow }
                });
            SetField(caseDefinition, "initialResults", new List<EDNAResultData> { initial });
            SetField(
                caseDefinition,
                "mockSampleOutcomes",
                new List<MockSampleOutcomeDefinition>
                {
                    new MockSampleOutcomeDefinition
                    {
                        siteId = "summit",
                        depthBand = DepthBand.Deep,
                        resultTemplate = deepTemplate
                    }
                });
            SetField(caseDefinition, "followUpSampleLimit", followUpLimit);
            SetField(caseDefinition, "correctHypothesisId", "warming");
            SetField(caseDefinition, "requireFollowUpSample", true);
            SetField(caseDefinition, "requiredOpposingEvidence", 1);
            return caseDefinition;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }

        private static EDNAResultData Result(InvestigationSamplePlan plan, params string[] speciesIds)
        {
            return new EDNAResultData
            {
                requestId = plan.Request.requestId,
                sampleId = $"sample_{plan.RoundIndex}",
                siteId = plan.Request.siteId,
                depthBand = plan.Request.depthBand,
                roundIndex = plan.RoundIndex,
                sampleQuality = SampleQuality.High,
                detectedSpeciesIds = new List<string>(speciesIds),
                resultFlags = new List<EDNAResultFlag>()
            };
        }

        private static EvidenceRecord FindEvidence(InvestigationState state, EvidenceType type)
        {
            for (int index = 0; index < state.UnlockedEvidence.Count; index++)
            {
                if (state.UnlockedEvidence[index].EvidenceType == type)
                {
                    return state.UnlockedEvidence[index];
                }
            }

            return null;
        }

        private static void Identify(
            InvestigationStateUpdater updater,
            InvestigationState state,
            EvidenceRecord evidence,
            AnomalyClaimType claimType)
        {
            Assert.That(evidence, Is.Not.Null);
            Assert.That(updater.TryIdentifyAnomaly(state, evidence.EvidenceId, claimType, out string feedback), Is.True, feedback);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing test setup field: {fieldName}");
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing test setup field: {fieldName}");
            return (T)field.GetValue(target);
        }
    }
}
