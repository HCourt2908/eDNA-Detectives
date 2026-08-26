using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single-page seamount map screen for Rosette Deployment.
/// Coordinates are authored against the supplied 1600 x 1200 design image.
/// </summary>
public class RosetteMapHub : MonoBehaviour
{
    private static readonly Color DeepSea = new Color(0.008f, 0.028f, 0.04f, 1f);
    private static readonly Color PanelFill = new Color(0.025f, 0.105f, 0.13f, 0.94f);
    private static readonly Color PanelInner = new Color(0.008f, 0.045f, 0.06f, 0.9f);
    private static readonly Color Cyan = new Color(0.48f, 0.88f, 0.95f, 1f);
    private static readonly Color CyanDim = new Color(0.24f, 0.56f, 0.62f, 1f);
    private static readonly Color Ink = new Color(0.68f, 0.93f, 0.96f, 1f);
    private static readonly Color SelectedMagenta = new Color(1f, 0.38f, 0.86f, 1f);

    private const float DesignWidth = 1600f;
    private const float DesignHeight = 1200f;

    private readonly List<Button> locationButtons = new List<Button>();
    private readonly int[] locationIds = { 1, 2, 3, 4, 5, 6 };

    private Canvas canvas;
    private GameObject rootPanel;
    private RectTransform designRoot;
    private GameObject locationInfoContent;
    private GameObject idleInfoContent;
    private Button goButton;
    private TMP_Text locationHeaderText;
    private TMP_Text coordinatesValueText;
    private TMP_Text temperatureValueText;
    private TMP_Text depthValueText;
    private TMP_Text habitatsValueText;
    private int selectedLocationIndex = -1;
    private bool isVisible;

    public event Action<int> ReadyToDeploy;

    public void Initialise(Canvas targetCanvas)
    {
        canvas = targetCanvas != null ? targetCanvas : FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("RosetteMapHub requires a Canvas in the active scene.");
            return;
        }

