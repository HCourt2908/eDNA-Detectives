using System;
using System.Collections.Generic;
using EDNA.Core;

namespace EDNA.Investigation.Domain
{
    public sealed class EvidenceEvaluator
    {
        public List<EvidenceRecord> Evaluate(
            InvestigationCaseDefinition caseDefinition,
            IReadOnlyList<EDNAResultData> results)
        {
            if (caseDefinition == null)
            {
                throw new ArgumentNullException(nameof(caseDefinition));
            }

            Dictionary<string, EvidenceRecord> evidenceById = new Dictionary<string, EvidenceRecord>();
            AddResultQualityEvidence(evidenceById, results);
            AddDetectionEvidence(evidenceById, caseDefinition, results);
            AddNonDetectionEvidence(evidenceById, caseDefinition, results);
            AddDepthShiftEvidence(evidenceById, caseDefinition, results);

            List<EvidenceRecord> evidence = new List<EvidenceRecord>(evidenceById.Values);
            evidence.Sort((left, right) => string.Compare(left.EvidenceId, right.EvidenceId, StringComparison.Ordinal));
            return evidence;
        }

        private static void AddResultQualityEvidence(
            IDictionary<string, EvidenceRecord> evidenceById,
            IReadOnlyList<EDNAResultData> results)
        {
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                EDNAResultData result = results[resultIndex];
                if (result == null)
                {
                    continue;
                }

                string sourceId = GetSourceId(result);
                if (result.sampleQuality == SampleQuality.Low
                    || result.sampleQuality == SampleQuality.Unknown
                    || HasFlag(result, EDNAResultFlag.LowQuality)
                    || HasFlag(result, EDNAResultFlag.MissingMockData))
                {
                    AddEvidence(
                        evidenceById,
                        new EvidenceRecord(
                            $"low_quality:{sourceId}",
                            EvidenceType.LowQualityResult,
                            Array.Empty<string>(),
                            new[] { sourceId },
                            EvidenceConfidence.Low,
                            new[] { EvidenceType.LowQualityResult.ToString() },
                            $"Result {sourceId} may be unreliable.",
                            "The sample is low quality or uses fallback data."));
                }

                if (HasFlag(result, EDNAResultFlag.ContaminationWarning))
                {
                    AddEvidence(
                        evidenceById,
                        new EvidenceRecord(
                            $"contamination:{sourceId}",
                            EvidenceType.ContaminationWarning,
                            result.detectedSpeciesIds,
                            new[] { sourceId },
                            EvidenceConfidence.Low,
                            new[] { EvidenceType.ContaminationWarning.ToString() },
                            $"Result {sourceId} has a contamination warning.",
                            "A contamination warning prevents this result from being strong evidence on its own."));
                }
            }
        }

