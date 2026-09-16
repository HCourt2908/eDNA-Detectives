using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private static CanvasGroup EnsureCanvasGroup(RectTransform rect)
        {
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            return group == null ? rect.gameObject.AddComponent<CanvasGroup>() : group;
        }

        private static RectTransform FindNamedRect(Transform root, string objectName)
        {
            if (root == null) return null;
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            for (int index = 0; index < rects.Length; index++)
            {
                if (rects[index].gameObject.activeInHierarchy && rects[index].name == objectName) return rects[index];
            }
            return null;
        }

        private static string CompactReportMetadataValue(string value, int maximumCharacters, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            if (normalized.Length <= maximumCharacters) return normalized;
            return normalized.Substring(0, Mathf.Max(1, maximumCharacters - 1)).TrimEnd() + "…";
        }

        private static void ConfigureContentDrivenText(TextMeshProUGUI text)
        {
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        private static void ConfigureWrappingChoice(Button button, TextMeshProUGUI label)
        {
            ConfigureContentDrivenText(label);
            button.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
            InvestigationContentHeightLayoutElement contentHeight =
                button.gameObject.AddComponent<InvestigationContentHeightLayoutElement>();
            contentHeight.Configure(label, 52f, 20f, 16f);
        }

    }
}
