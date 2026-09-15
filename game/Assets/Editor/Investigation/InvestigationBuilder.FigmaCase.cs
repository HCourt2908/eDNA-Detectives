using System;
using System.Collections.Generic;
using System.Linq;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEditor;
using UnityEngine;

namespace EDNA.Investigation.Editor
{
    public static partial class InvestigationBuilder
    {
        private static readonly string[] FigmaChain = { "shark", "tuna", "atlantic_herring", "krill", "phytoplankton" };
        private static readonly string[] FigmaFindings = { "E01_SHARK_NONDETECTION", "E02_TUNA_WIDER_DETECTION",
            "E03_HERRING_FEWER_SITES", "E04_KRILL_WIDER_DETECTION", "E05_PHYTOPLANKTON_FEWER_SITES" };
        private static readonly PredictionState[] ExampleDirections = { PredictionState.Decrease, PredictionState.Increase,
            PredictionState.Decrease, PredictionState.Increase, PredictionState.Decrease };

        [MenuItem("eDNA Detectives/Update Figma Food Chain")]
        public static void UpdateFigmaFoodChain() => UpdateFigmaFoodChainCase();

        private static InvestigationCaseDefinition UpdateFigmaFoodChainCase()
        {
            EnsureFolder(DataRoot);
            ConfigureArtworkImporters();
            var shark = CreateSpecies("Species_Shark.asset", "shark", "Shark",
                "The large predator in the Figma food-chain example. Its role here is an illustrative teaching assumption.",
                SpeciesGlyphKind.Shark, new[] { DepthBand.Shallow, DepthBand.Mid, DepthBand.Deep },
                Array.Empty<string>(), Array.Empty<string>(), new[] { "FoodWeb" }, DepthBand.Shallow);
            var tuna = CreateSpecies("Species_Tuna.asset", "tuna", "Tuna",
                "The Figma example connects tuna to Atlantic herring. The model follows this complete chain rather than skipping the herring.",
                SpeciesGlyphKind.Tuna, new[] { DepthBand.Shallow, DepthBand.Mid },
                Array.Empty<string>(), Array.Empty<string>(), new[] { "FoodWeb" }, DepthBand.Shallow);
            var krill = CreateSpecies("Species_Krill.asset", "krill", "Krill",
                "Northern krill represent the zooplankton link between herring and phytoplankton in this illustrative model.",
                SpeciesGlyphKind.Krill, new[] { DepthBand.Mid, DepthBand.Deep },
                Array.Empty<string>(), Array.Empty<string>(), new[] { "FoodWeb" }, DepthBand.Mid);
            var catalog = CreateCanonicalSpeciesCatalog(shark, tuna, krill);
            var species = FigmaChain.Select(id => catalog.First(s => s.SpeciesId == id)).ToArray();
            foreach (var entry in species)
            {
                var data = new SerializedObject(entry);
                if (entry.SpeciesId == "atlantic_herring") SetString(data, "shortDisplayName", "Herring");
                if (entry.SpeciesId == "phytoplankton") SetString(data, "shortDisplayName", "Phytoplankton");
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(entry);
            }

            // Figma specifies relationships, not measured population changes. The
            // two fishing trials share the same explicit predator-removal premise;
            // the existing qualitative cascade derives the other four responses.
            var longLine = CreateThreat("Threat_LongLine.asset", "longline", "Long-line fishing",
                "Main explanation for this case. Its predator-removal model follows the five-species food chain.",
                ThreatGlyphKind.LongLine, new[] { P("shark", PredictionState.Decrease, "Assume fewer predators in this teaching trial.") },
                "No seabed observation supplied by this example.", "No physical observation is used in this activity.");
            var trawling = CreateThreat("Threat_BottomTrawling.asset", "bottom_trawling", "Bottom trawling",
                "Possible alternative. Its model can also produce a similar five-species food-chain pattern.",
                ThreatGlyphKind.BottomTrawling, new[] { P("shark", PredictionState.Decrease, "Use the same predator-removal premise to compare the two fishing models.") },
                "No seabed observation supplied by this example.", "No physical observation is used in this activity.");
            var plastic = CreateThreat("Threat_Plastic.asset", "plastic", "Plastic pollution",
                "Provisional comparison retained while the team reviews this scenario. New species responses are unknown, not invented.",
                ThreatGlyphKind.Plastic, new[] {
                    P("shark", PredictionState.Unknown, "This provisional scenario does not specify a shark response."),
                    P("tuna", PredictionState.Stable, "Retained prototype assumption; not a measured effect of plastic."),
                    P("atlantic_herring", PredictionState.Unknown, "A response has not been supplied for this species."),
                    P("krill", PredictionState.Decrease, "Retained prototype assumption pending the team's scenario review."),
                    P("phytoplankton", PredictionState.Unknown, "A response has not been supplied for this species.") },
                "No seabed observation supplied by this example.", "The team is reviewing the third scenario.");
            var threats = new[] { plastic, longLine, trawling };
            var definition = LoadOrCreate<InvestigationCaseDefinition>($"{DataRoot}/InvestigationCase_LongLine.asset");
            var so = new SerializedObject(definition);
            SetString(so, "caseId", "investigation_foodchain_02");
            SetString(so, "displayName", "A Food-chain Mystery");
            SetString(so, "briefing", "Compare an example survey with the five-species food chain. More than one cause may fit the same pattern.");
            SetSurveyContext(so, "example_survey_02", "Example survey", "seamount_a", "Seamount A", "Illustrative eDNA survey for the food-chain activity");
            SetObjectArray(so, "species", species);
            SetObjectArray(so, "speciesCatalog", catalog);
            SetStringArray(so, "foodWebChainSpeciesIds", FigmaChain);
            SetString(so, "simulationFoodWebId", "reference_main");
            SetFoodWebEdges(so);
            var edges = so.FindProperty("foodWebEdges");
            edges.DeleteArrayElementAtIndex(0); edges.DeleteArrayElementAtIndex(0);
            for (int i = 0; i < edges.arraySize; i++)
            {
                var edge = edges.GetArrayElementAtIndex(i);
                bool main = edge.FindPropertyRelative("networkId").stringValue == "reference_main";
                edge.FindPropertyRelative("caseRelevant").boolValue = main;
                if (main)
                {
                    edge.FindPropertyRelative("confidence").enumValueIndex = 0;
                    edge.FindPropertyRelative("explanation").stringValue = "Figma teaching example; a qualitative relationship, not a validated population forecast.";
                }
            }
            SetStringArray(so, "benthicIndicatorSpeciesIds", Array.Empty<string>());
            SetStringArray(so, "followUpLockedSpeciesIds", Array.Empty<string>());
            SetInteger(so, "maximumSurveySpecies", 7);
            SetObjectArray(so, "threats", threats);
            SetInteger(so, "minimumObserveDiscoveries", 5);
            SetStringArray(so, "supportedModelThreatIds", new[] { "longline", "bottom_trawling" });
            SetString(so, "correctThreatId", "longline"); // Retained only for the legacy report API.
            SetStringArray(so, "requiredComparedThreatIds", new[] { "longline", "bottom_trawling" });
            SetInteger(so, "requiredComparisonsPerThreat", 2);
            var required = so.FindProperty("requiredComparisonSpecies");
            required.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                required.GetArrayElementAtIndex(i).FindPropertyRelative("threatId").stringValue = i == 0 ? "longline" : "bottom_trawling";
                var values = required.GetArrayElementAtIndex(i).FindPropertyRelative("requiredComparisonSpeciesIds");
                values.arraySize = 1; values.GetArrayElementAtIndex(0).stringValue = "tuna";
            }
            // Retain legacy-only confirmation data for API compatibility. It is
            // never discovered or used by the active two-act conclusion.
            SetObservations(so);
            var observations = so.FindProperty("observations");
            string[] labels = { "Shark repeatedly not detected", "Tuna detected at more sites", "Herring detected at fewer sites",
                "Krill detected at more sites", "Phytoplankton detected at fewer sites" };
            for (int i = 0; i < 5; i++)
                SetObservation(observations.GetArrayElementAtIndex(i), FigmaFindings[i], labels[i],
                    "Illustrative survey record: " + labels[i] + " compared with the historical example. This is an authored learning scenario, not a field measurement.",
                    FigmaChain[i], ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty,
                    i == 0 ? EvidenceConfidence.High : EvidenceConfidence.Medium,
                    i == 0 ? ObservationClaimType.NotDetected : ExampleDirections[i] == PredictionState.Increase
                        ? ObservationClaimType.ChangedDepthOrDistribution : ObservationClaimType.ReducedDetection,
                    EvidenceCategory.FoodWeb, "The pictures compare detection patterns, not exact animal counts. Non-detection does not prove absence.");
            observations.GetArrayElementAtIndex(6).FindPropertyRelative("relatedSpeciesId").stringValue = string.Empty;
            SetStringArray(so, "confirmationEvidenceIds", new[] { "E07_FISHING_LINE", "E08_SEAFLOOR_INTACT" });
            SetInteger(so, "minimumReportEvidence", 4);
            var requirements = so.FindProperty("evidenceCategoryRequirements"); requirements.arraySize = 2;
            SetEvidenceCategoryRequirement(requirements.GetArrayElementAtIndex(0), EvidenceCategory.FoodWeb, 2);
            SetEvidenceCategoryRequirement(requirements.GetArrayElementAtIndex(1), EvidenceCategory.Confirmation, 1);
            SetInteger(so, "minimumConfirmationEvidenceInReport", 1); SetInteger(so, "minimumReportLimitations", 1);
            SetString(so, "requiredReasoningId", "food_web_cascade"); SetReasoningOptions(so); SetLimitations(so);
            SetString(so, "successFeedback", "Long-line fishing is our main explanation for this case. Bottom trawling remains a possible alternative. A model match is not proof of cause.");
            so.ApplyModifiedPropertiesWithoutUndo();
            WriteFigmaComparisonRules(definition);
            AssetDatabase.SaveAssets();
            Debug.Log("FIGMA_FOOD_CHAIN_UPDATED: five catalog species, both fishing explanations retained.");
            return definition;
        }

