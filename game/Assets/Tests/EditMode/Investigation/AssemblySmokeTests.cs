using EDNA.Core;
using EDNA.Investigation.Domain;
using NUnit.Framework;

namespace EDNA.Investigation.Tests
{
    public sealed class AssemblySmokeTests
    {
        [Test]
        public void TestAssembly_CanReferenceCoreAndDomain()
        {
            SampleRequest request = new SampleRequest
            {
                requestId = "request_01",
                siteId = "mock_summit",
                depthBand = DepthBand.Deep
            };

            Assert.That(request.siteId, Is.EqualTo("mock_summit"));
            Assert.That(EvidenceConfidence.High, Is.GreaterThan(EvidenceConfidence.Low));
        }
    }
}
