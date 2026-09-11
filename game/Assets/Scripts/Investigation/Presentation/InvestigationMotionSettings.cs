using UnityEngine;

namespace EDNA.Investigation
{
    public static class InvestigationMotionSettings
    {
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        // Rendering tests can freeze animation without changing any saved player setting.
        private static bool reducedMotionForTests;
        public static bool ReducedMotion => reducedMotionForTests;

        public static void SetReducedMotionForTests(bool reducedMotion)
        {
            reducedMotionForTests = reducedMotion;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTestOverride() => reducedMotionForTests = false;
#else
        public static bool ReducedMotion => false;
#endif
    }
}
