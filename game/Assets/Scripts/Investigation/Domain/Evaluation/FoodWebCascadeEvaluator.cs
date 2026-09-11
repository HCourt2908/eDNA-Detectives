using System;
using System.Collections.Generic;

namespace EDNA.Investigation.Domain
{
    public sealed class FoodWebCascadeEvaluator
    {
        public IReadOnlyDictionary<string, PredictionState> Evaluate(
            InvestigationCaseDefinition caseDefinition,
            string networkId,
            string seedSpeciesId,
            PredictionState seedState)
        {
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));
            InvestigationSpeciesDefinition seed = caseDefinition.FindSpecies(seedSpeciesId);
            if (seed == null) throw new ArgumentException("Seed species is not in the catalog.", nameof(seedSpeciesId));

            var states = new Dictionary<string, PredictionState>(StringComparer.Ordinal)
            {
                [seed.CanonicalSpeciesId] = seedState
            };
            var queue = new Queue<string>();
            queue.Enqueue(seed.CanonicalSpeciesId);
            while (queue.Count > 0)
            {
                string predatorId = queue.Dequeue();
                PredictionState predatorState = states[predatorId];
                for (int index = 0; index < caseDefinition.FoodWebEdges.Count; index++)
                {
                    FoodWebEdgeDefinition edge = caseDefinition.FoodWebEdges[index];
                    if (edge == null
                        || !string.Equals(edge.NetworkId, networkId, StringComparison.Ordinal)
                        || !string.Equals(edge.PredatorSpeciesId, predatorId, StringComparison.Ordinal)
                        || states.ContainsKey(edge.PreySpeciesId))
                    {
                        continue;
                    }
                    states.Add(edge.PreySpeciesId, OppositeResponse(predatorState));
                    queue.Enqueue(edge.PreySpeciesId);
                }
            }
            return states;
        }

        public void FillMissingPredictions(
            InvestigationCaseDefinition caseDefinition,
            string networkId,
            List<SimulationPrediction> predictions)
        {
            if (caseDefinition == null || predictions == null || string.IsNullOrWhiteSpace(networkId)) return;
            for (int chainIndex = 0; chainIndex < caseDefinition.FoodWebChainSpeciesIds.Count - 1; chainIndex++)
            {
                string predatorId = caseDefinition.FoodWebChainSpeciesIds[chainIndex];
                string preyId = caseDefinition.FoodWebChainSpeciesIds[chainIndex + 1];
                if (FindPrediction(predictions, preyId) != null) continue;
                SimulationPrediction predatorPrediction = FindPrediction(predictions, predatorId);
                InvestigationSpeciesDefinition predator = caseDefinition.FindSpecies(predatorId);
                InvestigationSpeciesDefinition prey = caseDefinition.FindSpecies(preyId);
                if (predatorPrediction == null || predator == null || prey == null
                    || !HasEdge(caseDefinition, networkId, predator.CanonicalSpeciesId, prey.CanonicalSpeciesId))
                {
                    continue;
                }
                predictions.Add(new SimulationPrediction(
                    predatorPrediction.ThreatId,
                    prey.SpeciesId,
                    OppositeResponse(predatorPrediction.PredictedState),
                    $"Inferred from the authored {networkId} predator–prey chain."));
            }
        }

        private static bool HasEdge(
            InvestigationCaseDefinition caseDefinition,
            string networkId,
            string predatorId,
            string preyId)
        {
            for (int index = 0; index < caseDefinition.FoodWebEdges.Count; index++)
            {
                FoodWebEdgeDefinition edge = caseDefinition.FoodWebEdges[index];
                if (edge != null
                    && string.Equals(edge.NetworkId, networkId, StringComparison.Ordinal)
                    && string.Equals(edge.PredatorSpeciesId, predatorId, StringComparison.Ordinal)
                    && string.Equals(edge.PreySpeciesId, preyId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static SimulationPrediction FindPrediction(IReadOnlyList<SimulationPrediction> predictions, string speciesId)
        {
            for (int index = 0; index < predictions.Count; index++)
                if (string.Equals(predictions[index].SpeciesId, speciesId, StringComparison.Ordinal)) return predictions[index];
            return null;
        }

        private static PredictionState OppositeResponse(PredictionState state)
        {
            switch (state)
            {
                case PredictionState.Increase: return PredictionState.Decrease;
                case PredictionState.Decrease: return PredictionState.Increase;
                case PredictionState.Stable: return PredictionState.Stable;
                default: return PredictionState.Unknown;
            }
        }
    }
}
