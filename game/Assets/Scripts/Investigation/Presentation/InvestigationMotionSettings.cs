using UnityEngine;

namespace EDNA.Investigation
{
    public static class InvestigationMotionSettings
    {
        private const string ReducedMotionPreferenceKey = "EDNA.Investigation.ReducedMotion";

        public static bool ReducedMotion => PlayerPrefs.GetInt(ReducedMotionPreferenceKey, 0) == 1;

        public static void SetReducedMotion(bool reducedMotion)
        {
            PlayerPrefs.SetInt(ReducedMotionPreferenceKey, reducedMotion ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
