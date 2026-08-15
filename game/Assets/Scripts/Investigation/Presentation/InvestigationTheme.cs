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
        public static readonly Color Background = new Color32(6, 24, 35, 255);
        public static readonly Color BackgroundDeep = new Color32(4, 17, 27, 255);
        public static readonly Color Surface = new Color32(13, 43, 58, 255);
        public static readonly Color SurfaceRaised = new Color32(17, 55, 70, 255);
        public static readonly Color SurfaceInteractive = new Color32(18, 62, 78, 255);
        public static readonly Color SurfaceSelected = new Color32(18, 72, 83, 255);
        public static readonly Color SurfaceSuccess = new Color32(16, 61, 51, 255);
        public static readonly Color SurfaceWarning = new Color32(61, 47, 27, 255);

        public static readonly Color Primary = new Color32(50, 204, 209, 255);
        public static readonly Color PrimarySoft = new Color32(103, 220, 222, 255);
        public static readonly Color Sand = new Color32(245, 230, 190, 255);
        public static readonly Color TextPrimary = new Color32(242, 247, 248, 255);
        public static readonly Color TextSecondary = new Color32(177, 207, 213, 255);
        public static readonly Color TextMuted = new Color32(142, 174, 181, 255);
        public static readonly Color Border = new Color32(38, 91, 104, 255);
        public static readonly Color BorderQuiet = new Color32(27, 69, 82, 255);
        public static readonly Color Success = new Color32(92, 214, 157, 255);
        public static readonly Color Warning = new Color32(255, 190, 90, 255);
        public static readonly Color Danger = new Color32(242, 125, 111, 255);

        public const float MotionFast = 0.12f;
        public const float MotionStandard = 0.20f;

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
