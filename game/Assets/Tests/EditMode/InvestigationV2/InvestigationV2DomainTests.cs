using System;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.V2.Domain;
using NUnit.Framework;
using UnityEditor;

namespace EDNA.Investigation.V2.Tests
{
    public sealed class InvestigationV2DomainTests
    {
        private const string CasePath = "Assets/Data/InvestigationV2/LongLineCase/InvestigationCaseV2_LongLine.asset";
        private InvestigationV2CaseDefinition caseDefinition;
        private InvestigationV2StateUpdater updater;

        [SetUp]
        public void SetUp()
        {
            caseDefinition = AssetDatabase.LoadAssetAtPath<InvestigationV2CaseDefinition>(CasePath);
            Assert.That(caseDefinition, Is.Not.Null, "Run eDNA Detectives > Build Investigation V2 before tests.");
            updater = new InvestigationV2StateUpdater(caseDefinition);
        }

        [Test]
        public void CaseValidator_AcceptsCompleteEvidenceAndPredictionMatrices()
        {
            List<string> errors = new InvestigationV2CaseValidator().Validate(caseDefinition);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void EveryComparisonRule_HasADecisiveOptionThatRejectsNotEnoughEvidence()
        {
            for (int ruleIndex = 0; ruleIndex < caseDefinition.ComparisonRules.Count; ruleIndex++)
            {
                PredictionComparisonRuleDefinition rule = caseDefinition.ComparisonRules[ruleIndex];
                bool found = false;
                for (int optionIndex = 0; optionIndex < rule.ObservationOptions.Count; optionIndex++)
                {
                    JudgementResolutionDefinition resolution = rule.ObservationOptions[optionIndex]
                        .FindResolution(ComparisonJudgement.NotEnoughEvidence);
                    if (resolution != null && resolution.Outcome == ComparisonEvaluationOutcome.Incorrect) found = true;
                }
                Assert.That(found, Is.True, $"{rule.ThreatId}/{rule.SpeciesId} has no decisive observation.");
            }
        }

        [Test]
        public void ThreatMatrix_ContainsFourThreatsAndFivePredictionsEach()
        {
            Assert.That(caseDefinition.Threats, Has.Count.EqualTo(4));
            Assert.That(caseDefinition.Species, Has.Count.EqualTo(5));
            for (int index = 0; index < caseDefinition.Threats.Count; index++)
            {
                Assert.That(caseDefinition.Threats[index].SpeciesPredictions, Has.Count.EqualTo(5));
            }
            Assert.That(caseDefinition.ComparisonRules, Has.Count.EqualTo(20));
        }

        [Test]
        public void SpeciesAndThreats_UseImportedTransparentSpriteArtwork()
        {
            for (int index = 0; index < caseDefinition.Species.Count; index++)
            {
                InvestigationV2SpeciesDefinition species = caseDefinition.Species[index];
                Assert.That(species.Icon, Is.Not.Null, $"Missing sprite for species {species.SpeciesId}");
                AssertSpriteImporterUsesTransparency(species.Icon);
            }

            for (int index = 0; index < caseDefinition.Threats.Count; index++)
            {
                ThreatSimulationDefinition threat = caseDefinition.Threats[index];
                Assert.That(threat.Icon, Is.Not.Null, $"Missing sprite for threat {threat.ThreatId}");
                AssertSpriteImporterUsesTransparency(threat.Icon);
            }
        }

        [Test]
        public void LongLineAndBottomTrawling_ShareCascadeAndDifferOnBenthicPrediction()
        {
            ThreatSimulationDefinition longLine = caseDefinition.FindThreat("longline");
            ThreatSimulationDefinition trawling = caseDefinition.FindThreat("bottom_trawling");
            foreach (string speciesId in new[] { "shark", "tuna", "krill" })
            {
                Assert.That(trawling.FindPrediction(speciesId).PredictedState,
                    Is.EqualTo(longLine.FindPrediction(speciesId).PredictedState));
            }
            Assert.That(longLine.FindPrediction("sea_star").PredictedState, Is.EqualTo(PredictionState.Stable));
            Assert.That(trawling.FindPrediction("sea_star").PredictedState, Is.EqualTo(PredictionState.Decrease));
        }

        [Test]
        public void Simulator_ReturnsSpeciesAndEnvironmentalPredictions()
        {
            SimulationResult result = new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, "longline");
            Assert.That(result.Predictions, Has.Count.EqualTo(5));
            Assert.That(result.FindPrediction("shark").PredictedState, Is.EqualTo(PredictionState.Decrease));
            Assert.That(result.FindPrediction("tuna").PredictedState, Is.EqualTo(PredictionState.Increase));
            Assert.That(result.FindPrediction("krill").PredictedState, Is.EqualTo(PredictionState.Decrease));
            Assert.That(result.SeafloorPrediction, Does.Contain("intact").IgnoreCase);
            Assert.That(result.PhysicalConfirmation, Does.Contain("gear").IgnoreCase);
        }