        private static void WriteFigmaComparisonRules(InvestigationCaseDefinition definition)
        {
            var so = new SerializedObject(definition);
            var rules = so.FindProperty("comparisonRules"); rules.arraySize = 15;
            int index = 0;
            foreach (var threat in definition.Threats)
            {
                var result = new EcosystemSimulatorEvaluator().Evaluate(definition, threat.ThreatId);
                for (int i = 0; i < FigmaChain.Length; i++)
                {
                    var rule = rules.GetArrayElementAtIndex(index++);
                    rule.FindPropertyRelative("threatId").stringValue = threat.ThreatId;
                    rule.FindPropertyRelative("speciesId").stringValue = FigmaChain[i];
                    rule.FindPropertyRelative("targetKind").intValue = (int)PredictionTargetKind.Species;
                    rule.FindPropertyRelative("targetId").stringValue = FigmaChain[i];
                    rule.FindPropertyRelative("progressRole").intValue = (int)(threat.ThreatId == "plastic" ? ComparisonProgressRole.AlternativeCauseCheck
                        : threat.ThreatId == "longline" ? ComparisonProgressRole.FoodWebCascade : i == 1 ? ComparisonProgressRole.SharedPrediction : ComparisonProgressRole.ContextOnly);
                    var candidates = new[] { i, 2, 0, 1, 3, 4 }.Distinct().Take(4).ToArray();
                    var options = rule.FindPropertyRelative("observationOptions"); options.arraySize = candidates.Length;
                    PredictionState prediction = result.FindPrediction(FigmaChain[i]).PredictedState;
                    for (int j = 0; j < candidates.Length; j++)
                    {
                        var option = options.GetArrayElementAtIndex(j);
                        option.FindPropertyRelative("evidenceId").stringValue = FigmaFindings[candidates[j]];
                        var resolutions = option.FindPropertyRelative("resolutions"); resolutions.arraySize = 3;
                        for (int k = 0; k < 3; k++)
                        {
                            var judgement = (ComparisonJudgement)k;
                            bool unknown = j != 0 || prediction == PredictionState.Unknown;
                            bool match = prediction == ExampleDirections[i];
                            bool accepted = unknown ? judgement == ComparisonJudgement.NotEnoughEvidence
                                : judgement == (match ? ComparisonJudgement.Match : ComparisonJudgement.Mismatch);
                            var resolution = resolutions.GetArrayElementAtIndex(k);
                            resolution.FindPropertyRelative("judgement").intValue = k;
                            resolution.FindPropertyRelative("outcome").intValue = (int)(accepted
                                ? !unknown && match ? ComparisonEvaluationOutcome.AcceptedWithCaveat : ComparisonEvaluationOutcome.Accepted
                                : ComparisonEvaluationOutcome.Incorrect);
                            resolution.FindPropertyRelative("feedback").stringValue = unknown
                                ? "This observation and prediction do not establish a directional comparison. Unknown does not mean stable."
                                : match ? "The direction is consistent with this example survey, but a model match does not prove a cause."
                                : "This model direction conflicts with the recorded example survey.";
                        }
                    }
                }
            }
            var objectives = so.FindProperty("investigationObjectives"); objectives.arraySize = 7;
            SetInteger(so, "minimumCompletedComparisons", 7);
            SetObjective(objectives.GetArrayElementAtIndex(0), "plastic_tuna", "alternative", "Does this provisional model match the example survey?", "plastic",
                PredictionTargetKind.Species, "tuna", FigmaFindings[1], ComparisonJudgement.Mismatch, ComparisonProgressRole.AlternativeCauseCheck);
            for (int i = 0; i < 5; i++)
                SetObjective(objectives.GetArrayElementAtIndex(i + 1), "longline_" + FigmaChain[i], "food_web", "Does this model match the full five-species pattern?", "longline",
                    PredictionTargetKind.Species, FigmaChain[i], FigmaFindings[i], ComparisonJudgement.Match, ComparisonProgressRole.FoodWebCascade);
            SetObjective(objectives.GetArrayElementAtIndex(6), "bottom_tuna", "overlap", "Can this food chain distinguish the two fishing models?", "bottom_trawling",
                PredictionTargetKind.Species, "tuna", FigmaFindings[1], ComparisonJudgement.Match, ComparisonProgressRole.SharedPrediction);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(definition);
        }
    }
}
