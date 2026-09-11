using System;

namespace EDNA.Core
{
    [Serializable]
    public sealed class SampleRequest
    {
        public string requestId = string.Empty;
        public string siteId = string.Empty;
        public DepthBand depthBand;
    }
}
