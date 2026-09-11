using System;
using System.Collections.Generic;

namespace EDNA.Core
{
    [Serializable]
    public sealed class EDNAResultData
    {
        public string requestId = string.Empty;
        public string sampleId = string.Empty;
        public string siteId = string.Empty;
        public DepthBand depthBand;
        public int roundIndex;
        public SampleQuality sampleQuality;
        public List<string> detectedSpeciesIds = new List<string>();
        public List<EDNASpeciesObservationData> speciesObservations = new List<EDNASpeciesObservationData>();
        public List<EDNAResultFlag> resultFlags = new List<EDNAResultFlag>();
    }
}
