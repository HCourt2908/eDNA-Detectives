using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CleaningTarget : MonoBehaviour
{
    [Header("Bottle layout")]
    public RectTransform targetRect;
    public Image equipmentImage;
    public RectTransform foamRoot;
    public CleaningFoamSpot[] foamSpots;

    // Legacy builder fields. They are deliberately unused by the local-foam stage.
    [HideInInspector] public string displayName;
    [HideInInspector] public Image dirtyOverlay;
    [HideInInspector] public Image cleanProgressFill;
    [HideInInspector] public Image rinseProgressFill;
    [HideInInspector] public TMPro.TMP_Text statusText;

    [Header("Tuning")]
    [Min(0.05f)] public float scrubSecondsPerSpot = 0.3f;
    [Min(0.05f)] public float rinseSecondsPerSpot = 0.18f;

    private readonly Dictionary<CleaningFoamSpot, float> foam = new();
    public float FoamCoverage { get; private set; }

    public bool IsScreenPointOnBottle(Vector2 screenPoint)
    {
        var canvas = GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(targetRect, screenPoint, camera);
    }

    public void ResetTarget()
    {
        foam.Clear();
        foreach (CleaningFoamSpot spot in foamSpots)
        {
            foam[spot] = 0f;
            spot.SetCoverage(0f);
        }
        RefreshCoverage();
    }

    public void ApplyFoamAt(Vector2 screenPoint, float deltaTime)
    {
        CleaningFoamSpot spot = FindNearestSpot(screenPoint, true);
        if (spot == null) return;
        foam[spot] = Mathf.Clamp01(foam[spot] + deltaTime / scrubSecondsPerSpot);
        spot.SetCoverage(foam[spot]);
        RefreshCoverage();
    }

    public void RinseAt(Vector2 screenPoint, float deltaTime)
    {
        CleaningFoamSpot spot = FindNearestSpot(screenPoint, false);
        if (spot == null || foam[spot] <= 0f) return;
        foam[spot] = Mathf.Clamp01(foam[spot] - deltaTime / rinseSecondsPerSpot);
        spot.SetCoverage(foam[spot]);
        RefreshCoverage();
    }

    public void ClearFoam()
    {
        foreach (CleaningFoamSpot spot in foamSpots)
        {
            foam[spot] = 0f;
            spot.SetCoverage(0f);
        }
        RefreshCoverage();
    }

    private CleaningFoamSpot FindNearestSpot(Vector2 screenPoint, bool preferUncovered)
    {
        var canvas = GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(foamRoot, screenPoint, camera, out Vector2 local);
        CleaningFoamSpot selected = null;
        float best = float.MaxValue;
        foreach (CleaningFoamSpot spot in foamSpots)
        {
            float amount = foam[spot];
            if (preferUncovered ? amount >= .99f : amount <= .01f) continue;
            float distance = (spot.Rect.anchoredPosition - local).sqrMagnitude;
            if (distance < best) { best = distance; selected = spot; }
        }
        return selected;
    }

    private void RefreshCoverage()
    {
        float total = 0f;
        foreach (float value in foam.Values) total += value;
        FoamCoverage = foam.Count == 0 ? 0f : total / foam.Count;
    }
}
