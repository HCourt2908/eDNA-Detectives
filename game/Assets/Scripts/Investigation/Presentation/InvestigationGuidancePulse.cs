using System;
using UnityEngine;

namespace EDNA.Investigation
{
    /// <summary>One shared pulse phase keeps simultaneous choice cues coordinated.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InvestigationGuidancePulse : MonoBehaviour
    {
        private CanvasGroup group;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private void LateUpdate()
        {
            group.alpha = InvestigationMotionSettings.ReducedMotion ? 1f
                : .22f + .78f * (float)(.5d - .5d * Math.Cos(Time.unscaledTimeAsDouble * Math.PI * 2d / 1.4d));
        }
    }
}
