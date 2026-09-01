using System;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;

namespace EDNA.Investigation
{
    public readonly struct InvestigationSpeciesMapPlacement
    {
        public InvestigationSpeciesMapPlacement(Vector2 anchor, Vector2 markerSize)
        {
            Anchor = anchor;
            MarkerSize = markerSize;
        }

        public Vector2 Anchor { get; }
        public Vector2 MarkerSize { get; }
    }

    public static class InvestigationSpeciesMapLayout
    {
        private static readonly Vector2 FullMarkerSize = new Vector2(112f, 88f);
        private static readonly Vector2 MediumMarkerSize = new Vector2(96f, 78f);
        private static readonly Vector2 CompactMarkerSize = new Vector2(84f, 70f);
        private static readonly Vector2 DenseMarkerSize = new Vector2(74f, 62f);

        public static uint StableOrder(string layoutSeed, string speciesId, DepthBand depthBand, bool benthic)
        {
            return StableHash($"{layoutSeed}|{depthBand}|{(benthic ? "benthic" : "pelagic")}|{speciesId}|order");
        }

        public static InvestigationSpeciesMapPlacement Calculate(
            string layoutSeed,
            string speciesId,
            DepthBand depthBand,
            bool benthic,
            SurveyEra era,
            int slotIndex,
            int speciesCount)
        {
            int count = Mathf.Max(1, speciesCount);
            int rows = count <= 4 ? 1 : 2;
            int columns = Mathf.CeilToInt(count / (float)rows);
            int safeIndex = Mathf.Clamp(slotIndex, 0, count - 1);
            int row = safeIndex / columns;
            int indexInRow = safeIndex % columns;
            int rowStart = row * columns;
            int rowCount = Mathf.Min(columns, count - rowStart);

            uint seed = StableHash($"{layoutSeed}|{speciesId}|{depthBand}");
            float eraOffset = era == SurveyEra.Historical ? -0.006f : 0.006f;
            float x = Mathf.Clamp(CalculateX(seed, benthic, indexInRow, rowCount) + eraOffset, 0.10f, 0.90f);
            float y = CalculateY(seed, depthBand, benthic, row, rows, era);
            return new InvestigationSpeciesMapPlacement(
                new Vector2(x, y),
                MarkerSizeForCount(count));
        }

        public static DepthBand ResolveDepthBand(InvestigationSpeciesDefinition species)
        {
            return species == null ? DepthBand.Mid : species.MapDepthBand;
        }

        public static bool IsBenthic(InvestigationSpeciesDefinition species)
        {
            if (species == null) return false;
            if (species.GlyphKind == SpeciesGlyphKind.SeaStar || species.GlyphKind == SpeciesGlyphKind.Mussel)
                return true;
            for (int index = 0; index < species.SensitivityTags.Count; index++)
            {
                string tag = species.SensitivityTags[index];
                if (string.Equals(tag, "BenthicIndicator", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(tag, "FilterFeeder", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static float CalculateX(uint seed, bool benthic, int indexInRow, int rowCount)
        {
            float minimum = benthic ? 0.32f : 0.14f;
            float maximum = benthic ? 0.74f : 0.86f;
            if (rowCount <= 1)
            {
                if (benthic) return Mathf.Lerp(0.40f, 0.68f, Unit(seed));
                return Unit(seed) < 0.5f ? 0.20f : 0.80f;
            }

            float t = indexInRow / (float)(rowCount - 1);
            float jitter = SignedUnit(seed ^ 0x9E3779B9u) * (rowCount <= 3 ? 0.026f : 0.012f);
            return Mathf.Clamp(Mathf.Lerp(minimum, maximum, t) + jitter, 0.10f, 0.90f);
        }

        private static float CalculateY(uint seed, DepthBand depthBand, bool benthic, int row, int rows, SurveyEra era)
        {
            Vector2 range;
            switch (depthBand)
            {
                case DepthBand.Shallow: range = new Vector2(0.90f, 0.96f); break;
                case DepthBand.Deep: range = benthic ? new Vector2(0.20f, 0.30f) : new Vector2(0.31f, 0.36f); break;
                default: range = benthic ? new Vector2(0.48f, 0.56f) : new Vector2(0.63f, 0.72f); break;
            }

            float baseY = rows <= 1
                ? (range.x + range.y) * 0.5f
                : Mathf.Lerp(range.y, range.x, row / (float)(rows - 1));
            float densityJitter = SignedUnit(seed ^ 0x85EBCA6Bu) * (rows <= 1 ? 0.018f : 0.006f);
            float eraOffset = era == SurveyEra.Historical ? -0.006f : 0.006f;
            return Mathf.Clamp(baseY + densityJitter + eraOffset, range.x, range.y);
        }

        private static Vector2 MarkerSizeForCount(int count)
        {
            if (count <= 3) return FullMarkerSize;
            if (count <= 4) return MediumMarkerSize;
            if (count <= 6) return CompactMarkerSize;
            return DenseMarkerSize;
        }

        private static float Unit(uint value)
        {
            return (value & 0x00FFFFFFu) / 16777215f;
        }

        private static float SignedUnit(uint value)
        {
            return Unit(value) * 2f - 1f;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (value == null) return hash;
                for (int index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= 16777619u;
                }
                return hash;
            }
        }
    }
}
