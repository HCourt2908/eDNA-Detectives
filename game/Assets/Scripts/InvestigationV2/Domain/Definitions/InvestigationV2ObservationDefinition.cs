using System;
using UnityEngine;

namespace EDNA.Investigation.V2.Domain
{
    [Serializable]
    public sealed class InvestigationV2ObservationDefinition
    {
        [SerializeField] private string evidenceId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 5)] private string detail = string.Empty;
        [SerializeField] private string relatedSpeciesId = string.Empty;
        [SerializeField] private ObservationSource source;
        [SerializeField] private EvidenceUnlockStage unlockStage;
        [SerializeField] private string unlockThreatId = string.Empty;
        [SerializeField] private V2EvidenceConfidence confidence;
        [SerializeField] private ObservationClaimType claimType;
        [SerializeField] private EvidenceCategory category;
        [SerializeField, TextArea(1, 3)] private string confidenceReason = string.Empty;

        public string EvidenceId => evidenceId;
        public string DisplayName => displayName;
        public string Detail => detail;
        public string RelatedSpeciesId => relatedSpeciesId;
        public ObservationSource Source => source;
        public EvidenceUnlockStage UnlockStage => unlockStage;
        public string UnlockThreatId => unlockThreatId;
        public V2EvidenceConfidence Confidence => confidence;
        public ObservationClaimType ClaimType => claimType;
        public EvidenceCategory Category => category;
        public string ConfidenceReason => confidenceReason;
    }
}
