using System.Linq;
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
        public void DirectionalRulesAreDecisive_WhileUnknownPredictionsRemainOpen()
        {
            for (int ruleIndex = 0; ruleIndex < caseDefinition.ComparisonRules.Count; ruleIndex++)
            {
                PredictionComparisonRuleDefinition rule = caseDefinition.ComparisonRules[ruleIndex];
                if (new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, rule.ThreatId).FindPrediction(rule.SpeciesId)?.PredictedState == PredictionState.Unknown)
                {
                    Assert.That(rule.ObservationOptions[0].FindResolution(ComparisonJudgement.NotEnoughEvidence).Outcome, Is.EqualTo(ComparisonEvaluationOutcome.Accepted));
                    continue;
                }
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
        public void ThreatMatrix_CoversTheSixSurveySpecies()
        {
            Assert.That(caseDefinition.Threats, Has.Count.EqualTo(4));
            Assert.That(caseDefinition.Species, Has.Count.EqualTo(6));
            for (int index = 0; index < caseDefinition.Threats.Count; index++)
            {
                Assert.That(new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, caseDefinition.Threats[index].ThreatId).Predictions,
                    Has.Count.EqualTo(6));
            }
            Assert.That(caseDefinition.ComparisonRules, Has.Count.EqualTo(24));
            Assert.That(caseDefinition.InvestigationObjectives, Has.Count.EqualTo(7));
            Assert.That(caseDefinition.FindSpecies("shark").MapDepthBand, Is.EqualTo(DepthBand.Shallow));
            Assert.That(caseDefinition.FindSpecies("tuna").MapDepthBand, Is.EqualTo(DepthBand.Shallow));
            Assert.That(caseDefinition.FindSpecies("krill").MapDepthBand, Is.EqualTo(DepthBand.Mid));
            Assert.That(caseDefinition.FindSpecies("atlantic_herring").MapDepthBand, Is.EqualTo(DepthBand.Shallow));
            Assert.That(caseDefinition.FindSpecies("phytoplankton").MapDepthBand, Is.EqualTo(DepthBand.Shallow));
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
            Assert.That(caseDefinition.FoodWebChainSpeciesIds, Is.EqualTo(new[] { "shark", "tuna", "atlantic_herring", "krill", "phytoplankton" }));
            Assert.That(caseDefinition.SimulationFoodWebId, Is.EqualTo("reference_main"));
            Assert.That(caseDefinition.FoodWebEdges, Has.Count.EqualTo(10));
            Assert.That(caseDefinition.BenthicIndicatorSpeciesIds, Is.Empty);
            Assert.That(caseDefinition.FollowUpLockedSpeciesIds, Is.Empty);
            Assert.That(caseDefinition.MaximumSurveySpecies, Is.EqualTo(7));
        }

        // Approved Species List FigJam m631tWbfQ8NajWmFN3X93q, checked 2026-09-10.
        [TestCase("green_sea_urchin", "Strongylocentrotus droebachiensis")]
        [TestCase("reef_manta_ray", "Mobula alfredi")]
        [TestCase("kitefin_shark", "Dalatias licha")]
        [TestCase("great_hammerhead_shark", "Sphyrna mokarran")]
        [TestCase("orange_roughy", "Hoplostethus atlanticus")]
        [TestCase("pinecone_fish", "Monocentris japonica")]
        [TestCase("atlantic_bluefin_tuna", "Thunnus thynnus")]
        [TestCase("atlantic_herring", "Clupea harengus")]
        [TestCase("spotted_lanternfish", "Myctophum punctatum")]
        [TestCase("northern_krill", "Meganyctiphanes norvegica")]
        [TestCase("king_crab", "Neolithodes agassizii")]
        [TestCase("warty_squid", "Moroteuthopsis longimana")]
        [TestCase("flapjack_octopus", "Opisthoteuthis californiana")]
        [TestCase("giant_pacific_octopus", "Enteroctopus dofleini")]
        [TestCase("bone_eating_worm", "Osedax frankpressi")]
        [TestCase("tree_bubblegum_coral", "Paragorgia arborea")]
        [TestCase("precious_coral", "Corallium rubrum")]
        [TestCase("zigzag_coral", "Madrepora oculata")]
        [TestCase("moon_jellyfish", "Aurelia aurita")]
        [TestCase("phytoplankton", "Prochlorococcus marinus")]
        public void SharedCatalog_MatchesApprovedFigmaSpecies_AndImportsNames(string canonicalId, string scientificName)
        {
            var species = caseDefinition.FindCatalogSpecies(canonicalId);
            Assert.That(species, Is.Not.Null);
            Assert.That(species.ScientificName, Is.EqualTo(scientificName));
            Assert.That(caseDefinition.FindCatalogSpecies("  " + scientificName.ToUpperInvariant() + "  "), Is.SameAs(species));
            Assert.That(caseDefinition.FindCatalogSpecies(species.DisplayName), Is.SameAs(species));
            Assert.That(caseDefinition.FindCatalogSpecies(canonicalId.Replace('_', '-')), Is.SameAs(species));
            var input = new InvestigationGameInput();
            var result = new EDNAResultData();
            result.speciesObservations.Add(new EDNASpeciesObservationData
            {
                speciesId = scientificName,
                surveyTimepoint = SurveyTimepoint.Historical,
                detectionState = SpeciesDetectionState.Detected,
                depthBand = species.MapDepthBand
            });
            input.ednaResults.Add(result);
            var state = updater.CreateInitialState();
            Assert.That(updater.TryApplyExternalInput(state, input, out string feedback), Is.True, feedback);
            Assert.That(state.HasSurveySpecies(species.SpeciesId), Is.True);
        }

        [Test]
        public void ImportedSpecies_StayWithinApprovedCatalog_WithoutInventingAliasesForLegacyControls()
        {
            var input = new InvestigationGameInput();
            var result = new EDNAResultData();
            result.detectedSpeciesIds.AddRange(new[] { "unlisted_species", "sea_star", "mussel", "Mobula alfredi" });
            input.ednaResults.Add(result);
            var roster = new InvestigationCaseRosterBuilder().Build(caseDefinition, input);
            foreach (var record in roster.SurveyRecords)
                Assert.That(caseDefinition.FindCatalogSpecies(record.SpeciesId), Is.Not.Null);
            Assert.That(caseDefinition.FindCatalogSpecies("sea_star"), Is.Null);
            Assert.That(caseDefinition.FindCatalogSpecies("mussel"), Is.Null);
        }

        [Test]
        public void SharedCatalog_RejectsNamesThatWouldResolveToDifferentSpecies()
        {
            var clone = UnityEngine.Object.Instantiate(caseDefinition);
            var duplicate = UnityEngine.Object.Instantiate(caseDefinition.FindCatalogSpecies("reef_manta_ray"));
            try
            {
                var entry = new SerializedObject(duplicate);
                entry.FindProperty("displayName").stringValue = "  SPHYRNA MOKARRAN  ";
                entry.ApplyModifiedPropertiesWithoutUndo();
                var data = new SerializedObject(clone);
                var catalog = data.FindProperty("speciesCatalog");
                for (int i = 0; i < catalog.arraySize; i++)
                    if (catalog.GetArrayElementAtIndex(i).objectReferenceValue == caseDefinition.FindCatalogSpecies("reef_manta_ray"))
                        catalog.GetArrayElementAtIndex(i).objectReferenceValue = duplicate;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(new InvestigationCaseValidator().Validate(clone),
                    Has.Some.Contains("Species catalog name is ambiguous"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(duplicate);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void TeamArtwork_IsSharedByCaseAndImportedSpecies()
        {
            foreach (string id in new[] { "great_hammerhead_shark", "reef_manta_ray", "bone_eating_worm" })
            {
                var sprite = caseDefinition.FindCatalogSpecies(id).Icon;
                Assert.That(sprite, Is.Not.Null, id);
                string path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(path, Does.StartWith("Assets/Art/Investigation/TeamSpecies/"));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importer.alphaIsTransparency, Is.True);
            }
        }

        [Test]
        public void ExternalEdnaResults_NormalizeCanonicalAliasesToCaseSpeciesIds()
        {
            InvestigationState state = updater.CreateInitialState();
            InvestigationGameInput input = new InvestigationGameInput();
            EDNAResultData result = new EDNAResultData();
            result.speciesObservations.Add(new EDNASpeciesObservationData
            {
                speciesId = "great_hammerhead_shark", detectionState = SpeciesDetectionState.Detected
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
                speciesId = "reef_manta_ray",
                depthBand = DepthBand.Mid,
                surveyTimepoint = SurveyTimepoint.Current,
                detectionState = SpeciesDetectionState.Detected,
                confidence = SurveyConfidence.High
            });
            result.speciesObservations.Add(new EDNASpeciesObservationData
            {
                speciesId = "precious_coral",
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
            Assert.That(state.HasSurveySpecies("reef_manta_ray"), Is.True);
            Assert.That(state.HasSurveySpecies("precious_coral"), Is.False);
            Assert.That(state.HasSurveySpecies("moon_jellyfish"), Is.False);
            Assert.That(state.HasSurveySpecies("unknown_species"), Is.False);
            InvestigationSurveySpeciesRecord coral = state.FindSurveySpeciesRecord("reef_manta_ray", SurveyTimepoint.Current);
            Assert.That(coral, Is.Not.Null);
            Assert.That(coral.DepthBand, Is.EqualTo(DepthBand.Mid));
            Assert.That(coral.DetectionState, Is.EqualTo(SpeciesDetectionState.Detected));
            Assert.That(coral.SampleId, Is.EqualTo("sample-7"));
            Assert.That(feedback, Does.Contain("1 additional survey species"));
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
                Assert.That(simulation.FindPrediction("krill").PredictedState, Is.EqualTo(PredictionState.Increase));
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
                predictions.GetArrayElementAtIndex(1).FindPropertyRelative("speciesId").stringValue = "tuna";
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
        public void ExternalEdnaResults_AddOnlyCatalogSpecies_NotArbitraryCaseDefinitions()
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
                result.detectedSpeciesIds.Add("moon_jellyfish");
                result.detectedSpeciesIds.Add("unknown_species");
                input.ednaResults.Add(result);
                Assert.That(cloneUpdater.TryApplyExternalInput(state, input, out string feedback), Is.True);
                Assert.That(state.HasSurveySpecies("background_jelly"), Is.False);
                Assert.That(state.HasSurveySpecies("moon_jellyfish"), Is.True);
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
            InvestigationSpeciesDefinition invalidSpecies = UnityEngine.Object.Instantiate(caseDefinition.FindSpecies("atlantic_herring"));
            try
            {
                SerializedObject speciesObject = new SerializedObject(invalidSpecies);
                speciesObject.FindProperty("mapDepthBand").enumValueIndex = (int)DepthBand.Deep;
                speciesObject.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject caseObject = new SerializedObject(clone);
                SerializedProperty species = caseObject.FindProperty("species");
                for (int index = 0; index < species.arraySize; index++)
                {
                    InvestigationSpeciesDefinition candidate = species.GetArrayElementAtIndex(index).objectReferenceValue as InvestigationSpeciesDefinition;
                    if (candidate == null || candidate.SpeciesId != "atlantic_herring") continue;
                    species.GetArrayElementAtIndex(index).objectReferenceValue = invalidSpecies;
                    break;
                }
                caseObject.ApplyModifiedPropertiesWithoutUndo();

                List<string> errors = new InvestigationCaseValidator().Validate(clone);
                Assert.That(errors, Has.Some.Contains("map depth Deep is outside its preferred depth range"));
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
        public void CoreSpecies_UseApprovedArtworkWithBoundedTransparentTextures()
        {
            foreach (InvestigationSpeciesDefinition species in caseDefinition.Species)
            {
                string path = AssetDatabase.GetAssetPath(species.Icon);
                Assert.That(path, Does.StartWith(species.SpeciesId == "shark"
                    ? "Assets/Art/Investigation/TeamSpecies/" : "Assets/Art/Investigation/FieldGuide/"));
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.maxTextureSize, Is.EqualTo(species.SpeciesId == "shark" ? 1024 : 512));
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
        public void OptionalBloom_IsExplicitAndDoesNotAddCompletionRequirements()
        {
            var bloom = caseDefinition.FindThreat("toxic_algal_bloom");
            Assert.That(bloom.OptionalExploration, Is.True);
            Assert.That(bloom.UseFoodWebCascade, Is.False);
            Assert.That(bloom.Icon, Is.Not.Null);
            var simulation = new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, bloom.ThreatId);
            foreach (string id in new[] { "shark", "tuna", "atlantic_herring", "krill", "phytoplankton" })
                Assert.That(simulation.FindPrediction(id).PredictedState, Is.EqualTo(PredictionState.Absent));
            Assert.That(simulation.FindPrediction("tree_bubblegum_coral").PredictedState, Is.EqualTo(PredictionState.Unknown));
            Assert.That(caseDefinition.InvestigationObjectives.Any(o => o.Required && o.ThreatId == bloom.ThreatId), Is.False);
            var state = PrepareProvisionalReadyState(); ReviewModelPair(state);
            Assert.That(state.HasTriedThreat(bloom.ThreatId), Is.False);
            Assert.That(updater.SubmitModelConclusion(state, caseDefinition.PrimaryModelThreatId).Status, Is.EqualTo(InvestigationConclusionStatus.Correct));
        }

        [Test]
        public void FishingModels_ContrastOnTunaAndHerringButKeepSurfacePhytoplanktonStable()
        {
            var longLine = new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, "longline");
            var trawl = new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, "bottom_trawling");
            Assert.That(longLine.FindPrediction("tuna").PredictedState, Is.EqualTo(PredictionState.Increase));
            Assert.That(trawl.FindPrediction("tuna").PredictedState, Is.EqualTo(PredictionState.Decrease));
            Assert.That(longLine.FindPrediction("atlantic_herring").PredictedState, Is.EqualTo(PredictionState.Decrease));
            Assert.That(trawl.FindPrediction("atlantic_herring").PredictedState, Is.EqualTo(PredictionState.Increase));
            foreach (var model in new[] { longLine, trawl })
                Assert.That(model.FindPrediction("phytoplankton").PredictedState, Is.EqualTo(PredictionState.Stable));
            Assert.That(trawl.FindPrediction("tree_bubblegum_coral").PredictedState, Is.EqualTo(PredictionState.Absent));
            Assert.That(trawl.FindPrediction("krill").PredictedState, Is.EqualTo(PredictionState.Unknown));
            Assert.That(caseDefinition.SupportedModelThreatIds, Is.EquivalentTo(new[] { "bottom_trawling" }));
            Assert.That(caseDefinition.RequiredModelReviewIds, Is.EquivalentTo(new[] { "longline", "bottom_trawling" }));
            Assert.That(caseDefinition.FindThreat("bottom_trawling").FoodSupplyLinks.Select(l => l.Kind),
                Is.EqualTo(new[] { ScenarioFoodLinkKind.SinkingOrganicMatter, ScenarioFoodLinkKind.CoralSpawn, ScenarioFoodLinkKind.Feeding, ScenarioFoodLinkKind.Feeding }));
        }

        [Test]
        public void RevisedSurvey_KeepsSixTasksAndDoesNotImportTheRetiredProducerClaim()
        {
            Assert.That(caseDefinition.FindObservation("E05_PHYTOPLANKTON_STABLE").ClaimType, Is.EqualTo(ObservationClaimType.MatchesBaseline));
            Assert.That(caseDefinition.FindObservation("E05_PHYTOPLANKTON_FEWER_SITES"), Is.Null);
            Assert.That(caseDefinition.IsCaseSpecies("tree_bubblegum_coral"), Is.True);
            Assert.That(caseDefinition.FindSpecies("tree_bubblegum_coral").Icon, Is.Not.Null);
            Assert.That(caseDefinition.FindThreat("longline").FindPrediction("phytoplankton").PredictedState, Is.EqualTo(PredictionState.Stable));
            Assert.That(caseDefinition.FindThreat("bottom_trawling").UseFoodWebCascade, Is.False);
            Assert.That(caseDefinition.FindThreat("bottom_trawling").DisplaySpeciesIds,
                Is.EqualTo(new[] { "shark", "tuna", "atlantic_herring", "tree_bubblegum_coral", "phytoplankton" }));
            var state = updater.CreateInitialState();
            Assert.That(updater.TryApplyExternalInput(state, new InvestigationGameInput { caseId = "investigation_foodchain_02" }, out _), Is.False);
            Assert.That(state.DiscoveredObservationIds, Is.Empty);
        }

        [Test]
        public void Simulator_ReturnsSpeciesAndEnvironmentalPredictions()
        {
            SimulationResult result = new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, "longline");
            Assert.That(result.Predictions, Has.Count.EqualTo(6));
            Assert.That(result.FindPrediction("shark").PredictedState, Is.EqualTo(PredictionState.Decrease));
            Assert.That(result.FindPrediction("tuna").PredictedState, Is.EqualTo(PredictionState.Increase));
            Assert.That(result.FindPrediction("krill").PredictedState, Is.EqualTo(PredictionState.Increase));
            Assert.That(result.SeafloorPrediction, Does.Contain("No seabed observation"));
            Assert.That(result.PhysicalConfirmation, Does.Contain("No physical observation"));
        }

        [TestCase("bottom_trawling", "shark", "E01_SHARK_FEWER_SITES", ComparisonJudgement.Match, ComparisonEvaluationOutcome.AcceptedWithCaveat)]
        [TestCase("plastic", "tuna", "E02_TUNA_FEWER_SITES", ComparisonJudgement.Mismatch, ComparisonEvaluationOutcome.Accepted)]
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
            RunModel(state, "bottom_trawling");
            PredictionComparisonRecord open = updater.CompareEvidence(state, "bottom_trawling", PredictionTargetKind.Species,
                "shark", "E03_HERRING_WIDER_DETECTION");
            Assert.That(open.Judgement, Is.EqualTo(ComparisonJudgement.NotEnoughEvidence));
            Assert.That(open.IsAccepted, Is.True);
            Assert.That(open.LocksComparison, Is.False);
            Assert.That(state.CompletedObjectiveCount, Is.Zero);
            Assert.That(state.AcceptedComparisonCount, Is.Zero);
            Assert.That(state.MisstepCount, Is.Zero);

            PredictionComparisonRecord saved = updater.CompareEvidence(state, "bottom_trawling", PredictionTargetKind.Species,
                "shark", "E01_SHARK_FEWER_SITES");
            Assert.That(saved.Judgement, Is.EqualTo(ComparisonJudgement.Match));
            Assert.That(state.CompletedObjectiveCount, Is.EqualTo(1));
            PredictionComparisonRecord repeated = updater.CompareEvidence(state, "bottom_trawling", PredictionTargetKind.Species,
                "shark", "E03_HERRING_WIDER_DETECTION");
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
                "shark", "E01_SHARK_FEWER_SITES");
            Assert.That(record.Judgement, Is.EqualTo(ComparisonJudgement.NotEnoughEvidence));
            Assert.That(record.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.Accepted));
            Assert.That(record.Feedback, Does.Contain("Unknown does not mean stable"));
            Assert.That(state.CompletedObjectiveCount, Is.Zero);
        }

        [Test]
        public void EvidenceSelection_UsesRecordedHerringAndKeepsUnreviewedPhysicalFindingsLocked()
        {
            InvestigationState state = CreateSimulationReadyState();
            Assert.That(updater.CompareEvidence(state, "bottom_trawling", PredictionTargetKind.Species,
                "shark", "E01_SHARK_FEWER_SITES").IsAccepted, Is.False);
            RunModel(state, "bottom_trawling");
            Assert.That(updater.CompareEvidence(state, "bottom_trawling", PredictionTargetKind.Species,
                "shark", "E07_FISHING_LINE").IsAccepted, Is.False);
            Assert.That(updater.CompareEvidence(state, "bottom_trawling", PredictionTargetKind.Species,
                "atlantic_herring", "E03_HERRING_WIDER_DETECTION").CompletesObjective, Is.True);
            Assert.That(state.CompletedObjectiveCount, Is.EqualTo(1));
            Assert.That(state.ConfirmationReviewed, Is.False);
            Assert.That(new InvestigationHypothesisSummary(caseDefinition, state, "bottom_trawling").SupportCount, Is.EqualTo(1));
            Assert.That(updater.TrySubmitProvisional(state, "bottom_trawling", out _), Is.False);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.False);
            Assert.That(state.SelectedReportEvidenceIds, Is.Empty);
        }

        [Test]
        public void DirectRepeatedNonDetection_AcceptsMatchWithCaveatButRejectsNotEnoughEvidence()
        {
            InvestigationState matchState = CreateSimulationReadyState();
            RunModel(matchState, "bottom_trawling");
            PredictionComparisonRecord match = updater.Compare(matchState, "bottom_trawling", "tree_bubblegum_coral", "E04_CORAL_NONDETECTION", ComparisonJudgement.Match);

            InvestigationState cautiousState = CreateSimulationReadyState();
            RunModel(cautiousState, "bottom_trawling");
            PredictionComparisonRecord cautious = updater.Compare(cautiousState, "bottom_trawling", "tree_bubblegum_coral", "E04_CORAL_NONDETECTION", ComparisonJudgement.NotEnoughEvidence);
            Assert.That(match.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.AcceptedWithCaveat));
            Assert.That(cautious.Outcome, Is.EqualTo(ComparisonEvaluationOutcome.Incorrect));
        }

        [Test]
        public void CrossSpeciesCandidate_DoesNotBecomeAFalseDirectMatch()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord wrong = updater.Compare(state, "longline", "shark", "E03_HERRING_WIDER_DETECTION", ComparisonJudgement.Match);
            PredictionComparisonRecord cautious = updater.Compare(state, "longline", "shark", "E03_HERRING_WIDER_DETECTION", ComparisonJudgement.NotEnoughEvidence);
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
            Assert.That(updater.Compare(state, "longline", "shark", "E03_HERRING_WIDER_DETECTION", ComparisonJudgement.NotEnoughEvidence).IsAccepted, Is.True);
            RunModel(state, "bottom_trawling");
            Assert.That(updater.Compare(state, "bottom_trawling", "shark", "E03_HERRING_WIDER_DETECTION", ComparisonJudgement.NotEnoughEvidence).IsAccepted, Is.True);

            Assert.That(state.AcceptedComparisonCount, Is.Zero);
            Assert.That(state.MisstepCount, Is.Zero);
            Assert.That(updater.EvaluateReadiness(state).CanEnterProvisional, Is.False);
        }

        [Test]
        public void AcceptedNotEnoughEvidence_DoesNotLockAndCanBeReplaced()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "bottom_trawling");
            PredictionComparisonRecord cautious = updater.Compare(
                state,
                "bottom_trawling",
                "shark",
                "E03_HERRING_WIDER_DETECTION",
                ComparisonJudgement.NotEnoughEvidence);
            Assert.That(cautious.IsAccepted, Is.True);
            Assert.That(cautious.LocksComparison, Is.False);
            Assert.That(cautious.CompletesObjective, Is.False);

            PredictionComparisonRecord decisive = updater.Compare(
                state,
                "bottom_trawling",
                "shark",
                "E01_SHARK_FEWER_SITES",
                ComparisonJudgement.Match);
            Assert.That(decisive.LocksComparison, Is.True, decisive.Feedback);
            Assert.That(decisive.CompletesObjective, Is.True, decisive.Feedback);
            Assert.That(state.FindComparison("bottom_trawling", "shark").EvidenceId, Is.EqualTo("E01_SHARK_FEWER_SITES"));
        }

        [Test]
        public void AcceptedContextOnly_LocksButDoesNotCompleteObjective()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "longline");
            PredictionComparisonRecord context = updater.Compare(
                state,
                "longline",
                "phytoplankton",
                "E05_PHYTOPLANKTON_STABLE",
                ComparisonJudgement.Match);
            Assert.That(context.IsAccepted, Is.True);
            Assert.That(context.LocksComparison, Is.True);
            Assert.That(context.CompletesObjective, Is.False);
            Assert.That(context.ProgressRole, Is.EqualTo(ComparisonProgressRole.ContextOnly));

            PredictionComparisonRecord replay = updater.Compare(
                state,
                "longline",
                "phytoplankton",
                "E05_PHYTOPLANKTON_STABLE",
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
                "E01_SHARK_FEWER_SITES",
                ComparisonJudgement.Match);
            AssertAccepted(accepted);

            PredictionComparisonRecord replay = updater.Compare(
                state,
                "longline",
                "shark",
                "E03_HERRING_WIDER_DETECTION",
                ComparisonJudgement.Match);

            Assert.That(replay.IsAccepted, Is.True);
            Assert.That(replay.EvidenceId, Is.EqualTo("E01_SHARK_FEWER_SITES"));
            Assert.That(state.AcceptedComparisonCount, Is.EqualTo(1));
            Assert.That(state.MisstepCount, Is.Zero);
            Assert.That(replay.Feedback, Does.Contain("locked").IgnoreCase);
        }

        [Test]
        public void SimulateStage_RequiresAllSixObservedFindings()
        {
            InvestigationState state = updater.CreateInitialState();
            Discover(state, "E01_SHARK_FEWER_SITES");
            Discover(state, "E02_TUNA_FEWER_SITES");
            Discover(state, "E04_CORAL_NONDETECTION");
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);
            Discover(state, "E03_HERRING_WIDER_DETECTION");
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);
            Discover(state, "E05_PHYTOPLANKTON_STABLE");
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);
            Discover(state, "E09_KRILL_WIDER_DETECTION");
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.True);
        }

        [Test]
        public void MissingCoralEvidence_IsReportedByObjectiveReadiness()
        {
            InvestigationCaseDefinition clone = UnityEngine.Object.Instantiate(caseDefinition);
            try
            {
                SerializedObject serialized = new SerializedObject(clone);
                SerializedProperty objectives = serialized.FindProperty("investigationObjectives");
                for (int index = 0; index < objectives.arraySize; index++)
                {
                    if (objectives.GetArrayElementAtIndex(index).FindPropertyRelative("objectiveId").stringValue == "trawling_tree_bubblegum_coral")
                    {
                        objectives.MoveArrayElement(index, 0);
                        break;
                    }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                InvestigationStateUpdater cloneUpdater = new InvestigationStateUpdater(clone);
                InvestigationState state = cloneUpdater.CreateInitialState();
                DiscoverWith(cloneUpdater, state, "E01_SHARK_FEWER_SITES");
                DiscoverWith(cloneUpdater, state, "E02_TUNA_FEWER_SITES");
                DiscoverWith(cloneUpdater, state, "E03_HERRING_WIDER_DETECTION");
                DiscoverWith(cloneUpdater, state, "E05_PHYTOPLANKTON_STABLE");
                Assert.That(cloneUpdater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);

                InvestigationReadiness readiness = cloneUpdater.EvaluateReadiness(state);
                Assert.That(readiness.CanEnterProvisional, Is.False);
                Assert.That(readiness.MissingObjectiveId, Is.EqualTo("trawling_tree_bubblegum_coral"));
                Assert.That(readiness.MissingEvidenceId, Is.EqualTo("E04_CORAL_NONDETECTION"));
                Assert.That(cloneUpdater.TrySetPhase(state, InvestigationPhase.Observe, out _), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void ProvisionalGate_RequiresBothFishingModelsAndComparisons()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            InvestigationReadiness readiness = updater.EvaluateReadiness(state);
            Assert.That(readiness.RequiredThreatsCompared, Is.True);
            Assert.That(readiness.MinimumComparisonsComplete, Is.True);
            Assert.That(readiness.CanEnterProvisional, Is.True);
            Assert.That(readiness.RequiredObjectivesComplete, Is.True);
            Assert.That(state.ConfirmationReviewed, Is.False);
        }

        [Test]
        public void ProvisionalGate_RejectsHerringAndPhytoplanktonOnlyShortcut()
        {
            InvestigationState state = CreateSimulationReadyState();
            RunModel(state, "bottom_trawling");
            PredictionComparisonRecord lockedLongline = updater.Compare(state, "bottom_trawling", "atlantic_herring", "E03_HERRING_WIDER_DETECTION", ComparisonJudgement.Match);
            Assert.That(lockedLongline.CompletesObjective, Is.True);
            PredictionComparisonRecord longlinePhytoplankton = updater.Compare(state, "bottom_trawling", "phytoplankton", "E05_PHYTOPLANKTON_STABLE", ComparisonJudgement.Match);
            Assert.That(longlinePhytoplankton.LocksComparison, Is.True);
            Assert.That(longlinePhytoplankton.CompletesObjective, Is.True);
            RunModel(state, "longline");
            PredictionComparisonRecord lockedTrawl = updater.Compare(state, "longline", "atlantic_herring", "E03_HERRING_WIDER_DETECTION", ComparisonJudgement.Mismatch);
            Assert.That(lockedTrawl.CompletesObjective, Is.False);
            PredictionComparisonRecord trawlPhytoplankton = updater.Compare(state, "longline", "phytoplankton", "E05_PHYTOPLANKTON_STABLE", ComparisonJudgement.Match);
            Assert.That(trawlPhytoplankton.LocksComparison, Is.True);
            Assert.That(trawlPhytoplankton.CompletesObjective, Is.False);
            Assert.That(updater.EvaluateReadiness(state).CanEnterProvisional, Is.False);
            Assert.That(state.MisstepCount, Is.Zero, "Valid Herring comparisons must not count as scientific mistakes.");
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

        [Test, Category("LegacyReportCompatibility")]
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

        [Test, Category("LegacyReportCompatibility")]
        public void RovReport_PreservesThePlayersProvisionalChoiceAndAllowsRevision()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "bottom_trawling", out _), Is.True);
            Assert.That(state.ProvisionalThreatId, Is.EqualTo("bottom_trawling"));
            Assert.That(state.FinalThreatId, Is.Empty);
            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.False);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            Assert.That(state.FinalThreatId, Is.EqualTo("bottom_trawling"));
            Assert.That(state.SelectedReportEvidenceIds, Has.Count.EqualTo(7));
            Assert.That(state.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            Assert.That(state.SelectedReportEvidenceIds, Has.Count.EqualTo(7));
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
            Assert.That(state.CompletedObjectiveCount, Is.EqualTo(7));
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

        [Test, Category("LegacyReportCompatibility")]
        public void FinalReport_CompletesWithoutAnyFollowUpSampleState()
        {
            InvestigationState state = PrepareCompleteReport("bottom_trawling");
            InvestigationConclusionResult result = updater.SubmitFinal(state);
            Assert.That(result.Status, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(state.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
        }

        [Test, Category("LegacyReportCompatibility")]
        public void IncompleteFinalReport_ReturnsSpecificFeedbackWithoutPenaltyAndEditsRestoreDraftState()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "longline", out _), Is.True);
            InvestigationConclusionResult incomplete = updater.SubmitFinal(state);
            Assert.That(incomplete.Status, Is.EqualTo(InvestigationConclusionStatus.InsufficientEvidence));
            Assert.That(incomplete.Feedback, Does.Contain("ROV"));
            Assert.That(state.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(state.MisstepCount, Is.Zero);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.True);
            Assert.That(updater.EvaluateReadiness(state).CanSubmitFinal, Is.True);
            Assert.That(state.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.NotSubmitted));
        }

        [Test, Category("LegacyReportCompatibility")]
        public void WrongFinalCause_ExplainsWhyTheCompletePatternMatters()
        {
            InvestigationState state = PrepareCompleteReport("plastic");
            InvestigationConclusionResult result = updater.SubmitFinal(state);
            Assert.That(result.Status, Is.EqualTo(InvestigationConclusionStatus.Incorrect));
            Assert.That(result.Feedback, Does.Contain("complete survey pattern"));
        }

        [Test, Category("LegacyReportCompatibility")]
        public void AlwaysAvailableMethodLimitation_PreventsConditionalEvidenceDeadlock()
        {
            InvestigationObservationDefinition limitation = caseDefinition.FindObservation("L01_NONDETECTION_LIMITATION");
            Assert.That(limitation, Is.Not.Null);
            Assert.That(limitation.Source, Is.EqualTo(ObservationSource.Methodology));
            Assert.That(limitation.UnlockStage, Is.EqualTo(EvidenceUnlockStage.Always));
            Assert.That(caseDefinition.FindLimitation("L01_NONDETECTION_LIMITATION"), Is.Not.Null);
        }

        [Test, Category("LegacyReportCompatibility")]
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

        [Test, Category("LegacyReportCompatibility")]
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
                    "E01_SHARK_FEWER_SITES",
                    "E02_TUNA_FEWER_SITES",
                    "L01_NONDETECTION_LIMITATION",
                    "E07_FISHING_LINE"
                },
                environmentalObservations = new List<InvestigationExternalObservationData>
                {
                    new InvestigationExternalObservationData { observationId = "E01_SHARK_FEWER_SITES" }
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
            Assert.That(state.HasDiscoveredObservation("E01_SHARK_FEWER_SITES"), Is.True);
            Assert.That(state.HasDiscoveredObservation("E02_TUNA_FEWER_SITES"), Is.True);
            Assert.That(state.HasDiscoveredObservation("L01_NONDETECTION_LIMITATION"), Is.True);
            Assert.That(state.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);
        }

        [Test, Category("LegacyReportCompatibility")]
        public void LegacyReport_RequiresItsConfiguredEvidenceCategories()
        {
            InvestigationState state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, "longline", out _), Is.True);
            Assert.That(updater.TryReviewConfirmation(state, out _), Is.True);
            foreach (string id in new List<string>(state.SelectedReportEvidenceIds))
                Assert.That(updater.TrySetReportEvidence(state, id, false, out _), Is.True);
            CompleteFollowUpObjectives(state);
            Assert.That(updater.TrySetFinalThreat(state, "longline", out _), Is.True);
            Assert.That(updater.TrySetReasoning(state, "food_web_cascade", out _), Is.True);
            Assert.That(updater.TrySetLimitation(state, "L01_NONDETECTION_LIMITATION", out _), Is.True);
            foreach (string id in new[] { "E01_SHARK_FEWER_SITES", "E02_TUNA_FEWER_SITES", "E03_HERRING_WIDER_DETECTION", "E04_CORAL_NONDETECTION" })
                Assert.That(updater.TrySetReportEvidence(state, id, true, out _), Is.True);
            var missingConfirmation = updater.EvaluateReadiness(state);
            Assert.That(missingConfirmation.EvidenceComplete, Is.True);
            Assert.That(missingConfirmation.EvidenceCategoriesComplete, Is.False);
            Assert.That(missingConfirmation.MissingEvidenceCategory, Is.EqualTo(EvidenceCategory.Confirmation));
            Assert.That(missingConfirmation.CanSubmitFinal, Is.False);
            Assert.That(updater.TrySetReportEvidence(state, "E07_FISHING_LINE", true, out _), Is.True);
            Assert.That(updater.EvaluateReadiness(state).CanSubmitFinal, Is.True);
        }

        [Test]
        public void QaCheckpoint_ConclusionReady_UsesSurveyAndModelsWithoutRov()
        {
            InvestigationState manual = PrepareProvisionalReadyState();
            ReviewModelPair(manual);
            InvestigationState checkpoint = InvestigationQaStateFactory.Create(caseDefinition, InvestigationQaCheckpoint.ConclusionReady);
            Assert.That(CanonicalSnapshot(checkpoint), Is.EqualTo(CanonicalSnapshot(manual)));
            Assert.That(new InvestigationConclusionEvaluator().CanRecordModelConclusion(caseDefinition, checkpoint, "longline"), Is.True);
            Assert.That(checkpoint.ConfirmationReviewed, Is.False);
            Assert.That(updater.EvaluateReadiness(checkpoint).CanEnterProvisional, Is.True);
            Assert.That(new InvestigationCaseValidator().Validate(caseDefinition), Is.Empty);
            Assert.That(updater.EvaluateReadiness(checkpoint).CanSubmitFinal, Is.False, "The legacy full-report gate must still require ROV evidence");
        }

        private void ReviewModelPair(InvestigationState state, string selected = "bottom_trawling")
        {
            foreach (string id in caseDefinition.RequiredModelReviewIds)
                Assert.That(updater.TryReviewModelExplanation(state, id, out _), Is.True);
            Assert.That(updater.TryReviewModelExplanation(state, selected, out _), Is.True);
        }

        [TestCase("longline", "bottom_trawling")]
        [TestCase("bottom_trawling", "longline")]
        public void ModelConclusion_RequiresTwoDistinctReviewsInEitherOrder(string first, string second)
        {
            var state = PrepareProvisionalReadyState();
            Assert.That(updater.TryReviewModelExplanation(state, first, out _), Is.True);
            Assert.That(updater.TryReviewModelExplanation(state, first, out _), Is.True);
            Assert.That(state.ReviewedModelThreatIds.Count, Is.EqualTo(1));
            Assert.That(updater.SubmitModelConclusion(state, first).Status, Is.EqualTo(InvestigationConclusionStatus.InsufficientEvidence));
            Assert.That(state.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(updater.TryReviewModelExplanation(state, "plastic", out _), Is.False);
            Assert.That(updater.TryReviewModelExplanation(state, second, out _), Is.True);
            Assert.That(state.ReviewedModelThreatIds, Is.EquivalentTo(new[] { first, second }));
            Assert.That(updater.SubmitModelConclusion(state, caseDefinition.PrimaryModelThreatId).Status, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(updater.CreateInitialState().ReviewedModelThreatIds, Is.Empty);
        }

        [Test]
        public void ModelConclusion_RecordsOnlyObservedEvidenceWithoutWeakeningLegacyReportGate()
        {
            var state = PrepareProvisionalReadyState();
            updater.TrySubmitProvisional(state, "bottom_trawling", out _);
            Assert.That(updater.SubmitFinal(state).Status, Is.EqualTo(InvestigationConclusionStatus.InsufficientEvidence));
            ReviewModelPair(state);
            var result = updater.SubmitModelConclusion(state, "bottom_trawling");
            Assert.That(result.Status, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(state.DiscoveredObservationIds.Count, Is.EqualTo(6));
            Assert.That(state.SelectedReportEvidenceIds.Count, Is.EqualTo(6));
            Assert.That(state.ConfirmationReviewed, Is.False);
            Assert.That(state.SelectedReportEvidenceIds, Does.Not.Contain("E07_FISHING_LINE"));
            Assert.That(state.SelectedReportEvidenceIds, Does.Not.Contain("E08_SEAFLOOR_INTACT"));
            updater.SubmitModelConclusion(state, "bottom_trawling");
            Assert.That(state.FinalSubmissionAttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void ModelConclusion_RejectsIncompleteOrIncorrectModels()
        {
            var initial = updater.CreateInitialState();
            Assert.That(updater.SubmitModelConclusion(initial, "longline").Status, Is.EqualTo(InvestigationConclusionStatus.InsufficientEvidence));
            Assert.That(initial.FinalSubmissionAttemptCount, Is.Zero);
            var state = PrepareProvisionalReadyState(); ReviewModelPair(state); updater.TrySubmitProvisional(state, "plastic", out _);
            Assert.That(updater.SubmitModelConclusion(state, "plastic").Status, Is.EqualTo(InvestigationConclusionStatus.Incorrect));
            Assert.That(state.ConfirmationReviewed, Is.False);
            Assert.That(state.DiscoveredObservationIds.Count, Is.EqualTo(6));
        }

        [Test]
        public void FigmaConclusion_ValidatorRejectsMarkingTheContradictoryLongLineModelAsSupported()
        {
            var clone = UnityEngine.Object.Instantiate(caseDefinition);
            try
            {
                var data = new SerializedObject(clone);
                var supported = data.FindProperty("supportedModelThreatIds");
                supported.arraySize = 2; supported.GetArrayElementAtIndex(0).stringValue = "longline";
                supported.GetArrayElementAtIndex(1).stringValue = "bottom_trawling";
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(new InvestigationCaseValidator().Validate(clone),
                    Has.Some.Contains("Supported model list does not reflect the complete survey comparison for longline"));
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }

        [TestCase("longline")]
        [TestCase("bottom_trawling")]
        public void FigmaConclusion_ReviewsEitherOrderAndRecordsTheBestFittingModel(string cause)
        {
            var state = PrepareProvisionalReadyState();
            Assert.That(updater.TrySubmitProvisional(state, cause, out _), Is.True);
            ReviewModelPair(state, cause);
            var result = updater.SubmitModelConclusion(state, caseDefinition.PrimaryModelThreatId);
            Assert.That(result.Status, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(result.Feedback, Does.Contain("best fits").And.Contain("opposite tuna and herring changes"));
            Assert.That(caseDefinition.PrimaryModelThreatId, Is.EqualTo("bottom_trawling"));
            Assert.That(state.FinalThreatId, Is.EqualTo(caseDefinition.PrimaryModelThreatId));
            Assert.That(state.ConfirmationReviewed, Is.False);
            Assert.That(state.SelectedReportEvidenceIds, Is.EquivalentTo(new[] { "E01_SHARK_FEWER_SITES", "E02_TUNA_FEWER_SITES", "E03_HERRING_WIDER_DETECTION", "E04_CORAL_NONDETECTION", "E05_PHYTOPLANKTON_STABLE", "E09_KRILL_WIDER_DETECTION" }));
        }

        [Test]
        public void ModelConclusion_DoesNotExportLegacyConfirmationEvidence()
        {
            var state = PrepareCompleteReport("bottom_trawling");
            Assert.That(state.SelectedReportEvidenceIds, Does.Contain("E07_FISHING_LINE"));
            ReviewModelPair(state);
            Assert.That(updater.SubmitModelConclusion(state, "bottom_trawling").Status, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(state.SelectedReportEvidenceIds.Count, Is.EqualTo(6));
            Assert.That(state.SelectedReportEvidenceIds, Does.Not.Contain("E07_FISHING_LINE"));
            Assert.That(state.SelectedReportEvidenceIds, Does.Not.Contain("E08_SEAFLOOR_INTACT"));
        }

        [Test, Category("LegacyReportCompatibility")]
        public void WrongFinalReport_AddsAttemptAndMisstepBeforeCorrectRetry()
        {
            InvestigationState state = PrepareCompleteReport("plastic");
            InvestigationConclusionResult wrong = updater.SubmitFinal(state);
            Assert.That(wrong.Status, Is.EqualTo(InvestigationConclusionStatus.Incorrect));
            Assert.That(state.FinalSubmissionAttemptCount, Is.EqualTo(1));
            Assert.That(state.MisstepCount, Is.EqualTo(1));

            Assert.That(updater.TrySetFinalThreat(state, "bottom_trawling", out _), Is.True);
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
            input.discoveredObservationIds.AddRange(new[] { "E01_SHARK_FEWER_SITES", "E02_TUNA_FEWER_SITES",
                "E04_CORAL_NONDETECTION", "E03_HERRING_WIDER_DETECTION", "L01_NONDETECTION_LIMITATION" });
            Assert.That(updater.TryApplyExternalInput(state, input, out _), Is.True);
            Assert.That(state.DiscoveredObservationIds.Count, Is.EqualTo(5));
            Assert.That(InvestigationObserveEvaluator.CountFindings(caseDefinition, state), Is.EqualTo(4));
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);
            Assert.That(updater.TryRunThreat(state, "longline", out _, out _), Is.False);
            Discover(state, "E05_PHYTOPLANKTON_STABLE");
            Discover(state, "E05_PHYTOPLANKTON_STABLE");
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.False);
            Discover(state, "E09_KRILL_WIDER_DETECTION");
            Assert.That(InvestigationObserveEvaluator.CountFindings(caseDefinition, state), Is.EqualTo(6));
            Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.True);
        }

        [Test]
        public void ExternalSurvey_ConflictingCoreInputIsRejectedWithoutPartialMutation()
        {
            InvestigationState state = updater.CreateInitialState();
            string originalSurvey = state.SurveyId;
            InvestigationGameInput input = new InvestigationGameInput();
            input.surveyContext.surveyId = "should-not-be-applied";
            input.discoveredObservationIds.Add("E02_TUNA_FEWER_SITES");
            input.ednaResults.Add(new EDNAResultData { detectedSpeciesIds = new List<string> { "tree_bubblegum_coral", "moon_jellyfish" } });
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
            result.detectedSpeciesIds.Add("tree_bubblegum_coral");
            result.speciesObservations.Add(new EDNASpeciesObservationData { speciesId = "tree_bubblegum_coral", detectionState = SpeciesDetectionState.NotDetected });
            InvestigationGameInput input = new InvestigationGameInput();
            input.ednaResults.Add(result);
            Assert.That(updater.TryApplyExternalInput(state, input, out string feedback), Is.True, feedback);
            Assert.That(state.SurveySpeciesRecords, Has.Count.EqualTo(1));
            InvestigationSurveySummary current = InvestigationSurveyEvaluator.Resolve(caseDefinition, state, caseDefinition.FindSpecies("tree_bubblegum_coral"), SurveyTimepoint.Current);
            Assert.That(current.Detection, Is.EqualTo(SpeciesDetectionState.NotDetected));
            Assert.That(current.Result, Is.EqualTo(caseDefinition.FindObservation("E04_CORAL_NONDETECTION").DisplayName));
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
            RunModel(state, "bottom_trawling");
            PredictionComparisonRecord open = updater.Compare(state, "bottom_trawling", "shark", "E03_HERRING_WIDER_DETECTION", ComparisonJudgement.NotEnoughEvidence);
            Assert.That(open.IsAccepted, Is.True);
            InvestigationHypothesisSummary summary = new InvestigationHypothesisSummary(caseDefinition, state, "bottom_trawling");
            Assert.That(summary.OpenCount, Is.EqualTo(1));
            Assert.That(summary.SupportCount, Is.Zero);
            Assert.That(summary.ChallengeCount, Is.Zero);
            Assert.That(new InvestigationHypothesisSummary(caseDefinition, state, "plastic").Records, Is.Empty);
            updater.Compare(state, "bottom_trawling", "atlantic_herring", "E03_HERRING_WIDER_DETECTION", ComparisonJudgement.Match);
            Assert.That(new InvestigationHypothesisSummary(caseDefinition, state, "bottom_trawling").Records, Has.Count.EqualTo(2));
            updater.Compare(state, "bottom_trawling", "shark", "E01_SHARK_FEWER_SITES", ComparisonJudgement.Match);
            summary = new InvestigationHypothesisSummary(caseDefinition, state, "bottom_trawling");
            Assert.That(summary.SupportCount, Is.EqualTo(2));
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
            Assert.That(updater.TrySetReportEvidence(state, "E01_SHARK_FEWER_SITES", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E02_TUNA_FEWER_SITES", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E03_HERRING_WIDER_DETECTION", true, out _), Is.True);
            Assert.That(updater.TrySetReportEvidence(state, "E07_FISHING_LINE", true, out _), Is.True);
            Assert.That(updater.TrySetReasoning(state, "food_web_cascade", out _), Is.True);
            Assert.That(updater.TrySetLimitation(state, "L01_NONDETECTION_LIMITATION", out _), Is.True);
            return state;
        }

        private InvestigationState PrepareProvisionalReadyState()
        {
            var state = CreateSimulationReadyState();
            foreach (var threat in caseDefinition.Threats) if (!threat.OptionalExploration) RunModel(state, threat.ThreatId);
            foreach (var objective in caseDefinition.InvestigationObjectives)
                AssertObjective(updater.Compare(state, objective.ThreatId, objective.TargetId, objective.RequiredEvidenceId, objective.RequiredJudgement));
            return state;
        }

        private void CompleteFollowUpObjectives(InvestigationState state)
        {
            Assert.That(state.ConfirmationReviewed, Is.True);
            Assert.That(state.CompletedObjectiveCount, Is.EqualTo(7));
        }

        private InvestigationState CreateSimulationReadyState()
        {
            InvestigationState state = updater.CreateInitialState();
            DiscoverCoreObservations(state);
            return state;
        }

        private void DiscoverCoreObservations(InvestigationState state)
        {
            Discover(state, "E01_SHARK_FEWER_SITES");
            Discover(state, "E02_TUNA_FEWER_SITES");
            Discover(state, "E04_CORAL_NONDETECTION");
            Discover(state, "E03_HERRING_WIDER_DETECTION");
            Discover(state, "E05_PHYTOPLANKTON_STABLE");
            Discover(state, "E09_KRILL_WIDER_DETECTION");
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
            List<string> reviews = new List<string>(state.ReviewedModelThreatIds);
            reviews.Sort(StringComparer.Ordinal);
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
                string.Join(",", reviews),
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
