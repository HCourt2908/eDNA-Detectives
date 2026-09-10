using System;
using EDNA.Core;

namespace EDNA.Investigation.Domain
{
    public sealed class InvestigationSurveySummary
    {
        public SpeciesDetectionState Detection { get; internal set; }
        public DepthBand Depth { get; internal set; }
        public string Result { get; internal set; }
        public string Source { get; internal set; }
        public string Confidence { get; internal set; }
        public InvestigationObservationDefinition Observation { get; internal set; }
    }

    // The authored case survey remains authoritative for its core findings.
    // Additional species show an explicitly identified, selected upstream record.
    public static class InvestigationSurveyEvaluator
    {
        public static InvestigationObservationDefinition FindCaseObservation(InvestigationCaseDefinition definition, string speciesId)
        {
            for (int index = 0; index < definition.Observations.Count; index++)
            {
                InvestigationObservationDefinition observation = definition.Observations[index];
                if (InvestigationObserveEvaluator.IsInitialFinding(observation)
                    && string.Equals(observation.RelatedSpeciesId, speciesId, StringComparison.Ordinal)) return observation;
            }
            return null;
        }

        public static InvestigationSurveySummary Resolve(
            InvestigationCaseDefinition definition, InvestigationState state,
            InvestigationSpeciesDefinition species, SurveyTimepoint timepoint)
        {
            if (species == null) return null;
            InvestigationObservationDefinition observation = FindCaseObservation(definition, species.SpeciesId);
            if (observation != null)
            {
                bool historical = timepoint == SurveyTimepoint.Historical;
                SpeciesDetectionState detection = historical ? SpeciesDetectionState.Detected : DetectionFor(observation);
                return new InvestigationSurveySummary
                {
                    Detection = detection,
                    Depth = species.MapDepthBand,
                    Result = historical ? "Detected in baseline" : observation.DisplayName,
                    Source = historical ? "Case baseline" : "Case survey · eDNA",
                    Confidence = historical ? string.Empty : $"{observation.Confidence} confidence",
                    Observation = historical ? null : observation
                };
            }
            InvestigationSurveySpeciesRecord record = state.FindSurveySpeciesRecord(species.SpeciesId, timepoint);
            if (record == null) return null;
            return new InvestigationSurveySummary
            {
                Detection = record.DetectionState,
                Depth = record.DepthBand,
                Result = record.DetectionState == SpeciesDetectionState.Detected ? "Detected"
                    : record.DetectionState == SpeciesDetectionState.NotDetected ? "Not detected" : "Unknown result",
                Source = $"Selected survey record · {record.Source}",
                Confidence = record.Confidence != SurveyConfidence.Unknown ? $"{record.Confidence} confidence"
                    : record.SampleQuality != SampleQuality.Unknown ? $"{record.SampleQuality} sample quality" : "Confidence not supplied"
            };
        }

        public static bool ValidateInput(InvestigationCaseDefinition definition, InvestigationGameInput input,
            InvestigationCaseRoster roster, out string feedback)
        {
            feedback = string.Empty;
            foreach (InvestigationSurveySpeciesRecord record in roster.SurveyRecords)
            {
                if (!Enum.IsDefined(typeof(DepthBand), record.DepthBand)
                    || !Enum.IsDefined(typeof(SpeciesDetectionState), record.DetectionState)
                    || !Enum.IsDefined(typeof(SurveyConfidence), record.Confidence)
                    || !Enum.IsDefined(typeof(SampleQuality), record.SampleQuality)
                    || (record.SurveyTimepoint != SurveyTimepoint.Historical && record.SurveyTimepoint != SurveyTimepoint.Current))
                {
                    feedback = "A survey record has an invalid result, depth, quality or timepoint. Supply a historical or current survey record before importing it.";
                    return false;
                }
                string site = input.surveyContext?.siteId;
                if (!string.IsNullOrWhiteSpace(site) && !string.IsNullOrWhiteSpace(record.SiteId)
                    && !string.Equals(site.Trim(), record.SiteId, StringComparison.Ordinal))
                {
                    feedback = "The survey contains a record from a different site. Check the survey site before starting this case.";
                    return false;
                }
                foreach (InvestigationSurveySpeciesRecord other in roster.SurveyRecords)
                {
                    if (!string.IsNullOrEmpty(record.SampleId) && other != record
                        && record.SpeciesId == other.SpeciesId && record.SampleId == other.SampleId
                        && record.SiteId == other.SiteId && record.SurveyTimepoint == other.SurveyTimepoint
                        && record.DetectionState != SpeciesDetectionState.Unknown && other.DetectionState != SpeciesDetectionState.Unknown
                        && record.DetectionState != other.DetectionState)
                    {
                        feedback = "The same species and sample has conflicting detection results. Check the supplied sample records before importing this survey.";
                        return false;
                    }
                }
                InvestigationObservationDefinition observation = FindCaseObservation(definition, record.SpeciesId);
                if (observation == null || record.DetectionState == SpeciesDetectionState.Unknown) continue;
                SpeciesDetectionState expected = record.SurveyTimepoint == SurveyTimepoint.Historical
                    ? SpeciesDetectionState.Detected : DetectionFor(observation);
                if (expected == SpeciesDetectionState.Unknown || record.DetectionState == expected) continue;
                string speciesName = definition.FindSpecies(record.SpeciesId)?.DisplayName ?? record.SpeciesId;
                feedback = $"This survey cannot be used with this fixed case: {speciesName} is "
                    + (record.DetectionState == SpeciesDetectionState.Detected ? "detected" : "not detected")
                    + $" in the supplied {record.SurveyTimepoint.ToString().ToLowerInvariant()} record, but the case uses a different pattern. "
                    + "Check the supplied survey, or start the standalone case with its own evidence.";
                return false;
            }
            return true;
        }

        private static SpeciesDetectionState DetectionFor(InvestigationObservationDefinition observation)
        {
            switch (observation.ClaimType)
            {
                case ObservationClaimType.NotDetected: return SpeciesDetectionState.NotDetected;
                case ObservationClaimType.ReducedDetection:
                case ObservationClaimType.NewDetection:
                case ObservationClaimType.ChangedDepthOrDistribution:
                case ObservationClaimType.MatchesBaseline: return SpeciesDetectionState.Detected;
                default: return SpeciesDetectionState.Unknown;
            }
        }
    }
}
