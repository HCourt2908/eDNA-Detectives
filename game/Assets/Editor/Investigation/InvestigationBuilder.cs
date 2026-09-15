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
    public static partial class InvestigationBuilder
    {
        private const string DataRoot = "Assets/Data/Investigation/LongLineCase";
        private const string ArtRoot = "Assets/Art/Investigation/OpenMoji";
        private const string TeamArtRoot = "Assets/Art/Investigation/TeamSpecies";
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

            InvestigationCaseDefinition caseDefinition = UpdateFigmaFoodChainCase();

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
            UpdateFigmaFoodChainCase();
        }

        [MenuItem("eDNA Detectives/Update Case Evidence")]
        public static void UpdateCaseEvidence() => UpdateFigmaFoodChainCase();

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
            SetString(serialized, "description", ReviewedSpeciesDescription(speciesId, description));
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
            foreach (string file in new[] { "great-hammerhead-shark.png", "reef-manta-ray.png", "bone-eating-worm.png" })
                ConfigureSpriteImporter($"{TeamArtRoot}/{file}");
            foreach (string file in new[] { "hammerhead.png", "tuna.png", "atlantic-herring.png", "krill.png", "phytoplankton.png" })
                ConfigureSpriteImporter($"{FieldGuideArtRoot}/{file}");
            string[] artworkFiles =
            {
                "shark.png",
                "tuna.png",
                "krill.png",
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
                "arrow-path.png", "check-circle.png",
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
            foreach (string file in new[] { "great-hammerhead-shark.png", "reef-manta-ray.png", "bone-eating-worm.png" })
                ConfigureSpriteImporter($"{TeamArtRoot}/{file}");
            string[] ids = { "shark", "tuna", "atlantic_herring", "krill", "phytoplankton" };
            string[] names = { "Shark", "Tuna", "AtlanticHerring", "Krill", "Phytoplankton" };
            string[] files = { "hammerhead", "tuna", "atlantic-herring", "krill", "phytoplankton" };
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
            foreach (string name in new[] { "ReefMantaRay", "BoneEatingWorm" })
            {
                var species = AssetDatabase.LoadAssetAtPath<InvestigationSpeciesDefinition>($"{DataRoot}/Species_{name}.asset");
                if (species == null) throw new InvalidOperationException($"Missing catalog species: {name}");
                var serialized = new SerializedObject(species);
                serialized.FindProperty("icon").objectReferenceValue = LoadSpeciesIcon(species.SpeciesId);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(species);
            }
            Debug.Log("INVESTIGATION_VISUAL_ARTWORK_UPDATED species=7");
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
            string teamFile = speciesId == "shark" ? "great-hammerhead-shark" : speciesId.Replace('_', '-');
            Sprite teamArt = AssetDatabase.LoadAssetAtPath<Sprite>($"{TeamArtRoot}/{teamFile}.png");
            if (teamArt != null) return teamArt;
            string fieldGuideFile = speciesId == "shark" ? "hammerhead" : speciesId.Replace('_', '-');
            Sprite fieldGuide = AssetDatabase.LoadAssetAtPath<Sprite>($"{FieldGuideArtRoot}/{fieldGuideFile}.png");
            if (fieldGuide != null) return fieldGuide;
            switch (speciesId)
            {
                case "shark": return LoadIcon("shark.png");
                case "tuna": return LoadIcon("tuna.png");
                case "krill": return LoadIcon("krill.png");
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
            // The first five records are authored by the active Figma case.
            SetObservation(array.GetArrayElementAtIndex(5), "E07_FISHING_LINE", "Legacy fishing-line note", "Legacy API fixture, not a finding in this activity.", "shark", ObservationSource.ROV, EvidenceUnlockStage.AfterProvisional, string.Empty, EvidenceConfidence.High, ObservationClaimType.PhysicalObservation, EvidenceCategory.Confirmation, "Fishing activity alone does not identify the fishing method.");
            SetObservation(array.GetArrayElementAtIndex(6), "E08_SEAFLOOR_INTACT", "Legacy seafloor note", "Legacy API fixture, not a finding in this activity.", string.Empty, ObservationSource.ROV, EvidenceUnlockStage.AfterProvisional, string.Empty, EvidenceConfidence.High, ObservationClaimType.PhysicalObservation, EvidenceCategory.Confirmation, "No habitat measurement is supplied in the active example survey.");
            SetObservation(array.GetArrayElementAtIndex(7), "L01_NONDETECTION_LIMITATION", "Not detected does not mean gone", "eDNA non-detection does not prove complete absence; sampling and detection limits remain.", string.Empty, ObservationSource.Methodology, EvidenceUnlockStage.Always, string.Empty, EvidenceConfidence.High, ObservationClaimType.MethodologicalLimitation, EvidenceCategory.General, "A model match does not prove the cause.");
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
            SetReasoning(options.GetArrayElementAtIndex(0), "food_web_cascade", "Shark ↓ → Tuna ↑ → Herring ↓ → Krill ↑ → Phytoplankton ↓", "The illustrative response follows each link; the matching pattern does not distinguish the two fishing causes.");
            SetReasoning(options.GetArrayElementAtIndex(1), "shared_habitat_shift", "The species moved when their habitat shifted", "A shared habitat shift would need a coherent environmental or depth pattern.");
            SetReasoning(options.GetArrayElementAtIndex(2), "direct_fishing_loss", "Fishing directly removed every species", "The food-chain model distinguishes a direct premise from the subsequent inferred responses.");
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

        [MenuItem("eDNA Detectives/Add Investigation to Build Settings")]
        public static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(scene => string.Equals(scene.path, ScenePath, StringComparison.Ordinal));
            // Register this part without replacing the shared game's startup scene.
            if (index < 0) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            else scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
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
