using System.Collections.Generic;
using EDNA.Core;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation/Species", fileName = "Species_")]
    public sealed class InvestigationSpeciesDefinition : ScriptableObject
    {
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 5)] private string description = string.Empty;
        [SerializeField] private Sprite icon;
        [SerializeField] private SpeciesGlyphKind glyphKind;
        [SerializeField] private List<DepthBand> preferredDepths = new List<DepthBand>();
        [SerializeField] private string temperaturePreference = string.Empty;
        [SerializeField] private List<string> dietSpeciesIds = new List<string>();
        [SerializeField] private List<string> predatorSpeciesIds = new List<string>();
        [SerializeField] private List<string> sensitivityTags = new List<string>();
        [SerializeField] private Vector2 mapPosition;

        public string SpeciesId => speciesId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public SpeciesGlyphKind GlyphKind => glyphKind;
        public IReadOnlyList<DepthBand> PreferredDepths => preferredDepths;
        public string TemperaturePreference => temperaturePreference;
        public IReadOnlyList<string> DietSpeciesIds => dietSpeciesIds;
        public IReadOnlyList<string> PredatorSpeciesIds => predatorSpeciesIds;
        public IReadOnlyList<string> SensitivityTags => sensitivityTags;
        public Vector2 MapPosition => mapPosition;
    }
}
