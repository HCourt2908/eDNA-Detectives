using System.Collections.Generic;
using EDNA.Core;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation/Species", fileName = "Species_")]
    public sealed class InvestigationSpeciesDefinition : ScriptableObject
    {
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private string canonicalSpeciesId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string shortDisplayName = string.Empty;
        [SerializeField] private string scientificName = string.Empty;
        [SerializeField, TextArea(2, 5)] private string description = string.Empty;
        [SerializeField] private Sprite icon;
        [SerializeField] private SpeciesGlyphKind glyphKind;
        [SerializeField] private InvestigationTrophicRole trophicRole;
        [SerializeField] private List<string> aliases = new List<string>();
        [SerializeField] private List<DepthBand> preferredDepths = new List<DepthBand>();
        [SerializeField] private List<string> habitatTags = new List<string>();
        [SerializeField] private List<string> dietSpeciesIds = new List<string>();
        [SerializeField] private List<string> predatorSpeciesIds = new List<string>();
        [SerializeField] private List<string> sensitivityTags = new List<string>();
        [SerializeField] private DepthBand mapDepthBand;

        public string SpeciesId => speciesId;
        public string CanonicalSpeciesId => string.IsNullOrWhiteSpace(canonicalSpeciesId) ? speciesId : canonicalSpeciesId;
        public string DisplayName => displayName;
        public string GameplayName => string.IsNullOrWhiteSpace(shortDisplayName) ? displayName : shortDisplayName;
        public string ScientificName => scientificName;
        public string Description => description;
        public Sprite Icon => icon;
        public SpeciesGlyphKind GlyphKind => glyphKind;
        public InvestigationTrophicRole TrophicRole => trophicRole;
        public IReadOnlyList<string> Aliases => aliases;
        public IReadOnlyList<DepthBand> PreferredDepths => preferredDepths;
        public IReadOnlyList<string> HabitatTags => habitatTags;
        public IReadOnlyList<string> DietSpeciesIds => dietSpeciesIds;
        public IReadOnlyList<string> PredatorSpeciesIds => predatorSpeciesIds;
        public IReadOnlyList<string> SensitivityTags => sensitivityTags;
        public DepthBand MapDepthBand => mapDepthBand;

        public bool MatchesIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return false;
            if (string.Equals(speciesId, identifier, System.StringComparison.Ordinal)
                || string.Equals(CanonicalSpeciesId, identifier, System.StringComparison.Ordinal))
            {
                return true;
            }

            if (aliases == null) return false;
            for (int index = 0; index < aliases.Count; index++)
            {
                if (string.Equals(aliases[index], identifier, System.StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
