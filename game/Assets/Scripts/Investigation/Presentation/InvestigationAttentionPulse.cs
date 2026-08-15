using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationAttentionPulse : MonoBehaviour
    {
        [SerializeField] private Text textTarget;
        [SerializeField] private Image backgroundTarget;

        private Color restingTextColor;
        private Color pulseTextColor;
        private Color restingBackgroundColor;
        private Color pulseBackgroundColor;
        private float pulseSpeed;

        public bool IsPulsing => enabled && gameObject.activeInHierarchy;

        public void Configure(
            Text text,
            Image background,
            Color restingText,
            Color pulsingText,
            Color restingBackground,
            Color pulsingBackground,
            float speed)
        {
            textTarget = text;
            backgroundTarget = background;
            restingTextColor = restingText;
            pulseTextColor = pulsingText;
            restingBackgroundColor = restingBackground;
            pulseBackgroundColor = pulsingBackground;
            pulseSpeed = Mathf.Max(0.1f, speed);
        }

        public void SetPulsing(bool shouldPulse)
        {
            enabled = shouldPulse;
            if (!shouldPulse)
            {
                RestoreRestingVisuals();
            }
        }

        private void Update()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
            if (textTarget != null)
            {
                textTarget.color = Color.Lerp(restingTextColor, pulseTextColor, pulse);
            }

            if (backgroundTarget != null)
            {
                backgroundTarget.color = Color.Lerp(restingBackgroundColor, pulseBackgroundColor, pulse);
            }
        }

        private void OnDisable()
        {
            RestoreRestingVisuals();
        }

        private void RestoreRestingVisuals()
        {
            if (textTarget != null)
            {
                textTarget.color = restingTextColor;
            }

            if (backgroundTarget != null)
            {
                backgroundTarget.color = restingBackgroundColor;
            }
        }
    }
}
