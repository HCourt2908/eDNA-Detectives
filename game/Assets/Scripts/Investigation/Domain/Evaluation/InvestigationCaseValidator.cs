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
            if (caseDefinition.Species.Count < 2) errors.Add("The investigation requires at least two survey species.");
            if (caseDefinition.Threats.Count < 2) errors.Add("The investigation requires at least two threats to compare.");

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

            if (caseDefinition.SpeciesCatalog.Count != 20)
                errors.Add("The shared species catalog must contain exactly 20 species.");
            var normalizedIdentifiers = new Dictionary<string, string>(StringComparer.Ordinal);
            HashSet<string> catalogSpeciesIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> canonicalSpeciesIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> catalogIdentifiers = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.SpeciesCatalog.Count; index++)
            {
                InvestigationSpeciesDefinition catalogSpecies = caseDefinition.SpeciesCatalog[index];
                if (catalogSpecies == null || string.IsNullOrWhiteSpace(catalogSpecies.SpeciesId))
                {
                    errors.Add($"Species catalog entry {index} is missing an ID.");
                    continue;
                }
                if (!catalogSpeciesIds.Add(catalogSpecies.SpeciesId))
                    errors.Add($"Duplicate species catalog ID: {catalogSpecies.SpeciesId}.");
                if (!catalogIdentifiers.Add(catalogSpecies.SpeciesId))
                    errors.Add($"Species catalog identifier is ambiguous: {catalogSpecies.SpeciesId}.");
                if (string.IsNullOrWhiteSpace(catalogSpecies.CanonicalSpeciesId)
                    || !canonicalSpeciesIds.Add(catalogSpecies.CanonicalSpeciesId))
                {
                    errors.Add($"Species catalog entry {catalogSpecies.SpeciesId} has a missing or duplicate canonical ID.");
                }
                else if (!string.Equals(catalogSpecies.SpeciesId, catalogSpecies.CanonicalSpeciesId, StringComparison.Ordinal)
                    && !catalogIdentifiers.Add(catalogSpecies.CanonicalSpeciesId))
                {
                    errors.Add($"Species catalog identifier is ambiguous: {catalogSpecies.CanonicalSpeciesId}.");
                }
                for (int aliasIndex = 0; aliasIndex < catalogSpecies.Aliases.Count; aliasIndex++)
                {
                    string alias = catalogSpecies.Aliases[aliasIndex];
                    if (string.IsNullOrWhiteSpace(alias) || !catalogIdentifiers.Add(alias))
                        errors.Add($"Species catalog alias is missing or ambiguous for {catalogSpecies.SpeciesId}: {alias}.");
                }
                var identifiers = new List<string>(catalogSpecies.Aliases)
                {
                    catalogSpecies.SpeciesId, catalogSpecies.CanonicalSpeciesId,
                    catalogSpecies.DisplayName, catalogSpecies.ScientificName
                };
                foreach (string identifier in identifiers)
                {
                    string normalized = InvestigationSpeciesDefinition.NormalizeIdentifier(identifier);
                    if (normalized.Length == 0) continue;
                    if (normalizedIdentifiers.TryGetValue(normalized, out string owner)
                        && owner != catalogSpecies.SpeciesId)
                        errors.Add($"Species catalog name is ambiguous: {identifier} ({owner}/{catalogSpecies.SpeciesId}).");
                    else normalizedIdentifiers[normalized] = catalogSpecies.SpeciesId;
                }
                if (string.IsNullOrWhiteSpace(catalogSpecies.DisplayName))
                    errors.Add($"Species catalog entry {catalogSpecies.SpeciesId} is missing a display name.");
                if (string.IsNullOrWhiteSpace(catalogSpecies.ScientificName))
                    errors.Add($"Species catalog entry {catalogSpecies.SpeciesId} is missing a scientific name.");
                if (catalogSpecies.PreferredDepths.Count == 0)
                    errors.Add($"Species catalog entry {catalogSpecies.SpeciesId} is missing its preferred depth range.");
                else if (!ContainsDepth(catalogSpecies.PreferredDepths, catalogSpecies.MapDepthBand))
                    errors.Add($"Species catalog entry {catalogSpecies.SpeciesId} map depth is outside its preferred depth range.");
            }
            for (int index = 0; index < caseDefinition.SpeciesCatalog.Count; index++)
            {
                InvestigationSpeciesDefinition catalogSpecies = caseDefinition.SpeciesCatalog[index];
                if (catalogSpecies == null) continue;
                ValidateSpeciesReferences(catalogSpecies, catalogSpecies.DietSpeciesIds, "diet", canonicalSpeciesIds, errors);
                ValidateSpeciesReferences(catalogSpecies, catalogSpecies.PredatorSpeciesIds, "predator", canonicalSpeciesIds, errors);
            }

            HashSet<string> presentationSpeciesIds = new HashSet<string>(StringComparer.Ordinal);
            ValidatePresentationSpeciesList(
                "food-web chain",
                caseDefinition.FoodWebChainSpeciesIds,
                catalogSpeciesIds,
                presentationSpeciesIds,
                2,
                errors);
            ValidatePresentationSpeciesList(
                "benthic indicators",
                caseDefinition.BenthicIndicatorSpeciesIds,
                speciesIds,
                presentationSpeciesIds,
                0,
                errors);
            if (caseDefinition.MaximumSurveySpecies < speciesIds.Count)
                errors.Add("maximumSurveySpecies cannot be lower than the active case-species count.");
            if (string.IsNullOrWhiteSpace(caseDefinition.SimulationFoodWebId))
                errors.Add("The case requires a simulation food-web network ID.");
            ValidateFoodWebEdges(caseDefinition, errors);
            // Survey species and model species need not be identical. A model
            // can include an unmeasured organism without inventing a finding.
            foreach (var threat in caseDefinition.Threats)
                if (threat != null)
                    foreach (string id in threat.DisplaySpeciesIds) presentationSpeciesIds.Add(id);
            foreach (string speciesId in speciesIds)
            {
                if (!presentationSpeciesIds.Contains(speciesId))
                    errors.Add($"Active species {speciesId} is not assigned to the food-web chain or benthic indicators.");
            }
            HashSet<string> followUpSpeciesIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.FollowUpLockedSpeciesIds.Count; index++)
            {
                string speciesId = caseDefinition.FollowUpLockedSpeciesIds[index];
                if (!followUpSpeciesIds.Add(speciesId))
                    errors.Add($"Follow-up locked species is duplicated: {speciesId}.");
                if (!ContainsOrdinal(caseDefinition.BenthicIndicatorSpeciesIds, speciesId))
                    errors.Add($"Follow-up locked species {speciesId} must be a benthic indicator.");
            }

            foreach (var activeSpecies in caseDefinition.Species)
                if (activeSpecies != null && caseDefinition.SimulationFoodWebId == "reference_main"
                    && caseDefinition.FindCatalogSpecies(activeSpecies.CanonicalSpeciesId) != activeSpecies)
                    errors.Add($"Active species {activeSpecies.SpeciesId} is outside the shared catalog.");
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
                var effective = new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, threat.ThreatId);
                if (effective.Predictions.Count < caseDefinition.Species.Count)
                    errors.Add($"Threat {threat.ThreatId} does not resolve the complete prediction matrix.");
                var predictedIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var prediction in effective.Predictions)
                    if (!predictedIds.Add(prediction.SpeciesId) || caseDefinition.FindSpecies(prediction.SpeciesId) == null)
                        errors.Add($"Threat {threat.ThreatId} contains a duplicate or unknown prediction: {prediction.SpeciesId}.");
                var displayIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (string id in threat.DisplaySpeciesIds)
                    if (!displayIds.Add(id) || !predictedIds.Contains(id))
                        errors.Add($"Threat {threat.ThreatId} displays a duplicate species or one without a prediction: {id}.");
                foreach (var link in threat.FoodSupplyLinks)
                    if (link == null || caseDefinition.FindSpecies(link.SourceSpeciesId) == null
                        || caseDefinition.FindSpecies(link.ConsumerSpeciesId) == null
                        || !Enum.IsDefined(typeof(ScenarioFoodLinkKind), link.Kind))
                        errors.Add($"Threat {threat.ThreatId} has an invalid food-supply link.");
                foreach (string speciesId in speciesIds)
                {
                    if (effective.FindPrediction(speciesId) == null)
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
                    if (!hasDecisiveNotEnoughEvidence && effective.FindPrediction(speciesId)?.PredictedState != PredictionState.Unknown)
                        errors.Add($"Comparison {threat.ThreatId}/{speciesId} must include at least one option where Not enough evidence is incorrect.");
                    if (!hasProgressingResolution && effective.FindPrediction(speciesId)?.PredictedState != PredictionState.Unknown)
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

            var reviewIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in caseDefinition.RequiredModelReviewIds)
            {
                if (!threatIds.Contains(id) || !reviewIds.Add(id)) errors.Add($"Required model review is unknown or duplicated: {id}.");
                if (caseDefinition.FindThreat(id)?.OptionalExploration == true) errors.Add($"Optional model {id} cannot require a review.");
            }
            var supported = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in caseDefinition.SupportedModelThreatIds)
                if (!threatIds.Contains(id) || !supported.Add(id))
                    errors.Add($"Supported model is unknown or duplicated: {id}.");

            if (caseDefinition.SupportedModelThreatIds.Count > 0)
            {
                foreach (string id in threatIds)
                {
                    bool matchesEveryFinding = true;
                    int comparableFindings = 0;
                    var model = new EcosystemSimulatorEvaluator().Evaluate(caseDefinition, id);
                    foreach (var finding in caseDefinition.Observations)
                    {
                        if (!InvestigationObserveEvaluator.IsInitialFinding(finding) || string.IsNullOrEmpty(finding.RelatedSpeciesId)) continue;
                        if (model.FindPrediction(finding.RelatedSpeciesId)?.PredictedState == PredictionState.Unknown) continue;
                        comparableFindings++;
                        var match = caseDefinition.FindComparisonRule(id, finding.RelatedSpeciesId)
                            ?.FindOption(finding.EvidenceId)?.FindResolution(ComparisonJudgement.Match);
                        if (match == null || match.Outcome == ComparisonEvaluationOutcome.Incorrect) matchesEveryFinding = false;
                    }
                    if (supported.Contains(id) != (matchesEveryFinding && comparableFindings >= 2))
                        errors.Add($"Supported model list does not reflect the complete survey comparison for {id}.");
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
                if (caseDefinition.FindThreat(requiredThreatId)?.OptionalExploration == true)
                    errors.Add($"Optional model {requiredThreatId} cannot require a comparison.");
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
            if (caseDefinition.RequiredComparedThreatIds.Count < 2) errors.Add("At least two threat models must be required for comparison.");
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
                        if (caseDefinition.FindThreat(objective.ThreatId)?.OptionalExploration == true)
                            errors.Add($"Optional model {objective.ThreatId} cannot have a required objective.");
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
                if (followUpObjectiveCount == 0 && caseDefinition.BenthicIndicatorSpeciesIds.Count > 0)
                    errors.Add("The case requires at least one benthic discriminator objective to compare the fishing models.");
            }
            else
            {
                errors.Add("Investigation requires data-driven investigation objectives.");
            }

            if (caseDefinition.MinimumObserveDiscoveries < caseDefinition.Species.Count)
                errors.Add("The case must require all survey findings before Simulate.");

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

        private static void ValidateSpeciesReferences(
            InvestigationSpeciesDefinition species,
            IReadOnlyList<string> references,
            string relationship,
            HashSet<string> canonicalSpeciesIds,
            List<string> errors)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < references.Count; index++)
            {
                string referencedId = references[index];
                if (!seen.Add(referencedId))
                    errors.Add($"Species {species.SpeciesId} repeats {relationship} reference {referencedId}.");
                if (!canonicalSpeciesIds.Contains(referencedId))
                    errors.Add($"Species {species.SpeciesId} references unknown canonical {relationship} species {referencedId}.");
            }
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], expected, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static bool ContainsDepth(IReadOnlyList<EDNA.Core.DepthBand> values, EDNA.Core.DepthBand expected)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index] == expected) return true;
            }
            return false;
        }

        private static void ValidatePresentationSpeciesList(
            string label,
            IReadOnlyList<string> ids,
            HashSet<string> activeSpeciesIds,
            HashSet<string> assignedIds,
            int minimumCount,
            List<string> errors)
        {
            if (ids.Count < minimumCount) errors.Add($"The {label} requires at least {minimumCount} species.");
            HashSet<string> localIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < ids.Count; index++)
            {
                string speciesId = ids[index];
                if (!localIds.Add(speciesId)) errors.Add($"The {label} repeats species {speciesId}.");
                if (!activeSpeciesIds.Contains(speciesId)) errors.Add($"The {label} references inactive species {speciesId}.");
                if (!assignedIds.Add(speciesId)) errors.Add($"Species {speciesId} appears in more than one simulation presentation group.");
            }
        }

        private static void ValidateFoodWebEdges(
            InvestigationCaseDefinition caseDefinition,
            List<string> errors)
        {
            if (caseDefinition.FoodWebEdges.Count == 0)
            {
                errors.Add("The shared species catalog requires authored food-web edges.");
                return;
            }
            var edgeKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.FoodWebEdges.Count; index++)
            {
                FoodWebEdgeDefinition edge = caseDefinition.FoodWebEdges[index];
                if (edge == null || string.IsNullOrWhiteSpace(edge.EdgeId))
                {
                    errors.Add($"Food-web edge {index} is missing an ID.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(edge.NetworkId)) errors.Add($"Food-web edge {edge.EdgeId} is missing its network ID.");
                if (caseDefinition.FindSpecies(edge.PredatorSpeciesId) == null)
                    errors.Add($"Food-web edge {edge.EdgeId} references unknown predator {edge.PredatorSpeciesId}.");
                if (caseDefinition.FindSpecies(edge.PreySpeciesId) == null)
                    errors.Add($"Food-web edge {edge.EdgeId} references unknown prey {edge.PreySpeciesId}.");
                string key = $"{edge.NetworkId}|{edge.PredatorSpeciesId}|{edge.PreySpeciesId}";
                if (!edgeKeys.Add(key)) errors.Add($"Food-web edge is duplicated within network {edge.NetworkId}: {edge.PredatorSpeciesId}/{edge.PreySpeciesId}.");
            }

            for (int index = 0; index < caseDefinition.FoodWebChainSpeciesIds.Count - 1; index++)
            {
                InvestigationSpeciesDefinition predator = caseDefinition.FindSpecies(caseDefinition.FoodWebChainSpeciesIds[index]);
                InvestigationSpeciesDefinition prey = caseDefinition.FindSpecies(caseDefinition.FoodWebChainSpeciesIds[index + 1]);
                if (predator == null || prey == null) continue;
                string key = $"{caseDefinition.SimulationFoodWebId}|{predator.CanonicalSpeciesId}|{prey.CanonicalSpeciesId}";
                if (!edgeKeys.Contains(key))
                    errors.Add($"Simulation food-web network {caseDefinition.SimulationFoodWebId} is missing {predator.CanonicalSpeciesId} → {prey.CanonicalSpeciesId}.");
            }
        }
    }
}
