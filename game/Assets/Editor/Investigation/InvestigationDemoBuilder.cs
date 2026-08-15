using System;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EDNA.Investigation.Editor
{
    public static class InvestigationDemoBuilder
    {
        private const string DataRoot = "Assets/Data/Investigation/DemoCase";
        private const string PrefabRoot = "Assets/Prefabs/Investigation";
        private const string ScenePath = "Assets/Scenes/InvestigationScene.unity";

        [MenuItem("eDNA Detectives/Build Investigation Demo")]
        public static void CreateDemoProject()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(PrefabRoot);

            SpeciesDefinition coldFish = CreateSpecies(
                "Species_ColdWaterFishA.asset",
                "mock_cold_fish",
                "Cold-water Fish A",
                "A temperature-sensitive fish historically found in shallow water around the summit.",
                "Cold water below 12°C",
                new[] { DepthBand.Shallow },
                new[] { "RockyReef", "ColdSensitive" });
            SpeciesDefinition predator = CreateSpecies(
                "Species_PredatorB.asset",
                "mock_predator",
                "Predator B",
                "A mobile predator whose distribution can follow its prey.",
                "Temperate water",
                new[] { DepthBand.Shallow, DepthBand.Mid },
                new[] { "Predator", "Mobile" });
            SpeciesDefinition stable = CreateSpecies(
                "Species_StableC.asset",
                "mock_stable_species",
                "Stable Species C",
                "A broad-tolerance indicator species used as a comparison against changing species.",
                "Broad tolerance",
                new[] { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep },
                new[] { "StableIndicator" });
            SpeciesDefinition prey = CreateSpecies(
                "Species_PreyD.asset",
                "mock_prey",
                "Prey D",
                "A common prey species associated with the slope habitat.",
                "Temperate water",
                new[] { DepthBand.Shallow, DepthBand.Mid },
                new[] { "Prey", "FoodWeb" });
            SpeciesDefinition deepSpecies = CreateSpecies(
                "Species_DeepE.asset",
                "mock_deep_species",
                "Deep Species E",
                "A deep-water indicator expected near the lower sampling zone.",
                "Cold deep water",
                new[] { DepthBand.Deep },
                new[] { "DeepHabitat" });
            SpeciesDefinition warmFish = CreateSpecies(
                "Species_WarmWaterFishF.asset",
                "mock_warm_fish",
                "Warm-water Fish F",
                "A warm-affinity fish not present in the historical shallow-water records.",
                "Warm water above 15°C",
                new[] { DepthBand.Shallow, DepthBand.Mid },
                new[] { "WarmWater", "RangeExpansion" });

            SampleSiteDefinition summit = CreateSite(
                "Site_Summit.asset",
                "mock_summit",
                "Seamount Summit",
                "A shallow rocky habitat where the strongest historical cold-water signal was recorded.",
                new[] { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep },
                new Vector2(0.25f, 0.72f));
            SampleSiteDefinition slope = CreateSite(
                "Site_Slope.asset",
                "mock_slope",
                "Eastern Slope",
                "A mixed reef and sediment habitat with a known prey community.",
                new[] { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep },
                new Vector2(0.58f, 0.5f));
            SampleSiteDefinition deepZone = CreateSite(
                "Site_DeepZone.asset",
                "mock_deep_zone",
                "Deep Reference Zone",
                "A colder reference site used to compare deep-water species.",
                new[] { DepthBand.Mid, DepthBand.Deep },
                new Vector2(0.78f, 0.22f));

            HypothesisDefinition warming = CreateHypothesis(
                "Hypothesis_Warming.asset",
                "mock_ocean_warming",
                "Ocean warming shifted species distributions",
                "Warm-affinity species expanded into shallow water while a cold-sensitive species moved deeper.",
                new[] { EvidenceType.NewDetection.ToString(), EvidenceType.DepthShift.ToString() },
                Array.Empty<string>(),
                EvidenceConfidence.Medium,
                2);
            HypothesisDefinition fishing = CreateHypothesis(
                "Hypothesis_Fishing.asset",
                "mock_overfishing",
                "Fishing pressure disrupted the food web",
                "Predator and prey losses should form a repeated, reliable food-web pattern.",
                new[] { EvidenceType.RepeatedNonDetection.ToString(), "FoodWeb" },
                Array.Empty<string>(),
                EvidenceConfidence.Medium,
                2);
            HypothesisDefinition contamination = CreateHypothesis(
                "Hypothesis_Contamination.asset",
                "mock_contamination",
                "The apparent change is laboratory contamination",
                "Low-quality samples and contamination warnings could create a misleading pattern.",
                new[] { EvidenceType.ContaminationWarning.ToString(), EvidenceType.LowQualityResult.ToString() },
                new[] { EvidenceType.DepthShift.ToString() },
                EvidenceConfidence.Low,
                2);

            InvestigationCaseDefinition caseDefinition = LoadOrCreate<InvestigationCaseDefinition>($"{DataRoot}/InvestigationCase_Demo.asset");
            SerializedObject caseObject = new SerializedObject(caseDefinition);
            SetString(caseObject, "caseId", "investigation_demo_01");
            SetString(caseObject, "displayName", "The Shifting Seamount");
            SetString(
                caseObject,
                "briefing",
                "Recent eDNA samples show a warmer-water visitor and missing cold-water species. Determine whether this is an ecosystem shift, a food-web problem, or unreliable sampling.");
            SetObjectArray(caseObject, "species", new UnityEngine.Object[] { coldFish, predator, stable, prey, deepSpecies, warmFish });
            SetObjectArray(caseObject, "sampleSites", new UnityEngine.Object[] { summit, slope, deepZone });
            SetObjectArray(caseObject, "hypotheses", new UnityEngine.Object[] { warming, fishing, contamination });
            SetHistoricalBaseline(caseObject);
            SetInitialResults(caseObject);
            SetMockOutcomes(caseObject);
            SetInteger(caseObject, "followUpSampleLimit", 2);
            SetString(caseObject, "correctHypothesisId", "mock_ocean_warming");
            SetBoolean(caseObject, "requireFollowUpSample", true);
            SetInteger(caseObject, "requiredOpposingEvidence", 1);
            SetString(
                caseObject,
                "successFeedback",
                "Case solved: a warm-water arrival plus the cold-water fish's deeper detection supports a depth shift. The contamination warning remains an important uncertainty, but it does not explain the full pattern.");
            caseObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(caseDefinition);

            GameObject viewPrefab = CreatePresentationPrefabs();
            global::InvestigationUiPrefabBuilder.Rebuild();
            viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/InvestigationRuntimeView.prefab");
            CreateScene(caseDefinition, viewPrefab);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Investigation demo generated at {ScenePath}");
        }

        private static SpeciesDefinition CreateSpecies(
            string fileName,
            string speciesId,
            string displayName,
            string description,
            string temperature,
            DepthBand[] depths,
            string[] sensitivityTags)
        {
            SpeciesDefinition asset = LoadOrCreate<SpeciesDefinition>($"{DataRoot}/{fileName}");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "speciesId", speciesId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "description", description);
            SetString(serialized, "temperaturePreference", temperature);
            SetEnumArray(serialized, "preferredDepths", depths);
            SetStringArray(serialized, "habitatTags", sensitivityTags);
            SetStringArray(serialized, "sensitivityTags", sensitivityTags);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static SampleSiteDefinition CreateSite(
            string fileName,
            string siteId,
            string displayName,
            string description,
            DepthBand[] depths,
            Vector2 position)
        {
            SampleSiteDefinition asset = LoadOrCreate<SampleSiteDefinition>($"{DataRoot}/{fileName}");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "siteId", siteId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "description", description);
            SetEnumArray(serialized, "availableDepths", depths);
            SetStringArray(serialized, "habitatTags", new[] { "PlaceholderSite" });
            serialized.FindProperty("mapPosition").vector2Value = position;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static HypothesisDefinition CreateHypothesis(
            string fileName,
            string hypothesisId,
            string displayName,
            string explanation,
            string[] requiredTags,
            string[] contradictingTags,
            EvidenceConfidence minimumConfidence,
            int minimumSupportingEvidence)
        {
            HypothesisDefinition asset = LoadOrCreate<HypothesisDefinition>($"{DataRoot}/{fileName}");
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "hypothesisId", hypothesisId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "explanation", explanation);
            SetStringArray(serialized, "requiredEvidenceTags", requiredTags);
            SetStringArray(serialized, "contradictingEvidenceTags", contradictingTags);
            serialized.FindProperty("minimumConfidence").enumValueIndex = (int)minimumConfidence;
            SetInteger(serialized, "minimumSupportingEvidence", minimumSupportingEvidence);
            SetString(serialized, "feedbackText", "Compare every claim against the available evidence and its confidence.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetHistoricalBaseline(SerializedObject caseObject)
        {
            SerializedProperty baseline = caseObject.FindProperty("historicalBaseline");
            baseline.arraySize = 8;
            SetHistoricalRecord(baseline.GetArrayElementAtIndex(0), "mock_cold_fish", "mock_summit", DepthBand.Shallow);
            SetHistoricalRecord(baseline.GetArrayElementAtIndex(1), "mock_predator", "mock_summit", DepthBand.Shallow);
            SetHistoricalRecord(baseline.GetArrayElementAtIndex(2), "mock_stable_species", "mock_summit", DepthBand.Shallow);
            SetHistoricalRecord(baseline.GetArrayElementAtIndex(3), "mock_cold_fish", "mock_slope", DepthBand.Shallow);
            SetHistoricalRecord(baseline.GetArrayElementAtIndex(4), "mock_prey", "mock_slope", DepthBand.Shallow);
            SetHistoricalRecord(baseline.GetArrayElementAtIndex(5), "mock_deep_species", "mock_deep_zone", DepthBand.Deep);
            SetHistoricalRecord(baseline.GetArrayElementAtIndex(6), "mock_stable_species", "mock_deep_zone", DepthBand.Deep);
            SetHistoricalRecord(baseline.GetArrayElementAtIndex(7), "mock_stable_species", "mock_slope", DepthBand.Shallow);
        }

        private static void SetHistoricalRecord(
            SerializedProperty property,
            string speciesId,
            string siteId,
            DepthBand depthBand)
        {
            property.FindPropertyRelative("speciesId").stringValue = speciesId;
            property.FindPropertyRelative("siteId").stringValue = siteId;
            property.FindPropertyRelative("depthBand").enumValueIndex = (int)depthBand;
            property.FindPropertyRelative("expectedPresence").boolValue = true;
        }

        private static void SetInitialResults(SerializedObject caseObject)
        {
            SerializedProperty results = caseObject.FindProperty("initialResults");
            results.arraySize = 2;
            SetResult(
                results.GetArrayElementAtIndex(0),
                "initial_summit_shallow",
                "mock_summit",
                DepthBand.Shallow,
                0,
                SampleQuality.High,
                new[] { "mock_warm_fish", "mock_stable_species" },
                Array.Empty<EDNAResultFlag>());
            SetResult(
                results.GetArrayElementAtIndex(1),
                "initial_slope_shallow",
                "mock_slope",
                DepthBand.Shallow,
                0,
                SampleQuality.Low,
                new[] { "mock_prey", "mock_stable_species" },
                new[] { EDNAResultFlag.LowQuality, EDNAResultFlag.ContaminationWarning });
        }

        private static void SetMockOutcomes(SerializedObject caseObject)
        {
            SerializedProperty outcomes = caseObject.FindProperty("mockSampleOutcomes");
            outcomes.arraySize = 8;
            SetMockOutcome(outcomes.GetArrayElementAtIndex(0), "mock_summit", DepthBand.Shallow, SampleQuality.High, new[] { "mock_warm_fish", "mock_stable_species" });
            SetMockOutcome(outcomes.GetArrayElementAtIndex(1), "mock_summit", DepthBand.Mid, SampleQuality.Medium, new[] { "mock_warm_fish", "mock_cold_fish", "mock_stable_species" });
            SetMockOutcome(outcomes.GetArrayElementAtIndex(2), "mock_summit", DepthBand.Deep, SampleQuality.High, new[] { "mock_cold_fish", "mock_stable_species" });
            SetMockOutcome(outcomes.GetArrayElementAtIndex(3), "mock_slope", DepthBand.Shallow, SampleQuality.High, new[] { "mock_prey", "mock_stable_species" });
            SetMockOutcome(outcomes.GetArrayElementAtIndex(4), "mock_slope", DepthBand.Mid, SampleQuality.Medium, new[] { "mock_prey", "mock_stable_species" });
            SetMockOutcome(outcomes.GetArrayElementAtIndex(5), "mock_slope", DepthBand.Deep, SampleQuality.Medium, new[] { "mock_deep_species" });
            SetMockOutcome(outcomes.GetArrayElementAtIndex(6), "mock_deep_zone", DepthBand.Mid, SampleQuality.Medium, new[] { "mock_deep_species" });
            SetMockOutcome(outcomes.GetArrayElementAtIndex(7), "mock_deep_zone", DepthBand.Deep, SampleQuality.High, new[] { "mock_deep_species", "mock_stable_species" });
        }

        private static void SetMockOutcome(
            SerializedProperty property,
            string siteId,
            DepthBand depthBand,
            SampleQuality quality,
            string[] speciesIds)
        {
            property.FindPropertyRelative("siteId").stringValue = siteId;
            property.FindPropertyRelative("depthBand").enumValueIndex = (int)depthBand;
            SetResult(
                property.FindPropertyRelative("resultTemplate"),
                string.Empty,
                siteId,
                depthBand,
                0,
                quality,
                speciesIds,
                Array.Empty<EDNAResultFlag>());
        }

        private static void SetResult(
            SerializedProperty property,
            string sampleId,
            string siteId,
            DepthBand depthBand,
            int roundIndex,
            SampleQuality quality,
            string[] speciesIds,
            EDNAResultFlag[] flags)
        {
            property.FindPropertyRelative("requestId").stringValue = string.Empty;
            property.FindPropertyRelative("sampleId").stringValue = sampleId;
            property.FindPropertyRelative("siteId").stringValue = siteId;
            property.FindPropertyRelative("depthBand").enumValueIndex = (int)depthBand;
            property.FindPropertyRelative("roundIndex").intValue = roundIndex;
            property.FindPropertyRelative("sampleQuality").enumValueIndex = (int)quality;
            SetStringArray(property.FindPropertyRelative("detectedSpeciesIds"), speciesIds);
            SetEnumArray(property.FindPropertyRelative("resultFlags"), flags);
        }

        private static GameObject CreatePresentationPrefabs()
        {
            InvestigationButtonView buttonPrefab = CreateButtonPrefab();
            SpeciesComparisonCardView cardPrefab = CreateComparisonCardPrefab();
            SampleComparisonBoardView boardPrefab = CreateComparisonBoardPrefab();
            InvestigationStatusBannerView statusBannerPrefab = CreateStatusBannerPrefab();
            return CreateRuntimeViewPrefab(buttonPrefab, boardPrefab, cardPrefab, statusBannerPrefab);
        }

        private static InvestigationButtonView CreateButtonPrefab()
        {
            const string path = PrefabRoot + "/InvestigationButton.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing.GetComponent<InvestigationButtonView>();
            }

            GameObject root = new GameObject(
                "Investigation Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(InvestigationButtonView));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 50f);
            Image background = root.GetComponent<Image>();
            background.color = new Color32(23, 104, 115, 255);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(50, 204, 209, 255);
            colors.pressedColor = new Color32(245, 230, 190, 255);
            colors.selectedColor = background.color;
            button.colors = colors;
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.minHeight = 0f;
            layout.preferredHeight = 50f;
            layout.flexibleWidth = 1f;

            Text label = CreateText("Label", root.transform, "BUTTON", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 8f, 4f, -8f, -4f);
            root.GetComponent<InvestigationButtonView>().ConfigureReferences(button, background, label);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<InvestigationButtonView>();
        }

        private static SpeciesComparisonCardView CreateComparisonCardPrefab()
        {
            const string path = PrefabRoot + "/SpeciesComparisonCard.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing.GetComponent<SpeciesComparisonCardView>();
            }

            GameObject root = new GameObject(
                "Species Comparison Card",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(SpeciesComparisonCardView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1400f, 190f);
            Image background = root.GetComponent<Image>();
            background.color = new Color32(20, 64, 82, 255);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(31, 126, 136, 255);
            colors.pressedColor = new Color32(245, 230, 190, 255);
            button.colors = colors;
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.minHeight = 190f;
            layout.preferredHeight = 190f;
            layout.flexibleWidth = 1f;

            RectTransform portrait = CreatePanel("Portrait Placeholder", root.transform, new Color32(35, 105, 119, 255));
            Anchor(portrait, 0f, 0f, 0f, 1f, 12f, 12f, 174f, -12f);
            Text portraitText = CreateText("Portrait Marker", portrait, "eDNA\nDETECTED", 18, FontStyle.Bold, new Color32(245, 230, 190, 255), TextAnchor.MiddleCenter);
            Stretch(portraitText.rectTransform, 8f, 8f, -8f, -8f);

            Text nameText = CreateText("Species Name", root.transform, "Species Name", 24, FontStyle.Bold, new Color32(245, 230, 190, 255), TextAnchor.MiddleLeft);
            Anchor(nameText.rectTransform, 0f, 0.7f, 1f, 1f, 194f, 0f, -18f, -8f);
            Text historicalText = CreateText("Historical Data", root.transform, "20 YEARS AGO", 17, FontStyle.Bold, new Color32(169, 201, 207, 255), TextAnchor.UpperLeft);
            Anchor(historicalText.rectTransform, 0f, 0.27f, 0.46f, 0.7f, 194f, 4f, -8f, -4f);
            Text currentText = CreateText("Current Data", root.transform, "CURRENT SAMPLE", 17, FontStyle.Bold, new Color32(50, 204, 209, 255), TextAnchor.UpperLeft);
            Anchor(currentText.rectTransform, 0.46f, 0.27f, 0.7f, 0.7f, 8f, 4f, -8f, -4f);
            Text traitsText = CreateText("Characteristics", root.transform, "CHARACTERISTICS", 15, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            Anchor(traitsText.rectTransform, 0.7f, 0.27f, 1f, 0.7f, 8f, 4f, -18f, -4f);
            Text findingText = CreateText("Finding State", root.transform, "SELECT AND CLASSIFY", 16, FontStyle.Bold, new Color32(255, 190, 90, 255), TextAnchor.MiddleLeft);
            Anchor(findingText.rectTransform, 0f, 0f, 1f, 0.27f, 194f, 4f, -18f, -2f);

            root.GetComponent<SpeciesComparisonCardView>().ConfigureReferences(
                button,
                background,
                portrait.GetComponent<Image>(),
                portraitText,
                nameText,
                historicalText,
                currentText,
                traitsText,
                findingText);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<SpeciesComparisonCardView>();
        }

        private static SampleComparisonBoardView CreateComparisonBoardPrefab()
        {
            const string path = PrefabRoot + "/SampleComparisonBoard.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing.GetComponent<SampleComparisonBoardView>();
            }

            GameObject root = new GameObject(
                "Sample Comparison Board",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter),
                typeof(SampleComparisonBoardView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1400f, 0f);
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 12);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            root.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text header = CreateLayoutText("Sample Header", root.transform, "SAMPLE COMPARISON", 24, FontStyle.Bold, new Color32(245, 230, 190, 255), 42f);
            Text instructions = CreateLayoutText("Instructions", root.transform, "Select a card and classify the change.", 17, FontStyle.Normal, new Color32(169, 201, 207, 255), 58f);

            GameObject cardsObject = new GameObject("Comparison Cards", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cardsObject.transform.SetParent(root.transform, false);
            RectTransform cardsRoot = cardsObject.GetComponent<RectTransform>();
            VerticalLayoutGroup cardsLayout = cardsObject.GetComponent<VerticalLayoutGroup>();
            cardsLayout.spacing = 10f;
            cardsLayout.childControlWidth = true;
            cardsLayout.childControlHeight = true;
            cardsLayout.childForceExpandWidth = true;
            cardsLayout.childForceExpandHeight = false;
            cardsObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text findings = CreateLayoutText("Findings Summary", root.transform, "Identified findings: 0", 16, FontStyle.Italic, new Color32(50, 204, 209, 255), 38f);
            root.GetComponent<SampleComparisonBoardView>().ConfigureReferences(header, instructions, cardsRoot, findings);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<SampleComparisonBoardView>();
        }

        private static InvestigationStatusBannerView CreateStatusBannerPrefab()
        {
            const string path = PrefabRoot + "/InvestigationStatusBanner.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing.GetComponent<InvestigationStatusBannerView>();
            }

            GameObject root = new GameObject(
                "Investigation Status Banner",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(InvestigationStatusBannerView));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 64f);
            Image background = root.GetComponent<Image>();
            background.color = new Color32(7, 25, 38, 250);
            background.raycastTarget = false;

            RectTransform accent = CreatePanel("Status Accent", root.transform, new Color32(50, 204, 209, 255));
            Anchor(accent, 0f, 0f, 0f, 1f, 0f, 0f, 6f, 0f);
            accent.GetComponent<Image>().raycastTarget = false;

            Text label = CreateText("Status Label", root.transform, "NEXT STEP", 13, FontStyle.Bold, new Color32(50, 204, 209, 255), TextAnchor.UpperLeft);
            Anchor(label.rectTransform, 0f, 0.62f, 1f, 1f, 18f, 0f, -16f, -6f);
            label.raycastTarget = false;

            Text message = CreateText("Status Message", root.transform, "Review the case briefing and species records, then start the comparison.", 18, FontStyle.Normal, new Color32(245, 230, 190, 255), TextAnchor.LowerLeft);
            Anchor(message.rectTransform, 0f, 0f, 1f, 0.68f, 18f, 5f, -16f, -2f);
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Truncate;
            message.raycastTarget = false;

            root.GetComponent<InvestigationStatusBannerView>().ConfigureReferences(
                root.GetComponent<CanvasGroup>(),
                background,
                accent.GetComponent<Image>(),
                label,
                message);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<InvestigationStatusBannerView>();
        }

        private static GameObject CreateRuntimeViewPrefab(
            InvestigationButtonView buttonPrefab,
            SampleComparisonBoardView boardPrefab,
            SpeciesComparisonCardView cardPrefab,
            InvestigationStatusBannerView statusBannerPrefab)
        {
            const string path = PrefabRoot + "/InvestigationRuntimeView.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && existing.GetComponentInChildren<InvestigationStatusBannerView>(true) != null)
            {
                return existing;
            }

            GameObject root = new GameObject(
                "Investigation UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(InvestigationRuntimeView));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = CreatePanel("Ocean Background", root.transform, new Color32(10, 32, 48, 255));
            Stretch(background, 0f, 0f, 0f, 0f);

            RectTransform header = CreatePanel("Header", background, new Color32(18, 54, 73, 245));
            Anchor(header, 0f, 1f, 1f, 1f, 16f, -154f, -16f, -12f);
            Text title = CreateText("Title", header, "eDNA DETECTIVES", 30, FontStyle.Bold, new Color32(245, 230, 190, 255), TextAnchor.UpperLeft);
            Anchor(title.rectTransform, 0f, 0.54f, 0.62f, 1f, 22f, 0f, 0f, -8f);
            Text progress = CreateText("Progress", header, "ROUND 0    SAMPLES: 2    FINDINGS: 0/0", 17, FontStyle.Normal, new Color32(50, 204, 209, 255), TextAnchor.UpperRight);
            Anchor(progress.rectTransform, 0.62f, 0.54f, 1f, 1f, 0f, 0f, -22f, -8f);
            GameObject statusBannerObject = (GameObject)PrefabUtility.InstantiatePrefab(statusBannerPrefab.gameObject, header);
            RectTransform statusBannerRect = statusBannerObject.GetComponent<RectTransform>();
            Anchor(statusBannerRect, 0f, 0f, 1f, 0f, 22f, 8f, -22f, 72f);
            InvestigationStatusBannerView statusBanner = statusBannerObject.GetComponent<InvestigationStatusBannerView>();

            RectTransform navigation = CreatePanel("Navigation", background, new Color32(7, 25, 38, 255));
            Anchor(navigation, 0f, 1f, 1f, 1f, 16f, -214f, -16f, -162f);
            HorizontalLayoutGroup navigationLayout = navigation.gameObject.AddComponent<HorizontalLayoutGroup>();
            navigationLayout.spacing = 8f;
            navigationLayout.padding = new RectOffset(8, 8, 6, 6);
            navigationLayout.childControlWidth = true;
            navigationLayout.childControlHeight = true;
            navigationLayout.childForceExpandWidth = true;
            navigationLayout.childForceExpandHeight = true;

            RectTransform contentPanel = CreatePanel("Content Panel", background, new Color32(18, 54, 73, 245));
            Anchor(contentPanel, 0f, 0f, 1f, 1f, 16f, 164f, -16f, -226f);
            ScrollRect scroll = contentPanel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 12f;
            RectTransform viewport = CreatePanel("Viewport", contentPanel, new Color(0f, 0f, 0f, 0f));
            Stretch(viewport, 18f, 18f, -18f, -18f);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            Text body = CreateText("Case Content", viewport, "Case loading...", 21, FontStyle.Normal, new Color32(245, 230, 190, 255), TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.lineSpacing = 1.15f;
            body.rectTransform.anchorMin = new Vector2(0f, 1f);
            body.rectTransform.anchorMax = new Vector2(1f, 1f);
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.rectTransform.anchoredPosition = Vector2.zero;
            body.rectTransform.sizeDelta = Vector2.zero;
            ContentSizeFitter bodyFitter = body.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = body.rectTransform;

            RectTransform actions = CreatePanel("Actions", background, new Color32(7, 25, 38, 255));
            Anchor(actions, 0f, 0f, 1f, 0f, 16f, 16f, -16f, 148f);
            GridLayoutGroup actionLayout = actions.gameObject.AddComponent<GridLayoutGroup>();
            actionLayout.padding = new RectOffset(12, 12, 12, 12);
            actionLayout.spacing = new Vector2(10f, 10f);
            actionLayout.cellSize = new Vector2(365f, 49f);
            actionLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            actionLayout.constraintCount = 4;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;

            root.GetComponent<InvestigationRuntimeView>().ConfigureReferences(
                title,
                progress,
                statusBanner.MessageText,
                statusBanner,
                body,
                navigation,
                actions,
                viewport,
                scroll,
                buttonPrefab,
                boardPrefab,
                cardPrefab);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static Text CreateLayoutText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle style,
            Color color,
            float preferredHeight)
        {
            Text text = CreateText(name, parent, value, fontSize, style, color, TextAnchor.MiddleLeft);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = preferredHeight;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;
            return text;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = false;
            return text;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel.GetComponent<RectTransform>();
        }

        private static void Anchor(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY,
            float left,
            float bottom,
            float right,
            float top)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            Anchor(rect, 0f, 0f, 1f, 1f, left, bottom, right, top);
        }

        private static void CreateScene(
            InvestigationCaseDefinition caseDefinition,
            GameObject viewPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "InvestigationScene";

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(5, 20, 31, 255);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject demoObject = new GameObject("Investigation Demo", typeof(InvestigationDemoBootstrap));
            SerializedObject bootstrap = new SerializedObject(demoObject.GetComponent<InvestigationDemoBootstrap>());
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
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            string[] parts = path.Split('/');
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            serialized.FindProperty(propertyName).stringValue = value;
        }

        private static void SetInteger(SerializedObject serialized, string propertyName, int value)
        {
            serialized.FindProperty(propertyName).intValue = value;
        }

        private static void SetBoolean(SerializedObject serialized, string propertyName, bool value)
        {
            serialized.FindProperty(propertyName).boolValue = value;
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, UnityEngine.Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void SetStringArray(SerializedObject serialized, string propertyName, string[] values)
        {
            SetStringArray(serialized.FindProperty(propertyName), values);
        }

        private static void SetStringArray(SerializedProperty property, string[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue = values[index];
            }
        }

        private static void SetEnumArray<T>(SerializedObject serialized, string propertyName, T[] values)
            where T : Enum
        {
            SetEnumArray(serialized.FindProperty(propertyName), values);
        }

        private static void SetEnumArray<T>(SerializedProperty property, T[] values)
            where T : Enum
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).enumValueIndex = Convert.ToInt32(values[index]);
            }
        }
    }
}
