using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CTDSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/CTD-Minigame.unity";
    private const string LaboratoryBackgroundPath = "Assets/Art/CTD-Minigame/Backgrounds/laboratory_bg.jpg";
    private const string UnderseaBackgroundPath = "Assets/Art/CTD-Minigame/Backgrounds/undersea_bg.jpg";
    private const string SingleBottlePath = "Assets/Art/CTD-Minigame/CTD/niksin_bottle_1.png";
    private const string TubePath = "Assets/Art/CTD-Minigame/CTD/tube.png";
    private const string FilterPath = "Assets/Art/CTD-Minigame/CTD/filter.png";
    private const string SpongePath = "Assets/Art/CTD-Minigame/CTD/sponge.png";
    private const string MapBackgroundPath = "Assets/Art/Rosette-Deployment/Backgrounds/undersea_background.jpeg";
    private const string SeamountPath = "Assets/Art/Rosette-Deployment/Map/seamount_fine.png";
    private const string LogPanelPath = "Assets/Art/Rosette-Deployment/Extracted/log_panel_frame.png";
    private const string DatabasePanelPath = "Assets/Art/Rosette-Deployment/Extracted/database_panel_frame.png";
    private const string WaypointPath = "Assets/Art/Rosette-Deployment/Extracted/single_waypoint_marker.png";
    private const string DeployFramePath = "Assets/Art/Rosette-Deployment/Extracted/deploy_button_frame.png";
    private const string RosetteEntryPath = "Assets/Art/Rosette-Deployment/Launch/rosette_entry_ocean_animation.png";
    private static readonly Color Navy = new Color(0.018f, 0.055f, 0.12f);
    private static readonly Color PanelBlue = new Color(0.035f, 0.13f, 0.22f, 0.96f);
    private static readonly Color Cyan = new Color(0.18f, 0.82f, 0.86f);
    private static readonly Color Green = new Color(0.25f, 0.92f, 0.62f);
    private static readonly Color White = new Color(0.94f, 0.98f, 1f);
    private static Sprite uiSprite;

    [MenuItem("OceanX/Build CTD Minigame Scene")]
    public static void Build()
    {
        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CTD-Minigame";

        CreateCamera();
        Canvas canvas = CreateCanvas();
        CreateEventSystem();

        GameObject managerObject = new GameObject("CTDGameManager");
        CTDGameManager manager = managerObject.AddComponent<CTDGameManager>();

        GameObject mapPanel = CreateMapPanel(canvas.transform, out RosetteMapHub mapHub);
        GameObject preparationPanel = CreatePreparationPanel(canvas.transform, out EquipmentPreparationSequence preparationSequence);
        GameObject cleaningPanel = CreateCleaningPanel(canvas, out CleaningMinigame cleaningMinigame);
        ApplyBackground(cleaningPanel, LaboratoryBackgroundPath, Color.white);
        GameObject launchPanel = CreateTransitionPanel(
            canvas.transform,
            "LaunchPanel",
            "ROSETTE ENTRY",
            out RectTransform launchRosette,
            out TMP_Text transitionStatusText,
            false);
        ApplyBackground(launchPanel, RosetteEntryPath, Color.white);
        GameObject samplingPanel = CreateSamplingPanel(canvas.transform, out CTDSamplingController samplingController);
        GameObject recoveryPanel = CreateTransitionPanel(
            canvas.transform,
            "RecoveryPanel",
            "RECOVERING CTD",
            out RectTransform recoveryRosette,
            out _,
            true);
        ApplyBackground(recoveryPanel, UnderseaBackgroundPath, Color.white);
        GameObject completePanel = CreateCompletePanel(
            canvas.transform,
            out TMP_Text completionSummary,
            out Button replayButton,
            out Button continueButton);
        ApplyBackground(completePanel, UnderseaBackgroundPath, Color.white);

        manager.mapHub = mapHub;
        manager.preparationPanel = preparationPanel;
        manager.cleaningPanel = cleaningPanel;
        manager.launchPanel = launchPanel;
        manager.samplingPanel = samplingPanel;
        manager.recoveryPanel = recoveryPanel;
        manager.completePanel = completePanel;
        manager.preparationSequence = preparationSequence;
        manager.cleaningMinigame = cleaningMinigame;
        manager.samplingController = samplingController;
        manager.launchRosette = launchRosette;
        manager.recoveryRosette = recoveryRosette;
        manager.transitionStatusText = transitionStatusText;
        manager.completionSummaryText = completionSummary;
        manager.replayButton = replayButton;
        manager.continueButton = continueButton;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddScenesToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"CTD minigame scene created at {ScenePath}");
    }

    [MenuItem("OceanX/Validate CTD Minigame Scene")]
    public static void Validate()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            throw new InvalidOperationException($"CTD scene does not exist at {ScenePath}.");
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CTDGameManager manager = UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            throw new InvalidOperationException("CTDGameManager is missing from the CTD scene.");
        }

        Require(manager.mapHub, "RosetteMapHub");
        Require(manager.preparationPanel, "PreparationPanel");
        Require(manager.cleaningPanel, "CleaningPanel");
        Require(manager.launchPanel, "LaunchPanel");
        Require(manager.samplingPanel, "SamplingPanel");
        Require(manager.recoveryPanel, "RecoveryPanel");
        Require(manager.completePanel, "CompletePanel");
        Require(manager.cleaningMinigame, "CleaningMinigame");
        Require(manager.samplingController, "CTDSamplingController");
        Require(manager.preparationSequence, "EquipmentPreparationSequence");
        Require(manager.replayButton, "Replay button");
        Require(manager.continueButton, "Continue button");

        if (manager.mapHub.waypointButtons == null || manager.mapHub.waypointButtons.Length != 6)
        {
            throw new InvalidOperationException("The scene must contain six map waypoints.");
        }

        if (manager.cleaningMinigame.targets == null || manager.cleaningMinigame.targets.Length != 1)
        {
            throw new InvalidOperationException("The cleaning activity must contain one Niskin-bottle target.");
        }

        if (manager.samplingController.bottleImages == null || manager.samplingController.bottleImages.Length != 3)
        {
            throw new InvalidOperationException("The sampling activity must contain exactly three bottle visuals.");
        }

        int missingScriptCount = scene.GetRootGameObjects()
            .Sum(root => CountMissingScripts(root));

        if (missingScriptCount > 0)
        {
            throw new InvalidOperationException($"The CTD scene contains {missingScriptCount} missing script reference(s).");
        }

        Debug.Log("CTD scene validation passed: all core panels, controls, gameplay references, and scripts are present.");
    }

    [MenuItem("OceanX/Reset CTD Cleaning Tutorial")]
    public static void ResetCleaningTutorial()
    {
        PlayerPrefs.DeleteKey(CleaningMinigame.TutorialCompleteKey);
        PlayerPrefs.Save();
        Debug.Log("CTD cleaning tutorial progress reset. The full cleaning interaction will appear next time Play Mode starts.");
    }

    private static void Require(UnityEngine.Object value, string label)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Missing required CTD scene reference: {label}.");
        }
    }

    private static int CountMissingScripts(GameObject gameObject)
    {
        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

        foreach (Transform child in gameObject.transform)
        {
            count += CountMissingScripts(child.gameObject);
        }

        return count;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Navy;
        camera.orthographic = true;
        cameraObject.AddComponent<AudioListener>();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private static GameObject CreateIntroPanel(Transform parent, out Button beginButton)
    {
        GameObject panel = CreatePanel("IntroPanel", parent, Navy);
        CreateText("Eyebrow", panel.transform, "OCEANX  •  eDNA DETECTIVES", 28, Cyan, new Vector2(0f, 380f), new Vector2(1000f, 60f));
        CreateText("Title", panel.transform, "CTD WATER-SAMPLING MISSION", 64, White, new Vector2(0f, 245f), new Vector2(1500f, 120f), FontStyles.Bold);
        CreateText(
            "Subtitle",
            panel.transform,
            "Prepare clean equipment, lower the CTD rosette, and collect three trustworthy water samples.",
            31,
            new Color(0.76f, 0.88f, 0.94f),
            new Vector2(0f, 135f),
            new Vector2(1350f, 100f));

        GameObject card = CreateImage("MissionCard", panel.transform, PanelBlue, new Vector2(0f, -55f), new Vector2(1180f, 250f));
        CreateText("Line1", card.transform, "1  CLEAN", 30, Green, new Vector2(-390f, 52f), new Vector2(300f, 55f), FontStyles.Bold);
        CreateText("Line2", card.transform, "2  SAMPLE", 30, Green, new Vector2(0f, 52f), new Vector2(300f, 55f), FontStyles.Bold);
        CreateText("Line3", card.transform, "3  RECOVER", 30, Green, new Vector2(390f, 52f), new Vector2(300f, 55f), FontStyles.Bold);
        CreateText("Explain", card.transform, "Every step protects the quality of the eDNA evidence.", 29, White, new Vector2(0f, -55f), new Vector2(950f, 70f));

        beginButton = CreateButton("BeginButton", panel.transform, "BEGIN MISSION", new Vector2(0f, -330f), new Vector2(420f, 100f), Cyan);
        return panel;
    }

    private static GameObject CreateMapPanel(Transform parent, out RosetteMapHub mapHub)
    {
        GameObject panel = CreatePanel("MapPanel", parent, Navy);
        ApplyBackground(panel, MapBackgroundPath, Color.white);
        mapHub = panel.AddComponent<RosetteMapHub>();
        mapHub.rootPanel = panel;

        GameObject seamount = CreateImage("Seamount", panel.transform, Color.white, new Vector2(-240f, -260f), new Vector2(1450f, 1000f));
        Image seamountImage = seamount.GetComponent<Image>();
        seamountImage.sprite = LoadSprite(SeamountPath);
        seamountImage.preserveAspect = true;
        Shader blackKeyShader = Shader.Find("Rosette/SeamountBlackKey");
        if (blackKeyShader != null)
        {
            seamountImage.material = new Material(blackKeyShader);
        }

        GameObject logPanel = CreateImage("LogPanel", panel.transform, Color.white, new Vector2(-490f, 300f), new Vector2(770f, 350f));
        SetSprite(logPanel, LogPanelPath);
        CreateText("LogText", logPanel.transform, "[ LOG ENTRY ]\nADDITIONAL DATA UNAVAILABLE.\nDEPLOY MORE PROBES FOR ANALYSIS.", 29, Cyan, Vector2.zero, new Vector2(650f, 220f), FontStyles.Bold);

        GameObject databasePanel = CreateImage("DatabasePanel", panel.transform, Color.white, new Vector2(575f, 165f), new Vector2(650f, 670f));
        SetSprite(databasePanel, DatabasePanelPath);
        GameObject idle = CreateEmptyUI("IdleInformation", databasePanel.transform);
        Stretch(idle.GetComponent<RectTransform>());
        CreateText("Prompt", idle.transform, "[SEAMOUNT_DATABASE]\nSELECT A WAYPOINT\nTO VIEW PROFILE", 27, Cyan, Vector2.zero, new Vector2(500f, 160f), FontStyles.Bold);

        GameObject location = CreateEmptyUI("LocationInformation", databasePanel.transform);
        Stretch(location.GetComponent<RectTransform>());
        CreateText("Header", location.transform, "[SEAMOUNT_DATABASE]\nSUB-SURFACE PROFILE", 25, Cyan, new Vector2(0f, 216f), new Vector2(510f, 80f), FontStyles.Bold);
        CreateText("ConductivityLabel", location.transform, "CONDUCTIVITY", 18, new Color(0.70f, 0.90f, 0.95f), new Vector2(-155f, 108f), new Vector2(220f, 32f));
        TMP_Text conductivity = CreateText("Conductivity", location.transform, "", 26, White, new Vector2(105f, 108f), new Vector2(270f, 42f), FontStyles.Bold);
        CreateText("TemperatureLabel", location.transform, "TEMPERATURE", 18, new Color(0.70f, 0.90f, 0.95f), new Vector2(-155f, 42f), new Vector2(220f, 32f));
        TMP_Text temperature = CreateText("Temperature", location.transform, "", 26, White, new Vector2(105f, 42f), new Vector2(270f, 42f), FontStyles.Bold);
        CreateText("DepthLabel", location.transform, "DEPTH", 18, new Color(0.70f, 0.90f, 0.95f), new Vector2(-155f, -24f), new Vector2(220f, 32f));
        TMP_Text depth = CreateText("Depth", location.transform, "", 26, White, new Vector2(105f, -24f), new Vector2(270f, 42f), FontStyles.Bold);
        CreateText("HabitatsLabel", location.transform, "POTENTIAL HABITATS OBSERVED", 18, Cyan, new Vector2(0f, -105f), new Vector2(480f, 32f), FontStyles.Bold);
        TMP_Text habitats = CreateText("Habitats", location.transform, "", 21, White, new Vector2(0f, -205f), new Vector2(480f, 150f));
        TMP_Text locationId = CreateText("LocationId", location.transform, "", 25, new Color(0.30f, 0.95f, 0.72f), new Vector2(0f, 165f), new Vector2(480f, 42f), FontStyles.Bold);

        Button[] waypoints = new Button[6];
        Vector2[] positions =
        {
            new Vector2(-360f, 185f), new Vector2(-270f, 50f), new Vector2(-480f, -80f),
            new Vector2(-90f, -130f), new Vector2(-520f, -390f), new Vector2(-30f, -310f)
        };
        for (int index = 0; index < waypoints.Length; index++)
        {
            GameObject marker = CreateImage($"Waypoint-{index + 1:00}", panel.transform, Color.white, positions[index], new Vector2(80f, 120f));
            SetSprite(marker, WaypointPath);
            marker.GetComponent<Image>().preserveAspect = true;
            Button button = marker.AddComponent<Button>();
            button.targetGraphic = marker.GetComponent<Image>();
            waypoints[index] = button;
        }

        GameObject deploy = CreateImage("DeployButton", panel.transform, Color.white, new Vector2(650f, -345f), new Vector2(270f, 130f));
        SetSprite(deploy, DeployFramePath);
        deploy.GetComponent<Image>().preserveAspect = true;
        Button deployButton = deploy.AddComponent<Button>();
        deployButton.targetGraphic = deploy.GetComponent<Image>();
        CreateText("Label", deploy.transform, "DEPLOY\nDRONE", 22, Cyan, Vector2.zero, new Vector2(190f, 70f), FontStyles.Bold);

        mapHub.waypointButtons = waypoints;
        mapHub.idleInformation = idle;
        mapHub.locationInformation = location;
        mapHub.deployButton = deployButton;
        mapHub.locationIdText = locationId;
        mapHub.conductivityText = conductivity;
        mapHub.temperatureText = temperature;
        mapHub.depthText = depth;
        mapHub.habitatsText = habitats;
        mapHub.locations = new[]
        {
            Profile("WAYPOINT-01 [A]", "84.250 mS/cm", "25.60 °C", "221 m", "Deep coral colonies\nEndemic fauna hotspots\nGeothermal vents"),
            Profile("WAYPOINT-02 [C]", "81.900 mS/cm", "17.40 °C", "465 m", "Sponge gardens\nMigrating lanternfish\nCold-water coral"),
            Profile("WAYPOINT-03 [B]", "79.520 mS/cm", "8.25 °C", "690 m", "Hydrothermal vent plume\nCrustacean aggregation\nMicrobial mats"),
            Profile("WAYPOINT-04 [D]", "78.100 mS/cm", "6.10 °C", "835 m", "Slope fauna corridor\nDeep coral colonies"),
            Profile("WAYPOINT-05 [E]", "76.800 mS/cm", "4.80 °C", "940 m", "Abyssal sponge field\nDetrital feeding grounds"),
            Profile("WAYPOINT-06 [F]", "80.300 mS/cm", "11.75 °C", "520 m", "Midwater fauna hotspot\nLarval fish nursery")
        };
        return panel;
    }

    private static RosetteMapHub.LocationProfile Profile(string id, string conductivity, string temperature, string depth, string habitats)
    {
        return new RosetteMapHub.LocationProfile
        {
            locationId = id,
            conductivity = conductivity,
            temperature = temperature,
            depth = depth,
            habitats = habitats
        };
    }

    private static GameObject CreatePreparationPanel(Transform parent, out EquipmentPreparationSequence sequence)
    {
        GameObject panel = CreatePanel("PreparationPanel", parent, Navy);
        ApplyBackground(panel, MapBackgroundPath, Color.white);
        sequence = panel.AddComponent<EquipmentPreparationSequence>();
        CreateText("Title", panel.transform, "PREPARING YOUR EQUIPMENT", 58, White, new Vector2(0f, 245f), new Vector2(1500f, 90f), FontStyles.Bold);
        TMP_Text status = CreateText("Status", panel.transform, "PREPARING YOUR EQUIPMENT", 30, Cyan, new Vector2(0f, -270f), new Vector2(1000f, 50f), FontStyles.Bold);
        TMP_Text tip = CreateText("Tip", panel.transform, "", 27, White, new Vector2(0f, 90f), new Vector2(1250f, 130f));
        GameObject progressTrack = CreateImage("ProgressTrack", panel.transform, new Color(0.015f, 0.08f, 0.12f, 0.9f), new Vector2(0f, -350f), new Vector2(1180f, 34f));
        GameObject progressObject = CreateImage("ProgressFill", progressTrack.transform, Cyan, Vector2.zero, new Vector2(1120f, 18f));
        Image progress = progressObject.GetComponent<Image>();
        progress.type = Image.Type.Filled;
        progress.fillMethod = Image.FillMethod.Horizontal;
        progress.fillAmount = 0f;
        Button ready = CreateButton("ReadyButton", panel.transform, "READY", new Vector2(0f, -430f), new Vector2(310f, 80f), Green);
        sequence.statusText = status;
        sequence.tipText = tip;
        sequence.progressFill = progress;
        sequence.readyButton = ready;
        return panel;
    }

    private static GameObject CreateCleaningPanel(Canvas canvas, out CleaningMinigame minigame)
    {
        GameObject panel = CreatePanel("CleaningPanel", canvas.transform, Navy);
        minigame = panel.AddComponent<CleaningMinigame>();

        CreateText("Title", panel.transform, "STERILISE THE NISKIN BOTTLE", 48, White, new Vector2(0f, 450f), new Vector2(1250f, 80f), FontStyles.Bold);
        TMP_Text explanation = CreateText(
            "Explanation",
            panel.transform,
            "Remove contamination, then rinse with sterile water before the first sample.",
            27,
            new Color(0.72f, 0.88f, 0.96f),
            new Vector2(0f, 375f),
            new Vector2(1450f, 70f));
        TMP_Text instruction = CreateText(
            "Instruction",
            panel.transform,
            "Drag the sponge across the bottle, then rinse it with sterile water.",
            28,
            White,
            new Vector2(0f, 315f),
            new Vector2(1500f, 60f),
            FontStyles.Bold);

        GameObject manualGroup = CreateEmptyUI("ManualCleaningGroup", panel.transform);
        Stretch(manualGroup.GetComponent<RectTransform>());
        CleaningTarget[] targets = new CleaningTarget[1];
        targets[0] = CreateCleaningTarget(manualGroup.transform, "Niskin bottle", "NISKIN BOTTLE", 0f, SingleBottlePath, new Vector2(280f, 330f));

        CleaningTool cleaningTool = CreateCleaningTool(
            manualGroup.transform,
            canvas,
            "CleaningSolution",
            "DECONTAMINATION SOLUTION\nCLICK OR DRAG",
            CleaningToolType.DecontaminationSolution,
            new Vector2(-210f, -405f),
            Color.white,
            SpongePath);
        CleaningTool rinseTool = CreateCleaningTool(
            manualGroup.transform,
            canvas,
            "SterileWater",
            "STERILE WATER RINSE\nCLICK OR DRAG",
            CleaningToolType.SterileWater,
            new Vector2(210f, -405f),
            new Color(0.25f, 0.62f, 0.94f));
        cleaningTool.minigame = minigame;
        rinseTool.minigame = minigame;

        GameObject quickGroup = CreateEmptyUI("QuickCleaningGroup", panel.transform);
        Stretch(quickGroup.GetComponent<RectTransform>());
        CreateText(
            "QuickText",
            quickGroup.transform,
            "FIRST ATTEMPT TUTORIAL",
            20,
            White,
            new Vector2(700f, -405f),
            new Vector2(320f, 45f),
            FontStyles.Bold);
        Button quickButton = CreateButton("QuickCleanButton", quickGroup.transform, "AUTO-CLEAN", new Vector2(700f, -465f), new Vector2(300f, 72f), Cyan);

        Button continueButton = CreateButton("CleaningContinueButton", panel.transform, "CONTINUE TO SAMPLING PLAN", new Vector2(0f, -465f), new Vector2(620f, 82f), Green);

        minigame.targets = targets;
        minigame.manualCleaningGroup = manualGroup;
        minigame.quickCleaningGroup = quickGroup;
        minigame.instructionText = instruction;
        minigame.explanationText = explanation;
        minigame.quickCleanButton = quickButton;
        minigame.continueButton = continueButton;
        return panel;
    }

    private static CleaningTarget CreateCleaningTarget(Transform parent, string displayName, string label, float x, string spritePath, Vector2 imageSize)
    {
        GameObject card = CreateImage($"{label} Card", parent, PanelBlue, new Vector2(x, 5f), new Vector2(420f, 500f));
        CreateText("Label", card.transform, label, 27, White, new Vector2(0f, 205f), new Vector2(350f, 50f), FontStyles.Bold);

        GameObject equipment = CreateImage("Equipment", card.transform, Color.white, new Vector2(0f, 68f), imageSize);
        Image equipmentImage = equipment.GetComponent<Image>();
        equipmentImage.sprite = LoadSprite(spritePath);
        equipmentImage.type = Image.Type.Simple;
        equipmentImage.preserveAspect = true;

        CreateText("CleanLabel", card.transform, "CLEAN", 17, new Color(0.72f, 0.88f, 0.94f), new Vector2(-145f, -92f), new Vector2(90f, 30f));
        Image cleanFill = CreateProgressBar(card.transform, new Vector2(38f, -92f), new Color(0.35f, 0.90f, 0.62f));
        CreateText("RinseLabel", card.transform, "RINSE", 17, new Color(0.72f, 0.88f, 0.94f), new Vector2(-145f, -138f), new Vector2(90f, 30f));
        Image rinseFill = CreateProgressBar(card.transform, new Vector2(38f, -138f), new Color(0.30f, 0.70f, 1f));
        TMP_Text status = CreateText("Status", card.transform, "Use the cleaning sponge", 21, White, new Vector2(0f, -190f), new Vector2(360f, 65f));

        CleaningTarget target = card.AddComponent<CleaningTarget>();
        target.displayName = displayName;
        target.targetRect = card.GetComponent<RectTransform>();
        target.equipmentImage = equipmentImage;
        target.dirtyOverlay = null;
        target.cleanProgressFill = cleanFill;
        target.rinseProgressFill = rinseFill;
        target.statusText = status;
        return target;
    }

    private static Image CreateProgressBar(Transform parent, Vector2 position, Color colour)
    {
        GameObject background = CreateImage("ProgressBackground", parent, new Color(0.02f, 0.06f, 0.10f), position, new Vector2(270f, 22f));
        GameObject fillObject = CreateImage("Fill", background.transform, colour, Vector2.zero, new Vector2(250f, 12f));
        Image fill = fillObject.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        return fill;
    }

    private static CleaningTool CreateCleaningTool(
        Transform parent,
        Canvas canvas,
        string name,
        string label,
        CleaningToolType toolType,
        Vector2 position,
        Color colour,
        string spritePath = null)
    {
        bool usesSprite = !string.IsNullOrEmpty(spritePath);
        Vector2 toolSize = usesSprite ? new Vector2(180f, 130f) : new Vector2(330f, 92f);
        GameObject toolObject = CreateImage(name, parent, colour, position, toolSize);
        Image toolImage = toolObject.GetComponent<Image>();

        if (usesSprite)
        {
            toolImage.sprite = LoadSprite(spritePath);
            toolImage.type = Image.Type.Simple;
            toolImage.preserveAspect = true;
        }

        CreateText(
            "Label",
            toolObject.transform,
            usesSprite ? "CLEANING SPONGE\nCLICK OR DRAG" : label,
            usesSprite ? 18 : 22,
            usesSprite ? White : Navy,
            usesSprite ? new Vector2(0f, -88f) : Vector2.zero,
            usesSprite ? new Vector2(310f, 55f) : new Vector2(290f, 75f),
            FontStyles.Bold);
        CleaningTool tool = toolObject.AddComponent<CleaningTool>();
        tool.toolType = toolType;
        tool.rectTransform = toolObject.GetComponent<RectTransform>();
        tool.rootCanvas = canvas;
        return tool;
    }

    private static GameObject CreatePlanningPanel(
        Transform parent,
        out Button[] locationButtons,
        out TMP_Text selectedLocationText,
        out Button deployButton)
    {
        GameObject panel = CreatePanel("PlanningPanel", parent, Navy);
        CreateText("Title", panel.transform, "CHOOSE A SAMPLING STATION", 50, White, new Vector2(0f, 405f), new Vector2(1400f, 80f), FontStyles.Bold);
        CreateText(
            "Instructions",
            panel.transform,
            "One CTD cast will collect deep, midwater, and surface samples as the rosette rises.",
            29,
            new Color(0.74f, 0.88f, 0.95f),
            new Vector2(0f, 330f),
            new Vector2(1450f, 70f));

        GameObject map = CreateImage("SeamountMap", panel.transform, new Color(0.025f, 0.12f, 0.20f), new Vector2(0f, 65f), new Vector2(1160f, 430f));
        CreateText("Seamount", map.transform, "SEAMOUNT SURVEY AREA", 28, Cyan, new Vector2(0f, 145f), new Vector2(650f, 55f), FontStyles.Bold);
        CreateImage("Mountain", map.transform, new Color(0.12f, 0.35f, 0.39f), new Vector2(0f, -30f), new Vector2(720f, 190f));

        locationButtons = new Button[3];
        locationButtons[0] = CreateButton("StationA", map.transform, "STATION A", new Vector2(-340f, 30f), new Vector2(245f, 85f), new Color(0.12f, 0.28f, 0.40f));
        locationButtons[1] = CreateButton("StationB", map.transform, "STATION B", new Vector2(0f, -65f), new Vector2(245f, 85f), new Color(0.12f, 0.28f, 0.40f));
        locationButtons[2] = CreateButton("StationC", map.transform, "STATION C", new Vector2(340f, 30f), new Vector2(245f, 85f), new Color(0.12f, 0.28f, 0.40f));

        selectedLocationText = CreateText(
            "Selection",
            panel.transform,
            "Selected: Station A\nTargets: Deep 820 m  •  Midwater 500 m  •  Surface 180 m",
            28,
            White,
            new Vector2(0f, -225f),
            new Vector2(1350f, 100f),
            FontStyles.Bold);
        deployButton = CreateButton("DeployButton", panel.transform, "DEPLOY CTD", new Vector2(0f, -380f), new Vector2(440f, 100f), Cyan);
        return panel;
    }

    private static GameObject CreateTransitionPanel(
        Transform parent,
        string panelName,
        string title,
        out RectTransform rosette,
        out TMP_Text status,
        bool recovery)
    {
        GameObject panel = CreatePanel(panelName, parent, Navy);
        CreateImage("Sea", panel.transform, new Color(1f, 1f, 1f, 0f), new Vector2(0f, -150f), new Vector2(1920f, 780f));
        CreateImage("Deck", panel.transform, new Color(1f, 1f, 1f, 0f), new Vector2(0f, 430f), new Vector2(1920f, 220f));
        CreateText("Title", panel.transform, title, 50, White, new Vector2(0f, 445f), new Vector2(1200f, 70f), FontStyles.Bold);

        GameObject cable = CreateImage("Cable", panel.transform, new Color(0.75f, 0.82f, 0.85f), new Vector2(0f, 80f), new Vector2(10f, 720f));
        cable.GetComponent<Image>().raycastTarget = false;
        rosette = CreateRosetteGraphic(panel.transform, recovery ? "RecoveredRosette" : "LaunchRosette", new Vector2(0f, recovery ? -220f : 210f), null);
        status = CreateText(
            "Status",
            panel.transform,
            recovery ? "Three samples secured — returning the CTD to deck" : "Deck crew secured — lowering the CTD rosette",
            30,
            White,
            new Vector2(0f, -445f),
            new Vector2(1250f, 70f));
        return panel;
    }

    private static GameObject CreateSamplingPanel(Transform parent, out CTDSamplingController controller)
    {
        GameObject panel = CreatePanel("SamplingPanel", parent, Navy);
        controller = panel.AddComponent<CTDSamplingController>();

        GameObject ocean = CreateImage("OceanBackground", panel.transform, Color.white, Vector2.zero, new Vector2(1920f, 1080f));
        Image oceanImage = ocean.GetComponent<Image>();
        oceanImage.sprite = LoadSprite(UnderseaBackgroundPath);
        oceanImage.type = Image.Type.Simple;
        CreateImage("TopShade", panel.transform, new Color(1f, 1f, 1f, 0f), new Vector2(0f, 440f), new Vector2(1920f, 200f));
        CreateImage("BottomShade", panel.transform, new Color(1f, 1f, 1f, 0f), new Vector2(0f, -455f), new Vector2(1920f, 170f));

        TMP_Text phaseText = CreateText("Phase", panel.transform, "DOWNCAST", 27, Cyan, new Vector2(0f, 488f), new Vector2(1200f, 45f), FontStyles.Bold);
        TMP_Text targetText = CreateText("Target", panel.transform, "Sampling begins during the upcast", 34, White, new Vector2(0f, 435f), new Vector2(1450f, 60f), FontStyles.Bold);

        GameObject gaugeFrame = CreateImage("DepthGaugeFrame", panel.transform, new Color(0.015f, 0.06f, 0.10f, 0.90f), new Vector2(-760f, -10f), new Vector2(250f, 760f));
        CreateText("GaugeTitle", gaugeFrame.transform, "DEPTH", 23, White, new Vector2(0f, 335f), new Vector2(200f, 45f), FontStyles.Bold);
        GameObject gauge = CreateImage("DepthGauge", gaugeFrame.transform, new Color(0.04f, 0.18f, 0.28f), new Vector2(25f, 0f), new Vector2(72f, 650f));
        RectTransform gaugeRect = gauge.GetComponent<RectTransform>();
        gaugeRect.pivot = new Vector2(0.5f, 1f);
        gaugeRect.anchoredPosition = new Vector2(25f, 310f);

        CreateGaugeBand(gauge.transform, "SurfaceBand", new Color(0.10f, 0.62f, 0.85f, 0.50f), 0f);
        CreateGaugeBand(gauge.transform, "MidwaterBand", new Color(0.05f, 0.34f, 0.57f, 0.55f), -216.5f);
        CreateGaugeBand(gauge.transform, "DeepBand", new Color(0.015f, 0.09f, 0.23f, 0.75f), -433f);

        GameObject targetBandObject = CreateImage("TargetBand", gauge.transform, new Color(0.22f, 0.80f, 0.95f, 0.35f), Vector2.zero, new Vector2(96f, 90f));
        RectTransform targetBand = targetBandObject.GetComponent<RectTransform>();
        targetBand.anchorMin = new Vector2(0.5f, 1f);
        targetBand.anchorMax = new Vector2(0.5f, 1f);
        targetBand.pivot = new Vector2(0.5f, 0.5f);

        GameObject markerObject = CreateImage("DepthMarker", gauge.transform, White, Vector2.zero, new Vector2(118f, 10f));
        RectTransform marker = markerObject.GetComponent<RectTransform>();
        marker.anchorMin = new Vector2(0.5f, 1f);
        marker.anchorMax = new Vector2(0.5f, 1f);
        marker.pivot = new Vector2(0.5f, 0.5f);

        CreateText("SurfaceLabel", gaugeFrame.transform, "SURFACE", 18, White, new Vector2(-62f, 225f), new Vector2(110f, 35f));
        CreateText("MidLabel", gaugeFrame.transform, "MID", 18, White, new Vector2(-62f, 8f), new Vector2(110f, 35f));
        CreateText("DeepLabel", gaugeFrame.transform, "DEEP", 18, White, new Vector2(-62f, -208f), new Vector2(110f, 35f));
        TMP_Text depthText = CreateText("CurrentDepth", gaugeFrame.transform, "0 m", 28, Cyan, new Vector2(0f, -345f), new Vector2(190f, 50f), FontStyles.Bold);

        Image[] bottleImages = new Image[3];
        RectTransform rosette = CreateRosetteGraphic(panel.transform, "CTDRosette", new Vector2(0f, 20f), bottleImages);
        rosette.sizeDelta = new Vector2(430f, 430f);
        CreateText("RosetteLabel", panel.transform, "CTD ROSETTE", 24, White, new Vector2(0f, -245f), new Vector2(360f, 45f), FontStyles.Bold);

        GameObject dataPanel = CreateImage("DataPanel", panel.transform, new Color(0.015f, 0.06f, 0.10f, 0.90f), new Vector2(690f, 40f), new Vector2(430f, 650f));
        CreateText("DataTitle", dataPanel.transform, "LIVE DATA", 24, Cyan, new Vector2(0f, 270f), new Vector2(320f, 45f), FontStyles.Bold);
        TMP_Text sensorText = CreateText("Sensors", dataPanel.transform, "Temperature  22.0 °C\nSalinity         34.1 PSU", 24, White, new Vector2(0f, 185f), new Vector2(350f, 105f));

        TMP_Text[] bottleStatuses = new TMP_Text[3];
        for (int index = 0; index < bottleStatuses.Length; index++)
        {
            bottleStatuses[index] = CreateText(
                $"Bottle{index + 1}Status",
                dataPanel.transform,
                $"Bottle {index + 1:00}: OPEN",
                23,
                White,
                new Vector2(0f, 70f - index * 62f),
                new Vector2(340f, 46f),
                FontStyles.Bold);
        }

        TMP_Text feedback = CreateText(
            "Feedback",
            dataPanel.transform,
            "The CTD is recording temperature, salinity, and depth.",
            21,
            new Color(0.78f, 0.90f, 0.96f),
            new Vector2(0f, -195f),
            new Vector2(350f, 120f));

        GameObject hintCard = CreateImage(
            "TutorialHintCard",
            panel.transform,
            new Color(0.93f, 0.96f, 0.56f, 0.96f),
            new Vector2(0f, -310f),
            new Vector2(950f, 82f));
        TMP_Text hint = CreateText(
            "TutorialHint",
            hintCard.transform,
            "The marker is inside the target zone. Press CLOSE BOTTLE now!",
            27,
            Navy,
            Vector2.zero,
            new Vector2(900f, 70f),
            FontStyles.Bold);

        Button closeButton = CreateButton("CloseBottleButton", panel.transform, "CLOSE BOTTLE", new Vector2(0f, -445f), new Vector2(470f, 95f), Green);

        controller.oceanBackground = oceanImage;
        controller.depthGauge = gaugeRect;
        controller.depthMarker = marker;
        controller.targetBand = targetBand;
        controller.phaseText = phaseText;
        controller.depthText = depthText;
        controller.sensorText = sensorText;
        controller.targetText = targetText;
        controller.feedbackText = feedback;
        controller.tutorialHintText = hint;
        controller.closeBottleButton = closeButton;
        controller.bottleImages = bottleImages;
        controller.bottleStatusTexts = bottleStatuses;
        return panel;
    }

    private static void CreateGaugeBand(Transform parent, string name, Color colour, float y)
    {
        GameObject band = CreateImage(name, parent, colour, new Vector2(0f, y), new Vector2(62f, 216f));
        RectTransform rect = band.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
    }

    private static RectTransform CreateRosetteGraphic(Transform parent, string name, Vector2 position, Image[] bottleOutput)
    {
        GameObject rosetteObject = CreateImage(name, parent, new Color(0.18f, 0.25f, 0.29f, 0.98f), position, new Vector2(300f, 300f));
        RectTransform rosette = rosetteObject.GetComponent<RectTransform>();
        CreateImage("SensorColumn", rosetteObject.transform, new Color(0.22f, 0.75f, 0.78f), new Vector2(0f, -5f), new Vector2(70f, 220f));
        CreateImage("TopFrame", rosetteObject.transform, new Color(0.75f, 0.82f, 0.85f), new Vector2(0f, 110f), new Vector2(240f, 18f));
        CreateImage("BottomFrame", rosetteObject.transform, new Color(0.75f, 0.82f, 0.85f), new Vector2(0f, -110f), new Vector2(240f, 18f));

        float[] bottleX = { -92f, 0f, 92f };
        for (int index = 0; index < bottleX.Length; index++)
        {
            GameObject bottle = CreateImage(
                $"Bottle{index + 1:00}",
                rosetteObject.transform,
                new Color(0.74f, 0.86f, 0.91f),
                new Vector2(bottleX[index], 0f),
                new Vector2(58f, 175f));
            CreateText("Number", bottle.transform, $"{index + 1:00}", 19, Navy, Vector2.zero, new Vector2(50f, 45f), FontStyles.Bold);

            if (bottleOutput != null && index < bottleOutput.Length)
            {
                bottleOutput[index] = bottle.GetComponent<Image>();
            }
        }

        return rosette;
    }

    private static GameObject CreateCompletePanel(
        Transform parent,
        out TMP_Text summary,
        out Button replayButton,
        out Button continueButton)
    {
        GameObject panel = CreatePanel("CompletePanel", parent, Navy);
        CreateText("Title", panel.transform, "SAMPLES SECURED", 56, Green, new Vector2(0f, 410f), new Vector2(1300f, 90f), FontStyles.Bold);
        CreateText(
            "Subtitle",
            panel.transform,
            "The CTD cast is complete and the water samples are ready for the next laboratory stage.",
            28,
            White,
            new Vector2(0f, 335f),
            new Vector2(1450f, 70f));

        GameObject card = CreateImage("SummaryCard", panel.transform, PanelBlue, new Vector2(0f, 30f), new Vector2(1150f, 500f));
        summary = CreateText("Summary", card.transform, "CTD CAST COMPLETE", 26, White, Vector2.zero, new Vector2(1020f, 430f));
        summary.alignment = TextAlignmentOptions.TopLeft;

        replayButton = CreateButton("ReplayButton", panel.transform, "REPLAY CTD", new Vector2(-280f, -360f), new Vector2(420f, 95f), new Color(0.18f, 0.42f, 0.54f));
        continueButton = CreateButton("ContinueButton", panel.transform, "CONTINUE TO DNA", new Vector2(280f, -360f), new Vector2(470f, 95f), Cyan);
        return panel;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color backgroundColour)
    {
        GameObject panel = CreateImage(name, parent, backgroundColour, Vector2.zero, new Vector2(1920f, 1080f));
        Stretch(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static void ApplyBackground(GameObject panel, string assetPath, Color tint)
    {
        Sprite sprite = LoadSprite(assetPath);

        if (sprite == null)
        {
            Debug.LogWarning($"CTD background not found at {assetPath}. The placeholder colour will be used.");
            return;
        }

        Image image = panel.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = tint;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
    }

    private static void SetSprite(GameObject gameObject, string assetPath)
    {
        Image image = gameObject.GetComponent<Image>();
        image.sprite = LoadSprite(assetPath);
        image.type = Image.Type.Simple;
    }

    private static GameObject CreateEmptyUI(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreateImage(string name, Transform parent, Color colour, Vector2 position, Vector2 size)
    {
        GameObject gameObject = CreateEmptyUI(name, parent);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = gameObject.AddComponent<Image>();
        image.color = colour;
        image.sprite = uiSprite;
        image.type = Image.Type.Sliced;
        return gameObject;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color colour,
        Vector2 position,
        Vector2 size,
        FontStyles style = FontStyles.Normal)
    {
        GameObject gameObject = CreateEmptyUI(name, parent);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = colour;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color colour)
    {
        GameObject buttonObject = CreateImage(name, parent, colour, position, size);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        ColorBlock colours = button.colors;
        colours.normalColor = colour;
        colours.highlightedColor = Color.Lerp(colour, Color.white, 0.18f);
        colours.pressedColor = Color.Lerp(colour, Color.black, 0.20f);
        colours.selectedColor = colours.highlightedColor;
        colours.disabledColor = new Color(colour.r * 0.55f, colour.g * 0.55f, colour.b * 0.55f, 0.65f);
        button.colors = colours;

        CreateText("Label", buttonObject.transform, label, 25, Navy, Vector2.zero, size - new Vector2(30f, 20f), FontStyles.Bold);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        AddSceneIfMissing(scenes, ScenePath);
        AddSceneIfMissing(scenes, "Assets/Scenes/Petri-Dish-Game.unity");
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string path)
    {
        if (scenes.All(scene => scene.path != path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
