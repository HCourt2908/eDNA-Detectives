using System;
using System.Collections.Generic;
using EDNA.Core;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationCaseRosterBuilder
    {
        private sealed class Candidate
        {
            public string SpeciesId = string.Empty;
            public int Score;
            public int InputOrder;
        }

        public InvestigationCaseRoster Build(
            InvestigationCaseDefinition caseDefinition,
            InvestigationGameInput input)
        {
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));

            var rosterIds = new List<string>();
            AddCoreSpecies(caseDefinition, rosterIds);

            var records = new List<InvestigationSurveySpeciesRecord>();
            var candidates = new Dictionary<string, Candidate>(StringComparer.Ordinal);
            int inputOrder = 0;
            if (input?.ednaResults != null)
            {
                for (int resultIndex = 0; resultIndex < input.ednaResults.Count; resultIndex++)
                {
                    EDNAResultData result = input.ednaResults[resultIndex];
                    if (result == null) continue;
                    if (result.speciesObservations != null)
                    {
                        for (int observationIndex = 0; observationIndex < result.speciesObservations.Count; observationIndex++)
                        {
                            EDNASpeciesObservationData observation = result.speciesObservations[observationIndex];
                            AddObservation(caseDefinition, result, observation, records, candidates, ref inputOrder);
                        }
                    }
                    if (result.detectedSpeciesIds == null) continue;
                    for (int speciesIndex = 0; speciesIndex < result.detectedSpeciesIds.Count; speciesIndex++)
                    {
                        string identifier = result.detectedSpeciesIds[speciesIndex];
                        AddLegacyDetection(caseDefinition, result, identifier, records, candidates, ref inputOrder);
                    }
                }
            }

            var ranked = new List<Candidate>(candidates.Values);
            ranked.Sort((left, right) =>
            {
                int score = right.Score.CompareTo(left.Score);
                if (score != 0) return score;
                int order = left.InputOrder.CompareTo(right.InputOrder);
                return order != 0 ? order : string.CompareOrdinal(left.SpeciesId, right.SpeciesId);
            });
            int maximum = Math.Max(caseDefinition.MaximumSurveySpecies, rosterIds.Count);
            for (int index = 0; index < ranked.Count && rosterIds.Count < maximum; index++)
                AddUnique(rosterIds, ranked[index].SpeciesId);

            records.RemoveAll(record => !Contains(rosterIds, record.SpeciesId));
            return new InvestigationCaseRoster(rosterIds, records);
        }

        private static void AddCoreSpecies(InvestigationCaseDefinition caseDefinition, List<string> rosterIds)
        {
            for (int index = 0; index < caseDefinition.Observations.Count; index++)
            {
                InvestigationObservationDefinition observation = caseDefinition.Observations[index];
                if (observation == null || observation.UnlockStage != EvidenceUnlockStage.Observe) continue;
                InvestigationSpeciesDefinition species = caseDefinition.FindSpecies(observation.RelatedSpeciesId);
                if (species != null) AddUnique(rosterIds, species.SpeciesId);
            }
            if (rosterIds.Count > 0) return;
            for (int index = 0; index < caseDefinition.Species.Count; index++)
            {
                InvestigationSpeciesDefinition species = caseDefinition.Species[index];
                if (species != null) AddUnique(rosterIds, species.SpeciesId);
            }
        }

        private static void AddObservation(
            InvestigationCaseDefinition caseDefinition,
            EDNAResultData result,
            EDNASpeciesObservationData observation,
            List<InvestigationSurveySpeciesRecord> records,
            Dictionary<string, Candidate> candidates,
            ref int inputOrder)
        {
            if (observation == null) return;
            InvestigationSpeciesDefinition species = caseDefinition.FindSpecies(observation.speciesId);
            if (species == null) return;
            string normalizedId = species.SpeciesId;
            SampleQuality quality = observation.sampleQuality == SampleQuality.Unknown ? result.sampleQuality : observation.sampleQuality;
            records.Add(new InvestigationSurveySpeciesRecord(
                normalizedId,
                Prefer(observation.sampleId, result.sampleId),
                Prefer(observation.siteId, result.siteId),
                observation.source,
                observation.depthBand,
                observation.surveyTimepoint,
                observation.detectionState,
                observation.confidence,
                quality));
            AddCandidate(candidates, normalizedId, CandidateScore(caseDefinition, species, observation) + (int)quality - (int)observation.sampleQuality, inputOrder++);
        }

        private static void AddLegacyDetection(
            InvestigationCaseDefinition caseDefinition,
            EDNAResultData result,
            string identifier,
            List<InvestigationSurveySpeciesRecord> records,
            Dictionary<string, Candidate> candidates,
            ref int inputOrder)
        {
            InvestigationSpeciesDefinition species = caseDefinition.FindSpecies(identifier);
            if (species == null) return;
            string normalizedId = species.SpeciesId;
            if (result.speciesObservations != null)
            {
                foreach (EDNASpeciesObservationData detailed in result.speciesObservations)
                {
                    if (detailed != null && detailed.surveyTimepoint != SurveyTimepoint.Historical
                        && caseDefinition.FindSpecies(detailed.speciesId)?.SpeciesId == normalizedId) return;
                }
            }
            records.Add(new InvestigationSurveySpeciesRecord(
                normalizedId,
                result.sampleId,
                result.siteId,
                "eDNA",
                result.depthBand,
                SurveyTimepoint.Current,
                SpeciesDetectionState.Detected,
                ConfidenceFromQuality(result.sampleQuality),
                result.sampleQuality));
            var legacy = new EDNASpeciesObservationData
            {
                speciesId = identifier,
                detectionState = SpeciesDetectionState.Detected,
                confidence = ConfidenceFromQuality(result.sampleQuality),
                sampleQuality = result.sampleQuality
            };
            AddCandidate(candidates, normalizedId, CandidateScore(caseDefinition, species, legacy), inputOrder++);
        }

        private static int CandidateScore(
            InvestigationCaseDefinition caseDefinition,
            InvestigationSpeciesDefinition species,
            EDNASpeciesObservationData observation)
        {
            int score = caseDefinition.HasFoodWebConnection(species.SpeciesId) ? 100 : 0;
            if (observation.detectionState == SpeciesDetectionState.NotDetected) score += 40;
            else if (observation.detectionState == SpeciesDetectionState.Detected) score += 20;
            score += (int)observation.confidence * 4;
            score += (int)observation.sampleQuality;
            return score;
        }

        private static SurveyConfidence ConfidenceFromQuality(SampleQuality quality)
        {
            switch (quality)
            {
                case SampleQuality.High: return SurveyConfidence.High;
                case SampleQuality.Medium: return SurveyConfidence.Medium;
                case SampleQuality.Low: return SurveyConfidence.Low;
                default: return SurveyConfidence.Unknown;
            }
        }

        private static string Prefer(string candidate, string fallback)
        {
            return string.IsNullOrWhiteSpace(candidate) ? fallback ?? string.Empty : candidate.Trim();
        }

        private static void AddCandidate(
            Dictionary<string, Candidate> candidates,
            string speciesId,
            int score,
            int inputOrder)
        {
            if (!candidates.TryGetValue(speciesId, out Candidate candidate))
            {
                candidates.Add(speciesId, new Candidate { SpeciesId = speciesId, Score = score, InputOrder = inputOrder });
                return;
            }
            if (score > candidate.Score) candidate.Score = score;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (!string.IsNullOrEmpty(value) && !Contains(values, value)) values.Add(value);
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (int index = 0; index < values.Count; index++)
                if (string.Equals(values[index], value, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
