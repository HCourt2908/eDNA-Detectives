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
        public string Depth => depthText == null ? string.Empty : depthText.text;
        public string Temperature => temperatureText == null ? string.Empty : temperatureText.text;
        public string Habitat => habitatText == null ? string.Empty : habitatText.text;
        public string Sensitivity => sensitivityText == null ? string.Empty : sensitivityText.text;

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
            depthText.text = $"Depth range: {AsSentence(depths)}";
            temperatureText.text = $"Temperature: {AsSentence(temperature)}";
            habitatText.text = $"Habitat: {AsSentence(habitat)}";
            sensitivityText.text = $"Sensitivity: {AsSentence(sensitivity)}";
            missionText.text = mission;
        }

        private static string AsSentence(string value)
        {
            string trimmed = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
            char finalCharacter = trimmed[trimmed.Length - 1];
            return finalCharacter == '.' || finalCharacter == '!' || finalCharacter == '?'
                ? trimmed
                : $"{trimmed}.";
        }
    }
}
