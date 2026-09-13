using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the authored 3.1 ocean layers from the current CTD depth.
/// All layer geometry stays in the scene; this component only changes colour,
/// opacity, noise time and depth-driven environment offsets.
/// </summary>
public class SamplingOceanBackground : MonoBehaviour
{
    [Header("Layer references")]
    public Image baseLayer;
    public Image depthGradientLayer;
    public Image surfaceRefractionLayer;
    public Image[] lightBeams;
    public RectTransform[] particleDots;

    [Header("Depth colour response")]
    public Color surfaceTint = new Color(0.72f, 0.93f, 1f, 1f);
    public Color deepTint = new Color(0.16f, 0.28f, 0.42f, 1f);
    [Min(0f)] public float surfaceDepth = 180f;
    [Min(0f)] public float deepDepth = 820f;
    [Range(0f, 1f)] public float deepGradientOpacity = 0.46f;

    [Header("Light beams")]
    [Range(0f, 1f)] public float surfaceBeamOpacity = 0.52f;
    [Range(0f, 1f)] public float deepBeamOpacity = 0.04f;
    public Color beamTint = new Color(0.48f, 0.86f, 1f, 1f);

    [Header("Suspended particles")]
    [Range(0f, 1f)] public float surfaceParticleOpacity = 0.72f;
    [Range(0f, 1f)] public float deepParticleOpacity = 0.20f;
    [Range(0f, 1f)] public float particleDensity = 1f;
    [Min(0f)] public float particleDriftSpeed = 0.12f;
    [Min(0f)] public float particleDriftDistance = 18f;

    [Header("Surface refraction")]
    [Range(0f, 1f)] public float surfaceRefractionOpacity = 0.34f;
    [Min(0f)] public float noiseSpeed = 0.35f;
    [Range(0f, 1f)] public float noiseStrength = 0.18f;

    [Header("Ascent parallax (pixels per metre)")]
    [Min(0f)] public float farParticleScroll = 0.65f;
    [Min(0f)] public float nearParticleScroll = 2.6f;
    public RectTransform seabed;
    [Min(0f)] public float seabedScroll = 2.4f;
    [Min(1f)] public float seabedVisibleAscent = 150f;
    [Header("Beam motion")]
    [Min(0f)] public float beamSwayDegrees = 2f;
    [Min(0f)] public float beamSwaySpeed = 0.35f;
    [Range(0f, 1f)] public float beamPulse = 0.18f;
    private Quaternion[] beamRotations;
    private Vector2 seabedOrigin;
    private float ascentDistance;
    private Vector2[] particleOrigins;
    private float[] particlePhases;
    private Color[] particleBaseColours;
    private Color[] beamBaseColours;
    private Material gradientMaterial;
    private Material refractionMaterial;

    private void Awake()
    {
        CaptureAuthoredLayers();
        SetDepth(0f, 1010f);
    }

    private void OnEnable()
    {
        if (particleOrigins == null)
        {
            CaptureAuthoredLayers();
        }
    }

