using System;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationStepperView : MonoBehaviour
    {
        [SerializeField] private Text categoryText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Text valueText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text countText;

        public string CategoryLabel => categoryText == null ? string.Empty : categoryText.text;
        public string ValueLabel => valueText == null ? string.Empty : valueText.text;
        public string CountLabel => countText == null ? string.Empty : countText.text;
        public Button PreviousButton => previousButton;
        public Button NextButton => nextButton;

        public void ConfigureReferences(
            Text categoryReference,
            Button previousReference,
            Text valueReference,
            Button nextReference,
            Text countReference)
        {
            categoryText = categoryReference;
            previousButton = previousReference;
            valueText = valueReference;
            nextButton = nextReference;
            countText = countReference;
        }

        public void Bind(
            string category,
            string value,
            int selectedIndex,
            int itemCount,
            Action onPrevious,
            Action onNext)
        {
            int safeCount = Mathf.Max(0, itemCount);
            int safeIndex = safeCount == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, safeCount - 1);
            categoryText.text = category.ToUpperInvariant();
            valueText.text = string.IsNullOrWhiteSpace(value) ? "Not available" : value;
            countText.text = safeCount == 0 ? "0 / 0" : $"{safeIndex + 1} / {safeCount}";
            BindButton(previousButton, onPrevious, safeCount > 1);
            BindButton(nextButton, onNext, safeCount > 1);
        }

        private static void BindButton(Button button, Action action, bool isInteractable)
        {
            if (button == null) return;
            button.interactable = isInteractable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
        }
    }
}
