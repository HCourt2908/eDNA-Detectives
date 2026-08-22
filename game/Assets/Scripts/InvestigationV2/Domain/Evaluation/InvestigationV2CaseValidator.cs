using System;
using System.Collections.Generic;

namespace EDNA.Investigation.V2.Domain
{
    public sealed class InvestigationV2CaseValidator
    {
        public List<string> Validate(InvestigationV2CaseDefinition caseDefinition)
        {
            List<string> errors = new List<string>();
            if (caseDefinition == null)
            {
                errors.Add("Case definition is missing.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(caseDefinition.CaseId)) errors.Add("Case ID is missing.");
            if (caseDefinition.Species.Count != 5) errors.Add("The V2 vertical slice requires exactly five species.");
            if (caseDefinition.Threats.Count != 4) errors.Add("The V2 vertical slice requires exactly four threats.");

            HashSet<string> speciesIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.Species.Count; index++)
            {
                InvestigationV2SpeciesDefinition species = caseDefinition.Species[index];
                if (species == null || string.IsNullOrWhiteSpace(species.SpeciesId))
                    errors.Add($"Species {index} is missing an ID.");
                else if (!speciesIds.Add(species.SpeciesId))
                    errors.Add($"Duplicate species ID: {species.SpeciesId}.");
            }

            HashSet<string> threatIds = new HashSet<string>(StringComparer.Ordinal);
            for (int threatIndex = 0; threatIndex < caseDefinition.Threats.Count; threatIndex++)
            {
                ThreatSimulationDefinition threat = caseDefinition.Threats[threatIndex];
                if (threat == null || string.IsNullOrWhiteSpace(threat.ThreatId))
                {
                    errors.Add($"Threat {threatIndex} is missing an ID.");
                    continue;
                }
                if (!threatIds.Add(threat.ThreatId)) errors.Add($"Duplicate threat ID: {threat.ThreatId}.");
                if (threat.SpeciesPredictions.Count != caseDefinition.Species.Count)
                    errors.Add($"Threat {threat.ThreatId} does not define the complete Threat × Species matrix.");
                foreach (string speciesId in speciesIds)
                {
                    if (threat.FindPrediction(speciesId) == null)
                        errors.Add($"Threat {threat.ThreatId} is missing a prediction for {speciesId}.");
                    PredictionComparisonRuleDefinition rule = caseDefinition.FindComparisonRule(threat.ThreatId, speciesId);
                    if (rule == null)
                    {
                        errors.Add($"Threat {threat.ThreatId} is missing a comparison rule for {speciesId}.");
                        continue;
                    }
                    if (rule.ObservationOptions.Count < 2 || rule.ObservationOptions.Count > 4)
                        errors.Add($"Comparison {threat.ThreatId}/{speciesId} must expose 2–4 candidate observations.");
                    bool hasDecisiveNotEnoughEvidence = false;
                    bool hasProgressingResolution = false;
                    for (int optionIndex = 0; optionIndex < rule.ObservationOptions.Count; optionIndex++)
                    {
                        ObservationComparisonOptionDefinition option = rule.ObservationOptions[optionIndex];
                        if (option == null || caseDefinition.FindObservation(option.EvidenceId) == null)
                        {
                            errors.Add($"Comparison {threat.ThreatId}/{speciesId} references an unknown observation.");
                            continue;
                        }
                        bool hasAccepted = false;
                        HashSet<ComparisonJudgement> judgements = new HashSet<ComparisonJudgement>();
                        for (int resolutionIndex = 0; resolutionIndex < option.Resolutions.Count; resolutionIndex++)
                        {
                            JudgementResolutionDefinition resolution = option.Resolutions[resolutionIndex];
                            if (resolution == null) continue;
                            if (!judgements.Add(resolution.Judgement))
                                errors.Add($"Comparison {threat.ThreatId}/{speciesId}/{option.EvidenceId} repeats judgement {resolution.Judgement}.");
                            if (resolution.Outcome != ComparisonEvaluationOutcome.Incorrect) hasAccepted = true;
                            if (resolution.Outcome != ComparisonEvaluationOutcome.Incorrect
                                && resolution.Judgement != ComparisonJudgement.NotEnoughEvidence)
                            {
                                hasProgressingResolution = true;
                            }
                            if (resolution.Judgement == ComparisonJudgement.NotEnoughEvidence
                                && resolution.Outcome == ComparisonEvaluationOutcome.Incorrect)
                            {
                                hasDecisiveNotEnoughEvidence = true;
                            }
                        }
                        if (judgements.Count != 3)
                            errors.Add($"Comparison {threat.ThreatId}/{speciesId}/{option.EvidenceId} must resolve all three judgements exactly once.");
                        if (!hasAccepted) errors.Add($"Comparison {threat.ThreatId}/{speciesId}/{option.EvidenceId} has no acceptable judgement.");
                    }
                    if (!hasDecisiveNotEnoughEvidence)
                        errors.Add($"Comparison {threat.ThreatId}/{speciesId} must include at least one option where Not enough evidence is incorrect.");
                    if (!hasProgressingResolution)
                        errors.Add($"Comparison {threat.ThreatId}/{speciesId} must include a Match or Mismatch resolution that advances progress.");
                }
            }

            HashSet<string> evidenceIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.Observations[index];
                if (observation == null || string.IsNullOrWhiteSpace(observation.EvidenceId))
                    errors.Add($"Observation {index} is missing an ID.");
                else if (!evidenceIds.Add(observation.EvidenceId))
                    errors.Add($"Duplicate observation ID: {observation.EvidenceId}.");
            }

            for (int index = 0; index < caseDefinition.RequiredComparedThreatIds.Count; index++)
            {
                string requiredThreatId = caseDefinition.RequiredComparedThreatIds[index];
                if (!threatIds.Contains(requiredThreatId))
                    errors.Add($"Required comparison threat is unknown: {requiredThreatId}.");
                InvestigationV2RequiredComparisonSpeciesDefinition requiredSpecies = caseDefinition.FindRequiredComparisonSpecies(requiredThreatId);
                if (requiredSpecies == null || requiredSpecies.RequiredComparisonSpeciesIds.Count == 0)
                {
                    errors.Add($"Required comparison threat {requiredThreatId} must name at least one required species prediction.");
                    continue;
                }
                if (requiredSpecies.RequiredComparisonSpeciesIds.Count > caseDefinition.RequiredComparisonsPerThreat)
                    errors.Add($"Required species comparisons for {requiredThreatId} exceed the per-threat comparison count.");
                HashSet<string> requiredSpeciesIds = new HashSet<string>(StringComparer.Ordinal);
                for (int speciesIndex = 0; speciesIndex < requiredSpecies.RequiredComparisonSpeciesIds.Count; speciesIndex++)
                {
                    string requiredSpeciesId = requiredSpecies.RequiredComparisonSpeciesIds[speciesIndex];
                    if (!requiredSpeciesIds.Add(requiredSpeciesId))
                        errors.Add($"Required comparison species is duplicated for {requiredThreatId}: {requiredSpeciesId}.");
                    if (!speciesIds.Contains(requiredSpeciesId))
                        errors.Add($"Required comparison species is unknown for {requiredThreatId}: {requiredSpeciesId}.");
                    if (caseDefinition.FindComparisonRule(requiredThreatId, requiredSpeciesId) == null)
                        errors.Add($"Required comparison {requiredThreatId}/{requiredSpeciesId} has no comparison rule.");
                }
            }
            if (!threatIds.Contains(caseDefinition.CorrectThreatId)) errors.Add("Correct threat ID is not available.");
            if (caseDefinition.RequiredComparedThreatIds.Count < 2) errors.Add("At least two overlapping threats must be required for comparison.");
            if (caseDefinition.MinimumCompletedComparisons < caseDefinition.RequiredComparisonsPerThreat * caseDefinition.RequiredComparedThreatIds.Count)
                errors.Add("Minimum completed comparisons cannot be lower than the required per-threat total.");

            for (int index = 0; index < caseDefinition.ConfirmationEvidenceIds.Count; index++)
            {
                InvestigationV2ObservationDefinition confirmation = caseDefinition.FindObservation(caseDefinition.ConfirmationEvidenceIds[index]);
                if (confirmation == null || confirmation.UnlockStage != EvidenceUnlockStage.AfterProvisional)
                    errors.Add($"Confirmation evidence is missing or unlocks at the wrong stage: {caseDefinition.ConfirmationEvidenceIds[index]}.");
            }

            bool hasAlwaysLimitation = false;
            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.Observations[index];
                if (observation != null
                    && observation.Source == ObservationSource.Methodology
                    && observation.UnlockStage == EvidenceUnlockStage.Always)
                {
                    hasAlwaysLimitation = true;
                    break;
                }
            }
            if (!hasAlwaysLimitation || caseDefinition.Limitations.Count == 0)
                errors.Add("The case requires an always-available methodological limitation.");
            if (caseDefinition.MinimumReportLimitations != 1)
                errors.Add("Investigation V2 stores one selected limitation, so minimumReportLimitations must be exactly 1.");

            HashSet<string> reasoningIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.ReasoningOptions.Count; index++)
            {
                InvestigationV2ReasoningDefinition reasoning = caseDefinition.ReasoningOptions[index];
                if (reasoning == null || string.IsNullOrWhiteSpace(reasoning.ReasoningId))
                    errors.Add($"Reasoning option {index} is missing an ID.");
                else if (!reasoningIds.Add(reasoning.ReasoningId))
                    errors.Add($"Duplicate reasoning ID: {reasoning.ReasoningId}.");
            }
            if (caseDefinition.ReasoningOptions.Count < 3)
                errors.Add("The report requires at least three authored reasoning choices.");
            if (!reasoningIds.Contains(caseDefinition.RequiredReasoningId))
                errors.Add("Required reasoning ID is not one of the authored reasoning choices.");

            return errors;
        }
    }
}
