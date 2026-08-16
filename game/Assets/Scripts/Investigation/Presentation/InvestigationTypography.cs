using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public enum InvestigationFontRole
    {
        Interface,
        Data
    }

    /// <summary>
    /// Shared font resolver for the investigation UI. Font assets live in
    /// Resources so runtime-created action labels use the same typography as
    /// the serialized prefabs.
    /// </summary>
    public static class InvestigationTypography
    {
        private const string SansRegularPath = "Investigation/Fonts/FiraSans-Regular";
        private const string SansSemiboldPath = "Investigation/Fonts/FiraSans-SemiBold";
        private const string MonoRegularPath = "Investigation/Fonts/FiraMono-Regular";
        private const string MonoMediumPath = "Investigation/Fonts/FiraMono-Medium";

        private static Font sansRegular;
        private static Font sansSemibold;
        private static Font monoRegular;
        private static Font monoMedium;
        private static Font fallback;

        public static void Apply(Text text, InvestigationFontRole role, FontStyle emphasis)
        {
            if (text == null) return;
            bool emphasized = emphasis == FontStyle.Bold || emphasis == FontStyle.BoldAndItalic;
            text.font = Resolve(role, emphasized);
            text.fontStyle = emphasis == FontStyle.Italic || emphasis == FontStyle.BoldAndItalic
                ? FontStyle.Italic
                : FontStyle.Normal;
        }

        public static Font Resolve(InvestigationFontRole role, bool emphasized = false)
        {
            Font resolved;
            if (role == InvestigationFontRole.Data)
            {
                resolved = emphasized
                    ? Load(ref monoMedium, MonoMediumPath)
                    : Load(ref monoRegular, MonoRegularPath);
            }
            else
            {
                resolved = emphasized
                    ? Load(ref sansSemibold, SansSemiboldPath)
                    : Load(ref sansRegular, SansRegularPath);
            }

            if (resolved != null) return resolved;
            if (fallback == null) fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return fallback;
        }

        private static Font Load(ref Font cache, string resourcePath)
        {
            if (cache == null) cache = Resources.Load<Font>(resourcePath);
            return cache;
        }
    }
}
