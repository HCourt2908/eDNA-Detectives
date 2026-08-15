using System;
using System.Collections.Generic;
using System.Text;
using EDNA.Investigation.Domain;

namespace EDNA.Investigation
{
    public static class InvestigationDisplayNames
    {
        public static string Classification(AnomalyClaimType claimType)
        {
            switch (claimType)
            {
                case AnomalyClaimType.NewArrival: return "New arrival";
                case AnomalyClaimType.ExpectedButMissing: return "Expected but missing";
                case AnomalyClaimType.DifferentDepth: return "Different depth";
                case AnomalyClaimType.ResultWarning: return "Result warning";
                case AnomalyClaimType.MatchesBaseline: return "Matches baseline";
                default: return FormatIdentifier(claimType.ToString());
            }
        }

        public static string EvidencePattern(string evidenceTag)
        {
            if (string.IsNullOrWhiteSpace(evidenceTag)) return "Unknown pattern";

            switch (evidenceTag)
            {
                case nameof(EvidenceType.NewDetection):
                    return Classification(AnomalyClaimType.NewArrival);
                case nameof(EvidenceType.NotDetectedInSample):
                case nameof(EvidenceType.RepeatedNonDetection):
                    return Classification(AnomalyClaimType.ExpectedButMissing);
                case nameof(EvidenceType.DepthShift):
                    return Classification(AnomalyClaimType.DifferentDepth);
                case nameof(EvidenceType.LowQualityResult):
                case nameof(EvidenceType.ContaminationWarning):
                    return Classification(AnomalyClaimType.ResultWarning);
                case nameof(EvidenceType.StableIndicator):
                case nameof(EvidenceType.RepeatedDetection):
                    return Classification(AnomalyClaimType.MatchesBaseline);
                case "FoodWeb":
                    return "Food-web species";
                default:
                    return FormatIdentifier(evidenceTag);
            }
        }

        public static string EvidencePatterns(IReadOnlyList<string> evidenceTags)
        {
            return JoinMapped(evidenceTags, EvidencePattern);
        }

        public static string Traits(IReadOnlyList<string> traits)
        {
            return JoinMapped(traits, FormatIdentifier);
        }

        public static string HypothesisStatus(HypothesisStatus status)
        {
            return FormatIdentifier(status.ToString());
        }

        public static string Confidence(EvidenceConfidence confidence)
        {
            return FormatIdentifier(confidence.ToString());
        }

        public static string ConclusionStatus(ConclusionStatus status)
        {
            return FormatIdentifier(status.ToString());
        }

        public static string FormatIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "None";

            StringBuilder words = new StringBuilder(value.Length + 8);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character == '_' || character == '-')
                {
                    if (words.Length > 0 && words[words.Length - 1] != ' ') words.Append(' ');
                    continue;
                }

                bool startsNewWord = index > 0
                    && char.IsUpper(character)
                    && (char.IsLower(value[index - 1])
                        || (index + 1 < value.Length && char.IsLower(value[index + 1])));
                if (startsNewWord && words.Length > 0 && words[words.Length - 1] != ' ') words.Append(' ');
                words.Append(character);
            }

            string result = words.ToString().Trim();
            if (result.Length == 0) return "None";
            return char.ToUpperInvariant(result[0]) + result.Substring(1).ToLowerInvariant();
        }

        private static string JoinMapped(IReadOnlyList<string> values, Func<string, string> map)
        {
            if (values == null || values.Count == 0) return "None";

            StringBuilder result = new StringBuilder();
            HashSet<string> mappedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < values.Count; index++)
            {
                string mapped = map(values[index]);
                if (string.IsNullOrWhiteSpace(mapped) || !mappedValues.Add(mapped)) continue;
                if (result.Length > 0) result.Append(", ");
                result.Append(mapped);
            }

            return result.Length == 0 ? "None" : result.ToString();
        }
    }
}
