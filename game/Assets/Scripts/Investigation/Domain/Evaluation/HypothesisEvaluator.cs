using System;

namespace EDNA.Investigation.Domain
{
    public sealed class HypothesisEvaluator
    {
        public HypothesisEvaluation Evaluate(HypothesisDefinition hypothesis, InvestigationState state)
        {
            if (hypothesis == null)
            {
                throw new ArgumentNullException(nameof(hypothesis));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            int supportingCount = 0;
            int opposingCount = 0;
            EvidenceConfidence highestSupportingConfidence = EvidenceConfidence.Low;
            bool hasContradictingEvidence = false;

            for (int assignmentIndex = 0; assignmentIndex < state.EvidenceAssignments.Count; assignmentIndex++)
            {
                EvidenceAssignmentRecord assignment = state.EvidenceAssignments[assignmentIndex];
                if (!string.Equals(assignment.HypothesisId, hypothesis.HypothesisId, StringComparison.Ordinal))
                {
                    continue;
                }

                EvidenceRecord evidence = state.FindEvidence(assignment.EvidenceId);
                if (evidence == null)
                {
                    continue;
                }

                if (assignment.AssignmentKind == EvidenceAssignmentKind.Supports)
                {
                    supportingCount++;
                    if (evidence.Confidence > highestSupportingConfidence)
                    {
                        highestSupportingConfidence = evidence.Confidence;
                    }
                }
                else
                {
                    opposingCount++;
                    if (HasAnyTag(evidence, hypothesis.ContradictingEvidenceTags))
                    {
                        hasContradictingEvidence = true;
                    }
                }
            }

            if (hasContradictingEvidence)
            {
                return new HypothesisEvaluation(
                    hypothesis.HypothesisId,
                    HypothesisStatus.Contradicted,
                    supportingCount,
                    opposingCount,
                    "A key opposing observation contradicts this hypothesis.");
            }

            bool hasAllRequiredTags = HasAllRequiredTags(hypothesis, state);
            bool meetsCount = supportingCount >= hypothesis.MinimumSupportingEvidence;
            bool meetsConfidence = highestSupportingConfidence >= hypothesis.MinimumConfidence;
            if (hasAllRequiredTags && meetsCount && meetsConfidence)
            {
                return new HypothesisEvaluation(
                    hypothesis.HypothesisId,
                    HypothesisStatus.Supported,
                    supportingCount,
                    opposingCount,
                    "The assigned observations meet the required evidence rules.");
            }

            if (supportingCount > 0 || opposingCount > 0)
            {
                return new HypothesisEvaluation(
                    hypothesis.HypothesisId,
                    HypothesisStatus.Plausible,
                    supportingCount,
                    opposingCount,
                    "Some evidence is assigned, but the hypothesis is not fully supported yet.");
            }

            return new HypothesisEvaluation(
                hypothesis.HypothesisId,
                HypothesisStatus.Unexplored,
                0,
                0,
                "No evidence has been assigned to this hypothesis.");
        }

        private static bool HasAllRequiredTags(HypothesisDefinition hypothesis, InvestigationState state)
        {
            for (int tagIndex = 0; tagIndex < hypothesis.RequiredEvidenceTags.Count; tagIndex++)
            {
                string requiredTag = hypothesis.RequiredEvidenceTags[tagIndex];
                bool found = false;

                for (int assignmentIndex = 0; assignmentIndex < state.EvidenceAssignments.Count; assignmentIndex++)
                {
                    EvidenceAssignmentRecord assignment = state.EvidenceAssignments[assignmentIndex];
                    if (assignment.AssignmentKind != EvidenceAssignmentKind.Supports
                        || !string.Equals(assignment.HypothesisId, hypothesis.HypothesisId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    EvidenceRecord evidence = state.FindEvidence(assignment.EvidenceId);
                    if (evidence != null && evidence.HasTag(requiredTag))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasAnyTag(EvidenceRecord evidence, System.Collections.Generic.IReadOnlyList<string> tags)
        {
            for (int index = 0; index < tags.Count; index++)
            {
                if (evidence.HasTag(tags[index]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