    private void Update()
    {
        float time = Time.time;
        if (lightBeams != null && beamRotations != null)
            for (int i = 0; i < lightBeams.Length; i++)
                if (lightBeams[i] != null)
                    lightBeams[i].rectTransform.localRotation = beamRotations[i] *
                        Quaternion.Euler(0f, 0f, Mathf.Sin(time * beamSwaySpeed + i * 2.1f) * beamSwayDegrees);
        if (gradientMaterial != null)
        {
            gradientMaterial.SetFloat("_NoiseTime", time * noiseSpeed);
            gradientMaterial.SetFloat("_NoiseStrength", noiseStrength);
        }

        if (refractionMaterial != null)
        {
            refractionMaterial.SetFloat("_WaveTime", time * noiseSpeed);
            refractionMaterial.SetFloat("_WaveStrength", noiseStrength);
        }

        if (particleDots == null || particleOrigins == null)
        {
            return;
        }

        for (int index = 0; index < particleDots.Length; index++)
        {
            RectTransform dot = particleDots[index];
            if (dot == null || index >= particleOrigins.Length)
            {
                continue;
            }

            // Density controls visibility; authored positions seed depth scrolling.
            bool visible = particleDots.Length == 0 ||
                           (index + 1f) / particleDots.Length <= particleDensity;
            if (dot.gameObject.activeSelf != visible)
            {
                dot.gameObject.SetActive(visible);
            }

            float phase = particlePhases[index];
            Vector2 position = particleOrigins[index];
            position.x += Mathf.Sin(time * particleDriftSpeed + phase) * particleDriftDistance;
            // The same depth change drives all layers. Wrapping happens outside
            // the viewport, so particles continuously pass the rising device.
            float parallax = Mathf.Lerp(farParticleScroll, nearParticleScroll, (index % 3) / 2f);
            RectTransform parent = dot.parent as RectTransform;
            float height = parent != null ? parent.rect.height : 900f;
            float bottom = parent != null ? parent.rect.yMin : -450f;
            float anchorY = parent != null ? Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, dot.anchorMin.y) : 0f;
            float low = bottom - anchorY - 30f;
            position.y = low + Mathf.Repeat(position.y - ascentDistance * parallax - low, Mathf.Max(1f, height + 60f));
            dot.anchoredPosition = position;
        }
    }

    public void SetDepth(float depth, float maximumDepth)
    {
        if (beamBaseColours == null || (particleDots != null && particleBaseColours == null))
        {
            CaptureAuthoredLayers();
        }

        ascentDistance = Mathf.Max(0f, maximumDepth - depth);
        if (seabed != null)
        {
            seabed.anchoredPosition = seabedOrigin + Vector2.down * ascentDistance * seabedScroll;
            CanvasGroup group = seabed.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f - Mathf.SmoothStep(0f, 1f, ascentDistance / Mathf.Max(1f, seabedVisibleAscent));
        }
        float normalizedDepth = Mathf.Clamp01(depth / Mathf.Max(1f, maximumDepth));
        float surfaceFactor = 1f - Mathf.InverseLerp(surfaceDepth, deepDepth, depth);
        surfaceFactor = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(surfaceFactor));

        if (baseLayer != null)
        {
            baseLayer.color = Color.Lerp(deepTint, surfaceTint, surfaceFactor);
        }

        if (gradientMaterial != null)
        {
            gradientMaterial.SetFloat("_Depth01", normalizedDepth);
            gradientMaterial.SetFloat("_Opacity", deepGradientOpacity);
        }

        float beamOpacity = Mathf.Lerp(deepBeamOpacity, surfaceBeamOpacity, surfaceFactor);
        if (lightBeams != null)
        {
            for (int index = 0; index < lightBeams.Length; index++)
            {
                Image beam = lightBeams[index];
                if (beam == null) continue;
                Color baseColour = index < beamBaseColours.Length ? beamBaseColours[index] : beamTint;
                beam.color = new Color(beamTint.r, beamTint.g, beamTint.b, baseColour.a * beamOpacity * (1f - beamPulse * (0.5f + 0.5f * Mathf.Sin(Time.time * beamSwaySpeed * 1.7f + index * 2.1f))));
            }
        }

        if (refractionMaterial != null)
        {
            refractionMaterial.SetFloat("_SurfaceFactor", surfaceFactor);
            refractionMaterial.SetFloat("_Opacity", surfaceRefractionOpacity);
        }
        else if (surfaceRefractionLayer != null)
        {
            Color colour = surfaceRefractionLayer.color;
            colour.a = surfaceRefractionOpacity * surfaceFactor;
            surfaceRefractionLayer.color = colour;
        }

        float particleOpacity = Mathf.Lerp(deepParticleOpacity, surfaceParticleOpacity, surfaceFactor);
        if (particleDots != null)
        {
            for (int index = 0; index < particleDots.Length; index++)
            {
                RectTransform dot = particleDots[index];
                if (dot == null) continue;
                Image image = dot.GetComponent<Image>();
                if (image == null) continue;
                Color baseColour = index < particleBaseColours.Length ? particleBaseColours[index] : Color.white;
                image.color = new Color(baseColour.r, baseColour.g, baseColour.b, baseColour.a * particleOpacity);
            }
        }
    }

    private void OnDestroy()
    {
        if (gradientMaterial != null) Destroy(gradientMaterial);
        if (refractionMaterial != null) Destroy(refractionMaterial);
    }

    private void CaptureAuthoredLayers()
    {
        if (baseLayer == null)
        {
            baseLayer = GetComponent<Image>();
        }

        if (seabed != null) seabedOrigin = seabed.anchoredPosition;
        gradientMaterial = depthGradientLayer != null ? new Material(depthGradientLayer.material) : null;
        if (depthGradientLayer != null) depthGradientLayer.material = gradientMaterial;
        refractionMaterial = surfaceRefractionLayer != null ? new Material(surfaceRefractionLayer.material) : null;
        if (surfaceRefractionLayer != null) surfaceRefractionLayer.material = refractionMaterial;

        if (particleDots != null)
        {
            particleOrigins = new Vector2[particleDots.Length];
            particlePhases = new float[particleDots.Length];
            particleBaseColours = new Color[particleDots.Length];
            for (int index = 0; index < particleDots.Length; index++)
            {
                RectTransform dot = particleDots[index];
                if (dot == null) continue;
                particleOrigins[index] = dot.anchoredPosition;
                particlePhases[index] = (index * 2.399963f) % (Mathf.PI * 2f);
                Image image = dot.GetComponent<Image>();
                particleBaseColours[index] = image != null ? image.color : Color.white;
            }
        }

        if (lightBeams != null)
        {
            beamBaseColours = new Color[lightBeams.Length];
            beamRotations = new Quaternion[lightBeams.Length];
            for (int index = 0; index < lightBeams.Length; index++)
            {
                beamRotations[index] = lightBeams[index] != null ? lightBeams[index].rectTransform.localRotation : Quaternion.identity;
                beamBaseColours[index] = lightBeams[index] != null ? lightBeams[index].color : beamTint;
            }
        }
        else
        {
            beamBaseColours = new Color[0];
        }
    }
}
