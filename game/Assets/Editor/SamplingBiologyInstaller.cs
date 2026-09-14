using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Installs the small, authored 3.1 biology preview without rebuilding the
/// scene. All valid images in the Biology folder are preloaded at fixed depth
/// encounters; the runtime layer then scrolls and animates those objects.
/// </summary>
public static class SamplingBiologyInstaller
{
    private const string ScenePath = "Assets/Scenes/CTD-Minigame.unity";
    private const string BiologyFolder = "Assets/Art/CTD-Minigame/Biology/";
    private const string ShaderPath = "Assets/Shaders/BiologyWhiteKey.shader";
    private const string MaterialPath = BiologyFolder + "BiologyWhiteKey.mat";

    private sealed class PreviewEntry
    {
        public string fileName;
        public float depth;
        public float x;
        public float laneOffset;
        public float speed;
        public float size;
        public float opacity;
        public float phase;

        public PreviewEntry(string fileName, float depth, float x, float laneOffset, float speed, float size, float opacity, float phase)
        {
            this.fileName = fileName;
            this.depth = depth;
            this.x = x;
            this.laneOffset = laneOffset;
            this.speed = speed;
            this.size = size;
            this.opacity = opacity;
            this.phase = phase;
        }
    }

    [MenuItem("OceanX/Apply 3.1 Biology Preview")]
    public static void Apply()
    {
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CTDGameManager manager = UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);
        if (manager == null || manager.samplingPanel == null || manager.samplingController == null)
        {
            throw new InvalidOperationException("The CTD scene is missing its sampling panel or controller.");
        }

        Transform oceanBackground = manager.samplingPanel.transform.Find("OceanBackground");
        if (oceanBackground == null)
        {
            throw new InvalidOperationException("The CTD sampling panel is missing OceanBackground.");
        }

        Transform oldWorld = oceanBackground.Find("BiologyWorld");
        if (oldWorld != null)
        {
            UnityEngine.Object.DestroyImmediate(oldWorld.gameObject);
        }

        Material whiteKey = GetOrCreateWhiteKeyMaterial();
        GameObject worldObject = new GameObject("BiologyWorld", typeof(RectTransform), typeof(RectMask2D));
        worldObject.transform.SetParent(oceanBackground, false);
        worldObject.transform.SetAsLastSibling();
        RectTransform worldRoot = worldObject.GetComponent<RectTransform>();
        worldRoot.anchorMin = Vector2.zero;
        worldRoot.anchorMax = Vector2.one;
        worldRoot.offsetMin = Vector2.zero;
        worldRoot.offsetMax = Vector2.zero;
        worldRoot.pivot = new Vector2(0.5f, 0.5f);

        SamplingBiologyLayer layer = worldObject.AddComponent<SamplingBiologyLayer>();
        layer.worldRoot = worldRoot;
        manager.samplingController.maximumDepth = 1010f;
        layer.maximumDepth = manager.samplingController.maximumDepth;
        layer.depthPixelsPerMeter = 1.8f;
        layer.fadeDepthMeters = 210f;

        List<SamplingBiologyFish> fish = new List<SamplingBiologyFish>();
        PreviewEntry[] entries = BuildPreviewEntries();

        foreach (PreviewEntry entry in entries)
        {
            string assetPath = BiologyFolder + entry.fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Biology preview sprite not found: {assetPath}");
            }

            GameObject fishObject = new GameObject(entry.fileName, typeof(RectTransform), typeof(Image), typeof(SamplingBiologyFish));
            fishObject.transform.SetParent(worldRoot, false);
            RectTransform fishRect = fishObject.GetComponent<RectTransform>();
            fishRect.anchorMin = new Vector2(0.5f, 0.5f);
            fishRect.anchorMax = new Vector2(0.5f, 0.5f);
            fishRect.pivot = new Vector2(0.5f, 0.5f);
            fishRect.anchoredPosition = new Vector2(entry.x, 0f);
            fishRect.sizeDelta = new Vector2(entry.size, entry.size);

