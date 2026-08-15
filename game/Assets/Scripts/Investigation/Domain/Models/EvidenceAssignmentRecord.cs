using System;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [Serializable]
    public sealed class EvidenceAssignmentRecord
    {
        [SerializeField] private string evidenceId = string.Empty;
        [SerializeField] private string hypothesisId = string.Empty;
        [SerializeField] private EvidenceAssignmentKind assignmentKind;

        public EvidenceAssignmentRecord(string evidenceId, string hypothesisId, EvidenceAssignmentKind assignmentKind)
        {
            this.evidenceId = evidenceId;
            this.hypothesisId = hypothesisId;
            this.assignmentKind = assignmentKind;
        }

        public string EvidenceId => evidenceId;
        public string HypothesisId => hypothesisId;
        public EvidenceAssignmentKind AssignmentKind => assignmentKind;
    }
}
