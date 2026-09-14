using UnityEngine;
using UnityEngine.UI;

// A lightweight, editable seabed silhouette; no texture or spawned objects.
[RequireComponent(typeof(CanvasRenderer))]
public class SamplingSeabedGraphic : MaskableGraphic
{
    protected override void OnEnable()
    {
        // Repair older scene instances before Graphic accesses its renderer.
        if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
        base.OnEnable();
    }

    [Range(0f, 1f)] public float ridgeHeight = 0.45f;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect bounds = rectTransform.rect;
        const int segments = 48;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float ridge = 0.48f + Mathf.Sin(t * 17f) * 0.19f + Mathf.Sin(t * 39f + 1f) * 0.08f;
            float x = Mathf.Lerp(bounds.xMin, bounds.xMax, t);
            mesh.AddVert(new Vector3(x, bounds.yMin), color, Vector2.zero);
            Color crest = color; crest.a *= 0.7f;
            mesh.AddVert(new Vector3(x, bounds.yMin + bounds.height * (1f - ridgeHeight + ridge * ridgeHeight)), crest, Vector2.one);
            if (i == segments) continue;
            int n = i * 2;
            mesh.AddTriangle(n, n + 1, n + 2);
            mesh.AddTriangle(n + 1, n + 3, n + 2);
        }
    }
}
