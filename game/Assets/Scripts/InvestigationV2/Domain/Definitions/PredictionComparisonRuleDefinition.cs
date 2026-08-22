using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDNA.Investigation.V2.Domain
{
    [Serializable]
    public sealed class JudgementResolutionDefinition
    {
        [SerializeField] private ComparisonJudgement judgement;
        [SerializeField] private ComparisonEvaluationOutcome outcome;
        [SerializeField, TextArea(1, 4)] private string feedback = string.Empty;

        public ComparisonJudgement Judgement => judgement;
        public ComparisonEvaluationOutcome Outcome => outcome;
        public string Feedback => feedback;
    }

    [Serializable]
    public sealed class ObservationComparisonOptionDefinition
    {
        [SerializeField] private string evidenceId = string.Empty;
        [SerializeField] private List<JudgementResolutionDefinition> resolutions = new List<JudgementResolutionDefinition>();

        public string EvidenceId => evidenceId;
        public IReadOnlyList<JudgementResolutionDefinition> Resolutions => resolutions;

        public JudgementResolutionDefinition FindResolution(ComparisonJudgement judgement)
        {
            for (int index = 0; index < resolutions.Count; index++)
            {
                JudgementResolutionDefinition resolution = resolutions[index];
                if (resolution != null && resolution.Judgement == judgement)
                {
                    return resolution;
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class PredictionComparisonRuleDefinition
    {
        [SerializeField] private string threatId = string.Empty;
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private List<ObservationComparisonOptionDefinition> observationOptions = new List<ObservationComparisonOptionDefinition>();

        public string ThreatId => threatId;
        public string SpeciesId => speciesId;
        public IReadOnlyList<ObservationComparisonOptionDefinition> ObservationOptions => observationOptions;

        public ObservationComparisonOptionDefinition FindOption(string evidenceId)
        {
            for (int index = 0; index < observationOptions.Count; index++)
            {
                ObservationComparisonOptionDefinition option = observationOptions[index];
                if (option != null && string.Equals(option.EvidenceId, evidenceId, StringComparison.Ordinal))
                {
                    return option;
                }
            }

            return null;
        }
    }
}
