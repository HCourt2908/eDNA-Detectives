using System;
using System.Collections.Generic;

namespace EDNA.Investigation.Domain
{
    public sealed class EcosystemSimulatorEvaluator
    {
        public SimulationResult Evaluate(InvestigationCaseDefinition caseDefinition, string threatId)
        {
            if (caseDefinition == null) throw new ArgumentNullException(nameof(caseDefinition));
            ThreatSimulationDefinition threat = caseDefinition.FindThreat(threatId);
            if (threat == null) throw new ArgumentException("Threat is not part of this case.", nameof(threatId));

            List<SimulationPrediction> predictions = new List<SimulationPrediction>();
            for (int index = 0; index < threat.SpeciesPredictions.Count; index++)
            {
                ThreatSpeciesPredictionDefinition definition = threat.SpeciesPredictions[index];
                if (definition == null) continue;
                predictions.Add(new SimulationPrediction(
                    threat.ThreatId,
                    definition.SpeciesId,
                    definition.PredictedState,
                    definition.Rationale));
            }

            return new SimulationResult(
                threat.ThreatId,
                predictions,
                threat.TemperaturePrediction,
                threat.SeafloorPrediction,
                threat.PhysicalConfirmation);
        }
    }
}
