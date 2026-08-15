using System.Collections.Generic;
using EDNA.Core;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation/Sample Site", fileName = "Site_")]
    public sealed class SampleSiteDefinition : ScriptableObject
    {
        [SerializeField] private string siteId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 5)] private string description = string.Empty;
        [SerializeField] private List<DepthBand> availableDepths = new List<DepthBand>();
        [SerializeField] private List<string> habitatTags = new List<string>();
        [SerializeField] private Vector2 mapPosition;

        public string SiteId => siteId;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<DepthBand> AvailableDepths => availableDepths;
        public IReadOnlyList<string> HabitatTags => habitatTags;
        public Vector2 MapPosition => mapPosition;

        public bool SupportsDepth(DepthBand depthBand)
        {
            return availableDepths.Contains(depthBand);
        }
    }
}
