using System;

namespace EDNA.Core
{
    public enum SpeciesDetectionState
    {
        Unknown = 0,
        Detected = 1,
        NotDetected = 2
    }

    public enum SurveyTimepoint
    {
        Unknown = 0,
        Historical = 1,
        Current = 2
    }

    public enum SurveyConfidence
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    [Serializable]
    public sealed class EDNASpeciesObservationData
    {
        public string speciesId = string.Empty;
        public string sampleId = string.Empty;
        public string siteId = string.Empty;
        public string source = "eDNA";
        public DepthBand depthBand;
        public SurveyTimepoint surveyTimepoint = SurveyTimepoint.Current;
        public SpeciesDetectionState detectionState = SpeciesDetectionState.Unknown;
        public SurveyConfidence confidence = SurveyConfidence.Unknown;
        public SampleQuality sampleQuality = SampleQuality.Unknown;
    }
}
