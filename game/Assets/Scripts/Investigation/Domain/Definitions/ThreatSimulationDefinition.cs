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

    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation/Threat", fileName = "Threat_")]
    public sealed class ThreatSimulationDefinition : ScriptableObject
    {
        [SerializeField] private string threatId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 5)] private string summary = string.Empty;
        [SerializeField] private Sprite icon;
        [SerializeField] private ThreatGlyphKind glyphKind;
        [SerializeField] private List<ThreatSpeciesPredictionDefinition> speciesPredictions = new List<ThreatSpeciesPredictionDefinition>();
        [SerializeField, TextArea(1, 3)] private string temperaturePrediction = string.Empty;
        [SerializeField, TextArea(1, 3)] private string seafloorPrediction = string.Empty;
        [SerializeField, TextArea(1, 3)] private string physicalConfirmation = string.Empty;

        public string ThreatId => threatId;
        public string DisplayName => displayName;
        public string Summary => summary;
        public Sprite Icon => icon;
        public ThreatGlyphKind GlyphKind => glyphKind;
        public IReadOnlyList<ThreatSpeciesPredictionDefinition> SpeciesPredictions => speciesPredictions;
        public string TemperaturePrediction => temperaturePrediction;
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
