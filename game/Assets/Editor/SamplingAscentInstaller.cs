using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SamplingAscentInstaller
{
    [MenuItem("OceanX/Apply Ascent Environment")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CTD-Minigame.unity");
        var controller = UnityEngine.Object.FindAnyObjectByType<CTDSamplingController>(FindObjectsInactive.Include);
        if (controller == null || controller.oceanVisuals == null) throw new Exception("Missing ocean controller");
        var ocean = controller.oceanVisuals;
        if (ocean.GetComponent<RectMask2D>() == null) ocean.gameObject.AddComponent<RectMask2D>();
        var existing = ocean.transform.Find("SeabedReference");
        if (existing == null)
        {
            var go = new GameObject("SeabedReference", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(SamplingSeabedGraphic));
            go.transform.SetParent(ocean.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f); rect.sizeDelta = new Vector2(0f, 125f);
            rect.anchoredPosition = Vector2.zero;
            rect.SetSiblingIndex(2);
            var graphic = go.GetComponent<SamplingSeabedGraphic>();
            graphic.color = new Color(0.055f, 0.16f, 0.19f, 0.85f);
            graphic.raycastTarget = false;
            var group = go.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            existing = rect;
        }
        if (existing.GetComponent<CanvasRenderer>() == null) existing.gameObject.AddComponent<CanvasRenderer>();
        ocean.seabed = (RectTransform)existing;
        ocean.deepParticleOpacity = 0.48f;
        ocean.particleDriftDistance = 7f;
        // Size differences match the near/middle/far scroll groups.
        for (int i = 0; i < ocean.particleDots.Length; i++)
            if (ocean.particleDots[i] != null)
                ocean.particleDots[i].sizeDelta = Vector2.one * (3f + (i % 3) * 2.5f);
        EditorUtility.SetDirty(ocean);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ASCENT_ENVIRONMENT_OK: depth-driven particles, seabed and beam motion installed; device layout unchanged.");
    }
    public static void Validate()
    {
        CTDSceneBuilder.Validate();
        var controller = UnityEngine.Object.FindAnyObjectByType<CTDSamplingController>(FindObjectsInactive.Include);
        var ocean = controller.oceanVisuals;
        if (ocean.seabed == null || ocean.GetComponent<RectMask2D>() == null)
            throw new Exception("Missing clipped seabed reference");
        if (ocean.seabed.GetComponent<CanvasRenderer>() == null)
            throw new Exception("Seabed is missing CanvasRenderer");
        var graphic = ocean.seabed.GetComponent<SamplingSeabedGraphic>();
        // Exercise mesh generation and renderer submission, not just motion.
        graphic.Rebuild(CanvasUpdate.PreRender);
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(SamplingOceanBackground).GetMethod("CaptureAuthoredLayers", flags).Invoke(ocean, null);
        var update = typeof(SamplingOceanBackground).GetMethod("Update", flags);
        ocean.SetDepth(controller.maximumDepth, controller.maximumDepth);
        update.Invoke(ocean, null);
        var start = new Vector2[ocean.particleDots.Length];
        for (int i = 0; i < start.Length; i++) start[i] = ocean.particleDots[i].anchoredPosition;
        Vector2 floorStart = ocean.seabed.anchoredPosition;
        ocean.SetDepth(controller.maximumDepth - 1f, controller.maximumDepth);
        update.Invoke(ocean, null);
        for (int i = 0; i < start.Length; i++)
        {
            float expected = Mathf.Lerp(ocean.farParticleScroll, ocean.nearParticleScroll, (i % 3) / 2f);
            float actual = start[i].y - ocean.particleDots[i].anchoredPosition.y;
            if (Mathf.Abs(actual - expected) > 0.02f) throw new Exception("Particle depth motion mismatch: " + i);
        }
        if (Mathf.Abs(floorStart.y - ocean.seabed.anchoredPosition.y - ocean.seabedScroll) > 0.02f)
            throw new Exception("Seabed scroll mismatch");
        ocean.SetDepth(controller.maximumDepth - ocean.seabedVisibleAscent, controller.maximumDepth);
        if (ocean.seabed.GetComponent<CanvasGroup>().alpha > 0.001f)
            throw new Exception("Seabed failed to disappear");
        ocean.SetDepth(controller.maximumDepth, controller.maximumDepth);
        update.Invoke(ocean, null);
        for (int i = 0; i < start.Length; i++)
            if (Mathf.Abs(start[i].y - ocean.particleDots[i].anchoredPosition.y) > 0.02f)
                throw new Exception("Particle restart mismatch");
        if ((floorStart - ocean.seabed.anchoredPosition).sqrMagnitude > 0.001f)
            throw new Exception("Seabed restart mismatch");
        Debug.Log("ASCENT_VALIDATION_OK: all particles follow depth, seabed scrolls/fades, restart restores initial depth state.");
        // Checks intentionally modify only the in-memory scene; never save.
    }

}
