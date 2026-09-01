using System;
using System.Collections.Generic;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationCaseValidator
    {
        public List<string> Validate(InvestigationCaseDefinition caseDefinition)
        {
            List<string> errors = new List<string>();
            if (caseDefinition == null)
            {
                errors.Add("Case definition is missing.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(caseDefinition.CaseId)) errors.Add("Case ID is missing.");
            if (caseDefinition.Species.Count != 5) errors.Add("The investigation vertical slice requires exactly five species.");
            if (caseDefinition.Threats.Count != 4) errors.Add("The investigation vertical slice requires exactly four threats.");

            HashSet<string> speciesIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.Species.Count; index++)
            {
                InvestigationSpeciesDefinition species = caseDefinition.Species[index];
                if (species == null || string.IsNullOrWhiteSpace(species.SpeciesId))
                    errors.Add($"Species {index} is missing an ID.");
                else if (!speciesIds.Add(species.SpeciesId))
                    errors.Add($"Duplicate species ID: {species.SpeciesId}.");
                if (species != null && species.PreferredDepths.Count > 0)
                {
                    bool supportsMapDepth = false;
                    for (int depthIndex = 0; depthIndex < species.PreferredDepths.Count; depthIndex++)
                    {
                        if (species.PreferredDepths[depthIndex] == species.MapDepthBand)
                        {
                            supportsMapDepth = true;
                            break;
                        }
                    }
                    if (!supportsMapDepth)
                        errors.Add($"Species {species.SpeciesId} map depth {species.MapDepthBand} is outside its preferred depth range.");
                }
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

            for (int ruleIndex = 0; ruleIndex < caseDefinition.ComparisonRules.Count; ruleIndex++)
            {
                PredictionComparisonRuleDefinition rule = caseDefinition.ComparisonRules[ruleIndex];
                if (rule == null || rule.TargetKind == PredictionTargetKind.Species) continue;
                if (!threatIds.Contains(rule.ThreatId))
                    errors.Add($"Non-species comparison {ruleIndex} references unknown threat {rule.ThreatId}.");
                if (string.IsNullOrWhiteSpace(rule.TargetId))
                    errors.Add($"Non-species comparison {rule.ThreatId} is missing a target ID.");
                if (rule.ObservationOptions.Count < 1 || rule.ObservationOptions.Count > 4)
                    errors.Add($"Comparison {rule.ThreatId}/{rule.TargetKind}/{rule.TargetId} must expose 1–4 candidate observations.");
                for (int optionIndex = 0; optionIndex < rule.ObservationOptions.Count; optionIndex++)
                {
                    ObservationComparisonOptionDefinition option = rule.ObservationOptions[optionIndex];
                    if (option == null || caseDefinition.FindObservation(option.EvidenceId) == null)
                        errors.Add($"Comparison {rule.ThreatId}/{rule.TargetKind}/{rule.TargetId} references an unknown observation.");
                    else if (option.Resolutions.Count != 3)
                        errors.Add($"Comparison {rule.ThreatId}/{rule.TargetKind}/{rule.TargetId}/{option.EvidenceId} must resolve all three judgements.");
                }
            }

            HashSet<string> evidenceIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.Observations[index];
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
                InvestigationRequiredComparisonSpeciesDefinition requiredSpecies = caseDefinition.FindRequiredComparisonSpecies(requiredThreatId);
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

            if (caseDefinition.InvestigationObjectives.Count > 0)
            {
                HashSet<string> objectiveIds = new HashSet<string>(StringComparer.Ordinal);
                int provisionalObjectiveCount = 0;
                int followUpObjectiveCount = 0;
                for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
                {
                    InvestigationObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                    if (objective == null || string.IsNullOrWhiteSpace(objective.ObjectiveId))
                    {
                        errors.Add($"Investigation objective {index} is missing an ID.");
                        continue;
                    }
                    if (!objectiveIds.Add(objective.ObjectiveId))
                        errors.Add($"Duplicate investigation objective ID: {objective.ObjectiveId}.");
                    if (objective.Required)
                    {
                        if (objective.ProgressRole == ComparisonProgressRole.BenthicDiscriminator) followUpObjectiveCount++;
                        else provisionalObjectiveCount++;
                    }
                    if (string.IsNullOrWhiteSpace(objective.QuestionId) || string.IsNullOrWhiteSpace(objective.QuestionPrompt))
                        errors.Add($"Objective {objective.ObjectiveId} is missing its case-question label.");
                    if (!threatIds.Contains(objective.ThreatId))
                        errors.Add($"Objective {objective.ObjectiveId} references unknown threat {objective.ThreatId}.");
                    InvestigationObservationDefinition evidence = caseDefinition.FindObservation(objective.RequiredEvidenceId);
                    if (evidence == null)
                    {
                        errors.Add($"Objective {objective.ObjectiveId} references unknown evidence {objective.RequiredEvidenceId}.");
                        continue;
                    }
                    if (evidence.UnlockStage == EvidenceUnlockStage.AfterProvisional)
                        errors.Add($"Objective {objective.ObjectiveId} depends on evidence that unlocks after provisional report.");
                    if (evidence.UnlockStage == EvidenceUnlockStage.OnThreatRun
                        && !string.Equals(evidence.UnlockThreatId, objective.ThreatId, StringComparison.Ordinal))
                    {
                        errors.Add($"Objective {objective.ObjectiveId} depends on evidence unlocked by a different threat.");
                    }
                    PredictionComparisonRuleDefinition rule = caseDefinition.FindComparisonRule(
                        objective.ThreatId,
                        objective.TargetKind,
                        objective.TargetId);
                    if (rule == null)
                    {
                        errors.Add($"Objective {objective.ObjectiveId} has no comparison rule.");
                        continue;
                    }
                    if (rule.ProgressRole != objective.ProgressRole)
                        errors.Add($"Objective {objective.ObjectiveId} progress role does not match its comparison rule.");
                    ObservationComparisonOptionDefinition option = rule.FindOption(objective.RequiredEvidenceId);
                    JudgementResolutionDefinition resolution = option?.FindResolution(objective.RequiredJudgement);
                    if (resolution == null || resolution.Outcome == ComparisonEvaluationOutcome.Incorrect
                        || objective.RequiredJudgement == ComparisonJudgement.NotEnoughEvidence)
                    {
                        errors.Add($"Objective {objective.ObjectiveId} does not resolve to an accepted decisive judgement.");
                    }
                }
                if (provisionalObjectiveCount == 0)
                    errors.Add("The case requires at least one objective before the provisional explanation.");
                if (followUpObjectiveCount == 0)
                    errors.Add("The case requires at least one benthic discriminator objective after the ROV follow-up.");
            }
            else
            {
                errors.Add("Investigation requires data-driven investigation objectives.");
            }

            if (caseDefinition.MinimumObserveDiscoveries < 5)
                errors.Add("The Long-line vertical slice must require all five Observe findings before Simulate.");

            int minimumCategoryTotal = 0;
            HashSet<EvidenceCategory> requiredCategories = new HashSet<EvidenceCategory>();
            for (int index = 0; index < caseDefinition.EvidenceCategoryRequirements.Count; index++)
            {
                InvestigationEvidenceCategoryRequirement requirement = caseDefinition.EvidenceCategoryRequirements[index];
                if (requirement == null) continue;
                if (requirement.Category == EvidenceCategory.General)
                    errors.Add("Report evidence requirements cannot use the General category.");
                if (!requiredCategories.Add(requirement.Category))
                    errors.Add($"Duplicate report evidence category requirement: {requirement.Category}.");
                int available = 0;
                for (int evidenceIndex = 0; evidenceIndex < caseDefinition.Observations.Count; evidenceIndex++)
                {
                    InvestigationObservationDefinition observation = caseDefinition.Observations[evidenceIndex];
                    if (observation != null && observation.Category == requirement.Category) available++;
                }
                if (available < requirement.MinimumCount)
                    errors.Add($"Report category {requirement.Category} requires {requirement.MinimumCount} but only {available} observations exist.");
                minimumCategoryTotal += requirement.MinimumCount;
            }
            if (caseDefinition.EvidenceCategoryRequirements.Count == 0)
                errors.Add("The final report requires evidence category requirements.");
            if (caseDefinition.MinimumReportEvidence < minimumCategoryTotal)
                errors.Add("minimumReportEvidence cannot be lower than the sum of category minimums.");

            for (int index = 0; index < caseDefinition.ConfirmationEvidenceIds.Count; index++)
            {
                InvestigationObservationDefinition confirmation = caseDefinition.FindObservation(caseDefinition.ConfirmationEvidenceIds[index]);
                if (confirmation == null || confirmation.UnlockStage != EvidenceUnlockStage.AfterProvisional)
                    errors.Add($"Confirmation evidence is missing or unlocks at the wrong stage: {caseDefinition.ConfirmationEvidenceIds[index]}.");
            }

            bool hasAlwaysLimitation = false;
            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.Observations[index];
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
                errors.Add("Investigation stores one selected limitation, so minimumReportLimitations must be exactly 1.");

            HashSet<string> reasoningIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.ReasoningOptions.Count; index++)
            {
                InvestigationReasoningDefinition reasoning = caseDefinition.ReasoningOptions[index];
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
