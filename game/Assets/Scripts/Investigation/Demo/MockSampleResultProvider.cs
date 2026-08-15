using System;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;

namespace EDNA.Investigation
{
    public sealed class MockSampleResultProvider
    {
        private readonly InvestigationCaseDefinition caseDefinition;

        public MockSampleResultProvider(InvestigationCaseDefinition caseDefinition)
        {
            this.caseDefinition = caseDefinition ?? throw new ArgumentNullException(nameof(caseDefinition));
        }

        public EDNAResultData CreateResult(InvestigationSamplePlan plan)
        {
            if (plan == null || plan.Request == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            IReadOnlyList<MockSampleOutcomeDefinition> outcomes = caseDefinition.MockSampleOutcomes;
            for (int index = 0; index < outcomes.Count; index++)
            {
                MockSampleOutcomeDefinition outcome = outcomes[index];
                if (outcome != null
                    && outcome.resultTemplate != null
                    && string.Equals(outcome.siteId, plan.Request.siteId, StringComparison.Ordinal)
                    && outcome.depthBand == plan.Request.depthBand)
                {
                    return CloneForRequest(outcome.resultTemplate, plan);
                }
            }

            return new EDNAResultData
            {
                requestId = plan.Request.requestId,
                sampleId = $"mock_{plan.Request.requestId}",
                siteId = plan.Request.siteId,
                depthBand = plan.Request.depthBand,
                roundIndex = plan.RoundIndex,
                sampleQuality = SampleQuality.Low,
                detectedSpeciesIds = new List<string>(),
                resultFlags = new List<EDNAResultFlag>
                {
                    EDNAResultFlag.LowQuality,
                    EDNAResultFlag.MissingMockData
                }
            };
        }

        private static EDNAResultData CloneForRequest(EDNAResultData template, InvestigationSamplePlan plan)
        {
            return new EDNAResultData
            {
                requestId = plan.Request.requestId,
                sampleId = $"mock_{plan.Request.requestId}",
                siteId = plan.Request.siteId,
                depthBand = plan.Request.depthBand,
                roundIndex = plan.RoundIndex,
                sampleQuality = template.sampleQuality,
                detectedSpeciesIds = template.detectedSpeciesIds == null
                    ? new List<string>()
                    : new List<string>(template.detectedSpeciesIds),
                resultFlags = template.resultFlags == null
                    ? new List<EDNAResultFlag>()
                    : new List<EDNAResultFlag>(template.resultFlags)
            };
        }
    }
}
