using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [Serializable]
    public sealed class ThreatSpeciesPredictionDefinition
    {
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private PredictionState predictedState;
        [SerializeField, TextArea(1, 3)] private string rationale = string.Empty;

        public string SpeciesId => speciesId;
        public PredictionState PredictedState => predictedState;
        public string Rationale => rationale;
    }

    public enum ScenarioFoodLinkKind { Feeding = 0, SinkingOrganicMatter = 1, CoralSpawn = 2 }

    [Serializable]
    public sealed class ScenarioFoodLinkDefinition
    {
        [SerializeField] private string sourceSpeciesId = string.Empty;
        [SerializeField] private string consumerSpeciesId = string.Empty;
        [SerializeField] private ScenarioFoodLinkKind kind;
        public string SourceSpeciesId => sourceSpeciesId;
        public string ConsumerSpeciesId => consumerSpeciesId;
        public ScenarioFoodLinkKind Kind => kind;
    }

    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation/Threat", fileName = "Threat_")]
    public sealed class ThreatSimulationDefinition : ScriptableObject
    {
        [SerializeField] private string threatId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 5)] private string summary = string.Empty;
        [SerializeField] private Sprite icon;
        [SerializeField] private ThreatGlyphKind glyphKind = ThreatGlyphKind.Plastic;
        [SerializeField] private List<ThreatSpeciesPredictionDefinition> speciesPredictions = new List<ThreatSpeciesPredictionDefinition>();
        [SerializeField] private bool useFoodWebCascade = true;
        [SerializeField] private bool optionalExploration;
        public bool OptionalExploration => optionalExploration;
        [SerializeField] private List<string> displaySpeciesIds = new List<string>();
        [SerializeField] private List<ScenarioFoodLinkDefinition> foodSupplyLinks = new List<ScenarioFoodLinkDefinition>();
        public bool UseFoodWebCascade => useFoodWebCascade;
        public IReadOnlyList<string> DisplaySpeciesIds => displaySpeciesIds;
        public IReadOnlyList<ScenarioFoodLinkDefinition> FoodSupplyLinks => foodSupplyLinks;
        [SerializeField, TextArea(1, 3)] private string seafloorPrediction = string.Empty;
        [SerializeField, TextArea(1, 3)] private string physicalConfirmation = string.Empty;

        public string ThreatId => threatId;
        public string DisplayName => displayName;
        public string Summary => summary;
        public Sprite Icon => icon;
        public ThreatGlyphKind GlyphKind => glyphKind;
        public IReadOnlyList<ThreatSpeciesPredictionDefinition> SpeciesPredictions => speciesPredictions;
        public string SeafloorPrediction => seafloorPrediction;
        public string PhysicalConfirmation => physicalConfirmation;

        public ThreatSpeciesPredictionDefinition FindPrediction(string speciesId)
        {
            for (int index = 0; index < speciesPredictions.Count; index++)
            {
                ThreatSpeciesPredictionDefinition prediction = speciesPredictions[index];
                if (prediction != null && string.Equals(prediction.SpeciesId, speciesId, StringComparison.Ordinal))
                {
                    return prediction;
                }
            }

            return null;
        }
    }
}
