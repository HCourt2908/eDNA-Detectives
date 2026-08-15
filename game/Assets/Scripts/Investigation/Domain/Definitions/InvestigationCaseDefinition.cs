using System;
using System.Collections.Generic;
using EDNA.Core;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    [CreateAssetMenu(menuName = "eDNA Detectives/Investigation/Case", fileName = "InvestigationCase_")]
    public sealed class InvestigationCaseDefinition : ScriptableObject
    {
        [SerializeField] private string caseId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 6)] private string briefing = string.Empty;
        [SerializeField] private List<HistoricalRecordDefinition> historicalBaseline = new List<HistoricalRecordDefinition>();
        [SerializeField] private List<SpeciesDefinition> species = new List<SpeciesDefinition>();
        [SerializeField] private List<SampleSiteDefinition> sampleSites = new List<SampleSiteDefinition>();
        [SerializeField] private List<HypothesisDefinition> hypotheses = new List<HypothesisDefinition>();
        [SerializeField] private List<EDNAResultData> initialResults = new List<EDNAResultData>();
        [SerializeField] private List<MockSampleOutcomeDefinition> mockSampleOutcomes = new List<MockSampleOutcomeDefinition>();
        [SerializeField, Min(0)] private int followUpSampleLimit = 2;
        [SerializeField] private string correctHypothesisId = string.Empty;
        [SerializeField] private bool requireFollowUpSample = true;
        [SerializeField, Min(0)] private int requiredOpposingEvidence = 1;
        [SerializeField, TextArea(2, 5)] private string successFeedback = string.Empty;

        public string CaseId => caseId;
        public string DisplayName => displayName;
        public string Briefing => briefing;
        public IReadOnlyList<HistoricalRecordDefinition> HistoricalBaseline => historicalBaseline;
        public IReadOnlyList<SpeciesDefinition> Species => species;
        public IReadOnlyList<SampleSiteDefinition> SampleSites => sampleSites;
        public IReadOnlyList<HypothesisDefinition> Hypotheses => hypotheses;
        public IReadOnlyList<EDNAResultData> InitialResults => initialResults;
        public IReadOnlyList<MockSampleOutcomeDefinition> MockSampleOutcomes => mockSampleOutcomes;
        public int FollowUpSampleLimit => Mathf.Max(0, followUpSampleLimit);
        public string CorrectHypothesisId => correctHypothesisId;
        public bool RequireFollowUpSample => requireFollowUpSample;
        public int RequiredOpposingEvidence => Mathf.Max(0, requiredOpposingEvidence);
        public string SuccessFeedback => successFeedback;

        public SpeciesDefinition FindSpecies(string speciesId)
        {
            for (int index = 0; index < species.Count; index++)
            {
                SpeciesDefinition definition = species[index];
                if (definition != null && string.Equals(definition.SpeciesId, speciesId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public SampleSiteDefinition FindSite(string siteId)
        {
            for (int index = 0; index < sampleSites.Count; index++)
            {
                SampleSiteDefinition definition = sampleSites[index];
                if (definition != null && string.Equals(definition.SiteId, siteId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public HypothesisDefinition FindHypothesis(string hypothesisId)
        {
            for (int index = 0; index < hypotheses.Count; index++)
            {
                HypothesisDefinition definition = hypotheses[index];
                if (definition != null && string.Equals(definition.HypothesisId, hypothesisId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
