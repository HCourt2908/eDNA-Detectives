using System;
using UnityEngine;

namespace EDNA.Investigation
{
    /// <summary>Owns one shared appearance clock and material for both survey maps.</summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationSeamountBackdrop : MonoBehaviour
    {
        public const double CycleDuration = 20d;
        private const string ResourceRoot = "Investigation/Seamount/";
        private static readonly int IllustratedTextureId = Shader.PropertyToID("_IllustratedTex");
        private static readonly int BathymetryTextureId = Shader.PropertyToID("_BathymetryTex");
        private static readonly int SurfaceMaskId = Shader.PropertyToID("_SurfaceMask");
        private static readonly int BlendBId = Shader.PropertyToID("_BlendB");
        private static readonly int BlendCId = Shader.PropertyToID("_BlendC");

        private Material renderMaterial;
        private Texture2D naturalTexture;
        private double startedAt;
        private bool initialisationAttempted;
        private Vector2 blendWeights;

        public Material RenderMaterial => renderMaterial;
        public Texture2D NaturalTexture => naturalTexture;
        public Vector2 BlendWeights => blendWeights;
        public double ElapsedSeconds => Time.unscaledTimeAsDouble - startedAt;

        public bool TryInitialise()
        {
            if (initialisationAttempted) return renderMaterial != null;
            initialisationAttempted = true;
            naturalTexture = Resources.Load<Texture2D>(ResourceRoot + "natural");
            Texture2D illustrated = Resources.Load<Texture2D>(ResourceRoot + "illustrated");
            Texture2D bathymetry = Resources.Load<Texture2D>(ResourceRoot + "bathymetry");
            Texture2D surfaceMask = Resources.Load<Texture2D>(ResourceRoot + "surface-mask");
            Shader shader = Resources.Load<Shader>(ResourceRoot + "SeamountCrossfade");
            if (naturalTexture == null || illustrated == null || bathymetry == null || surfaceMask == null || shader == null || !shader.isSupported)
            {
                Debug.LogError("The Investigation seamount appearance assets or shader are unavailable. Using the static seamount.", this);
                return false;
            }
            renderMaterial = new Material(shader)
            {
                name = "Investigation Shared Seamount Crossfade",
                hideFlags = HideFlags.HideAndDontSave
            };
            renderMaterial.SetTexture(IllustratedTextureId, illustrated);
            renderMaterial.SetTexture(BathymetryTextureId, bathymetry);
            renderMaterial.SetTexture(SurfaceMaskId, surfaceMask);
            RestartCycle();
            return true;
        }

        public void RestartCycle()
        {
            startedAt = Time.unscaledTimeAsDouble;
            ApplyCurrentAppearance();
        }

        private void LateUpdate() => ApplyCurrentAppearance();

        private void ApplyCurrentAppearance()
        {
            if (renderMaterial == null) return;
            Vector2 next = EvaluateBlends(ElapsedSeconds, InvestigationMotionSettings.ReducedMotion);
            if (next == blendWeights) return;
            blendWeights = next;
            renderMaterial.SetFloat(BlendBId, next.x);
            renderMaterial.SetFloat(BlendCId, next.y);
        }

        public static Vector2 EvaluateBlends(double elapsedSeconds, bool reducedMotion)
        {
            if (reducedMotion) return Vector2.zero;
            double t = Math.Max(0d, elapsedSeconds) % CycleDuration;
            // The B layer stays fully present beneath the C transition.
            float b = Ease((t - 1d) / 4d) * (1f - Ease((t - 15d) / 4d));
            float c = Ease((t - 5.5d) / 4d) * (1f - Ease((t - 10.5d) / 4d));
            return new Vector2(b, c);
        }

        private static float Ease(double value)
        {
            double t = Math.Max(0d, Math.Min(1d, value));
            return (float)((1d - Math.Cos(Math.PI * t)) * 0.5d);
        }

        private void OnDestroy()
        {
            if (renderMaterial == null) return;
            if (Application.isPlaying) Destroy(renderMaterial);
            else DestroyImmediate(renderMaterial);
            renderMaterial = null;
        }
    }
}
