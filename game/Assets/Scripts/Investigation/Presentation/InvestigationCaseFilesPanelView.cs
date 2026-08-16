using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationCaseFilesPanelView : MonoBehaviour
    {
        [SerializeField] private Text caseTitleText;
        [SerializeField] private Text briefingText;
        [SerializeField] private InvestigationGlyphGraphic speciesGraphic;
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
            InvestigationGlyphGraphic speciesGraphicReference,
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
            speciesGraphic = speciesGraphicReference;
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
            string speciesId,
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
            if (speciesGraphic != null) speciesGraphic.SetGlyph(GetSpeciesGlyph(speciesId));
            speciesCounterText.text = $"SPECIES RECORD  {speciesNumber:00} / {speciesCount:00}";
            speciesTitleText.text = speciesTitle;
            speciesDescriptionText.text = speciesDescription;
            depthText.text = $"DEPTH RANGE: {AsLowerSentence(depths)}";
            temperatureText.text = $"TEMPERATURE: {AsLowerSentence(temperature)}";
            habitatText.text = $"HABITAT: {AsLowerSentence(habitat)}";
            sensitivityText.text = $"SENSITIVITY: {AsLowerSentence(sensitivity)}";
            missionText.text = mission;
        }

        private static InvestigationGlyph GetSpeciesGlyph(string speciesId)
        {
            switch (speciesId)
            {
                case "mock_cold_fish": return InvestigationGlyph.ColdFish;
                case "mock_predator": return InvestigationGlyph.PredatorFish;
                case "mock_stable_species": return InvestigationGlyph.StableFish;
                case "mock_prey": return InvestigationGlyph.PreyFish;
                case "mock_deep_species": return InvestigationGlyph.DeepFish;
                case "mock_warm_fish": return InvestigationGlyph.WarmFish;
                default: return InvestigationGlyph.Fish;
            }
        }

        private static string AsLowerSentence(string value)
        {
            string trimmed = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
            char finalCharacter = trimmed[trimmed.Length - 1];
            string sentence = finalCharacter == '.' || finalCharacter == '!' || finalCharacter == '?'
                ? trimmed
                : $"{trimmed}.";
            return sentence.ToLowerInvariant().Replace("°c", "°C");
        }
    }
}
