using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public enum InvestigationStatusTone
    {
        Guide,
        Warning,
        Success
    }

    [DisallowMultipleComponent]
    public sealed class InvestigationStatusBannerView : MonoBehaviour
    {
        private static readonly Color GuideAccent = new Color32(50, 204, 209, 255);
        private static readonly Color WarningAccent = new Color32(255, 190, 90, 255);
        private static readonly Color SuccessAccent = new Color32(92, 214, 157, 255);
        private static readonly Color GuideBackground = new Color32(7, 25, 38, 250);
        private static readonly Color WarningBackground = new Color32(48, 40, 25, 250);
        private static readonly Color SuccessBackground = new Color32(10, 43, 43, 250);

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image accentImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Text messageText;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;
        [SerializeField, Range(0f, 1f)] private float fadeStartAlpha = 0.55f;

        private float fadeElapsed;
        private string currentLabel = string.Empty;
        private string currentMessage = string.Empty;
        private InvestigationStatusTone currentTone;

        public string CurrentLabel => currentLabel;
        public string CurrentMessage => currentMessage;
        public InvestigationStatusTone CurrentTone => currentTone;
        public Text MessageText => messageText;

        public void ConfigureReferences(
            CanvasGroup groupReference,
            Image backgroundReference,
            Image accentReference,
            Text labelReference,
            Text messageReference)
        {
            canvasGroup = groupReference;
            backgroundImage = backgroundReference;
            accentImage = accentReference;
            labelText = labelReference;
            messageText = messageReference;
        }

        public void Show(string label, string message, InvestigationStatusTone tone)
        {
            label = label ?? string.Empty;
            message = message ?? string.Empty;
            bool contentChanged = label != currentLabel || message != currentMessage || tone != currentTone;

            currentLabel = label;
            currentMessage = message;
            currentTone = tone;

            Color accentColor = GetAccentColor(tone);
            if (labelText != null)
            {
                labelText.text = label;
                labelText.color = accentColor;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            if (accentImage != null)
            {
                accentImage.color = accentColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = GetBackgroundColor(tone);
            }

            if (canvasGroup == null || !Application.isPlaying || !contentChanged)
            {
                if (canvasGroup != null) canvasGroup.alpha = 1f;
                enabled = false;
                return;
            }

            fadeElapsed = 0f;
            canvasGroup.alpha = fadeStartAlpha;
            enabled = true;
        }

        private void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            enabled = false;
        }

        private void Update()
        {
            if (canvasGroup == null)
            {
                enabled = false;
                return;
            }

            fadeElapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(fadeStartAlpha, 1f, Mathf.Clamp01(fadeElapsed / fadeDuration));
            if (fadeElapsed >= fadeDuration)
            {
                canvasGroup.alpha = 1f;
                enabled = false;
            }
        }

        private static Color GetAccentColor(InvestigationStatusTone tone)
        {
            switch (tone)
            {
                case InvestigationStatusTone.Warning: return WarningAccent;
                case InvestigationStatusTone.Success: return SuccessAccent;
                default: return GuideAccent;
            }
        }

        private static Color GetBackgroundColor(InvestigationStatusTone tone)
        {
            switch (tone)
            {
                case InvestigationStatusTone.Warning: return WarningBackground;
                case InvestigationStatusTone.Success: return SuccessBackground;
                default: return GuideBackground;
            }
        }
    }
}
