using System;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text labelText;

        public string Label => labelText == null ? string.Empty : labelText.text;

        public void ConfigureReferences(Button buttonReference, Image backgroundReference, Text labelReference)
        {
            button = buttonReference;
            background = backgroundReference;
            labelText = labelReference;
        }

        public void Bind(string label, Action action, Color color, bool isInteractable = true)
        {
            if (labelText != null)
            {
                labelText.text = label;
            }

            if (background != null)
            {
                background.color = color;
            }

            if (button == null)
            {
                return;
            }

            button.interactable = isInteractable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
        }
    }
}
