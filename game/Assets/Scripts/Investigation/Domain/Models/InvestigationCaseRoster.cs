using System;
using System.Collections.Generic;
using EDNA.Core;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationSurveySpeciesRecord
    {
        public InvestigationSurveySpeciesRecord(
            string speciesId,
            string sampleId,
            string siteId,
            string source,
            DepthBand depthBand,
            SurveyTimepoint surveyTimepoint,
            SpeciesDetectionState detectionState,
            SurveyConfidence confidence,
            SampleQuality sampleQuality)
        {
            SpeciesId = speciesId ?? string.Empty;
            SampleId = sampleId ?? string.Empty;
            SiteId = siteId ?? string.Empty;
            Source = string.IsNullOrWhiteSpace(source) ? "eDNA" : source.Trim();
            DepthBand = depthBand;
            SurveyTimepoint = surveyTimepoint;
            DetectionState = detectionState;
            Confidence = confidence;
            SampleQuality = sampleQuality;
        }

        public string SpeciesId { get; }
        public string SampleId { get; }
        public string SiteId { get; }
        public string Source { get; }
        public DepthBand DepthBand { get; }
        public SurveyTimepoint SurveyTimepoint { get; }
        public SpeciesDetectionState DetectionState { get; }
        public SurveyConfidence Confidence { get; }
        public SampleQuality SampleQuality { get; }
    }

    public sealed class InvestigationCaseRoster
    {
        public InvestigationCaseRoster(
            IEnumerable<string> speciesIds,
            IEnumerable<InvestigationSurveySpeciesRecord> surveyRecords)
        {
            SpeciesIds = new List<string>(speciesIds ?? Array.Empty<string>());
            SurveyRecords = new List<InvestigationSurveySpeciesRecord>(surveyRecords ?? Array.Empty<InvestigationSurveySpeciesRecord>());
        }

        public IReadOnlyList<string> SpeciesIds { get; }
        public IReadOnlyList<InvestigationSurveySpeciesRecord> SurveyRecords { get; }
    }
}
