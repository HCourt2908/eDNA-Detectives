using System;
using System.Collections.Generic;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationCaseValidator
    {
        public List<string> Validate(InvestigationCaseDefinition caseDefinition)
        {
            List<string> errors = new List<string>();
            if (caseDefinition == null)
            {
                errors.Add("Case definition is missing.");
                return errors;
            }

            ValidateUniqueAssetIds(caseDefinition, errors);
            ValidateHistoricalRecords(caseDefinition, errors);
            ValidateInitialResults(caseDefinition, errors);
            ValidateMockOutcomes(caseDefinition, errors);

            if (caseDefinition.FindHypothesis(caseDefinition.CorrectHypothesisId) == null)
            {
                errors.Add("The correct hypothesis ID does not match a case hypothesis.");
            }

            return errors;
        }

        private static void ValidateUniqueAssetIds(InvestigationCaseDefinition caseDefinition, ICollection<string> errors)
        {
            HashSet<string> speciesIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.Species.Count; index++)
            {
                SpeciesDefinition species = caseDefinition.Species[index];
                if (species == null || string.IsNullOrEmpty(species.SpeciesId))
                {
                    errors.Add("Every species needs a stable ID.");
                }
                else if (!speciesIds.Add(species.SpeciesId))
                {
                    errors.Add($"Duplicate species ID: {species.SpeciesId}");
                }
            }

            HashSet<string> siteIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.SampleSites.Count; index++)
            {
                SampleSiteDefinition site = caseDefinition.SampleSites[index];
                if (site == null || string.IsNullOrEmpty(site.SiteId))
                {
                    errors.Add("Every sample site needs a stable ID.");
                }
                else if (!siteIds.Add(site.SiteId))
                {
                    errors.Add($"Duplicate sample site ID: {site.SiteId}");
                }
            }

            HashSet<string> hypothesisIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.Hypotheses.Count; index++)
            {
                HypothesisDefinition hypothesis = caseDefinition.Hypotheses[index];
                if (hypothesis == null || string.IsNullOrEmpty(hypothesis.HypothesisId))
                {
                    errors.Add("Every hypothesis needs a stable ID.");
                }
                else if (!hypothesisIds.Add(hypothesis.HypothesisId))
                {
                    errors.Add($"Duplicate hypothesis ID: {hypothesis.HypothesisId}");
                }
            }
        }

        private static void ValidateHistoricalRecords(InvestigationCaseDefinition caseDefinition, ICollection<string> errors)
        {
            for (int index = 0; index < caseDefinition.HistoricalBaseline.Count; index++)
            {
                HistoricalRecordDefinition record = caseDefinition.HistoricalBaseline[index];
                if (record == null)
                {
                    errors.Add("Historical baseline contains an empty record.");
                    continue;
                }

                if (caseDefinition.FindSpecies(record.speciesId) == null)
                {
                    errors.Add($"Historical record uses unknown species ID: {record.speciesId}");
                }

                SampleSiteDefinition site = caseDefinition.FindSite(record.siteId);
                if (site == null)
                {
                    errors.Add($"Historical record uses unknown site ID: {record.siteId}");
                }
                else if (!site.SupportsDepth(record.depthBand))
                {
                    errors.Add($"Historical record uses an unavailable depth at site: {record.siteId}");
                }
            }
        }

        private static void ValidateMockOutcomes(InvestigationCaseDefinition caseDefinition, ICollection<string> errors)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.MockSampleOutcomes.Count; index++)
            {
                MockSampleOutcomeDefinition outcome = caseDefinition.MockSampleOutcomes[index];
                if (outcome == null)
                {
                    errors.Add("Mock outcomes contain an empty record.");
                    continue;
                }

                string key = $"{outcome.siteId}:{outcome.depthBand}";
                if (!keys.Add(key))
                {
                    errors.Add($"Duplicate mock outcome: {key}");
                }

                SampleSiteDefinition site = caseDefinition.FindSite(outcome.siteId);
                if (site == null || !site.SupportsDepth(outcome.depthBand))
                {
                    errors.Add($"Mock outcome uses an unavailable site/depth: {key}");
                }

                if (outcome.resultTemplate == null)
                {
                    errors.Add($"Mock outcome has no result template: {key}");
                }
                else
                {
                    ValidateResultSpecies(caseDefinition, outcome.resultTemplate, $"Mock outcome {key}", errors);
                }
            }
        }

        private static void ValidateInitialResults(
            InvestigationCaseDefinition caseDefinition,
            ICollection<string> errors)
        {
            HashSet<string> resultIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < caseDefinition.InitialResults.Count; index++)
            {
                EDNA.Core.EDNAResultData result = caseDefinition.InitialResults[index];
                if (result == null)
                {
                    errors.Add("Initial results contain an empty result.");
                    continue;
                }

                string resultId = !string.IsNullOrEmpty(result.sampleId) ? result.sampleId : result.requestId;
                if (string.IsNullOrEmpty(resultId))
                {
                    errors.Add("Every initial result needs a sample or request ID.");
                }
                else if (!resultIds.Add(resultId))
                {
                    errors.Add($"Duplicate initial result ID: {resultId}");
                }

                SampleSiteDefinition site = caseDefinition.FindSite(result.siteId);
                if (site == null || !site.SupportsDepth(result.depthBand))
                {
                    errors.Add($"Initial result uses an unavailable site/depth: {result.siteId}:{result.depthBand}");
                }

                ValidateResultSpecies(caseDefinition, result, $"Initial result {resultId}", errors);
            }
        }

        private static void ValidateResultSpecies(
            InvestigationCaseDefinition caseDefinition,
            EDNA.Core.EDNAResultData result,
            string context,
            ICollection<string> errors)
        {
            if (result.detectedSpeciesIds == null)
            {
                errors.Add($"{context} has no detected-species list.");
                return;
            }

            for (int speciesIndex = 0; speciesIndex < result.detectedSpeciesIds.Count; speciesIndex++)
            {
                string speciesId = result.detectedSpeciesIds[speciesIndex];
                if (caseDefinition.FindSpecies(speciesId) == null)
                {
                    errors.Add($"{context} uses unknown species ID: {speciesId}");
                }
            }
        }
    }
}