        [Test]
        public void DirectRepeatedNonDetection_AcceptsMatchWithCaveatButRejectsNotEnoughEvidence()
        {
            InvestigationV2State matchState = CreateSimulationReadyState();
            RunModel(matchState, "longline");
            PredictionComparisonRecord match = updater.Compare(matchState, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match);

            InvestigationV2State cautiousState = CreateSimulationReadyState();
            RunModel(cautiousState, "longline");
            PredictionComparisonRecord cautious = updater.Compare(cautiousState, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.NotEnoughEvidence);
            Assert.That(match.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.AcceptedWithCaveat));
            Assert.That(cautious.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.Incorrect));
        }

        [Test]
        public void CrossSpeciesCandidate_DoesNotBecomeAFalseDirectMatch()
        {
            InvestigationV2State state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord wrong = updater.Compare(state, "longline", "shark", "E04_BENTHIC_STABLE", ComparisonJudgement.Match);
            PredictionComparisonRecord cautious = updater.Compare(state, "longline", "shark", "E04_BENTHIC_STABLE", ComparisonJudgement.NotEnoughEvidence);
            Assert.That(wrong.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.Incorrect));
            Assert.That(cautious.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.Accepted));
            Assert.That(cautious.CountsTowardProgress, Is.False);
            Assert.That(state.AcceptedComparisonCount, Is.Zero);
        }

        [Test]
        public void NotEnoughEvidence_OnUnrelatedOptions_NeverAdvancesProgressOrPassesGate()
        {
            InvestigationV2State state = CreateSimulationReadyState();
            RunModel(state, "longline");
            Assert.That(updater.Compare(state, "longline", "shark", "E04_BENTHIC_STABLE", ComparisonJudgement.NotEnoughEvidence).IsAccepted, Is.True);
            Assert.That(updater.Compare(state, "longline", "sea_star", "E03_KRILL_NONDETECTION", ComparisonJudgement.NotEnoughEvidence).IsAccepted, Is.True);
            RunModel(state, "bottom_trawling");
            Assert.That(updater.Compare(state, "bottom_trawling", "shark", "E04_BENTHIC_STABLE", ComparisonJudgement.NotEnoughEvidence).IsAccepted, Is.True);
            Assert.That(updater.Compare(state, "bottom_trawling", "sea_star", "E03_KRILL_NONDETECTION", ComparisonJudgement.NotEnoughEvidence).IsAccepted, Is.True);

            Assert.That(state.AcceptedComparisonCount, Is.Zero);
            Assert.That(state.MisstepCount, Is.Zero);
            Assert.That(updater.EvaluateReadiness(state).CanEnterProvisional, Is.False);
        }

        [Test]
        public void AcceptedComparison_IsLockedAndCannotRegressOrAddMissteps()
        {
            InvestigationV2State state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord accepted = updater.Compare(
                state,
                "longline",
                "shark",
                "E01_SHARK_NONDETECTION",
                ComparisonJudgement.Match);
            AssertAccepted(accepted);

            PredictionComparisonRecord replay = updater.Compare(
                state,
                "longline",
                "shark",
                "E04_BENTHIC_STABLE",
                ComparisonJudgement.Match);

            Assert.That(replay.IsAccepted, Is.True);
            Assert.That(replay.EvidenceId, Is.EqualTo("E01_SHARK_NONDETECTION"));
            Assert.That(state.AcceptedComparisonCount, Is.EqualTo(1));
            Assert.That(state.MisstepCount, Is.Zero);
            Assert.That(replay.Feedback, Does.Contain("locked").IgnoreCase);
        }

        [Test]
        public void SimulateStage_RequiresFourObservedFindings()
        {
            InvestigationV2State state = updater.CreateInitialState();
            Discover(state, "E01_SHARK_NONDETECTION");
            Discover(state, "E02_TUNA_WIDER_DETECTION");
            Discover(state, "E03_KRILL_NONDETECTION");
            Assert.That(updater.TrySetPhase(state, InvestigationV2Phase.Simulate, out _), Is.False);
            Discover(state, "E04_BENTHIC_STABLE");
            Assert.That(updater.TrySetPhase(state, InvestigationV2Phase.Simulate, out _), Is.True);
        }

        [Test]
        public void ProvisionalGate_RequiresBothOverlappingThreatsAndComparisons()
        {
            InvestigationV2State state = PrepareProvisionalReadyState();
            InvestigationV2Readiness readiness = updater.EvaluateReadiness(state);
            Assert.That(readiness.RequiredThreatsCompared, Is.True);
            Assert.That(readiness.MinimumComparisonsComplete, Is.True);
            Assert.That(readiness.CanEnterProvisional, Is.True);
        }

        [Test]
        public void ProvisionalGate_RejectsFourSharedComparisonsUntilSeaStarIsComparedForBothThreats()
        {
            InvestigationV2State state = CreateSimulationReadyState();
            RunModel(state, "longline");
            AssertAccepted(updater.Compare(state, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match));
            AssertAccepted(updater.Compare(state, "longline", "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match));
            RunModel(state, "bottom_trawling");
            AssertAccepted(updater.Compare(state, "bottom_trawling", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match));
            AssertAccepted(updater.Compare(state, "bottom_trawling", "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match));

            InvestigationV2Readiness sharedOnly = updater.EvaluateReadiness(state);
            Assert.That(sharedOnly.MinimumComparisonsComplete, Is.True);
            Assert.That(sharedOnly.RequiredThreatsCompared, Is.False);
            Assert.That(sharedOnly.CanEnterProvisional, Is.False);

            AssertAccepted(updater.Compare(state, "longline", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match));
            AssertAccepted(updater.Compare(state, "bottom_trawling", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch));
            Assert.That(updater.EvaluateReadiness(state).CanEnterProvisional, Is.True);
        }

        [Test]
        public void ThreatModel_CannotBypassObserveGateThroughDomainApi()
        {
            InvestigationV2State state = updater.CreateInitialState();
            Assert.That(updater.TryRunThreat(state, "longline", out _, out string blockedFeedback), Is.False);
            Assert.That(blockedFeedback, Does.Contain("Record at least"));
            Assert.That(state.HasTriedThreat("longline"), Is.False);

            state = CreateSimulationReadyState();
            Assert.That(updater.TryRunThreat(state, "longline", out SimulationResult result, out _), Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(state.HasTriedThreat("longline"), Is.True);
        }

        [Test]
        public void ConfirmationEvidence_RemainsLockedUntilProvisionalAndReview()
        {
            InvestigationV2State state = PrepareProvisionalReadyState();
            Assert.That(updater.TryDiscoverObservation(state, "E07_FISHING_LINE", out _), Is.False);
            Assert.That(state.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);
            Assert.That(updater.TrySubmitProvisional(state, "bottom_trawling", out _), Is.True);
            Assert.That(state.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            Assert.That(state.HasDiscoveredObservation("E07_FISHING_LINE"), Is.True);
            Assert.That(state.HasDiscoveredObservation("E08_SEAFLOOR_INTACT"), Is.True);
        }

        [Test]
        public void ProvisionalChoice_DoesNotSilentlyBecomeFinalChoice()
        {
            InvestigationV2State state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "bottom_trawling", out _), Is.True);
            Assert.That(state.ProvisionalThreatId, Is.EqualTo("bottom_trawling"));
            Assert.That(state.FinalThreatId, Is.Empty);
            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.False);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.True);
        }

        [Test]
        public void WarmingModel_UnlocksTemperatureObservationWithoutBecomingAReportGate()
        {
            InvestigationV2State state = updater.CreateInitialState();
            Assert.That(state.HasDiscoveredObservation("E05_TEMPERATURE_NORMAL"), Is.False);
            Discover(state, "E01_SHARK_NONDETECTION");
            Discover(state, "E02_TUNA_WIDER_DETECTION");
            Discover(state, "E03_KRILL_NONDETECTION");
            Discover(state, "E04_BENTHIC_STABLE");
            RunModel(state, "warming");
            Assert.That(state.HasDiscoveredObservation("E05_TEMPERATURE_NORMAL"), Is.True);
        }

        [Test]
        public void FinalReport_CompletesWithoutAnyFollowUpSampleState()
        {
            InvestigationV2State state = PrepareCompleteReport("longline");
            InvestigationV2ConclusionResult result = updater.SubmitFinal(state);
            Assert.That(result.Status, Is.EqualTo(InvestigationV2ConclusionStatus.Correct));
            Assert.That(state.ConclusionStatus, Is.EqualTo(InvestigationV2ConclusionStatus.Correct));
        }

        [Test]
        public void WrongFinalCause_ExplainsWhyOverlapNeedsBenthicAndRovEvidence()
        {
            InvestigationV2State state = PrepareCompleteReport("bottom_trawling");
            InvestigationV2ConclusionResult result = updater.SubmitFinal(state);
            Assert.That(result.Status, Is.EqualTo(InvestigationV2ConclusionStatus.Incorrect));
            Assert.That(result.Feedback, Does.Contain("benthic").IgnoreCase);
        }

        [Test]
        public void AlwaysAvailableMethodLimitation_PreventsConditionalEvidenceDeadlock()
        {
            InvestigationV2ObservationDefinition limitation = caseDefinition.FindObservation("L01_NONDETECTION_LIMITATION");
            Assert.That(limitation, Is.Not.Null);
            Assert.That(limitation.Source, Is.EqualTo(ObservationSource.Methodology));
            Assert.That(limitation.UnlockStage, Is.EqualTo(EvidenceUnlockStage.Always));
            Assert.That(caseDefinition.FindLimitation("L01_NONDETECTION_LIMITATION"), Is.Not.Null);
        }

        [Test]
        public void Validator_RejectsUnsupportedMultipleLimitationRequirement()
        {
            InvestigationV2CaseDefinition clone = UnityEngine.Object.Instantiate(caseDefinition);
            try
            {
                SerializedObject serialized = new SerializedObject(clone);
                serialized.FindProperty("minimumReportLimitations").intValue = 2;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                List<string> errors = new InvestigationV2CaseValidator().Validate(clone);
                Assert.That(string.Join("\n", errors), Does.Contain("minimumReportLimitations must be exactly 1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void ReasoningChoices_AreCaseDataAndRejectUnknownIds()
        {
            Assert.That(caseDefinition.ReasoningOptions, Has.Count.EqualTo(3));
            Assert.That(caseDefinition.FindReasoning(caseDefinition.RequiredReasoningId), Is.Not.Null);
            InvestigationV2State state = updater.CreateInitialState();
            Assert.That(updater.TrySetReasoning(state, "invented_reason", out _), Is.False);
            Assert.That(updater.TrySetReasoning(state, "food_web_cascade", out _), Is.True);
        }

        [Test]
        public void ExternalMiniGameInput_ImportsOnlyObserveAndAlwaysEvidence()
        {
            InvestigationV2State state = updater.CreateInitialState();
            InvestigationGameInput input = new InvestigationGameInput
            {
                caseId = caseDefinition.CaseId,
                discoveredObservationIds = new List<string>
                {
                    "E01_SHARK_NONDETECTION",
                    "E02_TUNA_WIDER_DETECTION",
                    "E05_TEMPERATURE_NORMAL",
                    "L01_NONDETECTION_LIMITATION",
                    "E07_FISHING_LINE"
                }
            };
            Assert.That(updater.TryApplyExternalInput(state, input, out _), Is.True);
            Assert.That(state.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
            Assert.That(state.HasDiscoveredObservation("E02_TUNA_WIDER_DETECTION"), Is.True);
            Assert.That(state.HasDiscoveredObservation("L01_NONDETECTION_LIMITATION"), Is.True);
            Assert.That(state.HasDiscoveredObservation("E05_TEMPERATURE_NORMAL"), Is.False);
            Assert.That(state.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);
        }

        [Test]
        public void WrongFinalReport_AddsAttemptAndMisstepBeforeCorrectRetry()
        {
            InvestigationV2State state = PrepareCompleteReport("bottom_trawling");
            InvestigationV2ConclusionResult wrong = updater.SubmitFinal(state);
            Assert.That(wrong.Status, Is.EqualTo(InvestigationV2ConclusionStatus.Incorrect));
            Assert.That(state.FinalSubmissionAttemptCount, Is.EqualTo(1));
            Assert.That(state.MisstepCount, Is.EqualTo(1));

            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.True);
            InvestigationV2ConclusionResult correct = updater.SubmitFinal(state);
            Assert.That(correct.Status, Is.EqualTo(InvestigationV2ConclusionStatus.Correct));
            Assert.That(state.FinalSubmissionAttemptCount, Is.EqualTo(2));
            Assert.That(state.MisstepCount, Is.EqualTo(1));
        }

        private InvestigationV2State PrepareCompleteReport(string finalThreatId)
        {
            InvestigationV2State state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "bottom_trawling", out _), Is.True);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            Assert.That(updater.TrySetFinalThreat(state, finalThreatId, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E01_SHARK_NONDETECTION", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E07_FISHING_LINE", true, out _), Is.True);
            Assert.That(updater.TrySetReasoning(state, "food_web_cascade", out _), Is.True);
            Assert.That(updater.TrySetLimitation(state, "L01_NONDETECTION_LIMITATION", out _), Is.True);
            return state;
        }

        private InvestigationV2State PrepareProvisionalReadyState()
        {
            InvestigationV2State state = updater.CreateInitialState();
            Discover(state, "E01_SHARK_NONDETECTION");
            Discover(state, "E02_TUNA_WIDER_DETECTION");
            Discover(state, "E03_KRILL_NONDETECTION");
            Discover(state, "E04_BENTHIC_STABLE");
            RunModel(state, "longline");
            AssertAccepted(updater.Compare(state, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match));
            AssertAccepted(updater.Compare(state, "longline", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match));
            RunModel(state, "bottom_trawling");
            AssertAccepted(updater.Compare(state, "bottom_trawling", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match));
            AssertAccepted(updater.Compare(state, "bottom_trawling", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch));
            return state;
        }

        private InvestigationV2State CreateSimulationReadyState()
        {
            InvestigationV2State state = updater.CreateInitialState();
            Discover(state, "E01_SHARK_NONDETECTION");
            Discover(state, "E02_TUNA_WIDER_DETECTION");
            Discover(state, "E03_KRILL_NONDETECTION");
            Discover(state, "E04_BENTHIC_STABLE");
            return state;
        }

        private void RunModel(InvestigationV2State state, string threatId)
        {
            Assert.That(updater.TryRunThreat(state, threatId, out SimulationResult result, out string feedback), Is.True, feedback);
            Assert.That(result, Is.Not.Null);
        }

        private void Discover(InvestigationV2State state, string evidenceId)
        {
            Assert.That(updater.TryDiscoverObservation(state, evidenceId, out string feedback), Is.True, feedback);
        }

        private static void AssertAccepted(PredictionComparisonRecord record)
        {
            Assert.That(record.CountsTowardProgress, Is.True, record.Feedback);
        }

        private static void AssertSpriteImporterUsesTransparency(UnityEngine.Sprite sprite)
        {
            string path = AssetDatabase.GetAssetPath(sprite);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
        }
    }
}