        private static void AddDetectionEvidence(
            IDictionary<string, EvidenceRecord> evidenceById,
            InvestigationCaseDefinition caseDefinition,
            IReadOnlyList<EDNAResultData> results)
        {
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                EDNAResultData result = results[resultIndex];
                if (result == null)
                {
                    continue;
                }

                string sourceId = GetSourceId(result);
                if (result.detectedSpeciesIds == null)
                {
                    continue;
                }

                for (int speciesIndex = 0; speciesIndex < result.detectedSpeciesIds.Count; speciesIndex++)
                {
                    string speciesId = result.detectedSpeciesIds[speciesIndex];
                    int totalDetections = CountDetections(results, speciesId);
                    bool historicallyExpected = IsExpected(
                        caseDefinition.HistoricalBaseline,
                        speciesId,
                        result.siteId,
                        result.depthBand);
                    SpeciesDefinition species = caseDefinition.FindSpecies(speciesId);
                    string speciesName = GetSpeciesName(species, speciesId);
                    string siteName = GetSiteName(caseDefinition, result.siteId);
                    List<string> tags = BuildEvidenceTags(
                        historicallyExpected && totalDetections > 1
                            ? EvidenceType.RepeatedDetection
                            : historicallyExpected
                                ? EvidenceType.StableIndicator
                                : EvidenceType.NewDetection,
                        species);

                    if (!historicallyExpected)
                    {
                        AddEvidence(
                            evidenceById,
                            new EvidenceRecord(
                                $"new:{speciesId}:{result.siteId}:{result.depthBand}",
                                EvidenceType.NewDetection,
                                new[] { speciesId },
                                new[] { sourceId },
                                QualityToConfidence(result.sampleQuality),
                                tags,
                                $"New detection: {speciesName} was not in the historical baseline at {siteName} ({result.depthBand}).",
                                GetQualityReason(result.sampleQuality, totalDetections)));
                    }
                    else if (totalDetections > 1)
                    {
                        AddEvidence(
                            evidenceById,
                            new EvidenceRecord(
                                $"repeated:{speciesId}",
                                EvidenceType.RepeatedDetection,
                                new[] { speciesId },
                                FindDetectionSources(results, speciesId),
                                EvidenceConfidence.High,
                                tags,
                                $"Repeated detection: {speciesName} appears in more than one independent sample.",
                                "The same species was detected in multiple samples."));
                    }
                    else
                    {
                        AddEvidence(
                            evidenceById,
                            new EvidenceRecord(
                                $"stable:{speciesId}:{result.siteId}:{result.depthBand}",
                                EvidenceType.StableIndicator,
                                new[] { speciesId },
                                new[] { sourceId },
                                QualityToConfidence(result.sampleQuality),
                                tags,
                                $"Stable indicator: {speciesName} is still detected at {siteName} ({result.depthBand}).",
                                GetQualityReason(result.sampleQuality, totalDetections)));
                    }
                }
            }
        }

        private static void AddNonDetectionEvidence(
            IDictionary<string, EvidenceRecord> evidenceById,
            InvestigationCaseDefinition caseDefinition,
            IReadOnlyList<EDNAResultData> results)
        {
            IReadOnlyList<HistoricalRecordDefinition> baseline = caseDefinition.HistoricalBaseline;
            for (int baselineIndex = 0; baselineIndex < baseline.Count; baselineIndex++)
            {
                HistoricalRecordDefinition record = baseline[baselineIndex];
                if (record == null || !record.expectedPresence)
                {
                    continue;
                }

                List<EDNAResultData> matchingResults = FindResultsAtContext(
                    results,
                    record.siteId,
                    record.depthBand);
                if (matchingResults.Count == 0 || AnyDetection(matchingResults, record.speciesId))
                {
                    continue;
                }

                int reliableSampleCount = CountReliableSamples(matchingResults);
                EvidenceType evidenceType = reliableSampleCount >= 2
                    ? EvidenceType.RepeatedNonDetection
                    : EvidenceType.NotDetectedInSample;
                EvidenceConfidence confidence = reliableSampleCount >= 2
                    ? EvidenceConfidence.High
                    : reliableSampleCount == 1
                        ? EvidenceConfidence.Medium
                        : EvidenceConfidence.Low;
                SpeciesDefinition species = caseDefinition.FindSpecies(record.speciesId);
                string speciesName = GetSpeciesName(species, record.speciesId);
                string siteName = GetSiteName(caseDefinition, record.siteId);

                AddEvidence(
                    evidenceById,
                    new EvidenceRecord(
                        $"not_detected:{record.speciesId}:{record.siteId}:{record.depthBand}",
                        evidenceType,
                        new[] { record.speciesId },
                        GetSourceIds(matchingResults),
                        confidence,
                        BuildEvidenceTags(evidenceType, species),
                        $"Not detected in this sample: {speciesName} was expected at {siteName} ({record.depthBand}).",
                        reliableSampleCount >= 2
                            ? "Several reliable samples show the same pattern."
                            : reliableSampleCount == 1
                                ? "This comes from one reliable sample, so absence is not certain."
                                : "Only low-quality evidence is available."));
            }
        }

        private static void AddDepthShiftEvidence(
            IDictionary<string, EvidenceRecord> evidenceById,
            InvestigationCaseDefinition caseDefinition,
            IReadOnlyList<EDNAResultData> results)
        {
            IReadOnlyList<HistoricalRecordDefinition> baseline = caseDefinition.HistoricalBaseline;
            for (int baselineIndex = 0; baselineIndex < baseline.Count; baselineIndex++)
            {
                HistoricalRecordDefinition record = baseline[baselineIndex];
                if (record == null || !record.expectedPresence || record.depthBand != DepthBand.Shallow)
                {
                    continue;
                }

                List<EDNAResultData> shallowResults = FindResultsAtContext(results, record.siteId, DepthBand.Shallow);
                List<EDNAResultData> deepResults = FindResultsAtContext(results, record.siteId, DepthBand.Deep);
                if (shallowResults.Count == 0
                    || AnyDetection(shallowResults, record.speciesId)
                    || !AnyDetection(deepResults, record.speciesId))
                {
                    continue;
                }

                SpeciesDefinition species = caseDefinition.FindSpecies(record.speciesId);
                string speciesName = GetSpeciesName(species, record.speciesId);
                string siteName = GetSiteName(caseDefinition, record.siteId);
                List<string> sourceIds = GetSourceIds(shallowResults);
                sourceIds.AddRange(GetSourceIds(deepResults));

                AddEvidence(
                    evidenceById,
                    new EvidenceRecord(
                        $"depth_shift:{record.speciesId}:{record.siteId}",
                        EvidenceType.DepthShift,
                        new[] { record.speciesId },
                        sourceIds,
                        EvidenceConfidence.High,
                        BuildEvidenceTags(EvidenceType.DepthShift, species),
                        $"Depth shift: {speciesName} was not detected in shallow water but was detected deep at {siteName}.",
                        "Two depth-specific results support a distribution change rather than disappearance."));
            }
        }

        private static void AddEvidence(IDictionary<string, EvidenceRecord> evidenceById, EvidenceRecord evidence)
        {
            if (!evidenceById.TryGetValue(evidence.EvidenceId, out EvidenceRecord existing)
                || evidence.Confidence > existing.Confidence)
            {
                evidenceById[evidence.EvidenceId] = evidence;
            }
        }

        private static bool IsExpected(
            IReadOnlyList<HistoricalRecordDefinition> baseline,
            string speciesId,
            string siteId,
            DepthBand depthBand)
        {
            for (int index = 0; index < baseline.Count; index++)
            {
                HistoricalRecordDefinition record = baseline[index];
                if (record != null
                    && record.expectedPresence
                    && string.Equals(record.speciesId, speciesId, StringComparison.Ordinal)
                    && string.Equals(record.siteId, siteId, StringComparison.Ordinal)
                    && record.depthBand == depthBand)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountDetections(IReadOnlyList<EDNAResultData> results, string speciesId)
        {
            int count = 0;
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                EDNAResultData result = results[resultIndex];
                if (ContainsSpecies(result, speciesId))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string> FindDetectionSources(IReadOnlyList<EDNAResultData> results, string speciesId)
        {
            List<string> sourceIds = new List<string>();
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                EDNAResultData result = results[resultIndex];
                if (ContainsSpecies(result, speciesId))
                {
                    sourceIds.Add(GetSourceId(result));
                }
            }

            return sourceIds;
        }

        private static List<EDNAResultData> FindResultsAtContext(
            IReadOnlyList<EDNAResultData> results,
            string siteId,
            DepthBand depthBand)
        {
            List<EDNAResultData> matching = new List<EDNAResultData>();
            for (int index = 0; index < results.Count; index++)
            {
                EDNAResultData result = results[index];
                if (result != null
                    && string.Equals(result.siteId, siteId, StringComparison.Ordinal)
                    && result.depthBand == depthBand)
                {
                    matching.Add(result);
                }
            }

            return matching;
        }

        private static bool AnyDetection(IReadOnlyList<EDNAResultData> results, string speciesId)
        {
            for (int index = 0; index < results.Count; index++)
            {
                if (ContainsSpecies(results[index], speciesId))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountReliableSamples(IReadOnlyList<EDNAResultData> results)
        {
            int count = 0;
            for (int index = 0; index < results.Count; index++)
            {
                EDNAResultData result = results[index];
                if (result.sampleQuality >= SampleQuality.Medium
                    && !HasFlag(result, EDNAResultFlag.ContaminationWarning)
                    && !HasFlag(result, EDNAResultFlag.MissingMockData))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasFlag(EDNAResultData result, EDNAResultFlag flag)
        {
            return result.resultFlags != null && result.resultFlags.Contains(flag);
        }

        private static bool ContainsSpecies(EDNAResultData result, string speciesId)
        {
            return result != null
                && result.detectedSpeciesIds != null
                && result.detectedSpeciesIds.Contains(speciesId);
        }

        private static List<string> GetSourceIds(IReadOnlyList<EDNAResultData> results)
        {
            List<string> sourceIds = new List<string>();
            for (int index = 0; index < results.Count; index++)
            {
                sourceIds.Add(GetSourceId(results[index]));
            }

            return sourceIds;
        }

        private static string GetSourceId(EDNAResultData result)
        {
            if (!string.IsNullOrEmpty(result.sampleId))
            {
                return result.sampleId;
            }

            if (!string.IsNullOrEmpty(result.requestId))
            {
                return result.requestId;
            }

            return "unknown_sample";
        }

        private static List<string> BuildEvidenceTags(EvidenceType evidenceType, SpeciesDefinition species)
        {
            List<string> tags = new List<string> { evidenceType.ToString() };
            if (species == null)
            {
                return tags;
            }

            tags.Add($"species:{species.SpeciesId}");
            for (int index = 0; index < species.SensitivityTags.Count; index++)
            {
                string tag = species.SensitivityTags[index];
                if (!string.IsNullOrEmpty(tag) && !tags.Contains(tag))
                {
                    tags.Add(tag);
                }
            }

            return tags;
        }

        private static EvidenceConfidence QualityToConfidence(SampleQuality quality)
        {
            return quality == SampleQuality.High
                ? EvidenceConfidence.Medium
                : quality == SampleQuality.Medium
                    ? EvidenceConfidence.Medium
                    : EvidenceConfidence.Low;
        }

        private static string GetQualityReason(SampleQuality quality, int detectionCount)
        {
            if (detectionCount > 1)
            {
                return "The same result appears in multiple samples.";
            }

            return quality >= SampleQuality.Medium
                ? "This comes from one reliable sample."
                : "This comes from a low-quality sample.";
        }

        private static string GetSpeciesName(SpeciesDefinition species, string fallbackId)
        {
            return species != null && !string.IsNullOrEmpty(species.DisplayName)
                ? species.DisplayName
                : fallbackId;
        }

        private static string GetSiteName(InvestigationCaseDefinition caseDefinition, string siteId)
        {
            SampleSiteDefinition site = caseDefinition.FindSite(siteId);
            return site != null && !string.IsNullOrEmpty(site.DisplayName)
                ? site.DisplayName
                : siteId;
        }
    }
}
