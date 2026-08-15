using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationCaseFilesPanelView : MonoBehaviour
    {
        [SerializeField] private Text caseTitleText;
        [SerializeField] private Text briefingText;
        [SerializeField] private Text speciesCounterText;
        [SerializeField] private Text speciesTitleText;
        [SerializeField] private Text speciesDescriptionText;
        [SerializeField] private Text depthText;
        [SerializeField] private Text temperatureText;
        [SerializeField] private Text habitatText;
        [SerializeField] private Text sensitivityText;
        [SerializeField] private Text missionText;

        public string CaseTitle => caseTitleText == null ? string.Empty : caseTitleText.text;
        public string SpeciesTitle => speciesTitleText == null ? string.Empty : speciesTitleText.text;

        public void ConfigureReferences(
            Text caseTitleReference,
            Text briefingReference,
            Text speciesCounterReference,
            Text speciesTitleReference,
            Text speciesDescriptionReference,
            Text depthReference,
            Text temperatureReference,
            Text habitatReference,
            Text sensitivityReference,
            Text missionReference)
        {
            caseTitleText = caseTitleReference;
            briefingText = briefingReference;
            speciesCounterText = speciesCounterReference;
            speciesTitleText = speciesTitleReference;
            speciesDescriptionText = speciesDescriptionReference;
            depthText = depthReference;
            temperatureText = temperatureReference;
            habitatText = habitatReference;
            sensitivityText = sensitivityReference;
            missionText = missionReference;
        }

        public void Bind(
            string caseTitle,
            string briefing,
            int speciesNumber,
            int speciesCount,
            string speciesTitle,
            string speciesDescription,
            string depths,
            string temperature,
            string habitat,
            string sensitivity,
            string mission)
        {
            caseTitleText.text = caseTitle;
            briefingText.text = briefing;
            speciesCounterText.text = $"SPECIES RECORD  {speciesNumber:00} / {speciesCount:00}";
            speciesTitleText.text = speciesTitle;
            speciesDescriptionText.text = speciesDescription;
            depthText.text = $"DEPTH RANGE\n{depths}";
            temperatureText.text = $"TEMPERATURE\n{temperature}";
            habitatText.text = $"HABITAT\n{habitat}";
            sensitivityText.text = $"SENSITIVITY\n{sensitivity}";
            missionText.text = mission;
        }
    }
}
