using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationSamplePlannerPanelView : MonoBehaviour
    {
        [SerializeField] private RectTransform selectorRoot;
        [SerializeField] private Text siteTitleText;
        [SerializeField] private Text siteDescriptionText;
        [SerializeField] private Text metadataText;

        public RectTransform SelectorRoot => selectorRoot;
        public string SiteTitle => siteTitleText == null ? string.Empty : siteTitleText.text;
        public string Metadata => metadataText == null ? string.Empty : metadataText.text;

        public void ConfigureReferences(
            RectTransform selectorReference,
            Text siteTitleReference,
            Text siteDescriptionReference,
            Text metadataReference)
        {
            selectorRoot = selectorReference;
            siteTitleText = siteTitleReference;
            siteDescriptionText = siteDescriptionReference;
            metadataText = metadataReference;
        }

        public void Bind(
            string siteTitle,
            string siteDescription,
            string relatedHypothesis,
            int availableSamples,
            int pendingSamples)
        {
            siteTitleText.text = string.IsNullOrWhiteSpace(siteTitle) ? "No sampling site available" : siteTitle;
            siteDescriptionText.text = string.IsNullOrWhiteSpace(siteDescription)
                ? "Choose a valid site before collecting a sample."
                : siteDescription;
            metadataText.text =
                $"Related theory: {(string.IsNullOrWhiteSpace(relatedHypothesis) ? "None" : relatedHypothesis)}\n" +
                $"Follow-up samples available: {availableSamples}" +
                (pendingSamples > 0 ? $"    Awaiting results: {pendingSamples}" : string.Empty);
        }
    }
}
