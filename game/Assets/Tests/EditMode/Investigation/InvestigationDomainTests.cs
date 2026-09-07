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
        public void ThreatMatrix_ContainsThreeThreatsAndFivePredictionsEach()
        {
            Assert.That(caseDefinition.Threats, Has.Count.EqualTo(3));
            Assert.That(caseDefinition.Species, Has.Count.EqualTo(5));
            for (int index = 0; index < caseDefinition.Threats.Count; index++)
            {
                Assert.That(caseDefinition.Threats[index].SpeciesPredictions, Has.Count.EqualTo(5));
            }
            Assert.That(caseDefinition.ComparisonRules, Has.Count.EqualTo(15));
            Assert.That(caseDefinition.InvestigationObjectives, Has.Count.EqualTo(7));
            Assert.That(caseDefinition.FindSpecies("shark").MapDepthBand, Is.EqualTo(DepthBand.Shallow));
            Assert.That(caseDefinition.FindSpecies("tuna").MapDepthBand, Is.EqualTo(DepthBand.Shallow));
            Assert.That(caseDefinition.FindSpecies("krill").MapDepthBand, Is.EqualTo(DepthBand.Mid));
            Assert.That(caseDefinition.FindSpecies("sea_star").MapDepthBand, Is.EqualTo(DepthBand.Deep));
            Assert.That(caseDefinition.FindSpecies("mussel").MapDepthBand, Is.EqualTo(DepthBand.Deep));
        }

        [Test]
        public void SharedSpeciesCatalog_ContainsCanonicalRosterAndAuthoredPresentationGroups()
        {
            Assert.That(caseDefinition.SpeciesCatalog, Has.Count.EqualTo(20));
            HashSet<string> canonicalIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.SpeciesCatalog.Count; index++)
            {
                InvestigationSpeciesDefinition species = caseDefinition.SpeciesCatalog[index];
                Assert.That(species, Is.Not.Null);
                Assert.That(species.CanonicalSpeciesId, Is.Not.Empty);
                Assert.That(species.ScientificName, Is.Not.Empty);
                Assert.That(canonicalIds.Add(species.CanonicalSpeciesId), Is.True, species.CanonicalSpeciesId);
            }

            Assert.That(canonicalIds, Is.EquivalentTo(new[]
            {
                "green_sea_urchin", "reef_manta_ray", "kitefin_shark", "great_hammerhead_shark",
                "orange_roughy", "pinecone_fish", "atlantic_bluefin_tuna", "atlantic_herring",
                "spotted_lanternfish", "northern_krill", "king_crab", "warty_squid",
                "flapjack_octopus", "giant_pacific_octopus", "bone_eating_worm",
                "tree_bubblegum_coral", "precious_coral", "zigzag_coral", "moon_jellyfish",
                "phytoplankton"
            }));

            Assert.That(caseDefinition.FindSpecies("great_hammerhead_shark"), Is.SameAs(caseDefinition.FindSpecies("shark")));
            Assert.That(caseDefinition.FindSpecies("sphyrna_mokarran"), Is.SameAs(caseDefinition.FindSpecies("shark")));
            Assert.That(caseDefinition.FindSpecies("shark").DisplayName, Is.EqualTo("Great Hammerhead Shark"));
            Assert.That(caseDefinition.FindSpecies("shark").GameplayName, Is.EqualTo("Shark"));
            Assert.That(caseDefinition.FindSpecies("moon_jellyfish").Icon, Is.Null);
            Assert.That(caseDefinition.FoodWebChainSpeciesIds, Is.EqualTo(new[] { "shark", "tuna", "krill" }));
            Assert.That(caseDefinition.SimulationFoodWebId, Is.EqualTo("case_simplified"));
            Assert.That(caseDefinition.FoodWebEdges, Has.Count.EqualTo(12));
            Assert.That(caseDefinition.BenthicIndicatorSpeciesIds, Is.EqualTo(new[] { "sea_star", "mussel" }));
            Assert.That(caseDefinition.FollowUpLockedSpeciesIds, Is.EqualTo(new[] { "sea_star" }));
            Assert.That(caseDefinition.MaximumSurveySpecies, Is.EqualTo(7));
        }

        [Test]
        public void ExternalEdnaResults_NormalizeCanonicalAliasesToCaseSpeciesIds()
        {
            InvestigationState state = updater.CreateInitialState();
            InvestigationGameInput input = new InvestigationGameInput();
            EDNAResultData result = new EDNAResultData();
            result.speciesObservations.Add(new EDNASpeciesObservationData
            {
                speciesId = "great_hammerhead_shark", detectionState = SpeciesDetectionState.NotDetected
            });
            result.detectedSpeciesIds.Add("moon_jellyfish");
            input.ednaResults.Add(result);

            Assert.That(updater.TryApplyExternalInput(state, input, out _), Is.True);
            Assert.That(state.HasSurveySpecies("shark"), Is.True);
            Assert.That(state.HasSurveySpecies("great_hammerhead_shark"), Is.False);
            Assert.That(state.HasSurveySpecies("moon_jellyfish"), Is.True);
        }

        [Test]
        public void DetailedEdnaResults_BuildACappedDepthAwareRoster()
        {
            InvestigationState state = updater.CreateInitialState();
            InvestigationGameInput input = new InvestigationGameInput();
            EDNAResultData result = new EDNAResultData { sampleId = "sample-7", siteId = "ridge", sampleQuality = SampleQuality.High };
            result.speciesObservations.Add(new EDNASpeciesObservationData
            {
                speciesId = "atlantic_herring",
                depthBand = DepthBand.Mid,
                surveyTimepoint = SurveyTimepoint.Current,
                detectionState = SpeciesDetectionState.Detected,
                confidence = SurveyConfidence.High
            });
            result.speciesObservations.Add(new EDNASpeciesObservationData
            {
                speciesId = "tree_bubblegum_coral",
                depthBand = DepthBand.Deep,
                surveyTimepoint = SurveyTimepoint.Current,
                detectionState = SpeciesDetectionState.NotDetected,
                confidence = SurveyConfidence.High
            });
            result.speciesObservations.Add(new EDNASpeciesObservationData
            {
                speciesId = "moon_jellyfish",
                depthBand = DepthBand.Shallow,
                surveyTimepoint = SurveyTimepoint.Current,
                detectionState = SpeciesDetectionState.Detected,
                confidence = SurveyConfidence.Low
            });
            result.speciesObservations.Add(new EDNASpeciesObservationData { speciesId = "unknown_species" });
            input.ednaResults.Add(result);

            Assert.That(updater.TryApplyExternalInput(state, input, out string feedback), Is.True);
            Assert.That(state.SurveySpeciesIds, Has.Count.EqualTo(7));
            Assert.That(state.HasSurveySpecies("atlantic_herring"), Is.True);
            Assert.That(state.HasSurveySpecies("tree_bubblegum_coral"), Is.True);
            Assert.That(state.HasSurveySpecies("moon_jellyfish"), Is.False);
            Assert.That(state.HasSurveySpecies("unknown_species"), Is.False);
            InvestigationSurveySpeciesRecord coral = state.FindSurveySpeciesRecord("tree_bubblegum_coral", SurveyTimepoint.Current);
            Assert.That(coral, Is.Not.Null);
            Assert.That(coral.DepthBand, Is.EqualTo(DepthBand.Deep));
            Assert.That(coral.DetectionState, Is.EqualTo(SpeciesDetectionState.NotDetected));
            Assert.That(coral.SampleId, Is.EqualTo("sample-7"));
            Assert.That(feedback, Does.Contain("2 additional survey species"));
        }

        [Test]
        public void ReferenceFoodWebCascade_PropagatesAcrossTheFiveNodeChain()
        {
            IReadOnlyDictionary<string, PredictionState> states = new FoodWebCascadeEvaluator().Evaluate(
                caseDefinition,
                "reference_main",
                "great_hammerhead_shark",
                PredictionState.Decrease);

            Assert.That(states["great_hammerhead_shark"], Is.EqualTo(PredictionState.Decrease));
            Assert.That(states["atlantic_bluefin_tuna"], Is.EqualTo(PredictionState.Increase));
            Assert.That(states["atlantic_herring"], Is.EqualTo(PredictionState.Decrease));
            Assert.That(states["northern_krill"], Is.EqualTo(PredictionState.Increase));
            Assert.That(states["phytoplankton"], Is.EqualTo(PredictionState.Decrease));
        }

        [Test]
        public void Simulator_FillsMissingPredictionsFromTheAuthoredCaseNetwork()
        {
            InvestigationCaseDefinition caseClone = UnityEngine.Object.Instantiate(caseDefinition);
            ThreatSimulationDefinition threatClone = UnityEngine.Object.Instantiate(caseDefinition.FindThreat("longline"));
            try
            {
                SerializedObject threatObject = new SerializedObject(threatClone);
                threatObject.FindProperty("speciesPredictions").arraySize = 1;
                threatObject.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject caseObject = new SerializedObject(caseClone);
                SerializedProperty threats = caseObject.FindProperty("threats");
                for (int index = 0; index < threats.arraySize; index++)
                {
                    ThreatSimulationDefinition candidate = threats.GetArrayElementAtIndex(index).objectReferenceValue as ThreatSimulationDefinition;
                    if (candidate != null && candidate.ThreatId == "longline")
                        threats.GetArrayElementAtIndex(index).objectReferenceValue = threatClone;
                }
                caseObject.ApplyModifiedPropertiesWithoutUndo();

                SimulationResult simulation = new EcosystemSimulatorEvaluator().Evaluate(caseClone, "longline");
                Assert.That(simulation.FindPrediction("shark").PredictedState, Is.EqualTo(PredictionState.Decrease));
                Assert.That(simulation.FindPrediction("tuna").PredictedState, Is.EqualTo(PredictionState.Increase));
                Assert.That(simulation.FindPrediction("krill").PredictedState, Is.EqualTo(PredictionState.Decrease));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(threatClone);
                UnityEngine.Object.DestroyImmediate(caseClone);
            }
        }

        [Test]
        public void Simulator_ExplicitIntermediatePredictionControlsDownstreamInference()
        {
            InvestigationCaseDefinition caseClone = UnityEngine.Object.Instantiate(caseDefinition);
            ThreatSimulationDefinition threatClone = UnityEngine.Object.Instantiate(caseDefinition.FindThreat("longline"));
            try
            {
                SerializedObject threatObject = new SerializedObject(threatClone);
                SerializedProperty predictions = threatObject.FindProperty("speciesPredictions");
                predictions.arraySize = 2;
                predictions.GetArrayElementAtIndex(1).FindPropertyRelative("predictedState").enumValueIndex = (int)PredictionState.Stable;
                threatObject.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject caseObject = new SerializedObject(caseClone);
                SerializedProperty threats = caseObject.FindProperty("threats");
                for (int index = 0; index < threats.arraySize; index++)
                {
                    ThreatSimulationDefinition candidate = threats.GetArrayElementAtIndex(index).objectReferenceValue as ThreatSimulationDefinition;
                    if (candidate != null && candidate.ThreatId == "longline")
                        threats.GetArrayElementAtIndex(index).objectReferenceValue = threatClone;
                }
                caseObject.ApplyModifiedPropertiesWithoutUndo();

                SimulationResult simulation = new EcosystemSimulatorEvaluator().Evaluate(caseClone, "longline");
                Assert.That(simulation.FindPrediction("tuna").PredictedState, Is.EqualTo(PredictionState.Stable));
                Assert.That(simulation.FindPrediction("krill").PredictedState, Is.EqualTo(PredictionState.Stable));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(threatClone);
                UnityEngine.Object.DestroyImmediate(caseClone);
            }
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
                speciesObject.FindProperty("mapDepthBand").enumValueIndex = (int)DepthBand.Mid;
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
                Assert.That(feedback, Does.Contain("1 additional survey species"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(backgroundSpecies);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void Validator_RejectsMapDepthOutsidePreferredRange()
        {
            InvestigationCaseDefinition clone = UnityEngine.Object.Instantiate(caseDefinition);
            InvestigationSpeciesDefinition invalidSpecies = UnityEngine.Object.Instantiate(caseDefinition.FindSpecies("sea_star"));
            try
            {
                SerializedObject speciesObject = new SerializedObject(invalidSpecies);
                speciesObject.FindProperty("mapDepthBand").enumValueIndex = (int)DepthBand.Shallow;
                speciesObject.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject caseObject = new SerializedObject(clone);
                SerializedProperty species = caseObject.FindProperty("species");
                for (int index = 0; index < species.arraySize; index++)
                {
                    InvestigationSpeciesDefinition candidate = species.GetArrayElementAtIndex(index).objectReferenceValue as InvestigationSpeciesDefinition;
                    if (candidate == null || candidate.SpeciesId != "sea_star") continue;
                    species.GetArrayElementAtIndex(index).objectReferenceValue = invalidSpecies;
                    break;
                }
                caseObject.ApplyModifiedPropertiesWithoutUndo();

                List<string> errors = new InvestigationCaseValidator().Validate(clone);
                Assert.That(errors, Has.Some.Contains("map depth Shallow is outside its preferred depth range"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(invalidSpecies);
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
        public void CoreSpecies_UseTheFieldGuideSetWithBoundedTransparentTextures()
        {
            foreach (InvestigationSpeciesDefinition species in caseDefinition.Species)
            {
                string path = AssetDatabase.GetAssetPath(species.Icon);
                Assert.That(path, Does.StartWith("Assets/Art/Investigation/FieldGuide/"));
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.maxTextureSize, Is.EqualTo(512));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
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

        [TestCase("longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match, ComparisonEvaluationOutcome.AcceptedWithCaveat)]
        [TestCase("plastic", "mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch, ComparisonEvaluationOutcome.Accepted)]
        public void EvidenceSelection_ResolvesTheAuthoredRelationshipImmediately(
            string threatId, string speciesId, string evidenceId,
            ComparisonJudgement expectedJudgement, ComparisonEvaluationOutcome expectedOutcome)
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, threatId);
            PredictionComparisonRecord record = updater.CompareEvidence(state, threatId, PredictionTargetKind.Species, speciesId, evidenceId);
            Assert.That(record.Judgement, Is.EqualTo(expectedJudgement));
            Assert.That(record.Outcome, Is.EqualTo(expectedOutcome));
            Assert.That(record.CompletesObjective, Is.True, record.Feedback);
            Assert.That(state.CompletedObjectiveCount, Is.EqualTo(1));
            if (expectedOutcome == ComparisonEvaluationOutcome.AcceptedWithCaveat)
                Assert.That(record.Feedback, Does.Contain("does not prove"));
        }

        [Test]
        public void EvidenceSelection_UnrelatedFindingStaysOpenUntilARelevantFindingIsChosen()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord open = updater.CompareEvidence(state, "longline", PredictionTargetKind.Species,
                "shark", "E04_BENTHIC_STABLE");
            Assert.That(open.Judgement, Is.EqualTo(ComparisonJudgement.NotEnoughEvidence));
            Assert.That(open.IsAccepted, Is.True);
            Assert.That(open.LocksComparison, Is.False);
            Assert.That(state.CompletedObjectiveCount, Is.Zero);
            Assert.That(state.AcceptedComparisonCount, Is.Zero);
            Assert.That(state.MisstepCount, Is.Zero);

            PredictionComparisonRecord saved = updater.CompareEvidence(state, "longline", PredictionTargetKind.Species,
                "shark", "E01_SHARK_NONDETECTION");
            Assert.That(saved.Judgement, Is.EqualTo(ComparisonJudgement.Match));
            Assert.That(state.CompletedObjectiveCount, Is.EqualTo(1));
            PredictionComparisonRecord repeated = updater.CompareEvidence(state, "longline", PredictionTargetKind.Species,
                "shark", "E04_BENTHIC_STABLE");
            Assert.That(repeated.EvidenceId, Is.EqualTo(saved.EvidenceId));
            Assert.That(repeated.Judgement, Is.EqualTo(saved.Judgement));
            Assert.That(state.CompletedObjectiveCount, Is.EqualTo(1));
            Assert.That(state.ComparisonRecords, Has.Count.EqualTo(1));
        }

        [Test]
        public void EvidenceSelection_PreservesTheAuthoredCaveatForAnUncertainModel()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "plastic");
            PredictionComparisonRecord record = updater.CompareEvidence(state, "plastic", PredictionTargetKind.Species,
                "shark", "E01_SHARK_NONDETECTION");
            Assert.That(record.Judgement, Is.EqualTo(ComparisonJudgement.Mismatch));
            Assert.That(record.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.AcceptedWithCaveat));
            Assert.That(record.Feedback, Does.Contain("no directional explanation"));
            Assert.That(state.CompletedObjectiveCount, Is.Zero);
        }

        [Test]
        public void EvidenceSelection_RespectsModelDiscoveryAndRovGates()
        {
            InvestigationState state = CreateSimulationReadyState();
            Assert.That(updater.CompareEvidence(state, "longline", PredictionTargetKind.Species,
                "shark", "E01_SHARK_NONDETECTION").IsAccepted, Is.False);
            RunModel(state, "longline");
            Assert.That(updater.CompareEvidence(state, "longline", PredictionTargetKind.Species,
                "shark", "E07_FISHING_LINE").IsAccepted, Is.False);
            Assert.That(updater.CompareEvidence(state, "longline", PredictionTargetKind.Species,
                "sea_star", "E04_BENTHIC_STABLE").IsAccepted, Is.False);
            Assert.That(state.ComparisonRecords, Is.Empty);
            Assert.That(state.CompletedObjectiveCount, Is.Zero);

            state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "longline", out _), Is.True);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            Assert.That(updater.CompareEvidence(state, "longline", PredictionTargetKind.Species,
                "sea_star", "E04_BENTHIC_STABLE").CompletesObjective, Is.True);
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
                SerializedProperty objectives = serialized.FindProperty("investigationObjectives");
                for (int index = 0; index < objectives.arraySize; index++)
                {
                    if (objectives.GetArrayElementAtIndex(index).FindPropertyRelative("objectiveId").stringValue == "longline_krill")
                    {
                        objectives.MoveArrayElement(index, 0);
                        break;
                    }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                InvestigationStateUpdater cloneUpdater = new InvestigationStateUpdater(clone);
                InvestigationState state = cloneUpdater.CreateInitialState();
                DiscoverWith(cloneUpdater, state, "E01_SHARK_NONDETECTION");
                DiscoverWith(cloneUpdater, state, "E02_TUNA_WIDER_DETECTION");
                DiscoverWith(cloneUpdater, state, "E04_BENTHIC_STABLE");
                DiscoverWith(cloneUpdater, state, "E06_PLASTIC_INDICATOR_STABLE");
                Assert.That(cloneUpdater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);

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
        public void RetiredWarmingModel_IsUnavailableAndDoesNotBlockRemainingObjectives()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(caseDefinition.FindThreat("warming"), Is.Null);
            Assert.That(caseDefinition.FindObservation("E05_TEMPERATURE_NORMAL"), Is.Null);
            Assert.That(caseDefinition.FindComparisonRule("warming", "shark"), Is.Null);
            Assert.That(updater.TryRunThreat(state, "warming", out _, out _), Is.False);
            Assert.That(state.FindSimulation("warming"), Is.Null);
            Assert.That(state.TriedThreatIds, Has.Count.EqualTo(3));
            Assert.That(state.CompletedObjectiveCount, Is.EqualTo(5));
            Assert.That(updater.EvaluateReadiness(state).CanEnterProvisional, Is.True);
            Assert.That(updater.TrySubmitProvisional(state, "warming", out _), Is.False);
            Assert.That(updater.TrySubmitProvisional(state, "longline", out _), Is.True);
        }

        [Test]
        public void RemainingThreatAssets_KeepTheirSerializedGlyphIds()
        {
            Assert.That((int)PredictionTargetKind.Seafloor, Is.EqualTo(2));
            Assert.That((int)PredictionTargetKind.PhysicalConfirmation, Is.EqualTo(3));
            Assert.That(Enum.IsDefined(typeof(PredictionTargetKind), 1), Is.False);
            Assert.That(Enum.IsDefined(typeof(ThreatGlyphKind), 0), Is.False);
            string[] ids = { "plastic", "longline", "bottom_trawling" };
            for (int index = 0; index < ids.Length; index++)
            {
                ThreatSimulationDefinition threat = caseDefinition.FindThreat(ids[index]);
                Assert.That((int)threat.GlyphKind, Is.EqualTo(index + 1), ids[index]);
                Assert.That(new SerializedObject(threat).FindProperty("glyphKind").intValue,
                    Is.EqualTo(index + 1), "Existing asset IDs must not shift when an enum member is removed.");
            }
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
                    "L01_NONDETECTION_LIMITATION",
                    "E07_FISHING_LINE"
                },
                environmentalObservations = new List<InvestigationExternalObservationData>
                {
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

        [Test]
        public void ObserveGate_MethodNotesCannotReplaceCoreFindings()
        {
            InvestigationState state = updater.CreateInitialState();
            InvestigationGameInput input = new InvestigationGameInput();
            input.discoveredObservationIds.AddRange(new[] { "E01_SHARK_NONDETECTION", "E02_TUNA_WIDER_DETECTION",
                "E03_KRILL_NONDETECTION", "E04_BENTHIC_STABLE", "L01_NONDETECTION_LIMITATION" });
            Assert.That(updater.TryApplyExternalInput(state, input, out _), Is.True);
            Assert.That(state.DiscoveredObservationIds.Count, Is.EqualTo(5));
            Assert.That(InvestigationObserveEvaluator.CountFindings(caseDefinition, state), Is.EqualTo(4));
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);
            Assert.That(updater.TryRunThreat(state, "longline", out _, out _), Is.False);
            Discover(state, "E06_PLASTIC_INDICATOR_STABLE");
            Discover(state, "E06_PLASTIC_INDICATOR_STABLE");
            Assert.That(InvestigationObserveEvaluator.CountFindings(caseDefinition, state), Is.EqualTo(5));
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.True);
        }

        [Test]
        public void ExternalSurvey_ConflictingCoreInputIsRejectedWithoutPartialMutation()
        {
            InvestigationState state = updater.CreateInitialState();
            string originalSurvey = state.SurveyId;
            InvestigationGameInput input = new InvestigationGameInput();
            input.surveyContext.surveyId = "should-not-be-applied";
            input.discoveredObservationIds.Add("E02_TUNA_WIDER_DETECTION");
            input.ednaResults.Add(new EDNAResultData { detectedSpeciesIds = new List<string> { "great_hammerhead_shark", "moon_jellyfish" } });
            Assert.That(updater.TryApplyExternalInput(state, input, out string feedback), Is.False);
            Assert.That(feedback, Does.Contain("fixed case"));
            Assert.That(state.SurveyId, Is.EqualTo(originalSurvey));
            Assert.That(state.DiscoveredObservationIds, Is.Empty);
            Assert.That(state.HasSurveySpecies("moon_jellyfish"), Is.False);
        }

        [Test]
        public void ExternalSurvey_DetailedCurrentRecordTakesPrecedenceOverLegacyDetection()
        {
            InvestigationState state = updater.CreateInitialState();
            EDNAResultData result = new EDNAResultData { sampleQuality = SampleQuality.High };
            result.detectedSpeciesIds.Add("great_hammerhead_shark");
            result.speciesObservations.Add(new EDNASpeciesObservationData { speciesId = "shark", detectionState = SpeciesDetectionState.NotDetected });
            InvestigationGameInput input = new InvestigationGameInput();
            input.ednaResults.Add(result);
            Assert.That(updater.TryApplyExternalInput(state, input, out string feedback), Is.True, feedback);
            Assert.That(state.SurveySpeciesRecords, Has.Count.EqualTo(1));
            InvestigationSurveySummary current = InvestigationSurveyEvaluator.Resolve(caseDefinition, state, caseDefinition.FindSpecies("shark"), SurveyTimepoint.Current);
            Assert.That(current.Detection, Is.EqualTo(SpeciesDetectionState.NotDetected));
            Assert.That(current.Result, Is.EqualTo(caseDefinition.FindObservation("E01_SHARK_NONDETECTION").DisplayName));
            Assert.That(current.Source, Does.Contain("Case survey"));
        }

        [Test]
        public void ExternalSurvey_HistoricalSupplementRetainsItsNonDetection()
        {
            InvestigationState state = updater.CreateInitialState();
            InvestigationGameInput input = new InvestigationGameInput();
            EDNAResultData result = new EDNAResultData();
            result.speciesObservations.Add(new EDNASpeciesObservationData { speciesId = "moon_jellyfish",
                surveyTimepoint = SurveyTimepoint.Historical, detectionState = SpeciesDetectionState.NotDetected, depthBand = DepthBand.Deep });
            input.ednaResults.Add(result);
            Assert.That(updater.TryApplyExternalInput(state, input, out _), Is.True);
            InvestigationSurveySummary historical = InvestigationSurveyEvaluator.Resolve(caseDefinition, state, caseDefinition.FindSpecies("moon_jellyfish"), SurveyTimepoint.Historical);
            Assert.That(historical.Detection, Is.EqualTo(SpeciesDetectionState.NotDetected));
            Assert.That(historical.Depth, Is.EqualTo(DepthBand.Deep));
            Assert.That(historical.Observation, Is.Null);
            Assert.That(InvestigationSurveyEvaluator.Resolve(caseDefinition, state, caseDefinition.FindSpecies("moon_jellyfish"), SurveyTimepoint.Current), Is.Null);
        }

        [Test]
        public void ExternalSurvey_UnknownEraAndWrongSiteHaveActionableErrors()
        {
            InvestigationGameInput input = new InvestigationGameInput();
            input.surveyContext.siteId = "site-a";
            EDNASpeciesObservationData record = new EDNASpeciesObservationData { speciesId = "moon_jellyfish", surveyTimepoint = SurveyTimepoint.Unknown };
            EDNAResultData result = new EDNAResultData();
            result.speciesObservations.Add(record);
            input.ednaResults.Add(result);
            Assert.That(updater.TryApplyExternalInput(updater.CreateInitialState(), input, out string feedback), Is.False);
            Assert.That(feedback, Does.Contain("timepoint"));
            record.surveyTimepoint = SurveyTimepoint.Current;
            record.siteId = "site-b";
            Assert.That(updater.TryApplyExternalInput(updater.CreateInitialState(), input, out feedback), Is.False);
            Assert.That(feedback, Does.Contain("different site"));
        }

        [Test]
        public void ExternalSurvey_ConflictsInOneSampleAreDistinctFromSeparateSamples()
        {
            InvestigationGameInput input = new InvestigationGameInput();
            EDNAResultData result = new EDNAResultData { sampleId = "one-sample" };
            result.speciesObservations.Add(new EDNASpeciesObservationData { speciesId = "moon_jellyfish", detectionState = SpeciesDetectionState.Detected });
            EDNASpeciesObservationData second = new EDNASpeciesObservationData { speciesId = "moon_jellyfish", detectionState = SpeciesDetectionState.NotDetected };
            result.speciesObservations.Add(second);
            input.ednaResults.Add(result);
            Assert.That(updater.TryApplyExternalInput(updater.CreateInitialState(), input, out _), Is.False);
            second.sampleId = "another-sample";
            Assert.That(updater.TryApplyExternalInput(updater.CreateInitialState(), input, out _), Is.True);
        }

        [Test]
        public void HypothesisSummary_DerivesOnlyRecordedChecksAndKeepsUncertaintyOpen()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord open = updater.Compare(state, "longline", "shark", "E04_BENTHIC_STABLE", ComparisonJudgement.NotEnoughEvidence);
            Assert.That(open.IsAccepted, Is.True);
            InvestigationHypothesisSummary summary = new InvestigationHypothesisSummary(caseDefinition, state, "longline");
            Assert.That(summary.OpenCount, Is.EqualTo(1));
            Assert.That(summary.SupportCount, Is.Zero);
            Assert.That(summary.ChallengeCount, Is.Zero);
            Assert.That(new InvestigationHypothesisSummary(caseDefinition, state, "plastic").Records, Is.Empty);
            updater.Compare(state, "longline", "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match);
            Assert.That(new InvestigationHypothesisSummary(caseDefinition, state, "longline").Records, Has.Count.EqualTo(1));
            updater.Compare(state, "longline", "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match);
            summary = new InvestigationHypothesisSummary(caseDefinition, state, "longline");
            Assert.That(summary.SupportCount, Is.EqualTo(1));
            Assert.That(summary.OpenCount, Is.Zero);
        }

        [Test]
        public void ReportPhase_RequiresASavedProvisionalIdeaEvenWhenComparisonsAreComplete()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(updater.EvaluateReadiness(state).CanEnterProvisional, Is.True);
            InvestigationPhase phaseBefore = state.Phase;
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Report, out string feedback), Is.False);
            Assert.That(feedback, Does.Contain("Save a first idea"));
            Assert.That(state.Phase, Is.EqualTo(phaseBefore));
            Assert.That(state.ProvisionalThreatId, Is.Empty);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.False);

            Assert.That(updater.TrySubmitProvisional(state, "longline", out _), Is.True);
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Observe, out _), Is.True);
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Report, out _), Is.True);
            Assert.That(state.ProvisionalThreatId, Is.EqualTo("longline"));
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