            Image image = fishObject.GetComponent<Image>();
            image.sprite = sprite;
            image.material = whiteKey;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, entry.opacity);

            SamplingBiologyFish fishBehaviour = fishObject.GetComponent<SamplingBiologyFish>();
            fishBehaviour.depthMeters = entry.depth;
            fishBehaviour.laneOffset = entry.laneOffset;
            fishBehaviour.horizontalSpeed = entry.speed;
            fishBehaviour.swimPhase = entry.phase;
            fishBehaviour.horizontalWrap = 1040f;
            fishBehaviour.opacity = entry.opacity;
            fishBehaviour.depthPixelsPerMeter = layer.depthPixelsPerMeter;
            fishBehaviour.fadeDepthMeters = layer.fadeDepthMeters;
            fishBehaviour.swimWobble = entry.fileName.StartsWith("l_", StringComparison.Ordinal) ? 8f : 18f;
            fishBehaviour.swimWobbleSpeed = entry.fileName.StartsWith("l_", StringComparison.Ordinal) ? 0.8f : 1.5f;

            fish.Add(fishBehaviour);
        }

        layer.organisms = fish.ToArray();
        manager.samplingController.biologyLayer = layer;
        RecoveryCableInstaller.InstallIntoScene(manager);
        EditorUtility.SetDirty(layer);
        EditorUtility.SetDirty(manager.samplingController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"SAMPLING_BIOLOGY_PREVIEW_OK: {fish.Count} preloaded encounters installed.");
    }

    private static Material GetOrCreateWhiteKeyMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException($"Biology white-key shader not found: {ShaderPath}");
            }

            material = new Material(shader)
            {
                name = "BiologyWhiteKey"
            };
            material.SetFloat("_WhiteCutoff", 0.88f);
            material.SetFloat("_WhiteSoftness", 0.12f);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        return material;
    }

    private static PreviewEntry[] BuildPreviewEntries()
    {
        string[] fileNames = Directory.GetFiles(BiologyFolder, "*.png")
            .Select(Path.GetFileName)
            .Where(name => TryParseName(name, out _, out _))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        List<PreviewEntry> entries = new List<PreviewEntry>();
        int[] bandCounts = new int[4];
        for (int index = 0; index < fileNames.Length; index++)
        {
            string fileName = fileNames[index];
            if (!TryParseName(fileName, out char sizeCode, out char depthCode))
            {
                continue;
            }

            int bandIndex = BandIndex(depthCode);
            int bandIndexInList = bandCounts[bandIndex]++;
            float depth = PreviewDepth(depthCode, bandIndexInList);
            bool large = sizeCode == 'l';
            float x = PreviewX(bandIndex, bandIndexInList);
            float lane = PreviewLane(bandIndex, bandIndexInList);
            float speed = ((index + bandIndexInList) % 2 == 0 ? 1f : -1f) * (large ? 28f : 16f);
            float size = large ? 340f : 135f;
            float opacity = large ? 0.74f : 0.58f;
            entries.Add(new PreviewEntry(fileName, depth, x, lane, speed, size, opacity, index * 0.73f));
        }

        return entries.ToArray();
    }

    private static bool TryParseName(string fileName, out char sizeCode, out char depthCode)
    {
        sizeCode = '\0';
        depthCode = '\0';
        string stem = Path.GetFileNameWithoutExtension(fileName);
        int firstSeparator = stem.IndexOf('_');
        int lastSeparator = stem.LastIndexOf('_');
        if (firstSeparator != 1 || lastSeparator <= firstSeparator + 1 || lastSeparator >= stem.Length - 1)
        {
            return false;
        }

        sizeCode = char.ToLowerInvariant(stem[0]);
        depthCode = char.ToLowerInvariant(stem[stem.Length - 1]);
        return (sizeCode == 's' || sizeCode == 'l') && BandIndex(depthCode) >= 0;
    }

    private static int BandIndex(char depthCode)
    {
        switch (char.ToLowerInvariant(depthCode))
        {
            case 's': return 0;
            case 'm': return 1;
            case 'b': return 2;
            case 'd': return 3;
            default: return -1;
        }
    }

    private static float PreviewDepth(char depthCode, int indexInBand)
    {
        switch (char.ToLowerInvariant(depthCode))
        {
            case 's': return 12f + indexInBand * 12f;
            case 'm': return Mathf.Min(780f, 260f + indexInBand * 150f);
            case 'b': return Mathf.Min(990f, 860f + indexInBand * 70f);
            case 'd': return Mathf.Min(1010f, 1002f + indexInBand * 4f);
            default: return 500f;
        }
    }

    private static float PreviewX(int bandIndex, int indexInBand)
    {
        float[] lanes = { -720f, -260f, 230f, 690f };
        return lanes[(bandIndex + indexInBand) % lanes.Length];
    }

    private static float PreviewLane(int bandIndex, int indexInBand)
    {
        float[] offsets = { 45f, -70f, 95f, -35f };
        return offsets[(bandIndex * 2 + indexInBand) % offsets.Length];
    }

}
