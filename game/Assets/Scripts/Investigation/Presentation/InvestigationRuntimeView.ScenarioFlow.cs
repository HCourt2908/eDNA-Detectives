using TMPro;
using System;
using System.Collections.Generic;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum ScenarioStage { BuildingChain, TryingCauses, PlayingCause, Comparing }
        private InvestigationState scenarioSession;
        private ScenarioStage scenarioStage;
        private readonly HashSet<string> viewedScenarios = new HashSet<string>(StringComparer.Ordinal);
        private double scenarioStarted;
        private int scenarioRun;
        private string scenarioActiveId = string.Empty;
        private string scenarioFeedback = string.Empty;
        private string scenarioConflictSpecies = string.Empty;
        private bool scenarioSubmitting;
        private const float ScenarioRecordsRevealSeconds = .85f;
        private double scenarioRecordsRevealStarted = -100d;
        private const float ScenarioSeconds = 7.2f;
        private bool ScenarioModeActive => state != null && state.Phase == InvestigationPhase.Simulate;
        private bool ScenarioWorkspaceActive => state != null && (state.Phase == InvestigationPhase.Simulate || state.Phase == InvestigationPhase.Report);
        private bool RequiredScenariosViewed
        {
            get
            {
                if (caseDefinition == null || caseDefinition.Threats.Count == 0) return false;
                foreach (var cause in caseDefinition.Threats) if (!cause.OptionalExploration && !viewedScenarios.Contains(cause.ThreatId)) return false;
                return true;
            }
        }
        private int RequiredScenarioCount
        {
            get { int count = 0; foreach (var model in caseDefinition.Threats) if (!model.OptionalExploration) count++; return count; }
        }
        private int RequiredScenariosTried
        {
            get { int count = 0; foreach (var model in caseDefinition.Threats) if (!model.OptionalExploration && state.HasTriedThreat(model.ThreatId)) count++; return count; }
        }
        private int RequiredScenariosViewedCount
        {
            get { int count = 0; foreach (var model in caseDefinition.Threats) if (!model.OptionalExploration && viewedScenarios.Contains(model.ThreatId)) count++; return count; }
        }

        private string ScenarioEdnaMessage
        {
            get
            {
                if (scenarioSession != state || scenarioStage == ScenarioStage.BuildingChain)
                    return "Our notebook is opening. Use Play on any card to test its prediction, then compare the results.";
                if (!string.IsNullOrEmpty(scenarioFeedback)) return scenarioFeedback;
                if (!string.IsNullOrEmpty(state.ProvisionalThreatId)) return "Your explanation is saved. Replay any model, then return to your summary.";
                if (scenarioStage == ScenarioStage.Comparing)
                    return "Compare the shared species with our survey. Look closely at tuna and herring before recording your conclusion.";
                if (scenarioStage == ScenarioStage.PlayingCause)
                {
                    if (IsBloomScenario(scenarioActiveId))
                        return "Extreme trial: watch the bloom spread, then the model organisms disappear. This is not the outcome of every algal bloom.";
                    return "Watch the highlighted groups: more organisms join, or some leave. Every trial starts with the same groups; these symbols show relative change, not actual population counts.";
                }
                return "Use Play on each card. Watch the groups change, then review how the explanations compare with our survey.";
            }
        }

        private void EnsureScenarioSession()
        {
            if (ReferenceEquals(scenarioSession, state)) return;
            scenarioSession = state;
            viewedScenarios.Clear();
            foreach (var cause in caseDefinition.Threats) if (state.HasTriedThreat(cause.ThreatId)) viewedScenarios.Add(cause.ThreatId);
            scenarioStage = viewedScenarios.Count == 0 ? ScenarioStage.BuildingChain
                : RequiredScenariosViewed ? ScenarioStage.Comparing : ScenarioStage.TryingCauses;
            scenarioStarted = Time.unscaledTimeAsDouble;
            scenarioRecordsRevealStarted = viewedScenarios.Count == 0 && !InvestigationMotionSettings.ReducedMotion ? scenarioStarted : -100d;
            if (scenarioRecordsRevealStarted >= 0d) scenarioStarted += ScenarioRecordsRevealSeconds;
            scenarioActiveId = state.ActiveThreatId;
            scenarioFeedback = scenarioConflictSpecies = string.Empty;
            scenarioSubmitting = false; scenarioRun++;
            ResetScenarioBriefing();
            ResetScenarioEnding();
            if (InvestigationMotionSettings.ReducedMotion && scenarioStage == ScenarioStage.BuildingChain) scenarioStage = ScenarioStage.TryingCauses;
        }

        private void StartScenario(string id)
        {
            EnsureScenarioSession();
            if (!ScenarioModeActive || ScenarioCaseClosed || scenarioSubmitting || caseDefinition.FindThreat(id) == null) return;
            CloseScenarioBriefingForAction();
            scenarioRecordsRevealStarted = -100d;
            navigationRevealTarget = "Scenario Result " + id; navigationRevealAtTop = false;
            scenarioActiveId = selectedThreatId = id;
            scenarioFeedback = scenarioConflictSpecies = string.Empty;
            scenarioStage = ScenarioStage.PlayingCause;
            scenarioStarted = Time.unscaledTimeAsDouble;
            scenarioRun++;
            runThreat?.Invoke(id);
            if (state.FindSimulation(id) == null)
            {
                scenarioStage = ScenarioStage.TryingCauses;
                scenarioFeedback = "Finish recording the survey findings before trying a cause.";
                RefreshPresentationOnly(); return;
            }
            if (InvestigationMotionSettings.ReducedMotion) FinishScenarioAnimation();
        }

        private void FinishScenarioAnimation()
        {
            if (!ScenarioModeActive || !ReferenceEquals(scenarioSession, state)) return;
            CloseScenarioBriefingForAction();
            scenarioRecordsRevealStarted = -100d;
            if (scenarioStage == ScenarioStage.BuildingChain) scenarioStage = ScenarioStage.TryingCauses;
            else if (scenarioStage == ScenarioStage.PlayingCause)
            {
                if (state.FindSimulation(scenarioActiveId) == null) return;
                viewedScenarios.Add(scenarioActiveId);
                scenarioStage = RequiredScenariosViewed ? ScenarioStage.Comparing : ScenarioStage.TryingCauses;
            }
            else return;
            scenarioRun++;
            RefreshPresentationOnly();
        }

        private void ChooseScenarioExplanation(string id)
        {
            if (!ScenarioModeActive || scenarioSubmitting || scenarioStage == ScenarioStage.PlayingCause
                || caseDefinition.FindThreat(id) == null || !viewedScenarios.Contains(id)) return;
            if (IsBloomScenario(id))
            {
                CloseScenarioBriefingForAction(); scenarioBriefedConflict = string.Empty;
                scenarioSubmitting = true;
                try
                {
                    foreach (string species in new[] { "atlantic_herring", "phytoplankton" })
                    {
                        var finding = FindObserveObservationForSpecies(species);
                        if (finding != null) compareEvidence?.Invoke(id, PredictionTargetKind.Species, species, finding.EvidenceId);
                    }
                }
                finally { scenarioSubmitting = false; }
                scenarioConflictSpecies = "atlantic_herring";
                scenarioFeedback = "This extreme trial predicts widespread losses. But our herring increased and phytoplankton stayed stable, so it does not match our survey. You can continue with the three required models.";
                RefreshPresentationOnly(); return;
            }
            if (!RequiredScenariosViewed || scenarioStage != ScenarioStage.Comparing) return;
            CloseScenarioBriefingForAction();
            scenarioBriefedConflict = string.Empty;
            if (!caseDefinition.CanReviewModel(id))
            {
                var evaluator = new PredictionComparisonEvaluator();
                foreach (var finding in caseDefinition.Observations)
                {
                    if (!InvestigationObserveEvaluator.IsInitialFinding(finding) || string.IsNullOrEmpty(finding.RelatedSpeciesId)) continue;
                    var mismatch = evaluator.Evaluate(caseDefinition, id, finding.RelatedSpeciesId, finding.EvidenceId, ComparisonJudgement.Mismatch);
                    var match = evaluator.Evaluate(caseDefinition, id, finding.RelatedSpeciesId, finding.EvidenceId, ComparisonJudgement.Match);
                    if (!mismatch.IsAccepted || match.IsAccepted) continue;
                    scenarioConflictSpecies = finding.RelatedSpeciesId;
                    var species = caseDefinition.FindSpecies(finding.RelatedSpeciesId);
                    var predicted = state.FindSimulation(id)?.FindPrediction(finding.RelatedSpeciesId);
                    scenarioFeedback = $"{caseDefinition.FindThreat(id).DisplayName}: look at {species?.GameplayName ?? finding.RelatedSpeciesId}. This model predicts {PredictionLabel(predicted?.PredictedState ?? PredictionState.Unknown).ToLowerInvariant()}, but our finding is '{finding.DisplayName}'. Try another explanation.";
                    RefreshPresentationOnly(); return;
                }
            }
            // Record all authored comparisons before opening the conclusion.
            // Reviewing an alternative is separate from supporting it as the conclusion.
            foreach (var objective in caseDefinition.InvestigationObjectives)
                if (objective.Required && (!state.HasTriedThreat(objective.ThreatId) || !state.HasDiscoveredObservation(objective.RequiredEvidenceId)))
                {
                    scenarioFeedback = "A survey record or model is still missing. Revisit the survey before choosing an explanation.";
                    RefreshPresentationOnly(); return;
                }
            scenarioFeedback = scenarioConflictSpecies = string.Empty;
            if (!string.IsNullOrEmpty(state.ProvisionalThreatId))
            {
                if (state.ConfirmationReviewed) { setFinalThreat?.Invoke(id); setPhase?.Invoke(InvestigationPhase.Report); }
                else submitProvisional?.Invoke(id);
                return;
            }
            scenarioSubmitting = true;
            foreach (var objective in caseDefinition.InvestigationObjectives)
                if (objective.Required) compareEvidence?.Invoke(objective.ThreatId, objective.TargetKind, objective.TargetId, objective.RequiredEvidenceId);
            scenarioSubmitting = false;
            if (!new InvestigationConclusionEvaluator().EvaluateReadiness(caseDefinition, state).CanEnterProvisional)
            {
                scenarioFeedback = "Some evidence is still unresolved. Review the model results before continuing.";
                RefreshPresentationOnly(); return;
            }
            submitProvisional?.Invoke(id);
        }

        private List<string> ScenarioSpecies(string threatId = null)
        {
            var model = caseDefinition.FindThreat(threatId);
            if (model != null && model.DisplaySpeciesIds.Count > 0) return new List<string>(model.DisplaySpeciesIds);
            var ids = new List<string>();
            foreach (var species in caseDefinition.Species) ids.Add(species.SpeciesId);
            foreach (string id in caseDefinition.BenthicIndicatorSpeciesIds) if (!ids.Contains(id)) ids.Add(id);
            return ids;
        }

        private void RenderScenarioComparison()
        {
            EnsureScenarioSession();
            bindScenarioPlayback = null;
            PrepareScenarioBriefing();
            // Override only this act's chrome; Observe's metrics and navigation
            // are rebuilt normally when the player returns to their records.
            metricsText.text = $"FINDINGS {CountInitialFindings()}/{InvestigationObserveEvaluator.RequiredCount(caseDefinition)}\nREQUIRED MODELS {RequiredScenariosViewedCount}/{RequiredScenarioCount}";
            var report = stageRoot.Find("Stage Report")?.GetComponent<Button>();
            if (report != null) report.interactable = !string.IsNullOrEmpty(state.ProvisionalThreatId);
            RectTransform root = CreateScenarioWorkspace();
            RenderScenarioObservedPattern(root);
            TextMeshProUGUI chain = CreateText("Scenario Food Chain", root,
                "Food chains vary by scenario · Hover or tap a title to explore its links.",
                13, FontStyle.Bold, InvestigationTheme.Primary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(chain.rectTransform, 24f, 0f);
            RenderScenarioAlternatives(root);
            if (scenarioStage == ScenarioStage.BuildingChain)
            {
                int run = scenarioRun; var session = state;
                bindScenarioPlayback += () => root.gameObject.AddComponent<InvestigationScenarioPlayback>().Configure(scenarioStarted, .15f, _ => { },
                    () => { if (run == scenarioRun && ReferenceEquals(session, state) && scenarioStage == ScenarioStage.BuildingChain) FinishScenarioAnimation(); });
            }
            Canvas.ForceUpdateCanvases();
            var guide = root.gameObject.AddComponent<InvestigationScenarioBriefingHost>();
            var bind = bindScenarioPlayback;
            guide.Configure(() => { FitScenarioWorkspace(root); bind?.Invoke(); RenderScenarioRecordsReveal(root, guide); RenderScenarioBriefing(root, guide); });
        }

        private RectTransform CreateScenarioColumn(string name, Transform parent, Color color)
        {
            RectTransform panel = CreatePanel(name, parent, color, InvestigationTheme.CardRadius);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return panel;
        }

    }
}
