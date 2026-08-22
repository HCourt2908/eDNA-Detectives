using System;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.V2;
using EDNA.Investigation.V2.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EDNA.Investigation.V2.Editor
{
    public static class InvestigationV2Builder
    {
        private const string DataRoot = "Assets/Data/InvestigationV2/LongLineCase";
        private const string ArtRoot = "Assets/Art/InvestigationV2/OpenMoji";
        private const string StatusIconRoot = "Assets/Resources/InvestigationV2/Icons/Heroicons";
        private const string PrefabRoot = "Assets/Prefabs/InvestigationV2";
        private const string ScenePath = "Assets/Scenes/InvestigationSceneV2.unity";
        private const string PrefabPath = PrefabRoot + "/InvestigationV2Runtime.prefab";

        private readonly struct PredictionSpec
        {
            public PredictionSpec(string speciesId, PredictionState state, string rationale)
            {
                SpeciesId = speciesId;
                State = state;
                Rationale = rationale;
            }
            public string SpeciesId { get; }
            public PredictionState State { get; }
            public string Rationale { get; }
        }

        [MenuItem("eDNA Detectives/Build Investigation V2")]
        public static void CreateV2Project()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(PrefabRoot);
            AssetDatabase.Refresh();
            ConfigureArtworkImporters();

            InvestigationV2SpeciesDefinition shark = CreateSpecies(
                "SpeciesV2_Shark.asset", "shark", "Shark",
                "A large predator that normally controls tuna abundance around the seamount.",
                SpeciesGlyphKind.Shark,
                new[] { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep },
                "Broad range; current temperature alone does not explain a repeated all-depth non-detection.",
                new[] { "tuna" }, Array.Empty<string>(),
                new[] { "LargePredator", "LongLineSensitive", "TrawlBycatch" },
                new Vector2(0.22f, 0.80f));
            InvestigationV2SpeciesDefinition tuna = CreateSpecies(
                "SpeciesV2_Tuna.asset", "tuna", "Tuna",
                "A mobile fish that eats krill and is normally preyed on by sharks in this simplified food web.",
                SpeciesGlyphKind.Tuna,
                new[] { DepthBand.Shallow, DepthBand.Mid },
                "Warm-affinity visitor; distribution can also respond to predator removal.",
                new[] { "krill" }, new[] { "shark" },
                new[] { "Mobile", "WarmAffinity", "FoodWeb" },
                new Vector2(0.60f, 0.54f));
            InvestigationV2SpeciesDefinition krill = CreateSpecies(
                "SpeciesV2_Krill.asset", "krill", "Krill",
                "A small prey species linking plankton production to larger fish.",
                SpeciesGlyphKind.Krill,
                new[] { DepthBand.Mid, DepthBand.Deep },
                "Sensitive to several pressures; non-detection alone cannot identify the cause.",
                Array.Empty<string>(), new[] { "tuna" },
                new[] { "Prey", "FoodWeb", "PlasticSensitive" },
                new Vector2(0.33f, 0.30f));
            InvestigationV2SpeciesDefinition seaStar = CreateSpecies(
                "SpeciesV2_SeaStar.asset", "sea_star", "Sea star",
                "A benthic indicator used to test whether the seafloor community was disturbed.",
                SpeciesGlyphKind.SeaStar,
                new[] { DepthBand.Deep },
                "Broad temperature tolerance in this case.",
                Array.Empty<string>(), Array.Empty<string>(),
                new[] { "BenthicIndicator", "TrawlSensitive", "StableIndicator" },
                new Vector2(0.61f, 0.21f));
            InvestigationV2SpeciesDefinition mussel = CreateSpecies(
                "SpeciesV2_Mussel.asset", "mussel", "Filter-feeding mussel",
                "A filter feeder used as a plastic-sensitive comparison species.",
                SpeciesGlyphKind.Mussel,
                new[] { DepthBand.Mid, DepthBand.Deep },
                "Broad temperature tolerance; sensitive to suspended contaminants.",
                Array.Empty<string>(), Array.Empty<string>(),
                new[] { "FilterFeeder", "PlasticSensitive", "StableIndicator" },
                new Vector2(0.77f, 0.34f));

            InvestigationV2SpeciesDefinition[] species = { shark, tuna, krill, seaStar, mussel };
            ThreatSimulationDefinition warming = CreateThreat(
                "ThreatV2_Warming.asset", "warming", "Ocean warming",
                "Temperature change can shift distributions, but it should produce a coherent depth-shift pattern rather than only a predator cascade.",
                ThreatGlyphKind.Warming,
                new[]
                {
                    P("shark", PredictionState.DepthShift, "A warming explanation predicts redistribution before claiming disappearance."),
                    P("tuna", PredictionState.Increase, "Warm-affinity tuna may be detected at more sites."),
                    P("krill", PredictionState.Decrease, "Some krill signals may decline under changed conditions."),
                    P("sea_star", PredictionState.Unknown, "The benthic response is not determined by this simple model."),
                    P("mussel", PredictionState.Stable, "The reference mussel remains stable in this scenario.")
                },
                "Historical range exceeded or a consistent depth-shift pattern.",
                "Seafloor remains structurally intact.",
                "No fishing gear or trawl marks are required by this model.");
            ThreatSimulationDefinition plastic = CreateThreat(
                "ThreatV2_Plastic.asset", "plastic", "Plastic pollution",
                "Plastic pollution should affect sensitive filter feeders as well as prey signals; it does not predict a selective shark–tuna cascade.",
                ThreatGlyphKind.Plastic,
                new[]
                {
                    P("shark", PredictionState.Unknown, "The model cannot predict a direct shark response from this evidence."),
                    P("tuna", PredictionState.Stable, "Tuna are not expected to expand solely because of this plastic scenario."),
                    P("krill", PredictionState.Decrease, "Krill may decline under plastic exposure."),
                    P("sea_star", PredictionState.Stable, "The benthic indicator remains stable in this simplified scenario."),
                    P("mussel", PredictionState.Decrease, "A plastic-sensitive filter feeder should decline.")
                },
                "Temperature may remain normal.",
                "Seafloor structure remains intact.",
                "Plastic or contamination patterns may be present.");
            ThreatSimulationDefinition longLine = CreateThreat(
                "ThreatV2_LongLine.asset", "longline", "Long-line fishing",
                "Selective predator removal can trigger the shark–tuna–krill cascade without damaging the benthic community.",
                ThreatGlyphKind.LongLine,
                new[]
                {
                    P("shark", PredictionState.Decrease, "Long-line gear directly reduces the large predator."),
                    P("tuna", PredictionState.Increase, "Tuna increase after losing a predator."),
                    P("krill", PredictionState.Decrease, "More tuna consume more krill."),
                    P("sea_star", PredictionState.Stable, "Selective fishing does not directly damage the seafloor indicator."),
                    P("mussel", PredictionState.Stable, "The plastic-sensitive reference remains stable.")
                },
                "Temperature may remain within its historical range.",
                "Seafloor remains intact.",
                "Fishing gear may be recorded near predator habitat.");
            ThreatSimulationDefinition trawling = CreateThreat(
                "ThreatV2_BottomTrawling.asset", "bottom_trawling", "Bottom trawling",
                "This overlapping cause can create the same predator cascade, but it also predicts damage to benthic species and the seafloor.",
                ThreatGlyphKind.BottomTrawling,
                new[]
                {
                    P("shark", PredictionState.Decrease, "Bycatch reduces the same large predator."),
                    P("tuna", PredictionState.Increase, "Tuna increase after losing a predator."),
                    P("krill", PredictionState.Decrease, "More tuna consume more krill."),
                    P("sea_star", PredictionState.Decrease, "Dragging gear damages the benthic indicator."),
                    P("mussel", PredictionState.Stable, "The plastic-sensitive reference remains stable.")
                },
                "Temperature may remain within its historical range.",
                "Seafloor should be disturbed or damaged.",
                "Trawl marks or damaged habitat should be visible.");
            ThreatSimulationDefinition[] threats = { warming, plastic, longLine, trawling };

            InvestigationV2CaseDefinition caseDefinition = LoadOrCreate<InvestigationV2CaseDefinition>($"{DataRoot}/InvestigationCaseV2_LongLine.asset");
            SerializedObject caseObject = new SerializedObject(caseDefinition);
            SetString(caseObject, "caseId", "investigation_v2_longline_01");
            SetString(caseObject, "displayName", "The Missing Predator");
            SetString(caseObject, "briefing", "A modern eDNA survey shows a repeated shark non-detection, tuna at more sites, and repeated krill non-detection. Compare overlapping ecosystem models before writing a report.");
            SetObjectArray(caseObject, "species", species);
            SetObservations(caseObject);
            SetObjectArray(caseObject, "threats", threats);
            SetComparisonRules(caseObject, threats, species);
            SetInteger(caseObject, "minimumObserveDiscoveries", 4);
            SetStringArray(caseObject, "requiredComparedThreatIds", new[] { "longline", "bottom_trawling" });
            SetRequiredComparisonSpecies(caseObject);
            SetInteger(caseObject, "requiredComparisonsPerThreat", 2);
            SetInteger(caseObject, "minimumCompletedComparisons", 4);
            SetString(caseObject, "correctThreatId", "longline");
            SetStringArray(caseObject, "confirmationEvidenceIds", new[] { "E07_FISHING_LINE", "E08_SEAFLOOR_INTACT" });
            SetInteger(caseObject, "minimumReportEvidence", 2);
            SetInteger(caseObject, "minimumConfirmationEvidenceInReport", 1);
            SetInteger(caseObject, "minimumReportLimitations", 1);
            SetString(caseObject, "requiredReasoningId", "food_web_cascade");
            SetReasoningOptions(caseObject);
            SetLimitations(caseObject);
            SetString(caseObject, "successFeedback", "Case solved. Long-line fishing best explains the shared shark–tuna–krill cascade, while stable benthic eDNA and an intact seafloor challenge bottom trawling. The fishing line confirms the best-supported explanation without turning eDNA non-detection into proof of absence.");
            caseObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(caseDefinition);

            GameObject prefab = CreateRuntimePrefab();
            CreateScene(caseDefinition, prefab);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Investigation V2 generated at {ScenePath}");
        }

        public static void BuildFromCommandLine() => CreateV2Project();

        private static PredictionSpec P(string speciesId, PredictionState state, string rationale) => new PredictionSpec(speciesId, state, rationale);

        private static InvestigationV2SpeciesDefinition CreateSpecies(
            string fileName,
            string speciesId,
            string displayName,
            string description,
            SpeciesGlyphKind glyphKind,
            DepthBand[] depths,
            string temperature,
            string[] dietIds,
            string[] predatorIds,
            string[] sensitivityTags,
            Vector2 mapPosition)
        {
            InvestigationV2SpeciesDefinition asset = LoadOrCreate<InvestigationV2SpeciesDefinition>($"{DataRoot}/{fileName}");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "speciesId", speciesId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "description", description);
            serialized.FindProperty("icon").objectReferenceValue = LoadSpeciesIcon(speciesId);
            serialized.FindProperty("glyphKind").enumValueIndex = (int)glyphKind;
            SetEnumArray(serialized, "preferredDepths", depths);
            SetString(serialized, "temperaturePreference", temperature);
            SetStringArray(serialized, "dietSpeciesIds", dietIds);
            SetStringArray(serialized, "predatorSpeciesIds", predatorIds);
            SetStringArray(serialized, "sensitivityTags", sensitivityTags);
            serialized.FindProperty("mapPosition").vector2Value = mapPosition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ThreatSimulationDefinition CreateThreat(
            string fileName,
            string threatId,
            string displayName,
            string summary,
            ThreatGlyphKind glyphKind,
            PredictionSpec[] predictions,
            string temperature,
            string seafloor,
            string physical)
        {
            ThreatSimulationDefinition asset = LoadOrCreate<ThreatSimulationDefinition>($"{DataRoot}/{fileName}");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "threatId", threatId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "summary", summary);
            serialized.FindProperty("icon").objectReferenceValue = LoadThreatIcon(threatId);
            serialized.FindProperty("glyphKind").enumValueIndex = (int)glyphKind;
            SerializedProperty predictionArray = serialized.FindProperty("speciesPredictions");
            predictionArray.arraySize = predictions.Length;
            for (int index = 0; index < predictions.Length; index++)
            {
                SerializedProperty property = predictionArray.GetArrayElementAtIndex(index);
                property.FindPropertyRelative("speciesId").stringValue = predictions[index].SpeciesId;
                property.FindPropertyRelative("predictedState").enumValueIndex = (int)predictions[index].State;
                property.FindPropertyRelative("rationale").stringValue = predictions[index].Rationale;
            }
            SetString(serialized, "temperaturePrediction", temperature);
            SetString(serialized, "seafloorPrediction", seafloor);
            SetString(serialized, "physicalConfirmation", physical);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void ConfigureArtworkImporters()
        {
            string[] artworkFiles =
            {
                "shark.png",
                "tuna.png",
                "krill.png",
                "sea-star.png",
                "mussel.png",
                "warming.png",
                "plastic.png",
                "long-line.png",
                "bottom-trawling.png"
            };

            for (int index = 0; index < artworkFiles.Length; index++)
            {
                ConfigureSpriteImporter($"{ArtRoot}/{artworkFiles[index]}");
            }

            string[] statusIcons = { "check-circle.png", "x-circle.png", "question-mark-circle.png" };
            for (int index = 0; index < statusIcons.Length; index++)
                ConfigureSpriteImporter($"{StatusIconRoot}/{statusIcons[index]}");
        }

        private static void ConfigureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Investigation V2 artwork is missing or not importable: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSpeciesIcon(string speciesId)
        {
            switch (speciesId)
            {
                case "shark": return LoadIcon("shark.png");
                case "tuna": return LoadIcon("tuna.png");
                case "krill": return LoadIcon("krill.png");
                case "sea_star": return LoadIcon("sea-star.png");
                case "mussel": return LoadIcon("mussel.png");
                default: return null;
            }
        }

        private static Sprite LoadThreatIcon(string threatId)
        {
            switch (threatId)
            {
                case "warming": return LoadIcon("warming.png");
                case "plastic": return LoadIcon("plastic.png");
                case "longline": return LoadIcon("long-line.png");
                case "bottom_trawling": return LoadIcon("bottom-trawling.png");
                default: return null;
            }
        }

        private static Sprite LoadIcon(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{fileName}");
        }

        private static void SetObservations(SerializedObject caseObject)
        {
            SerializedProperty array = caseObject.FindProperty("observations");
            array.arraySize = 9;
            SetObservation(array.GetArrayElementAtIndex(0), "E01_SHARK_NONDETECTION", "Shark repeatedly not detected", "Shark DNA was not detected in several high-quality samples across the surveyed depths.", "shark", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, V2EvidenceConfidence.High, ObservationClaimType.NotDetected, "Repeated high-quality non-detection is stronger than one sample, but it still does not prove absence.");
            SetObservation(array.GetArrayElementAtIndex(1), "E02_TUNA_WIDER_DETECTION", "Tuna detected at more sites", "Tuna DNA was detected across more survey locations than in the historical baseline.", "tuna", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, V2EvidenceConfidence.Medium, ObservationClaimType.ChangedDepthOrDistribution, "Wider detection is consistent with expansion but does not directly measure abundance.");
            SetObservation(array.GetArrayElementAtIndex(2), "E03_KRILL_NONDETECTION", "Krill repeatedly not detected", "Krill DNA was not detected in several high-quality samples where it was historically expected.", "krill", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, V2EvidenceConfidence.High, ObservationClaimType.NotDetected, "The pattern supports a decline hypothesis but does not prove a population count.");
            SetObservation(array.GetArrayElementAtIndex(3), "E04_BENTHIC_STABLE", "Sea star remains stable", "The benthic indicator was repeatedly detected at its historical deep sites.", "sea_star", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, V2EvidenceConfidence.Medium, ObservationClaimType.MatchesBaseline, "Repeated detection across the same sites supports stability, pending ROV confirmation of habitat condition.");
            SetObservation(array.GetArrayElementAtIndex(4), "E05_TEMPERATURE_NORMAL", "Temperature remains in the historical range", "The multi-depth CTD profile remains within the historical range and no coherent depth-shift pattern appears.", "shark", ObservationSource.CTDLog, EvidenceUnlockStage.OnThreatRun, "warming", V2EvidenceConfidence.Medium, ObservationClaimType.EnvironmentalReading, "A normal profile challenges warming but cannot rule it out alone.");
            SetObservation(array.GetArrayElementAtIndex(5), "E06_PLASTIC_INDICATOR_STABLE", "Filter-feeding mussel remains stable", "The plastic-sensitive reference species remains detected at its historical sites.", "mussel", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, V2EvidenceConfidence.Medium, ObservationClaimType.MatchesBaseline, "This challenges a broad plastic-impact pattern but cannot rule it out alone.");
            SetObservation(array.GetArrayElementAtIndex(6), "E07_FISHING_LINE", "Fishing line recorded near shark habitat", "ROV footage shows fishing line near the area where sharks were historically recorded.", "shark", ObservationSource.ROV, EvidenceUnlockStage.AfterProvisional, string.Empty, V2EvidenceConfidence.High, ObservationClaimType.PhysicalObservation, "A physical gear observation confirms an already-developed long-line hypothesis.");
            SetObservation(array.GetArrayElementAtIndex(7), "E08_SEAFLOOR_INTACT", "Seafloor remains intact", "ROV footage shows no obvious trawl marks or broad habitat damage.", "sea_star", ObservationSource.ROV, EvidenceUnlockStage.AfterProvisional, string.Empty, V2EvidenceConfidence.High, ObservationClaimType.PhysicalObservation, "The intact habitat strongly challenges bottom trawling alongside the stable benthic eDNA pattern.");
            SetObservation(array.GetArrayElementAtIndex(8), "L01_NONDETECTION_LIMITATION", "Not detected does not mean gone", "eDNA non-detection does not prove complete absence; sampling and detection limits remain.", string.Empty, ObservationSource.Methodology, EvidenceUnlockStage.Always, string.Empty, V2EvidenceConfidence.High, ObservationClaimType.MethodologicalLimitation, "This scientific limitation is always available in the final report.");
        }

        private static void SetObservation(
            SerializedProperty property,
            string id,
            string displayName,
            string detail,
            string speciesId,
            ObservationSource source,
            EvidenceUnlockStage unlockStage,
            string unlockThreatId,
            V2EvidenceConfidence confidence,
            ObservationClaimType claimType,
            string confidenceReason)
        {
            property.FindPropertyRelative("evidenceId").stringValue = id;
            property.FindPropertyRelative("displayName").stringValue = displayName;
            property.FindPropertyRelative("detail").stringValue = detail;
            property.FindPropertyRelative("relatedSpeciesId").stringValue = speciesId;
            property.FindPropertyRelative("source").enumValueIndex = (int)source;
            property.FindPropertyRelative("unlockStage").enumValueIndex = (int)unlockStage;
            property.FindPropertyRelative("unlockThreatId").stringValue = unlockThreatId;
            property.FindPropertyRelative("confidence").enumValueIndex = (int)confidence;
            property.FindPropertyRelative("claimType").enumValueIndex = (int)claimType;
            property.FindPropertyRelative("confidenceReason").stringValue = confidenceReason;
        }

        private static void SetComparisonRules(
            SerializedObject caseObject,
            IReadOnlyList<ThreatSimulationDefinition> threats,
            IReadOnlyList<InvestigationV2SpeciesDefinition> species)
        {
            SerializedProperty rules = caseObject.FindProperty("comparisonRules");
            rules.arraySize = threats.Count * species.Count;
            int ruleIndex = 0;
            for (int threatIndex = 0; threatIndex < threats.Count; threatIndex++)
            {
                ThreatSimulationDefinition threat = threats[threatIndex];
                for (int speciesIndex = 0; speciesIndex < species.Count; speciesIndex++)
                {
                    InvestigationV2SpeciesDefinition speciesDefinition = species[speciesIndex];
                    PredictionState predictedState = threat.FindPrediction(speciesDefinition.SpeciesId).PredictedState;
                    SerializedProperty rule = rules.GetArrayElementAtIndex(ruleIndex++);
                    rule.FindPropertyRelative("threatId").stringValue = threat.ThreatId;
                    rule.FindPropertyRelative("speciesId").stringValue = speciesDefinition.SpeciesId;
                    string[] candidates = CandidateEvidence(speciesDefinition.SpeciesId);
                    SerializedProperty options = rule.FindPropertyRelative("observationOptions");
                    options.arraySize = candidates.Length;
                    for (int optionIndex = 0; optionIndex < candidates.Length; optionIndex++)
                    {
                        SerializedProperty option = options.GetArrayElementAtIndex(optionIndex);
                        option.FindPropertyRelative("evidenceId").stringValue = candidates[optionIndex];
                        SetJudgementResolutions(
                            option.FindPropertyRelative("resolutions"),
                            speciesDefinition.SpeciesId,
                            predictedState,
                            candidates[optionIndex]);
                    }
                }
            }
        }

        private static string[] CandidateEvidence(string speciesId)
        {
            switch (speciesId)
            {
                case "shark": return new[] { "E01_SHARK_NONDETECTION", "E04_BENTHIC_STABLE", "E02_TUNA_WIDER_DETECTION" };
                case "tuna": return new[] { "E02_TUNA_WIDER_DETECTION", "E01_SHARK_NONDETECTION", "E04_BENTHIC_STABLE" };
                case "krill": return new[] { "E03_KRILL_NONDETECTION", "E02_TUNA_WIDER_DETECTION", "E06_PLASTIC_INDICATOR_STABLE" };
                case "sea_star": return new[] { "E04_BENTHIC_STABLE", "E03_KRILL_NONDETECTION", "E01_SHARK_NONDETECTION" };
                case "mussel": return new[] { "E06_PLASTIC_INDICATOR_STABLE", "E03_KRILL_NONDETECTION", "E04_BENTHIC_STABLE" };
                default: return new[] { "E01_SHARK_NONDETECTION", "E02_TUNA_WIDER_DETECTION" };
            }
        }

        private static void SetJudgementResolutions(
            SerializedProperty resolutions,
            string predictionSpeciesId,
            PredictionState predictedState,
            string evidenceId)
        {
            resolutions.arraySize = 3;
            string observationSpeciesId = EvidenceSpecies(evidenceId);
            bool sameSpecies = string.Equals(predictionSpeciesId, observationSpeciesId, StringComparison.Ordinal);
            for (int index = 0; index < 3; index++)
            {
                ComparisonJudgement judgement = (ComparisonJudgement)index;
                ComparisonEvaluationOutcome outcome;
                string feedback;
                ResolveJudgement(sameSpecies, predictedState, evidenceId, judgement, out outcome, out feedback);
                SerializedProperty resolution = resolutions.GetArrayElementAtIndex(index);
                resolution.FindPropertyRelative("judgement").enumValueIndex = (int)judgement;
                resolution.FindPropertyRelative("outcome").enumValueIndex = (int)outcome;
                resolution.FindPropertyRelative("feedback").stringValue = feedback;
            }
        }

        private static void ResolveJudgement(
            bool sameSpecies,
            PredictionState predictedState,
            string evidenceId,
            ComparisonJudgement judgement,
            out ComparisonEvaluationOutcome outcome,
            out string feedback)
        {
            if (!sameSpecies)
            {
                outcome = judgement == ComparisonJudgement.NotEnoughEvidence
                    ? ComparisonEvaluationOutcome.Accepted
                    : ComparisonEvaluationOutcome.Incorrect;
                feedback = judgement == ComparisonJudgement.NotEnoughEvidence
                    ? "Reasonable, but this unrelated observation does not complete the comparison. Choose evidence that directly tests the predicted species to make progress."
                    : "This observation may be part of the wider food web, but it does not directly match or contradict this species prediction.";
                return;
            }

            bool nonDetection = evidenceId == "E01_SHARK_NONDETECTION" || evidenceId == "E03_KRILL_NONDETECTION";
            bool widerDetection = evidenceId == "E02_TUNA_WIDER_DETECTION";
            bool stableDetection = evidenceId == "E04_BENTHIC_STABLE" || evidenceId == "E06_PLASTIC_INDICATOR_STABLE";

            if (predictedState == PredictionState.Unknown)
            {
                outcome = judgement == ComparisonJudgement.Mismatch
                    ? ComparisonEvaluationOutcome.AcceptedWithCaveat
                    : ComparisonEvaluationOutcome.Incorrect;
                feedback = judgement == ComparisonJudgement.Mismatch
                    ? "Accepted with a caveat: the observation is clear, but this model offers no directional explanation for it."
                    : judgement == ComparisonJudgement.NotEnoughEvidence
                        ? "The observation directly concerns this species. Judge whether an Unknown prediction explains that observed pattern."
                        : "An Unknown prediction does not directly match a clear directional observation.";
                return;
            }

            if (predictedState == PredictionState.DepthShift)
            {
                outcome = judgement == ComparisonJudgement.Mismatch && nonDetection
                        ? ComparisonEvaluationOutcome.AcceptedWithCaveat
                        : ComparisonEvaluationOutcome.Incorrect;
                feedback = outcome == ComparisonEvaluationOutcome.AcceptedWithCaveat
                        ? "Accepted with a caveat: repeated all-depth non-detection challenges a simple depth-shift prediction."
                        : judgement == ComparisonJudgement.NotEnoughEvidence
                            ? "This repeated, high-quality observation directly tests the predicted shark pattern. Decide whether it matches or challenges a depth shift."
                            : "A depth shift needs evidence from different depths; this observation does not directly match it.";
                return;
            }

            bool observedDirectionMatches = predictedState == PredictionState.Decrease && nonDetection
                || predictedState == PredictionState.Increase && widerDetection
                || predictedState == PredictionState.Stable && stableDetection;
            bool observedDirectionConflicts = predictedState == PredictionState.Decrease && stableDetection
                || predictedState == PredictionState.Increase && (stableDetection || nonDetection)
                || predictedState == PredictionState.Stable && (nonDetection || widerDetection);

            if (observedDirectionMatches)
            {
                if (judgement == ComparisonJudgement.Match)
                {
                    outcome = nonDetection || widerDetection ? ComparisonEvaluationOutcome.AcceptedWithCaveat : ComparisonEvaluationOutcome.Accepted;
                    feedback = nonDetection
                        ? "Accepted with a caveat: repeated non-detection is consistent with decrease but does not prove abundance or complete absence."
                        : widerDetection
                            ? "Accepted with a caveat: wider detection is consistent with increase but is not a direct population count."
                            : "Accepted: the repeated stable observation matches the Stable model prediction.";
                    return;
                }
            }
            else if (observedDirectionConflicts && judgement == ComparisonJudgement.Mismatch)
            {
                outcome = ComparisonEvaluationOutcome.Accepted;
                feedback = "Accepted: the observation conflicts with the model's predicted direction.";
                return;
            }

            outcome = ComparisonEvaluationOutcome.Incorrect;
            feedback = judgement == ComparisonJudgement.NotEnoughEvidence
                ? "This observation directly concerns the predicted species. Use Match or Mismatch to compare its direction, while keeping the stated scientific caveat."
                : "Try again. Compare the model direction with what the observation can actually support.";
        }

        private static string EvidenceSpecies(string evidenceId)
        {
            switch (evidenceId)
            {
                case "E01_SHARK_NONDETECTION": return "shark";
                case "E02_TUNA_WIDER_DETECTION": return "tuna";
                case "E03_KRILL_NONDETECTION": return "krill";
                case "E04_BENTHIC_STABLE": return "sea_star";
                case "E06_PLASTIC_INDICATOR_STABLE": return "mussel";
                default: return string.Empty;
            }
        }

        private static void SetLimitations(SerializedObject caseObject)
        {
            SerializedProperty limitations = caseObject.FindProperty("limitations");
            limitations.arraySize = 2;
            SetLimitation(limitations.GetArrayElementAtIndex(0), "L01_NONDETECTION_LIMITATION", "Not detected does not mean gone", "eDNA non-detection does not prove complete absence.");
            SetLimitation(limitations.GetArrayElementAtIndex(1), "L02_SURVEY_COVERAGE", "Only three sites were surveyed", "The survey did not cover every place or time.");
        }

        private static void SetRequiredComparisonSpecies(SerializedObject caseObject)
        {
            SerializedProperty requirements = caseObject.FindProperty("requiredComparisonSpecies");
            requirements.arraySize = 2;
            SetRequiredComparisonSpecies(requirements.GetArrayElementAtIndex(0), "longline", new[] { "sea_star" });
            SetRequiredComparisonSpecies(requirements.GetArrayElementAtIndex(1), "bottom_trawling", new[] { "sea_star" });
        }

        private static void SetRequiredComparisonSpecies(SerializedProperty property, string threatId, IReadOnlyList<string> speciesIds)
        {
            property.FindPropertyRelative("threatId").stringValue = threatId;
            SerializedProperty species = property.FindPropertyRelative("requiredComparisonSpeciesIds");
            species.arraySize = speciesIds.Count;
            for (int index = 0; index < speciesIds.Count; index++) species.GetArrayElementAtIndex(index).stringValue = speciesIds[index];
        }

        private static void SetReasoningOptions(SerializedObject caseObject)
        {
            SerializedProperty options = caseObject.FindProperty("reasoningOptions");
            options.arraySize = 3;
            SetReasoning(options.GetArrayElementAtIndex(0), "food_web_cascade", "Fewer sharks → more tuna → fewer krill", "Removing the predator lets tuna expand, increasing predation on krill.");
            SetReasoning(options.GetArrayElementAtIndex(1), "shared_habitat_shift", "All three species moved when their habitat shifted", "A shared habitat shift would need a coherent environmental or depth pattern.");
            SetReasoning(options.GetArrayElementAtIndex(2), "direct_fishing_loss", "Fishing directly removed sharks, tuna, and krill", "Selective long-line fishing does not directly remove every level of this food web.");
        }

        private static void SetReasoning(SerializedProperty property, string id, string name, string explanation)
        {
            property.FindPropertyRelative("reasoningId").stringValue = id;
            property.FindPropertyRelative("displayName").stringValue = name;
            property.FindPropertyRelative("explanation").stringValue = explanation;
        }

        private static void SetLimitation(SerializedProperty property, string id, string name, string explanation)
        {
            property.FindPropertyRelative("limitationId").stringValue = id;
            property.FindPropertyRelative("displayName").stringValue = name;
            property.FindPropertyRelative("explanation").stringValue = explanation;
        }

        private static GameObject CreateRuntimePrefab()
        {
            GameObject root = new GameObject(
                "Investigation V2 Runtime",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(InvestigationV2Controller),
                typeof(InvestigationV2RuntimeView));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200f, 760f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void CreateScene(InvestigationV2CaseDefinition caseDefinition, GameObject viewPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "InvestigationSceneV2";
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = InvestigationV2Theme.Background;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject bootstrapObject = new GameObject("Investigation V2 Demo", typeof(InvestigationV2DemoBootstrap));
            SerializedObject bootstrap = new SerializedObject(bootstrapObject.GetComponent<InvestigationV2DemoBootstrap>());
            bootstrap.FindProperty("caseDefinition").objectReferenceValue = caseDefinition;
            bootstrap.FindProperty("viewPrefab").objectReferenceValue = viewPrefab;
            bootstrap.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int index = 0; index < scenes.Count; index++)
            {
                if (string.Equals(scenes[index].path, ScenePath, StringComparison.Ordinal))
                {
                    scenes[index].enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            serialized.FindProperty(propertyName).stringValue = value ?? string.Empty;
        }

        private static void SetInteger(SerializedObject serialized, string propertyName, int value)
        {
            serialized.FindProperty(propertyName).intValue = value;
        }

        private static void SetStringArray(SerializedObject serialized, string propertyName, IReadOnlyList<string> values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++) property.GetArrayElementAtIndex(index).stringValue = values[index];
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, IReadOnlyList<UnityEngine.Object> values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void SetEnumArray<T>(SerializedObject serialized, string propertyName, IReadOnlyList<T> values) where T : Enum
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++) property.GetArrayElementAtIndex(index).enumValueIndex = Convert.ToInt32(values[index]);
        }
    }
}
