using System;
using System.Collections.Generic;
using System.IO;
using EDNA.Core;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationDomainTests
    {
        private const string CasePath = "Assets/Data/Investigation/LongLineCase/InvestigationCase_LongLine.asset";
        private InvestigationCaseDefinition caseDefinition;
        private InvestigationStateUpdater updater;

        [SetUp]
        public void SetUp()
        {
            caseDefinition = AssetDatabase.LoadAssetAtPath<InvestigationCaseDefinition>(CasePath);
            Assert.That(caseDefinition, Is.Not.Null, "Run eDNA Detectives > Build Investigation before tests.");
            updater = new InvestigationStateUpdater(caseDefinition);
        }

        [Test]
        public void CaseValidator_AcceptsCompleteEvidenceAndPredictionMatrices()
        {
            List<string> errors = new InvestigationCaseValidator().Validate(caseDefinition);
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
            Assert.That(caseDefinition.ComparisonRules, Has.Count.EqualTo(21));
            Assert.That(caseDefinition.InvestigationObjectives, Has.Count.EqualTo(8));
        }

        [Test]
        public void SpeciesMapLayout_DeterministicallyScattersTwentySpeciesWithinDepthBands()
        {
            string seed = "investigation_longline_01|survey_12|seamount_a";
            int[] counts = { 7, 7, 6 };
            DepthBand[] bands = { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep };
            int total = 0;
            bool foundDifferentCasePlacement = false;
            for (int bandIndex = 0; bandIndex < bands.Length; bandIndex++)
            {
                DepthBand band = bands[bandIndex];
                List<string> speciesIds = new List<string>();
                for (int index = 0; index < counts[bandIndex]; index++)
                    speciesIds.Add($"species_{band}_{index}");
                speciesIds.Sort((left, right) => InvestigationSpeciesMapLayout
                    .StableOrder(seed, left, band, false)
                    .CompareTo(InvestigationSpeciesMapLayout.StableOrder(seed, right, band, false)));

                List<Vector2> positions = new List<Vector2>();
                for (int index = 0; index < speciesIds.Count; index++)
                {
                    InvestigationSpeciesMapPlacement first = InvestigationSpeciesMapLayout.Calculate(
                        seed, speciesIds[index], band, false, SurveyEra.Current, index, speciesIds.Count);
                    InvestigationSpeciesMapPlacement repeated = InvestigationSpeciesMapLayout.Calculate(
                        seed, speciesIds[index], band, false, SurveyEra.Current, index, speciesIds.Count);
                    InvestigationSpeciesMapPlacement historical = InvestigationSpeciesMapLayout.Calculate(
                        seed, speciesIds[index], band, false, SurveyEra.Historical, index, speciesIds.Count);
                    InvestigationSpeciesMapPlacement anotherCase = InvestigationSpeciesMapLayout.Calculate(
                        seed + "|new-case", speciesIds[index], band, false, SurveyEra.Current, index, speciesIds.Count);
                    Assert.That(repeated.Anchor, Is.EqualTo(first.Anchor));
                    Assert.That(first.Anchor.x, Is.InRange(0.10f, 0.90f));
                    Assert.That(first.Anchor.y, Is.InRange(0.19f, 0.97f));
                    Assert.That(first.MarkerSize.x, Is.GreaterThanOrEqualTo(74f));
                    Assert.That(first.MarkerSize.y, Is.GreaterThanOrEqualTo(62f));
                    Assert.That(Vector2.Distance(first.Anchor, historical.Anchor), Is.LessThan(0.08f));
                    if (Vector2.Distance(first.Anchor, anotherCase.Anchor) > 0.01f)
                        foundDifferentCasePlacement = true;
                    positions.Add(first.Anchor);
                    total++;
                }
                for (int first = 0; first < positions.Count; first++)
                    for (int second = first + 1; second < positions.Count; second++)
                        Assert.That(Vector2.Distance(positions[first], positions[second]), Is.GreaterThan(0.035f));
            }
            Assert.That(total, Is.EqualTo(20));
            Assert.That(foundDifferentCasePlacement, Is.True,
                "A new case seed should produce a visibly different arrangement while preserving the same constraints.");
        }

        [Test]
        public void ExternalEdnaResults_AddOnlyKnownDetectedSpeciesToSurveyRoster()
        {
            InvestigationCaseDefinition clone = UnityEngine.Object.Instantiate(caseDefinition);
            InvestigationSpeciesDefinition backgroundSpecies = ScriptableObject.CreateInstance<InvestigationSpeciesDefinition>();
            try
            {
                SerializedObject speciesObject = new SerializedObject(backgroundSpecies);
                speciesObject.FindProperty("speciesId").stringValue = "background_jelly";
                speciesObject.FindProperty("displayName").stringValue = "Background jelly";
                SerializedProperty depths = speciesObject.FindProperty("preferredDepths");
                depths.arraySize = 1;
                depths.GetArrayElementAtIndex(0).enumValueIndex = (int)DepthBand.Mid;
                speciesObject.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject caseObject = new SerializedObject(clone);
                SerializedProperty species = caseObject.FindProperty("species");
                int originalCount = species.arraySize;
                species.arraySize++;
                species.GetArrayElementAtIndex(originalCount).objectReferenceValue = backgroundSpecies;
                caseObject.ApplyModifiedPropertiesWithoutUndo();

                InvestigationStateUpdater cloneUpdater = new InvestigationStateUpdater(clone);
                InvestigationState state = cloneUpdater.CreateInitialState();
                Assert.That(state.HasSurveySpecies("background_jelly"), Is.False);
                InvestigationGameInput input = new InvestigationGameInput();
                EDNAResultData result = new EDNAResultData();
                result.detectedSpeciesIds.Add("background_jelly");
                result.detectedSpeciesIds.Add("unknown_species");
                input.ednaResults.Add(result);
                Assert.That(cloneUpdater.TryApplyExternalInput(state, input, out string feedback), Is.True);
                Assert.That(state.HasSurveySpecies("background_jelly"), Is.True);
                Assert.That(state.HasSurveySpecies("unknown_species"), Is.False);
                Assert.That(feedback, Does.Contain("1 additional detected species"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(backgroundSpecies);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void SpeciesAndThreats_UseImportedTransparentSpriteArtwork()
        {
            for (int index = 0; index < caseDefinition.Species.Count; index++)
            {
                InvestigationSpeciesDefinition species = caseDefinition.Species[index];
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
        public void SeamountSprite_IsSingleAngleCompressedAndWithinSourceBudget()
        {
            const string path = "Assets/Art/Investigation/Seamount/seamount_hero.png";
            UnityEngine.Sprite sprite = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.rect.width, Is.EqualTo(600f).Within(0.1f));
            Assert.That(sprite.rect.height, Is.EqualTo(434f).Within(0.1f));

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.wrapMode, Is.EqualTo(UnityEngine.TextureWrapMode.Clamp));
            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            Assert.That(platform.maxTextureSize, Is.EqualTo(1024));
            Assert.That(platform.textureCompression, Is.EqualTo(TextureImporterCompression.CompressedHQ));
            Assert.That(platform.compressionQuality, Is.EqualTo(100));

            string absolutePath = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, path);
            Assert.That(new FileInfo(absolutePath).Length, Is.LessThan(150 * 1024));
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
            InvestigationState matchState = CreateSimulationReadyState();
            RunModel(matchState, "longline");
            PredictionComparisonRecord match = updater.Compare(matchState, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match);

            InvestigationState cautiousState = CreateSimulationReadyState();
            RunModel(cautiousState, "longline");
            PredictionComparisonRecord cautious = updater.Compare(cautiousState, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.NotEnoughEvidence);
            Assert.That(match.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.AcceptedWithCaveat));
            Assert.That(cautious.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.Incorrect));
        }

        [Test]
        public void CrossSpeciesCandidate_DoesNotBecomeAFalseDirectMatch()
        {
            InvestigationState state = CreateSimulationReadyState();
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
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "longline");
            Assert.That(updater.Compare(state, "longline", "shark", "E04_BENTHIC_STABLE", ComparisonJudgement.NotEnoughEvidence).IsAccepted, Is.True);
            RunModel(state, "bottom_trawling");
            Assert.That(updater.Compare(state, "bottom_trawling", "shark", "E04_BENTHIC_STABLE", ComparisonJudgement.NotEnoughEvidence).IsAccepted, Is.True);

            Assert.That(state.AcceptedComparisonCount, Is.Zero);
            Assert.That(state.MisstepCount, Is.Zero);
            Assert.That(updater.EvaluateReadiness(state).CanEnterProvisional, Is.False);
        }

        [Test]
        public void AcceptedNotEnoughEvidence_DoesNotLockAndCanBeReplaced()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord cautious = updater.Compare(
                state,
                "longline",
                "shark",
                "E04_BENTHIC_STABLE",
                ComparisonJudgement.NotEnoughEvidence);
            Assert.That(cautious.IsAccepted, Is.True);
            Assert.That(cautious.LocksComparison, Is.False);
            Assert.That(cautious.CompletesObjective, Is.False);

            PredictionComparisonRecord decisive = updater.Compare(
                state,
                "longline",
                "shark",
                "E01_SHARK_NONDETECTION",
                ComparisonJudgement.Match);
            Assert.That(decisive.LocksComparison, Is.True, decisive.Feedback);
            Assert.That(decisive.CompletesObjective, Is.True, decisive.Feedback);
            Assert.That(state.FindComparison("longline", "shark").EvidenceId, Is.EqualTo("E01_SHARK_NONDETECTION"));
        }

        [Test]
        public void AcceptedContextOnly_LocksButDoesNotCompleteObjective()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord context = updater.Compare(
                state,
                "longline",
                "mussel",
                "E06_PLASTIC_INDICATOR_STABLE",
                ComparisonJudgement.Match);
            Assert.That(context.IsAccepted, Is.True);
            Assert.That(context.LocksComparison, Is.True);
            Assert.That(context.CompletesObjective, Is.False);
            Assert.That(context.ProgressRole, Is.EqualTo(ComparisonProgressRole.ContextOnly));

            PredictionComparisonRecord replay = updater.Compare(
                state,
                "longline",
                "mussel",
                "E06_PLASTIC_INDICATOR_STABLE",
                ComparisonJudgement.Mismatch);
            Assert.That(replay.Judgement, Is.EqualTo(ComparisonJudgement.Match));
            Assert.That(replay.Feedback, Does.Contain("locked").IgnoreCase);
        }

        [Test]
        public void AcceptedComparison_IsLockedAndCannotRegressOrAddMissteps()
        {
            InvestigationState state = CreateSimulationReadyState();
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
        public void SimulateStage_RequiresAllFiveObservedFindings()
        {
            InvestigationState state = updater.CreateInitialState();
            Discover(state, "E01_SHARK_NONDETECTION");
            Discover(state, "E02_TUNA_WIDER_DETECTION");
            Discover(state, "E03_KRILL_NONDETECTION");
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);
            Discover(state, "E04_BENTHIC_STABLE");
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);
            Discover(state, "E06_PLASTIC_INDICATOR_STABLE");
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.True);
        }

        [Test]
        public void MissingKrillEvidence_IsReportedByObjectiveReadiness()
        {
            InvestigationCaseDefinition clone = UnityEngine.Object.Instantiate(caseDefinition);
            try
            {
                SerializedObject serialized = new SerializedObject(clone);
                serialized.FindProperty("minimumObserveDiscoveries").intValue = 4;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                InvestigationStateUpdater cloneUpdater = new InvestigationStateUpdater(clone);
                InvestigationState state = cloneUpdater.CreateInitialState();
                DiscoverWith(cloneUpdater, state, "E01_SHARK_NONDETECTION");
                DiscoverWith(cloneUpdater, state, "E02_TUNA_WIDER_DETECTION");
                DiscoverWith(cloneUpdater, state, "E04_BENTHIC_STABLE");
                DiscoverWith(cloneUpdater, state, "E06_PLASTIC_INDICATOR_STABLE");
                Assert.That(cloneUpdater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.True);
                RunWith(cloneUpdater, state, "warming");
                Assert.That(cloneUpdater.Compare(state, "warming", PredictionTargetKind.Temperature, "temperature", "E05_TEMPERATURE_NORMAL", ComparisonJudgement.Mismatch).CompletesObjective, Is.True);
                RunWith(cloneUpdater, state, "plastic");
                Assert.That(cloneUpdater.Compare(state, "plastic", "mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch).CompletesObjective, Is.True);
                RunWith(cloneUpdater, state, "longline");
                Assert.That(cloneUpdater.Compare(state, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match).CompletesObjective, Is.True);
                Assert.That(cloneUpdater.Compare(state, "longline", "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match).CompletesObjective, Is.True);

                InvestigationReadiness readiness = cloneUpdater.EvaluateReadiness(state);
                Assert.That(readiness.CanEnterProvisional, Is.False);
                Assert.That(readiness.MissingObjectiveId, Is.EqualTo("longline_krill"));
                Assert.That(readiness.MissingEvidenceId, Is.EqualTo("E03_KRILL_NONDETECTION"));
                Assert.That(cloneUpdater.TrySetPhase(state, InvestigationPhase.Observe, out _), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void ProvisionalGate_RequiresBothOverlappingThreatsAndComparisons()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            InvestigationReadiness readiness = updater.EvaluateReadiness(state);
            Assert.That(readiness.RequiredThreatsCompared, Is.True);
            Assert.That(readiness.MinimumComparisonsComplete, Is.True);
            Assert.That(readiness.CanEnterProvisional, Is.True);
            Assert.That(readiness.RequiredObjectivesComplete, Is.False,
                "The Sea-star discriminator remains deliberately locked until after the ROV follow-up.");
        }

        [Test]
        public void ProvisionalGate_RejectsSeaStarAndMusselOnlyShortcut()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord lockedLongline = updater.Compare(state, "longline", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match);
            Assert.That(lockedLongline.IsAccepted, Is.False);
            Assert.That(lockedLongline.Feedback, Does.Contain("ROV").IgnoreCase);
            PredictionComparisonRecord longlineMussel = updater.Compare(state, "longline", "mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Match);
            Assert.That(longlineMussel.LocksComparison, Is.True);
            Assert.That(longlineMussel.CompletesObjective, Is.False);
            RunModel(state, "bottom_trawling");
            PredictionComparisonRecord lockedTrawl = updater.Compare(state, "bottom_trawling", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch);
            Assert.That(lockedTrawl.IsAccepted, Is.False);
            PredictionComparisonRecord trawlMussel = updater.Compare(state, "bottom_trawling", "mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Match);
            Assert.That(trawlMussel.LocksComparison, Is.True);
            Assert.That(trawlMussel.CompletesObjective, Is.False);
            Assert.That(updater.EvaluateReadiness(state).CanEnterProvisional, Is.False);
            Assert.That(state.MisstepCount, Is.Zero, "Trying a follow-up-locked comparison must not count as a scientific mistake.");
        }

        [Test]
        public void ThreatModel_CannotBypassObserveGateThroughDomainApi()
        {
            InvestigationState state = updater.CreateInitialState();
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
            InvestigationState state = PrepareProvisionalReadyState();
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
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "bottom_trawling", out _), Is.True);
            Assert.That(state.ProvisionalThreatId, Is.EqualTo("bottom_trawling"));
            Assert.That(state.FinalThreatId, Is.Empty);
            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.False);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.True);
        }

        [Test]
        public void WarmingModel_UnlocksTemperatureObjectiveEvidence()
        {
            InvestigationState state = updater.CreateInitialState();
            Assert.That(state.HasDiscoveredObservation("E05_TEMPERATURE_NORMAL"), Is.False);
            DiscoverCoreObservations(state);
            RunModel(state, "warming");
            Assert.That(state.HasDiscoveredObservation("E05_TEMPERATURE_NORMAL"), Is.True);
            PredictionComparisonRecord comparison = updater.Compare(
                state,
                "warming",
                PredictionTargetKind.Temperature,
                "temperature",
                "E05_TEMPERATURE_NORMAL",
                ComparisonJudgement.Mismatch);
            Assert.That(comparison.CompletesObjective, Is.True, comparison.Feedback);
        }

        [Test]
        public void FinalReport_CompletesWithoutAnyFollowUpSampleState()
        {
            InvestigationState state = PrepareCompleteReport("longline");
            InvestigationConclusionResult result = updater.SubmitFinal(state);
            Assert.That(result.Status, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(state.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
        }

        [Test]
        public void IncompleteFinalReport_ReturnsSpecificFeedbackWithoutPenaltyAndEditsRestoreDraftState()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "bottom_trawling", out _), Is.True);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);

            InvestigationConclusionResult incomplete = updater.SubmitFinal(state);
            Assert.That(incomplete.Status, Is.EqualTo(InvestigationConclusionStatus.InsufficientEvidence));
            Assert.That(incomplete.Feedback, Does.Contain("Return to Simulate"));
            Assert.That(state.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(state.MisstepCount, Is.Zero);

            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.True);
            Assert.That(state.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.NotSubmitted));
        }

        [Test]
        public void WrongFinalCause_ExplainsWhyOverlapNeedsBenthicAndRovEvidence()
        {
            InvestigationState state = PrepareCompleteReport("bottom_trawling");
            InvestigationConclusionResult result = updater.SubmitFinal(state);
            Assert.That(result.Status, Is.EqualTo(InvestigationConclusionStatus.Incorrect));
            Assert.That(result.Feedback, Does.Contain("benthic").IgnoreCase);
        }

        [Test]
        public void AlwaysAvailableMethodLimitation_PreventsConditionalEvidenceDeadlock()
        {
            InvestigationObservationDefinition limitation = caseDefinition.FindObservation("L01_NONDETECTION_LIMITATION");
            Assert.That(limitation, Is.Not.Null);
            Assert.That(limitation.Source, Is.EqualTo(ObservationSource.Methodology));
            Assert.That(limitation.UnlockStage, Is.EqualTo(EvidenceUnlockStage.Always));
            Assert.That(caseDefinition.FindLimitation("L01_NONDETECTION_LIMITATION"), Is.Not.Null);
        }

        [Test]
        public void Validator_RejectsUnsupportedMultipleLimitationRequirement()
        {
            InvestigationCaseDefinition clone = UnityEngine.Object.Instantiate(caseDefinition);
            try
            {
                SerializedObject serialized = new SerializedObject(clone);
                serialized.FindProperty("minimumReportLimitations").intValue = 2;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                List<string> errors = new InvestigationCaseValidator().Validate(clone);
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
            InvestigationState state = updater.CreateInitialState();
            Assert.That(updater.TrySetReasoning(state, "invented_reason", out _), Is.False);
            Assert.That(updater.TrySetReasoning(state, "food_web_cascade", out _), Is.True);
        }

        [Test]
        public void ExternalMiniGameInput_ImportsOnlyObserveAndAlwaysEvidence()
        {
            InvestigationState state = updater.CreateInitialState();
            InvestigationGameInput input = new InvestigationGameInput
            {
                caseId = caseDefinition.CaseId,
                surveyContext = new InvestigationSurveyContextData
                {
                    surveyId = "upstream_survey_42",
                    surveyDisplayName = "Survey 42",
                    siteId = "waypoint_c",
                    siteDisplayName = "Waypoint C",
                    processedSampleSummary = "Processed samples from three depth bands"
                },
                discoveredObservationIds = new List<string>
                {
                    "E01_SHARK_NONDETECTION",
                    "E02_TUNA_WIDER_DETECTION",
                    "E05_TEMPERATURE_NORMAL",
                    "L01_NONDETECTION_LIMITATION",
                    "E07_FISHING_LINE"
                },
                environmentalObservations = new List<InvestigationExternalObservationData>
                {
                    new InvestigationExternalObservationData { observationId = "E05_TEMPERATURE_NORMAL" },
                    new InvestigationExternalObservationData { observationId = "E01_SHARK_NONDETECTION" }
                },
                physicalObservations = new List<InvestigationExternalObservationData>
                {
                    new InvestigationExternalObservationData { observationId = "E07_FISHING_LINE" }
                }
            };
            Assert.That(updater.TryApplyExternalInput(state, input, out _), Is.True);
            Assert.That(state.SurveyId, Is.EqualTo("upstream_survey_42"));
            Assert.That(state.SurveyDisplayName, Is.EqualTo("Survey 42"));
            Assert.That(state.SiteId, Is.EqualTo("waypoint_c"));
            Assert.That(state.SiteDisplayName, Is.EqualTo("Waypoint C"));
            Assert.That(state.ProcessedSampleSummary, Is.EqualTo("Processed samples from three depth bands"));
            Assert.That(state.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
            Assert.That(state.HasDiscoveredObservation("E02_TUNA_WIDER_DETECTION"), Is.True);
            Assert.That(state.HasDiscoveredObservation("L01_NONDETECTION_LIMITATION"), Is.True);
            Assert.That(state.HasDiscoveredObservation("E05_TEMPERATURE_NORMAL"), Is.False);
            Assert.That(state.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);
        }

        [Test]
        public void Report_RequiresFoodWebBenthicAndRovEvidenceCategories()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "longline", out _), Is.True);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            CompleteFollowUpObjectives(state);
            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.True);
            Assert.That(updater.TrySetReasoning(state, "food_web_cascade", out _), Is.True);
            Assert.That(updater.TrySetLimitation(state, "L01_NONDETECTION_LIMITATION", out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E01_SHARK_NONDETECTION", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E02_TUNA_WIDER_DETECTION", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E07_FISHING_LINE", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E08_SEAFLOOR_INTACT", true, out _), Is.True);

            InvestigationReadiness missingBenthic = updater.EvaluateReadiness(state);
            Assert.That(missingBenthic.EvidenceComplete, Is.True);
            Assert.That(missingBenthic.EvidenceCategoriesComplete, Is.False);
            Assert.That(missingBenthic.MissingEvidenceCategory, Is.EqualTo(EvidenceCategory.Benthic));
            Assert.That(missingBenthic.CanSubmitFinal, Is.False);

            Assert.That(updater.TrySetReportEvidence(state, "E04_BENTHIC_STABLE", true, out _), Is.True);
            Assert.That(updater.EvaluateReadiness(state).CanSubmitFinal, Is.True);
        }

        [Test]
        public void QaCheckpoint_ReportReady_MatchesManualRouteSnapshot()
        {
            InvestigationState manual = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(manual, "longline", out _), Is.True);
            InvestigationState checkpoint = InvestigationQaStateFactory.Create(
                caseDefinition,
                InvestigationQaCheckpoint.ReportReady);
            Assert.That(CanonicalSnapshot(checkpoint), Is.EqualTo(CanonicalSnapshot(manual)));
            Assert.That(updater.EvaluateReadiness(checkpoint).CanEnterProvisional, Is.True);
            Assert.That(new InvestigationCaseValidator().Validate(caseDefinition), Is.Empty);
        }

        [Test]
        public void QaCheckpoint_FinalReady_MatchesManualRouteSnapshot()
        {
            InvestigationState manual = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(manual, "longline", out _), Is.True);
            Assert.That(updater.TryReviewConfirmation(manual, out _), Is.True);
            CompleteFollowUpObjectives(manual);
            Assert.That(updater.TrySetFinalThreat(manual, "longline", out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(manual, "E01_SHARK_NONDETECTION", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(manual, "E02_TUNA_WIDER_DETECTION", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(manual, "E04_BENTHIC_STABLE", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(manual, "E07_FISHING_LINE", true, out _), Is.True);
            Assert.That(updater.TrySetReasoning(manual, "food_web_cascade", out _), Is.True);
            Assert.That(updater.TrySetLimitation(manual, "L01_NONDETECTION_LIMITATION", out _), Is.True);

            InvestigationState checkpoint = InvestigationQaStateFactory.Create(
                caseDefinition,
                InvestigationQaCheckpoint.FinalReportReady);
            Assert.That(CanonicalSnapshot(checkpoint), Is.EqualTo(CanonicalSnapshot(manual)));
            Assert.That(updater.EvaluateReadiness(checkpoint).CanSubmitFinal, Is.True);
        }

        [Test]
        public void WrongFinalReport_AddsAttemptAndMisstepBeforeCorrectRetry()
        {
            InvestigationState state = PrepareCompleteReport("bottom_trawling");
            InvestigationConclusionResult wrong = updater.SubmitFinal(state);
            Assert.That(wrong.Status, Is.EqualTo(InvestigationConclusionStatus.Incorrect));
            Assert.That(state.FinalSubmissionAttemptCount, Is.EqualTo(1));
            Assert.That(state.MisstepCount, Is.EqualTo(1));

            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.True);
            Assert.That(state.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.NotSubmitted));
            InvestigationConclusionResult correct = updater.SubmitFinal(state);
            Assert.That(correct.Status, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(state.FinalSubmissionAttemptCount, Is.EqualTo(2));
            Assert.That(state.MisstepCount, Is.EqualTo(1));
        }

        private InvestigationState PrepareCompleteReport(string finalThreatId)
        {
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "bottom_trawling", out _), Is.True);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            CompleteFollowUpObjectives(state);
            Assert.That(updater.TrySetFinalThreat(state, finalThreatId, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E01_SHARK_NONDETECTION", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E02_TUNA_WIDER_DETECTION", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E04_BENTHIC_STABLE", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E07_FISHING_LINE", true, out _), Is.True);
            Assert.That(updater.TrySetReasoning(state, "food_web_cascade", out _), Is.True);
            Assert.That(updater.TrySetLimitation(state, "L01_NONDETECTION_LIMITATION", out _), Is.True);
            return state;
        }

        private InvestigationState PrepareProvisionalReadyState()
        {
            InvestigationState state = updater.CreateInitialState();
            DiscoverCoreObservations(state);
            RunModel(state, "warming");
            AssertObjective(updater.Compare(state, "warming", PredictionTargetKind.Temperature, "temperature", "E05_TEMPERATURE_NORMAL", ComparisonJudgement.Mismatch));
            RunModel(state, "plastic");
            AssertObjective(updater.Compare(state, "plastic", "mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch));
            RunModel(state, "longline");
            AssertObjective(updater.Compare(state, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match));
            AssertObjective(updater.Compare(state, "longline", "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match));
            AssertObjective(updater.Compare(state, "longline", "krill", "E03_KRILL_NONDETECTION", ComparisonJudgement.Match));
            RunModel(state, "bottom_trawling");
            AssertObjective(updater.Compare(state, "bottom_trawling", "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match));
            return state;
        }

        private void CompleteFollowUpObjectives(InvestigationState state)
        {
            Assert.That(state.ConfirmationReviewed, Is.True);
            AssertObjective(updater.Compare(state, "longline", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match));
            AssertObjective(updater.Compare(state, "bottom_trawling", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch));
        }

        private InvestigationState CreateSimulationReadyState()
        {
            InvestigationState state = updater.CreateInitialState();
            DiscoverCoreObservations(state);
            return state;
        }

        private void DiscoverCoreObservations(InvestigationState state)
        {
            Discover(state, "E01_SHARK_NONDETECTION");
            Discover(state, "E02_TUNA_WIDER_DETECTION");
            Discover(state, "E03_KRILL_NONDETECTION");
            Discover(state, "E04_BENTHIC_STABLE");
            Discover(state, "E06_PLASTIC_INDICATOR_STABLE");
        }

        private void RunModel(InvestigationState state, string threatId)
        {
            Assert.That(updater.TryRunThreat(state, threatId, out SimulationResult result, out string feedback), Is.True, feedback);
            Assert.That(result, Is.Not.Null);
        }

        private void Discover(InvestigationState state, string evidenceId)
        {
            Assert.That(updater.TryDiscoverObservation(state, evidenceId, out string feedback), Is.True, feedback);
        }

        private static void DiscoverWith(
            InvestigationStateUpdater targetUpdater,
            InvestigationState state,
            string evidenceId)
        {
            Assert.That(targetUpdater.TryDiscoverObservation(state, evidenceId, out string feedback), Is.True, feedback);
        }

        private static void RunWith(
            InvestigationStateUpdater targetUpdater,
            InvestigationState state,
            string threatId)
        {
            Assert.That(targetUpdater.TryRunThreat(state, threatId, out SimulationResult result, out string feedback), Is.True, feedback);
            Assert.That(result, Is.Not.Null);
        }

        private static void AssertAccepted(PredictionComparisonRecord record)
        {
            Assert.That(record.LocksComparison, Is.True, record.Feedback);
        }

        private static void AssertObjective(PredictionComparisonRecord record)
        {
            Assert.That(record.CompletesObjective, Is.True, record.Feedback);
        }

        private static string CanonicalSnapshot(InvestigationState state)
        {
            List<string> observations = new List<string>(state.DiscoveredObservationIds);
            observations.Sort(StringComparer.Ordinal);
            List<string> threats = new List<string>(state.TriedThreatIds);
            threats.Sort(StringComparer.Ordinal);
            List<string> comparisons = new List<string>();
            for (int index = 0; index < state.ComparisonRecords.Count; index++)
            {
                PredictionComparisonRecord record = state.ComparisonRecords[index];
                comparisons.Add($"{record.ThreatId}/{record.TargetKind}/{record.TargetId}/{record.EvidenceId}/{record.Judgement}/{record.Outcome}/{record.ObjectiveId}");
            }
            comparisons.Sort(StringComparer.Ordinal);
            List<string> evidence = new List<string>(state.SelectedReportEvidenceIds);
            evidence.Sort(StringComparer.Ordinal);
            return string.Join("|", new[]
            {
                state.Phase.ToString(),
                string.Join(",", observations),
                string.Join(",", threats),
                string.Join(",", comparisons),
                state.ProvisionalThreatId,
                state.ConfirmationReviewed.ToString(),
                state.FinalThreatId,
                string.Join(",", evidence),
                state.SelectedReasoningId,
                state.SelectedLimitationId,
                state.MisstepCount.ToString(),
                state.FinalSubmissionAttemptCount.ToString()
            });
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
