using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDNA.Investigation.V2.Domain
{
    [Serializable]
    public sealed class InvestigationV2LimitationDefinition
    {
        [SerializeField] private string limitationId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(1, 3)] private string explanation = string.Empty;

        public string LimitationId => limitationId;
        public string DisplayName => displayName;
        public string Explanation => explanation;
    }

    [Serializable]
    public sealed class InvestigationV2ReasoningDefinition
    {
        [SerializeField] private string reasoningId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(1, 3)] private string explanation = string.Empty;

        public string ReasoningId => reasoningId;
        public string DisplayName => displayName;
        public string Explanation => explanation;
    }

    [Serializable]
    public sealed class InvestigationV2RequiredComparisonSpeciesDefinition
    {
        [SerializeField] private string threatId = string.Empty;
        [SerializeField] private List<string> requiredComparisonSpeciesIds = new List<string>();

        public string ThreatId => threatId;
        public IReadOnlyList<string> RequiredComparisonSpeciesIds => requiredComparisonSpeciesIds;
    }

    [Serializable]
    public sealed class InvestigationV2ObjectiveDefinition
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
    public sealed class InvestigationV2EvidenceCategoryRequirement
    {
        [SerializeField] private EvidenceCategory category;
        [SerializeField, Min(1)] private int minimumCount = 1;

        public EvidenceCategory Category => category;
        public int MinimumCount => Mathf.Max(1, minimumCount);
    }

    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation V2/Case", fileName = "InvestigationCaseV2_")]
    public sealed class InvestigationV2CaseDefinition : ScriptableObject
    {
        [SerializeField] private string caseId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 6)] private string briefing = string.Empty;
        [SerializeField] private List<InvestigationV2SpeciesDefinition> species = new List<InvestigationV2SpeciesDefinition>();
        [SerializeField] private List<InvestigationV2ObservationDefinition> observations = new List<InvestigationV2ObservationDefinition>();
        [SerializeField] private List<ThreatSimulationDefinition> threats = new List<ThreatSimulationDefinition>();
        [SerializeField] private List<PredictionComparisonRuleDefinition> comparisonRules = new List<PredictionComparisonRuleDefinition>();
        [SerializeField] private List<InvestigationV2ObjectiveDefinition> investigationObjectives = new List<InvestigationV2ObjectiveDefinition>();
        [SerializeField, Min(1)] private int minimumObserveDiscoveries = 4;
        [SerializeField] private List<string> requiredComparedThreatIds = new List<string>();
        [SerializeField] private List<InvestigationV2RequiredComparisonSpeciesDefinition> requiredComparisonSpecies = new List<InvestigationV2RequiredComparisonSpeciesDefinition>();
        [SerializeField, Min(1)] private int requiredComparisonsPerThreat = 2;
        [SerializeField, Min(1)] private int minimumCompletedComparisons = 4;
        [SerializeField] private string correctThreatId = string.Empty;
        [SerializeField] private List<string> confirmationEvidenceIds = new List<string>();
        [SerializeField, Min(1)] private int minimumReportEvidence = 2;
        [SerializeField] private List<InvestigationV2EvidenceCategoryRequirement> evidenceCategoryRequirements = new List<InvestigationV2EvidenceCategoryRequirement>();
        [SerializeField, Min(1)] private int minimumConfirmationEvidenceInReport = 1;
        [SerializeField, Min(1)] private int minimumReportLimitations = 1;
        [SerializeField] private string requiredReasoningId = "food_web_cascade";
        [SerializeField] private List<InvestigationV2ReasoningDefinition> reasoningOptions = new List<InvestigationV2ReasoningDefinition>();
        [SerializeField] private List<InvestigationV2LimitationDefinition> limitations = new List<InvestigationV2LimitationDefinition>();
        [SerializeField, TextArea(2, 5)] private string successFeedback = string.Empty;

        public string CaseId => caseId;
        public string DisplayName => displayName;
        public string Briefing => briefing;
        public IReadOnlyList<InvestigationV2SpeciesDefinition> Species => species;
        public IReadOnlyList<InvestigationV2ObservationDefinition> Observations => observations;
        public IReadOnlyList<ThreatSimulationDefinition> Threats => threats;
        public IReadOnlyList<PredictionComparisonRuleDefinition> ComparisonRules => comparisonRules;
        public IReadOnlyList<InvestigationV2ObjectiveDefinition> InvestigationObjectives => investigationObjectives;
        public int MinimumObserveDiscoveries => Mathf.Max(1, minimumObserveDiscoveries);
        public IReadOnlyList<string> RequiredComparedThreatIds => requiredComparedThreatIds;
        public IReadOnlyList<InvestigationV2RequiredComparisonSpeciesDefinition> RequiredComparisonSpecies => requiredComparisonSpecies;
        public int RequiredComparisonsPerThreat => Mathf.Max(1, requiredComparisonsPerThreat);
        public int MinimumCompletedComparisons => Mathf.Max(1, minimumCompletedComparisons);
        public string CorrectThreatId => correctThreatId;
        public IReadOnlyList<string> ConfirmationEvidenceIds => confirmationEvidenceIds;
        public int MinimumReportEvidence => Mathf.Max(1, minimumReportEvidence);
        public IReadOnlyList<InvestigationV2EvidenceCategoryRequirement> EvidenceCategoryRequirements => evidenceCategoryRequirements;
        public int MinimumConfirmationEvidenceInReport => Mathf.Max(1, minimumConfirmationEvidenceInReport);
        public int MinimumReportLimitations => Mathf.Max(1, minimumReportLimitations);
        public string RequiredReasoningId => requiredReasoningId;
        public IReadOnlyList<InvestigationV2ReasoningDefinition> ReasoningOptions => reasoningOptions;
        public IReadOnlyList<InvestigationV2LimitationDefinition> Limitations => limitations;
        public string SuccessFeedback => successFeedback;

        public InvestigationV2SpeciesDefinition FindSpecies(string speciesId)
        {
            for (int index = 0; index < species.Count; index++)
            {
                InvestigationV2SpeciesDefinition definition = species[index];
                if (definition != null && string.Equals(definition.SpeciesId, speciesId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }

        public InvestigationV2ObservationDefinition FindObservation(string evidenceId)
        {
            for (int index = 0; index < observations.Count; index++)
            {
                InvestigationV2ObservationDefinition definition = observations[index];
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

        public InvestigationV2ObjectiveDefinition FindObjective(string objectiveId)
        {
            for (int index = 0; index < investigationObjectives.Count; index++)
            {
                InvestigationV2ObjectiveDefinition objective = investigationObjectives[index];
                if (objective != null && string.Equals(objective.ObjectiveId, objectiveId, StringComparison.Ordinal))
                    return objective;
            }
            return null;
        }

        public InvestigationV2ObjectiveDefinition FindObjectiveForComparison(
            string threatId,
            PredictionTargetKind targetKind,
            string targetId,
            string evidenceId,
            ComparisonJudgement judgement)
        {
            for (int index = 0; index < investigationObjectives.Count; index++)
            {
                InvestigationV2ObjectiveDefinition objective = investigationObjectives[index];
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

        public InvestigationV2EvidenceCategoryRequirement FindEvidenceCategoryRequirement(EvidenceCategory category)
        {
            for (int index = 0; index < evidenceCategoryRequirements.Count; index++)
            {
                InvestigationV2EvidenceCategoryRequirement requirement = evidenceCategoryRequirements[index];
                if (requirement != null && requirement.Category == category) return requirement;
            }
            return null;
        }

        public InvestigationV2LimitationDefinition FindLimitation(string limitationId)
        {
            for (int index = 0; index < limitations.Count; index++)
            {
                InvestigationV2LimitationDefinition definition = limitations[index];
                if (definition != null && string.Equals(definition.LimitationId, limitationId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }

        public InvestigationV2ReasoningDefinition FindReasoning(string reasoningId)
        {
            for (int index = 0; index < reasoningOptions.Count; index++)
            {
                InvestigationV2ReasoningDefinition definition = reasoningOptions[index];
                if (definition != null && string.Equals(definition.ReasoningId, reasoningId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }

        public InvestigationV2RequiredComparisonSpeciesDefinition FindRequiredComparisonSpecies(string threatId)
        {
            for (int index = 0; index < requiredComparisonSpecies.Count; index++)
            {
                InvestigationV2RequiredComparisonSpeciesDefinition definition = requiredComparisonSpecies[index];
                if (definition != null && string.Equals(definition.ThreatId, threatId, StringComparison.Ordinal)) return definition;
            }
            return null;
        }
    }
}
