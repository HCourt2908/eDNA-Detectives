using UnityEngine;

namespace EDNA.Investigation
{
    public static class InvestigationTheme
    {
        public static readonly Color32 Background = new Color32(6, 22, 31, 255);
        public static readonly Color32 Deep = new Color32(10, 31, 42, 255);
        public static readonly Color32 SurfaceQuiet = new Color32(15, 43, 55, 255);
        public static readonly Color32 Surface = new Color32(22, 54, 66, 255);
        public static readonly Color32 SurfaceRaised = new Color32(29, 70, 79, 255);
        public static readonly Color32 BorderSoft = new Color32(148, 178, 181, 32);
        public static readonly Color32 BorderStrong = new Color32(126, 157, 162, 95);
        public static readonly Color32 Accent = new Color32(240, 155, 119, 255);
        public static readonly Color32 OnAccent = new Color32(43, 15, 6, 255);
        public static readonly Color32 Primary = new Color32(124, 205, 194, 255);
        public static readonly Color32 OnPrimary = new Color32(4, 34, 43, 255);
        public static readonly Color32 Focus = new Color32(237, 218, 152, 255);
        public static readonly Color32 Success = new Color32(137, 206, 175, 255);
        public static readonly Color32 Danger = new Color32(238, 153, 141, 255);
        public static readonly Color32 Unknown = new Color32(147, 178, 196, 255);
        public static readonly Color32 TextPrimary = new Color32(242, 248, 251, 255);
        public static readonly Color32 TextSecondary = new Color32(179, 204, 218, 255);
        public static readonly Color32 TextMuted = new Color32(145, 177, 194, 255);
        public static readonly Color32 Paper = new Color32(243, 240, 231, 255);
        public static readonly Color32 PaperRaised = new Color32(251, 250, 245, 255);
        public static readonly Color32 PaperInk = new Color32(24, 52, 60, 255);
        public static readonly Color32 PaperMuted = new Color32(76, 98, 101, 255);
        public static readonly Color32 PaperBorder = new Color32(117, 130, 124, 255);
        public static readonly Color32 PaperSelected = new Color32(221, 235, 226, 255);
        public static readonly Color32 PaperSelectedBorder = new Color32(41, 106, 99, 255);
        public static readonly Color32 ReportGuide = new Color32(228, 236, 232, 255);
        public static readonly Color32 ReportSuccess = new Color32(221, 238, 223, 255);
        public static readonly Color32 ReportError = new Color32(245, 226, 218, 255);
        public static readonly Color32 PrimaryShadow = new Color32(0, 0, 0, 42);
        public static readonly Color32 PaperShadow = new Color32(18, 48, 67, 22);

        public static readonly Color32 PaperRule = new Color32(210, 214, 202, 255);

        // Water column, top to bottom. A flat fill gives the scene no light
        // direction, which is what made the deep sea read as a single dark
        // rectangle rather than a body of water.
        public static readonly Color32 WaterTop = new Color32(18, 63, 89, 255);
        public static readonly Color32 WaterUpper = new Color32(10, 43, 63, 255);
        public static readonly Color32 WaterLower = new Color32(5, 24, 38, 255);
        public static readonly Color32 WaterFloor = new Color32(1, 8, 16, 255);

        // Light and particulate. All three stay very low alpha on purpose: the
        // cue reads as water at a whisper and as stage lighting at any strength.
        public static readonly Color32 GodRay = new Color32(141, 194, 200, 9);
        public static readonly Color32 Caustic = new Color32(140, 218, 218, 10);
        public static readonly Color32 Vignette = new Color32(1, 6, 12, 110);
        public const float MarineSnowFarMaxAlpha = 0.10f;
        public const float MarineSnowNearMaxAlpha = 0.14f;
        public const float MarineSnowLargeMaxAlpha = 0.06f;

        // Semi-transparent so the water column and its particles carry through
        // the survey maps instead of stopping at an opaque panel edge.
        public static readonly Color32 MapSurface = new Color32(9, 35, 46, 210);
        public static readonly Color32 MapSurfaceHistorical = new Color32(23, 42, 50, 210);

        public const float ShellRadius = 14f;
        public const float CardRadius = 12f;
        public const float SmallRadius = 8f;
        public const float ControlRadius = 10f;

        private static Font bodyFont;
        private static Font displayFont;
        private static Font dataFont;

        public static Font BodyFont => bodyFont != null ? bodyFont : bodyFont = LoadFont(
            "Investigation/Fonts/NunitoSans-Variable");

        public static Font DisplayFont => displayFont != null ? displayFont : displayFont = LoadFont(
            "Investigation/Fonts/NunitoSans-Variable");

        public static Font DataFont => dataFont != null ? dataFont : dataFont = LoadFont(
            "Investigation/Fonts/FiraMono-Medium",
            "Investigation/Fonts/FiraMono-Regular");

        private static Font LoadFont(string preferredPath, string fallbackPath = null)
        {
            Font font = Resources.Load<Font>(preferredPath);
            if (font != null) return font;
            if (!string.IsNullOrEmpty(fallbackPath)) font = Resources.Load<Font>(fallbackPath);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
