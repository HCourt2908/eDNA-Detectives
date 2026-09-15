using TMPro;
using System.Collections;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum ComparisonBriefingStep { Notebook, Species, Compare, Finished }
        private ComparisonBriefingStep comparisonBriefingStep;
        private RectTransform comparisonBriefingOverlay;
        private bool ComparisonBriefingActive => ComparisonWorkspaceVisible && CountInitialFindings() == 0
            && comparisonBriefingStep != ComparisonBriefingStep.Finished;

        private void ResetComparisonBriefing()
        {
            RemoveComparisonBriefingPresentation();
            comparisonBriefingStep = ComparisonBriefingStep.Notebook;
        }

        private void RemoveComparisonBriefingPresentation()
        {
            if (comparisonBriefingOverlay == null) return;
            comparisonBriefingOverlay.gameObject.SetActive(false);
            Destroy(comparisonBriefingOverlay.gameObject);
            comparisonBriefingOverlay = null;
        }

        private void AdvanceComparisonBriefing(ComparisonBriefingStep shownStep)
        {
            // A second click queued on the old button must not skip the next line.
            if (!ComparisonBriefingActive || shownStep != comparisonBriefingStep) return;
            comparisonBriefingStep++;
            if (comparisonBriefingStep == ComparisonBriefingStep.Finished)
            {
                observeComparisonFeedback = "Your turn: choose a species, check its notebook records, then click its change.";
                navigationRevealTarget = "Observe Comparison Board";
                navigationRevealAtTop = true;
            }
            RefreshPresentationOnly();
        }

        private void SkipComparisonBriefing(ComparisonBriefingStep shownStep)
        {
            if (!ComparisonBriefingActive || shownStep != comparisonBriefingStep) return;
            comparisonBriefingStep = ComparisonBriefingStep.Finished;
            observeComparisonFeedback = "Choose a species, check its notebook records, then select its change.";
            navigationRevealTarget = "Observe Comparison Board";
            navigationRevealAtTop = true;
            RefreshPresentationOnly();
        }

        private void ApplyComparisonBriefingVisibility(RectTransform edna, RectTransform species, RectTransform changes, TextMeshProUGUI feedback)
        {
            if (!ComparisonBriefingActive) return;
            EnsureCanvasGroup(edna).alpha = 0f;
            EnsureCanvasGroup(feedback.rectTransform).alpha = 0f;
            FindNamedRect(species, "Select Species Instruction").GetComponent<TextMeshProUGUI>().text = "Species from our records";
            FindNamedRect(changes, "Choose Change Instruction").GetComponent<TextMeshProUGUI>().text = "Possible changes";
            EnsureCanvasGroup(species).alpha = comparisonBriefingStep >= ComparisonBriefingStep.Species ? 1f : 0f;
            EnsureCanvasGroup(changes).alpha = comparisonBriefingStep >= ComparisonBriefingStep.Compare ? 1f : 0f;
        }

        private string ComparisonBriefingTargetName => comparisonBriefingStep == ComparisonBriefingStep.Notebook ? "Comparison Notebook"
            : comparisonBriefingStep == ComparisonBriefingStep.Species ? "Comparison Species Page" : "Comparison Changes Page";

        private string ComparisonBriefingMessage
        {
            get
            {
                switch (comparisonBriefingStep)
                {
                    case ComparisonBriefingStep.Notebook:
                        return "These are the records we just collected. The old survey is on the left and Today is on the right. We'll use this notebook to look for changes.";
                    case ComparisonBriefingStep.Species:
                        return "We've pulled out the species from those records. Each card is one species to investigate. Next, I'll show you where to put them.";
                    default:
                        return "Now compare its records across 20 years. Choose More, Fewer, Not detected or Same. Click a species and then its change, or drag it into a category.";
                }
            }
        }

        private static Rect BriefingBoundsIn(RectTransform element, RectTransform space)
        {
            var corners = new Vector3[4]; element.GetWorldCorners(corners);
            Vector2 min = space.InverseTransformPoint(corners[0]);
            Vector2 max = space.InverseTransformPoint(corners[2]);
            return new Rect(min, max - min);
        }

        private static void PositionBriefingElement(RectTransform element, Rect bounds)
        {
            element.anchorMin = element.anchorMax = new Vector2(.5f, .5f);
            element.pivot = new Vector2(.5f, .5f);
            element.anchoredPosition = bounds.center;
            element.sizeDelta = bounds.size;
        }

        private void RenderComparisonBriefingPresentation()
        {
            if (!ComparisonBriefingActive) return;
            ComparisonBriefingStep shownStep = comparisonBriefingStep;
            RenderSpotlightBriefing(ComparisonBriefingTargetName, "Comparison Briefing",
                $"EDNA · {(int)comparisonBriefingStep + 1}/3", ComparisonBriefingMessage,
                comparisonBriefingStep == ComparisonBriefingStep.Compare ? "Start comparing →" : "Next →",
                () => AdvanceComparisonBriefing(shownStep), comparisonBriefingStep == ComparisonBriefingStep.Notebook,
                () => SkipComparisonBriefing(shownStep));
        }

        private void RenderSpotlightBriefing(string targetName, string prefix, string heading, string explanation,
            string actionLabel, System.Action action, bool allowTargetInput = false, System.Action skipAction = null)
        {
            RectTransform target = FindNamedRect(contentRoot, targetName);
            if (target == null) return;
            Canvas.ForceUpdateCanvases();
            RectTransform canvas = GetComponent<Canvas>().rootCanvas.GetComponent<RectTransform>();
            comparisonBriefingOverlay = CreatePanel(prefix + " Overlay", canvas, Color.clear, 0f);
            Stretch(comparisonBriefingOverlay, 0f, 0f, 0f, 0f);
            comparisonBriefingOverlay.SetAsLastSibling();
            Rect screen = comparisonBriefingOverlay.rect;
            Rect original = BriefingBoundsIn(target, comparisonBriefingOverlay);
            RectTransform shade = CreatePanel(prefix + " Dimmer", comparisonBriefingOverlay, new Color(.08f, .09f, .10f, .78f), 0f);
            Stretch(shade, 0f, 0f, 0f, 0f);
            shade.GetComponent<Image>().raycastTarget = true;

            CanvasGroup page = EnsureCanvasGroup(contentRoot);
            page.interactable = false; page.blocksRaycasts = false;
            if (contentScroll != null) { contentScroll.StopMovement(); contentScroll.enabled = false; }

            // Lift the actual panel above the shade. A placeholder keeps all other
            // columns in place; the next normal render restores the panel hierarchy.
            Transform parent = target.parent;
            int index = target.GetSiblingIndex();
            RectTransform placeholder = new GameObject("Briefing Placeholder " + target.name, typeof(RectTransform)).GetComponent<RectTransform>();
            placeholder.SetParent(parent, false); placeholder.SetSiblingIndex(index);
            target.SetParent(comparisonBriefingOverlay, false);
            PositionBriefingElement(target, original);
            CanvasGroup focus = EnsureCanvasGroup(target);
            focus.alpha = 1f; focus.interactable = true;
            focus.blocksRaycasts = allowTargetInput;

            const float margin = 24f;
            float leftSpace = original.xMin - screen.xMin - margin * 2f;
            float rightSpace = screen.xMax - original.xMax - margin * 2f;
            bool compact = screen.width < 860f || Mathf.Max(leftSpace, rightSpace) < 340f;
            bool onLeft = leftSpace >= rightSpace;
            float width = compact ? screen.width - margin * 2f : Mathf.Min(580f, Mathf.Max(leftSpace, rightSpace));
            float portraitWidth = compact ? 112f : 148f;
            RectTransform speech = CreatePanel(prefix + " Speech", comparisonBriefingOverlay, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            TextMeshProUGUI name = CreateText(prefix + " Speaker", speech, heading, 13,
                FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            TextMeshProUGUI message = CreateText(prefix + " Message", speech, explanation, 16,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(message);
            float textWidth = width - portraitWidth - 40f;
            float textHeight = message.GetPreferredValues(message.text, textWidth, 0f).y;
            float height = Mathf.Max(compact ? 150f : 196f, textHeight + 116f);
            float x = compact || onLeft ? screen.xMin + margin : screen.xMax - margin - width;
            PositionBriefingElement(speech, new Rect(x, screen.yMin + margin, width, height));
            Anchor(name.rectTransform, 0f, 1f, 1f, 1f, portraitWidth + 24f, -36f, -16f, -10f);
            Anchor(message.rectTransform, 0f, 0f, 1f, 1f, portraitWidth + 24f, 68f, -16f, -42f);
            EnsureEdnaArtwork();
            Image portrait = CreateStatusIcon(prefix + " Portrait", speech, EdnaSpeakingArtwork, Color.white);
            Anchor(portrait.rectTransform, 0f, 0f, 0f, 1f, 3f, skipAction == null ? 0f : 64f, portraitWidth + 3f, 2f);
            Button next = CreateButton(prefix + " Next", speech, actionLabel,
                ButtonVisualStyle.PaperPrimary, () => action(), out TextMeshProUGUI nextLabel);
            nextLabel.fontSize = 14;
            next.GetComponent<LayoutElement>().ignoreLayout = true;
            float nextWidth = Mathf.Min(180f, skipAction == null ? width - portraitWidth - 32f : width - 150f);
            Anchor(next.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f, 16f, 12f, 16f + nextWidth, 56f);
            Button skip = null;
            if (skipAction != null)
            {
                skip = CreateButton(prefix + " Skip", speech, "Skip guide", ButtonVisualStyle.PaperChoice,
                    () => skipAction(), out TextMeshProUGUI skipLabel);
                skipLabel.fontSize = 13;
                skip.GetComponent<LayoutElement>().ignoreLayout = true;
                Anchor(skip.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f, 24f + nextWidth, 12f, 126f + nextWidth, 56f);
                skip.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = next, selectOnDown = skip, selectOnLeft = next, selectOnRight = skip };
            }
            next.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = next, selectOnDown = skip ?? next, selectOnLeft = next, selectOnRight = skip ?? next };

            Rect highlight = original;
            if (compact)
            {
                float availableHeight = Mathf.Max(80f, screen.height - height - margin * 3f);
                float scale = Mathf.Min(1f, (screen.width - margin * 2f) / original.width, availableHeight / original.height);
                highlight = new Rect(screen.center.x - original.width * scale * .5f,
                    screen.yMax - margin - original.height * scale, original.width * scale, original.height * scale);
                target.anchoredPosition = highlight.center;
                target.localScale = Vector3.one * scale;
            }
            InvestigationBorderGraphic border = CreateGraphic<InvestigationBorderGraphic>(prefix + " Spotlight", comparisonBriefingOverlay);
            border.Configure(InvestigationTheme.CardRadius, 2f); border.color = InvestigationTheme.Primary; border.raycastTarget = false;
            PositionBriefingElement(border.rectTransform, new Rect(highlight.xMin - 5f, highlight.yMin - 5f, highlight.width + 10f, highlight.height + 10f));
            speech.SetAsLastSibling();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
            if (!InvestigationMotionSettings.ReducedMotion) StartCoroutine(FadeComparisonBriefing(speech));
        }

        private IEnumerator FadeComparisonBriefing(RectTransform overlay)
        {
            CanvasGroup group = EnsureCanvasGroup(overlay);
            float elapsed = 0f;
            while (elapsed < .2f)
            {
                if (group == null) yield break;
                group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / .2f);
                elapsed += Time.unscaledDeltaTime; yield return null;
            }
            if (group != null) group.alpha = 1f;
        }
    }
}
