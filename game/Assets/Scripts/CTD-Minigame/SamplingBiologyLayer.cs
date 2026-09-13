using UnityEngine;

/// <summary>
/// Preloaded depth-world presentation for the 3.1 upcast. Organisms are
/// authored as independent UI children and are never spawned randomly during
/// the short sampling run. The layer only updates their scroll and swim motion.
/// </summary>
public class SamplingBiologyLayer : MonoBehaviour
{
    [Header("Depth world")]
    [Tooltip("The authored organism container. Keep this under the ocean background and above the background layers.")]
    public RectTransform worldRoot;
    [Range(0f, 1010f)] public float maximumDepth = 1000f;
    [Min(1f)] public float depthPixelsPerMeter = 1.8f;
    [Min(0f)] public float fadeDepthMeters = 210f;

    [Header("Preloaded organisms")]
    public SamplingBiologyFish[] organisms;

    private bool running;

    public void Begin(float currentDepth, float depthLimit)
    {
        maximumDepth = Mathf.Max(1f, depthLimit);
        running = true;
        if (organisms == null)
        {
            return;
        }

        foreach (SamplingBiologyFish organism in organisms)
        {
            if (organism == null)
            {
                continue;
            }

            organism.depthPixelsPerMeter = depthPixelsPerMeter;
            organism.fadeDepthMeters = fadeDepthMeters;
            organism.Begin(currentDepth, maximumDepth);
        }
    }

    public void SetDepth(float currentDepth, float depthLimit)
    {
        maximumDepth = Mathf.Max(1f, depthLimit);
        if (!running || organisms == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        foreach (SamplingBiologyFish organism in organisms)
        {
            if (organism == null)
            {
                continue;
            }

            organism.depthPixelsPerMeter = depthPixelsPerMeter;
            organism.fadeDepthMeters = fadeDepthMeters;
            organism.ApplyDepth(currentDepth, maximumDepth, deltaTime);
        }
    }

    private void OnDisable()
    {
        running = false;
    }
}
