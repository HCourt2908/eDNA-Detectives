using TMPro;
using System;
using System.Collections.Generic;
using EDNA.Investigation.Domain;
using EDNA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private float surveyLensValue;
        private bool surveyLensExplored;
        private string observeComparisonFeedback = string.Empty;

        private void ResetSurveyPresentation()
        {
            ResetObserveArrival();
            ResetObserveSummary();
            ResetComparisonBriefing();
            selectedComparisonSpecies = string.Empty; observeMapOpen = false; comparisonNotebookScrollOffset = 0f;
            surveyLensValue = 0f; surveyLensExplored = false; observeComparisonFeedback = string.Empty;
        }

        private Slider CreateWorkbenchSlider(string name, Transform parent, float value, Action<float> changed)
        {
            RectTransform track = CreatePanel(name, parent, InvestigationTheme.SurfaceRaised, InvestigationTheme.SmallRadius);
            AddLayout(track, 44f, 1f);
            Slider slider = track.gameObject.AddComponent<Slider>();
            track.GetComponent<Image>().raycastTarget = true;
            slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
            RectTransform line = CreatePanel("Track", track, InvestigationTheme.Primary, 3f);
            Anchor(line, 0f, .5f, 1f, .5f, 16f, -2f, -16f, 2f);
            RectTransform area = CreatePanel("Handle Area", track, Color.clear, 0f);
            Stretch(area, 20f, 0f, -20f, 0f);
            RectTransform handle = CreatePanel("Handle", area, InvestigationTheme.Paper, 10f);
            handle.sizeDelta = new Vector2(36f, -8f);
            handle.GetComponent<Image>().raycastTarget = true;
            slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => changed?.Invoke(v));
            return slider;
        }

        private void RenderSurveyLens(RectTransform parent)
        {
            RectTransform maps = CreatePanel("Survey Lens Maps", parent, Color.clear, 0f);
            Anchor(maps, 0f, 0f, 1f, 1f, 0f, 54f, 0f, 0f);
            CreateSurveyMap(maps, SurveyEra.Historical);
            RectTransform historical = FindNamedRect(maps, "Historical Seamount");
            Stretch(historical, 0f, 0f, 0f, 0f);
            CreateSurveyMap(maps, SurveyEra.Current);
            RectTransform current = FindNamedRect(maps, "Current Seamount");
            Stretch(current, 0f, 0f, 0f, 0f);
            // Both surveys occupy the same coordinates; the lens clips without
            // scaling either map or changing its species hit areas.
            // RectMask2D clips descendants, not the Graphic on its own object.
            // Put the opaque cover inside the mask so it reveals the baseline too.
            current.GetComponent<Image>().color = Color.clear;
            RectTransform currentWater = CreatePanel("Current Survey Water", current, InvestigationTheme.MapSurface, InvestigationTheme.CardRadius);
            Stretch(currentWater, 0f, 0f, 0f, 0f);
            currentWater.SetAsFirstSibling();
            RectMask2D mask = current.gameObject.AddComponent<RectMask2D>();
            RectTransform divider = CreatePanel("Survey Lens Divider", maps, InvestigationTheme.Paper, 0f);
            divider.GetComponent<Image>().raycastTarget = false;
            RectTransform controls = CreatePanel("Survey Lens Controls", parent, Color.clear, 0f);
            Anchor(controls, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 48f);
            Slider slider = CreateWorkbenchSlider("Survey Time Lens", controls, surveyLensValue, v => { surveyLensValue = v; UpdateHistoryRecordingPrompt(); });
            Stretch(slider.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            maps.gameObject.AddComponent<InvestigationSurveyLens>().Configure(mask, divider, slider);
            current.gameObject.AddComponent<InvestigationLensRaycastFilter>().Configure(slider);
            historical.gameObject.AddComponent<InvestigationLensRaycastFilter>().Configure(slider, true);
            FindNamedRect(current, "Survey Title").gameObject.SetActive(false);
            TextMeshProUGUI today = CreateText("Lens Today Label", maps, "Today", 18, FontStyle.Bold, InvestigationTheme.Primary,
                TextAnchor.UpperRight, InvestigationTheme.DisplayFont);
            Anchor(today.rectTransform, .7f, 1f, 1f, 1f, 0f, -44f, -14f, -10f);
            today.gameObject.SetActive(surveyLensValue < .99f);
            TextMeshProUGUI historyTitle = FindNamedRect(historical, "Survey Title").GetComponent<TextMeshProUGUI>();
            historyTitle.gameObject.SetActive(surveyLensValue > .01f);
            slider.onValueChanged.AddListener(v =>
            {
                today.gameObject.SetActive(v < .99f);
                historyTitle.gameObject.SetActive(v > .01f);
            });
            slider.interactable = CanSlideSurveyLens;
            if (CanSlideSurveyLens && !surveyLensExplored)
            {
                AddChoiceBorderCue("Survey Lens Handle Cue", slider.handleRect);
                RectTransform cue = FindNamedRect(slider.handleRect, "Survey Lens Handle Cue");
                Stretch(cue, -5f, -5f, 5f, 5f);
                float initialValue = surveyLensValue;
                slider.onValueChanged.AddListener(v =>
                {
                    if (Mathf.Abs(v - initialValue) < .04f) return;
                    surveyLensExplored = true;
                    cue.gameObject.SetActive(false);
                });
            }
            RectTransform handleArea = FindNamedRect(slider.transform, "Handle Area");
            Stretch(handleArea, 20f, 18f, -20f, -2f);
            Anchor(FindNamedRect(slider.transform, "Track"), 0f, .66f, 1f, .66f, 16f, -2f, -16f, 2f);
            TextMeshProUGUI caption = CreateText("Survey Lens Caption", slider.transform, CanSlideSurveyLens ? "Today  ←  slide to compare  →  20 years ago"
                : RecordingEra == SurveyEra.Historical ? "20 YEARS AGO · Record this survey" : "TODAY · Record this survey before comparing", 12, FontStyle.Bold,
                InvestigationTheme.TextPrimary, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Anchor(caption.rectTransform, 0f, 0f, 1f, 0f, 42f, 0f, -42f, 18f);
            caption.raycastTarget = false;
        }

        private InvestigationObservationDefinition ObserveQuestion
        {
            get
            {
                if (caseDefinition == null || state == null) return null;
                foreach (InvestigationObservationDefinition finding in caseDefinition.Observations)
                    if (InvestigationObserveEvaluator.IsInitialFinding(finding)
                        && !state.HasDiscoveredObservation(finding.EvidenceId)) return finding;
                return null;
            }
        }

    }
}
