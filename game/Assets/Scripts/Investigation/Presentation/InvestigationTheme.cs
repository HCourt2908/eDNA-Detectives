using UnityEngine;

namespace EDNA.Investigation
{
    /// <summary>
    /// Shared visual tokens for the ocean-research investigation interface.
    /// Keep presentation components semantic so a future theme pass does not
    /// require hunting down screen-specific colour literals.
    /// </summary>
    public static class InvestigationTheme
    {
        public static readonly Color Background = new Color32(7, 16, 24, 255);
        public static readonly Color BackgroundDeep = new Color32(4, 10, 15, 255);
        public static readonly Color Surface = new Color32(15, 27, 36, 255);
        public static readonly Color SurfaceRaised = new Color32(20, 35, 46, 255);
        public static readonly Color SurfaceInteractive = new Color32(24, 43, 55, 255);
        public static readonly Color SurfaceSelected = new Color32(23, 54, 64, 255);
        public static readonly Color SurfaceSuccess = new Color32(19, 55, 45, 255);
        public static readonly Color SurfaceWarning = new Color32(58, 44, 25, 255);

        public static readonly Color Primary = new Color32(40, 184, 192, 255);
        public static readonly Color PrimarySoft = new Color32(103, 207, 211, 255);
        public static readonly Color Sand = new Color32(238, 224, 194, 255);
        public static readonly Color TextPrimary = new Color32(237, 243, 245, 255);
        public static readonly Color TextSecondary = new Color32(170, 192, 201, 255);
        public static readonly Color TextMuted = new Color32(114, 140, 151, 255);
        public static readonly Color Border = new Color32(44, 72, 84, 255);
        public static readonly Color BorderQuiet = new Color32(28, 49, 58, 255);
        public static readonly Color Success = new Color32(91, 199, 153, 255);
        public static readonly Color Warning = new Color32(232, 168, 79, 255);
        public static readonly Color Danger = new Color32(229, 112, 102, 255);

        public const float MotionFast = 0.12f;
        public const float MotionStandard = 0.20f;

        public const float CornerRadiusSmall = 4f;
        public const float CornerRadiusControl = 6f;
        public const float CornerRadiusCard = 8f;

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        public static Color Hover(Color color)
        {
            return Color.Lerp(color, PrimarySoft, 0.18f);
        }

        public static Color Pressed(Color color)
        {
            return Color.Lerp(color, BackgroundDeep, 0.24f);
        }
    }
}
