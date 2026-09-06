using System;
using System.Linq;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CleaningStageInstaller
{
    private const string ScenePath = "Assets/Scenes/CTD-Minigame.unity";
    private const string ArtPath = "Assets/Art/Rosette-Deployment/Cleaning/";
    private const string SpongeHandPath = "Assets/Art/Rosette-Deployment/Cockpit/gloved_cleaning_hand.png";

    [MenuItem("OceanX/Apply Local-Foam Cleaning Stage")]
    public static void Apply()
    {
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CTDGameManager manager = UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);
        CleaningMinigame game = manager.cleaningMinigame;
        Transform panel = manager.cleaningPanel.transform;

        Sprite hand = ImportSprite(ArtPath + "cleaning_hand_hose.png");
        Sprite bottle = ImportSprite(ArtPath + "niskin_bottle_01.png");
        Sprite spongeHand = ImportSprite(SpongeHandPath);
        Material blackKey = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Rosette-Deployment/Cockpit/NiskinKey.mat");
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        CleaningTarget target = game.targets[0];
        RectTransform targetRect = target.targetRect;
        targetRect.anchoredPosition = new Vector2(-10f, -10f);
        targetRect.sizeDelta = new Vector2(700f, 1250f);
        Image oldCard = targetRect.GetComponent<Image>();
        if (oldCard != null) oldCard.enabled = false;
        foreach (Transform child in targetRect)
            if (child.name != "Equipment") child.gameObject.SetActive(false);

        Image bottleImage = target.equipmentImage;
        bottleImage.sprite = bottle;
        bottleImage.material = blackKey;
        bottleImage.preserveAspect = true;
        bottleImage.color = Color.white;
        Set(bottleImage.rectTransform, new Vector2(0f, 10f), new Vector2(620f, 1240f));
        bottleImage.raycastTarget = false;

        Transform oldFoam = bottleImage.transform.Find("FoamLayer");
        if (oldFoam != null) UnityEngine.Object.DestroyImmediate(oldFoam.gameObject);
        RectTransform foamRoot = CreateRect("FoamLayer", bottleImage.transform, Vector2.zero, new Vector2(510f, 1040f));
        foamRoot.SetAsLastSibling();
        target.foamRoot = foamRoot;
        target.foamSpots = BuildFoam(foamRoot, circle);

        TMP_Text status = panel.Find("BottleStatus") != null
            ? panel.Find("BottleStatus").GetComponent<TMP_Text>()
            : CreateText("BottleStatus", panel, "KEEP SCRUBBING THE BOTTLE", new Vector2(-5f, -378f), new Vector2(620f, 52f), 30, Color.white);
        Set(status.rectTransform, new Vector2(-5f, -378f), new Vector2(620f, 52f));
        status.text = "KEEP SCRUBBING THE BOTTLE";
        status.fontStyle = FontStyles.Bold;
        game.bottleStatusText = status;

        CleaningTool handTool = game.manualCleaningGroup.GetComponentsInChildren<CleaningTool>(true).First(x => x.toolType == CleaningToolType.DecontaminationSolution);
        Image handImage = handTool.GetComponent<Image>();
        handImage.sprite = spongeHand;
        // The legacy sponge-hand PNG has a black matte. The existing key shader
        // removes that matte while preserving the hand and sponge edges.
        handImage.material = blackKey;
        handImage.preserveAspect = true;
        handImage.color = Color.white;
        Set(handTool.rectTransform, new Vector2(500f, -25f), new Vector2(560f, 560f));
        handTool.rectTransform.localEulerAngles = Vector3.zero;
        foreach (TMP_Text label in handTool.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
        handTool.contactPoint = EnsureContactPoint(handTool.transform, "SpongeContactPoint", new Vector2(-120f, 120f));
        handTool.waterFlow = null;

        CleaningTool oldRinse = game.manualCleaningGroup.GetComponentsInChildren<CleaningTool>(true).First(x => x.toolType == CleaningToolType.SterileWater);
        Image hoseImage = oldRinse.GetComponent<Image>();
        hoseImage.sprite = hand;
        hoseImage.material = null;
        hoseImage.preserveAspect = true;
        hoseImage.color = Color.white;
        Set(oldRinse.rectTransform, new Vector2(450f, -10f), new Vector2(380f, 252f));
        oldRinse.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        foreach (TMP_Text label in oldRinse.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
        oldRinse.contactPoint = EnsureContactPoint(oldRinse.transform, "HoseNozzleContact", new Vector2(180f, 0f));
        foreach (Transform child in oldRinse.transform)
            if (child.name == "WaterFlow") UnityEngine.Object.DestroyImmediate(child.gameObject);
        oldRinse.waterFlow = CreateWaterFlow(oldRinse.transform, circle).gameObject;
        oldRinse.gameObject.SetActive(false);
        game.spongeTool = handTool;
        game.hoseTool = oldRinse;
        if (game.quickCleaningGroup != null) game.quickCleaningGroup.SetActive(false);

        Button rinse = EnsureButton("RinseButton", panel, "RINSE", new Vector2(690f, -365f), new Vector2(270f, 78f), new Color(.2f, .65f, .95f));
        Button skip = EnsureButton("SkipButton", panel, "SKIP", new Vector2(745f, -455f), new Vector2(160f, 54f), new Color(.18f, .22f, .27f));
        game.rinseButton = rinse;
        game.skipButton = skip;
        game.continueButton.transform.SetAsLastSibling();
        Set(game.continueButton.GetComponent<RectTransform>(), new Vector2(0f, -445f), new Vector2(450f, 75f));

        if (panel.Find("YellowInstructionNote") != null)
        {
            TMP_Text[] noteText = panel.Find("YellowInstructionNote").GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text item in noteText)
            {
                if (item.name == "NoteTitle") item.text = "CLEANING NOTES";
            }
        }

        EditorUtility.SetDirty(game);
        EditorUtility.SetDirty(target);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LOCAL_FOAM_CLEANING_APPLIED");
    }

    private static CleaningFoamSpot[] BuildFoam(RectTransform root, Sprite circle)
    {
        float[] xs = { -152f, -54f, 52f, 150f };
        float[] ys = { 370f, 230f, 90f, -50f, -190f, -330f };
        CleaningFoamSpot[] spots = new CleaningFoamSpot[xs.Length * ys.Length];
        int index = 0;
        foreach (float y in ys)
        foreach (float x in xs)
        {
            float jitterX = Mathf.Sin(index * 2.71f) * 12f;
            float jitterY = Mathf.Cos(index * 1.93f) * 10f;
            RectTransform item = CreateRect("FoamSpot_" + index.ToString("00"), root, new Vector2(x + (index % 2 == 0 ? 18f : -14f) + jitterX, y + jitterY), new Vector2(140f, 140f));
            CleaningFoamSpot spot = item.gameObject.AddComponent<CleaningFoamSpot>();
            spot.bubbles = new[]
            {
                Bubble(item, circle, new Vector2(-28f, 16f), 62f),
                Bubble(item, circle, new Vector2(24f, 20f), 76f),
                Bubble(item, circle, new Vector2(6f, -28f), 54f),
                Bubble(item, circle, new Vector2(-34f, -26f), 38f),
                Bubble(item, circle, new Vector2(38f, -10f), 44f)
            };
            item.gameObject.SetActive(false);
            spots[index++] = spot;
        }
        return spots;
    }

    private static Image Bubble(Transform parent, Sprite sprite, Vector2 position, float size)
    {
        RectTransform rect = CreateRect("Bubble", parent, position, new Vector2(size, size));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = new Color(1f, 1f, 1f, .8f);
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateWaterFlow(Transform parent, Sprite circle)
    {
        RectTransform root = CreateRect("WaterFlow", parent, new Vector2(336f, 2f), new Vector2(170f, 80f));
        root.localEulerAngles = new Vector3(0f, 0f, -10f);
        Image stream = root.gameObject.AddComponent<Image>();
        stream.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        stream.type = Image.Type.Sliced;
        stream.color = new Color(.4f, .9f, 1f, .68f);
        stream.raycastTarget = false;
        for (int i = 0; i < 5; i++)
            Bubble(root, circle, new Vector2(50f - i * 24f, (i % 2 == 0 ? 25f : -22f)), 10f + i * 2f).color = new Color(.7f, .96f, 1f, .75f);
        root.gameObject.SetActive(false);
        return root;
    }

    private static Image EnsureSponge(Transform parent, Sprite sprite)
    {
        Transform existing = parent.Find("CleaningSponge");
        GameObject go = existing == null ? new GameObject("CleaningSponge", typeof(RectTransform), typeof(Image)) : existing.gameObject;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Set(rect, new Vector2(-18f, 118f), new Vector2(155f, 112f));
        rect.localEulerAngles = new Vector3(0f, 0f, -12f);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform EnsureContactPoint(Transform parent, string name, Vector2 position)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing == null ? CreateRect(name, parent, position, new Vector2(24f, 24f)) : existing.GetComponent<RectTransform>();
        Set(rect, position, new Vector2(24f, 24f));
        return rect;
    }

    private static Button EnsureButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color colour)
    {
        Transform found = parent.Find(name);
        GameObject go = found == null ? new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)) : found.gameObject;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Set(rect, position, size);
        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = colour;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = go.GetComponentInChildren<TMP_Text>(true);
        if (text == null) text = CreateText("Label", go.transform, label, Vector2.zero, size - new Vector2(16f, 12f), 26, Color.white);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static Sprite ImportSprite(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Set(rect, position, size);
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize, Color colour)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = colour;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static void Set(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
