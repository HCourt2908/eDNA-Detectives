using System;
using System.Collections.Generic;

namespace EDNA.Core
{
    [Serializable]
    public sealed class InvestigationExternalObservationData
    {
        public string observationId = string.Empty;
        public string source = string.Empty;
        public string value = string.Empty;
        public string confidence = string.Empty;
    }

    [Serializable]
    public sealed class InvestigationGameInput
    {
        public string caseId = string.Empty;
        public List<EDNAResultData> ednaResults = new List<EDNAResultData>();
        public List<InvestigationExternalObservationData> environmentalObservations = new List<InvestigationExternalObservationData>();
        public List<InvestigationExternalObservationData> physicalObservations = new List<InvestigationExternalObservationData>();
        public List<string> discoveredObservationIds = new List<string>();
    }
}
