using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {

        private IEnumerator AnimateCaseClosedStamp()
        {
            RectTransform stamp = FindNamedRect(contentRoot, "Case Closed Stamp");
            if (stamp == null || InvestigationMotionSettings.ReducedMotion) yield break;
            CanvasGroup opacity = stamp.gameObject.AddComponent<CanvasGroup>();
            const float duration = 0.28f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (stamp == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                opacity.alpha = eased;
                stamp.localScale = Vector3.one * Mathf.Lerp(1.16f, 1f, eased);
                stamp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-9f, -3f, eased));
                yield return null;
            }
            opacity.alpha = 1f;
            stamp.localScale = Vector3.one;
            stamp.localRotation = Quaternion.Euler(0f, 0f, -3f);
        }
    }
}
