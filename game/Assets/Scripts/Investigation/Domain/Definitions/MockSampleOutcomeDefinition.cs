using System;
using EDNA.Core;

namespace EDNA.Investigation.Domain
{
    [Serializable]
    public sealed class MockSampleOutcomeDefinition
    {
        public string siteId = string.Empty;
        public DepthBand depthBand;
        public EDNAResultData resultTemplate = new EDNAResultData();
    }
}
