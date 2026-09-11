using System;
using UnityEngine;

namespace EDNA.Investigation.Domain
{
    public enum FoodWebRelationshipStrength
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum FoodWebRelationshipConfidence
    {
        Provisional = 0,
        Supported = 1,
        Confirmed = 2
    }

    [Serializable]
    public sealed class FoodWebEdgeDefinition
    {
        [SerializeField] private string edgeId = string.Empty;
        [SerializeField] private string networkId = string.Empty;
        [SerializeField] private string predatorSpeciesId = string.Empty;
        [SerializeField] private string preySpeciesId = string.Empty;
        [SerializeField] private FoodWebRelationshipStrength strength = FoodWebRelationshipStrength.Medium;
        [SerializeField] private FoodWebRelationshipConfidence confidence = FoodWebRelationshipConfidence.Provisional;
        [SerializeField] private bool caseRelevant;
        [SerializeField, TextArea(1, 3)] private string explanation = string.Empty;

        public string EdgeId => edgeId;
        public string NetworkId => networkId;
        public string PredatorSpeciesId => predatorSpeciesId;
        public string PreySpeciesId => preySpeciesId;
        public FoodWebRelationshipStrength Strength => strength;
        public FoodWebRelationshipConfidence Confidence => confidence;
        public bool CaseRelevant => caseRelevant;
        public string Explanation => explanation;
    }
}
