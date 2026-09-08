using System;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation;
using EDNA.Investigation.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EDNA.Investigation.Editor
{
    public static class InvestigationBuilder
    {
        private const string DataRoot = "Assets/Data/Investigation/LongLineCase";
        private const string ArtRoot = "Assets/Art/Investigation/OpenMoji";
        private const string FieldGuideArtRoot = "Assets/Art/Investigation/FieldGuide";
        private const string SeamountSpritePath = "Assets/Art/Investigation/Seamount/seamount_hero.png";
        private const string StatusIconRoot = "Assets/Resources/Investigation/Icons/Heroicons";
        private const string PrefabRoot = "Assets/Prefabs/Investigation";
        private const string ScenePath = "Assets/Scenes/InvestigationScene.unity";
        private const string PrefabPath = PrefabRoot + "/InvestigationRuntime.prefab";

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

        [MenuItem("eDNA Detectives/Build Investigation")]
        public static void CreateProject()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(PrefabRoot);
            AssetDatabase.Refresh();
            ConfigureArtworkImporters();

            InvestigationSpeciesDefinition shark = CreateSpecies(
                "Species_Shark.asset", "shark", "Shark",
                "A large predator that normally controls tuna abundance around the seamount.",
                SpeciesGlyphKind.Shark,
                new[] { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep },
                new[] { "tuna" }, Array.Empty<string>(),
                new[] { "LargePredator", "LongLineSensitive", "TrawlBycatch" },
                DepthBand.Shallow);
            InvestigationSpeciesDefinition tuna = CreateSpecies(
                "Species_Tuna.asset", "tuna", "Tuna",
                "A mobile fish that eats krill and is normally preyed on by sharks in this simplified food web.",
                SpeciesGlyphKind.Tuna,
                new[] { DepthBand.Shallow, DepthBand.Mid },
                new[] { "krill" }, new[] { "shark" },
                new[] { "Mobile", "FoodWeb" },
                DepthBand.Shallow);
            InvestigationSpeciesDefinition krill = CreateSpecies(
                "Species_Krill.asset", "krill", "Krill",
                "A small prey species linking plankton production to larger fish.",
                SpeciesGlyphKind.Krill,
                new[] { DepthBand.Mid, DepthBand.Deep },
                Array.Empty<string>(), new[] { "tuna" },
                new[] { "Prey", "FoodWeb", "PlasticSensitive" },
                DepthBand.Mid);
            InvestigationSpeciesDefinition seaStar = CreateSpecies(
                "Species_SeaStar.asset", "sea_star", "Sea star",
                "A benthic indicator used to test whether the seafloor community was disturbed.",
                SpeciesGlyphKind.SeaStar,
                new[] { DepthBand.Deep },
                Array.Empty<string>(), Array.Empty<string>(),
                new[] { "BenthicIndicator", "TrawlSensitive", "StableIndicator" },
                DepthBand.Deep);
            InvestigationSpeciesDefinition mussel = CreateSpecies(
                "Species_Mussel.asset", "mussel", "Filter-feeding mussel",
                "A filter feeder used as a plastic-sensitive comparison species.",
                SpeciesGlyphKind.Mussel,
                new[] { DepthBand.Mid, DepthBand.Deep },
                Array.Empty<string>(), Array.Empty<string>(),
                new[] { "FilterFeeder", "PlasticSensitive", "StableIndicator" },
                DepthBand.Deep);

            InvestigationSpeciesDefinition[] species = { shark, tuna, krill, seaStar, mussel };
            InvestigationSpeciesDefinition[] speciesCatalog = CreateCanonicalSpeciesCatalog(shark, tuna, krill);
            ThreatSimulationDefinition plastic = CreateThreat(
                "Threat_Plastic.asset", "plastic", "Plastic pollution",
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
                "Seafloor structure remains intact.",
                "Plastic or contamination patterns may be present.");
            ThreatSimulationDefinition longLine = CreateThreat(
                "Threat_LongLine.asset", "longline", "Long-line fishing",
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
                "Seafloor remains intact.",
                "Fishing gear may be recorded near predator habitat.");
            ThreatSimulationDefinition trawling = CreateThreat(
                "Threat_BottomTrawling.asset", "bottom_trawling", "Bottom trawling",
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
                "Seafloor should be disturbed or damaged.",
                "Trawl marks or damaged habitat should be visible.");
            ThreatSimulationDefinition[] threats = { plastic, longLine, trawling };

            InvestigationCaseDefinition caseDefinition = LoadOrCreate<InvestigationCaseDefinition>($"{DataRoot}/InvestigationCase_LongLine.asset");
            SerializedObject caseObject = new SerializedObject(caseDefinition);
            SetString(caseObject, "caseId", "investigation_longline_01");
            SetString(caseObject, "displayName", "The Missing Predator");
            SetString(caseObject, "briefing", "A modern eDNA survey shows a repeated shark non-detection, tuna at more sites, and repeated krill non-detection. Compare overlapping ecosystem models before writing a report.");
            SetSurveyContext(
                caseObject,
                "survey_12",
                "Survey 12",
                "seamount_a",
                "Seamount A",
                "Processed eDNA results from shallow, mid and deep samples");
            SetObjectArray(caseObject, "species", species);
            SetObjectArray(caseObject, "speciesCatalog", speciesCatalog);
            SetStringArray(caseObject, "foodWebChainSpeciesIds", new[] { "shark", "tuna", "krill" });
            SetString(caseObject, "simulationFoodWebId", "case_simplified");
            SetFoodWebEdges(caseObject);
            SetStringArray(caseObject, "benthicIndicatorSpeciesIds", new[] { "sea_star", "mussel" });
            SetStringArray(caseObject, "followUpLockedSpeciesIds", Array.Empty<string>());
            SetInteger(caseObject, "maximumSurveySpecies", 7);
            SetObservations(caseObject);
            SetObjectArray(caseObject, "threats", threats);
            SetComparisonRules(caseObject, threats, species);
            SetInvestigationObjectives(caseObject);
            SetInteger(caseObject, "minimumObserveDiscoveries", 5);
            SetStringArray(caseObject, "requiredComparedThreatIds", new[] { "longline", "bottom_trawling" });
            SetRequiredComparisonSpecies(caseObject);
            SetInteger(caseObject, "requiredComparisonsPerThreat", 2);
            SetString(caseObject, "correctThreatId", "longline");
            SetStringArray(caseObject, "confirmationEvidenceIds", new[] { "E07_FISHING_LINE", "E08_SEAFLOOR_INTACT" });
            SetInteger(caseObject, "minimumReportEvidence", 4);
            SetEvidenceCategoryRequirements(caseObject);
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
            Debug.Log($"Investigation generated at {ScenePath}");
        }

        public static void BuildFromCommandLine() => CreateProject();

        [MenuItem("eDNA Detectives/Update Species Catalog")]
        public static void UpdateSpeciesCatalog()
        {
            EnsureFolder(DataRoot);
            InvestigationSpeciesDefinition shark = AssetDatabase.LoadAssetAtPath<InvestigationSpeciesDefinition>($"{DataRoot}/Species_Shark.asset");
            InvestigationSpeciesDefinition tuna = AssetDatabase.LoadAssetAtPath<InvestigationSpeciesDefinition>($"{DataRoot}/Species_Tuna.asset");
            InvestigationSpeciesDefinition krill = AssetDatabase.LoadAssetAtPath<InvestigationSpeciesDefinition>($"{DataRoot}/Species_Krill.asset");
            InvestigationCaseDefinition caseDefinition = AssetDatabase.LoadAssetAtPath<InvestigationCaseDefinition>($"{DataRoot}/InvestigationCase_LongLine.asset");
            if (shark == null || tuna == null || krill == null || caseDefinition == null)
            {
                Debug.LogError("Build the investigation case before updating its shared species catalog.");
                return;
            }

            InvestigationSpeciesDefinition[] catalog = CreateCanonicalSpeciesCatalog(shark, tuna, krill);
            SerializedObject caseObject = new SerializedObject(caseDefinition);
            SetObjectArray(caseObject, "speciesCatalog", catalog);
            SetStringArray(caseObject, "foodWebChainSpeciesIds", new[] { "shark", "tuna", "krill" });
            SetString(caseObject, "simulationFoodWebId", "case_simplified");
            SetFoodWebEdges(caseObject);
            SetStringArray(caseObject, "benthicIndicatorSpeciesIds", new[] { "sea_star", "mussel" });
            SetStringArray(caseObject, "followUpLockedSpeciesIds", Array.Empty<string>());
            SetInteger(caseObject, "maximumSurveySpecies", 7);
            caseObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(caseDefinition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Investigation species catalog updated with 20 canonical entries.");
        }

        [MenuItem("eDNA Detectives/Remove Warming Scenario")]
        public static void RemoveWarmingScenario()
        {
            InvestigationCaseDefinition caseDefinition = AssetDatabase.LoadAssetAtPath<InvestigationCaseDefinition>($"{DataRoot}/InvestigationCase_LongLine.asset");
            if (caseDefinition == null) throw new InvalidOperationException("The investigation case is missing.");

            SerializedObject caseObject = new SerializedObject(caseDefinition);
            SerializedProperty threats = caseObject.FindProperty("threats");
            for (int index = threats.arraySize - 1; index >= 0; index--)
            {
                SerializedProperty entry = threats.GetArrayElementAtIndex(index);
                ThreatSimulationDefinition threat = entry.objectReferenceValue as ThreatSimulationDefinition;
                if (threat == null || threat.ThreatId != "warming") continue;
                entry.objectReferenceValue = null;
                threats.DeleteArrayElementAtIndex(index);
            }
            foreach (string propertyName in new[] { "comparisonRules", "investigationObjectives" })
            {
                SerializedProperty entries = caseObject.FindProperty(propertyName);
                for (int index = entries.arraySize - 1; index >= 0; index--)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                    // Target ID 1 belonged to the retired temperature prediction.
                    if (entry.FindPropertyRelative("threatId").stringValue == "warming"
                        || entry.FindPropertyRelative("targetKind").intValue == 1)
                        entries.DeleteArrayElementAtIndex(index);
                }
            }
            SerializedProperty objectives = caseObject.FindProperty("investigationObjectives");
            int requiredCount = 0;
            for (int index = 0; index < objectives.arraySize; index++)
                if (objectives.GetArrayElementAtIndex(index).FindPropertyRelative("required").boolValue) requiredCount++;
            SetInteger(caseObject, "minimumCompletedComparisons", requiredCount);
            caseObject.ApplyModifiedProperties();

            var migratedPaths = new List<string> { AssetDatabase.GetAssetPath(caseDefinition) };
            foreach (string guid in AssetDatabase.FindAssets("t:InvestigationSpeciesDefinition", new[] { DataRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                migratedPaths.Add(path);
                InvestigationSpeciesDefinition species = AssetDatabase.LoadAssetAtPath<InvestigationSpeciesDefinition>(path);
                SerializedObject speciesObject = new SerializedObject(species);
                SerializedProperty tags = speciesObject.FindProperty("sensitivityTags");
                for (int index = tags.arraySize - 1; index >= 0; index--)
                    if (tags.GetArrayElementAtIndex(index).stringValue == "WarmAffinity") tags.DeleteArrayElementAtIndex(index);
                speciesObject.ApplyModifiedProperties();
            }
            foreach (ThreatSimulationDefinition threat in caseDefinition.Threats)
                migratedPaths.Add(AssetDatabase.GetAssetPath(threat));
            AssetDatabase.SaveAssets();
            // Reserialize only the affected definitions to remove retired fields.
            AssetDatabase.ForceReserializeAssets(migratedPaths, ForceReserializeAssetsOptions.ReserializeAssets);
            AssetDatabase.DeleteAsset($"{DataRoot}/Threat_Warming.asset");
            AssetDatabase.DeleteAsset($"{ArtRoot}/warming.png");
            AssetDatabase.SaveAssets();

            List<string> errors = new InvestigationCaseValidator().Validate(caseDefinition);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Debug.Log($"Warming removed: {caseDefinition.Threats.Count} models and {requiredCount} required comparisons remain.");
        }

        [MenuItem("eDNA Detectives/Update Case Evidence")]
        public static void UpdateCaseEvidence()
        {
            InvestigationCaseDefinition caseDefinition = AssetDatabase.LoadAssetAtPath<InvestigationCaseDefinition>($"{DataRoot}/InvestigationCase_LongLine.asset");
            if (caseDefinition == null)
            {
                Debug.LogError("Build the investigation case before updating its evidence.");
                return;
            }
            SerializedObject caseObject = new SerializedObject(caseDefinition);
            SetObservations(caseObject);
            SetComparisonRules(caseObject, caseDefinition.Threats, caseDefinition.Species);
            SetInvestigationObjectives(caseObject);
            caseObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(caseDefinition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Investigation evidence updated for the three-cause case.");
        }

        private static PredictionSpec P(string speciesId, PredictionState state, string rationale) => new PredictionSpec(speciesId, state, rationale);

        private static InvestigationSpeciesDefinition CreateSpecies(
            string fileName,
            string speciesId,
            string displayName,
            string description,
            SpeciesGlyphKind glyphKind,
            DepthBand[] depths,
            string[] dietIds,
            string[] predatorIds,
            string[] sensitivityTags,
            DepthBand mapDepthBand,
            string canonicalSpeciesId = "",
            string scientificName = "",
            InvestigationTrophicRole trophicRole = InvestigationTrophicRole.Unknown,
            string[] aliases = null,
            string[] habitatTags = null,
            string shortDisplayName = "")
        {
            InvestigationSpeciesDefinition asset = LoadOrCreate<InvestigationSpeciesDefinition>($"{DataRoot}/{fileName}");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "speciesId", speciesId);
            SetString(serialized, "canonicalSpeciesId", string.IsNullOrWhiteSpace(canonicalSpeciesId) ? speciesId : canonicalSpeciesId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "shortDisplayName", shortDisplayName);
            SetString(serialized, "scientificName", scientificName);
            SetString(serialized, "description", description);
            serialized.FindProperty("icon").objectReferenceValue = LoadSpeciesIcon(speciesId);
            serialized.FindProperty("glyphKind").intValue = (int)glyphKind;
            serialized.FindProperty("trophicRole").enumValueIndex = (int)trophicRole;
            SetStringArray(serialized, "aliases", aliases ?? Array.Empty<string>());
            SetEnumArray(serialized, "preferredDepths", depths);
            SetStringArray(serialized, "habitatTags", habitatTags ?? Array.Empty<string>());
            SetStringArray(serialized, "dietSpeciesIds", dietIds);
            SetStringArray(serialized, "predatorSpeciesIds", predatorIds);
            SetStringArray(serialized, "sensitivityTags", sensitivityTags);
            serialized.FindProperty("mapDepthBand").enumValueIndex = (int)mapDepthBand;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static InvestigationSpeciesDefinition[] CreateCanonicalSpeciesCatalog(
            InvestigationSpeciesDefinition shark,
            InvestigationSpeciesDefinition tuna,
            InvestigationSpeciesDefinition krill)
        {
            ApplyCanonicalMetadata(
                shark,
                "great_hammerhead_shark",
                "Great Hammerhead Shark",
                "Sphyrna mokarran",
                InvestigationTrophicRole.ApexPredator,
                new[] { "great_hammerhead", "sphyrna_mokarran" },
                new[] { "pelagic", "reef-associated", "shallow", "mid-water" },
                new[] { "atlantic_bluefin_tuna" },
                Array.Empty<string>(),
                "Shark");
            ApplyCanonicalMetadata(
                tuna,
                "atlantic_bluefin_tuna",
                "Atlantic Bluefin Tuna",
                "Thunnus thynnus",
                InvestigationTrophicRole.Predator,
                new[] { "bluefin_tuna", "thunnus_thynnus" },
                new[] { "pelagic", "shallow", "mid-water" },
                new[] { "atlantic_herring" },
                new[] { "great_hammerhead_shark" },
                "Tuna");
            ApplyCanonicalMetadata(
                krill,
                "northern_krill",
                "Northern Krill",
                "Meganyctiphanes norvegica",
                InvestigationTrophicRole.PrimaryConsumer,
                new[] { "meganyctiphanes_norvegica" },
                new[] { "pelagic", "mid-water", "deep" },
                new[] { "phytoplankton" },
                new[] { "atlantic_herring", "reef_manta_ray" },
                "Krill");

            return new[]
            {
                CreateCatalogSpecies("Species_GreenSeaUrchin.asset", "green_sea_urchin", "Green Sea Urchin", "Strongylocentrotus droebachiensis", "A cold-water grazer associated with rocky benthic habitat.", InvestigationTrophicRole.PrimaryConsumer, new[] { DepthBand.Mid, DepthBand.Deep }, DepthBand.Deep, new[] { "phytoplankton" }, Array.Empty<string>(), new[] { "benthic", "rocky-habitat" }),
                CreateCatalogSpecies("Species_ReefMantaRay.asset", "reef_manta_ray", "Reef Manta Ray", "Mobula alfredi", "A large filter-feeding ray that consumes plankton in productive surface waters.", InvestigationTrophicRole.SecondaryConsumer, new[] { DepthBand.Shallow, DepthBand.Mid }, DepthBand.Shallow, new[] { "northern_krill", "phytoplankton" }, Array.Empty<string>(), new[] { "pelagic", "reef-associated" }),
                CreateCatalogSpecies("Species_KitefinShark.asset", "kitefin_shark", "Kitefin Shark", "Dalatias licha", "A deep-water shark used in an alternative offshore food-chain branch.", InvestigationTrophicRole.ApexPredator, new[] { DepthBand.Deep }, DepthBand.Deep, new[] { "orange_roughy" }, Array.Empty<string>(), new[] { "deep", "pelagic" }),
                shark,
                CreateCatalogSpecies("Species_OrangeRoughy.asset", "orange_roughy", "Orange Roughy", "Hoplostethus atlanticus", "A long-lived deep-water fish that feeds on smaller fish and invertebrates.", InvestigationTrophicRole.Predator, new[] { DepthBand.Deep }, DepthBand.Deep, new[] { "spotted_lanternfish" }, new[] { "kitefin_shark" }, new[] { "deep", "seamount" }),
                CreateCatalogSpecies("Species_PineconeFish.asset", "pinecone_fish", "Pinecone Fish", "Monocentris japonica", "A reef-associated fish that forages for small crustaceans at night.", InvestigationTrophicRole.SecondaryConsumer, new[] { DepthBand.Mid, DepthBand.Deep }, DepthBand.Mid, new[] { "northern_krill" }, Array.Empty<string>(), new[] { "reef-associated", "mid-water" }),
                tuna,
                CreateCatalogSpecies("Species_AtlanticHerring.asset", "atlantic_herring", "Atlantic Herring", "Clupea harengus", "A schooling forage fish connecting plankton to larger predators.", InvestigationTrophicRole.SecondaryConsumer, new[] { DepthBand.Shallow, DepthBand.Mid }, DepthBand.Shallow, new[] { "northern_krill", "phytoplankton" }, new[] { "atlantic_bluefin_tuna" }, new[] { "pelagic", "schooling" }),
                CreateCatalogSpecies("Species_SpottedLanternfish.asset", "spotted_lanternfish", "Spotted Lanternfish", "Myctophum punctatum", "A vertically migrating mesopelagic fish that transfers energy through the water column.", InvestigationTrophicRole.SecondaryConsumer, new[] { DepthBand.Mid, DepthBand.Deep }, DepthBand.Mid, new[] { "northern_krill", "phytoplankton" }, new[] { "orange_roughy" }, new[] { "mesopelagic", "vertical-migrant" }),
                krill,
                CreateCatalogSpecies("Species_KingCrab.asset", "king_crab", "King Crab", "Neolithodes agassizii", "A deep benthic crab that scavenges and preys on seafloor organisms.", InvestigationTrophicRole.Scavenger, new[] { DepthBand.Deep }, DepthBand.Deep, Array.Empty<string>(), new[] { "giant_pacific_octopus" }, new[] { "benthic", "deep" }),
                CreateCatalogSpecies("Species_WartySquid.asset", "warty_squid", "Warty Squid", "Moroteuthopsis longimana", "A deep-water squid that hunts fish and crustaceans.", InvestigationTrophicRole.Predator, new[] { DepthBand.Deep }, DepthBand.Deep, new[] { "spotted_lanternfish" }, Array.Empty<string>(), new[] { "deep", "pelagic" }),
                CreateCatalogSpecies("Species_FlapjackOctopus.asset", "flapjack_octopus", "Flapjack Octopus", "Opisthoteuthis californiana", "A soft-bodied deep-sea octopus that searches the seafloor for small prey.", InvestigationTrophicRole.Predator, new[] { DepthBand.Deep }, DepthBand.Deep, Array.Empty<string>(), Array.Empty<string>(), new[] { "benthic", "deep" }),
                CreateCatalogSpecies("Species_GiantPacificOctopus.asset", "giant_pacific_octopus", "Giant Pacific Octopus", "Enteroctopus dofleini", "A large benthic predator included in a crab-focused food-chain branch.", InvestigationTrophicRole.Predator, new[] { DepthBand.Mid, DepthBand.Deep }, DepthBand.Deep, new[] { "king_crab" }, Array.Empty<string>(), new[] { "benthic", "reef-associated" }),
                CreateCatalogSpecies("Species_BoneEatingWorm.asset", "bone_eating_worm", "Bone Eating Worm", "Osedax frankpressi", "A specialist decomposer that colonises vertebrate bones on the deep seafloor.", InvestigationTrophicRole.Decomposer, new[] { DepthBand.Deep }, DepthBand.Deep, Array.Empty<string>(), Array.Empty<string>(), new[] { "benthic", "deep", "whale-fall" }),
                CreateCatalogSpecies("Species_TreeBubblegumCoral.asset", "tree_bubblegum_coral", "Tree Bubblegum Coral", "Paragorgia arborea", "A habitat-forming cold-water coral vulnerable to physical seafloor disturbance.", InvestigationTrophicRole.HabitatForming, new[] { DepthBand.Deep }, DepthBand.Deep, Array.Empty<string>(), Array.Empty<string>(), new[] { "benthic", "deep", "coral" }),
                CreateCatalogSpecies("Species_PreciousCoral.asset", "precious_coral", "Precious Coral", "Corallium rubrum", "A slow-growing habitat-forming coral associated with hard substrate.", InvestigationTrophicRole.HabitatForming, new[] { DepthBand.Mid, DepthBand.Deep }, DepthBand.Deep, Array.Empty<string>(), Array.Empty<string>(), new[] { "benthic", "coral", "rocky-habitat" }),
                CreateCatalogSpecies("Species_ZigzagCoral.asset", "zigzag_coral", "Zigzag Coral", "Madrepora oculata", "A branching cold-water coral that provides three-dimensional habitat.", InvestigationTrophicRole.HabitatForming, new[] { DepthBand.Deep }, DepthBand.Deep, Array.Empty<string>(), Array.Empty<string>(), new[] { "benthic", "deep", "coral" }),
                CreateCatalogSpecies("Species_MoonJellyfish.asset", "moon_jellyfish", "Moon Jellyfish", "Aurelia aurita", "A gelatinous predator that consumes zooplankton and small crustaceans.", InvestigationTrophicRole.SecondaryConsumer, new[] { DepthBand.Shallow, DepthBand.Mid }, DepthBand.Shallow, new[] { "northern_krill" }, Array.Empty<string>(), new[] { "pelagic", "shallow" }),
                CreateCatalogSpecies("Species_Phytoplankton.asset", "phytoplankton", "Phytoplankton", "Prochlorococcus marinus", "A photosynthetic primary producer forming the base of the pelagic food web.", InvestigationTrophicRole.PrimaryProducer, new[] { DepthBand.Shallow }, DepthBand.Shallow, Array.Empty<string>(), new[] { "northern_krill", "atlantic_herring", "reef_manta_ray" }, new[] { "pelagic", "sunlit-zone" })
            };
        }

        private static InvestigationSpeciesDefinition CreateCatalogSpecies(
            string fileName,
            string speciesId,
            string displayName,
            string scientificName,
            string description,
            InvestigationTrophicRole trophicRole,
            DepthBand[] depths,
            DepthBand mapDepthBand,
            string[] dietIds,
            string[] predatorIds,
            string[] habitatTags)
        {
            return CreateSpecies(
                fileName,
                speciesId,
                displayName,
                description,
                SpeciesGlyphKind.NameOnly,
                depths,
                dietIds,
                predatorIds,
                Array.Empty<string>(),
                mapDepthBand,
                speciesId,
                scientificName,
                trophicRole,
                new[] { scientificName.ToLowerInvariant().Replace(' ', '_') },
                habitatTags);
        }

        private static void ApplyCanonicalMetadata(
            InvestigationSpeciesDefinition species,
            string canonicalId,
            string displayName,
            string scientificName,
            InvestigationTrophicRole trophicRole,
            string[] aliases,
            string[] habitatTags,
            string[] dietIds,
            string[] predatorIds,
            string shortDisplayName)
        {
            SerializedObject serialized = new SerializedObject(species);
            SetString(serialized, "canonicalSpeciesId", canonicalId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "shortDisplayName", shortDisplayName);
            SetString(serialized, "scientificName", scientificName);
            serialized.FindProperty("trophicRole").enumValueIndex = (int)trophicRole;
            SetStringArray(serialized, "aliases", aliases);
            SetStringArray(serialized, "habitatTags", habitatTags);
            SetStringArray(serialized, "dietSpeciesIds", dietIds);
            SetStringArray(serialized, "predatorSpeciesIds", predatorIds);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(species);
        }

        private static ThreatSimulationDefinition CreateThreat(
            string fileName,
            string threatId,
            string displayName,
            string summary,
            ThreatGlyphKind glyphKind,
            PredictionSpec[] predictions,
            string seafloor,
            string physical)
        {
            ThreatSimulationDefinition asset = LoadOrCreate<ThreatSimulationDefinition>($"{DataRoot}/{fileName}");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "threatId", threatId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "summary", summary);
            serialized.FindProperty("icon").objectReferenceValue = LoadThreatIcon(threatId);
            serialized.FindProperty("glyphKind").intValue = (int)glyphKind;
            SerializedProperty predictionArray = serialized.FindProperty("speciesPredictions");
            predictionArray.arraySize = predictions.Length;
            for (int index = 0; index < predictions.Length; index++)
            {
                SerializedProperty property = predictionArray.GetArrayElementAtIndex(index);
                property.FindPropertyRelative("speciesId").stringValue = predictions[index].SpeciesId;
                property.FindPropertyRelative("predictedState").enumValueIndex = (int)predictions[index].State;
                property.FindPropertyRelative("rationale").stringValue = predictions[index].Rationale;
            }
            SetString(serialized, "seafloorPrediction", seafloor);
            SetString(serialized, "physicalConfirmation", physical);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void ConfigureArtworkImporters()
        {
            foreach (string file in new[] { "hammerhead.png", "tuna.png", "krill.png", "sea-star.png", "mussel.png" })
                ConfigureSpriteImporter($"{FieldGuideArtRoot}/{file}");
            string[] artworkFiles =
            {
                "shark.png",
                "tuna.png",
                "krill.png",
                "sea-star.png",
                "mussel.png",
                "plastic.png",
                "long-line.png",
                "bottom-trawling.png"
            };

            for (int index = 0; index < artworkFiles.Length; index++)
            {
                ConfigureSpriteImporter($"{ArtRoot}/{artworkFiles[index]}");
            }

            string[] statusIcons =
            {
                "check-circle.png",
                "x-circle.png",
                "question-mark-circle.png",
                "link.png",
                "map.png",
                "beaker.png",
                "signal.png"
            };
            for (int index = 0; index < statusIcons.Length; index++)
                ConfigureSpriteImporter($"{StatusIconRoot}/{statusIcons[index]}");

            ConfigureSeamountImporter(SeamountSpritePath);
        }

        private static void ConfigureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Investigation artwork is missing or not importable: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = path.StartsWith(FieldGuideArtRoot, StringComparison.Ordinal)
                ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = path.StartsWith(FieldGuideArtRoot, StringComparison.Ordinal) ? 512 : 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        [MenuItem("eDNA Detectives/Update Visual Artwork")]
        public static void UpdateVisualArtwork()
        {
            AssetDatabase.Refresh();
            string[] ids = { "shark", "tuna", "krill", "sea_star", "mussel" };
            string[] names = { "Shark", "Tuna", "Krill", "SeaStar", "Mussel" };
            string[] files = { "hammerhead", "tuna", "krill", "sea-star", "mussel" };
            for (int index = 0; index < ids.Length; index++)
            {
                ConfigureSpriteImporter($"{FieldGuideArtRoot}/{files[index]}.png");
                InvestigationSpeciesDefinition species = AssetDatabase.LoadAssetAtPath<InvestigationSpeciesDefinition>($"{DataRoot}/Species_{names[index]}.asset");
                if (species == null) throw new InvalidOperationException($"Missing case species: {ids[index]}");
                SerializedObject serialized = new SerializedObject(species);
                serialized.FindProperty("icon").objectReferenceValue = LoadSpeciesIcon(ids[index]);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(species);
            }
            Debug.Log("INVESTIGATION_VISUAL_ARTWORK_UPDATED species=5");
        }

        private static void ConfigureSeamountImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Investigation seamount artwork is missing or not importable: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.maxTextureSize = 1024;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Sprite LoadSpeciesIcon(string speciesId)
        {
            string fieldGuideFile = speciesId == "shark" ? "hammerhead" : speciesId == "sea_star" ? "sea-star" : speciesId;
            Sprite fieldGuide = AssetDatabase.LoadAssetAtPath<Sprite>($"{FieldGuideArtRoot}/{fieldGuideFile}.png");
            if (fieldGuide != null) return fieldGuide;
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
            array.arraySize = 8;
            SetObservation(array.GetArrayElementAtIndex(0), "E01_SHARK_NONDETECTION", "Shark repeatedly not detected", "Shark DNA was not detected in several high-quality samples across the surveyed depths.", "shark", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, EvidenceConfidence.High, ObservationClaimType.NotDetected, EvidenceCategory.FoodWeb, "Repeated high-quality non-detection is stronger than one sample, but it still does not prove absence.");
            SetObservation(array.GetArrayElementAtIndex(1), "E02_TUNA_WIDER_DETECTION", "Tuna detected at more sites", "Tuna DNA was detected across more survey locations than in the historical baseline.", "tuna", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, EvidenceConfidence.Medium, ObservationClaimType.ChangedDepthOrDistribution, EvidenceCategory.FoodWeb, "Wider detection is consistent with expansion but does not directly measure abundance.");
            SetObservation(array.GetArrayElementAtIndex(2), "E03_KRILL_NONDETECTION", "Krill repeatedly not detected", "Krill DNA was not detected in several high-quality samples where it was historically expected.", "krill", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, EvidenceConfidence.High, ObservationClaimType.NotDetected, EvidenceCategory.FoodWeb, "The pattern supports a decline hypothesis but does not prove a population count.");
            SetObservation(array.GetArrayElementAtIndex(3), "E04_BENTHIC_STABLE", "Sea star remains stable", "The benthic indicator was repeatedly detected at its historical deep sites.", "sea_star", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, EvidenceConfidence.Medium, ObservationClaimType.MatchesBaseline, EvidenceCategory.Benthic, "Repeated detection across the same sites supports stability, pending ROV confirmation of habitat condition.");
            SetObservation(array.GetArrayElementAtIndex(4), "E06_PLASTIC_INDICATOR_STABLE", "Filter-feeding mussel remains stable", "The plastic-sensitive reference species remains detected at its historical sites.", "mussel", ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty, EvidenceConfidence.Medium, ObservationClaimType.MatchesBaseline, EvidenceCategory.Alternative, "This challenges a broad plastic-impact pattern but cannot rule it out alone.");
            SetObservation(array.GetArrayElementAtIndex(5), "E07_FISHING_LINE", "Fishing line recorded near shark habitat", "ROV footage shows fishing line near the area where sharks were historically recorded.", "shark", ObservationSource.ROV, EvidenceUnlockStage.AfterProvisional, string.Empty, EvidenceConfidence.High, ObservationClaimType.PhysicalObservation, EvidenceCategory.Confirmation, "A physical gear observation confirms an already-developed long-line hypothesis.");
            SetObservation(array.GetArrayElementAtIndex(6), "E08_SEAFLOOR_INTACT", "Seafloor remains intact", "ROV footage shows no obvious trawl marks or broad habitat damage.", "sea_star", ObservationSource.ROV, EvidenceUnlockStage.AfterProvisional, string.Empty, EvidenceConfidence.High, ObservationClaimType.PhysicalObservation, EvidenceCategory.Confirmation, "The intact habitat strongly challenges bottom trawling alongside the stable benthic eDNA pattern.");
            SetObservation(array.GetArrayElementAtIndex(7), "L01_NONDETECTION_LIMITATION", "Not detected does not mean gone", "eDNA non-detection does not prove complete absence; sampling and detection limits remain.", string.Empty, ObservationSource.Methodology, EvidenceUnlockStage.Always, string.Empty, EvidenceConfidence.High, ObservationClaimType.MethodologicalLimitation, EvidenceCategory.General, "This scientific limitation is always available in the final report.");
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
            EvidenceConfidence confidence,
            ObservationClaimType claimType,
            EvidenceCategory category,
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
            property.FindPropertyRelative("category").enumValueIndex = (int)category;
            property.FindPropertyRelative("confidenceReason").stringValue = confidenceReason;
        }

        private static void SetComparisonRules(
            SerializedObject caseObject,
            IReadOnlyList<ThreatSimulationDefinition> threats,
            IReadOnlyList<InvestigationSpeciesDefinition> species)
        {
            SerializedProperty rules = caseObject.FindProperty("comparisonRules");
            rules.arraySize = threats.Count * species.Count;
            int ruleIndex = 0;
            for (int threatIndex = 0; threatIndex < threats.Count; threatIndex++)
            {
                ThreatSimulationDefinition threat = threats[threatIndex];
                for (int speciesIndex = 0; speciesIndex < species.Count; speciesIndex++)
                {
                    InvestigationSpeciesDefinition speciesDefinition = species[speciesIndex];
                    PredictionState predictedState = threat.FindPrediction(speciesDefinition.SpeciesId).PredictedState;
                    SerializedProperty rule = rules.GetArrayElementAtIndex(ruleIndex++);
                    rule.FindPropertyRelative("threatId").stringValue = threat.ThreatId;
                    rule.FindPropertyRelative("speciesId").stringValue = speciesDefinition.SpeciesId;
                    rule.FindPropertyRelative("targetKind").intValue = (int)PredictionTargetKind.Species;
                    rule.FindPropertyRelative("targetId").stringValue = speciesDefinition.SpeciesId;
                    rule.FindPropertyRelative("progressRole").enumValueIndex = (int)ProgressRoleFor(threat.ThreatId, speciesDefinition.SpeciesId);
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

        private static ComparisonProgressRole ProgressRoleFor(string threatId, string speciesId)
        {
            if (threatId == "plastic" && speciesId == "mussel") return ComparisonProgressRole.AlternativeCauseCheck;
            if (threatId == "longline" && (speciesId == "shark" || speciesId == "tuna" || speciesId == "krill"))
                return ComparisonProgressRole.FoodWebCascade;
            if (threatId == "bottom_trawling" && speciesId == "tuna") return ComparisonProgressRole.SharedPrediction;
            if ((threatId == "longline" || threatId == "bottom_trawling") && speciesId == "sea_star")
                return ComparisonProgressRole.BenthicDiscriminator;
            return ComparisonProgressRole.ContextOnly;
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

        private static void SetInvestigationObjectives(SerializedObject caseObject)
        {
            SerializedProperty objectives = caseObject.FindProperty("investigationObjectives");
            objectives.arraySize = 7;
            SetInteger(caseObject, "minimumCompletedComparisons", objectives.arraySize);
            SetObjective(objectives.GetArrayElementAtIndex(0), "plastic_mussel", "plastic", "Does plastic fit the indicator species?", "plastic", PredictionTargetKind.Species, "mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch, ComparisonProgressRole.AlternativeCauseCheck);
            SetObjective(objectives.GetArrayElementAtIndex(1), "longline_shark", "food_web", "Can fishing trigger the food-web changes?", "longline", PredictionTargetKind.Species, "shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match, ComparisonProgressRole.FoodWebCascade);
            SetObjective(objectives.GetArrayElementAtIndex(2), "longline_tuna", "food_web", "Can fishing trigger the food-web changes?", "longline", PredictionTargetKind.Species, "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match, ComparisonProgressRole.FoodWebCascade);
            SetObjective(objectives.GetArrayElementAtIndex(3), "longline_krill", "food_web", "Can fishing trigger the food-web changes?", "longline", PredictionTargetKind.Species, "krill", "E03_KRILL_NONDETECTION", ComparisonJudgement.Match, ComparisonProgressRole.FoodWebCascade);
            SetObjective(objectives.GetArrayElementAtIndex(4), "bottom_tuna", "overlap", "Why do two fishing models partly match?", "bottom_trawling", PredictionTargetKind.Species, "tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match, ComparisonProgressRole.SharedPrediction);
            SetObjective(objectives.GetArrayElementAtIndex(5), "longline_seastar", "benthic", "Which clue separates the fishing models?", "longline", PredictionTargetKind.Species, "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match, ComparisonProgressRole.BenthicDiscriminator);
            SetObjective(objectives.GetArrayElementAtIndex(6), "bottom_seastar", "benthic", "Which clue separates the fishing models?", "bottom_trawling", PredictionTargetKind.Species, "sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch, ComparisonProgressRole.BenthicDiscriminator);
        }

        private static void SetObjective(
            SerializedProperty property,
            string objectiveId,
            string questionId,
            string questionPrompt,
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement,
            ComparisonProgressRole role)
        {
            property.FindPropertyRelative("objectiveId").stringValue = objectiveId;
            property.FindPropertyRelative("questionId").stringValue = questionId;
            property.FindPropertyRelative("questionPrompt").stringValue = questionPrompt;
            property.FindPropertyRelative("threatId").stringValue = threatId;
            property.FindPropertyRelative("targetKind").intValue = (int)targetKind;
            property.FindPropertyRelative("targetId").stringValue = targetId;
            property.FindPropertyRelative("requiredEvidenceId").stringValue = evidenceId;
            property.FindPropertyRelative("requiredJudgement").enumValueIndex = (int)judgement;
            property.FindPropertyRelative("progressRole").enumValueIndex = (int)role;
            property.FindPropertyRelative("required").boolValue = true;
        }

        private static void SetEvidenceCategoryRequirements(SerializedObject caseObject)
        {
            SerializedProperty requirements = caseObject.FindProperty("evidenceCategoryRequirements");
            requirements.arraySize = 3;
            SetEvidenceCategoryRequirement(requirements.GetArrayElementAtIndex(0), EvidenceCategory.FoodWeb, 2);
            SetEvidenceCategoryRequirement(requirements.GetArrayElementAtIndex(1), EvidenceCategory.Benthic, 1);
            SetEvidenceCategoryRequirement(requirements.GetArrayElementAtIndex(2), EvidenceCategory.Confirmation, 1);
        }

        private static void SetFoodWebEdges(SerializedObject caseObject)
        {
            SerializedProperty edges = caseObject.FindProperty("foodWebEdges");
            edges.arraySize = 12;
            SetFoodWebEdge(edges.GetArrayElementAtIndex(0), "case_shark_tuna", "case_simplified", "great_hammerhead_shark", "atlantic_bluefin_tuna", FoodWebRelationshipStrength.High, FoodWebRelationshipConfidence.Provisional, true, "Simplified case link used to model predator removal; retained separately from the reference chain.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(1), "case_tuna_krill", "case_simplified", "atlantic_bluefin_tuna", "northern_krill", FoodWebRelationshipStrength.Medium, FoodWebRelationshipConfidence.Provisional, true, "Simplified case link used by the current three-node detective puzzle.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(2), "reference_hammerhead_bluefin", "reference_main", "great_hammerhead_shark", "atlantic_bluefin_tuna", FoodWebRelationshipStrength.Low, FoodWebRelationshipConfidence.Provisional, false, "Storyboard relationship; scientific suitability for the final exhibit still requires review.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(3), "reference_bluefin_herring", "reference_main", "atlantic_bluefin_tuna", "atlantic_herring", FoodWebRelationshipStrength.High, FoodWebRelationshipConfidence.Supported, false, "Bluefin tuna prey on schooling forage fish including Atlantic herring.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(4), "reference_herring_krill", "reference_main", "atlantic_herring", "northern_krill", FoodWebRelationshipStrength.High, FoodWebRelationshipConfidence.Supported, false, "Atlantic herring consume krill and other zooplankton.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(5), "reference_krill_phytoplankton", "reference_main", "northern_krill", "phytoplankton", FoodWebRelationshipStrength.High, FoodWebRelationshipConfidence.Supported, false, "Northern krill graze on phytoplankton.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(6), "manta_krill", "manta_branch", "reef_manta_ray", "northern_krill", FoodWebRelationshipStrength.Medium, FoodWebRelationshipConfidence.Provisional, false, "Filter-feeding branch derived from the storyboard and awaiting location-specific review.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(7), "manta_krill_phytoplankton", "manta_branch", "northern_krill", "phytoplankton", FoodWebRelationshipStrength.High, FoodWebRelationshipConfidence.Supported, false, "Krill connect the manta branch to primary production.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(8), "kitefin_orange_roughy", "deep_branch", "kitefin_shark", "orange_roughy", FoodWebRelationshipStrength.Medium, FoodWebRelationshipConfidence.Provisional, false, "Deep-water storyboard branch; final ecological review is pending.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(9), "orange_roughy_lanternfish", "deep_branch", "orange_roughy", "spotted_lanternfish", FoodWebRelationshipStrength.Medium, FoodWebRelationshipConfidence.Provisional, false, "Deep-water storyboard branch; final ecological review is pending.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(10), "lanternfish_krill", "deep_branch", "spotted_lanternfish", "northern_krill", FoodWebRelationshipStrength.Medium, FoodWebRelationshipConfidence.Provisional, false, "Mesopelagic fish link the deep branch to zooplankton.");
            SetFoodWebEdge(edges.GetArrayElementAtIndex(11), "octopus_king_crab", "benthic_branch", "giant_pacific_octopus", "king_crab", FoodWebRelationshipStrength.Medium, FoodWebRelationshipConfidence.Provisional, false, "Benthic predator branch from the shared storyboard.");
        }

        private static void SetFoodWebEdge(
            SerializedProperty property,
            string edgeId,
            string networkId,
            string predatorId,
            string preyId,
            FoodWebRelationshipStrength strength,
            FoodWebRelationshipConfidence confidence,
            bool caseRelevant,
            string explanation)
        {
            property.FindPropertyRelative("edgeId").stringValue = edgeId;
            property.FindPropertyRelative("networkId").stringValue = networkId;
            property.FindPropertyRelative("predatorSpeciesId").stringValue = predatorId;
            property.FindPropertyRelative("preySpeciesId").stringValue = preyId;
            property.FindPropertyRelative("strength").enumValueIndex = (int)strength;
            property.FindPropertyRelative("confidence").enumValueIndex = (int)confidence;
            property.FindPropertyRelative("caseRelevant").boolValue = caseRelevant;
            property.FindPropertyRelative("explanation").stringValue = explanation;
        }

        private static void SetEvidenceCategoryRequirement(
            SerializedProperty property,
            EvidenceCategory category,
            int minimumCount)
        {
            property.FindPropertyRelative("category").enumValueIndex = (int)category;
            property.FindPropertyRelative("minimumCount").intValue = minimumCount;
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
                "Investigation Runtime",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(InvestigationController),
                typeof(InvestigationRuntimeView));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200f, 760f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            SerializedObject runtimeView = new SerializedObject(root.GetComponent<InvestigationRuntimeView>());
            runtimeView.FindProperty("seamountSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SeamountSpritePath);
            runtimeView.ApplyModifiedPropertiesWithoutUndo();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void CreateScene(InvestigationCaseDefinition caseDefinition, GameObject viewPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "InvestigationScene";
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = InvestigationTheme.Background;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject bootstrapObject = new GameObject("Investigation Demo", typeof(InvestigationDemoBootstrap));
            SerializedObject bootstrap = new SerializedObject(bootstrapObject.GetComponent<InvestigationDemoBootstrap>());
            bootstrap.FindProperty("caseDefinition").objectReferenceValue = caseDefinition;
            bootstrap.FindProperty("viewPrefab").objectReferenceValue = viewPrefab;
            bootstrap.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        [MenuItem("eDNA Detectives/Use Investigation Startup Scene")]
        public static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(scene => string.Equals(scene.path, ScenePath, StringComparison.Ordinal));
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
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

        private static void SetSurveyContext(
            SerializedObject serialized,
            string surveyId,
            string surveyDisplayName,
            string siteId,
            string siteDisplayName,
            string processedSampleSummary)
        {
            SerializedProperty context = serialized.FindProperty("surveyContext");
            context.FindPropertyRelative("surveyId").stringValue = surveyId;
            context.FindPropertyRelative("surveyDisplayName").stringValue = surveyDisplayName;
            context.FindPropertyRelative("siteId").stringValue = siteId;
            context.FindPropertyRelative("siteDisplayName").stringValue = siteDisplayName;
            context.FindPropertyRelative("processedSampleSummary").stringValue = processedSampleSummary;
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
