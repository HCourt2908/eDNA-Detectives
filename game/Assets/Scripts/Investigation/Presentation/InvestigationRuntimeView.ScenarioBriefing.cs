using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum ScenarioBriefingStep { None, Survey, Model, Causes, Result, Compare, Conflict, Ending }
        private ScenarioBriefingStep scenarioBriefingStep;
        private bool scenarioIntroBriefed, scenarioResultBriefed, scenarioCompareBriefed, scenarioBriefingSequence;
        private string scenarioBriefedConflict = string.Empty;
        private int scenarioBriefingVersion;
        private double scenarioBriefingPausedAt = -1d;
        private double ScenarioPlaybackTime => scenarioBriefingPausedAt >= 0d ? scenarioBriefingPausedAt : Time.unscaledTimeAsDouble;
        private bool ScenarioBriefingActive => ScenarioWorkspaceActive && scenarioBriefingStep != ScenarioBriefingStep.None;

        private void ResetScenarioBriefing()
        {
            scenarioBriefingStep = ScenarioBriefingStep.None;
            scenarioIntroBriefed = viewedScenarios.Count > 0;
            scenarioResultBriefed = viewedScenarios.Count > 0;
            scenarioCompareBriefed = !string.IsNullOrEmpty(state.ProvisionalThreatId);
            scenarioBriefingSequence = false; scenarioBriefedConflict = string.Empty;
            scenarioBriefingPausedAt = -1d; scenarioBriefingVersion++;
        }

        private void PrepareScenarioBriefing()
        {
            if (ScenarioBriefingActive || notebookDrawerOpen || provisionalReviewOpen || restartConfirmationPending) return;
            if (!string.IsNullOrEmpty(scenarioConflictSpecies) && scenarioFeedback != scenarioBriefedConflict)
                BeginScenarioBriefing(ScenarioBriefingStep.Conflict);
            else if (scenarioStage == ScenarioStage.Comparing && !scenarioCompareBriefed)
                BeginScenarioBriefing(ScenarioBriefingStep.Compare);
            else if (scenarioStage == ScenarioStage.TryingCauses && viewedScenarios.Count == 0 && !scenarioIntroBriefed)
                BeginScenarioBriefing(ScenarioBriefingStep.Survey, true);
            else if (scenarioStage == ScenarioStage.TryingCauses && viewedScenarios.Count > 0 && !scenarioResultBriefed)
                BeginScenarioBriefing(ScenarioBriefingStep.Result);
        }

        private void BeginScenarioBriefing(ScenarioBriefingStep step, bool sequence = false)
        {
            scenarioBriefingStep = step; scenarioBriefingSequence = sequence; scenarioBriefingVersion++;
            if (scenarioStage == ScenarioStage.BuildingChain || scenarioStage == ScenarioStage.PlayingCause)
                scenarioBriefingPausedAt = Time.unscaledTimeAsDouble;
        }

        private void OpenScenarioBriefing()
        {
            EnsureScenarioSession();
            if (ScenarioBriefingActive || notebookDrawerOpen || provisionalReviewOpen || restartConfirmationPending) return;
            if (state.Phase == EDNA.Investigation.Domain.InvestigationPhase.Report) { BeginScenarioBriefing(ScenarioBriefingStep.Ending); RefreshPresentationOnly(); return; }
            if (scenarioStage == ScenarioStage.BuildingChain) { FinishScenarioAnimation(); return; }
            BeginScenarioBriefing(!string.IsNullOrEmpty(scenarioConflictSpecies) ? ScenarioBriefingStep.Conflict
                : scenarioStage == ScenarioStage.Comparing ? ScenarioBriefingStep.Compare
                : scenarioStage == ScenarioStage.PlayingCause || scenarioStage == ScenarioStage.BuildingChain ? ScenarioBriefingStep.Model
                : viewedScenarios.Count > 0 ? ScenarioBriefingStep.Result : ScenarioBriefingStep.Causes);
            RefreshPresentationOnly();
        }

        private void CloseScenarioBriefingForAction()
        {
            if (!ScenarioBriefingActive) return;
            if (scenarioBriefingSequence) scenarioIntroBriefed = true;
            if (scenarioBriefingStep == ScenarioBriefingStep.Result) scenarioResultBriefed = true;
            if (scenarioBriefingStep == ScenarioBriefingStep.Compare) scenarioCompareBriefed = true;
            if (scenarioBriefingStep == ScenarioBriefingStep.Conflict) scenarioBriefedConflict = scenarioFeedback;
            if (scenarioBriefingPausedAt >= 0d) scenarioStarted += Time.unscaledTimeAsDouble - scenarioBriefingPausedAt;
            scenarioBriefingPausedAt = -1d;
            scenarioBriefingStep = ScenarioBriefingStep.None; scenarioBriefingSequence = false; scenarioBriefingVersion++;
        }

        private void AdvanceScenarioBriefing(int shownVersion, bool skip)
        {
            if (!ScenarioBriefingActive || shownVersion != scenarioBriefingVersion) return;
            if (!skip && scenarioBriefingSequence && scenarioBriefingStep != ScenarioBriefingStep.Causes)
            {
                scenarioBriefingStep = ScenarioBriefingStep.Causes;
                scenarioBriefingVersion++;
            }
            else CloseScenarioBriefingForAction();
            RefreshPresentationOnly();
        }

        private string ScenarioBriefingTarget => scenarioBriefingStep == ScenarioBriefingStep.Ending ? "Scenario Ending Panel" : scenarioBriefingStep == ScenarioBriefingStep.Survey ? "Scenario Survey Target"
            : scenarioBriefingStep == ScenarioBriefingStep.Causes ? "Scenario Results"
            : scenarioBriefingStep == ScenarioBriefingStep.Compare ? "Scenario Results"
            : scenarioBriefingStep == ScenarioBriefingStep.Conflict ? "Scenario Observed " + scenarioConflictSpecies : "Scenario Result " + scenarioActiveId;

        private string ScenarioBriefingWords
        {
            get
            {
                switch (scenarioBriefingStep)
                {
                    case ScenarioBriefingStep.Survey: return "I took our findings out of the notebook. Each pair compares 20 years ago with Today. Open the notebook beside me to revisit the full picture.";
                    case ScenarioBriefingStep.Model: return "Each group starts with three symbols. More symbols mean an increase; fewer mean a decrease. They are not animal counts. Sea star and mussel help us check the cause.";
                    case ScenarioBriefingStep.Causes: return "Use the Play button on any card. Its groups change right here. Play all three, then choose the best fit. Replay lets you watch a result again.";
                    case ScenarioBriefingStep.Result: return "This is what that cause predicts. Compare the changes with our recorded survey, including the control species. Try the other causes to see how they differ.";
                    case ScenarioBriefingStep.Compare: return "Now compare the three predictions with our records. Sea star and mussel help separate similar explanations. Replay any cause, then choose the best fit.";
                    case ScenarioBriefingStep.Conflict: return scenarioFeedback;
                    case ScenarioBriefingStep.Ending: return ScenarioEndingMessage;
                    default: return string.Empty;
                }
            }
        }

        private static Rect ScenarioGuideBounds(RectTransform rect, RectTransform space)
        {
            var points = new Vector3[4]; rect.GetWorldCorners(points);
            Vector2 min = space.InverseTransformPoint(points[0]), max = space.InverseTransformPoint(points[2]);
            return new Rect(min, max - min);
        }
        private static void PositionScenarioGuide(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = bounds.center; rect.sizeDelta = bounds.size;
        }

        private void RenderScenarioBriefing(RectTransform root, InvestigationScenarioBriefingHost host)
        {
            if (!ScenarioBriefingActive || notebookDrawerOpen || provisionalReviewOpen || restartConfirmationPending || !root.gameObject.activeInHierarchy) return;
            RectTransform target = FindNamedRect(root, ScenarioBriefingTarget);
            if (target == null) return;
            Canvas.ForceUpdateCanvases();
            RectTransform canvas = GetComponent<Canvas>().rootCanvas.GetComponent<RectTransform>();
            RectTransform overlay = CreatePanel("Scenario Briefing Overlay", canvas, Color.clear, 0f);
            Stretch(overlay, 0f, 0f, 0f, 0f); overlay.SetAsLastSibling();
            Rect original = ScenarioGuideBounds(target, overlay);
            host.Own(overlay);
            // Highlight the live card through a clear window; do not duplicate
            // animated sprites or change the workbench's transform/hierarchy.
            RectTransform shield = CreatePanel("Scenario Briefing Dimmer", overlay, new Color(0f, 0f, 0f, .001f), 0f);
            Stretch(shield, 0f, 0f, 0f, 0f); shield.GetComponent<Image>().raycastTarget = true;
            Rect screen = overlay.rect;
            Color shade = new Color(.08f, .09f, .10f, .78f);
            void Shade(string name, Rect bounds)
            {
                if (bounds.width <= 0f || bounds.height <= 0f) return;
                PositionScenarioGuide(CreatePanel(name, overlay, shade, 0f), bounds);
            }
            Shade("Scenario Shade Top", new Rect(screen.xMin, original.yMax, screen.width, screen.yMax - original.yMax));
            Shade("Scenario Shade Bottom", new Rect(screen.xMin, screen.yMin, screen.width, original.yMin - screen.yMin));
            Shade("Scenario Shade Left", new Rect(screen.xMin, original.yMin, original.xMin - screen.xMin, original.height));
            Shade("Scenario Shade Right", new Rect(original.xMax, original.yMin, screen.xMax - original.xMax, original.height));
            RectTransform focus = CreatePanel("Scenario Briefing Focus " + target.name, overlay, Color.clear, 0f);
            PositionScenarioGuide(focus, original);
            var border = CreateGraphic<InvestigationBorderGraphic>("Scenario Briefing Spotlight", overlay);
            border.Configure(InvestigationTheme.CardRadius, 2f); border.color = InvestigationTheme.Primary; border.raycastTarget = false;
            PositionScenarioGuide(border.rectTransform, new Rect(original.xMin - 3f, original.yMin - 3f, original.width + 6f, original.height + 6f));
            RemoveEdnaObject(ednaConversation);
            bool above = scenarioBriefingStep == ScenarioBriefingStep.Model || scenarioBriefingStep == ScenarioBriefingStep.Result
                || scenarioBriefingStep == ScenarioBriefingStep.Compare || scenarioBriefingStep == ScenarioBriefingStep.Ending;
            RenderScenarioEdnaDock(overlay, above, true);
        }
    }
}
