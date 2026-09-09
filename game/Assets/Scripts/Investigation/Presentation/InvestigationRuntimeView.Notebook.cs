using System.Collections;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private int NotebookFindingCount => ScenarioWorkspaceActive ? CountInitialFindings() : CountVisibleNotebookObservations();
        private bool notebookDrawerOpen;
        private float notebookDrawerScrollPosition = 1f;
        private string notebookFocusTargetAfterRender = string.Empty;
        private bool hypothesisSummaryExpanded;
        private string expandedHypothesisId = string.Empty;
        private bool notebookOpeningAnimationPending;
        private bool notebookHasBeenOpened;
        private bool notebookIntroductionVisible;

        private void ToggleNotebookDrawer()
        {
            // The drawer hides EDNA through its visibility rules. Keep the
            // conversation/reply state so closing resumes it only if it was open.
            notebookDrawerOpen = !notebookDrawerOpen;
            notebookIntroductionVisible = notebookDrawerOpen && !notebookHasBeenOpened;
            if (notebookDrawerOpen) notebookHasBeenOpened = true;
            notebookOpeningAnimationPending = notebookDrawerOpen;
            notebookFocusTargetAfterRender = notebookDrawerOpen
                ? "Close Notebook Drawer"
                : "Toggle Notebook Drawer";
            RefreshPresentationOnly();
        }

        private void CloseNotebookDrawer()
        {
            if (!notebookDrawerOpen) return;
            notebookDrawerOpen = false;
            notebookIntroductionVisible = false;
            notebookFocusTargetAfterRender = "Toggle Notebook Drawer";
            RefreshPresentationOnly();
        }

        private void OpenNotebookComparisons(string threatId)
        {
            hypothesisSummaryExpanded = true;
            expandedHypothesisId = threatId;
            notebookDrawerScrollPosition = 1f;
            navigationRevealTarget = $"Hypothesis Card {threatId}";
            navigationRevealAtTop = true;
            if (notebookDrawerOpen) RefreshPresentationOnly();
            else ToggleNotebookDrawer();
        }

        private void RemoveNotebookDrawer()
        {
            if (contentScroll != null) contentScroll.enabled = true;
            if (contentPanel == null) return;
            // Destroy is deferred until the end of the frame. Remove every matching
            // child so repeated renders cannot leave a newer drawer behind.
            for (int index = contentPanel.childCount - 1; index >= 0; index--)
            {
                Transform existing = contentPanel.GetChild(index);
                if (existing.name != "Notebook Drawer" && existing.name != "Notebook Drawer Scrim") continue;
                existing.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }
        }

        private void RenderNotebookDrawer()
        {
            if (!notebookDrawerOpen
                || (ComparisonWorkspaceVisible && !ObserveComplete)
                || state == null
                || (state.Phase == InvestigationPhase.Observe && CountInitialFindings() == 0 && !HasTodaySurveyNotes)
                // The completed report has its own summary, but returning to
                // Simulate must still allow the visible Notebook button to open it.
                || (state.Phase == InvestigationPhase.Report
                    && state.ConclusionStatus == InvestigationConclusionStatus.Correct && !ScenarioWorkspaceActive))
            {
                return;
            }

            CanvasGroup pageGroup = contentRoot.GetComponent<CanvasGroup>();
            if (pageGroup == null) pageGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
            pageGroup.interactable = false;
            pageGroup.blocksRaycasts = false;
            // The drawer is a sibling of the page under its ScrollRect. Pause
            // that ancestor so header scroll/drag events cannot move the page.
            if (contentScroll != null)
            {
                contentScroll.StopMovement();
                contentScroll.enabled = false;
            }

            Button scrim = CreateButton(
                "Notebook Drawer Scrim",
                contentPanel,
                string.Empty,
                ButtonVisualStyle.Tertiary,
                CloseNotebookDrawer,
                out Text scrimLabel);
            scrimLabel.gameObject.SetActive(false);
            scrim.navigation = new Navigation { mode = Navigation.Mode.None };
            scrim.GetComponent<Image>().color = new Color(0f, 0.05f, 0.08f, 0.42f);
            scrim.GetComponent<LayoutElement>().ignoreLayout = true;
            Stretch(scrim.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            RectTransform drawer = CreatePanel(
                "Notebook Drawer",
                contentPanel,
                InvestigationTheme.Paper,
                InvestigationTheme.CardRadius);
            drawer.GetComponent<Image>().raycastTarget = true;
            bool portrait = Screen.height > Screen.width;
            if (portrait) Anchor(drawer, 0.04f, 0f, 0.96f, 0.72f, 0f, 8f, 0f, 0f);
            else Anchor(drawer, 0.64f, 0f, 1f, 1f, 4f, 6f, -16f, -6f);
            EnsureOutline(drawer.gameObject, InvestigationTheme.PaperBorder, new Vector2(2f, -2f));
            Shadow shadow = drawer.gameObject.AddComponent<Shadow>();
            shadow.effectColor = InvestigationTheme.PaperShadow;
            shadow.effectDistance = new Vector2(-2f, -1f);
            shadow.useGraphicAlpha = true;
            CreateNotebookBinding(drawer);

            Text title = CreateText(
                "Notebook Drawer Title",
                drawer,
                $"MY NOTEBOOK · {NotebookFindingCount}",
                19,
                FontStyle.Bold,
                InvestigationTheme.PaperMuted,
                TextAnchor.MiddleLeft,
                InvestigationTheme.DataFont);
            Anchor(title.rectTransform, 0f, 0.89f, 0.72f, 1f, 42f, 0f, -4f, -8f);

            Text description = CreateText(
                "Notebook Drawer Description",
                drawer,
                state.Phase == InvestigationPhase.Observe || ScenarioWorkspaceActive ? "Our survey records · 20 years ago → Today"
                    : "NEW = untested · USED = compared · REPORT = selected · OPEN = unresolved",
                11,
                FontStyle.Normal,
                InvestigationTheme.PaperMuted,
                TextAnchor.MiddleLeft,
                InvestigationTheme.BodyFont);
            Anchor(description.rectTransform, 0f, 0.82f, 1f, 0.90f, 42f, 0f, -18f, 0f);

            Button close = CreateButton(
                "Close Notebook Drawer",
                drawer,
                state.Phase == InvestigationPhase.Observe && !ObserveComplete ? "Seamount view" : "Close",
                ButtonVisualStyle.PaperChoice,
                () => { if (state.Phase == InvestigationPhase.Observe && !ObserveComplete) ReturnToComparisonSeamount(); else CloseNotebookDrawer(); },
                out Text closeLabel);
            closeLabel.fontSize = 12;
            close.GetComponent<LayoutElement>().ignoreLayout = true;
            Anchor(close.GetComponent<RectTransform>(), 0.74f, 1f, 1f, 1f, 0f, -52f, -12f, -8f);

            RectTransform scrollRoot = CreatePanel("Notebook Drawer Scroll", drawer, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(scrollRoot, 0f, 0f, 1f, 0.82f, 40f, 12f, -14f, 0f);
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;

            RectTransform viewport = CreatePanel("Notebook Drawer Viewport", scrollRoot, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            RectTransform entries = new GameObject(
                "Notebook Drawer Entries",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            entries.SetParent(viewport, false);
            entries.anchorMin = new Vector2(0f, 1f);
            entries.anchorMax = new Vector2(1f, 1f);
            entries.pivot = new Vector2(0.5f, 1f);
            entries.anchoredPosition = Vector2.zero;
            entries.sizeDelta = Vector2.zero;
            VerticalLayoutGroup entriesLayout = entries.GetComponent<VerticalLayoutGroup>();
            entriesLayout.padding = new RectOffset(0, 8, 2, 8);
            entriesLayout.spacing = 0f;
            entriesLayout.childControlWidth = true;
            entriesLayout.childControlHeight = true;
            entriesLayout.childForceExpandWidth = true;
            entriesLayout.childForceExpandHeight = false;
            entries.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = entries;
            if (ScenarioWorkspaceActive)
            {
                // Carry Act 1's saved picture into Act 2 without appending the
                // retired text evidence list or per-prediction comparison UI.
                AddLayout(CreateSurveyStory(entries, "Notebook Survey Story"), 490f, 0f);
            }
            else
            {
                if (notebookIntroductionVisible) RenderNotebookIntroduction(entries);
                if (state.Phase != InvestigationPhase.Observe || state.TriedThreatIds.Count > 0) RenderHypothesisSummary(entries);
                RenderTodaySurveyNotes(entries);
                if (observeSummarySaved) AddLayout(CreateSurveyStory(entries, "Notebook Survey Story"), 490f, 0f);
                if (state.Phase != InvestigationPhase.Observe || !observeSummarySaved) RenderNotebookEntries(entries);
            }

            RectTransform scrollbarRect = CreatePanel("Notebook Drawer Scrollbar", scrollRoot, new Color32(179, 204, 218, 110), 5f);
            Anchor(scrollbarRect, 1f, 0f, 1f, 1f, -7f, 3f, 0f, -3f);
            scrollbarRect.GetComponent<Image>().raycastTarget = true;
            Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            RectTransform slidingArea = new GameObject("Sliding Area", typeof(RectTransform)).GetComponent<RectTransform>();
            slidingArea.SetParent(scrollbarRect, false);
            Stretch(slidingArea, 1f, 1f, -1f, -1f);
            RectTransform handle = CreatePanel("Handle", slidingArea, InvestigationTheme.PaperMuted, 4f);
            Stretch(handle, 0f, 0f, 0f, 0f);
            handle.GetComponent<Image>().raycastTarget = true;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 5f;
            scroll.verticalNormalizedPosition = notebookDrawerScrollPosition;

            scrim.transform.SetAsLastSibling();
            drawer.SetAsLastSibling();
        }

        private void RenderNotebookIntroduction(Transform parent)
        {
            RectTransform note = CreatePanel("Notebook Edna Introduction", parent,
                InvestigationTheme.PaperSelected, InvestigationTheme.SmallRadius);
            HorizontalLayoutGroup row = note.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 8, 10, 12);
            row.spacing = 8f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.UpperLeft;
            RectTransform words = new GameObject("Notebook Introduction Words", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
            words.SetParent(note, false);
            words.GetComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup column = words.GetComponent<VerticalLayoutGroup>();
            column.spacing = 4f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            Text name = CreateText("Notebook Edna Name", words, "EDNA", 11, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.DataFont);
            ConfigureContentDrivenText(name);
            string instruction = state.Phase == InvestigationPhase.Observe
                ? observeSummarySaved ? "Here is our survey picture. You can open it again while testing possible causes." : "Survey records are grouped by date. Your comparisons appear below. Close the notebook to return to the survey."
                : hypothesisSummaryExpanded
                ? "Select a cause with recorded checks, then a check to reopen it in the model. Close the notebook to continue investigating."
                : "Scroll below to revisit your survey findings. Use Compare causes to review the checks you save while testing models.";
            Text message = CreateText("Notebook Edna Instructions", words, instruction, 13, FontStyle.Normal,
                InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(message);
            EnsureEdnaArtwork();
            Image avatar = CreateStatusIcon("Notebook Edna Avatar", note, ednaAvatar ?? ednaPortrait, Color.white);
            LayoutElement avatarSize = avatar.gameObject.AddComponent<LayoutElement>();
            avatarSize.minWidth = avatarSize.preferredWidth = 44f;
            avatarSize.minHeight = avatarSize.preferredHeight = 44f;
        }

        private void CreateNotebookBinding(RectTransform paper)
        {
            RectTransform margin = CreatePanel("Notebook Binding Margin", paper,
                new Color32(225, 223, 212, 160), 6f);
            Anchor(margin, 0f, 0f, 0f, 1f, 0f, 12f, 19f, -12f);
            RectTransform marginRule = CreatePanel("Notebook Binding Rule", paper, InvestigationTheme.PaperRule, 0f);
            Anchor(marginRule, 0f, 0f, 0f, 1f, 32f, 16f, 33f, -16f);

            for (int index = 0; index < 6; index++)
            {
                float y = 0.86f - index * 0.14f;
                RectTransform hole = CreatePanel($"Notebook Binding Hole {index}", paper, InvestigationTheme.PaperMuted, 4f);
                Anchor(hole, 0f, y, 0f, y, 25f, -4f, 33f, 4f);

                InvestigationBorderGraphic shadow = CreateGraphic<InvestigationBorderGraphic>($"Notebook Ring Shadow {index}", paper);
                Anchor(shadow.rectTransform, 0f, y, 0f, y, 5f, -8f, 31f, 4f);
                shadow.Configure(6f, 3f);
                shadow.color = InvestigationTheme.PaperShadow;

                InvestigationBorderGraphic ring = CreateGraphic<InvestigationBorderGraphic>($"Notebook Binding Ring {index}", paper);
                Anchor(ring.rectTransform, 0f, y, 0f, y, 4f, -6f, 30f, 6f);
                ring.Configure(6f, 2.5f);
                ring.color = InvestigationTheme.PaperMuted;

                InvestigationBorderGraphic highlight = CreateGraphic<InvestigationBorderGraphic>($"Notebook Ring Highlight {index}", paper);
                Anchor(highlight.rectTransform, 0f, y, 0f, y, 5f, -4.5f, 29f, 5.5f);
                highlight.Configure(5f, 1f);
                highlight.color = new Color32(208, 224, 216, 255);
            }
        }

        private Button CreateNotebookDrawerButton(Transform parent)
        {
            Button button = CreateButton(
                "Toggle Notebook Drawer",
                parent,
                string.Empty,
                ButtonVisualStyle.Tertiary,
                ToggleNotebookDrawer,
                out Text label);
            label.gameObject.SetActive(false);
            LayoutElement size = button.GetComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 62f;
            size.minHeight = size.preferredHeight = 50f;
            size.flexibleWidth = size.flexibleHeight = 0f;
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(62f, 50f);
            CreateNotebookButtonArtwork(button);

            RectTransform badge = CreatePanel("Notebook Finding Count", button.transform, InvestigationTheme.Primary, 9f);
            Anchor(badge, 1f, 1f, 1f, 1f, -20f, -19f, -1f, 0f);
            badge.gameObject.SetActive(NotebookFindingCount > 0);
            Text count = CreateText("Notebook Finding Count Text", badge, NotebookFindingCount.ToString(),
                11, FontStyle.Bold, InvestigationTheme.OnPrimary, TextAnchor.MiddleCenter, InvestigationTheme.DataFont);
            Stretch(count.rectTransform, 1f, 1f, -1f, -1f);

            RectTransform tooltip = CreatePanel("Notebook Button Tooltip", button.transform, InvestigationTheme.Deep, InvestigationTheme.SmallRadius);
            Anchor(tooltip, 0.5f, 1f, 0.5f, 1f, -78f, 6f, 78f, 36f);
            Text hint = CreateText("Notebook Button Hint", tooltip, notebookDrawerOpen ? "Close notebook" : "Open notebook",
                12, FontStyle.Normal, InvestigationTheme.TextPrimary, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            Stretch(hint.rectTransform, 6f, 2f, -6f, -2f);
            tooltip.gameObject.SetActive(false);
            InvestigationHoverTooltipTrigger trigger = button.gameObject.AddComponent<InvestigationHoverTooltipTrigger>();
            trigger.Configure(0.3f,
                () => { if (tooltip != null) tooltip.gameObject.SetActive(true); },
                () => { if (tooltip != null) tooltip.gameObject.SetActive(false); });
            if (!notebookHasBeenOpened && ednaIntroductions.Contains("notebook-introduction"))
                AddChoiceBorderCue("Open Notebook Cue", button.transform);
            return button;
        }

        private void CreateNotebookButtonArtwork(Button button)
        {
            RectTransform artwork = CreatePanel("Notebook Button Artwork", button.transform, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(artwork, 8f, 2f, -8f, -2f);
            if (notebookDrawerOpen)
            {
                RectTransform left = CreatePanel("Notebook Left Page", artwork, InvestigationTheme.Paper, 4f);
                Anchor(left, 0f, 0.10f, 0.49f, 0.94f, 0f, 0f, 0f, 0f);
                RectTransform right = CreatePanel("Notebook Right Page", artwork, InvestigationTheme.PaperRaised, 4f);
                Anchor(right, 0.51f, 0.10f, 1f, 0.94f, 0f, 0f, 0f, 0f);
                RectTransform crease = CreatePanel("Notebook Page Crease", artwork, InvestigationTheme.PaperSelectedBorder, 0f);
                Anchor(crease, 0.48f, 0.08f, 0.52f, 0.94f, 0f, 0f, 0f, 0f);
                for (int index = 0; index < 3; index++)
                {
                    float y = 0.65f - index * 0.17f;
                    CreateNotebookRule(left, index, y);
                    CreateNotebookRule(right, index, y);
                }
                button.targetGraphic = right.GetComponent<Image>();
                return;
            }

            artwork.localRotation = Quaternion.Euler(0f, 0f, -6f);
            RectTransform pages = CreatePanel("Notebook Page Edges", artwork, InvestigationTheme.PaperRaised, 4f);
            Anchor(pages, 0.10f, 0.02f, 0.96f, 0.94f, 0f, 0f, 0f, 0f);
            RectTransform cover = CreatePanel("Notebook Cover", artwork, InvestigationTheme.Paper, 5f);
            Anchor(cover, 0.02f, 0.09f, 0.90f, 1f, 0f, 0f, 0f, 0f);
            AddSingleShadow(cover.gameObject, InvestigationTheme.PrimaryShadow, new Vector2(1.5f, -2f));
            RectTransform spine = CreatePanel("Notebook Spine", artwork, InvestigationTheme.PaperSelectedBorder, 2f);
            Anchor(spine, 0f, 0.09f, 0.16f, 1f, 0f, 0f, 0f, 0f);
            for (int index = 0; index < 3; index++)
            {
                CreateNotebookRule(cover, index, 0.67f - index * 0.17f);
                RectTransform binding = CreatePanel($"Notebook Binding {index}", artwork, InvestigationTheme.Primary, 2f);
                float y = 0.30f + index * 0.23f;
                Anchor(binding, 0f, y, 0.18f, y, -2f, -1f, 0f, 2f);
            }
            RectTransform bookmark = CreatePanel("Notebook Bookmark", artwork, InvestigationTheme.Accent, 1f);
            Anchor(bookmark, 0.64f, 0.77f, 0.76f, 1.03f, 0f, 0f, 0f, 0f);
            button.targetGraphic = cover.GetComponent<Image>();
        }

        private static void CreateNotebookRule(RectTransform parent, int index, float y)
        {
            RectTransform rule = CreatePanel($"Notebook Cover Rule {index}", parent, InvestigationTheme.PaperMuted, 0f);
            Anchor(rule, 0.29f, y, index == 2 ? 0.64f : 0.80f, y, 0f, 0f, 0f, 1f);
        }

        private IEnumerator AnimateNotebookOpening()
        {
            RectTransform drawer = FindNamedRect(contentPanel, "Notebook Drawer");
            if (drawer == null || InvestigationMotionSettings.ReducedMotion) yield break;
            CanvasGroup opacity = EnsureCanvasGroup(drawer);
            Vector2 target = drawer.anchoredPosition;
            Vector2 offset = Screen.height > Screen.width ? new Vector2(0f, -12f) : new Vector2(12f, 0f);
            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                if (drawer == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                opacity.alpha = Mathf.Lerp(0.45f, 1f, eased);
                drawer.anchoredPosition = target + offset * (1f - eased);
                drawer.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, eased);
                yield return null;
            }
            opacity.alpha = 1f;
            drawer.anchoredPosition = target;
            drawer.localScale = Vector3.one;
        }

        private void RenderHypothesisSummary(Transform parent)
        {
            Button toggle = CreateButton("Toggle Hypothesis Summary", parent,
                hypothesisSummaryExpanded ? "Hide comparisons −" : "Compare causes +",
                ButtonVisualStyle.PaperChoice, () =>
                {
                    hypothesisSummaryExpanded = !hypothesisSummaryExpanded;
                    notebookFocusTargetAfterRender = "Toggle Hypothesis Summary";
                    RefreshPresentationOnly();
                }, out Text toggleLabel);
            ConfigureWrappingChoice(toggle, toggleLabel);
            toggle.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
            if (!hypothesisSummaryExpanded) return;

            Text explanation = CreateText("Hypothesis Summary Explanation", parent,
                "Recorded checks, not probabilities. Tap a cause for details.",
                11, FontStyle.Normal, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(explanation);
            foreach (ThreatSimulationDefinition threat in caseDefinition.Threats)
            {
                InvestigationHypothesisSummary summary = new InvestigationHypothesisSummary(caseDefinition, state, threat.ThreatId);
                bool expanded = expandedHypothesisId == threat.ThreatId;
                Button card = CreateButton($"Hypothesis Card {threat.ThreatId}", parent, string.Empty,
                    ButtonVisualStyle.PaperChoice, () =>
                    {
                        expandedHypothesisId = expandedHypothesisId == threat.ThreatId ? string.Empty : threat.ThreatId;
                        notebookFocusTargetAfterRender = $"Hypothesis Card {threat.ThreatId}";
                        RefreshPresentationOnly();
                    }, out Text hidden);
                hidden.gameObject.SetActive(false);
                AddLayout(card.GetComponent<RectTransform>(), 72f, 1f);
                card.interactable = summary.Records.Count > 0;
                card.targetGraphic.color = expanded ? InvestigationTheme.PaperSelected : InvestigationTheme.PaperRaised;
                card.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
                Image icon = CreateStatusIcon("Hypothesis Icon", card.transform,
                    threat.Icon != null ? threat.Icon : InvestigationStatusIconLibrary.Question,
                    threat.Icon != null ? Color.white : InvestigationTheme.PaperMuted);
                Anchor(icon.rectTransform, 0f, 0.5f, 0f, 0.5f, 10f, -15f, 40f, 15f);
                Text name = CreateText($"Hypothesis Name {threat.ThreatId}", card.transform, threat.DisplayName,
                    14, FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
                Anchor(name.rectTransform, 0f, 0.5f, 1f, 1f, 48f, 0f, -12f, -6f);
                string counts = summary.Records.Count > 0
                    ? $"SUPPORT {summary.SupportCount} · CHALLENGE {summary.ChallengeCount} · OPEN {summary.OpenCount}"
                    : state.HasTriedThreat(threat.ThreatId) ? "NO CHECKS YET" : "NOT TESTED";
                Text stats = CreateText($"Hypothesis Summary {threat.ThreatId}", card.transform, counts,
                    10, FontStyle.Normal, InvestigationTheme.PaperMuted, TextAnchor.MiddleLeft, InvestigationTheme.DataFont);
                Anchor(stats.rectTransform, 0f, 0f, 1f, 0.5f, 48f, 6f, -12f, 0f);
                if (!expanded) continue;

                foreach (PredictionComparisonRecord record in summary.Records)
                {
                    string label = !record.LocksComparison ? "Open"
                        : record.Judgement == ComparisonJudgement.Match ? "Supports" : "Challenges";
                    string target = caseDefinition.FindSpecies(record.TargetId)?.GameplayName ?? record.TargetId;
                    Button check = CreateButton($"Revisit Comparison {record.ThreatId} {record.TargetId}", parent,
                        $"{target} · {label}", ButtonVisualStyle.PaperChoice,
                        () => RevisitComparison(record), out Text checkLabel);
                    checkLabel.fontSize = 13;
                    ConfigureWrappingChoice(check, checkLabel);
                    check.GetComponent<InvestigationFocusRing>().KeepVisibleOnKeyboardFocus = true;
                }
            }
        }

        private void RevisitComparison(PredictionComparisonRecord record)
        {
            workbenchInspectPrediction = true;
            workbenchFoodWebReady = true;
            restingExperiments.Remove(record.ThreatId);
            selectedThreatId = record.ThreatId;
            selectedPredictionTargetKind = record.TargetKind;
            selectedPredictionSpeciesId = record.TargetId;
            selectedObservationId = record.EvidenceId;
            notebookDrawerOpen = false;
            notebookFocusTargetAfterRender = $"Prediction {record.TargetId}";
            if (state.Phase != InvestigationPhase.Simulate) setPhase?.Invoke(InvestigationPhase.Simulate);
            else RefreshPresentationOnly();
        }

        private IEnumerator FocusNotebookControlNextFrame(string objectName)
        {
            yield return null;
            if (EventSystem.current == null) yield break;
            Button target = FindInteractableButton(objectName);
            if (target == null) yield break;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}
