using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class SampleComparisonBoardView : MonoBehaviour
    {
        [SerializeField] private Text sampleHeaderText;
        [SerializeField] private Text instructionsText;
        [SerializeField] private RectTransform cardsRoot;
        [SerializeField] private Text findingsText;

        public RectTransform CardsRoot => cardsRoot;

        public void ConfigureReferences(
            Text sampleHeaderReference,
            Text instructionsReference,
            RectTransform cardsRootReference,
            Text findingsReference)
        {
            sampleHeaderText = sampleHeaderReference;
            instructionsText = instructionsReference;
            cardsRoot = cardsRootReference;
            findingsText = findingsReference;
        }

        public void SetContent(string sampleHeader, string instructions, string findings)
        {
            sampleHeaderText.text = sampleHeader;
            instructionsText.text = instructions;
            findingsText.text = findings;
        }

        public void ClearCards()
        {
            for (int index = cardsRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(cardsRoot.GetChild(index).gameObject);
            }
        }
    }
}
