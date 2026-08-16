using EDNA.Investigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InvestigationUiPrefabBuilder
{
    private const string HypothesisPrefabPath = "Assets/Prefabs/Investigation/InvestigationHypothesisPanel.prefab";
    private const string CaseFilesPrefabPath = "Assets/Prefabs/Investigation/InvestigationCaseFilesPanel.prefab";
    private const string SamplePlannerPrefabPath = "Assets/Prefabs/Investigation/InvestigationSamplePlannerPanel.prefab";
    private const string StepperPrefabPath = "Assets/Prefabs/Investigation/InvestigationStepper.prefab";
    private const string ButtonPrefabPath = "Assets/Prefabs/Investigation/InvestigationButton.prefab";
    private const string RuntimePrefabPath = "Assets/Prefabs/Investigation/InvestigationRuntimeView.prefab";
    private const string CardPrefabPath = "Assets/Prefabs/Investigation/SpeciesComparisonCard.prefab";
    private const string BoardPrefabPath = "Assets/Prefabs/Investigation/SampleComparisonBoard.prefab";
    private const string StatusPrefabPath = "Assets/Prefabs/Investigation/InvestigationStatusBanner.prefab";

    private static readonly Color Sand = InvestigationTheme.Sand;
    private static readonly Color Muted = InvestigationTheme.TextSecondary;
    private static readonly Color Metadata = InvestigationTheme.TextMuted;
    private static readonly Color Panel = InvestigationTheme.Surface;
    private static readonly Color Cyan = InvestigationTheme.Primary;

    [MenuItem("eDNA Detectives/Rebuild Investigation UI Prefabs")]
    public static void Rebuild()
    {
        CreateCaseFilesPanelPrefab();
        CreateStepperPrefab();
        CreateHypothesisPanelPrefab();
        CreateSamplePlannerPanelPrefab();
        UpdateStatusBannerPrefab();
        UpdateButtonPrefab();
        UpdateRuntimePrefab();
        UpdateComparisonBoardPrefab();
        UpdateComparisonCardPrefab();
        ApplyTypographyToUiPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Investigation UI prefabs rebuilt.");
    }

    private static void CreateCaseFilesPanelPrefab()
    {
        GameObject root = new GameObject(
            "InvestigationCaseFilesPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter),
            typeof(InvestigationCaseFilesPanelView));
        root.layer = 5;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1400f, 0f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        root.GetComponent<Image>().color = InvestigationTheme.WithAlpha(InvestigationTheme.Surface, 0.94f);
        ConfigureRoundedCorners(root, InvestigationTheme.CornerRadiusCard);
        ConfigureOutline(root.GetComponent<Outline>(), InvestigationTheme.BorderQuiet, 1f);
        root.GetComponent<Outline>().enabled = false;

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 20);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject caseCard = CreateCardContainer(root.transform, "Case Brief Card", 124f, 1f);
        VerticalLayoutGroup caseCardLayout = caseCard.GetComponent<VerticalLayoutGroup>();
        caseCardLayout.padding = new RectOffset(16, 16, 10, 10);
        caseCardLayout.spacing = 4f;
        CreateText(caseCard.transform, "Case Eyebrow", "ACTIVE CASE // OCEAN CHANGE INVESTIGATION", 12, FontStyle.Bold, Metadata, 18f);
        Text caseTitle = CreateText(caseCard.transform, "Case Title", "Case title", 24, FontStyle.Bold, Sand, 30f);
        Text briefing = CreateText(caseCard.transform, "Case Briefing", "Case briefing", 16, FontStyle.Normal, Muted, 48f);
        briefing.lineSpacing = 1.18f;
        AddAccentRail(caseCard.transform, Cyan);

        GameObject speciesCard = CreateCardContainer(root.transform, "Species Record Card", 238f, 1f);
        LayoutElement speciesCardLayout = speciesCard.GetComponent<LayoutElement>();
        speciesCardLayout.minHeight = -1f;
        speciesCardLayout.preferredHeight = -1f;
        Text speciesCounter = CreateText(speciesCard.transform, "Species Counter", "SPECIES RECORD  01 / 03", 12, FontStyle.Bold, Metadata, 22f);
        GameObject speciesOverview = CreateLayoutObject(speciesCard.transform, "Species Overview", 122f);
        HorizontalLayoutGroup overviewLayout = speciesOverview.AddComponent<HorizontalLayoutGroup>();
        overviewLayout.spacing = 16f;
        overviewLayout.childAlignment = TextAnchor.MiddleLeft;
        overviewLayout.childControlWidth = true;
        overviewLayout.childControlHeight = true;
        overviewLayout.childForceExpandWidth = false;
        overviewLayout.childForceExpandHeight = true;

        GameObject glyphPanel = CreateSurfaceObject(speciesOverview.transform, "Species Illustration", InvestigationTheme.SurfaceRaised);
        SetLayoutWidth(glyphPanel.GetComponent<LayoutElement>(), 112f, 0f);
        InvestigationGlyphGraphic glyph = CreateGlyph(glyphPanel.transform, "Species Line Art", InvestigationGlyph.Fish, InvestigationTheme.PrimarySoft);
        Stretch(glyph.rectTransform, 14f, 14f, 18f, 18f);

        GameObject speciesCopy = CreateCardContainer(speciesOverview.transform, "Species Copy", 122f, 1f);
        speciesCopy.GetComponent<Image>().color = Color.clear;
        speciesCopy.GetComponent<Outline>().enabled = false;
        Text speciesTitle = CreateText(speciesCopy.transform, "Species Title", "Species title", 22, FontStyle.Bold, Sand, 34f);
        Text speciesDescription = CreateText(speciesCopy.transform, "Species Description", "Species description", 16, FontStyle.Normal, Muted, 66f);
        speciesDescription.lineSpacing = 1.15f;

        GameObject traitGridObject = CreateLayoutObject(speciesCard.transform, "Trait Grid", 42f);
        GridLayoutGroup traitGrid = traitGridObject.AddComponent<GridLayoutGroup>();
        traitGrid.padding = new RectOffset(0, 0, 0, 0);
        traitGrid.spacing = new Vector2(8f, 8f);
        traitGrid.cellSize = new Vector2(280f, 42f);
        traitGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        traitGrid.constraintCount = 4;
        InvestigationResponsiveGridLayout responsiveTraitGrid = traitGridObject.AddComponent<InvestigationResponsiveGridLayout>();
        Text depth = CreateTraitChip(traitGridObject.transform, "Depth Trait", "DEPTH RANGE: shallow, mid.");
        Text temperature = CreateTraitChip(traitGridObject.transform, "Temperature Trait", "TEMPERATURE: cold water.");
        Text habitat = CreateTraitChip(traitGridObject.transform, "Habitat Trait", "HABITAT: rocky reef.");
        Text sensitivity = CreateTraitChip(traitGridObject.transform, "Sensitivity Trait", "SENSITIVITY: cold sensitive.");
        responsiveTraitGrid.Configure(180f, 4, true);

        GameObject missionCard = CreateCardContainer(root.transform, "Mission Card", 94f, 1f);
        missionCard.GetComponent<Image>().color = InvestigationTheme.WithAlpha(InvestigationTheme.SurfaceSelected, 0.88f);
        CreateText(missionCard.transform, "Mission Eyebrow", "MISSION // INVESTIGATION PROTOCOL", 12, FontStyle.Bold, Metadata, 22f);
        Text mission = CreateText(missionCard.transform, "Mission Text", "Mission", 15, FontStyle.Normal, InvestigationTheme.TextPrimary, 48f);
        mission.lineSpacing = 1.12f;
        AddAccentRail(missionCard.transform, InvestigationTheme.Warning);

        root.GetComponent<InvestigationCaseFilesPanelView>().ConfigureReferences(
            caseTitle,
            briefing,
            glyph,
            speciesCounter,
            speciesTitle,
            speciesDescription,
            depth,
            temperature,
            habitat,
            sensitivity,
            mission);

        PrefabUtility.SaveAsPrefabAsset(root, CaseFilesPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void CreateHypothesisPanelPrefab()
    {
        GameObject root = new GameObject(
            "InvestigationHypothesisPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter),
            typeof(InvestigationHypothesisPanelView));
        root.layer = 5;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1400f, 0f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        root.GetComponent<Image>().color = InvestigationTheme.WithAlpha(Panel, 0.96f);
        ConfigureRoundedCorners(root, InvestigationTheme.CornerRadiusCard);
        ConfigureOutline(root.GetComponent<Outline>(), InvestigationTheme.BorderQuiet, 1f);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 20);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject headerRow = CreateLayoutObject(root.transform, "Reasoning Header", 38f);
        HorizontalLayoutGroup headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 10f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = true;
        Text heading = CreateText(headerRow.transform, "Working Hypothesis Heading", "REASONING WORKSPACE // BUILD A TESTABLE EXPLANATION", 13, FontStyle.Bold, Metadata, 38f);
        SetLayoutWidth(heading.GetComponent<LayoutElement>(), 640f, 1f);
        Text selectedIndicator = CreateText(headerRow.transform, "Selected Indicator", "SELECTED FOR CONCLUSION", 12, FontStyle.Bold, InvestigationTheme.Success, 38f);
        selectedIndicator.alignment = TextAnchor.MiddleRight;
        SetLayoutWidth(selectedIndicator.GetComponent<LayoutElement>(), 230f, 0f);
        selectedIndicator.gameObject.SetActive(false);

        RectTransform selectorRoot = CreateSelectorRoot(root.transform, "Hypothesis Selectors", 104f);

        GameObject workspace = CreateLayoutObject(root.transform, "Theory And Finding Workspace", 278f);
        HorizontalLayoutGroup workspaceLayout = workspace.AddComponent<HorizontalLayoutGroup>();
        workspaceLayout.spacing = 12f;
        workspaceLayout.childAlignment = TextAnchor.UpperLeft;
        workspaceLayout.childControlWidth = true;
        workspaceLayout.childControlHeight = true;
        workspaceLayout.childForceExpandWidth = false;
        workspaceLayout.childForceExpandHeight = true;

        GameObject theoryCard = CreateCardContainer(workspace.transform, "Theory Card", 278f, 1f);
        SetLayoutWidth(theoryCard.GetComponent<LayoutElement>(), 0f, 1.08f);
        Text counter = CreateText(theoryCard.transform, "Theory Counter", "THEORY 1 OF 3", 12, FontStyle.Bold, Metadata, 22f);
        Text title = CreateText(theoryCard.transform, "Hypothesis Title", "Hypothesis title", 20, FontStyle.Bold, Sand, 34f);
        Text description = CreateText(theoryCard.transform, "Hypothesis Description", "Hypothesis description", 16, FontStyle.Normal, Muted, 64f);
        description.lineSpacing = 1.15f;

        GameObject statusRow = CreateLayoutObject(theoryCard.transform, "Status Row", 34f);
        HorizontalLayoutGroup statusLayout = statusRow.AddComponent<HorizontalLayoutGroup>();
        statusLayout.spacing = 10f;
        statusLayout.childAlignment = TextAnchor.MiddleLeft;
        statusLayout.childControlWidth = true;
        statusLayout.childControlHeight = true;
        statusLayout.childForceExpandWidth = false;
        statusLayout.childForceExpandHeight = true;
        Text statusLabel = CreateText(statusRow.transform, "Status Label", "THEORY STATUS", 12, FontStyle.Bold, Metadata, 34f);
        LayoutElement statusLabelLayout = statusLabel.GetComponent<LayoutElement>();
        statusLabelLayout.preferredWidth = 116f;
        statusLabelLayout.flexibleWidth = 0f;

        GameObject badge = new GameObject("Status Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        badge.layer = 5;
        badge.transform.SetParent(statusRow.transform, false);
        Image badgeImage = badge.GetComponent<Image>();
        badgeImage.color = InvestigationTheme.TextMuted;
        badgeImage.raycastTarget = false;
        ConfigureRoundedCorners(badge, InvestigationTheme.CornerRadiusSmall);
        LayoutElement badgeLayout = badge.GetComponent<LayoutElement>();
        badgeLayout.preferredWidth = 156f;
        badgeLayout.preferredHeight = 30f;
        badgeLayout.flexibleWidth = 0f;
        Text status = CreateOverlayText(badge.transform, "Status", "UNEXPLORED", 12, FontStyle.Bold, InvestigationTheme.BackgroundDeep, TextAnchor.MiddleCenter);

        Text hypothesisMetadata = CreateText(
            theoryCard.transform,
            "Hypothesis Metadata",
            "Assigned support: 0    Assigned challenge: 0\nEvidence needed: New arrival, Different depth\nMinimum confidence: Medium",
            13,
            FontStyle.Normal,
            Metadata,
            74f);
        AddAccentRail(theoryCard.transform, Cyan);

        GameObject findingCard = CreateCardContainer(workspace.transform, "Finding Card", 278f, 1f);
        SetLayoutWidth(findingCard.GetComponent<LayoutElement>(), 0f, 0.92f);
        CreateText(findingCard.transform, "Finding Heading", "SELECTED EVIDENCE", 12, FontStyle.Bold, InvestigationTheme.Warning, 22f);
        Text findingTitle = CreateText(findingCard.transform, "Finding Title", "No identified finding yet", 18, FontStyle.Bold, Sand, 42f);
        Text findingDescription = CreateText(findingCard.transform, "Finding Description", "Identified finding details", 16, FontStyle.Normal, Muted, 76f);
        findingDescription.lineSpacing = 1.15f;
        Text findingMetadata = CreateText(findingCard.transform, "Finding Metadata", "Confidence and assignment", 13, FontStyle.Normal, Metadata, 34f);
        Text helper = CreateText(findingCard.transform, "Helper Text", "Assignment help", 14, FontStyle.Italic, Muted, 68f);
        helper.lineSpacing = 1.12f;
        AddAccentRail(findingCard.transform, InvestigationTheme.Warning);

        GameObject balanceRow = CreateLayoutObject(root.transform, "Evidence Balance", 78f);
        HorizontalLayoutGroup balanceLayout = balanceRow.AddComponent<HorizontalLayoutGroup>();
        balanceLayout.spacing = 12f;
        balanceLayout.childAlignment = TextAnchor.MiddleCenter;
        balanceLayout.childControlWidth = true;
        balanceLayout.childControlHeight = true;
        balanceLayout.childForceExpandWidth = true;
        balanceLayout.childForceExpandHeight = true;
        Text supportWell = CreateEvidenceWell(balanceRow.transform, "Support Well", InvestigationTheme.SurfaceSuccess, InvestigationTheme.Success, out Image supportWellImage);
        Text challengeWell = CreateEvidenceWell(balanceRow.transform, "Challenge Well", InvestigationTheme.SurfaceWarning, InvestigationTheme.Warning, out Image challengeWellImage);

        InvestigationHypothesisPanelView panel = root.GetComponent<InvestigationHypothesisPanelView>();
        panel.ConfigureReferences(
            counter,
            title,
            description,
            badgeImage,
            status,
            hypothesisMetadata,
            selectedIndicator,
            findingTitle,
            findingDescription,
            findingMetadata,
            helper,
            selectorRoot,
            supportWellImage,
            supportWell,
            challengeWellImage,
            challengeWell);

        PrefabUtility.SaveAsPrefabAsset(root, HypothesisPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void CreateSamplePlannerPanelPrefab()
    {
        GameObject root = new GameObject(
            "InvestigationSamplePlannerPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter),
            typeof(InvestigationSamplePlannerPanelView));
        root.layer = 5;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 0f);
        root.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        root.GetComponent<Image>().color = InvestigationTheme.WithAlpha(Panel, 0.96f);
        ConfigureRoundedCorners(root, InvestigationTheme.CornerRadiusCard);
        Outline rootOutline = root.AddComponent<Outline>();
        ConfigureOutline(rootOutline, InvestigationTheme.BorderQuiet, 1f);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 20);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(root.transform, "Plan Heading", "FOLLOW-UP SAMPLE PLAN // TEST THE THEORY", 13, FontStyle.Bold, Metadata, 24f);
        CreateText(
            root.transform,
            "Plan Help",
            "Choose a site, depth, and test target. The prototype returns an immediate lab result.",
            16,
            FontStyle.Normal,
            Muted,
            48f);
        RectTransform selectorRoot = CreateSelectorRoot(root.transform, "Sample Selectors", 164f);
        GameObject siteCard = CreateCardContainer(root.transform, "Selected Site Card", 164f, 1f);
        CreateText(siteCard.transform, "Site Details Heading", "SELECTED SITE // FIELD CONDITIONS", 12, FontStyle.Bold, Metadata, 22f);
        Text siteTitle = CreateText(siteCard.transform, "Site Title", "Site title", 20, FontStyle.Bold, Sand, 34f);
        Text siteDescription = CreateText(siteCard.transform, "Site Description", "Site description", 16, FontStyle.Normal, Muted, 62f);
        Text metadata = CreateText(siteCard.transform, "Plan Metadata", "Related theory and sample availability", 13, FontStyle.Normal, Metadata, 32f);
        AddAccentRail(siteCard.transform, Cyan);

        root.GetComponent<InvestigationSamplePlannerPanelView>().ConfigureReferences(
            selectorRoot,
            siteTitle,
            siteDescription,
            metadata);
        PrefabUtility.SaveAsPrefabAsset(root, SamplePlannerPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void CreateStepperPrefab()
    {
        GameObject root = new GameObject(
            "InvestigationStepper",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement),
            typeof(InvestigationStepperView));
        root.layer = 5;
        root.GetComponent<Image>().color = InvestigationTheme.SurfaceRaised;
        ConfigureRoundedCorners(root, InvestigationTheme.CornerRadiusControl);
        Outline stepperOutline = root.AddComponent<Outline>();
        ConfigureOutline(stepperOutline, InvestigationTheme.BorderQuiet, 1f);
        LayoutElement rootElement = root.GetComponent<LayoutElement>();
        rootElement.minHeight = 48f;
        rootElement.preferredHeight = 48f;
        rootElement.flexibleWidth = 1f;

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 4, 4);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        Text category = CreateText(root.transform, "Category", "THEORY", 12, FontStyle.Bold, Metadata, 40f);
        SetLayoutWidth(category.GetComponent<LayoutElement>(), 142f, 0f);
        Button previous = CreateStepperButton(root.transform, "Previous", "‹");
        Text value = CreateText(root.transform, "Value", "Selected value", 16, FontStyle.Bold, Sand, 40f);
        SetLayoutWidth(value.GetComponent<LayoutElement>(), 400f, 1f);
        Button next = CreateStepperButton(root.transform, "Next", "›");
        Text count = CreateText(root.transform, "Count", "1 / 3", 13, FontStyle.Normal, Metadata, 40f);
        count.alignment = TextAnchor.MiddleRight;
        SetLayoutWidth(count.GetComponent<LayoutElement>(), 72f, 0f);

        root.GetComponent<InvestigationStepperView>().ConfigureReferences(category, previous, value, next, count);
        PrefabUtility.SaveAsPrefabAsset(root, StepperPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void UpdateRuntimePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(RuntimePrefabPath);
        try
        {
            InvestigationRuntimeView view = root.GetComponent<InvestigationRuntimeView>();
            ApplyRuntimeShell(root);
            Transform actions = FindChild(root.transform, "Actions");
            Transform contentPanel = FindChild(root.transform, "Content Panel");
            Text body = FindChild(root.transform, "Case Content").GetComponent<Text>();
            body.supportRichText = true;
            body.fontSize = 16;
            body.color = InvestigationTheme.TextPrimary;
            body.lineSpacing = 1.2f;

            RectTransform standardActions = CreateStandardActions(actions);
            RectTransform classificationPanel = CreateClassificationPanel(actions);
            Text classificationPrompt = FindChild(classificationPanel, "Classification Prompt").GetComponent<Text>();
            RectTransform classificationRoot = FindChild(classificationPanel, "Classification Choices").GetComponent<RectTransform>();
            RectTransform compareNavigation = CreateCompareNavigation(actions);
            Scrollbar scrollbar = CreateVerticalScrollbar(contentPanel);
            InvestigationStatusBannerView statusView = root.GetComponentInChildren<InvestigationStatusBannerView>(true);
            InvestigationProgressView progressView = FindChild(root.transform, "Progress Metrics").GetComponent<InvestigationProgressView>();
            RectTransform navigation = FindChild(root.transform, "Navigation").GetComponent<RectTransform>();
            RectTransform content = contentPanel.GetComponent<RectTransform>();
            RectTransform actionsRect = actions.GetComponent<RectTransform>();

            InvestigationAdaptiveShellLayout shellLayout = root.GetComponent<InvestigationAdaptiveShellLayout>();
            if (shellLayout == null) shellLayout = root.AddComponent<InvestigationAdaptiveShellLayout>();
            SerializedObject serializedShellLayout = new SerializedObject(shellLayout);
            serializedShellLayout.FindProperty("compactActionsHeight").floatValue = 48f;
            serializedShellLayout.FindProperty("comparisonActionsHeight").floatValue = 128f;
            serializedShellLayout.FindProperty("minimumStatusHeight").floatValue = 32f;
            serializedShellLayout.FindProperty("maximumStatusHeight").floatValue = 48f;
            serializedShellLayout.FindProperty("statusTopOffset").floatValue = 116f;
            serializedShellLayout.FindProperty("statusContentOverlap").floatValue = 16f;
            serializedShellLayout.FindProperty("regionGap").floatValue = 6f;
            serializedShellLayout.ApplyModifiedPropertiesWithoutUndo();
            shellLayout.ConfigureReferences(
                statusView.GetComponent<RectTransform>(),
                statusView.MessageText,
                content,
                actionsRect,
                standardActions,
                classificationPanel,
                classificationRoot.GetComponent<InvestigationResponsiveGridLayout>(),
                compareNavigation,
                null,
                compareNavigation.GetComponent<InvestigationResponsiveGridLayout>());
            shellLayout.SetComparisonMode(false);

            InvestigationAccessibilityBridge accessibility = root.GetComponent<InvestigationAccessibilityBridge>();
            if (accessibility == null) accessibility = root.AddComponent<InvestigationAccessibilityBridge>();
            accessibility.ConfigureReferences(
                FindChild(root.transform, "Title").GetComponent<RectTransform>(),
                progressView.GetComponent<RectTransform>(),
                statusView.GetComponent<RectTransform>(),
                navigation,
                content,
                standardActions,
                compareNavigation,
                classificationRoot);

            ScrollRect scrollRect = contentPanel.GetComponent<ScrollRect>();
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.elasticity = 0f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalScrollbarSpacing = 4f;
            RectTransform viewport = scrollRect.viewport;
            viewport.sizeDelta = new Vector2(-54f, viewport.sizeDelta.y);
            viewport.anchoredPosition = new Vector2(-9f, viewport.anchoredPosition.y);

            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("actionRoot").objectReferenceValue = standardActions;
            serializedView.FindProperty("compareNavigationRoot").objectReferenceValue = compareNavigation;
            serializedView.FindProperty("classificationPanel").objectReferenceValue = classificationPanel;
            serializedView.FindProperty("classificationRoot").objectReferenceValue = classificationRoot;
            serializedView.FindProperty("classificationPromptText").objectReferenceValue = classificationPrompt;
            serializedView.FindProperty("progressView").objectReferenceValue = progressView;
            serializedView.FindProperty("adaptiveShellLayout").objectReferenceValue = shellLayout;
            serializedView.FindProperty("accessibilityBridge").objectReferenceValue = accessibility;
            GameObject caseFilesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaseFilesPrefabPath);
            GameObject hypothesisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HypothesisPrefabPath);
            GameObject samplePlannerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamplePlannerPrefabPath);
            GameObject stepperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StepperPrefabPath);
            serializedView.FindProperty("caseFilesPanelPrefab").objectReferenceValue =
                caseFilesPrefab.GetComponent<InvestigationCaseFilesPanelView>();
            serializedView.FindProperty("hypothesisPanelPrefab").objectReferenceValue =
                hypothesisPrefab.GetComponent<InvestigationHypothesisPanelView>();
            serializedView.FindProperty("samplePlannerPanelPrefab").objectReferenceValue =
                samplePlannerPrefab.GetComponent<InvestigationSamplePlannerPanelView>();
            serializedView.FindProperty("stepperPrefab").objectReferenceValue =
                stepperPrefab.GetComponent<InvestigationStepperView>();
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            classificationPanel.gameObject.SetActive(false);
            compareNavigation.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, RuntimePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateButtonPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ButtonPrefabPath);
        try
        {
            Image background = root.GetComponent<Image>();
            background.color = InvestigationTheme.SurfaceSelected;
            ConfigureRoundedCorners(root, InvestigationTheme.CornerRadiusControl);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.minHeight = 44f;
            layout.preferredHeight = 44f;

            Outline outline = root.GetComponent<Outline>();
            if (outline == null) outline = root.AddComponent<Outline>();
            outline.enabled = false;
            outline.effectColor = new Color32(255, 190, 90, 255);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;

            Transform existingGlyph = FindDirectChild(root.transform, "Button Glyph");
            if (existingGlyph != null) Object.DestroyImmediate(existingGlyph.gameObject);
            InvestigationGlyphGraphic glyph = CreateGlyph(root.transform, "Button Glyph", InvestigationGlyph.None, Metadata);
            SetOffsets(glyph.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), 12f, 9f, 38f, -9f);
            glyph.gameObject.SetActive(false);

            Transform existingIndicator = FindDirectChild(root.transform, "Active Indicator");
            if (existingIndicator != null) Object.DestroyImmediate(existingIndicator.gameObject);
            GameObject indicatorObject = new GameObject("Active Indicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            indicatorObject.layer = 5;
            indicatorObject.transform.SetParent(root.transform, false);
            RectTransform indicatorRect = indicatorObject.GetComponent<RectTransform>();
            SetOffsets(indicatorRect, new Vector2(0f, 0f), new Vector2(1f, 0f), 0f, 0f, 0f, 3f);
            Image indicator = indicatorObject.GetComponent<Image>();
            indicator.color = Cyan;
            indicator.raycastTarget = false;
            indicatorObject.SetActive(false);

            Transform existingBadge = FindDirectChild(root.transform, "Navigation Badge");
            if (existingBadge != null) Object.DestroyImmediate(existingBadge.gameObject);
            GameObject badgeObject = new GameObject(
                "Navigation Badge",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            badgeObject.layer = 5;
            badgeObject.transform.SetParent(root.transform, false);
            RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
            SetOffsets(badgeRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 12f, -12f, 36f, 12f);
            Image badgeBackground = badgeObject.GetComponent<Image>();
            badgeBackground.color = InvestigationTheme.SurfaceInteractive;
            badgeBackground.raycastTarget = false;
            ConfigureRoundedCorners(badgeObject, InvestigationTheme.CornerRadiusSmall);
            Text badgeText = CreateOverlayText(
                badgeObject.transform,
                "Badge Number",
                "01",
                11,
                FontStyle.Bold,
                InvestigationTheme.TextSecondary,
                TextAnchor.MiddleCenter);
            Stretch(badgeText.rectTransform, 0f, 0f, 0f, 0f);
            InvestigationTypography.Apply(badgeText, InvestigationFontRole.Data, FontStyle.Bold);
            InvestigationGlyphGraphic check = CreateGlyph(
                badgeObject.transform,
                "Completion Check",
                InvestigationGlyph.Check,
                InvestigationTheme.Success);
            Stretch(check.rectTransform, 5f, 5f, 5f, 5f);
            check.gameObject.SetActive(false);
            badgeObject.SetActive(false);

            Text label = FindDirectChild(root.transform, "Label").GetComponent<Text>();
            label.fontSize = 14;
            label.color = InvestigationTheme.TextPrimary;
            label.raycastTarget = false;
            InvestigationTypography.Apply(label, InvestigationFontRole.Interface, FontStyle.Bold);

            root.GetComponent<InvestigationButtonView>().ConfigureReferences(
                root.GetComponent<Button>(),
                background,
                label,
                outline,
                indicator,
                glyph,
                badgeBackground,
                badgeText,
                check);
            SerializedObject serializedButton = new SerializedObject(root.GetComponent<InvestigationButtonView>());
            serializedButton.FindProperty("primaryBackground").colorValue = InvestigationTheme.SurfaceInteractive;
            serializedButton.FindProperty("primaryText").colorValue = InvestigationTheme.TextPrimary;
            serializedButton.FindProperty("browseBackground").colorValue = InvestigationTheme.SurfaceRaised;
            serializedButton.FindProperty("browseText").colorValue = InvestigationTheme.TextSecondary;
            serializedButton.FindProperty("navigationBackground").colorValue = InvestigationTheme.BackgroundDeep;
            serializedButton.FindProperty("navigationText").colorValue = InvestigationTheme.TextMuted;
            serializedButton.FindProperty("commitBackground").colorValue = InvestigationTheme.Primary;
            serializedButton.FindProperty("commitText").colorValue = InvestigationTheme.BackgroundDeep;
            serializedButton.FindProperty("supportBackground").colorValue = InvestigationTheme.SurfaceSuccess;
            serializedButton.FindProperty("supportText").colorValue = InvestigationTheme.Success;
            serializedButton.FindProperty("challengeBackground").colorValue = InvestigationTheme.SurfaceWarning;
            serializedButton.FindProperty("challengeText").colorValue = InvestigationTheme.Warning;
            serializedButton.FindProperty("destructiveBackground").colorValue = InvestigationTheme.BackgroundDeep;
            serializedButton.FindProperty("destructiveText").colorValue = InvestigationTheme.Danger;
            serializedButton.FindProperty("destructiveOutline").colorValue = InvestigationTheme.Danger;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyRuntimeShell(GameObject root)
    {
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 600f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        Transform background = FindChild(root.transform, "Ocean Background");
        Transform header = FindChild(root.transform, "Header");
        Transform navigation = FindChild(root.transform, "Navigation");
        Transform content = FindChild(root.transform, "Content Panel");
        Transform actions = FindChild(root.transform, "Actions");
        InvestigationStatusBannerView statusView = root.GetComponentInChildren<InvestigationStatusBannerView>(true);
        Transform status = statusView == null ? null : statusView.transform;

        background.GetComponent<Image>().color = InvestigationTheme.Background;
        header.GetComponent<Image>().color = InvestigationTheme.WithAlpha(InvestigationTheme.SurfaceRaised, 0.96f);
        navigation.GetComponent<Image>().color = InvestigationTheme.BackgroundDeep;
        content.GetComponent<Image>().color = InvestigationTheme.WithAlpha(InvestigationTheme.Surface, 0.95f);
        actions.GetComponent<Image>().color = InvestigationTheme.BackgroundDeep;

        Transform existingBackdrop = FindDirectChild(background, "Research Backdrop");
        if (existingBackdrop != null) Object.DestroyImmediate(existingBackdrop.gameObject);
        GameObject backdropObject = new GameObject("Research Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(InvestigationBackdropGraphic));
        backdropObject.layer = 5;
        backdropObject.transform.SetParent(background, false);
        backdropObject.transform.SetAsFirstSibling();
        InvestigationBackdropGraphic backdrop = backdropObject.GetComponent<InvestigationBackdropGraphic>();
        backdrop.color = InvestigationTheme.WithAlpha(InvestigationTheme.TextMuted, 0.025f);
        backdrop.raycastTarget = false;
        Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);

        SetOffsets(header.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), 12f, -60f, -12f, -8f);
        SetOffsets(navigation.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), 12f, -112f, -12f, -64f);
        SetOffsets(content.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), 12f, 62f, -12f, -132f);
        SetOffsets(actions.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), 12f, 8f, -12f, 56f);

        if (status != null)
        {
            status.SetParent(background, false);
            SetOffsets(status.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), 28f, -148f, -28f, -116f);
        }

        Text title = FindChild(header, "Title").GetComponent<Text>();
        title.fontSize = 21;
        title.color = Sand;
        title.alignment = TextAnchor.LowerLeft;
        SetOffsets(title.rectTransform, new Vector2(0f, 0f), new Vector2(0.58f, 0.72f), 18f, 6f, -8f, -2f);

        Text progress = FindChild(header, "Progress").GetComponent<Text>();
        progress.fontSize = 14;
        progress.fontStyle = FontStyle.Bold;
        progress.color = Cyan;
        progress.alignment = TextAnchor.MiddleRight;
        SetOffsets(progress.rectTransform, new Vector2(0.56f, 0.15f), new Vector2(1f, 0.78f), 8f, 0f, -178f, 0f);
        progress.gameObject.SetActive(false);
        CreateProgressMetrics(header);

        Transform existingEyebrow = FindDirectChild(header, "Header Eyebrow");
        if (existingEyebrow != null) Object.DestroyImmediate(existingEyebrow.gameObject);
        Text eyebrow = CreateOverlayText(header, "Header Eyebrow", "MARINE eDNA INVESTIGATION // FIELD CONSOLE", 10, FontStyle.Bold, Metadata, TextAnchor.UpperLeft);
        SetOffsets(eyebrow.rectTransform, new Vector2(0f, 0.72f), new Vector2(0.58f, 1f), 18f, 0f, -8f, -6f);

        InvestigationResponsiveHeaderLayout headerLayout = header.GetComponent<InvestigationResponsiveHeaderLayout>();
        if (headerLayout == null) headerLayout = header.gameObject.AddComponent<InvestigationResponsiveHeaderLayout>();
        headerLayout.ConfigureReferences(
            title,
            eyebrow.rectTransform,
            FindChild(header, "Progress Metrics").GetComponent<RectTransform>());

        Transform existingLine = FindDirectChild(header, "Header Accent Line");
        if (existingLine != null) Object.DestroyImmediate(existingLine.gameObject);
        GameObject line = new GameObject("Header Accent Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        line.layer = 5;
        line.transform.SetParent(header, false);
        SetOffsets(line.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), 0f, 0f, 0f, 2f);
        line.GetComponent<Image>().color = InvestigationTheme.Border;
        line.GetComponent<Image>().raycastTarget = false;

        HorizontalLayoutGroup navigationLayout = navigation.GetComponent<HorizontalLayoutGroup>();
        navigationLayout.padding = new RectOffset(4, 4, 2, 2);
        navigationLayout.spacing = 0f;
        navigationLayout.childForceExpandWidth = true;
        navigationLayout.childForceExpandHeight = true;
        if (navigation.GetComponent<InvestigationResponsiveNavigationLayout>() == null)
        {
            navigation.gameObject.AddComponent<InvestigationResponsiveNavigationLayout>();
        }

        ConfigurePanelOutline(content.gameObject, InvestigationTheme.BorderQuiet);
        ConfigurePanelOutline(actions.gameObject, InvestigationTheme.BorderQuiet);
        AddTopRail(actions, "Actions Top Rail", InvestigationTheme.Border);
    }

    private static void CreateProgressMetrics(Transform header)
    {
        Transform existing = FindDirectChild(header, "Progress Metrics");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject(
            "Progress Metrics",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(InvestigationProgressView));
        root.layer = 5;
        root.transform.SetParent(header, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetOffsets(rect, new Vector2(0.50f, 0.12f), new Vector2(1f, 0.86f), 8f, 0f, -154f, 0f);

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        Text round = CreateProgressMetric(root.transform, "Round Metric", "ROUND", "1");
        Text samples = CreateProgressMetric(root.transform, "Samples Metric", "SAMPLES", "2");
        Text findings = CreateProgressMetric(root.transform, "Findings Metric", "FOUND", "0/0");
        Text missteps = CreateProgressMetric(root.transform, "Missteps Metric", "MISSTEPS", "0");
        root.GetComponent<InvestigationProgressView>().ConfigureReferences(round, samples, findings, missteps);
    }

    private static Text CreateProgressMetric(
        Transform parent,
        string name,
        string label,
        string value)
    {
        GameObject metric = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(LayoutElement));
        metric.layer = 5;
        metric.transform.SetParent(parent, false);
        Image background = metric.GetComponent<Image>();
        background.color = InvestigationTheme.WithAlpha(InvestigationTheme.SurfaceInteractive, 0.78f);
        background.raycastTarget = false;
        ConfigureRoundedCorners(metric, InvestigationTheme.CornerRadiusSmall);
        ConfigureOutline(metric.GetComponent<Outline>(), InvestigationTheme.BorderQuiet, 1f);
        metric.GetComponent<Outline>().enabled = false;
        LayoutElement metricLayout = metric.GetComponent<LayoutElement>();
        metricLayout.minWidth = 58f;
        metricLayout.preferredWidth = 72f;
        metricLayout.flexibleWidth = 1f;
        Text text = CreateOverlayText(
            metric.transform,
            "Metric Text",
            $"<size=10><color=#8EAEB5>{label}</color></size>\n<size=15><b><color=#F5E6BE>{value}</color></b></size>",
            14,
            FontStyle.Normal,
            Sand,
            TextAnchor.MiddleCenter);
        text.supportRichText = true;
        text.lineSpacing = 0.82f;
        Stretch(text.rectTransform, 3f, 2f, 3f, 2f);
        return text;
    }

    private static void UpdateStatusBannerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(StatusPrefabPath);
        try
        {
            Image background = root.GetComponent<Image>();
            background.color = InvestigationTheme.Surface;
            ConfigureRoundedCorners(root, InvestigationTheme.CornerRadiusCard);
            root.GetComponent<CanvasGroup>().blocksRaycasts = false;
            ConfigurePanelOutline(root, InvestigationTheme.BorderQuiet);
            Text label = FindChild(root.transform, "Status Label").GetComponent<Text>();
            Text message = FindChild(root.transform, "Status Message").GetComponent<Text>();
            Image accent = FindChild(root.transform, "Status Accent").GetComponent<Image>();
            label.fontSize = 11;
            label.fontStyle = FontStyle.Bold;
            label.color = Cyan;
            message.fontSize = 16;
            message.color = InvestigationTheme.TextPrimary;
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Truncate;
            message.lineSpacing = 1f;
            label.alignment = TextAnchor.MiddleLeft;
            message.alignment = TextAnchor.MiddleLeft;
            accent.color = Cyan;
            SetOffsets(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), 0f, 0f, 4f, 0f);
            SetOffsets(label.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), 16f, 0f, 104f, 0f);
            SetOffsets(message.rectTransform, Vector2.zero, Vector2.one, 112f, 4f, -16f, -4f);
            PrefabUtility.SaveAsPrefabAsset(root, StatusPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateComparisonBoardPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BoardPrefabPath);
        try
        {
            Image background = root.GetComponent<Image>();
            if (background == null) background = root.AddComponent<Image>();
            background.color = InvestigationTheme.WithAlpha(InvestigationTheme.Surface, 0.96f);
            background.raycastTarget = false;
            ConfigureRoundedCorners(root, InvestigationTheme.CornerRadiusCard);
            ConfigurePanelOutline(root, InvestigationTheme.BorderQuiet);

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 18);
            layout.spacing = 12f;

            Transform oldEyebrow = FindDirectChild(root.transform, "Board Eyebrow");
            if (oldEyebrow != null) Object.DestroyImmediate(oldEyebrow.gameObject);
            Text eyebrow = CreateText(root.transform, "Board Eyebrow", "FIELD SAMPLE // 20-YEAR BASELINE COMPARISON", 12, FontStyle.Bold, Metadata, 22f);
            eyebrow.transform.SetAsFirstSibling();

            Text header = FindChild(root.transform, "Sample Header").GetComponent<Text>();
            header.fontSize = 21;
            header.color = Sand;
            SetPreferredHeight(header.gameObject, 34f);
            Text instructions = FindChild(root.transform, "Instructions").GetComponent<Text>();
            instructions.fontSize = 15;
            instructions.color = Muted;
            instructions.lineSpacing = 1.15f;
            SetPreferredHeight(instructions.gameObject, 54f);
            Text findings = FindChild(root.transform, "Findings Summary").GetComponent<Text>();
            findings.fontSize = 14;
            findings.fontStyle = FontStyle.Bold;
            findings.color = InvestigationTheme.TextSecondary;
            SetPreferredHeight(findings.gameObject, 34f);

            VerticalLayoutGroup cards = FindChild(root.transform, "Comparison Cards").GetComponent<VerticalLayoutGroup>();
            cards.spacing = 12f;
            PrefabUtility.SaveAsPrefabAsset(root, BoardPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static RectTransform CreateStandardActions(Transform actions)
    {
        Transform existing = FindDirectChild(actions, "Standard Actions");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GridLayoutGroup legacyLayout = actions.GetComponent<GridLayoutGroup>();
        if (legacyLayout != null) Object.DestroyImmediate(legacyLayout);

        GameObject standard = new GameObject(
            "Standard Actions",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup));
        standard.layer = 5;
        standard.transform.SetParent(actions, false);
        RectTransform rect = standard.GetComponent<RectTransform>();
        Stretch(rect, 0f, 0f, 0f, 0f);

        HorizontalLayoutGroup layout = standard.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 2, 2);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        return rect;
    }

    private static RectTransform CreateSelectorRoot(Transform parent, string name, float preferredHeight)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        root.layer = 5;
        root.transform.SetParent(parent, false);
        LayoutElement element = root.GetComponent<LayoutElement>();
        element.preferredHeight = preferredHeight;
        element.flexibleWidth = 1f;

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return root.GetComponent<RectTransform>();
    }

    private static Button CreateStepperButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.layer = 5;
        buttonObject.transform.SetParent(parent, false);
        Image background = buttonObject.GetComponent<Image>();
        background.color = InvestigationTheme.BackgroundDeep;
        ConfigureRoundedCorners(buttonObject, InvestigationTheme.CornerRadiusSmall);
        Outline outline = buttonObject.AddComponent<Outline>();
        ConfigureOutline(outline, InvestigationTheme.Border, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.82f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.72f, 0.82f, 0.84f, 1f);
        colors.selectedColor = new Color(0.82f, 1f, 1f, 1f);
        colors.disabledColor = new Color(0.48f, 0.54f, 0.56f, 0.65f);
        colors.fadeDuration = InvestigationTheme.MotionFast;
        button.colors = colors;
        buttonObject.GetComponent<LayoutElement>().minWidth = 48f;
        SetLayoutWidth(buttonObject.GetComponent<LayoutElement>(), 48f, 0f);
        Text arrow = CreateOverlayText(buttonObject.transform, "Arrow", label, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        arrow.raycastTarget = false;
        return button;
    }

    private static void SetLayoutWidth(LayoutElement element, float preferredWidth, float flexibleWidth)
    {
        element.preferredWidth = preferredWidth;
        element.flexibleWidth = flexibleWidth;
    }

    private static RectTransform CreateClassificationPanel(Transform actions)
    {
        Transform existing = FindDirectChild(actions, "Classification Panel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject panel = new GameObject("Classification Panel", typeof(RectTransform));
        panel.layer = 5;
        panel.transform.SetParent(actions, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 2f);
        rect.offsetMax = new Vector2(-12f, -4f);

        Text prompt = CreateOverlayText(panel.transform, "Classification Prompt", "CLASSIFY THIS CARD", 13, FontStyle.Bold, Cyan, TextAnchor.UpperLeft);
        RectTransform promptRect = prompt.rectTransform;
        promptRect.anchorMin = new Vector2(0f, 1f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.pivot = new Vector2(0.5f, 1f);
        promptRect.offsetMin = new Vector2(4f, -21f);
        promptRect.offsetMax = new Vector2(-4f, 0f);

        GameObject choices = new GameObject(
            "Classification Choices",
            typeof(RectTransform),
            typeof(GridLayoutGroup),
            typeof(InvestigationResponsiveGridLayout));
        choices.layer = 5;
        choices.transform.SetParent(panel.transform, false);
        RectTransform choicesRect = choices.GetComponent<RectTransform>();
        choicesRect.anchorMin = Vector2.zero;
        choicesRect.anchorMax = Vector2.one;
        choicesRect.offsetMin = new Vector2(0f, 0f);
        choicesRect.offsetMax = new Vector2(0f, -22f);
        GridLayoutGroup layout = choices.GetComponent<GridLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = new Vector2(8f, 8f);
        layout.cellSize = new Vector2(144f, 44f);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        choices.GetComponent<InvestigationResponsiveGridLayout>().Configure(120f, 5);
        return rect;
    }

    private static RectTransform CreateCompareNavigation(Transform actions)
    {
        Transform existing = FindDirectChild(actions, "Compare Navigation");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject navigation = new GameObject(
            "Compare Navigation",
            typeof(RectTransform),
            typeof(GridLayoutGroup),
            typeof(InvestigationResponsiveGridLayout));
        navigation.layer = 5;
        navigation.transform.SetParent(actions, false);
        RectTransform rect = navigation.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.offsetMin = new Vector2(12f, 4f);
        rect.offsetMax = new Vector2(-12f, -2f);
        GridLayoutGroup grid = navigation.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.cellSize = new Vector2(300f, 44f);
        grid.spacing = new Vector2(16f, 0f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        navigation.GetComponent<InvestigationResponsiveGridLayout>().Configure(120f, 4);
        return rect;
    }

    private static Scrollbar CreateVerticalScrollbar(Transform contentPanel)
    {
        Transform existing = FindDirectChild(contentPanel, "Vertical Scrollbar");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject track = new GameObject("Vertical Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        track.layer = 5;
        track.transform.SetParent(contentPanel, false);
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = Vector2.one;
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(28f, -24f);
        trackRect.anchoredPosition = new Vector2(-2f, 0f);
        Image trackImage = track.GetComponent<Image>();
        trackImage.color = new Color32(7, 25, 38, 70);

        GameObject visualTrack = new GameObject("Track Visual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        visualTrack.layer = 5;
        visualTrack.transform.SetParent(track.transform, false);
        RectTransform visualTrackRect = visualTrack.GetComponent<RectTransform>();
        visualTrackRect.anchorMin = new Vector2(0.5f, 0f);
        visualTrackRect.anchorMax = new Vector2(0.5f, 1f);
        visualTrackRect.sizeDelta = new Vector2(6f, 0f);
        Image visualTrackImage = visualTrack.GetComponent<Image>();
        visualTrackImage.color = new Color32(7, 25, 38, 220);
        visualTrackImage.raycastTarget = false;

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.layer = 5;
        slidingArea.transform.SetParent(track.transform, false);
        RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
        Stretch(slidingRect, 9f, 2f, 9f, 2f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.layer = 5;
        handle.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        Stretch(handleRect, 0f, 0f, 0f, 0f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color32(50, 204, 209, 210);

        Scrollbar scrollbar = track.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;
        scrollbar.size = 0.25f;
        return scrollbar;
    }

    private static void UpdateComparisonCardPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(rootRect.sizeDelta.x, 186f);
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            rootLayout.minHeight = 186f;
            rootLayout.preferredHeight = 186f;
            root.GetComponent<Image>().color = InvestigationTheme.SurfaceRaised;
            ConfigureRoundedCorners(root, InvestigationTheme.CornerRadiusCard);
            Outline cardOutline = root.GetComponent<Outline>();
            if (cardOutline == null) cardOutline = root.AddComponent<Outline>();
            ConfigureOutline(cardOutline, InvestigationTheme.Border, 1f);

            RectTransform portraitRect = FindChild(root.transform, "Portrait Placeholder").GetComponent<RectTransform>();
            SetOffsets(portraitRect, new Vector2(0f, 0f), new Vector2(0f, 1f), 14f, 14f, 170f, -14f);
            portraitRect.GetComponent<Image>().color = InvestigationTheme.SurfaceInteractive;
            ConfigureRoundedCorners(portraitRect.gameObject, InvestigationTheme.CornerRadiusControl);
            ConfigurePanelOutline(portraitRect.gameObject, InvestigationTheme.Border);

            SetOffsets(FindChild(root.transform, "Species Name").GetComponent<RectTransform>(), new Vector2(0f, 0.72f), new Vector2(0.81f, 1f), 188f, 0f, -14f, -8f);
            SetOffsets(FindChild(root.transform, "Historical Data").GetComponent<RectTransform>(), new Vector2(0f, 0.36f), new Vector2(0.48f, 0.72f), 188f, 4f, -12f, -3f);
            SetOffsets(FindChild(root.transform, "Current Data").GetComponent<RectTransform>(), new Vector2(0.48f, 0.36f), new Vector2(0.81f, 0.72f), 12f, 4f, -14f, -3f);
            SetOffsets(FindChild(root.transform, "Characteristics").GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.81f, 0.34f), 198f, 8f, -16f, -7f);
            RectTransform findingRect = FindChild(root.transform, "Finding State").GetComponent<RectTransform>();
            SetOffsets(findingRect, new Vector2(0.82f, 0.1f), new Vector2(0.985f, 0.9f), 0f, 0f, 0f, 0f);

            Text nameText = FindChild(root.transform, "Species Name").GetComponent<Text>();
            nameText.fontSize = 21;
            nameText.color = Sand;
            nameText.alignment = TextAnchor.MiddleLeft;
            Text historicalText = FindChild(root.transform, "Historical Data").GetComponent<Text>();
            historicalText.fontSize = 15;
            historicalText.color = Muted;
            historicalText.lineSpacing = 1.1f;
            Text currentText = FindChild(root.transform, "Current Data").GetComponent<Text>();
            currentText.fontSize = 15;
            currentText.color = Cyan;
            currentText.lineSpacing = 1.1f;
            Text traitsText = FindChild(root.transform, "Characteristics").GetComponent<Text>();
            traitsText.fontSize = 14;
            traitsText.color = Muted;
            traitsText.alignment = TextAnchor.MiddleLeft;
            traitsText.verticalOverflow = VerticalWrapMode.Truncate;
            Text findingText = FindChild(root.transform, "Finding State").GetComponent<Text>();
            findingText.fontSize = 13;
            findingText.alignment = TextAnchor.MiddleCenter;

            Transform existingTraitBackground = FindDirectChild(root.transform, "Trait Rail Background");
            if (existingTraitBackground != null) Object.DestroyImmediate(existingTraitBackground.gameObject);
            GameObject traitBackground = new GameObject("Trait Rail Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            traitBackground.layer = 5;
            traitBackground.transform.SetParent(root.transform, false);
            RectTransform traitRect = traitBackground.GetComponent<RectTransform>();
            SetOffsets(traitRect, new Vector2(0f, 0f), new Vector2(0.81f, 0.34f), 188f, 6f, -14f, -6f);
            Image traitImage = traitBackground.GetComponent<Image>();
            traitImage.color = InvestigationTheme.WithAlpha(InvestigationTheme.BackgroundDeep, 0.62f);
            traitImage.raycastTarget = false;
            ConfigureRoundedCorners(traitBackground, InvestigationTheme.CornerRadiusSmall);
            ConfigureOutline(traitBackground.GetComponent<Outline>(), InvestigationTheme.BorderQuiet, 1f);
            traitRect.SetSiblingIndex(traitsText.rectTransform.GetSiblingIndex());

            Transform existingDivider = FindDirectChild(root.transform, "Timeline Divider");
            if (existingDivider != null) Object.DestroyImmediate(existingDivider.gameObject);
            GameObject divider = new GameObject("Timeline Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider.layer = 5;
            divider.transform.SetParent(root.transform, false);
            SetOffsets(divider.GetComponent<RectTransform>(), new Vector2(0.48f, 0.39f), new Vector2(0.48f, 0.68f), -1f, 0f, 1f, 0f);
            divider.GetComponent<Image>().color = InvestigationTheme.WithAlpha(Cyan, 0.36f);
            divider.GetComponent<Image>().raycastTarget = false;

            Transform existingGlyph = FindDirectChild(portraitRect, "Portrait Glyph");
            if (existingGlyph != null) Object.DestroyImmediate(existingGlyph.gameObject);
            InvestigationGlyphGraphic portraitGlyph = CreateGlyph(portraitRect, "Portrait Glyph", InvestigationGlyph.Dna, Sand);
            SetOffsets(portraitGlyph.rectTransform, new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.92f), 0f, 0f, 0f, 0f);
            Text portraitLabel = FindChild(portraitRect, "Portrait Marker").GetComponent<Text>();
            portraitLabel.fontSize = 11;
            portraitLabel.alignment = TextAnchor.LowerCenter;
            SetOffsets(portraitLabel.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.31f), 0f, 0f, 0f, 0f);

            Transform existingBackground = FindDirectChild(root.transform, "Card Action Background");
            if (existingBackground != null) Object.DestroyImmediate(existingBackground.gameObject);
            GameObject actionBackground = new GameObject("Card Action Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            actionBackground.layer = 5;
            actionBackground.transform.SetParent(root.transform, false);
            RectTransform actionRect = actionBackground.GetComponent<RectTransform>();
            SetOffsets(actionRect, new Vector2(0.82f, 0.1f), new Vector2(0.985f, 0.9f), 0f, 0f, 0f, 0f);
            Image actionImage = actionBackground.GetComponent<Image>();
            actionImage.color = new Color32(14, 48, 65, 255);
            actionImage.raycastTarget = false;
            ConfigureRoundedCorners(actionBackground, InvestigationTheme.CornerRadiusControl);
            Outline outline = actionBackground.GetComponent<Outline>();
            outline.effectColor = InvestigationTheme.WithAlpha(Cyan, 0.42f);
            outline.effectDistance = new Vector2(1f, -1f);
            actionRect.SetSiblingIndex(findingRect.GetSiblingIndex());

            SpeciesComparisonCardView card = root.GetComponent<SpeciesComparisonCardView>();
            InvestigationAttentionPulse attentionPulse = root.GetComponent<InvestigationAttentionPulse>();
            if (attentionPulse == null) attentionPulse = root.AddComponent<InvestigationAttentionPulse>();
            attentionPulse.enabled = false;
            SerializedObject serializedCard = new SerializedObject(card);
            serializedCard.FindProperty("actionBackground").objectReferenceValue = actionImage;
            serializedCard.FindProperty("traitsBackground").objectReferenceValue = traitImage;
            serializedCard.FindProperty("stateOutline").objectReferenceValue = cardOutline;
            serializedCard.FindProperty("portraitGlyph").objectReferenceValue = portraitGlyph;
            serializedCard.FindProperty("attentionPulse").objectReferenceValue = attentionPulse;
            serializedCard.ApplyModifiedPropertiesWithoutUndo();

            InvestigationResponsiveComparisonCardLayout responsiveLayout =
                root.GetComponent<InvestigationResponsiveComparisonCardLayout>();
            if (responsiveLayout == null)
            {
                responsiveLayout = root.AddComponent<InvestigationResponsiveComparisonCardLayout>();
            }
            responsiveLayout.ConfigureReferences(
                portraitRect,
                nameText.rectTransform,
                historicalText.rectTransform,
                currentText.rectTransform,
                traitsText.rectTransform,
                traitsText,
                findingRect,
                traitRect,
                actionRect,
                divider.GetComponent<RectTransform>());
            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject CreateLayoutObject(Transform parent, string name, float preferredHeight)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        child.layer = 5;
        child.transform.SetParent(parent, false);
        LayoutElement layout = child.GetComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;
        return child;
    }

    private static GameObject CreateCardContainer(
        Transform parent,
        string name,
        float preferredHeight,
        float flexibleWidth)
    {
        GameObject card = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement));
        card.layer = 5;
        card.transform.SetParent(parent, false);
        Image image = card.GetComponent<Image>();
        image.color = InvestigationTheme.SurfaceRaised;
        image.raycastTarget = false;
        ConfigureRoundedCorners(card, InvestigationTheme.CornerRadiusCard);
        ConfigureOutline(card.GetComponent<Outline>(), InvestigationTheme.BorderQuiet, 1f);
        card.GetComponent<Outline>().enabled = false;

        VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 14);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement element = card.GetComponent<LayoutElement>();
        element.preferredHeight = preferredHeight;
        element.flexibleWidth = flexibleWidth;
        return card;
    }

    private static GameObject CreateSurfaceObject(Transform parent, string name, Color color)
    {
        GameObject surface = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(LayoutElement));
        surface.layer = 5;
        surface.transform.SetParent(parent, false);
        Image image = surface.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        ConfigureRoundedCorners(surface, InvestigationTheme.CornerRadiusControl);
        ConfigureOutline(surface.GetComponent<Outline>(), InvestigationTheme.BorderQuiet, 1f);
        surface.GetComponent<Outline>().enabled = false;
        return surface;
    }

    private static InvestigationGlyphGraphic CreateGlyph(
        Transform parent,
        string name,
        InvestigationGlyph glyph,
        Color color)
    {
        GameObject icon = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(InvestigationGlyphGraphic));
        icon.layer = 5;
        icon.transform.SetParent(parent, false);
        InvestigationGlyphGraphic graphic = icon.GetComponent<InvestigationGlyphGraphic>();
        graphic.color = color;
        graphic.raycastTarget = false;
        graphic.SetGlyph(glyph);
        return graphic;
    }

    private static Text CreateTraitChip(Transform parent, string name, string value)
    {
        GameObject chip = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        chip.layer = 5;
        chip.transform.SetParent(parent, false);
        Image background = chip.GetComponent<Image>();
        background.color = InvestigationTheme.WithAlpha(InvestigationTheme.BackgroundDeep, 0.72f);
        background.raycastTarget = false;
        ConfigureRoundedCorners(chip, InvestigationTheme.CornerRadiusSmall);
        ConfigureOutline(chip.GetComponent<Outline>(), InvestigationTheme.BorderQuiet, 1f);
        chip.GetComponent<Outline>().enabled = false;
        Text text = CreateOverlayText(chip.transform, "Label", value, 12, FontStyle.Normal, Muted, TextAnchor.MiddleLeft);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 9;
        text.resizeTextMaxSize = 12;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        Stretch(text.rectTransform, 10f, 10f, 5f, 5f);
        return text;
    }

    private static Text CreateEvidenceWell(
        Transform parent,
        string name,
        Color backgroundColor,
        Color accentColor,
        out Image backgroundImage)
    {
        GameObject well = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        well.layer = 5;
        well.transform.SetParent(parent, false);
        backgroundImage = well.GetComponent<Image>();
        backgroundImage.color = InvestigationTheme.WithAlpha(backgroundColor, 0.72f);
        backgroundImage.raycastTarget = false;
        ConfigureRoundedCorners(well, InvestigationTheme.CornerRadiusControl);
        ConfigureOutline(well.GetComponent<Outline>(), accentColor, 1f);
        Text text = CreateOverlayText(well.transform, "Label", name.ToUpperInvariant(), 13, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft);
        Stretch(text.rectTransform, 16f, 12f, 8f, 8f);
        return text;
    }

    private static void AddAccentRail(Transform parent, Color color)
    {
        Transform existing = FindDirectChild(parent, "Accent Rail");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        GameObject rail = new GameObject("Accent Rail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        rail.layer = 5;
        rail.transform.SetParent(parent, false);
        rail.transform.SetAsFirstSibling();
        LayoutElement element = rail.GetComponent<LayoutElement>();
        element.ignoreLayout = true;
        SetOffsets(rail.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0f, 1f), 0f, 0f, 4f, 0f);
        Image image = rail.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void AddTopRail(Transform parent, string name, Color color)
    {
        Transform existing = FindDirectChild(parent, name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        GameObject rail = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        rail.layer = 5;
        rail.transform.SetParent(parent, false);
        rail.transform.SetAsFirstSibling();
        rail.GetComponent<LayoutElement>().ignoreLayout = true;
        SetOffsets(rail.GetComponent<RectTransform>(), new Vector2(0f, 1f), Vector2.one, 0f, -2f, 0f, 0f);
        Image image = rail.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void ConfigurePanelOutline(GameObject target, Color color)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null) outline = target.AddComponent<Outline>();
        ConfigureOutline(outline, color, 1f);
    }

    private static void ConfigureRoundedCorners(GameObject target, float radius)
    {
        InvestigationRoundedCorners rounded = target.GetComponent<InvestigationRoundedCorners>();
        if (rounded == null) rounded = target.AddComponent<InvestigationRoundedCorners>();
        rounded.Configure(radius);
    }

    private static void ConfigureOutline(Outline outline, Color color, float distance)
    {
        outline.enabled = true;
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = false;
    }

    private static void SetPreferredHeight(GameObject target, float height)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();
        if (element == null) element = target.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, float preferredHeight)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
        child.layer = 5;
        child.transform.SetParent(parent, false);
        Text text = child.GetComponent<Text>();
        ConfigureText(text, value, size, style, color, TextAnchor.UpperLeft);
        LayoutElement layout = child.GetComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;
        return text;
    }

    private static Text CreateOverlayText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        child.layer = 5;
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        Stretch(rect, 5f, 5f, 2f, 2f);
        Text text = child.GetComponent<Text>();
        ConfigureText(text, value, size, style, color, alignment);
        return text;
    }

    private static void ConfigureText(Text text, string value, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        InvestigationTypography.Apply(text, InvestigationFontRole.Interface, style);
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.lineSpacing = 1.05f;
    }

    private static void ApplyTypographyToUiPrefabs()
    {
        string[] paths =
        {
            CaseFilesPrefabPath,
            HypothesisPrefabPath,
            SamplePlannerPrefabPath,
            StepperPrefabPath,
            ButtonPrefabPath,
            RuntimePrefabPath,
            CardPrefabPath,
            BoardPrefabPath,
            StatusPrefabPath
        };

        for (int index = 0; index < paths.Length; index++)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(paths[index]);
            try
            {
                Text[] labels = root.GetComponentsInChildren<Text>(true);
                for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
                {
                    Text label = labels[labelIndex];
                    FontStyle emphasis = label.fontStyle;
                    if (emphasis == FontStyle.Normal && IsEmphasizedLabel(label))
                    {
                        emphasis = FontStyle.Bold;
                    }
                    InvestigationTypography.Apply(
                        label,
                        IsDataLabel(label.transform) ? InvestigationFontRole.Data : InvestigationFontRole.Interface,
                        emphasis);
                }
                PrefabUtility.SaveAsPrefabAsset(root, paths[index]);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static bool IsDataLabel(Transform label)
    {
        Transform current = label;
        while (current != null)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("eyebrow")
                || name.Contains("counter")
                || name.Contains("metric")
                || name.Contains("metadata")
                || name.Contains("count")
                || name.Contains("progress")
                || name.Contains("category")
                || name.Contains("status label")
                || name.Contains("findings summary")
                || name.Contains("trait"))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    private static bool IsEmphasizedLabel(Text label)
    {
        if (label.font != null
            && (label.font.name.Contains("SemiBold") || label.font.name.Contains("Medium")))
        {
            return true;
        }

        string name = label.name.ToLowerInvariant();
        return name == "title"
            || name.Contains("heading")
            || name.Contains("eyebrow")
            || name.Contains("counter")
            || name.Contains("metric")
            || name.Contains("category")
            || name.Contains("status label")
            || name.Contains("findings summary")
            || label.GetComponentInParent<InvestigationButtonView>() != null;
    }

    private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void SetOffsets(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float left,
        float bottom,
        float right,
        float top)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = FindChild(root.GetChild(index), name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindDirectChild(Transform root, string name)
    {
        for (int index = 0; index < root.childCount; index++)
        {
            Transform child = root.GetChild(index);
            if (child.name == name) return child;
        }
        return null;
    }
}
