using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [Serializable]
    public sealed class EvidenceRecord
    {
        [SerializeField] private string evidenceId = string.Empty;
        [SerializeField] private EvidenceType evidenceType;
        [SerializeField] private List<string> relatedSpeciesIds = new List<string>();
        [SerializeField] private List<string> sourceSampleIds = new List<string>();
        [SerializeField] private EvidenceConfidence confidence;
        [SerializeField] private List<string> evidenceTags = new List<string>();
        [SerializeField] private string displayText = string.Empty;
        [SerializeField] private string confidenceReason = string.Empty;

        public EvidenceRecord(
            string evidenceId,
            EvidenceType evidenceType,
            IEnumerable<string> relatedSpeciesIds,
            IEnumerable<string> sourceSampleIds,
            EvidenceConfidence confidence,
            IEnumerable<string> evidenceTags,
            string displayText,
            string confidenceReason)
        {
            this.evidenceId = evidenceId;
            this.evidenceType = evidenceType;
            this.relatedSpeciesIds = new List<string>(relatedSpeciesIds ?? Array.Empty<string>());
            this.sourceSampleIds = new List<string>(sourceSampleIds ?? Array.Empty<string>());
            this.confidence = confidence;
            this.evidenceTags = new List<string>(evidenceTags ?? Array.Empty<string>());
            this.displayText = displayText;
            this.confidenceReason = confidenceReason;
        }

        public string EvidenceId => evidenceId;
        public EvidenceType EvidenceType => evidenceType;
        public IReadOnlyList<string> RelatedSpeciesIds => relatedSpeciesIds;
        public IReadOnlyList<string> SourceSampleIds => sourceSampleIds;
        public EvidenceConfidence Confidence => confidence;
        public IReadOnlyList<string> EvidenceTags => evidenceTags;
        public string DisplayText => displayText;
        public string ConfidenceReason => confidenceReason;

        public bool HasTag(string tag)
        {
            for (int index = 0; index < evidenceTags.Count; index++)
            {
                if (string.Equals(evidenceTags[index], tag, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
