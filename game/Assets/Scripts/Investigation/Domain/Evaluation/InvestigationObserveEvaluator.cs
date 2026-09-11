using System;

namespace EDNA.Investigation.Domain
{
    public static class InvestigationObserveEvaluator
    {
        public static bool IsInitialFinding(InvestigationObservationDefinition observation)
        {
            return observation != null
                && observation.UnlockStage == EvidenceUnlockStage.Observe
                && observation.Source != ObservationSource.Methodology;
        }

        public static int CountFindings(InvestigationCaseDefinition definition, InvestigationState state)
        {
            int count = 0;
            for (int index = 0; index < definition.Observations.Count; index++)
            {
                InvestigationObservationDefinition observation = definition.Observations[index];
                if (IsInitialFinding(observation) && state.HasDiscoveredObservation(observation.EvidenceId)) count++;
            }
            return count;
        }

        public static int RequiredCount(InvestigationCaseDefinition definition)
        {
            int count = 0;
            for (int index = 0; index < definition.Observations.Count; index++)
                if (IsInitialFinding(definition.Observations[index])) count++;
            return Math.Max(count, definition.MinimumObserveDiscoveries);
        }

        public static bool IsComplete(InvestigationCaseDefinition definition, InvestigationState state)
        {
            return state != null && CountFindings(definition, state) >= RequiredCount(definition);
        }
    }
}
