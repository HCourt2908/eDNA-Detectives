using System;
using EDNA.Core;

namespace EDNA.Investigation.Domain
{
    [Serializable]
    public sealed class HistoricalRecordDefinition
    {
        public string speciesId = string.Empty;
        public string siteId = string.Empty;
        public DepthBand depthBand;
        public bool expectedPresence = true;
    }
}
