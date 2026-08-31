using System;
using System.Collections;
using System.Collections.Generic;
using EDNA.Investigation.V2.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    [DisallowMultipleComponent]
    public sealed partial class InvestigationV2RuntimeView : MonoBehaviour
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

        [SerializeField] private Sprite seamountSprite;

        private InvestigationV2CaseDefinition caseDefinition;
        private InvestigationV2State state;
        private Action<InvestigationV2Phase> setPhase;
        private Action<InvestigationV2Difficulty> setDifficulty;
        private Action<string> discoverObservation;
        private Action<string> runThreat;
        private Action<string, PredictionTargetKind, string, string, ComparisonJudgement> compare;
        private Action<string> submitProvisional;
        private Action reviewConfirmation;
        private Action<string> setFinalThreat;
        private Action<string, bool> setReportEvidence;
        private Action<string> setReasoning;
        private Action<string> setLimitation;
        private Action submitFinal;
        private Action restart;
        private Action<bool> setReducedMotion;

        private RectTransform stageRoot;
        private Text caseSubtitleText;
        private Text metricsText;
        private RectTransform statusPanelRoot;
        private Text statusText;
        private Image statusAccent;
        private RectTransform contentPanel;
        private RectTransform contentRoot;
        private ScrollRect contentScroll;
        private RectTransform footerRoot;
        private RectTransform footerLeft;
        private RectTransform footerRight;
        private Button difficultyButton;
        private Text difficultyText;
        private Button motionButton;
        private Text motionText;

        private string selectedThreatId = string.Empty;
        private PredictionTargetKind selectedPredictionTargetKind = PredictionTargetKind.Species;
        private string selectedPredictionSpeciesId = string.Empty;
        private string selectedObservationId = string.Empty;
        private string pendingTappedSpeciesId = string.Empty;
        private bool pendingTappedSpeciesHistorical;
        private RectTransform pendingTappedSpeciesMarker;
        private InvestigationV2SpeciesDefinition pendingTappedSpecies;
        private RectTransform speciesTooltip;
        private string statusMessage = string.Empty;
        private InvestigationV2StatusTone statusTone = InvestigationV2StatusTone.Guide;
        private readonly HashSet<string> animatedThreatIds = new HashSet<string>(StringComparer.Ordinal);
        private bool built;
        private bool viewportRefreshScheduled;
        private Vector2 lastViewportSize = new Vector2(-1f, -1f);
        private bool restartConfirmationPending;
        private bool hasRenderedPhase;
        private InvestigationV2Phase lastRenderedPhase;
        private InvestigationV2ConclusionStatus lastRenderedConclusionStatus = InvestigationV2ConclusionStatus.NotSubmitted;

        public InvestigationV2State State => state;
        public RectTransform ContentRoot => contentRoot;
        public Sprite SeamountSprite => seamountSprite;

        public void Bind(
            InvestigationV2CaseDefinition definition,
            Action<InvestigationV2Phase> onSetPhase,
            Action<InvestigationV2Difficulty> onSetDifficulty,
            Action<string> onDiscoverObservation,
            Action<string> onRunThreat,
            Action<string, PredictionTargetKind, string, string, ComparisonJudgement> onCompare,
            Action<string> onSubmitProvisional,
            Action onReviewConfirmation,
            Action<string> onSetFinalThreat,
            Action<string, bool> onSetReportEvidence,
            Action<string> onSetReasoning,
            Action<string> onSetLimitation,
            Action onSubmitFinal,
            Action onRestart,
            Action<bool> onSetReducedMotion)
        {
            caseDefinition = definition;
            setPhase = onSetPhase;
            setDifficulty = onSetDifficulty;
            discoverObservation = onDiscoverObservation;
            runThreat = onRunThreat;
            compare = onCompare;
            submitProvisional = onSubmitProvisional;
            reviewConfirmation = onReviewConfirmation;
            setFinalThreat = onSetFinalThreat;
            setReportEvidence = onSetReportEvidence;
            setReasoning = onSetReasoning;
            setLimitation = onSetLimitation;
            submitFinal = onSubmitFinal;
            restart = onRestart;
            setReducedMotion = onSetReducedMotion;
            EnsureUi();
        }

        public void ResetPresentationState()
        {
            selectedThreatId = string.Empty;
            selectedPredictionTargetKind = PredictionTargetKind.Species;
            selectedPredictionSpeciesId = string.Empty;
            selectedObservationId = string.Empty;
            pendingTappedSpeciesId = string.Empty;
            pendingTappedSpeciesMarker = null;
            pendingTappedSpecies = null;
            HideSpeciesTooltip();
            animatedThreatIds.Clear();
            restartConfirmationPending = false;
            hasRenderedPhase = false;
            lastRenderedConclusionStatus = InvestigationV2ConclusionStatus.NotSubmitted;
            contentScroll?.StopMovement();
            if (contentScroll != null) contentScroll.verticalNormalizedPosition = 1f;
        }

        public void Refresh(InvestigationV2State currentState, string message, InvestigationV2StatusTone tone)
        {
            state = currentState;
            statusMessage = message ?? string.Empty;
            statusTone = tone;
            EnsureUi();
            RenderAll();
        }

        public void ShowFatalError(string message)
        {
            EnsureUi();
            state = null;
            statusMessage = message ?? "Unknown Investigation V2 error.";
            statusTone = InvestigationV2StatusTone.Warning;
            RenderChrome();
            Clear(contentRoot);
            Text error = CreateText("Fatal Error", contentRoot, statusMessage, 21, FontStyle.Bold, InvestigationV2Theme.Danger, TextAnchor.UpperLeft, InvestigationV2Theme.DisplayFont);
            AddLayout(error.rectTransform, 120f, 1f);
            Clear(footerLeft);
            Clear(footerRight);
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

            RectTransform background = CreatePanel("V2 Deep Sea Background", transform, InvestigationV2Theme.Background, 0f);
            Stretch(background, 0f, 0f, 0f, 0f);

            // Water is built as a stack, back to front. Sibling order is the
            // render order, so everything before "V2 Safe Area" sits behind the
            // interface and everything after it sits in front.
            InvestigationV2WaterColumnGraphic waterColumn =
                CreateGraphic<InvestigationV2WaterColumnGraphic>("Water Column", background);
            Stretch(waterColumn.rectTransform, 0f, 0f, 0f, 0f);
            waterColumn.color = Color.white;

            InvestigationV2GodRayGraphic godRays = CreateGraphic<InvestigationV2GodRayGraphic>("God Rays", background);
            Stretch(godRays.rectTransform, 0f, 0f, 0f, 0f);
            godRays.color = Color.white;

            CreateMarineSnowLayer(
                "Marine Snow Far",
                background,
                30,
                new Vector2(1f, 2.2f),
                4f,
                new Vector2(0.10f, InvestigationV2Theme.MarineSnowFarMaxAlpha),
                6f,
                0x9E3779B9u);
            CreateMarineSnowLayer(
                "Marine Snow",
                background,
                20,
                new Vector2(2f, 3.4f),
                9f,
                new Vector2(0.20f, InvestigationV2Theme.MarineSnowNearMaxAlpha),
                11f,
                0x85EBCA6Bu);

            InvestigationV2VignetteGraphic vignette = CreateGraphic<InvestigationV2VignetteGraphic>("Water Vignette", background);
            Stretch(vignette.rectTransform, 0f, 0f, 0f, 0f);
            vignette.color = Color.white;

            RectTransform safeAreaRoot = CreatePanel("V2 Safe Area", background, new Color(0f, 0f, 0f, 0f), 0f);
            Stretch(safeAreaRoot, 0f, 0f, 0f, 0f);
            safeAreaRoot.gameObject.AddComponent<InvestigationV2SafeAreaFitter>();

            // In front of the interface. Deliberately sparse, large and faint:
            // enough for something to pass between the player and the scene,
            // few enough that it never competes with text.
            CreateMarineSnowLayer(
                "Marine Snow Foreground",
                background,
                7,
                new Vector2(4.5f, 8f),
                19f,
                new Vector2(0.09f, InvestigationV2Theme.MarineSnowForegroundMaxAlpha),
                16f,
                0xC2B2AE35u);

            RectTransform header = CreatePanel("V2 Header", safeAreaRoot, new Color32(8, 36, 54, 248), InvestigationV2Theme.SmallRadius);
            Anchor(header, 0f, 1f, 1f, 1f, OuterMargin, -132f, -OuterMargin, -OuterMargin);
            AddSubtleOutline(header, new Color32(95, 212, 214, 70));

            Text brand = CreateText("Brand", header, "ECOSYSTEM DETECTIVE", 23, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.UpperLeft, InvestigationV2Theme.DisplayFont);
            Anchor(brand.rectTransform, 0f, 0.56f, 0.42f, 1f, 14f, 0f, 0f, -8f);
            caseSubtitleText = CreateText("Case Subtitle", header, string.Empty, 11, FontStyle.Normal, InvestigationV2Theme.TextMuted, TextAnchor.LowerLeft, InvestigationV2Theme.DataFont);
            Anchor(caseSubtitleText.rectTransform, 0f, 0.56f, 0.42f, 1f, 14f, 5f, 0f, -36f);

            metricsText = CreateText("Metrics", header, "", 14, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.UpperRight, InvestigationV2Theme.DataFont);
            Anchor(metricsText.rectTransform, 0.42f, 0.60f, 0.78f, 1f, 0f, 0f, -8f, -14f);

            difficultyButton = CreateButton("Difficulty Toggle", header, "Easy", ButtonVisualStyle.Secondary, () =>
            {
                if (state == null) return;
                setDifficulty?.Invoke(state.Difficulty == InvestigationV2Difficulty.Easy ? InvestigationV2Difficulty.Hard : InvestigationV2Difficulty.Easy);
            }, out difficultyText);
            Anchor(difficultyButton.GetComponent<RectTransform>(), 0.79f, 0.62f, 0.89f, 1f, 0f, 7f, -6f, -10f);

            motionButton = CreateButton("Motion Toggle", header, "Motion: Full", ButtonVisualStyle.Secondary, () =>
            {
                setReducedMotion?.Invoke(!InvestigationV2MotionSettings.ReducedMotion);
            }, out motionText);
            Anchor(motionButton.GetComponent<RectTransform>(), 0.89f, 0.62f, 1f, 1f, 0f, 7f, -12f, -10f);

            stageRoot = CreatePanel("Stage Navigation", header, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(stageRoot, 0f, 0.04f, 1f, 0.50f, 8f, 0f, -8f, 0f);
            HorizontalLayoutGroup stageLayout = stageRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            stageLayout.spacing = 8f;
            stageLayout.childControlWidth = true;
            stageLayout.childControlHeight = true;
            stageLayout.childForceExpandWidth = true;
            stageLayout.childForceExpandHeight = true;

            statusPanelRoot = CreatePanel("Status Toast", safeAreaRoot, new Color32(14, 51, 72, 252), InvestigationV2Theme.SmallRadius);
            Anchor(statusPanelRoot, 0.16f, 1f, 0.84f, 1f, 0f, -184f, 0f, -142f);
            statusAccent = CreatePanel("Status Accent", statusPanelRoot, InvestigationV2Theme.Danger, 0f).GetComponent<Image>();
            Anchor(statusAccent.rectTransform, 0f, 0f, 0f, 1f, 0f, 0f, 6f, 0f);
            statusText = CreateText("Status Message", statusPanelRoot, "", 14, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleLeft, InvestigationV2Theme.BodyFont);
            Anchor(statusText.rectTransform, 0f, 0f, 1f, 1f, 18f, 0f, -12f, 0f);
            statusPanelRoot.gameObject.SetActive(false);

            contentPanel = CreatePanel("V2 Content", safeAreaRoot, new Color(0f, 0f, 0f, 0f), 0f);
            Anchor(contentPanel, 0f, 0f, 1f, 1f, OuterMargin, 84f, -OuterMargin, -138f);
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
            RectTransform handle = CreatePanel("Handle", slidingArea, InvestigationV2Theme.Primary, 5f);
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

            footerRoot = CreatePanel("V2 Footer", safeAreaRoot, new Color32(4, 18, 28, 245), InvestigationV2Theme.SmallRadius);
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
            FocusSnapshot focusSnapshot = CaptureFocus();
            bool enteringCaseClosed = state != null
                && hasRenderedPhase
                && state.Phase == InvestigationV2Phase.Report
                && lastRenderedPhase == InvestigationV2Phase.Report
                && state.ConclusionStatus == InvestigationV2ConclusionStatus.Correct
                && lastRenderedConclusionStatus != InvestigationV2ConclusionStatus.Correct;
            bool preservePhaseScroll = state != null
                && hasRenderedPhase
                && state.Phase == lastRenderedPhase
                && !enteringCaseClosed;
            float previousPageScroll = preservePhaseScroll && contentScroll != null
                ? contentScroll.verticalNormalizedPosition
                : 1f;
            ScrollRect previousNotebookScroll = preservePhaseScroll
                ? FindActiveScrollRect(contentRoot, "Notebook Entry Scroll")
                : null;
            float previousNotebookPosition = previousNotebookScroll == null
                ? 1f
                : previousNotebookScroll.verticalNormalizedPosition;

            HideSpeciesTooltip();
            StopAllCoroutines();
            ResetPageEntranceVisuals();
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
                case InvestigationV2Phase.Observe: RenderObserve(); break;
                case InvestigationV2Phase.Simulate: RenderSimulate(); break;
                case InvestigationV2Phase.Report: RenderReport(); break;
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            ShowPendingTappedSpeciesTooltip();
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
            if (enteringCaseClosed) StartCoroutine(FocusCaseClosedActionNextFrame());
            else ScheduleFocusRestore(focusSnapshot);
            if (animatePhaseChange && !InvestigationV2MotionSettings.ReducedMotion) StartCoroutine(AnimatePageEntrance());
        }

        private void ResetPageEntranceVisuals()
        {
            if (contentRoot == null) return;
            CanvasGroup group = contentRoot.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
            contentRoot.anchoredPosition = Vector2.zero;
        }

        private void ApplyPhaseLayout(InvestigationV2Phase phase)
        {
            bool report = phase == InvestigationV2Phase.Report;
            if (footerRoot != null) footerRoot.gameObject.SetActive(report);
            if (footerRoot != null && report)
            {
                Anchor(footerRoot, 0f, 0f, 1f, 0f, OuterMargin, OuterMargin, -OuterMargin, OuterMargin + 44f);
            }
            if (footerLeft != null && report)
                Anchor(footerLeft, 0f, 0f, 0.5f, 1f, 10f, 4f, -4f, -4f);
            if (footerRight != null && report)
                Anchor(footerRight, 0.5f, 0f, 1f, 1f, 4f, 4f, -10f, -4f);
            if (contentPanel != null)
            {
                float bottom = report ? 58f : OuterMargin;
                Anchor(contentPanel, 0f, 0f, 1f, 1f, OuterMargin, bottom, -OuterMargin, -138f);
            }
        }

        private void ConfigureCanvasForCurrentViewport()
        {
            Canvas canvas = GetComponent<Canvas>();
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (canvas == null || scaler == null) return;

            bool portrait = Screen.height > Screen.width;
            Vector2 targetReference = portrait ? PortraitReferenceResolution : LandscapeReferenceResolution;
            canvas.pixelPerfect = true;
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
                CreateStageButton("1 · Observe", InvestigationV2Phase.Observe);
                CreateStageButton("2 · Simulate", InvestigationV2Phase.Simulate);
                CreateStageButton("3 · Report", InvestigationV2Phase.Report);
                string revisions = state.MisstepCount > 0 ? $"    REVISIONS {state.MisstepCount}" : string.Empty;
                metricsText.text = $"CASE PROGRESS    FINDINGS {CountInitialFindings()}/{caseDefinition.MinimumObserveDiscoveries}    MODELS {state.TriedThreatIds.Count}/{caseDefinition.Threats.Count}    QUESTIONS {VisibleCompletedObjectiveCount()}/{VisibleRequiredObjectiveCount()}{revisions}";
                difficultyText.text = state.Difficulty == InvestigationV2Difficulty.Easy ? "Easy" : "Hard";
                caseSubtitleText.text = $"{caseDefinition.DisplayName.ToUpperInvariant()} // {state.SiteDisplayName.ToUpperInvariant()} // {state.SurveyDisplayName.ToUpperInvariant()}";
            }
            else
            {
                metricsText.text = "CASE ERROR";
                caseSubtitleText.text = "CASE UNAVAILABLE";
            }
            motionText.text = InvestigationV2MotionSettings.ReducedMotion ? "Motion: Reduced" : "Motion: Full";
            bool hasStatusMessage = !string.IsNullOrWhiteSpace(statusMessage);
            bool showReportDiagnostic = state != null
                && state.Phase == InvestigationV2Phase.Report
                && state.ConclusionStatus == InvestigationV2ConclusionStatus.InsufficientEvidence
                && statusTone == InvestigationV2StatusTone.Guide
                && hasStatusMessage;
            bool showImportNotice = statusTone == InvestigationV2StatusTone.Notice && hasStatusMessage;
            bool showStatus = (statusTone == InvestigationV2StatusTone.Warning && hasStatusMessage)
                || showImportNotice
                || showReportDiagnostic;
            statusPanelRoot.gameObject.SetActive(showStatus);
            if (showStatus)
            {
                statusText.text = statusMessage;
                statusAccent.color = showReportDiagnostic || showImportNotice
                    ? InvestigationV2Theme.Primary
                    : InvestigationV2Theme.Danger;
                statusPanelRoot.SetAsLastSibling();
                StartCoroutine(HideStatusToastAfterDelay());
            }
        }

        private IEnumerator HideStatusToastAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (statusPanelRoot != null) statusPanelRoot.gameObject.SetActive(false);
        }

        private void CreateStageButton(string label, InvestigationV2Phase phase)
        {
            Button button = CreateButton($"Stage {phase}", stageRoot, label, ButtonVisualStyle.Stage, () => setPhase?.Invoke(phase), out _);
            Image image = button.GetComponent<Image>();
            if (state.Phase == phase)
            {
                image.color = InvestigationV2Theme.SurfaceRaised;
                button.GetComponentInChildren<Text>().color = InvestigationV2Theme.TextPrimary;
                EnsureOutline(button.gameObject, InvestigationV2Theme.Primary, new Vector2(3f, -3f));
            }
        }

        private int CountInitialFindings()
        {
            int count = 0;
            for (int index = 0; index < state.DiscoveredObservationIds.Count; index++)
            {
                InvestigationV2ObservationDefinition observation = caseDefinition.FindObservation(state.DiscoveredObservationIds[index]);
                if (observation != null
                    && observation.UnlockStage == EvidenceUnlockStage.Observe
                    && observation.Source != ObservationSource.Methodology)
                {
                    count++;
                }
            }
            return count;
        }

        private bool IsObjectiveVisible(InvestigationV2ObjectiveDefinition objective)
        {
            return objective != null
                && objective.Required
                && (objective.ProgressRole != ComparisonProgressRole.BenthicDiscriminator || state.ConfirmationReviewed);
        }

        private int VisibleRequiredObjectiveCount()
        {
            int count = 0;
            for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
            {
                InvestigationV2ObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
                if (IsObjectiveVisible(objective)) count++;
            }
            return count;
        }

        private int VisibleCompletedObjectiveCount()
        {
            int count = 0;
            for (int index = 0; index < caseDefinition.InvestigationObjectives.Count; index++)
            {
                InvestigationV2ObjectiveDefinition objective = caseDefinition.InvestigationObjectives[index];
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
            RenderAll();
        }

        private Text CreateHeading(string title, string description, bool compact = false)
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

            Text heading = CreateText("Heading", block, title, titleFontSize, FontStyle.Bold, InvestigationV2Theme.TextPrimary, TextAnchor.MiddleLeft, InvestigationV2Theme.DisplayFont);
            heading.horizontalOverflow = HorizontalWrapMode.Overflow;
            heading.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
            headingLayout.minWidth = compact
                ? Mathf.Clamp(heading.preferredWidth + 4f, 100f, 220f)
                : Mathf.Clamp(heading.preferredWidth + 6f, 145f, 320f);
            headingLayout.preferredWidth = headingLayout.minWidth;
            headingLayout.preferredHeight = childHeight;

            RectTransform divider = CreatePanel("Heading Divider", block, InvestigationV2Theme.Primary, 0f);
            LayoutElement dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
            dividerLayout.minWidth = 2f;
            dividerLayout.preferredWidth = 2f;
            dividerLayout.minHeight = compact ? 16f : 22f;
            dividerLayout.preferredHeight = dividerLayout.minHeight;

            Text sub = CreateText("Description", block, description, descriptionFontSize, FontStyle.Normal, InvestigationV2Theme.TextSecondary, TextAnchor.MiddleLeft, InvestigationV2Theme.BodyFont);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            sub.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement subLayout = sub.gameObject.AddComponent<LayoutElement>();
            subLayout.minWidth = compact ? 180f : 220f;
            subLayout.preferredHeight = childHeight;
            subLayout.flexibleWidth = 1f;
            return heading;
        }

        private RectTransform CreateSection(string name, Transform parent, Color color, float preferredHeight, float radius = InvestigationV2Theme.CardRadius)
        {
            RectTransform section = CreatePanel(name, parent, color, radius);
            AddLayout(section, preferredHeight, 1f);
            return section;
        }

        private Button CreateButton(string name, Transform parent, string label, ButtonVisualStyle style, UnityEngine.Events.UnityAction action, out Text labelText)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(InvestigationV2RoundedCorners));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            float radius = 18f;
            Color background = InvestigationV2Theme.Surface;
            Color foreground = InvestigationV2Theme.TextPrimary;
            switch (style)
            {
                case ButtonVisualStyle.Primary: background = InvestigationV2Theme.Accent; foreground = InvestigationV2Theme.OnAccent; break;
                case ButtonVisualStyle.PaperPrimary: background = InvestigationV2Theme.Accent; foreground = InvestigationV2Theme.OnAccent; break;
                case ButtonVisualStyle.Tertiary: background = new Color(0f, 0f, 0f, 0f); foreground = InvestigationV2Theme.TextMuted; break;
                case ButtonVisualStyle.Secondary: background = InvestigationV2Theme.SurfaceRaised; foreground = InvestigationV2Theme.TextPrimary; break;
                case ButtonVisualStyle.Stage: background = new Color32(22, 69, 94, 225); foreground = InvestigationV2Theme.TextPrimary; radius = 16f; break;
                case ButtonVisualStyle.Choice: background = new Color32(25, 77, 106, 255); foreground = InvestigationV2Theme.TextPrimary; radius = 16f; break;
                case ButtonVisualStyle.PaperChoice: background = InvestigationV2Theme.PaperBorder; foreground = InvestigationV2Theme.PaperInk; radius = 16f; break;
                case ButtonVisualStyle.Danger: background = new Color32(88, 28, 31, 255); foreground = InvestigationV2Theme.Danger; break;
            }
            image.color = background;
            buttonObject.GetComponent<InvestigationV2RoundedCorners>().Configure(radius);
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

            if (style == ButtonVisualStyle.Choice || style == ButtonVisualStyle.Secondary || style == ButtonVisualStyle.Stage)
                EnsureOutline(buttonObject, InvestigationV2Theme.BorderStrong, new Vector2(2f, -2f));
            else if (style == ButtonVisualStyle.Danger)
                EnsureOutline(buttonObject, new Color32(242, 118, 107, 120), new Vector2(1f, -1f));

            if (style == ButtonVisualStyle.Primary)
                AddSingleShadow(buttonObject, InvestigationV2Theme.PrimaryShadow, new Vector2(3f, -3f));
            else if (style == ButtonVisualStyle.PaperPrimary)
                AddSingleShadow(buttonObject, InvestigationV2Theme.PaperShadow, new Vector2(3f, -3f));
            else if (style == ButtonVisualStyle.PaperChoice)
                ConfigurePaperChoiceButton(buttonObject, button, radius);

            bool primaryAction = style == ButtonVisualStyle.Primary || style == ButtonVisualStyle.PaperPrimary;
            labelText = CreateText("Label", buttonObject.transform, label, primaryAction ? 17 : 15, FontStyle.Bold, foreground, TextAnchor.MiddleCenter, InvestigationV2Theme.DisplayFont);
            Stretch(labelText.rectTransform, 10f, 4f, -10f, -4f);
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Overflow;
            labelText.raycastTarget = false;
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 52f;
            layout.minWidth = primaryAction ? 210f : 132f;
            layout.preferredWidth = primaryAction ? 250f : 170f;
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(layout.preferredWidth, layout.preferredHeight);
            InvestigationV2FocusRing focusRing = buttonObject.AddComponent<InvestigationV2FocusRing>();
            focusRing.Configure(radius, InvestigationV2Theme.Focus);
            return button;
        }

        private static void ConfigurePaperChoiceButton(GameObject buttonObject, Button button, float radius)
        {
            AddSingleShadow(buttonObject, InvestigationV2Theme.PaperShadow, new Vector2(3f, -3f));
            RectTransform face = CreatePanel("Paper Choice Face", buttonObject.transform, InvestigationV2Theme.PaperRaised, Mathf.Max(8f, radius - 2f));
            Anchor(face, 0f, 0f, 1f, 1f, 2f, 2f, -2f, -2f);
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
            InvestigationV2MarineSnowGraphic layer = CreateGraphic<InvestigationV2MarineSnowGraphic>(name, parent);
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
                InvestigationV2RoundedCorners rounded = panelObject.AddComponent<InvestigationV2RoundedCorners>();
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
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
            return outline;
        }

        private static void AddPanelAccent(RectTransform target, Color color, float height = 3f)
        {
            RectTransform accent = CreatePanel("Panel Accent", target, color, 1f);
            Anchor(accent, 0.04f, 1f, 0.96f, 1f, 0f, -height, 0f, 0f);
            LayoutElement layout = accent.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style, Color color, TextAnchor alignment, Font font)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            int minimumFontSize = font == InvestigationV2Theme.DataFont ? 10 : 11;
            text.fontSize = Mathf.Max(fontSize, minimumFontSize);
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = false;
            text.lineSpacing = 1.05f;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
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
            InvestigationV2SpeciesDefinition species,
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

            InvestigationV2GlyphGraphic glyph = CreateGraphic<InvestigationV2GlyphGraphic>(name, parent);
            glyph.color = fallbackColor;
            glyph.SetGlyph(ToSpeciesGlyph(species == null ? SpeciesGlyphKind.Tuna : species.GlyphKind));
            glyph.SetGhost(ghost);
            return glyph.rectTransform;
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

            InvestigationV2GlyphGraphic glyph = CreateGraphic<InvestigationV2GlyphGraphic>(name, parent);
            glyph.color = InvestigationV2Theme.Primary;
            glyph.SetGlyph(ToThreatGlyph(threat == null ? ThreatGlyphKind.Warming : threat.GlyphKind));
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

        private FocusSnapshot CaptureFocus()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
                return default;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected.transform != transform && !selected.transform.IsChildOf(transform)) return default;
            InvestigationV2FocusRing focusRing = selected.GetComponent<InvestigationV2FocusRing>();
            if (focusRing != null && focusRing.SelectedByPointer) return default;
            return new FocusSnapshot(true, selected.name);
        }

        private IEnumerator RefreshAfterViewportChange()
        {
            InvestigationV2Phase preservedPhase = state == null ? InvestigationV2Phase.Observe : state.Phase;
            InvestigationV2ConclusionStatus preservedConclusion = state == null
                ? InvestigationV2ConclusionStatus.NotSubmitted
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
            if (target == null && snapshot.ObjectName == "Review ROV Follow-up")
                target = FindInteractableButton("Submit Final Report");
            if (target == null && snapshot.ObjectName == "Restart V2 Case")
                target = FindInteractableButton("Cancel Restart V2 Case");
            if (target == null && snapshot.ObjectName == "Cancel Restart V2 Case")
                target = FindInteractableButton("Restart V2 Case");
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
                if (buttons[index].gameObject.activeInHierarchy && buttons[index].interactable)
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

        private static InvestigationV2Glyph ToSpeciesGlyph(SpeciesGlyphKind glyphKind)
        {
            switch (glyphKind)
            {
                case SpeciesGlyphKind.Shark: return InvestigationV2Glyph.Shark;
                case SpeciesGlyphKind.Tuna: return InvestigationV2Glyph.Tuna;
                case SpeciesGlyphKind.Krill: return InvestigationV2Glyph.Krill;
                case SpeciesGlyphKind.SeaStar: return InvestigationV2Glyph.SeaStar;
                case SpeciesGlyphKind.Mussel: return InvestigationV2Glyph.Mussel;
                default: return InvestigationV2Glyph.Tuna;
            }
        }

        private static InvestigationV2Glyph ToThreatGlyph(ThreatGlyphKind glyphKind)
        {
            switch (glyphKind)
            {
                case ThreatGlyphKind.Warming: return InvestigationV2Glyph.Warming;
                case ThreatGlyphKind.Plastic: return InvestigationV2Glyph.Plastic;
                case ThreatGlyphKind.LongLine: return InvestigationV2Glyph.LongLine;
                case ThreatGlyphKind.BottomTrawling: return InvestigationV2Glyph.BottomTrawling;
                default: return InvestigationV2Glyph.Question;
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
