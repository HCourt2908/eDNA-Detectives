using System;
using System.Collections.Generic;
using EDNA.Core;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [Serializable]
    public sealed class InvestigationState
    {
        [SerializeField] private int currentRound;
        [SerializeField] private int remainingSamples;
        [SerializeField] private List<EDNAResultData> allResults = new List<EDNAResultData>();
        [SerializeField] private List<EvidenceRecord> unlockedEvidence = new List<EvidenceRecord>();
        [SerializeField] private List<string> identifiedEvidenceIds = new List<string>();
        [SerializeField] private List<string> rejectedClassificationKeys = new List<string>();
        [SerializeField] private string selectedHypothesisId = string.Empty;
        [SerializeField] private List<EvidenceAssignmentRecord> evidenceAssignments = new List<EvidenceAssignmentRecord>();
        [SerializeField] private List<InvestigationSamplePlan> requestedSamples = new List<InvestigationSamplePlan>();
        [SerializeField] private List<string> completedSampleRequestIds = new List<string>();
        [SerializeField] private ConclusionStatus conclusionStatus = ConclusionStatus.NotSubmitted;

        public InvestigationState(int remainingSamples)
        {
            this.remainingSamples = Math.Max(0, remainingSamples);
        }

        public int CurrentRound => currentRound;
        public int RemainingSamples => remainingSamples;
        public int PendingSampleCount
        {
            get
            {
                int pendingCount = 0;
                for (int index = 0; index < requestedSamples.Count; index++)
                {
                    InvestigationSamplePlan plan = requestedSamples[index];
                    string requestId = plan?.Request?.requestId;
                    if (!IsSampleCompleted(requestId))
                    {
                        pendingCount++;
                    }
                }

                return pendingCount;
            }
        }
        public int AvailableSampleSlots => Math.Max(0, remainingSamples - PendingSampleCount);
        public int CompletedSampleCount => completedSampleRequestIds.Count;
        public IReadOnlyList<EDNAResultData> AllResults => allResults;
        public IReadOnlyList<EvidenceRecord> UnlockedEvidence => unlockedEvidence;
        public IReadOnlyList<string> IdentifiedEvidenceIds => identifiedEvidenceIds;
        public int MisclassificationCount => rejectedClassificationKeys.Count;
        public string SelectedHypothesisId => selectedHypothesisId;
        public IReadOnlyList<EvidenceAssignmentRecord> EvidenceAssignments => evidenceAssignments;
        public IReadOnlyList<InvestigationSamplePlan> RequestedSamples => requestedSamples;
        public ConclusionStatus ConclusionStatus => conclusionStatus;

        internal void AddInitialResult(EDNAResultData result)
        {
            if (result == null)
            {
                return;
            }

            allResults.Add(result);
            currentRound = Math.Max(currentRound, result.roundIndex);
        }

        internal void AddPlan(InvestigationSamplePlan plan)
        {
            requestedSamples.Add(plan);
            currentRound = plan.RoundIndex;
        }

        internal void AddResult(EDNAResultData result)
        {
            allResults.Add(result);
            currentRound = Math.Max(currentRound, result.roundIndex);
        }

        internal bool CompletePlan(string requestId)
        {
            if (string.IsNullOrEmpty(requestId) || IsSampleCompleted(requestId))
            {
                return false;
            }

            for (int index = 0; index < requestedSamples.Count; index++)
            {
                InvestigationSamplePlan plan = requestedSamples[index];
                if (plan?.Request != null
                    && string.Equals(plan.Request.requestId, requestId, StringComparison.Ordinal))
                {
                    completedSampleRequestIds.Add(requestId);
                    remainingSamples = Math.Max(0, remainingSamples - 1);
                    return true;
                }
            }

            return false;
        }

        internal bool CancelPendingPlan(string requestId)
        {
            if (string.IsNullOrEmpty(requestId) || IsSampleCompleted(requestId))
            {
                return false;
            }

            for (int index = 0; index < requestedSamples.Count; index++)
            {
                InvestigationSamplePlan plan = requestedSamples[index];
                if (plan?.Request != null
                    && string.Equals(plan.Request.requestId, requestId, StringComparison.Ordinal))
                {
                    requestedSamples.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public bool IsSampleCompleted(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                return false;
            }

            for (int index = 0; index < completedSampleRequestIds.Count; index++)
            {
                if (string.Equals(completedSampleRequestIds[index], requestId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal void ReplaceEvidence(List<EvidenceRecord> evidence)
        {
            unlockedEvidence = evidence ?? new List<EvidenceRecord>();

            for (int identifiedIndex = identifiedEvidenceIds.Count - 1; identifiedIndex >= 0; identifiedIndex--)
            {
                if (FindEvidence(identifiedEvidenceIds[identifiedIndex]) == null)
                {
                    identifiedEvidenceIds.RemoveAt(identifiedIndex);
                }
            }

            for (int assignmentIndex = evidenceAssignments.Count - 1; assignmentIndex >= 0; assignmentIndex--)
            {
                if (FindEvidence(evidenceAssignments[assignmentIndex].EvidenceId) == null)
                {
                    evidenceAssignments.RemoveAt(assignmentIndex);
                }
            }
        }

        internal void IdentifyEvidence(string evidenceId)
        {
            if (!string.IsNullOrEmpty(evidenceId) && !IsEvidenceIdentified(evidenceId))
            {
                identifiedEvidenceIds.Add(evidenceId);
            }
        }

        internal bool RecordMisclassification(string evidenceId, AnomalyClaimType claimType)
        {
            if (string.IsNullOrEmpty(evidenceId))
            {
                return false;
            }

            string key = BuildClassificationKey(evidenceId, claimType);
            if (rejectedClassificationKeys.Contains(key))
            {
                return false;
            }

            rejectedClassificationKeys.Add(key);
            return true;
        }

        public bool HasRejectedClassification(string evidenceId, AnomalyClaimType claimType)
        {
            return !string.IsNullOrEmpty(evidenceId)
                && rejectedClassificationKeys.Contains(BuildClassificationKey(evidenceId, claimType));
        }

        internal void AssignEvidence(string evidenceId, string hypothesisId, EvidenceAssignmentKind assignmentKind)
        {
            for (int index = evidenceAssignments.Count - 1; index >= 0; index--)
            {
                EvidenceAssignmentRecord existing = evidenceAssignments[index];
                if (string.Equals(existing.EvidenceId, evidenceId, StringComparison.Ordinal)
                    && string.Equals(existing.HypothesisId, hypothesisId, StringComparison.Ordinal))
                {
                    evidenceAssignments.RemoveAt(index);
                }
            }

            evidenceAssignments.Add(new EvidenceAssignmentRecord(evidenceId, hypothesisId, assignmentKind));
        }

        internal void SelectHypothesis(string hypothesisId)
        {
            selectedHypothesisId = hypothesisId ?? string.Empty;
            conclusionStatus = ConclusionStatus.NotSubmitted;
        }

        internal void SetConclusionStatus(ConclusionStatus status)
        {
            conclusionStatus = status;
        }

        public EvidenceRecord FindEvidence(string evidenceId)
        {
            for (int index = 0; index < unlockedEvidence.Count; index++)
            {
                if (string.Equals(unlockedEvidence[index].EvidenceId, evidenceId, StringComparison.Ordinal))
                {
                    return unlockedEvidence[index];
                }
            }

            return null;
        }

        public bool IsEvidenceIdentified(string evidenceId)
        {
            for (int index = 0; index < identifiedEvidenceIds.Count; index++)
            {
                if (string.Equals(identifiedEvidenceIds[index], evidenceId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public List<EvidenceRecord> GetIdentifiedEvidence()
        {
            List<EvidenceRecord> identified = new List<EvidenceRecord>();
            for (int index = 0; index < identifiedEvidenceIds.Count; index++)
            {
                EvidenceRecord evidence = FindEvidence(identifiedEvidenceIds[index]);
                if (evidence != null)
                {
                    identified.Add(evidence);
                }
            }

            return identified;
        }

        public bool ContainsResult(EDNAResultData candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            for (int index = 0; index < allResults.Count; index++)
            {
                EDNAResultData existing = allResults[index];
                bool sameSample = !string.IsNullOrEmpty(candidate.sampleId)
                    && string.Equals(existing.sampleId, candidate.sampleId, StringComparison.Ordinal);
                bool sameRequest = !string.IsNullOrEmpty(candidate.requestId)
                    && string.Equals(existing.requestId, candidate.requestId, StringComparison.Ordinal);

                if (sameSample || sameRequest)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildClassificationKey(string evidenceId, AnomalyClaimType claimType)
        {
            return $"{evidenceId}::{claimType}";
        }
    }
}
