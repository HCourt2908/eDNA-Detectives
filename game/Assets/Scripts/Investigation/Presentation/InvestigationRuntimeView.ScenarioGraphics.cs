using System;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private Action bindScenarioPlayback;
        private void RenderScenarioObservedPattern(Transform parent)
        {
            RectTransform paper = CreatePanel("Scenario Survey Target", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            AddLayout(paper, 104f, 0f);
            CreateNotebookBinding(paper);
            EnsureCanvasGroup(paper).alpha = ScenarioRecordsRevealing ? 0f : 1f;
            Text heading = CreateText("Scenario Survey Target Title", paper, "FROM MY NOTEBOOK · 20 YEARS AGO → TODAY", 12,
                FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(heading.rectTransform, 0f, 1f, 1f, 1f, 32f, -24f, -12f, -4f);
            var ids = ScenarioSpecies();
            for (int i = 0; i < ids.Count; i++)
            {
                var species = caseDefinition.FindSpecies(ids[i]);
                var finding = FindObserveObservationForSpecies(ids[i]);
                RectTransform tile = CreatePanel("Scenario Observed " + ids[i], paper, Color.clear, 0f);
                Anchor(tile, i / (float)ids.Count, 0f, (i + 1) / (float)ids.Count, 1f, 8f, 5f, -8f, -25f);
                Text name = CreateText("Observed Species", tile, species.GameplayName, 13,
                    FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperCenter, InvestigationTheme.BodyFont);
                Anchor(name.rectTransform, 0f, .75f, 1f, 1f, 0f, 0f, 0f, 0f);
                RectTransform past = CreateStorySymbols("Observed Past Artwork", tile, species, SurveyEra.Historical);
                Anchor(past, .03f, .24f, .44f, .76f, 0f, 0f, 0f, 0f);
                Text arrow = CreateText("Observed Time Arrow", tile, "→", 14, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Anchor(arrow.rectTransform, .43f, .24f, .57f, .76f, 0f, 0f, 0f, 0f);
                bool missing = ResolveSurveySummary(species, SurveyEra.Current)?.Detection == SpeciesDetectionState.NotDetected;
                if (missing)
                {
                    Text absent = CreateText("Observed Today Absence", tile, "No\nsignal", 11, FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                    Anchor(absent.rectTransform, .57f, .24f, .97f, .76f, 0f, 0f, 0f, 0f);
                }
                else
                {
                    RectTransform today = CreateStorySymbols("Observed Today Artwork", tile, species, SurveyEra.Current);
                    Anchor(today, .57f, .24f, .97f, .76f, 0f, 0f, 0f, 0f);
                }
                Text result = CreateText("Observed Change", tile, missing ? "Not detected" : finding?.ClaimType == ObservationClaimType.ChangedDepthOrDistribution ? "More sites"
                    : finding?.ClaimType == ObservationClaimType.ReducedDetection ? "Fewer sites" : "Stable", 12,
                    FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.LowerCenter, InvestigationTheme.BodyFont);
                Anchor(result.rectTransform, 0f, 0f, 1f, .25f, 0f, 0f, 0f, 0f);
                if (scenarioConflictSpecies == ids[i])
                {
                    var outline = CreateGraphic<InvestigationBorderGraphic>("Scenario Conflicting Record", tile);
                    outline.Configure(6f, 2f); outline.color = InvestigationTheme.Accent;
                    Stretch(outline.rectTransform, -2f, -2f, 2f, 2f);
                }
            }
        }

    }
}
