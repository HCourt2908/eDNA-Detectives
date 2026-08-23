using UnityEngine;

namespace EDNA.Investigation.V2
{
    public static class InvestigationV2Theme
    {
        public static readonly Color32 Background = new Color32(4, 18, 28, 255);
        public static readonly Color32 Deep = new Color32(8, 36, 54, 255);
        public static readonly Color32 SurfaceQuiet = new Color32(14, 51, 72, 255);
        public static readonly Color32 Surface = new Color32(20, 65, 92, 255);
        public static readonly Color32 SurfaceRaised = new Color32(27, 82, 115, 255);
        public static readonly Color32 BorderSoft = new Color32(95, 212, 214, 42);
        public static readonly Color32 BorderStrong = new Color32(95, 212, 214, 118);
        public static readonly Color32 Accent = new Color32(255, 138, 91, 255);
        public static readonly Color32 OnAccent = new Color32(43, 15, 6, 255);
        public static readonly Color32 Primary = new Color32(95, 212, 214, 255);
        public static readonly Color32 OnPrimary = new Color32(4, 34, 43, 255);
        public static readonly Color32 Focus = new Color32(244, 211, 94, 255);
        public static readonly Color32 Success = new Color32(95, 211, 154, 255);
        public static readonly Color32 Danger = new Color32(242, 118, 107, 255);
        public static readonly Color32 Unknown = new Color32(147, 178, 196, 255);
        public static readonly Color32 TextPrimary = new Color32(242, 248, 251, 255);
        public static readonly Color32 TextSecondary = new Color32(179, 204, 218, 255);
        public static readonly Color32 TextMuted = new Color32(145, 177, 194, 255);
        public static readonly Color32 Paper = new Color32(234, 241, 244, 255);
        public static readonly Color32 PaperRaised = new Color32(248, 251, 252, 255);
        public static readonly Color32 PaperInk = new Color32(18, 48, 67, 255);
        public static readonly Color32 PaperMuted = new Color32(77, 111, 132, 255);
        public static readonly Color32 PaperBorder = new Color32(116, 142, 155, 255);
        public static readonly Color32 PaperSelected = new Color32(217, 236, 243, 255);
        public static readonly Color32 PaperSelectedBorder = new Color32(27, 109, 138, 255);
        public static readonly Color32 MapLabelPlate = new Color32(4, 18, 28, 232);
        public static readonly Color32 PrimaryShadow = new Color32(0, 0, 0, 82);
        public static readonly Color32 PaperShadow = new Color32(18, 48, 67, 46);

        public const float ShellRadius = 20f;
        public const float CardRadius = 16f;
        public const float SmallRadius = 12f;
        public const float ControlRadius = 14f;

        private static Font bodyFont;
        private static Font displayFont;
        private static Font dataFont;

        public static Font BodyFont => bodyFont != null ? bodyFont : bodyFont = LoadFont(
            "InvestigationV2/Fonts/NunitoSans-Variable",
            "Investigation/Fonts/FiraSans-Regular");

        public static Font DisplayFont => displayFont != null ? displayFont : displayFont = LoadFont(
            "InvestigationV2/Fonts/NunitoSans-Variable",
            "Investigation/Fonts/FiraSans-SemiBold");

        public static Font DataFont => dataFont != null ? dataFont : dataFont = LoadFont(
            "Investigation/Fonts/FiraMono-Medium",
            "Investigation/Fonts/FiraMono-Regular");

        private static Font LoadFont(string preferredPath, string fallbackPath)
        {
            Font font = Resources.Load<Font>(preferredPath);
            if (font != null) return font;
            font = Resources.Load<Font>(fallbackPath);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
