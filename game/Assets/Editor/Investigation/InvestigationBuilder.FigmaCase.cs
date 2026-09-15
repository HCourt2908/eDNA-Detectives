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
        private static readonly string[] SurveySpecies = { "shark", "tuna", "atlantic_herring", "tree_bubblegum_coral", "phytoplankton", "krill" };
        private static readonly string[] FigmaFindings = { "E01_SHARK_FEWER_SITES", "E02_TUNA_FEWER_SITES",
            "E03_HERRING_WIDER_DETECTION", "E04_CORAL_NONDETECTION", "E05_PHYTOPLANKTON_STABLE", "E09_KRILL_WIDER_DETECTION" };
        private static readonly PredictionState[] ExampleDirections = { PredictionState.Decrease, PredictionState.Decrease,
            PredictionState.Increase, PredictionState.Absent, PredictionState.Stable, PredictionState.Increase };

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
            var species = SurveySpecies.Select(id => catalog.First(s => s.SpeciesId == id)).ToArray();
            foreach (var entry in species)
            {
                var data = new SerializedObject(entry);
                if (entry.SpeciesId == "atlantic_herring") SetString(data, "shortDisplayName", "Herring");
                if (entry.SpeciesId == "phytoplankton") SetString(data, "shortDisplayName", "Phytoplankton");
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(entry);
            }

            // Authored teaching responses: food supply does not imply a mandatory
            // change in the surface producer population. Direct trawling effects
            // are explicit rather than inferred from a single predator seed.
            var longLine = CreateThreat("Threat_LongLine.asset", "longline", "Long-line fishing",
                "A contrasting fishing model: predator removal increases tuna and reduces herring, unlike this survey.",
                ThreatGlyphKind.LongLine, new[] { P("shark", PredictionState.Decrease, "Assume fewer predators in this teaching trial."),
                    P("phytoplankton", PredictionState.Stable, "Surface phytoplankton remains unaffected in this teaching scenario."),
                    P("tree_bubblegum_coral", PredictionState.Unknown, "The team has not specified a coral response for this long-line model.") },
                "No seabed observation supplied by this example.", "No physical observation is used in this activity.");
            var trawling = CreateThreat("Threat_BottomTrawling.asset", "bottom_trawling", "Bottom trawling",
                "Best-fitting model for this case: targeted tuna and shark bycatch decline, herring increases, seabed coral is lost, and surface phytoplankton stays stable.",
                ThreatGlyphKind.BottomTrawling, new[] {
                    P("shark", PredictionState.Decrease, "Hammerheads decrease through bycatch in this scenario."),
                    P("tuna", PredictionState.Decrease, "Tuna decreases because it is targeted by the trawling scenario."),
                    P("atlantic_herring", PredictionState.Increase, "This scenario assumes reduced predation allows a herring population boom, despite the loss of coral-spawn food."),
                    P("krill", PredictionState.Unknown, "Krill is not included in this trawling food-supply model."),
                    P("phytoplankton", PredictionState.Stable, "Surface phytoplankton remains unaffected; dead organic material still sinks."),
                    P("tree_bubblegum_coral", PredictionState.Absent, "The scenario assumes seabed damage removes coral. This is a prediction of habitat loss.") },
                "Seabed damage removes coral in this teaching trial.", "Model-only response; no new field observation is supplied.");
            var plastic = CreateThreat("Threat_Plastic.asset", "plastic", "Plastic pollution",
                "Provisional comparison retained while the team reviews this scenario. New species responses are unknown, not invented.",
                ThreatGlyphKind.Plastic, new[] {
                    P("shark", PredictionState.Unknown, "This provisional scenario does not specify a shark response."),
                    P("tuna", PredictionState.Stable, "Retained prototype assumption; not a measured effect of plastic."),
                    P("atlantic_herring", PredictionState.Unknown, "A response has not been supplied for this species."),
                    P("krill", PredictionState.Decrease, "Retained prototype assumption pending the team's scenario review."),
                    P("phytoplankton", PredictionState.Unknown, "A response has not been supplied for this species."),
                    P("tree_bubblegum_coral", PredictionState.Unknown, "A coral response has not been supplied for this provisional model.") },
                "No seabed observation supplied by this example.", "The team is reviewing the plastic-pollution scenario.");
            var bloom = CreateThreat("Threat_ToxicAlgalBloom.asset", "toxic_algal_bloom", "Toxic algal bloom",
                "Extreme trial: the five model species disappear locally. This is an authored comparison, not a universal outcome of algal blooms.",
                ThreatGlyphKind.AlgalBloom, new[] {
                    P("shark", PredictionState.Absent, "This extreme trial assumes local loss of sharks after a severe bloom."),
                    P("tuna", PredictionState.Absent, "This extreme trial assumes local loss of tuna after a severe bloom."),
                    P("atlantic_herring", PredictionState.Absent, "This extreme trial assumes local loss of herring. Our survey instead recorded more herring."),
                    P("krill", PredictionState.Absent, "This extreme trial assumes local loss of krill; our survey instead recorded more krill."),
                    P("phytoplankton", PredictionState.Absent, "The assumed loss refers to our representative species, Prochlorococcus marinus, not all phytoplankton or the bloom-forming algae."),
                    P("tree_bubblegum_coral", PredictionState.Unknown, "A coral response has not been supplied for this extreme trial.") },
                "No seabed response supplied for this trial.", "A teaching scenario, not a field observation.");
            ConfigureScenario(bloom, false, FigmaChain, true);
            var bloomData = new SerializedObject(bloom);
            bloomData.FindProperty("icon").objectReferenceValue = catalog.First(entry => entry.SpeciesId == "phytoplankton").Icon;
            bloomData.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(bloom);
            ConfigureScenario(longLine, true, FigmaChain);
            ConfigureScenario(plastic, true, FigmaChain);
            ConfigureScenario(trawling, false, SurveySpecies.Take(5).ToArray());
            var coral = catalog.First(entry => entry.SpeciesId == "tree_bubblegum_coral");
            var coralData = new SerializedObject(coral); SetString(coralData, "shortDisplayName", "Coral");
            coralData.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(coral);
            var threats = new[] { plastic, longLine, trawling, bloom };
            var definition = LoadOrCreate<InvestigationCaseDefinition>($"{DataRoot}/InvestigationCase_LongLine.asset");
            var so = new SerializedObject(definition);
            SetString(so, "caseId", "investigation_foodchain_05");
            SetString(so, "displayName", "A Food-chain Mystery");
            SetString(so, "briefing", "Compare an example survey with the fishing models. Review the alternative and find the model that best matches our records.");
            SetSurveyContext(so, "example_survey_05", "Example survey", "seamount_a", "Seamount A", "Illustrative eDNA survey for the food-chain activity");
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
            SetInteger(so, "minimumObserveDiscoveries", SurveySpecies.Length);
            SetStringArray(so, "supportedModelThreatIds", new[] { "bottom_trawling" });
            SetStringArray(so, "requiredModelReviewIds", new[] { "bottom_trawling", "longline" });
            SetString(so, "correctThreatId", "bottom_trawling"); // Best-fitting model in the revised teaching case.
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
            observations.InsertArrayElementAtIndex(5);
            string[] labels = { "Shark detected at fewer sites", "Tuna detected at fewer sites", "Herring detected at more sites",
                "Coral not detected today", "Phytoplankton remains stable", "Krill detected at more sites" };
            for (int i = 0; i < SurveySpecies.Length; i++)
                SetObservation(observations.GetArrayElementAtIndex(i), FigmaFindings[i], labels[i],
                    "Illustrative survey record: " + labels[i] + " compared with the historical example. This is an authored learning scenario, not a field measurement.",
                    SurveySpecies[i], ObservationSource.EDNA, EvidenceUnlockStage.Observe, string.Empty,
                    i == 0 ? EvidenceConfidence.High : EvidenceConfidence.Medium,
                    ExampleDirections[i] == PredictionState.Absent ? ObservationClaimType.NotDetected : ExampleDirections[i] == PredictionState.Increase
                        ? ObservationClaimType.ChangedDepthOrDistribution : ExampleDirections[i] == PredictionState.Stable
                        ? ObservationClaimType.MatchesBaseline : ObservationClaimType.ReducedDetection,
                    EvidenceCategory.FoodWeb, "The pictures compare detection patterns, not exact animal counts. Non-detection does not prove absence.");
            observations.GetArrayElementAtIndex(7).FindPropertyRelative("relatedSpeciesId").stringValue = string.Empty;
            SetStringArray(so, "confirmationEvidenceIds", new[] { "E07_FISHING_LINE", "E08_SEAFLOOR_INTACT" });
            SetInteger(so, "minimumReportEvidence", 4);
            var requirements = so.FindProperty("evidenceCategoryRequirements"); requirements.arraySize = 2;
            SetEvidenceCategoryRequirement(requirements.GetArrayElementAtIndex(0), EvidenceCategory.FoodWeb, 2);
            SetEvidenceCategoryRequirement(requirements.GetArrayElementAtIndex(1), EvidenceCategory.Confirmation, 1);
            SetInteger(so, "minimumConfirmationEvidenceInReport", 1); SetInteger(so, "minimumReportLimitations", 1);
            SetString(so, "requiredReasoningId", "food_web_cascade"); SetReasoningOptions(so); SetLimitations(so);
            SetString(so, "successFeedback", "Bottom trawling best fits our survey: fewer sharks and tuna, more herring, coral not detected and stable phytoplankton. Long-line fishing predicts the opposite tuna and herring changes. A model match is not proof of cause.");
            so.ApplyModifiedPropertiesWithoutUndo();
            WriteFigmaComparisonRules(definition);
            AssetDatabase.SaveAssets();
            Debug.Log("FIGMA_FOOD_CHAIN_UPDATED: trawling survey, historical coral and two required model reviews.");
            return definition;
        }

        private static void ConfigureScenario(ThreatSimulationDefinition threat, bool cascade, string[] display, bool optional = false)
        {
            var data = new SerializedObject(threat);
            data.FindProperty("useFoodWebCascade").boolValue = cascade;
            data.FindProperty("optionalExploration").boolValue = optional;
            SetStringArray(data, "displaySpeciesIds", display);
            var links = data.FindProperty("foodSupplyLinks"); links.arraySize = threat.ThreatId == "bottom_trawling" ? 4 : 0;
            if (links.arraySize > 0)
            {
                string[] source = { "phytoplankton", "tree_bubblegum_coral", "atlantic_herring", "tuna" };
                string[] consumer = { "tree_bubblegum_coral", "atlantic_herring", "tuna", "shark" };
                for (int i = 0; i < links.arraySize; i++)
                {
                    var link = links.GetArrayElementAtIndex(i);
                    link.FindPropertyRelative("sourceSpeciesId").stringValue = source[i];
                    link.FindPropertyRelative("consumerSpeciesId").stringValue = consumer[i];
                    link.FindPropertyRelative("kind").intValue = i == 0 ? (int)ScenarioFoodLinkKind.SinkingOrganicMatter
                        : i == 1 ? (int)ScenarioFoodLinkKind.CoralSpawn : (int)ScenarioFoodLinkKind.Feeding;
                }
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(threat);
        }

        private static void WriteFigmaComparisonRules(InvestigationCaseDefinition definition)
        {
            var so = new SerializedObject(definition);
            var rules = so.FindProperty("comparisonRules"); rules.arraySize = definition.Threats.Count * SurveySpecies.Length;
            int index = 0;
            foreach (var threat in definition.Threats)
            {
                var result = new EcosystemSimulatorEvaluator().Evaluate(definition, threat.ThreatId);
                for (int i = 0; i < SurveySpecies.Length; i++)
                {
                    var rule = rules.GetArrayElementAtIndex(index++);
                    rule.FindPropertyRelative("threatId").stringValue = threat.ThreatId;
                    rule.FindPropertyRelative("speciesId").stringValue = SurveySpecies[i];
                    rule.FindPropertyRelative("targetKind").intValue = (int)PredictionTargetKind.Species;
                    rule.FindPropertyRelative("targetId").stringValue = SurveySpecies[i];
                    rule.FindPropertyRelative("progressRole").intValue = (int)((threat.ThreatId == "plastic" || threat.OptionalExploration) ? ComparisonProgressRole.AlternativeCauseCheck
                        : threat.ThreatId == "bottom_trawling" ? ComparisonProgressRole.FoodWebCascade : i == 1 ? ComparisonProgressRole.AlternativeCauseCheck : ComparisonProgressRole.ContextOnly);
                    var candidates = new[] { i, 2, 0, 1, 3, 4 }.Distinct().Take(4).ToArray();
                    var options = rule.FindPropertyRelative("observationOptions"); options.arraySize = candidates.Length;
                    PredictionState prediction = result.FindPrediction(SurveySpecies[i]).PredictedState;
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
                SetObjective(objectives.GetArrayElementAtIndex(i + 1), "trawling_" + SurveySpecies[i], "food_web", "Does this model fit the comparable survey findings?", "bottom_trawling",
                    PredictionTargetKind.Species, SurveySpecies[i], FigmaFindings[i], ComparisonJudgement.Match, ComparisonProgressRole.FoodWebCascade);
            SetObjective(objectives.GetArrayElementAtIndex(6), "longline_tuna", "contrast", "Does the long-line prediction agree with fewer tuna in our survey?", "longline",
                PredictionTargetKind.Species, "tuna", FigmaFindings[1], ComparisonJudgement.Mismatch, ComparisonProgressRole.AlternativeCauseCheck);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(definition);
        }
    }
}
