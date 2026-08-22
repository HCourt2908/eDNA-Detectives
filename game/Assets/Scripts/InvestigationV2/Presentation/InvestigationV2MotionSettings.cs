using UnityEngine;

namespace EDNA.Investigation.V2
{
    public static class InvestigationV2MotionSettings
    {
        private const string ReducedMotionPreferenceKey = "EDNA.Investigation.V2.ReducedMotion";

        public static bool ReducedMotion => PlayerPrefs.GetInt(ReducedMotionPreferenceKey, 0) == 1;

        public static void SetReducedMotion(bool reducedMotion)
        {
            PlayerPrefs.SetInt(ReducedMotionPreferenceKey, reducedMotion ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
