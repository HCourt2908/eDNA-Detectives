using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed partial class InvestigationRuntimeView : MonoBehaviour
    {
        private enum ButtonVisualStyle { Primary, PaperPrimary, Secondary, Tertiary, Stage, Choice, PaperChoice, Danger }

        private readonly struct FocusSnapshot
        {
            public FocusSnapshot(bool hadFocus, string objectName)
            {
                HadFocus = hadFocus;
                ObjectName = objectName ?? string.Empty;
            }

            public bool HadFocus { get; }
            public string ObjectName { get; }
        }

        private static readonly Vector2 LandscapeReferenceResolution = new Vector2(1280f, 720f);
        private static readonly Vector2 PortraitReferenceResolution = new Vector2(720f, 1280f);
        private const float OuterMargin = 8f;
        private const float ContentTopInset = 122f;

        [SerializeField] private Sprite seamountSprite;

        private InvestigationCaseDefinition caseDefinition;
        private InvestigationState state;
        private Action<InvestigationPhase> setPhase;
        private Action<string> discoverObservation;
        private Action<string> runThreat;
        private Action<string, PredictionTargetKind, string, string> compareEvidence;
        private Action<string> submitProvisional;
        private Action<string> setFinalThreat;
        private Action recordModelConclusion;
        private Action restart;

        private RectTransform stageRoot;
        private TextMeshProUGUI caseSubtitleText;
        private TextMeshProUGUI metricsText;
        private RectTransform statusPanelRoot;
        private TextMeshProUGUI statusText;
        private Image statusAccent;
        private RectTransform contentPanel;
        private RectTransform contentRoot;
        private ScrollRect contentScroll;
        private RectTransform footerRoot;
        private RectTransform footerLeft;
        private RectTransform footerRight;
        private Button restartButton;

        private string selectedThreatId = string.Empty;
        private string navigationRevealTarget = string.Empty;
        private bool navigationRevealAtTop;
        private string pendingTappedSpeciesId = string.Empty;
        private bool pendingTappedSpeciesHistorical;
        private RectTransform pendingTappedSpeciesMarker;
        private InvestigationSpeciesDefinition pendingTappedSpecies;
        private RectTransform speciesTooltip;
        private string statusMessage = string.Empty;
        private string observeLayoutSessionSeed = Guid.NewGuid().ToString("N");
        private InvestigationStatusTone statusTone = InvestigationStatusTone.Guide;
        private bool built;
        private bool viewportRefreshScheduled;
        private Vector2 lastViewportSize = new Vector2(-1f, -1f);
        private bool restartConfirmationPending;
        private bool hasRenderedPhase;
        private InvestigationPhase lastRenderedPhase;
        private InvestigationConclusionStatus lastRenderedConclusionStatus = InvestigationConclusionStatus.NotSubmitted;

        public InvestigationState State => state;
        public RectTransform ContentRoot => contentRoot;
        public Sprite SeamountSprite => seamountSprite;

        public void Bind(
            InvestigationCaseDefinition definition,
            Action<InvestigationPhase> onSetPhase,
            Action<string> onDiscoverObservation,
            Action<string> onRunThreat,
            Action<string, PredictionTargetKind, string, string> onCompareEvidence,
            Action<string> onSubmitProvisional,
            Action<string> onSetFinalThreat,
            Action onRestart,
            Action onRecordModelConclusion = null)
        {
            caseDefinition = definition;
            setPhase = onSetPhase;
            discoverObservation = onDiscoverObservation;
            runThreat = onRunThreat;
            compareEvidence = onCompareEvidence;
            submitProvisional = onSubmitProvisional;
            setFinalThreat = onSetFinalThreat;
            recordModelConclusion = onRecordModelConclusion;
            restart = onRestart;
            EnsureUi();
        }

        public void ResetPresentationState()
        {
            CloseScenarioDetail(false);
            RemoveRestartDialog();
            notebookFoodWebReferenceOpen = false;
            restartPausedAt = -1d;
            seamountBackdrop?.RestartCycle();
            ResetSurveyPresentation();
            observeLayoutSessionSeed = Guid.NewGuid().ToString("N");
            selectedThreatId = string.Empty;
            navigationRevealTarget = string.Empty;
            navigationRevealAtTop = false;
            pendingTappedSpeciesId = string.Empty;
            pendingTappedSpeciesMarker = null;
            pendingTappedSpecies = null;
            HideSpeciesTooltip();
            lastRecordedObservationId = string.Empty;
            notebookDrawerOpen = false;
            notebookOpeningAnimationPending = false;
            notebookHasBeenOpened = false;
            notebookIntroductionVisible = false;
            hypothesisSummaryExpanded = false;
            expandedHypothesisId = string.Empty;
            notebookDrawerScrollPosition = 1f;
            notebookFocusTargetAfterRender = string.Empty;
            restartConfirmationPending = false;
            hasRenderedPhase = false;
            lastRenderedConclusionStatus = InvestigationConclusionStatus.NotSubmitted;
            contentScroll?.StopMovement();
            if (contentScroll != null) contentScroll.verticalNormalizedPosition = 1f;
        }

        public void Refresh(InvestigationState currentState, string message, InvestigationStatusTone tone)
        {
            state = currentState;
            if (state == null || state.ConclusionStatus == InvestigationConclusionStatus.NotSubmitted)
                statusMessage = message ?? string.Empty;
            statusTone = tone;
            EnsureUi();
            RenderAll();
        }

        public void ShowFatalError(string message, Action startStandalone = null)
        {
            EnsureUi();
            state = null;
            ResetPresentationState();
            StopAllCoroutines();
            CancelTodayRecordingVisuals();
            RemoveObserveSummaryFlight();
            viewportRefreshScheduled = false;
            ResetPageEntranceVisuals();
            RemoveNotebookDrawer();
            RemoveEdnaPresentation();
            RemoveComparisonBriefingPresentation();
            statusMessage = message ?? "Unknown Investigation error.";
            statusTone = InvestigationStatusTone.Warning;
            RenderChrome();
            Clear(contentRoot);
            TextMeshProUGUI error = CreateText("Fatal Error", contentRoot, statusMessage, 21, FontStyle.Bold, InvestigationTheme.Danger, TextAnchor.UpperLeft, InvestigationTheme.DisplayFont);
            AddLayout(error.rectTransform, 120f, 1f);
            Clear(footerLeft);
            Clear(footerRight);
            if (startStandalone != null)
            {
                Button standalone = CreateButton("Start Standalone Case", contentRoot, "Start standalone case", ButtonVisualStyle.Primary, () => startStandalone(), out _);
                AddLayout(standalone.GetComponent<RectTransform>(), 48f, 1f);
            }
        }

        private void Awake()
        {
            EnsureUi();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!built) return;
            ConfigureCanvasForCurrentViewport();
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) return;
            Vector2 currentSize = root.rect.size;
            if ((currentSize - lastViewportSize).sqrMagnitude < 0.25f) return;
            lastViewportSize = currentSize;
            if (!Application.isPlaying || state == null || viewportRefreshScheduled) return;
            viewportRefreshScheduled = true;
            StartCoroutine(RefreshAfterViewportChange());
        }

        private void EnsureUi()
        {
            if (built) return;
            built = true;
            BuildUi();
            EnsureEventSystem();
        }

        private void BuildUi()
        {
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            ConfigureCanvasForCurrentViewport();
            lastViewportSize = rootRect.rect.size;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            RectTransform background = CreatePanel("Deep Sea Background", transform, InvestigationTheme.Background, 0f);
            Stretch(background, 0f, 0f, 0f, 0f);

            // Water uses a layered background. All decorative layers are ordered
            // before Safe Area so the interface remains above the water effects.
            InvestigationWaterColumnGraphic waterColumn =
                CreateGraphic<InvestigationWaterColumnGraphic>("Water Column", background);
            Stretch(waterColumn.rectTransform, 0f, 0f, 0f, 0f);
            waterColumn.color = Color.white;

            InvestigationGodRayGraphic godRays = CreateGraphic<InvestigationGodRayGraphic>("God Rays", background);
            Stretch(godRays.rectTransform, 0f, 0f, 0f, 0f);
            godRays.color = Color.white;

            CreateMarineSnowLayer(
                "Marine Snow Far",
                background,
                18,
                new Vector2(1f, 2.2f),
                4f,
                new Vector2(0.04f, InvestigationTheme.MarineSnowFarMaxAlpha),
                6f,
                0x9E3779B9u);
            CreateMarineSnowLayer(
                "Marine Snow",
                background,
                12,
                new Vector2(2f, 3.4f),
                9f,
                new Vector2(0.06f, InvestigationTheme.MarineSnowNearMaxAlpha),
                11f,
                0x85EBCA6Bu);

            InvestigationVignetteGraphic vignette = CreateGraphic<InvestigationVignetteGraphic>("Water Vignette", background);
            Stretch(vignette.rectTransform, 0f, 0f, 0f, 0f);
            vignette.color = Color.white;

            RectTransform safeAreaRoot = CreatePanel("Safe Area", background, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(safeAreaRoot, 0f, 0f, 0f, 0f);
            safeAreaRoot.gameObject.AddComponent<InvestigationSafeAreaFitter>();

            // All water particles sit behind the reading and interaction surfaces.
            CreateMarineSnowLayer(
                "Marine Snow Large",
                background,
                4,
                new Vector2(2.5f, 4f),
                8f,
                new Vector2(0.02f, InvestigationTheme.MarineSnowLargeMaxAlpha),
                16f,
                0xC2B2AE35u);
            background.Find("Marine Snow Large").SetSiblingIndex(safeAreaRoot.GetSiblingIndex());

            RectTransform header = CreatePanel("Investigation Header", safeAreaRoot, InvestigationTheme.Deep, InvestigationTheme.SmallRadius);
            Anchor(header, 0f, 1f, 1f, 1f, OuterMargin, -112f, -OuterMargin, -OuterMargin);

            TextMeshProUGUI brand = CreateText("Brand", header, "Ecosystem Detective", 22, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.DisplayFont);
            Anchor(brand.rectTransform, 0f, 1f, 0.58f, 1f, 16f, -36f, 0f, -2f);
            caseSubtitleText = CreateText("Case Subtitle", header, string.Empty, 11, FontStyle.Normal, InvestigationTheme.TextMuted, TextAnchor.LowerLeft, InvestigationTheme.DataFont);
            Anchor(caseSubtitleText.rectTransform, 0f, 1f, 0.68f, 1f, 16f, -58f, 0f, -41f);

            metricsText = CreateText("Metrics", header, "", 11, FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.MiddleRight, InvestigationTheme.DataFont);
            Anchor(metricsText.rectTransform, 0.36f, 1f, 1f, 1f, 8f, -40f, -156f, -6f);

            CreateHeaderRestart(header);

            stageRoot = CreatePanel("Stage Navigation", header, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(stageRoot, 0.18f, 0f, 0.82f, 0f, 12f, 4f, -12f, 46f);
            HorizontalLayoutGroup stageLayout = stageRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            stageLayout.spacing = 8f;
            stageLayout.childControlWidth = true;
            stageLayout.childControlHeight = true;
            stageLayout.childForceExpandWidth = true;
            stageLayout.childForceExpandHeight = true;

            statusPanelRoot = CreatePanel("Status Toast", safeAreaRoot, new Color32(14, 51, 72, 252), InvestigationTheme.SmallRadius);
            Anchor(statusPanelRoot, 0.16f, 1f, 0.84f, 1f, 0f, -168f, 0f, -126f);
            statusAccent = CreatePanel("Status Accent", statusPanelRoot, InvestigationTheme.Danger, 0f).GetComponent<Image>();
            Anchor(statusAccent.rectTransform, 0f, 0f, 0f, 1f, 0f, 0f, 6f, 0f);
            statusText = CreateText("Status Message", statusPanelRoot, "", 14, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(statusText.rectTransform, 0f, 0f, 1f, 1f, 18f, 0f, -12f, 0f);
            statusPanelRoot.gameObject.SetActive(false);

            contentPanel = CreatePanel("Investigation Content", safeAreaRoot, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(contentPanel, 0f, 0f, 1f, 1f, OuterMargin, 84f, -OuterMargin, -ContentTopInset);
            contentScroll = contentPanel.gameObject.AddComponent<ScrollRect>();
            contentScroll.horizontal = false;
            contentScroll.vertical = true;
            contentScroll.movementType = ScrollRect.MovementType.Clamped;
            contentScroll.scrollSensitivity = 24f;
            RectTransform viewport = CreatePanel("Viewport", contentPanel, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(viewport, 0f, 0f, 0f, 0f);
            // A transparent Graphic still needs to receive raycasts so wheel and drag
            // input reaches the ScrollRect while the pointer is over empty content.
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            contentScroll.viewport = viewport;

            RectTransform scrollbarRect = CreatePanel("Vertical Scrollbar", contentPanel, new Color32(14, 51, 72, 190), 7f);
            Anchor(scrollbarRect, 1f, 0f, 1f, 1f, -12f, 6f, -2f, -6f);
            scrollbarRect.GetComponent<Image>().raycastTarget = true;
            Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            RectTransform slidingArea = new GameObject("Sliding Area", typeof(RectTransform)).GetComponent<RectTransform>();
            slidingArea.SetParent(scrollbarRect, false);
            Stretch(slidingArea, 2f, 2f, -2f, -2f);
            RectTransform handle = CreatePanel("Handle", slidingArea, InvestigationTheme.Primary, 5f);
            Stretch(handle, 0f, 0f, 0f, 0f);
            handle.GetComponent<Image>().raycastTarget = true;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            contentScroll.verticalScrollbar = scrollbar;
            contentScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            contentScroll.verticalScrollbarSpacing = 8f;

            contentRoot = new GameObject("Page Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            contentRoot.SetParent(viewport, false);
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = Vector2.zero;
            VerticalLayoutGroup contentLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentScroll.content = contentRoot;

            footerRoot = CreatePanel("Investigation Footer", safeAreaRoot, new Color32(4, 18, 28, 245), InvestigationTheme.SmallRadius);
            Anchor(footerRoot, 0f, 0f, 1f, 0f, OuterMargin, OuterMargin, -OuterMargin, OuterMargin + 44f);
            AddSubtleOutline(footerRoot, new Color32(95, 212, 214, 45));
            footerLeft = CreatePanel("Footer Left", footerRoot, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(footerLeft, 0f, 0f, 0.5f, 1f, 10f, 4f, -4f, -4f);
            HorizontalLayoutGroup leftLayout = footerLeft.gameObject.AddComponent<HorizontalLayoutGroup>();
            leftLayout.spacing = 8f; leftLayout.childAlignment = TextAnchor.MiddleLeft;
            leftLayout.childControlWidth = false; leftLayout.childControlHeight = true;
            leftLayout.childForceExpandWidth = false; leftLayout.childForceExpandHeight = false;
            footerRight = CreatePanel("Footer Right", footerRoot, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(footerRight, 0.5f, 0f, 1f, 1f, 4f, 4f, -10f, -4f);
            HorizontalLayoutGroup rightLayout = footerRight.gameObject.AddComponent<HorizontalLayoutGroup>();
            rightLayout.spacing = 8f; rightLayout.childAlignment = TextAnchor.MiddleRight;
            rightLayout.childControlWidth = false; rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth = false; rightLayout.childForceExpandHeight = false;
        }

        private void RenderAll()
        {
            CloseScenarioDetail(false);
            FocusSnapshot focusSnapshot = CaptureFocus();
            bool enteringCaseClosed = state != null
                && hasRenderedPhase
                && state.Phase == InvestigationPhase.Report
                && lastRenderedPhase == InvestigationPhase.Report
                && state.ConclusionStatus == InvestigationConclusionStatus.Correct
                && lastRenderedConclusionStatus != InvestigationConclusionStatus.Correct;
            bool preservePhaseScroll = state != null
                && hasRenderedPhase
                && state.Phase == lastRenderedPhase
                && !enteringCaseClosed;
            // A page that previously fit can report zero as its normalized position.
            // When new content grows the page, start at its top.
            float previousPageScroll = preservePhaseScroll && contentScroll != null
                && contentScroll.content.rect.height > contentScroll.viewport.rect.height + 1f
                ? contentScroll.verticalNormalizedPosition
                : 1f;
            ScrollRect previousNotebookScroll = preservePhaseScroll
                ? FindActiveScrollRect(contentRoot, "Notebook Entry Scroll")
                : null;
            float previousNotebookPosition = previousNotebookScroll == null
                ? 1f
                : previousNotebookScroll.verticalNormalizedPosition;
            ScrollRect previousNotebookDrawerScroll = FindActiveScrollRect(contentPanel, "Notebook Drawer Scroll");
            if (previousNotebookDrawerScroll != null)
                notebookDrawerScrollPosition = previousNotebookDrawerScroll.verticalNormalizedPosition;

            SaveComparisonNotebookScroll();
            HideSpeciesTooltip();
            StopAllCoroutines();
            CancelTodayRecordingVisuals();
            RemoveObserveSummaryFlight();
            viewportRefreshScheduled = false;
            speciesPairHighlights.Clear();
            ResetPageEntranceVisuals();
            RemoveNotebookDrawer();
            RemoveEdnaPresentation();
            RemoveComparisonBriefingPresentation();
            bool animatePhaseChange = state != null && (!hasRenderedPhase || state.Phase != lastRenderedPhase);
            if (animatePhaseChange) restartConfirmationPending = false;
            RenderChrome();
            Clear(contentRoot);
            Clear(footerLeft);
            Clear(footerRight);
            if (state == null) return;
            pendingTappedSpeciesMarker = null;
            pendingTappedSpecies = null;
            ApplyPhaseLayout(state.Phase);
            switch (state.Phase)
            {
                case InvestigationPhase.Observe: RenderObserve(); break;
                case InvestigationPhase.Simulate: RenderScenarioComparison(); break;
                case InvestigationPhase.Report: RenderScenarioEnding(); break;
            }
            RenderNotebookDrawer();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            RestoreComparisonNotebookScroll();
            RenderEdnaPresentation();
            ResumeTodayRecording();
            ShowPendingTappedSpeciesTooltip();
            RenderRestartDialog();
            if (contentScroll != null)
            {
                contentScroll.StopMovement();
                contentScroll.verticalNormalizedPosition = preservePhaseScroll ? previousPageScroll : 1f;
            }
            if (preservePhaseScroll)
            {
                ScrollRect currentNotebookScroll = FindActiveScrollRect(contentRoot, "Notebook Entry Scroll");
                if (currentNotebookScroll != null)
                {
                    currentNotebookScroll.StopMovement();
                    currentNotebookScroll.verticalNormalizedPosition = previousNotebookPosition;
                }
            }
            if (state != null)
            {
                lastRenderedPhase = state.Phase;
                lastRenderedConclusionStatus = state.ConclusionStatus;
                hasRenderedPhase = true;
            }
            if (!string.IsNullOrEmpty(navigationRevealTarget))
            {
                string target = navigationRevealTarget;
                bool alignTop = navigationRevealAtTop;
                navigationRevealTarget = string.Empty;
                navigationRevealAtTop = false;
                notebookFocusTargetAfterRender = string.Empty;
                RevealNavigationTarget(target, alignTop);
            }
            else if (enteringCaseClosed)
            {
                StartCoroutine(FocusCaseClosedActionNextFrame());
                StartCoroutine(AnimateCaseClosedStamp());
            }
            else if (!string.IsNullOrEmpty(notebookFocusTargetAfterRender))
            {
                string focusTarget = notebookFocusTargetAfterRender;
                notebookFocusTargetAfterRender = string.Empty;
                StartCoroutine(FocusNotebookControlNextFrame(focusTarget));
            }
            else ScheduleFocusRestore(focusSnapshot);
            if (animatePhaseChange && !InvestigationMotionSettings.ReducedMotion) StartCoroutine(AnimatePageEntrance());
            if (notebookOpeningAnimationPending && notebookDrawerOpen) StartCoroutine(AnimateNotebookOpening());
            notebookOpeningAnimationPending = false;
            RenderComparisonBriefingPresentation();
            RenderArrivalBriefingPresentation();
            ResumeObserveSummarySaving();
        }

        private void ResetPageEntranceVisuals()
        {
            if (contentRoot == null) return;
            CanvasGroup group = contentRoot.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            contentRoot.anchoredPosition = Vector2.zero;
        }

        private void ApplyPhaseLayout(InvestigationPhase phase)
        {
            GetComponent<Canvas>().pixelPerfect = !ScenarioWorkspaceActive;
            bool report = phase == InvestigationPhase.Report && !ScenarioWorkspaceActive;
            if (footerRoot != null) footerRoot.gameObject.SetActive(report);
            if (footerRoot != null && report)
            {
                Anchor(footerRoot, 0f, 0f, 1f, 0f, OuterMargin, OuterMargin, -OuterMargin, OuterMargin + 60f);
            }
            if (footerLeft != null && report)
                Anchor(footerLeft, 0f, 0f, 0.5f, 1f, 10f, 4f, -4f, -4f);
            if (footerRight != null && report)
                Anchor(footerRight, 0.5f, 0f, 1f, 1f, 4f, 4f, -10f, -4f);
            if (contentScroll != null)
            {
                bool simulate = ScenarioWorkspaceActive;
                contentScroll.vertical = !simulate;
                contentScroll.verticalScrollbarVisibility = simulate ? ScrollRect.ScrollbarVisibility.AutoHide
                    : ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                Stretch(contentScroll.viewport, 0f, 0f, 0f, 0f);
            }
            if (contentPanel != null)
            {
                float bottom = report ? 74f : OuterMargin + (ScenarioWorkspaceActive ? ScenarioDockHeight + 12f : 0f);
                Anchor(contentPanel, 0f, 0f, 1f, 1f, OuterMargin, bottom, -OuterMargin, -ContentTopInset);
            }
        }

        private void ConfigureCanvasForCurrentViewport()
        {
            Canvas canvas = GetComponent<Canvas>();
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (canvas == null || scaler == null) return;

            bool portrait = Screen.height > Screen.width;
            Vector2 targetReference = portrait ? PortraitReferenceResolution : LandscapeReferenceResolution;
            // The fitted, animated workbench uses fractional transforms. Pixel
            // snapping can collapse small sprite meshes at those scales.
            canvas.pixelPerfect = !ScenarioWorkspaceActive;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            if (scaler.referenceResolution != targetReference) scaler.referenceResolution = targetReference;
        }

        private void RenderChrome()
        {
            Clear(stageRoot);
            if (state != null)
            {
                CreateStageButton("1 · Observe", InvestigationPhase.Observe);
                CreateStageButton("2 · Investigate", InvestigationPhase.Simulate);
                string revisions = state.MisstepCount > 0 ? $"    REVISIONS {state.MisstepCount}" : string.Empty;
                metricsText.text = $"FINDINGS {CountInitialFindings()}/{InvestigationObserveEvaluator.RequiredCount(caseDefinition)} · MODELS {state.TriedThreatIds.Count}/{caseDefinition.Threats.Count}\nCHECKS {VisibleCompletedObjectiveCount()}/{VisibleRequiredObjectiveCount()}{revisions}";
                caseSubtitleText.text = $"{caseDefinition.DisplayName} · {state.SiteDisplayName} · {state.SurveyDisplayName}";
            }
            else
            {
                metricsText.text = "CASE ERROR";
                caseSubtitleText.text = "CASE UNAVAILABLE";
            }
            restartButton.interactable = state != null && !restartConfirmationPending;
            bool hasStatusMessage = !string.IsNullOrWhiteSpace(statusMessage);
            bool showImportNotice = statusTone == InvestigationStatusTone.Notice && hasStatusMessage;
            bool showStatus = (statusTone == InvestigationStatusTone.Warning && hasStatusMessage)
                || showImportNotice;
            statusPanelRoot.gameObject.SetActive(showStatus);
            if (showStatus)
            {
                statusText.text = statusMessage;
                statusAccent.color = showImportNotice
                    ? InvestigationTheme.Primary
                    : InvestigationTheme.Danger;
                statusPanelRoot.SetAsLastSibling();
                StartCoroutine(HideStatusToastAfterDelay());
            }
        }

        private IEnumerator HideStatusToastAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (statusPanelRoot != null) statusPanelRoot.gameObject.SetActive(false);
        }

        private void CreateStageButton(string label, InvestigationPhase phase)
        {
            Button button = CreateButton($"Stage {phase}", stageRoot, label, ButtonVisualStyle.Stage,
                () => { if (phase == InvestigationPhase.Simulate && state.ConclusionStatus == InvestigationConclusionStatus.Correct) { setPhase?.Invoke(InvestigationPhase.Report); return; } if (phase == InvestigationPhase.Simulate && state.Phase == InvestigationPhase.Report) return; RequestInvestigationPhase(phase); }, out _);
            Image image = button.GetComponent<Image>();
            if (state.Phase == phase || (phase == InvestigationPhase.Simulate && state.Phase == InvestigationPhase.Report))
            {
                image.color = InvestigationTheme.SurfaceRaised;
                button.GetComponentInChildren<TextMeshProUGUI>().color = InvestigationTheme.TextPrimary;
                RectTransform accent = CreatePanel("Active Stage Accent", button.transform, InvestigationTheme.Primary, 0f);
                Anchor(accent, 0.12f, 0f, 0.88f, 0f, 0f, 0f, 0f, 2f);
            }
            bool complete = phase == InvestigationPhase.Observe
                ? InvestigationObserveEvaluator.IsComplete(caseDefinition, state)
                : phase == InvestigationPhase.Simulate
                    ? state.ConclusionStatus == InvestigationConclusionStatus.Correct
                    : state.ConclusionStatus == InvestigationConclusionStatus.Correct;
            if (complete)
            {
                Image check = CreateStatusIcon("Stage Complete", button.transform, InvestigationStatusIconLibrary.Check, InvestigationTheme.Primary);
                Anchor(check.rectTransform, 1f, 0.5f, 1f, 0.5f, -26f, -7f, -12f, 7f);
            }
        }

        private int CountInitialFindings()
        {
            return InvestigationObserveEvaluator.CountFindings(caseDefinition, state);
        }

        private bool IsObjectiveVisible(InvestigationObjectiveDefinition objective)
        {
            return objective != null && objective.Required;
        }

        private int VisibleRequiredObjectiveCount()
        {
            int count = 0;
            for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
            {
                InvestigationObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                if (IsObjectiveVisible(objective)) count++;
            }
            return count;
        }

        private int VisibleCompletedObjectiveCount()
        {
            int count = 0;
            for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
            {
                InvestigationObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                if (IsObjectiveVisible(objective) && state.HasCompletedObjective(objective.ObjectiveId)) count++;
            }
            return count;
        }

        private IEnumerator AnimatePageEntrance()
        {
            CanvasGroup group = contentRoot.GetComponent<CanvasGroup>();
            if (group == null) group = contentRoot.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            Vector2 target = contentRoot.anchoredPosition;
            Vector2 start = target + new Vector2(0f, -8f);
            float elapsed = 0f;
            const float duration = 0.24f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = t;
                contentRoot.anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }
            group.alpha = 1f;
            contentRoot.anchoredPosition = target;
        }

        private void RefreshPresentationOnly()
        {
            if (state == null) return;
            RenderAll();
        }

        private TextMeshProUGUI CreateHeading(string title, string description, bool compact = false)
        {
            float blockHeight = compact ? 28f : 42f;
            int titleFontSize = compact ? 12 : 18;
            int descriptionFontSize = compact ? 10 : 12;
            float childHeight = compact ? 26f : 40f;
            RectTransform block = CreatePanel("Page Heading", contentRoot, new Color(0f, 0f, 0f, 0f), 0f);
            AddLayout(block, blockHeight, 1f);
            HorizontalLayoutGroup layout = block.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 0, 0);
            layout.spacing = compact ? 6f : 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI heading = CreateText("Heading", block, title, titleFontSize, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleLeft, InvestigationTheme.DisplayFont);
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            heading.overflowMode = TextOverflowModes.Overflow;
            LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
            headingLayout.minWidth = compact
                ? Mathf.Clamp(heading.preferredWidth + 4f, 100f, 220f)
                : Mathf.Clamp(heading.preferredWidth + 6f, 145f, 320f);
            headingLayout.preferredWidth = headingLayout.minWidth;
            headingLayout.preferredHeight = childHeight;

            RectTransform divider = CreatePanel("Heading Divider", block, InvestigationTheme.Primary, 0f);
            LayoutElement dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
            dividerLayout.minWidth = 2f;
            dividerLayout.preferredWidth = 2f;
            dividerLayout.minHeight = compact ? 16f : 22f;
            dividerLayout.preferredHeight = dividerLayout.minHeight;

            TextMeshProUGUI sub = CreateText("Description", block, description, descriptionFontSize, FontStyle.Normal, InvestigationTheme.TextSecondary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            sub.textWrappingMode = TextWrappingModes.Normal;
            sub.overflowMode = TextOverflowModes.Overflow;
            LayoutElement subLayout = sub.gameObject.AddComponent<LayoutElement>();
            subLayout.minWidth = compact ? 180f : 220f;
            subLayout.preferredHeight = childHeight;
            subLayout.flexibleWidth = 1f;
            return heading;
        }

        private Button CreateButton(string name, Transform parent, string label, ButtonVisualStyle style, UnityEngine.Events.UnityAction action, out TextMeshProUGUI labelText)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(InvestigationRoundedCorners));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            float radius = InvestigationTheme.ControlRadius;
            Color background = InvestigationTheme.Surface;
            Color foreground = InvestigationTheme.TextPrimary;
            switch (style)
            {
                case ButtonVisualStyle.Primary: background = InvestigationTheme.Accent; foreground = InvestigationTheme.OnAccent; break;
                case ButtonVisualStyle.PaperPrimary: background = InvestigationTheme.Accent; foreground = InvestigationTheme.OnAccent; break;
                case ButtonVisualStyle.Tertiary: background = new Color(0f, 0f, 0f, 0f); foreground = InvestigationTheme.TextMuted; break;
                case ButtonVisualStyle.Secondary: background = InvestigationTheme.SurfaceRaised; foreground = InvestigationTheme.TextPrimary; break;
                case ButtonVisualStyle.Stage: background = InvestigationTheme.Deep; foreground = InvestigationTheme.TextSecondary; radius = 6f; break;
                case ButtonVisualStyle.Choice: background = InvestigationTheme.Surface; foreground = InvestigationTheme.TextPrimary; radius = InvestigationTheme.SmallRadius; break;
                case ButtonVisualStyle.PaperChoice: background = InvestigationTheme.PaperBorder; foreground = InvestigationTheme.PaperInk; radius = InvestigationTheme.SmallRadius; break;
                case ButtonVisualStyle.Danger: background = new Color32(88, 28, 31, 255); foreground = InvestigationTheme.Danger; break;
            }
            image.color = background;
            buttonObject.GetComponent<InvestigationRoundedCorners>().Configure(radius);
            Button button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            bool paperStyle = style == ButtonVisualStyle.PaperPrimary || style == ButtonVisualStyle.PaperChoice;
            float hoverBrightness = paperStyle ? 0.96f : 1.12f;
            colors.highlightedColor = new Color(hoverBrightness, hoverBrightness, hoverBrightness, 1f);
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.36f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            if (action != null) button.onClick.AddListener(action);

            if (style == ButtonVisualStyle.Danger)
                EnsureOutline(buttonObject, new Color32(242, 118, 107, 120), new Vector2(1f, -1f));

            if (style == ButtonVisualStyle.Primary)
                AddSingleShadow(buttonObject, InvestigationTheme.PrimaryShadow, new Vector2(3f, -3f));
            else if (style == ButtonVisualStyle.PaperPrimary)
                AddSingleShadow(buttonObject, InvestigationTheme.PaperShadow, new Vector2(3f, -3f));
            else if (style == ButtonVisualStyle.PaperChoice)
                ConfigurePaperChoiceButton(buttonObject, button, radius);

            bool primaryAction = style == ButtonVisualStyle.Primary || style == ButtonVisualStyle.PaperPrimary;
            labelText = CreateText("Label", buttonObject.transform, label, primaryAction ? 17 : 15, FontStyle.Bold, foreground, TextAnchor.MiddleCenter, InvestigationTheme.DisplayFont);
            Stretch(labelText.rectTransform, 10f, 4f, -10f, -4f);
            labelText.textWrappingMode = TextWrappingModes.Normal;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.raycastTarget = false;
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 52f;
            layout.minWidth = primaryAction ? 210f : 132f;
            layout.preferredWidth = primaryAction ? 250f : 170f;
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(layout.preferredWidth, layout.preferredHeight);
            InvestigationFocusRing focusRing = buttonObject.AddComponent<InvestigationFocusRing>();
            focusRing.Configure(radius, InvestigationTheme.Focus);
            return button;
        }

        private static void ConfigurePaperChoiceButton(GameObject buttonObject, Button button, float radius)
        {
            AddSingleShadow(buttonObject, InvestigationTheme.PaperShadow, new Vector2(3f, -3f));
            RectTransform face = CreatePanel("Paper Choice Face", buttonObject.transform, InvestigationTheme.PaperRaised, Mathf.Max(8f, radius - 2f));
            Anchor(face, 0f, 0f, 1f, 1f, 1f, 1f, -1f, -1f);
            button.targetGraphic = face.GetComponent<Image>();
        }

        private static void AddSingleShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void CreateMarineSnowLayer(
            string name,
            Transform parent,
            int count,
            Vector2 sizeRange,
            float speed,
            Vector2 alphaRange,
            float sway,
            uint seed)
        {
            InvestigationMarineSnowGraphic layer = CreateGraphic<InvestigationMarineSnowGraphic>(name, parent);
            Stretch(layer.rectTransform, 0f, 0f, 0f, 0f);
            layer.color = Color.white;
            layer.Configure(count, sizeRange, speed, alphaRange, sway, seed);
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color, float radius)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (radius > 0f)
            {
                InvestigationRoundedCorners rounded = panelObject.AddComponent<InvestigationRoundedCorners>();
                rounded.Configure(radius);
            }
            return panelObject.GetComponent<RectTransform>();
        }

        private static void AddSubtleOutline(RectTransform target, Color color)
        {
            EnsureOutline(target.gameObject, color, new Vector2(1f, -1f));
        }

        private static Outline EnsureOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null) outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(Mathf.Clamp(distance.x, -1f, 1f), Mathf.Clamp(distance.y, -1f, 1f));
            outline.useGraphicAlpha = true;
            return outline;
        }

        private static TextAlignmentOptions TmpAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Midline;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                default: return TextAlignmentOptions.BottomRight;
            }
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, int fontSize, FontStyle style, Color color, TextAnchor alignment, TMP_FontAsset font)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            int minimumFontSize = font == InvestigationTheme.DataFont ? 10 : 11;
            text.fontSize = Mathf.Max(fontSize, minimumFontSize);
            text.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : style == FontStyle.Italic ? FontStyles.Italic
                : style == FontStyle.BoldAndItalic ? FontStyles.Bold | FontStyles.Italic : FontStyles.Normal;
            text.color = color;
            text.alignment = TmpAlignment(alignment);
            text.richText = false;
            text.lineSpacing = 0f;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static T CreateGraphic<T>(string name, Transform parent) where T : Graphic
        {
            GameObject graphicObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
            graphicObject.transform.SetParent(parent, false);
            return graphicObject.GetComponent<T>();
        }

        private static Image CreateStatusIcon(string name, Transform parent, Sprite sprite, Color color)
        {
            Image icon = CreateGraphic<Image>(name, parent);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = color;
            return icon;
        }

        private RectTransform CreateSpeciesArtwork(
            string name,
            Transform parent,
            InvestigationSpeciesDefinition species,
            Color fallbackColor,
            bool ghost = false)
        {
            if (species != null && species.Icon != null)
            {
                Image image = CreateGraphic<Image>(name, parent);
                image.sprite = species.Icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, ghost ? 0.30f : 1f);
                return image.rectTransform;
            }

            TextMeshProUGUI fallback = CreateText(
                name,
                parent,
                species == null ? "Unknown species" : species.DisplayName,
                11,
                FontStyle.Bold,
                new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, ghost ? 0.38f : 1f),
                TextAnchor.MiddleCenter,
                InvestigationTheme.DisplayFont);
            fallback.textWrappingMode = TextWrappingModes.Normal;
            fallback.overflowMode = TextOverflowModes.Truncate;
            return fallback.rectTransform;
        }

        private RectTransform CreateThreatArtwork(string name, Transform parent, ThreatSimulationDefinition threat)
        {
            if (threat != null && threat.Icon != null)
            {
                Image image = CreateGraphic<Image>(name, parent);
                image.sprite = threat.Icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = Color.white;
                return image.rectTransform;
            }

            InvestigationGlyphGraphic glyph = CreateGraphic<InvestigationGlyphGraphic>(name, parent);
            glyph.color = InvestigationTheme.Primary;
            glyph.SetGlyph(threat == null ? InvestigationGlyph.Question : ToThreatGlyph(threat.GlyphKind));
            return glyph.rectTransform;
        }

        private static LayoutElement AddLayout(RectTransform rect, float preferredHeight, float flexibleWidth)
        {
            LayoutElement layout = rect.GetComponent<LayoutElement>();
            if (layout == null) layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            layout.minHeight = Mathf.Min(preferredHeight, 44f);
            layout.flexibleWidth = flexibleWidth;
            return layout;
        }

        private static void Anchor(RectTransform rect, float minX, float minY, float maxX, float maxY, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            Anchor(rect, 0f, 0f, 1f, 1f, left, bottom, right, top);
        }

        private static void Clear(Transform parent)
        {
            if (parent == null) return;
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                GameObject child = parent.GetChild(index).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private static ScrollRect FindActiveScrollRect(Transform root, string objectName)
        {
            if (root == null) return null;
            ScrollRect[] scrollRects = root.GetComponentsInChildren<ScrollRect>();
            for (int index = 0; index < scrollRects.Length; index++)
            {
                if (scrollRects[index].name == objectName) return scrollRects[index];
            }
            return null;
        }

        private void RevealNavigationTarget(string objectName, bool alignTop)
        {
            RectTransform target = FindNamedRect(contentPanel, objectName);
            if (target == null) return;
            // Run after rebuilding layout and restoring ordinary reading offsets.
            // Explicit navigation wins over both the old scroll and old focus.
            ScrollRect scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll != null && scroll.content != null && scroll.viewport != null && target.IsChildOf(scroll.content))
            {
                float hiddenHeight = scroll.content.rect.height - scroll.viewport.rect.height;
                if (scroll.vertical && hiddenHeight > 0f)
                {
                    Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, target);
                    Rect viewport = scroll.viewport.rect;
                    const float margin = 6f;
                    bool tallerThanViewport = bounds.size.y > viewport.height - margin * 2f;
                    float shift = alignTop || tallerThanViewport || bounds.max.y > viewport.yMax - margin
                        ? bounds.max.y - viewport.yMax + margin
                        : bounds.min.y < viewport.yMin + margin ? bounds.min.y - viewport.yMin - margin : 0f;
                    scroll.StopMovement();
                    scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + shift / hiddenHeight);
                }
            }
            Selectable focus = target.GetComponent<Selectable>() ?? FindFirstInteractableButton(target);
            if (focus != null && focus.interactable && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(focus.gameObject);
            }
        }

        private FocusSnapshot CaptureFocus()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
                return default;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected.transform != transform && !selected.transform.IsChildOf(transform)) return default;
            InvestigationFocusRing focusRing = selected.GetComponent<InvestigationFocusRing>();
            if (focusRing != null && focusRing.SelectedByPointer) return default;
            return new FocusSnapshot(true, selected.name);
        }

        private IEnumerator RefreshAfterViewportChange()
        {
            InvestigationPhase preservedPhase = state == null ? InvestigationPhase.Observe : state.Phase;
            InvestigationConclusionStatus preservedConclusion = state == null
                ? InvestigationConclusionStatus.NotSubmitted
                : state.ConclusionStatus;
            float preservedScroll = contentScroll == null ? 1f : contentScroll.verticalNormalizedPosition;
            yield return null;
            viewportRefreshScheduled = false;
            if (!built || state == null) yield break;
            RenderAll();
            if (contentScroll != null
                && state.Phase == preservedPhase
                && state.ConclusionStatus == preservedConclusion)
            {
                contentScroll.StopMovement();
                contentScroll.verticalNormalizedPosition = preservedScroll;
            }
        }

        private void ScheduleFocusRestore(FocusSnapshot snapshot)
        {
            if (!snapshot.HadFocus || !Application.isPlaying) return;
            StartCoroutine(RestoreFocusNextFrame(snapshot));
        }

        private IEnumerator RestoreFocusNextFrame(FocusSnapshot snapshot)
        {
            yield return null;
            if (EventSystem.current == null) yield break;

            Button target = FindInteractableButton(snapshot.ObjectName);
            if (target == null && snapshot.ObjectName == "Talk To Edna")
                target = FindInteractableButton("Scenario Briefing Next");
            if (target == null && snapshot.ObjectName == "Restart Case")
                target = FindInteractableButton("Cancel Restart Case");
            if (target == null && snapshot.ObjectName == "Cancel Restart Case")
                target = FindInteractableButton("Restart Case");
            if (target == null) target = FindFirstInteractableButton(contentRoot);
            if (target == null) target = FindFirstInteractableButton(footerRight);
            if (target == null) target = FindFirstInteractableButton(stageRoot);
            if (target == null) yield break;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        private IEnumerator FocusCaseClosedActionNextFrame()
        {
            yield return null;
            if (EventSystem.current == null) yield break;
            Button target = FindInteractableButton("Restart Completed Case");
            if (target == null) yield break;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        private Button FindInteractableButton(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return null;
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];
                if (button.gameObject.activeInHierarchy
                    && button.interactable
                    && button.name == objectName)
                {
                    return button;
                }
            }
            return null;
        }

        private static Button FindFirstInteractableButton(Transform root)
        {
            if (root == null) return null;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].gameObject.activeInHierarchy && buttons[index].interactable
                    && !buttons[index].name.StartsWith("Detail ", StringComparison.Ordinal))
                    return buttons[index];
            }
            return null;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static InvestigationGlyph ToThreatGlyph(ThreatGlyphKind glyphKind)
        {
            switch (glyphKind)
            {
                case ThreatGlyphKind.Plastic: return InvestigationGlyph.Plastic;
                case ThreatGlyphKind.LongLine: return InvestigationGlyph.LongLine;
                case ThreatGlyphKind.BottomTrawling: return InvestigationGlyph.BottomTrawling;
                default: return InvestigationGlyph.Question;
            }
        }

        private static string PredictionLabel(PredictionState state)
        {
            switch (state)
            {
                case PredictionState.Increase: return "Increase";
                case PredictionState.Decrease: return "Decrease";
                case PredictionState.Stable: return "Stable";
                case PredictionState.DepthShift: return "Depth shift";
                case PredictionState.Unknown: return "Unknown";
                default: return state.ToString();
            }
        }
    }
}
