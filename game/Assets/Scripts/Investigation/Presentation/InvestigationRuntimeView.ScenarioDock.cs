using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        // Reserve the same space throughout Act 2, including while the guide is
        // temporarily above the model. Opening EDNA never resizes the workbench.
        private float ScenarioDockHeight => GetComponent<Canvas>().rootCanvas.GetComponent<RectTransform>().rect.width < 900f ? 136f : 118f;
        private bool ScenarioRecordsRevealing => scenarioRecordsRevealStarted >= 0d
            && Time.unscaledTimeAsDouble < scenarioRecordsRevealStarted + ScenarioRecordsRevealSeconds;

        private void RenderScenarioEdnaDock(RectTransform parent = null, bool above = false, bool guide = false)
        {
            if (!ScenarioWorkspaceActive) return;
            EnsureEdnaArtwork();
            if (parent == null) parent = GetComponent<Canvas>().rootCanvas.GetComponent<RectTransform>();
            ednaConversation = CreatePanel("Scenario EDNA Dock", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            ednaConversation.GetComponent<Image>().raycastTarget = true;
            float height = ScenarioDockHeight;
            if (above) Anchor(ednaConversation, 0f, 1f, 1f, 1f, 12f, -height - 8f, -12f, -8f);
            else Anchor(ednaConversation, 0f, 0f, 1f, 0f, 12f, 8f, -12f, height + 8f);
            bool narrow = parent.rect.width < 900f;
            float portrait = narrow ? 60f : 72f;
            float tools = narrow ? 188f : 218f;
            float textRight = portrait + tools + 32f;
            string step = guide && scenarioBriefingSequence ? $" · {(scenarioBriefingStep == ScenarioBriefingStep.Survey ? 1 : 2)}/2" : string.Empty;
            Text speaker = CreateText("Scenario Briefing Speaker", ednaConversation, "EDNA" + step, 12,
                FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(speaker.rectTransform, 0f, 1f, 1f, 1f, 16f, -26f, -textRight, -4f);
            string message = guide ? ScenarioBriefingWords : notebookDrawerOpen
                ? "Your full survey picture is in the notebook. Close it when you're ready to carry on."
                : ScenarioRecordsRevealing ? "Let's take our findings out of the notebook and put them on the workbench."
                : state.Phase == InvestigationPhase.Report ? ScenarioEndingMessage : ScenarioEdnaMessage;
            Text words = CreateText("Scenario Briefing Message", ednaConversation, message, 13,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(words);
            Anchor(words.rectTransform, 0f, 0f, 1f, 1f, 16f, 50f, -textRight, -28f);
            Image person = CreateStatusIcon("Scenario Briefing Portrait", ednaConversation, ednaPortrait, Color.white);
            Anchor(person.rectTransform, 1f, 0f, 1f, 1f, -portrait, 0f, -3f, 2f);

            RectTransform actions = CreatePanel("Scenario EDNA Tools", ednaConversation, Color.clear, 0f);
            Anchor(actions, 1f, .5f, 1f, .5f, -portrait - tools - 12f, -29f, -portrait - 12f, 29f);
            Button notebook = CreateNotebookDrawerButton(actions);
            notebook.GetComponent<LayoutElement>().ignoreLayout = true;
            Anchor(notebook.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f, 0f, 0f, 58f, 0f);
            Button action = null;
            if (state.Phase == InvestigationPhase.Report) action = CreateScenarioEndingDockAction(actions);
            else if (!guide && (scenarioStage == ScenarioStage.BuildingChain || scenarioStage == ScenarioStage.PlayingCause))
                action = CreateButton("Finish Scenario Animation", actions, scenarioStage == ScenarioStage.BuildingChain ? "Skip intro →" : "Skip animation →",
                    ButtonVisualStyle.PaperPrimary, FinishScenarioAnimation, out _);
            else if (!string.IsNullOrEmpty(state.ProvisionalThreatId))
                action = CreateButton("Return To Scenario Report", actions, "Continue →", ButtonVisualStyle.PaperPrimary,
                    () => { CloseScenarioBriefingForAction(); notebookDrawerOpen = false; setPhase?.Invoke(InvestigationPhase.Report); }, out _);
            if (action != null)
            {
                action.GetComponent<LayoutElement>().ignoreLayout = true;
                Anchor(action.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, 66f, 5f, 0f, -5f);
                action.GetComponentInChildren<Text>().fontSize = narrow ? 12 : 14;
            }

            if (guide)
            {
                int version = scenarioBriefingVersion;
                string label = scenarioBriefingSequence ? scenarioBriefingStep == ScenarioBriefingStep.Causes ? "Start trying →" : "Next →" : "Continue →";
                Button next = CreateButton("Scenario Briefing Next", ednaConversation, label, ButtonVisualStyle.PaperPrimary,
                    () => AdvanceScenarioBriefing(version, false), out Text nextLabel);
                nextLabel.fontSize = 14; next.GetComponent<LayoutElement>().ignoreLayout = true;
                Anchor(next.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f, 16f, 6f, 146f, 44f);
                Button skip = CreateButton("Scenario Briefing Skip", ednaConversation, "Skip guide", ButtonVisualStyle.PaperChoice,
                    () => AdvanceScenarioBriefing(version, true), out Text skipLabel);
                skipLabel.fontSize = 13; skip.GetComponent<LayoutElement>().ignoreLayout = true;
                Anchor(skip.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f, 154f, 6f, 252f, 44f);
                // All live guide controls, including the notebook, are reachable by keyboard.
                next.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = notebook, selectOnRight = skip, selectOnDown = skip, selectOnUp = notebook };
                skip.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = next, selectOnRight = notebook, selectOnUp = next, selectOnDown = notebook };
                notebook.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = skip, selectOnRight = action != null ? action : next, selectOnUp = skip, selectOnDown = next };
                if (action != null) action.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = notebook, selectOnRight = next, selectOnDown = next, selectOnUp = notebook };
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
            }
            else
            {
                bool ending = state.Phase == InvestigationPhase.Report;
                Button help = CreateButton(ending ? "Scenario Compare Again" : "Talk To Edna", ednaConversation,
                    ending ? "Compare again" : "Show me where →", ButtonVisualStyle.PaperChoice,
                    () => { if (ending) ReturnToScenarioModels(); else OpenScenarioBriefing(); }, out Text label);
                help.interactable = !notebookDrawerOpen && state.ConclusionStatus != InvestigationConclusionStatus.Correct;
                label.fontSize = 13; help.GetComponent<LayoutElement>().ignoreLayout = true;
                Anchor(help.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f, 16f, 6f, 190f, 44f);
            }
        }

        private void RenderScenarioRecordsReveal(RectTransform root, InvestigationScenarioBriefingHost host)
        {
            if (!ScenarioRecordsRevealing || ScenarioBriefingActive || notebookDrawerOpen) return;
            RectTransform paper = FindNamedRect(root, "Scenario Survey Target");
            RectTransform notebook = FindNamedRect(ednaConversation, "Toggle Notebook Drawer");
            if (paper == null || notebook == null) return;
            RectTransform canvas = GetComponent<Canvas>().rootCanvas.GetComponent<RectTransform>();
            RectTransform flight = CreatePanel("Scenario Notebook Unfold", canvas, Color.clear, 0f);
            Stretch(flight, 0f, 0f, 0f, 0f);
            Rect end = ScenarioGuideBounds(paper, flight), start = ScenarioGuideBounds(notebook, flight);
            RectTransform sheet = host.Snapshot(paper, flight, 1f);
            sheet.name = "Scenario Findings In Flight";
            PositionScenarioGuide(sheet, end); sheet.sizeDelta = paper.rect.size;
            flight.gameObject.AddComponent<InvestigationScenarioPlayback>().Configure(scenarioRecordsRevealStarted, ScenarioRecordsRevealSeconds,
                progress =>
                {
                    float t = Mathf.SmoothStep(0f, 1f, progress);
                    sheet.anchoredPosition = Vector2.Lerp(start.center, ScenarioGuideBounds(paper, flight).center, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 28f;
                    sheet.localScale = Vector3.one * Mathf.Lerp(start.width / paper.rect.width, end.width / paper.rect.width, t);
                }, host.ClearPresentation);
        }
    }
}
