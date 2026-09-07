using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private void CreateCaseClosedStamp(Transform parent)
        {
            RectTransform slot = CreatePanel("Case Closed Stamp Slot", parent, new Color(0f, 0f, 0f, 0f), 0f);
            LayoutElement size = AddLayout(slot, 76f, 0f);
            size.minWidth = 180f;
            size.preferredWidth = 180f;
            RectTransform stamp = CreatePanel("Case Closed Stamp", slot, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(stamp, 10f, 10f, -10f, -10f);
            stamp.localRotation = Quaternion.Euler(0f, 0f, -3f);
            InvestigationBorderGraphic border = CreateGraphic<InvestigationBorderGraphic>("Stamp Border", stamp);
            Stretch(border.rectTransform, 0f, 0f, 0f, 0f);
            border.Configure(4f, 2f);
            border.color = InvestigationTheme.PaperSelectedBorder;
            Text label = CreateText("Stamp Title", stamp, "CASE CLOSED", 17, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleCenter, InvestigationTheme.DisplayFont);
            Anchor(label.rectTransform, 0f, 0.40f, 1f, 1f, 6f, 0f, -6f, -3f);
            Text caption = CreateText("Stamp Caption", stamp, "Evidence reviewed", 10, FontStyle.Normal,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleCenter, InvestigationTheme.DataFont);
            Anchor(caption.rectTransform, 0f, 0f, 1f, 0.43f, 6f, 4f, -6f, 0f);
        }

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
