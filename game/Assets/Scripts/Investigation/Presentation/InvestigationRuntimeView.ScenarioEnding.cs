using TMPro;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private bool scenarioEndingSubmitting;
        private float ScenarioMinimumWidth => ScenarioModeActive ? (ScenarioUsesTwoColumns ? 720f : 1200f) : 1000f;
        private double scenarioClosedAt = -100d;
        private void ResetScenarioEnding() { scenarioEndingSubmitting = false; scenarioClosedAt = -100d; }
        private bool ScenarioCaseClosed => state.ConclusionStatus == InvestigationConclusionStatus.Correct;
        private string ScenarioChosenCause => caseDefinition.FindThreat(string.IsNullOrEmpty(state.FinalThreatId) ? state.ProvisionalThreatId : state.FinalThreatId)?.DisplayName ?? "Your explanation";
        private string MissingScenarioReview
        {
            get
            {
                foreach (string id in caseDefinition.RequiredModelReviewIds)
                    if (!state.HasReviewedModel(id)) return id;
                return string.Empty;
            }
        }
        private string ScenarioEndingMessage => ScenarioCaseClosed
            ? "Conclusion recorded: bottom trawling best fits our records. Long-line fishing was checked and did not match the tuna and herring changes."
            : !string.IsNullOrEmpty(MissingScenarioReview)
                ? $"You reviewed {ScenarioChosenCause}. Use Compare again to examine {caseDefinition.FindThreat(MissingScenarioReview)?.DisplayName} before recording your conclusion."
                : "Both models reviewed. Bottom trawling best fits: fewer tuna, more herring and coral not detected today. Long-line fishing predicts the opposite tuna and herring changes. Record your conclusion when ready.";
        private bool CanRecordScenarioConclusion
        {
            get
            {
                string chosen = caseDefinition.PrimaryModelThreatId;
                return recordModelConclusion != null && !scenarioEndingSubmitting
                    && new InvestigationConclusionEvaluator().CanRecordModelConclusion(caseDefinition, state, chosen);
            }
        }
        private void RecordScenarioConclusion()
        {
            if (state.Phase != InvestigationPhase.Report || !CanRecordScenarioConclusion) return;
            CloseScenarioBriefingForAction(); notebookDrawerOpen = false;
            scenarioEndingSubmitting = true;
            try
            {
                scenarioClosedAt = Time.unscaledTimeAsDouble;
                recordModelConclusion?.Invoke();
            }
            finally { scenarioEndingSubmitting = false; }
            if (!ScenarioCaseClosed) RefreshPresentationOnly();
        }

        private RectTransform CreateScenarioWorkspace()
        {
            Canvas.ForceUpdateCanvases();
            contentScroll.StopMovement(); contentScroll.vertical = false;
            if (contentScroll.verticalScrollbar != null) contentScroll.verticalScrollbar.gameObject.SetActive(false);
            RectTransform viewport = CreatePanel("Scenario Workspace", contentRoot, Color.clear, 0f);
            AddLayout(viewport, Mathf.Max(80f, contentScroll.viewport.rect.height - 1f), 0f);
            RectTransform root = CreateScenarioColumn("Scenario Investigation", viewport, Color.clear);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, 1f);
            root.sizeDelta = new Vector2(Mathf.Max(ScenarioMinimumWidth, contentScroll.viewport.rect.width), 0f);
            return root;
        }

        private void FitScenarioWorkspace(RectTransform root)
        {
            RectTransform viewport = (RectTransform)root.parent;
            float width = Mathf.Max(ScenarioMinimumWidth, viewport.rect.width);
            root.localScale = Vector3.one;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            float height = Mathf.Max(1f, LayoutUtility.GetPreferredHeight(root));
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            float scale = Mathf.Min(1f, viewport.rect.width / width, viewport.rect.height / height);
            root.localScale = Vector3.one * scale;
            root.anchoredPosition = Vector2.zero;
        }

        private void ReturnToScenarioModels()
        {
            if (ScenarioCaseClosed) return;
            CloseScenarioBriefingForAction(); notebookDrawerOpen = false;
            scenarioStage = ScenarioStage.Comparing; scenarioRun++;
            setPhase?.Invoke(InvestigationPhase.Simulate);
        }

        private Button CreateScenarioEndingDockAction(Transform parent)
        {
            if (ScenarioCaseClosed)
                return CreateButton("Restart Completed Case", parent, "Investigate again", ButtonVisualStyle.PaperPrimary, () => restart?.Invoke(), out _);
            Button complete = CreateButton("Complete Scenario Investigation", parent, "Record conclusion", ButtonVisualStyle.PaperPrimary, RecordScenarioConclusion, out _);
            complete.interactable = CanRecordScenarioConclusion;
            return complete;
        }

        private void RenderScenarioEnding()
        {
            EnsureScenarioSession();
            metricsText.text = $"FINDINGS {CountInitialFindings()}/{InvestigationObserveEvaluator.RequiredCount(caseDefinition)}\n{(ScenarioCaseClosed ? "INVESTIGATION COMPLETE" : $"REVIEWS {state.ReviewedModelThreatIds.Count}/{caseDefinition.RequiredModelReviewIds.Count}")}";
            RectTransform root = CreateScenarioWorkspace();
            RenderScenarioObservedPattern(root);
            RenderScenarioChosenModel(root);
            RectTransform panel = CreatePanel("Scenario Ending Panel", root, InvestigationTheme.Deep, InvestigationTheme.CardRadius);
            AddLayout(panel, 268f, 0f);
            if (ScenarioCaseClosed) RenderScenarioClosed(panel);
            else RenderScenarioConclusion(panel);
            var host = root.gameObject.AddComponent<InvestigationScenarioBriefingHost>();
            host.Configure(() => { FitScenarioWorkspace(root); RenderScenarioBriefing(root, host); });
        }

        private void RenderScenarioChosenModel(Transform root)
        {
            RectTransform model = CreatePanel("Scenario Chosen Model", root, InvestigationTheme.SurfaceRaised, InvestigationTheme.CardRadius);
            AddLayout(model, 60f, 0f);
            TextMeshProUGUI selected = CreateText("Scenario Chosen Explanation", model, ((string.IsNullOrEmpty(state.FinalThreatId) ? state.ProvisionalThreatId : state.FinalThreatId) == caseDefinition.PrimaryModelThreatId
                ? "MAIN EXPLANATION\n" : "ALTERNATIVE CHECKED\n") + ScenarioChosenCause, 15,
                FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(selected.rectTransform, 0f, 0f, .30f, 1f, 14f, 6f, -8f, -6f);
            string cause = string.IsNullOrEmpty(state.FinalThreatId) ? state.ProvisionalThreatId : state.FinalThreatId;
            var ids = ScenarioSpecies(cause);
            for (int i = 0; i < ids.Count; i++)
            {
                var species = caseDefinition.FindSpecies(ids[i]);
                RectTransform group = CreatePanel("Scenario Chosen Group " + ids[i], model, Color.clear, 0f);
                Anchor(group, .30f + i * .70f / ids.Count, 0f, .30f + (i + 1) * .70f / ids.Count, 1f, 3f, 4f, -3f, -4f);
                TextMeshProUGUI name = CreateText("Chosen Species", group, species.GameplayName, 11,
                    FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperCenter, InvestigationTheme.BodyFont);
                Anchor(name.rectTransform, 0f, .62f, 1f, 1f, 0f, 0f, 0f, 0f);
                int count = ScenarioPopulationCount(state.FindSimulation(cause)?.FindPrediction(ids[i])?.PredictedState ?? PredictionState.Unknown);
                for (int unit = 0; unit < count; unit++)
                {
                    Image art = CreateStatusIcon("Chosen Specimen " + unit, group, species.Icon, Color.white);
                    float first = (5 - count) * .1f;
                    Anchor(art.rectTransform, first + unit * .2f, 0f, first + (unit + 1) * .2f, .60f, 0f, 0f, 0f, 0f);
                }
            }
        }

        private void RenderScenarioConclusion(RectTransform panel)
        {
            TextMeshProUGUI title = CreateText("Scenario Evidence Heading", panel, "WHAT OUR INVESTIGATION SHOWS", 17,
                FontStyle.Bold, InvestigationTheme.Primary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(title.rectTransform, 0f, 1f, 1f, 1f, 18f, -36f, -18f, -6f);
            CreateScenarioFindingSummary(panel, "survey", "What we found", "Shark and tuna: fewer sites. Herring and krill: more sites. Coral: not detected. Phytoplankton: stable.", 46f);
            CreateScenarioFindingSummary(panel, "controls", "Best fit", "Bottom trawling fits the fish changes and coral non-detection, with phytoplankton stable.", 110f);
            CreateScenarioFindingSummary(panel, "model", "Other explanation",
                "Long-line fishing predicts more tuna and fewer herring, unlike our records.", 174f);
            AddScenarioReviewStatus(panel, "controls", caseDefinition.PrimaryModelThreatId);
            AddScenarioReviewStatus(panel, "model", "longline");
            TextMeshProUGUI note = CreateText("Scenario Conclusion Limit", panel, "A model match is not proof of cause. Non-detection does not prove absence.", 13,
                FontStyle.Bold, InvestigationTheme.TextSecondary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(note.rectTransform, 0f, 0f, 1f, 0f, 18f, 5f, -18f, 31f);
        }
        private void CreateScenarioFindingSummary(RectTransform panel, string evidenceId, string heading, string explanation, float top)
        {
            RectTransform note = CreatePanel("Scenario Finding Summary " + evidenceId, panel, InvestigationTheme.Paper, InvestigationTheme.SmallRadius);
            Anchor(note, 0f, 1f, 1f, 1f, 18f, -top - 56f, -18f, -top);
            TextMeshProUGUI label = CreateText("Summary Finding", note, heading, 16, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(label.rectTransform, 0f, 0f, .31f, 1f, 14f, 8f, -10f, -8f);
            TextMeshProUGUI detail = CreateText("Summary Meaning", note, explanation, 14, FontStyle.Bold,
                InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(detail.rectTransform, .32f, 0f, 1f, 1f, 0f, 8f, -14f, -8f);
        }
        private void AddScenarioReviewStatus(RectTransform panel, string key, string threatId)
        {
            RectTransform row = FindNamedRect(panel, "Scenario Finding Summary " + key);
            row.Find("Summary Meaning").GetComponent<RectTransform>().offsetMax = new Vector2(-164f, -8f);
            bool reviewed = state.HasReviewedModel(threatId);
            RectTransform badge = CreatePanel("Review Status Badge " + threatId, row,
                reviewed ? InvestigationTheme.PaperSelected : InvestigationTheme.PaperRaised, InvestigationTheme.SmallRadius);
            Anchor(badge, 1f, 0f, 1f, 1f, -152f, 8f, -12f, -8f);
            badge.GetComponent<Image>().raycastTarget = false;
            TextMeshProUGUI status = CreateText("Review Status " + threatId, badge,
                reviewed ? "Reviewed" : "Awaiting review", 12, FontStyle.Bold,
                reviewed ? InvestigationTheme.PaperSelectedBorder : InvestigationTheme.PaperMuted,
                TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Stretch(status.rectTransform, reviewed ? 24f : 6f, 2f, -6f, -2f);
            if (reviewed)
            {
                Image check = CreateStatusIcon("Review Status Check", badge, InvestigationStatusIconLibrary.Check, InvestigationTheme.PaperSelectedBorder);
                Anchor(check.rectTransform, 0f, .5f, 0f, .5f, 8f, -7f, 22f, 7f);
            }
        }

        private void RenderScenarioClosed(RectTransform panel)
        {
            Image stamp = CreateStatusIcon("Scenario Case Stamp", panel, InvestigationStatusIconLibrary.Check, InvestigationTheme.Primary);
            Anchor(stamp.rectTransform, .43f, .58f, .57f, .91f, 0f, 0f, 0f, 0f);
            if (!InvestigationMotionSettings.ReducedMotion && scenarioClosedAt >= 0d && Time.unscaledTimeAsDouble - scenarioClosedAt < .55d)
                stamp.gameObject.AddComponent<InvestigationScenarioPlayback>().Configure(scenarioClosedAt, .55f,
                    t => stamp.rectTransform.localScale = Vector3.one * (Mathf.Lerp(.6f, 1f, t) + Mathf.Sin(t * Mathf.PI) * .2f), null);
            TextMeshProUGUI title = CreateText("Scenario Case Closed", panel, "CONCLUSION RECORDED", 26, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleCenter, InvestigationTheme.DisplayFont);
            Anchor(title.rectTransform, 0f, .36f, 1f, .59f, 16f, 0f, -16f, 0f);
            TextMeshProUGUI summary = CreateText("Scenario Closed Summary", panel, "Best fit: bottom trawling. Long-line fishing was checked; its tuna and herring predictions do not match our records.", 18,
                FontStyle.Bold, InvestigationTheme.Primary, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Anchor(summary.rectTransform, 0f, .12f, 1f, .35f, 24f, 0f, -24f, 0f);
        }
    }
}
