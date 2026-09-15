using System;
using System.Collections.Generic;
using EDNA.Core;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [Serializable]
    public sealed class InvestigationLimitationDefinition
    {
        [SerializeField] private string limitationId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(1, 3)] private string explanation = string.Empty;

        public string LimitationId => limitationId;
        public string DisplayName => displayName;
        public string Explanation => explanation;
    }

    [Serializable]
    public sealed class InvestigationReasoningDefinition
    {
        [SerializeField] private string reasoningId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(1, 3)] private string explanation = string.Empty;

        public string ReasoningId => reasoningId;
        public string DisplayName => displayName;
        public string Explanation => explanation;
    }

    [Serializable]
    public sealed class InvestigationRequiredComparisonSpeciesDefinition
    {
        [SerializeField] private string threatId = string.Empty;
        [SerializeField] private List<string> requiredComparisonSpeciesIds = new List<string>();

        public string ThreatId => threatId;
        public IReadOnlyList<string> RequiredComparisonSpeciesIds => requiredComparisonSpeciesIds;
    }

    [Serializable]
    public sealed class InvestigationObjectiveDefinition
    {
        [SerializeField] private string objectiveId = string.Empty;
        [SerializeField] private string questionId = string.Empty;
        [SerializeField] private string questionPrompt = string.Empty;
        [SerializeField] private string threatId = string.Empty;
        [SerializeField] private PredictionTargetKind targetKind = PredictionTargetKind.Species;
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string requiredEvidenceId = string.Empty;
        [SerializeField] private ComparisonJudgement requiredJudgement;
        [SerializeField] private ComparisonProgressRole progressRole;
        [SerializeField] private bool required = true;

        public string ObjectiveId => objectiveId;
        public string QuestionId => questionId;
        public string QuestionPrompt => questionPrompt;
        public string ThreatId => threatId;
        public PredictionTargetKind TargetKind => targetKind;
        public string TargetId => targetId;
        public string RequiredEvidenceId => requiredEvidenceId;
        public ComparisonJudgement RequiredJudgement => requiredJudgement;
        public ComparisonProgressRole ProgressRole => progressRole;
        public bool Required => required;
    }

    [Serializable]
    public sealed class InvestigationEvidenceCategoryRequirement
    {
        [SerializeField] private EvidenceCategory category;
        [SerializeField, Min(1)] private int minimumCount = 1;

        public EvidenceCategory Category => category;
        public int MinimumCount => Mathf.Max(1, minimumCount);
    }

    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation/Case", fileName = "InvestigationCase_")]
    public sealed class InvestigationCaseDefinition : ScriptableObject
    {
        [SerializeField] private string caseId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 6)] private string briefing = string.Empty;
        [SerializeField] private InvestigationSurveyContextData surveyContext = new InvestigationSurveyContextData();
        [SerializeField] private List<InvestigationSpeciesDefinition> species = new List<InvestigationSpeciesDefinition>();
        [SerializeField] private List<InvestigationSpeciesDefinition> speciesCatalog = new List<InvestigationSpeciesDefinition>();
        [SerializeField] private List<string> foodWebChainSpeciesIds = new List<string>();
        [SerializeField] private string simulationFoodWebId = "case_simplified";
        [SerializeField] private List<FoodWebEdgeDefinition> foodWebEdges = new List<FoodWebEdgeDefinition>();
        [SerializeField] private List<string> benthicIndicatorSpeciesIds = new List<string>();
        [SerializeField] private List<string> followUpLockedSpeciesIds = new List<string>();
        [SerializeField, Range(5, 12)] private int maximumSurveySpecies = 7;
        [SerializeField] private List<InvestigationObservationDefinition> observations = new List<InvestigationObservationDefinition>();
        [SerializeField] private List<ThreatSimulationDefinition> threats = new List<ThreatSimulationDefinition>();
        [SerializeField] private List<PredictionComparisonRuleDefinition> comparisonRules = new List<PredictionComparisonRuleDefinition>();
        [SerializeField] private List<InvestigationObjectiveDefinition> investigationObjectives = new List<InvestigationObjectiveDefinition>();
        [SerializeField, Min(1)] private int minimumObserveDiscoveries = 4;
        [SerializeField] private List<string> requiredComparedThreatIds = new List<string>();
        [SerializeField] private List<InvestigationRequiredComparisonSpeciesDefinition> requiredComparisonSpecies = new List<InvestigationRequiredComparisonSpeciesDefinition>();
        [SerializeField, Min(1)] private int requiredComparisonsPerThreat = 2;
        [SerializeField, Min(1)] private int minimumCompletedComparisons = 4;
        [SerializeField] private string correctThreatId = string.Empty;
        [SerializeField] private List<string> requiredModelReviewIds = new List<string>();
        [SerializeField] private List<string> supportedModelThreatIds = new List<string>();
        [SerializeField] private List<string> confirmationEvidenceIds = new List<string>();
        [SerializeField, Min(1)] private int minimumReportEvidence = 2;
        [SerializeField] private List<InvestigationEvidenceCategoryRequirement> evidenceCategoryRequirements = new List<InvestigationEvidenceCategoryRequirement>();
        [SerializeField, Min(1)] private int minimumConfirmationEvidenceInReport = 1;
        [SerializeField, Min(1)] private int minimumReportLimitations = 1;
        [SerializeField] private string requiredReasoningId = "food_web_cascade";
        [SerializeField] private List<InvestigationReasoningDefinition> reasoningOptions = new List<InvestigationReasoningDefinition>();
        [SerializeField] private List<InvestigationLimitationDefinition> limitations = new List<InvestigationLimitationDefinition>();
        [SerializeField, TextArea(2, 5)] private string successFeedback = string.Empty;

        public string CaseId => caseId;
        public string DisplayName => displayName;
        public string Briefing => briefing;
        public InvestigationSurveyContextData SurveyContext => surveyContext;
        public IReadOnlyList<InvestigationSpeciesDefinition> Species => species;
        public IReadOnlyList<InvestigationSpeciesDefinition> SpeciesCatalog => speciesCatalog;
        public IReadOnlyList<string> FoodWebChainSpeciesIds => foodWebChainSpeciesIds;
        public string SimulationFoodWebId => simulationFoodWebId;
        public IReadOnlyList<FoodWebEdgeDefinition> FoodWebEdges => foodWebEdges;
        public IReadOnlyList<string> BenthicIndicatorSpeciesIds => benthicIndicatorSpeciesIds;
        public IReadOnlyList<string> FollowUpLockedSpeciesIds => followUpLockedSpeciesIds;
        public int MaximumSurveySpecies => Mathf.Clamp(maximumSurveySpecies, 5, 12);
        public IReadOnlyList<InvestigationObservationDefinition> Observations => observations;
        public IReadOnlyList<ThreatSimulationDefinition> Threats => threats;
        public IReadOnlyList<PredictionComparisonRuleDefinition> ComparisonRules => comparisonRules;
        public IReadOnlyList<InvestigationObjectiveDefinition> InvestigationObjectives => investigationObjectives;
        public int MinimumObserveDiscoveries => Mathf.Max(1, minimumObserveDiscoveries);
        public IReadOnlyList<string> RequiredComparedThreatIds => requiredComparedThreatIds;
        public IReadOnlyList<InvestigationRequiredComparisonSpeciesDefinition> RequiredComparisonSpecies => requiredComparisonSpecies;
        public int RequiredComparisonsPerThreat => Mathf.Max(1, requiredComparisonsPerThreat);
        public int MinimumCompletedComparisons => Mathf.Max(1, minimumCompletedComparisons);
        public string CorrectThreatId => correctThreatId;
        // Authored case priority, separate from which models fit the same pattern.
        public string PrimaryModelThreatId => correctThreatId;
        public IReadOnlyList<string> RequiredModelReviewIds => requiredModelReviewIds.Count > 0 ? requiredModelReviewIds : supportedModelThreatIds;
        public bool CanReviewModel(string id) => RequiredModelReviewIds.Count > 0
            ? (requiredModelReviewIds.Count > 0 ? requiredModelReviewIds.Contains(id) : supportedModelThreatIds.Contains(id)) : SupportsModelConclusion(id);
        public IReadOnlyList<string> SupportedModelThreatIds => supportedModelThreatIds;
        public bool HasAmbiguousModelConclusion => supportedModelThreatIds.Count > 1;
        public bool SupportsModelConclusion(string id) => supportedModelThreatIds.Count > 0
            ? supportedModelThreatIds.Contains(id) : string.Equals(id, correctThreatId, StringComparison.Ordinal);

        public IReadOnlyList<string> ConfirmationEvidenceIds => confirmationEvidenceIds;
        public int MinimumReportEvidence => Mathf.Max(1, minimumReportEvidence);
        public IReadOnlyList<InvestigationEvidenceCategoryRequirement> EvidenceCategoryRequirements => evidenceCategoryRequirements;
        public int MinimumConfirmationEvidenceInReport => Mathf.Max(1, minimumConfirmationEvidenceInReport);
        public int MinimumReportLimitations => Mathf.Max(1, minimumReportLimitations);
        public string RequiredReasoningId => requiredReasoningId;
        public IReadOnlyList<InvestigationReasoningDefinition> ReasoningOptions => reasoningOptions;
        public IReadOnlyList<InvestigationLimitationDefinition> Limitations => limitations;
        public string SuccessFeedback => successFeedback;

        public InvestigationSpeciesDefinition FindSpecies(string speciesId)
        {
            for (int index = 0; index < species.Count; index++)
            {
                InvestigationSpeciesDefinition definition = species[index];
                if (definition != null && definition.MatchesIdentifier(speciesId)) return definition;
            }
            return FindCatalogSpecies(speciesId);
        }

        // Only approved catalog species can arrive from the identification game.
        public InvestigationSpeciesDefinition FindCatalogSpecies(string speciesId)
        {
            for (int index = 0; index < speciesCatalog.Count; index++)
            {
                InvestigationSpeciesDefinition definition = speciesCatalog[index];
                if (definition != null && definition.MatchesIdentifier(speciesId)) return definition;
            }
            return null;
        }

        public bool IsFollowUpLockedSpecies(string speciesId)
        {
            for (int index = 0; index < followUpLockedSpeciesIds.Count; index++)
            {
                if (string.Equals(followUpLockedSpeciesIds[index], speciesId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public bool IsCaseSpecies(string speciesId)
        {
            for (int index = 0; index < species.Count; index++)
            {
                InvestigationSpeciesDefinition definition = species[index];
                if (definition != null && string.Equals(definition.SpeciesId, speciesId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public bool HasFoodWebConnection(string speciesId)
        {
            InvestigationSpeciesDefinition speciesDefinition = FindSpecies(speciesId);
            if (speciesDefinition == null) return false;
            string canonicalId = speciesDefinition.CanonicalSpeciesId;
            for (int index = 0; index < foodWebEdges.Count; index++)
            {
                FoodWebEdgeDefinition edge = foodWebEdges[index];
                if (edge != null
                    && (string.Equals(edge.PredatorSpeciesId, canonicalId, StringComparison.Ordinal)
                        || string.Equals(edge.PreySpeciesId, canonicalId, StringComparison.Ordinal)))
                {
                    return true;
                }
            }
            return false;
        }

        public InvestigationObservationDefinition FindObservation(string evidenceId)
        {
            for (int index = 0; index < observations.Count; index++)
            {
                InvestigationObservationDefinition definition = observations[index];
                if (definition != null && string.Equals(definition.EvidenceId, evidenceId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }

        public ThreatSimulationDefinition FindThreat(string threatId)
        {
            for (int index = 0; index < threats.Count; index++)
            {
                ThreatSimulationDefinition definition = threats[index];
                if (definition != null && string.Equals(definition.ThreatId, threatId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }

        public PredictionComparisonRuleDefinition FindComparisonRule(string threatId, string speciesId)
        {
            return FindComparisonRule(threatId, PredictionTargetKind.Species, speciesId);
        }

        public PredictionComparisonRuleDefinition FindComparisonRule(
            string threatId,
            PredictionTargetKind targetKind,
            string targetId)
        {
            for (int index = 0; index < comparisonRules.Count; index++)
            {
                PredictionComparisonRuleDefinition definition = comparisonRules[index];
                if (definition != null && definition.Matches(threatId, targetKind, targetId))
                {
                    return definition;
                }
            }
            return null;
        }

        public InvestigationObjectiveDefinition FindObjective(string objectiveId)
        {
            for (int index = 0; index < investigationObjectives.Count; index++)
            {
                InvestigationObjectiveDefinition objective = investigationObjectives[index];
                if (objective != null && string.Equals(objective.ObjectiveId, objectiveId, StringComparison.Ordinal))
                    return objective;
            }
            return null;
        }

        public InvestigationObjectiveDefinition FindObjectiveForComparison(
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            for (int index = 0; index < investigationObjectives.Count; index++)
            {
                InvestigationObjectiveDefinition objective = investigationObjectives[index];
                if (objective != null
                    && string.Equals(objective.ThreatId, threatId, StringComparison.Ordinal)
                    && objective.TargetKind == targetKind
                    && string.Equals(objective.TargetId, targetId, StringComparison.Ordinal)
                    && string.Equals(objective.RequiredEvidenceId, evidenceId, StringComparison.Ordinal)
                    && objective.RequiredJudgement == judgement)
                {
                    return objective;
                }
            }
            return null;
        }

        public InvestigationEvidenceCategoryRequirement FindEvidenceCategoryRequirement(EvidenceCategory category)
        {
            for (int index = 0; index < evidenceCategoryRequirements.Count; index++)
            {
                InvestigationEvidenceCategoryRequirement requirement = evidenceCategoryRequirements[index];
                if (requirement != null && requirement.Category == category) return requirement;
            }
            return null;
        }

        public InvestigationLimitationDefinition FindLimitation(string limitationId)
        {
            for (int index = 0; index < limitations.Count; index++)
            {
                InvestigationLimitationDefinition definition = limitations[index];
                if (definition != null && string.Equals(definition.LimitationId, limitationId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }

        public InvestigationReasoningDefinition FindReasoning(string reasoningId)
        {
            for (int index = 0; index < reasoningOptions.Count; index++)
            {
                InvestigationReasoningDefinition definition = reasoningOptions[index];
                if (definition != null && string.Equals(definition.ReasoningId, reasoningId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }

        public InvestigationRequiredComparisonSpeciesDefinition FindRequiredComparisonSpecies(string threatId)
        {
            for (int index = 0; index < requiredComparisonSpecies.Count; index++)
            {
                InvestigationRequiredComparisonSpeciesDefinition definition = requiredComparisonSpecies[index];
                if (definition != null && string.Equals(definition.ThreatId, threatId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }
    }
}