        BuildInterface();
        Hide();
    }

    public void Show()
    {
        if (rootPanel == null)
        {
            Initialise(canvas);
        }

        if (rootPanel == null)
        {
            return;
        }

        rootPanel.SetActive(true);
        isVisible = true;
        UpdateDesignScale();
        ShowIdleInformation();
    }

    public void Hide()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        isVisible = false;
    }

    public bool IsVisible => isVisible;

    private void LateUpdate()
    {
        if (isVisible)
        {
            UpdateDesignScale();
        }
    }

    private void BuildInterface()
    {
        if (rootPanel != null)
        {
            return;
        }

        rootPanel = CreateImage("RosetteMapHub", canvas.transform, DeepSea, Vector2.zero, Vector2.zero, null, false);
        Stretch(rootPanel.GetComponent<RectTransform>());
        rootPanel.transform.SetAsLastSibling();

        GameObject backgroundLayer = CreateImage("BackgroundLayer", rootPanel.transform, Color.white, Vector2.zero, Vector2.zero,
            LoadSprite("RosetteDeployment/Backgrounds/undersea_background"), false);
        Stretch(backgroundLayer.GetComponent<RectTransform>());
        backgroundLayer.transform.SetAsFirstSibling();

        designRoot = CreateEmptyUI("Design1600x1200", rootPanel.transform).GetComponent<RectTransform>();
        designRoot.anchorMin = new Vector2(0.5f, 0.5f);
        designRoot.anchorMax = new Vector2(0.5f, 0.5f);
        designRoot.pivot = new Vector2(0.5f, 0.5f);
        designRoot.sizeDelta = new Vector2(DesignWidth, DesignHeight);
        designRoot.anchoredPosition = Vector2.zero;

        BuildSeamountMap();
        BuildPromptPanel();
        BuildLocationPanel();
        BuildGoButton();
    }

    private void BuildPromptPanel()
    {
        GameObject outer = CreateAssetPanel("MoreInformationPanel", designRoot, new Vector2(62f, 39f), new Vector2(765f, 350f),
            "RosetteDeployment/Extracted/log_panel_frame");
        CreateText("Prompt", outer.transform, "[ LOG ENTRY ]\nADDITIONAL DATA UNAVAILABLE.\nDEPLOY MORE PROBES FOR ANALYSIS.", 30, Cyan,
            new Vector2(330f, 174f), new Vector2(665f, 260f), TextAlignmentOptions.Center);
    }

    private void BuildLocationPanel()
    {
        GameObject outer = CreateAssetPanel("LocationPanel", designRoot, new Vector2(985f, 31f), new Vector2(635f, 632f),
            "RosetteDeployment/Extracted/database_panel_frame");
        GameObject inner = outer.transform.Find("Inner")?.gameObject;
        if (inner == null)
        {
            return;
        }

        idleInfoContent = CreateEmptyUI("IdleInfo", inner.transform);
        Stretch(idleInfoContent.GetComponent<RectTransform>());
        CreateText("IdlePrompt", idleInfoContent.transform, "[SEAMOUNT_DATABASE]\nSELECT A WAYPOINT\nTO VIEW PROFILE", 25, Cyan,
            new Vector2(0f, -20f), new Vector2(500f, 180f), TextAlignmentOptions.Center);

        locationInfoContent = CreateEmptyUI("LocationInfo", inner.transform);
        Stretch(locationInfoContent.GetComponent<RectTransform>());
        locationHeaderText = CreateText("LocationHeader", locationInfoContent.transform, "[SEAMOUNT_DATABASE]\nSUB-SURFACE PROFILE", 28, Cyan,
            new Vector2(-55f, 226f), new Vector2(520f, 90f), TextAlignmentOptions.Left);
        CreateText("CoordinatesLabel", locationInfoContent.transform, "C:", 28, Cyan,
            new Vector2(-210f, 132f), new Vector2(70f, 55f), TextAlignmentOptions.Left);
        CreateHatchedField(locationInfoContent.transform, "CoordinatesField", new Vector2(90f, 132f), new Vector2(205f, 52f));
        coordinatesValueText = CreateText("CoordinatesValue", locationInfoContent.transform, "WAITING", 18, Cyan,
            new Vector2(90f, 132f), new Vector2(190f, 40f), TextAlignmentOptions.Center);
        CreateText("TemperatureLabel", locationInfoContent.transform, "T:", 28, Cyan,
            new Vector2(-210f, 42f), new Vector2(70f, 55f), TextAlignmentOptions.Left);
        CreateEmptyField(locationInfoContent.transform, "TemperatureField", new Vector2(90f, 42f), new Vector2(215f, 48f));
        temperatureValueText = CreateText("TemperatureValue", locationInfoContent.transform, "WAITING", 18, Cyan,
            new Vector2(90f, 42f), new Vector2(200f, 38f), TextAlignmentOptions.Center);
        CreateText("DepthLabel", locationInfoContent.transform, "D:", 28, Cyan,
            new Vector2(-210f, -47f), new Vector2(70f, 55f), TextAlignmentOptions.Left);
        CreateEmptyField(locationInfoContent.transform, "DepthField", new Vector2(90f, -47f), new Vector2(215f, 48f));
        depthValueText = CreateText("DepthValue", locationInfoContent.transform, "WAITING", 18, Cyan,
            new Vector2(90f, -47f), new Vector2(200f, 38f), TextAlignmentOptions.Center);
        CreateText("HabitatsLabel", locationInfoContent.transform, "POTENTIAL HABITATS OBSERVED:", 20, Cyan,
            new Vector2(-130f, -145f), new Vector2(410f, 60f), TextAlignmentOptions.Left);
        CreateHatchedField(locationInfoContent.transform, "HabitatsField", new Vector2(0f, -228f), new Vector2(435f, 105f));
        habitatsValueText = CreateText("HabitatsValue", locationInfoContent.transform, "DATA PENDING\nDEPLOY MORE PROBES FOR ANALYSIS", 16, Cyan,
            new Vector2(0f, -228f), new Vector2(400f, 80f), TextAlignmentOptions.Center);

        locationInfoContent.SetActive(false);
    }

    private void BuildSeamountMap()
    {
        Sprite seamountSprite = LoadSprite("RosetteDeployment/Map/seamount_terrain");
        // These values match the RectTransform Inspector coordinates used while positioning:
        // Pos X = -222, Pos Y = -370, Width = 1450, Height = 1000.
        GameObject seamount = CreateImage("SeamountMesh", designRoot, Color.white, new Vector2(-222f, -370f), new Vector2(1450f, 1000f), seamountSprite, false);
        Image seamountImage = seamount.GetComponent<Image>();
        seamountImage.preserveAspect = true;
        Shader blackKeyShader = Shader.Find("Rosette/SeamountBlackKey");
        if (blackKeyShader != null)
        {
            seamountImage.material = new Material(blackKeyShader);
        }
        else
        {
            Debug.LogWarning("Rosette/SeamountBlackKey shader was not found; seamount background may cover the undersea background.");
        }

        Vector2[] markerPositions =
        {
            new Vector2(470f, 429f), new Vector2(549f, 558f), new Vector2(339f, 681f),
            new Vector2(752f, 725f), new Vector2(292f, 1059f), new Vector2(814f, 984f)
        };

        for (int index = 0; index < markerPositions.Length; index++)
        {
            Button marker = CreateQuestionButton($"Location{locationIds[index]}", designRoot, markerPositions[index]);
            int capturedIndex = index;
            marker.onClick.AddListener(() => SelectLocation(capturedIndex));
            locationButtons.Add(marker);
        }
    }

    private void BuildMapGrid()
    {
        GameObject grid = CreateEmptyUI("MapGrid", designRoot);
        Color gridColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.14f);
        const float mapLeft = -800f;
        const float mapRight = 650f;
        const float mapTop = 210f;
        const float mapBottom = -600f;

        for (float x = mapLeft; x <= mapRight; x += 72f)
        {
            CreateStroke(grid.transform, new Vector2(x, mapTop), new Vector2(x, mapBottom), 1f, gridColor);
        }

        for (float y = mapTop; y >= mapBottom; y -= 72f)
        {
            CreateStroke(grid.transform, new Vector2(mapLeft, y), new Vector2(mapRight, y), 1f, gridColor);
        }
    }

    private void BuildGoButton()
    {
        Sprite buttonSprite = LoadSprite("RosetteDeployment/Extracted/deploy_button_frame");
        GameObject buttonObject = CreateImage("GoButton", designRoot, Color.white, new Vector2(1350f, 714f), new Vector2(254f, 126f), buttonSprite, true);
        goButton = buttonObject.AddComponent<Button>();
        goButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = goButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.72f, 0.96f, 1f, 1f);
        colors.pressedColor = SelectedMagenta;
        goButton.colors = colors;
        goButton.onClick.AddListener(ConfirmLocation);

        CreateText("GoLabel", buttonObject.transform, "DEPLOY\nDRONE", 22, Cyan, Vector2.zero, new Vector2(220f, 86f), TextAlignmentOptions.Center);
        goButton.interactable = false;
    }

    private Button CreateQuestionButton(string name, Transform parent, Vector2 designPosition)
    {
        GameObject marker = CreateImage(name, parent, new Color(1f, 1f, 1f, 0f), DesignPosition(designPosition, new Vector2(58f, 58f)), new Vector2(58f, 58f), null, true);
        Button button = marker.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(0.15f, 0.65f, 0.73f, 1f);
        colors.pressedColor = SelectedMagenta;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Sprite markerSprite = LoadSprite("RosetteDeployment/Extracted/single_waypoint_marker");
        CreateImage("QuestionIcon", marker.transform, Cyan, new Vector2(0f, -24f), new Vector2(48f, 114f), markerSprite, false);
        return button;
    }

    private void SelectLocation(int index)
    {
        selectedLocationIndex = Mathf.Clamp(index, 0, locationButtons.Count - 1);
        idleInfoContent.SetActive(false);
        locationInfoContent.SetActive(true);
        goButton.interactable = true;
        SetLocationInformation(selectedLocationIndex, $"WAYPOINT-{selectedLocationIndex + 1:00}", "DATA PENDING", "DATA PENDING", "NO OBSERVATION DATA");

        for (int markerIndex = 0; markerIndex < locationButtons.Count; markerIndex++)
        {
            Image icon = locationButtons[markerIndex].transform.Find("QuestionIcon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.color = markerIndex == selectedLocationIndex ? SelectedMagenta : Cyan;
            }
        }
    }

    /// <summary>
    /// Allows the mission/data layer to push the selected waypoint's real values
    /// into this fixed panel once that information becomes available.
    /// </summary>
    public void SetLocationInformation(int locationIndex, string locationId, string conductivity,
        string temperature, string depth, string habitats = null)
    {
        if (locationHeaderText != null)
        {
            locationHeaderText.text = $"[SEAMOUNT_DATABASE]\n{locationId} PROFILE";
        }

        if (coordinatesValueText != null)
        {
            coordinatesValueText.text = conductivity;
        }

        if (temperatureValueText != null)
        {
            temperatureValueText.text = temperature;
        }

        if (depthValueText != null)
        {
            depthValueText.text = depth;
        }

        if (habitatsValueText != null)
        {
            habitatsValueText.text = string.IsNullOrWhiteSpace(habitats)
                ? "DATA PENDING\nDEPLOY MORE PROBES FOR ANALYSIS"
                : habitats;
        }
    }

    private void ShowIdleInformation()
    {
        selectedLocationIndex = -1;
        if (locationHeaderText != null)
        {
            locationHeaderText.text = "[SEAMOUNT_DATABASE]\nSUB-SURFACE PROFILE";
        }
        if (idleInfoContent != null)
        {
            idleInfoContent.SetActive(true);
        }

        if (locationInfoContent != null)
        {
            locationInfoContent.SetActive(false);
        }

        if (goButton != null)
        {
            goButton.interactable = false;
        }

        foreach (Button marker in locationButtons)
        {
            Image icon = marker.transform.Find("QuestionIcon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.color = Cyan;
            }
        }
    }

    private void ConfirmLocation()
    {
        if (selectedLocationIndex >= 0)
        {
            ReadyToDeploy?.Invoke(selectedLocationIndex);
        }
    }

    private void UpdateDesignScale()
    {
        if (canvas == null || designRoot == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float widthScale = canvasRect.rect.width / DesignWidth;
        float heightScale = canvasRect.rect.height / DesignHeight;
        float scale = Mathf.Min(widthScale, heightScale);
        designRoot.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    private static Vector2 DesignPosition(Vector2 topLeft, Vector2 size)
    {
        return new Vector2(topLeft.x + size.x * 0.5f - DesignWidth * 0.5f,
            DesignHeight * 0.5f - (topLeft.y + size.y * 0.5f));
    }

    private static GameObject CreateAssetPanel(string name, Transform parent, Vector2 topLeft, Vector2 size, string frameResourcesPath)
    {
        Sprite frameSprite = LoadSprite(frameResourcesPath);
        GameObject outer = CreateImage(name, parent, Color.white, DesignPosition(topLeft, size), size, frameSprite, false);
        GameObject inner = CreateEmptyUI("Inner", outer.transform);
        Stretch(inner.GetComponent<RectTransform>());
        return outer;
    }

    private static GameObject CreateHatchedField(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject field = CreateEmptyField(parent, name, position, size);
        for (int index = -5; index < 15; index++)
        {
            float x = -size.x * 0.5f + index * 28f;
            CreateStroke(field.transform, new Vector2(x, -size.y * 0.5f), new Vector2(x + size.y * 0.65f, size.y * 0.5f), 3f, Ink);
        }

        return field;
    }

    private static GameObject CreateEmptyField(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject field = CreateImage(name, parent, new Color(1f, 1f, 1f, 0f), position, size, null, false);
        Outline outline = field.AddComponent<Outline>();
        outline.effectColor = Ink;
        outline.effectDistance = new Vector2(2f, -2f);
        return field;
    }

    private static void CreateStroke(Transform parent, Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 delta = end - start;
        GameObject stroke = CreateImage("Hatch", parent, color, (start + end) * 0.5f,
            new Vector2(delta.magnitude, width), null, false);
        stroke.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static GameObject CreateImage(string name, Transform parent, Color color, Vector2 position, Vector2 size,
        Sprite sprite, bool raycastTarget)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        image.sprite = sprite != null ? sprite : Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = sprite != null ? Image.Type.Simple : Image.Type.Sliced;
        image.preserveAspect = false;
        return imageObject;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, int fontSize, Color color,
        Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateEmptyUI(string name, Transform parent)
    {
        GameObject objectRoot = new GameObject(name, typeof(RectTransform));
        objectRoot.transform.SetParent(parent, false);
        return objectRoot;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite LoadSprite(string resourcesPath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcesPath);
        if (texture == null)
        {
            Debug.LogWarning($"RosetteMapHub asset not found at Resources/{resourcesPath}");
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }
}
