using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationSceneSmokeTests
    {
        private bool originalReducedMotion;

        [SetUp]
        public void SetUp()
        {
            originalReducedMotion = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotion(false);
        }

        [TearDown]
        public void TearDown()
        {
            InvestigationMotionSettings.SetReducedMotion(originalReducedMotion);
        }

        [Test]
        public void InvestigationDisplayNames_UsePlayerFacingEvidenceLabelsWithoutDuplicates()
        {
            Assert.That(
                InvestigationDisplayNames.EvidencePatterns(
                    new[] { nameof(EvidenceType.ContaminationWarning), nameof(EvidenceType.LowQualityResult) }),
                Is.EqualTo("Result warning"));
            Assert.That(
                InvestigationDisplayNames.EvidencePatterns(
                    new[] { nameof(EvidenceType.RepeatedNonDetection), "FoodWeb" }),
                Is.EqualTo("Expected but missing, Food-web species"));
        }

        [UnityTest]
        public IEnumerator ResponsiveGrid_ReducesColumnsWithoutHorizontalOverflow()
        {
            GameObject gridObject = new GameObject(
                "Responsive Grid Test",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(LayoutElement),
                typeof(InvestigationResponsiveGridLayout));
            RectTransform rect = gridObject.GetComponent<RectTransform>();
            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
            LayoutElement layoutElement = gridObject.GetComponent<LayoutElement>();
            grid.padding = new RectOffset(12, 12, 0, 0);
            grid.spacing = new Vector2(16f, 10f);
            grid.cellSize = new Vector2(120f, 50f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            for (int index = 0; index < 4; index++)
            {
                new GameObject($"Item {index + 1}", typeof(RectTransform)).transform.SetParent(gridObject.transform, false);
            }

            InvestigationResponsiveGridLayout responsive = gridObject.GetComponent<InvestigationResponsiveGridLayout>();
            rect.sizeDelta = new Vector2(500f, 300f);
            responsive.Configure(120f, 4, true);
            Assert.That(grid.constraintCount, Is.EqualTo(3));
            Assert.That(grid.padding.horizontal + grid.constraintCount * grid.cellSize.x + (grid.constraintCount - 1) * grid.spacing.x, Is.LessThanOrEqualTo(rect.rect.width + 0.01f));
            Assert.That(layoutElement.preferredHeight, Is.EqualTo(110f));

            rect.sizeDelta = new Vector2(300f, 300f);
            responsive.ApplyNow();
            Assert.That(grid.constraintCount, Is.EqualTo(2));
            Assert.That(grid.padding.horizontal + grid.constraintCount * grid.cellSize.x + (grid.constraintCount - 1) * grid.spacing.x, Is.LessThanOrEqualTo(rect.rect.width + 0.01f));

            rect.sizeDelta = new Vector2(100f, 300f);
            responsive.ApplyNow();
            Assert.That(grid.constraintCount, Is.EqualTo(1));
            Assert.That(grid.padding.horizontal + grid.cellSize.x, Is.LessThanOrEqualTo(rect.rect.width + 0.01f));
            Assert.That(layoutElement.preferredHeight, Is.EqualTo(230f));

            Object.Destroy(gridObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResponsiveHeader_CompactsTitleAndKeepsAllMetricsAtNarrowWidths()
        {
            GameObject headerObject = new GameObject(
                "Responsive Header Test",
                typeof(RectTransform),
                typeof(InvestigationResponsiveHeaderLayout));
            RectTransform headerRect = headerObject.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(600f, 68f);

            Text title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            title.transform.SetParent(headerObject.transform, false);
            RectTransform eyebrow = new GameObject("Eyebrow", typeof(RectTransform)).GetComponent<RectTransform>();
            eyebrow.SetParent(headerObject.transform, false);
            RectTransform progress = new GameObject("Progress", typeof(RectTransform)).GetComponent<RectTransform>();
            progress.SetParent(headerObject.transform, false);
            for (int index = 0; index < 4; index++)
            {
                new GameObject($"Metric {index + 1}", typeof(RectTransform), typeof(LayoutElement))
                    .transform.SetParent(progress, false);
            }

            InvestigationResponsiveHeaderLayout responsive = headerObject.GetComponent<InvestigationResponsiveHeaderLayout>();
            responsive.ConfigureReferences(title, eyebrow, progress);
            responsive.SetTitle("eDNA DETECTIVES  /  COMPARE DATA");

            Assert.That(responsive.IsCompact, Is.True);
            Assert.That(title.text, Is.EqualTo("eDNA  /  COMPARE DATA"));
            LayoutElement[] metrics = progress.GetComponentsInChildren<LayoutElement>();
            Assert.That(metrics, Has.Length.EqualTo(4));
            Assert.That(metrics, Has.All.Matches<LayoutElement>(metric => metric.minWidth == 36f));

            headerRect.sizeDelta = new Vector2(960f, 68f);
            responsive.ApplyNow();
            Assert.That(responsive.IsCompact, Is.False);
            Assert.That(title.text, Is.EqualTo("eDNA DETECTIVES  /  COMPARE DATA"));

            Object.Destroy(headerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InvestigationScene_BootstrapsCompleteEnglishWorkflow()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null, "InvestigationScene must be present in Build Settings.");
            yield return loadOperation;
            yield return null;

            InvestigationDemoBootstrap bootstrap = Object.FindAnyObjectByType<InvestigationDemoBootstrap>();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.State, Is.Not.Null);
            Assert.That(controller.State.AllResults.Count, Is.EqualTo(2));
            Assert.That(controller.State.RemainingSamples, Is.EqualTo(2));
            Assert.That(
                controller.State.UnlockedEvidence,
                Has.Some.Matches<EvidenceRecord>(evidence => evidence.EvidenceType == EvidenceType.NewDetection));
            Assert.That(
                controller.State.UnlockedEvidence,
                Has.Some.Matches<EvidenceRecord>(evidence => evidence.EvidenceType == EvidenceType.LowQualityResult));
            Assert.That(
                controller.State.UnlockedEvidence,
                Has.Some.Matches<EvidenceRecord>(evidence => evidence.EvidenceType == EvidenceType.ContaminationWarning));
            Assert.That(controller.State.IdentifiedEvidenceIds, Is.Empty);
            Assert.That(canvas, Is.Not.Null);
            CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(canvasScaler, Is.Not.Null);
            Assert.That(canvasScaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(canvasScaler.referenceResolution, Is.EqualTo(new Vector2(960f, 600f)));
            Assert.That(canvasScaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Assert.That(canvasScaler.matchWidthOrHeight, Is.EqualTo(1f));

            InvestigationProgressView progressView = Object.FindAnyObjectByType<InvestigationProgressView>();
            InvestigationAdaptiveShellLayout shellLayout = Object.FindAnyObjectByType<InvestigationAdaptiveShellLayout>();
            InvestigationAccessibilityBridge accessibility = Object.FindAnyObjectByType<InvestigationAccessibilityBridge>();
            InvestigationResponsiveNavigationLayout navigationLayout = Object.FindAnyObjectByType<InvestigationResponsiveNavigationLayout>();
            InvestigationResponsiveHeaderLayout headerLayout = Object.FindAnyObjectByType<InvestigationResponsiveHeaderLayout>();
            Assert.That(progressView, Is.Not.Null);
            Assert.That(progressView.CurrentSummary, Does.Contain("2 samples available"));
            Assert.That(shellLayout, Is.Not.Null);
            Assert.That(shellLayout.IsComparisonMode, Is.False);
            Assert.That(shellLayout.CurrentActionsHeight, Is.LessThanOrEqualTo(56.1f));
            Assert.That(shellLayout.CurrentContentHeight, Is.GreaterThan(278f), "Compact pages should keep most vertical space for their content.");
            Assert.That(accessibility, Is.Not.Null);
            Assert.That(accessibility.NodeCount, Is.GreaterThanOrEqualTo(10));
            Assert.That(accessibility.LastPageTitle, Does.EndWith("CASE FILES"));
            Assert.That(accessibility.LastAnnouncement, Does.Contain("Review the historical records"));
            Assert.That(navigationLayout, Is.Not.Null);
            Assert.That(navigationLayout.CurrentMode, Is.EqualTo(InvestigationNavigationLabelMode.Full));
            Assert.That(headerLayout, Is.Not.Null);
            Assert.That(headerLayout.IsCompact, Is.False);
            Assert.That(headerLayout.AccessibleTitle, Does.EndWith("CASE FILES"));

            InvestigationStatusBannerView statusBanner = Object.FindAnyObjectByType<InvestigationStatusBannerView>();
            Assert.That(statusBanner, Is.Not.Null);
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("NEXT STEP"));
            Assert.That(statusBanner.CurrentMessage, Does.Contain("Review the historical records"));
            Assert.That(statusBanner.CurrentTone, Is.EqualTo(InvestigationStatusTone.Guide));
            Assert.That(statusBanner.MessageText.fontSize, Is.GreaterThanOrEqualTo(18));

            Text[] labels = Object.FindObjectsByType<Text>();
            Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("CASE FILES")));
            Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("The Shifting Seamount")));

            Canvas.ForceUpdateCanvases();
            Button[] buttons = Object.FindObjectsByType<Button>();
            Assert.That(buttons.Length, Is.GreaterThanOrEqualTo(8));
            for (int index = 0; index < buttons.Length; index++)
            {
                Text buttonLabel = buttons[index].GetComponentInChildren<Text>();
                Assert.That(buttonLabel, Is.Not.Null, $"Button {buttons[index].name} must have a Text child.");
                Assert.That(
                    string.IsNullOrWhiteSpace(buttonLabel.text),
                    Is.False,
                    $"Button {buttons[index].name} must display its label.");
                Assert.That(
                    buttons[index].GetComponent<RectTransform>().rect.height,
                    Is.GreaterThanOrEqualTo(44f),
                    $"Button {buttons[index].name} must remain readable and touch friendly at the default WebGL scale.");
            }

            Text caseContent = FindText("The Shifting Seamount");
            Assert.That(caseContent, Is.Not.Null);
            Canvas.ForceUpdateCanvases();
            Assert.That(caseContent.rectTransform.rect.height, Is.GreaterThan(1f), "Case content must have a visible layout height.");
            InvestigationCaseFilesPanelView caseFilesPanel = Object.FindAnyObjectByType<InvestigationCaseFilesPanelView>();
            Assert.That(caseFilesPanel, Is.Not.Null);
            Assert.That(caseFilesPanel.Depth, Is.EqualTo("Depth range: Shallow."));
            Assert.That(caseFilesPanel.Temperature, Is.EqualTo("Temperature: Cold water below 12°C."));
            Assert.That(caseFilesPanel.Habitat, Is.EqualTo("Habitat: Rocky reef, Cold sensitive."));
            Assert.That(caseFilesPanel.Sensitivity, Is.EqualTo("Sensitivity: Rocky reef, Cold sensitive."));
            Assert.That(caseFilesPanel.Depth, Does.Not.Contain("\n"));
            InvestigationResponsiveGridLayout traitGrid = caseFilesPanel.GetComponentInChildren<InvestigationResponsiveGridLayout>();
            Assert.That(traitGrid, Is.Not.Null);
            Assert.That(traitGrid.CurrentColumns, Is.GreaterThanOrEqualTo(2));

            InvestigationButtonView previousSpecies = FindButtonView("<  PREVIOUS SPECIES");
            InvestigationButtonView nextSpecies = FindButtonView("NEXT SPECIES  >");
            InvestigationButtonView startComparison = FindButtonView("START COMPARISON");
            Assert.That(previousSpecies, Is.Not.Null);
            Assert.That(nextSpecies, Is.Not.Null);
            Assert.That(startComparison, Is.Not.Null);
            Assert.That(previousSpecies.CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Browse));
            Assert.That(nextSpecies.CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Browse));
            Assert.That(startComparison.CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Commit));
            Assert.That(previousSpecies.BackgroundColor, Is.Not.EqualTo(startComparison.BackgroundColor));
            Assert.That(previousSpecies.LabelColor, Is.Not.EqualTo(startComparison.LabelColor));
            Assert.That(previousSpecies.transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(nextSpecies.transform.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(startComparison.transform.GetSiblingIndex(), Is.EqualTo(3), "Forward-stage actions belong in the rightmost column.");
            Assert.That(startComparison.transform.position.x, Is.GreaterThan(nextSpecies.transform.position.x + nextSpecies.GetComponent<RectTransform>().rect.width));
            GridLayoutGroup standardActionsGrid = previousSpecies.transform.parent.GetComponent<GridLayoutGroup>();
            Assert.That(standardActionsGrid, Is.Not.Null);
            Assert.That(standardActionsGrid.padding.vertical, Is.EqualTo(8));
            Assert.That(standardActionsGrid.cellSize.y, Is.EqualTo(48f));

            FindButton("3  BUILD HYPOTHESIS").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(FindButtonView("1  CASE FILES").IsCompletedNavigation, Is.False);
            Assert.That(FindButtonView("2  COMPARE DATA").IsCompletedNavigation, Is.False);

            InvestigationStepperView theoryBeforeFinding = FindStepper("THEORY");
            InvestigationStepperView emptyFinding = FindStepper("FINDING");
            Assert.That(theoryBeforeFinding, Is.Not.Null);
            Assert.That(emptyFinding, Is.Not.Null);
            Assert.That(emptyFinding.ValueLabel, Is.EqualTo("No identified finding yet"));
            Assert.That(FindButton("ASSIGN SUPPORT").interactable, Is.False);
            Assert.That(FindButton("ASSIGN CHALLENGE").interactable, Is.False);
            Assert.That(FindButtonView("SELECT THEORY  >").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Commit));
            Assert.That(FindButtonView("<  PREVIOUS THEORY"), Is.Null, "Theory browsing belongs inside the content panel.");

            FindButton("2  COMPARE DATA").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            SampleComparisonBoardView board = Object.FindAnyObjectByType<SampleComparisonBoardView>();
            SpeciesComparisonCardView[] cards = Object.FindObjectsByType<SpeciesComparisonCardView>();
            Assert.That(board, Is.Not.Null);
            Assert.That(cards.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(FindText("eDNA\nDETECTED"), Is.Not.Null);
            Assert.That(FindText("FISH\nSILHOUETTE"), Is.Null, "Detected cards must display the marker supplied by the runtime view.");
            Assert.That(FindText("SELECT THIS CARD TO CLASSIFY"), Is.Not.Null);
            Assert.That(FindButton("EXPECTED BUT MISSING"), Is.Not.Null);
            Assert.That(FindButton("EXPECTED BUT MISSING").interactable, Is.False, "Classification choices stay disabled until a card is selected.");
            Assert.That(FindButton("MATCHES BASELINE"), Is.Not.Null);
            Text classificationPrompt = FindNamedText("Classification Prompt");
            Assert.That(classificationPrompt, Is.Not.Null);
            Assert.That(classificationPrompt.gameObject.activeInHierarchy, Is.True);
            Assert.That(classificationPrompt.text, Is.EqualTo("CLASSIFY THIS CARD"));
            Transform classificationParent = FindButton("NEW ARRIVAL").transform.parent;
            Assert.That(FindButton("EXPECTED BUT MISSING").transform.parent, Is.EqualTo(classificationParent));
            Assert.That(FindButton("DIFFERENT DEPTH").transform.parent, Is.EqualTo(classificationParent));
            Assert.That(FindButton("RESULT WARNING").transform.parent, Is.EqualTo(classificationParent));
            Assert.That(FindButton("MATCHES BASELINE").transform.parent, Is.EqualTo(classificationParent));
            Assert.That(classificationParent.childCount, Is.EqualTo(5), "All five classification choices must share one responsive region.");
            GridLayoutGroup classificationGrid = classificationParent.GetComponent<GridLayoutGroup>();
            InvestigationResponsiveGridLayout responsiveClassification = classificationParent.GetComponent<InvestigationResponsiveGridLayout>();
            Assert.That(classificationGrid, Is.Not.Null);
            Assert.That(responsiveClassification, Is.Not.Null);
            Assert.That(responsiveClassification.CurrentColumns, Is.EqualTo(5));
            float classificationWidth = classificationGrid.padding.horizontal
                + responsiveClassification.CurrentColumns * classificationGrid.cellSize.x
                + (responsiveClassification.CurrentColumns - 1) * classificationGrid.spacing.x;
            Assert.That(
                classificationWidth,
                Is.LessThanOrEqualTo(classificationParent.GetComponent<RectTransform>().rect.width + 0.5f),
                "Classification choices must fit the WebGL-width row without clipping.");
            Assert.That(shellLayout.IsComparisonMode, Is.True);
            Assert.That(shellLayout.CurrentActionsHeight, Is.InRange(137.9f, 138.1f));
            Assert.That(FindButtonView("BUILD HYPOTHESIS").transform.GetSiblingIndex() % 4, Is.EqualTo(3));
            Assert.That(FindButtonView("BUILD HYPOTHESIS").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Commit));
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("NEXT STEP"));
            Assert.That(statusBanner.CurrentMessage, Does.Contain("Select a comparison card"));

            ScrollRect contentScroll = Object.FindAnyObjectByType<ScrollRect>();
            Assert.That(contentScroll, Is.Not.Null);
            Assert.That(contentScroll.scrollSensitivity, Is.EqualTo(12f));
            Assert.That(contentScroll.movementType, Is.EqualTo(ScrollRect.MovementType.Clamped));
            Assert.That(contentScroll.elasticity, Is.Zero);
            Assert.That(contentScroll.verticalScrollbar, Is.Not.Null, "Scrollable comparison content needs a visible affordance.");

            SpeciesComparisonCardView coldFishCard = null;
            for (int index = 0; index < cards.Length; index++)
            {
                if (cards[index].name.Contains("Cold-water Fish A"))
                {
                    coldFishCard = cards[index];
                    break;
                }
            }

            Assert.That(coldFishCard, Is.Not.Null);
            InvestigationResponsiveComparisonCardLayout cardLayout = coldFishCard.GetComponent<InvestigationResponsiveComparisonCardLayout>();
            Assert.That(cardLayout, Is.Not.Null);
            Assert.That(cardLayout.IsCompact, Is.False);
            Assert.That(coldFishCard.IsAwaitingSelection, Is.True);
            Assert.That(coldFishCard.TraitsLabel, Does.Contain("Rocky reef"));
            Assert.That(coldFishCard.TraitsLabel, Does.Contain("Cold sensitive"));
            Assert.That(coldFishCard.TraitsLabel, Does.Not.Contain("RockyReef"));
            Assert.That(coldFishCard.TraitsLabel, Does.Not.Contain("ColdSensitive"));
            contentScroll.verticalNormalizedPosition = 0.35f;
            Canvas.ForceUpdateCanvases();
            float scrollBeforeSelection = contentScroll.verticalNormalizedPosition;
            coldFishCard.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(contentScroll.verticalNormalizedPosition, Is.EqualTo(scrollBeforeSelection).Within(0.02f), "Selecting a comparison card must not jump the board back to the top.");
            Assert.That(FindText("SELECTED - CHOOSE A CLASSIFICATION BELOW"), Is.Not.Null);
            Assert.That(FindCard("Cold-water Fish A").IsAwaitingSelection, Is.False);
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("NEXT STEP"));
            Assert.That(statusBanner.CurrentMessage, Does.Contain("Choose the classification"));

            FindButton("NEW ARRIVAL").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(controller.State.MisclassificationCount, Is.EqualTo(1));
            Assert.That(contentScroll.verticalNormalizedPosition, Is.EqualTo(scrollBeforeSelection).Within(0.02f), "A classification result must preserve the comparison position.");
            Button ruledOut = FindButton("RULED OUT: NEW ARRIVAL");
            Assert.That(ruledOut, Is.Not.Null);
            Assert.That(ruledOut.interactable, Is.False);
            InvestigationButtonView ruledOutView = ruledOut.GetComponent<InvestigationButtonView>();
            Assert.That(ruledOutView.BackgroundColor, Is.EqualTo(InvestigationTheme.BackgroundDeep));
            Assert.That(ruledOutView.LabelColor, Is.EqualTo(InvestigationTheme.TextMuted));
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("TRY AGAIN"));
            Assert.That(statusBanner.CurrentTone, Is.EqualTo(InvestigationStatusTone.Warning));

            FindButton("EXPECTED BUT MISSING").onClick.Invoke();
            yield return null;

            Assert.That(controller.State.IdentifiedEvidenceIds, Has.Count.EqualTo(1));
            Assert.That(FindText("IDENTIFIED: EXPECTED BUT MISSING"), Is.Not.Null);
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("FINDING IDENTIFIED"));
            Assert.That(statusBanner.CurrentTone, Is.EqualTo(InvestigationStatusTone.Success));
            Assert.That(FindButtonView("2  COMPARE DATA").IsCompletedNavigation, Is.True);

            FindButton("3  BUILD HYPOTHESIS").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            InvestigationHypothesisPanelView hypothesisPanel = Object.FindAnyObjectByType<InvestigationHypothesisPanelView>();
            Assert.That(hypothesisPanel, Is.Not.Null);
            InvestigationStepperView theoryStepper = FindStepper("THEORY");
            InvestigationStepperView findingStepper = FindStepper("FINDING");
            Assert.That(theoryStepper, Is.Not.Null);
            Assert.That(findingStepper, Is.Not.Null);
            Assert.That(theoryStepper.ValueLabel, Does.Contain("Ocean warming"));
            Assert.That(findingStepper.ValueLabel, Does.Contain("not detected").IgnoreCase);
            Assert.That(hypothesisPanel.StatusLabel, Is.EqualTo("UNEXPLORED"));
            Assert.That(hypothesisPanel.HypothesisMetadata, Does.Contain("Evidence needed: New arrival, Different depth"));
            Assert.That(hypothesisPanel.HypothesisMetadata, Does.Not.Contain("NewDetection"));
            Assert.That(hypothesisPanel.HypothesisMetadata, Does.Not.Contain("DepthShift"));
            Assert.That(FindButtonView("<  COMPARE DATA").transform.GetSiblingIndex(), Is.EqualTo(0), "Back-stage actions belong in the leftmost column.");
            Assert.That(FindButtonView("<  COMPARE DATA").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Browse));
            Assert.That(FindButtonView("ASSIGN SUPPORT").transform.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(FindButtonView("ASSIGN CHALLENGE").transform.GetSiblingIndex(), Is.EqualTo(2));
            Assert.That(FindButtonView("SELECT THEORY  >").transform.GetSiblingIndex(), Is.EqualTo(3));
            Assert.That(FindButtonView("SELECT THEORY  >").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Commit));

            FindButton("ASSIGN CHALLENGE").onClick.Invoke();
            yield return null;
            FindButton("SELECT THEORY  >").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            InvestigationSamplePlannerPanelView plannerPanel = Object.FindAnyObjectByType<InvestigationSamplePlannerPanelView>();
            Assert.That(plannerPanel, Is.Not.Null);
            Assert.That(FindButtonView("3  BUILD HYPOTHESIS").IsCompletedNavigation, Is.True);
            Assert.That(FindStepper("SITE"), Is.Not.Null);
            Assert.That(FindStepper("DEPTH"), Is.Not.Null);
            Assert.That(FindStepper("TEST TARGET"), Is.Not.Null);
            Assert.That(FindButtonView("<  COMPARE RESULTS").transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(FindButtonView("<  COMPARE RESULTS").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Browse));
            Assert.That(FindButtonView("COLLECT SAMPLE  >").transform.GetSiblingIndex(), Is.EqualTo(3));
            Assert.That(FindButtonView("COLLECT SAMPLE  >").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Commit));
            Assert.That(FindButtonView("<  PREVIOUS SITE"), Is.Null, "Site browsing belongs inside the content panel.");

            FindButton("COLLECT SAMPLE  >").onClick.Invoke();
            yield return null;
            Assert.That(FindButtonView("4  PLAN SAMPLE").IsCompletedNavigation, Is.True);
            FindButton("4  PLAN SAMPLE").onClick.Invoke();
            yield return null;
            FindButton("COLLECT SAMPLE  >").onClick.Invoke();
            yield return null;
            FindButton("4  PLAN SAMPLE").onClick.Invoke();
            yield return null;
            Assert.That(FindButton("COLLECT SAMPLE  >").interactable, Is.False, "Collect must be disabled when no sample slots remain.");

            FindButton("5  CONCLUSION").onClick.Invoke();
            yield return null;
            Assert.That(FindText("Theory selected"), Is.Not.Null);
            Assert.That(FindText("Evidence supports it"), Is.Not.Null);
            Assert.That(FindText("Follow-up sample completed"), Is.Not.Null);
            Assert.That(FindText("At least one challenge assigned"), Is.Not.Null);
            Assert.That(FindButtonView("BUILD HYPOTHESIS"), Is.Null);
            Assert.That(FindButtonView("COMPARE DATA"), Is.Null);
            Assert.That(FindButtonView("PLAN SAMPLE"), Is.Null);
            Assert.That(FindButtonView("RESTART CASE").transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(FindButtonView("RESTART CASE").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Destructive));
            Assert.That(FindButtonView("RESTART CASE").BackgroundColor.a, Is.EqualTo(1f), "Destructive actions need an opaque surface so their outline cannot cover the label.");
            Assert.That(FindButtonView("RESTART CASE").BackgroundColor, Is.Not.EqualTo(FindButtonView("RESTART CASE").LabelColor));
            Assert.That(FindButtonView("SUBMIT CONCLUSION  >").transform.GetSiblingIndex(), Is.EqualTo(3));
            Assert.That(FindButtonView("SUBMIT CONCLUSION  >").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Commit));
            Assert.That(FindButton("SUBMIT CONCLUSION  >").interactable, Is.False, "Submission stays disabled until every checklist gate passes.");

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InvestigationScene_NewSampleShowsLatestResultAndReturnsToComparison()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);
            yield return loadOperation;
            yield return null;

            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.State.AllResults.Count, Is.EqualTo(2));

            FindButton("4  PLAN SAMPLE").onClick.Invoke();
            yield return null;
            FindButton("COLLECT SAMPLE  >").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(controller.State.AllResults.Count, Is.EqualTo(3));
            Assert.That(FindNamedText("Title").text, Does.EndWith("COMPARE DATA"));
            Assert.That(FindNamedText("Sample Header").text, Does.StartWith("SAMPLE 3 / 3"));
            Assert.That(FindButtonView("2  COMPARE DATA").IsCurrentNavigation, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InvestigationScene_SelectedAndIdentifiedCardsKeepKeyboardFocusOutline()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);
            yield return loadOperation;
            yield return null;

            FindButton("2  COMPARE DATA").onClick.Invoke();
            yield return null;

            SpeciesComparisonCardView coldFishCard = FindCard("Cold-water Fish A");
            Assert.That(coldFishCard, Is.Not.Null);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(coldFishCard.gameObject);
            coldFishCard.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;

            SpeciesComparisonCardView selectedCard = FindCard("Cold-water Fish A");
            Outline selectedOutline = selectedCard.GetComponent<Outline>();
            Assert.That(selectedCard.IsAwaitingSelection, Is.False);
            Assert.That(selectedCard.enabled, Is.True, "Selected cards must stay enabled to receive keyboard focus events.");
            Assert.That(selectedOutline, Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(selectedCard.gameObject), "Focus should follow the selected card when the comparison board is rebuilt.");

            Assert.That(selectedOutline.effectDistance, Is.EqualTo(new Vector2(2f, -2f)));
            Assert.That(selectedOutline.effectColor, Is.EqualTo(InvestigationTheme.Sand));

            FindButton("EXPECTED BUT MISSING").onClick.Invoke();
            yield return null;
            yield return null;

            SpeciesComparisonCardView identifiedCard = FindCard("Cold-water Fish A");
            Outline identifiedOutline = identifiedCard.GetComponent<Outline>();
            Assert.That(identifiedCard.IsAwaitingSelection, Is.False);
            Assert.That(identifiedCard.enabled, Is.True, "Identified cards must stay enabled to receive keyboard focus events.");
            Assert.That(identifiedOutline, Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(identifiedCard.gameObject), "Focus should remain on the identified card after classification refreshes the board.");

            Assert.That(identifiedOutline.effectDistance, Is.EqualTo(new Vector2(2f, -2f)));
            Assert.That(identifiedOutline.effectColor, Is.EqualTo(InvestigationTheme.Sand));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InvestigationScene_RestartReturnsViewToCaseFiles()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);
            yield return loadOperation;
            yield return null;

            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(controller, Is.Not.Null);

            FindButton("START COMPARISON").onClick.Invoke();
            yield return null;
            Assert.That(FindButtonView("1  CASE FILES").IsCompletedNavigation, Is.True);

            FindButton("5  CONCLUSION").onClick.Invoke();
            yield return null;
            Assert.That(FindButtonView("5  CONCLUSION").IsCurrentNavigation, Is.True);
            InvestigationState stateBeforeRestartRequest = controller.State;

            FindButton("RESTART CASE").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(controller.State, Is.SameAs(stateBeforeRestartRequest), "Requesting restart must not discard progress before confirmation.");
            Assert.That(FindNamedText("Title").text, Does.EndWith("CONCLUSION"));
            Assert.That(FindButton("CONFIRM RESTART"), Is.Not.Null);
            Assert.That(FindButton("CANCEL RESTART"), Is.Not.Null);
            Assert.That(FindButtonView("CANCEL RESTART").transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(FindButtonView("CONFIRM RESTART").transform.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(FindButtonView("CONFIRM RESTART").CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Destructive));
            Assert.That(Object.FindAnyObjectByType<InvestigationStatusBannerView>().CurrentTone, Is.EqualTo(InvestigationStatusTone.Warning));

            FindButton("CANCEL RESTART").onClick.Invoke();
            yield return null;
            Assert.That(FindButton("RESTART CASE"), Is.Not.Null);
            Assert.That(FindButton("CONFIRM RESTART"), Is.Null);
            Assert.That(FindNamedText("Title").text, Does.EndWith("CONCLUSION"));

            FindButton("RESTART CASE").onClick.Invoke();
            yield return null;
            FindButton("CONFIRM RESTART").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(controller.State.AllResults.Count, Is.EqualTo(2));
            Assert.That(controller.State.RemainingSamples, Is.EqualTo(2));
            Assert.That(FindNamedText("Title").text, Does.EndWith("CASE FILES"));
            Assert.That(FindButtonView("1  CASE FILES").IsCurrentNavigation, Is.True);
            Assert.That(FindButtonView("5  CONCLUSION").IsCurrentNavigation, Is.False);
            Assert.That(FindButtonView("1  CASE FILES").IsCompletedNavigation, Is.False);
            Assert.That(FindButtonView("3  BUILD HYPOTHESIS").IsCompletedNavigation, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReducedMotionToggleStopsDecorativeAnimation()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);
            yield return loadOperation;
            yield return null;

            FindButton("2  COMPARE DATA").onClick.Invoke();
            yield return null;

            InvestigationAttentionPulse[] activePulses = Object.FindObjectsByType<InvestigationAttentionPulse>();
            Assert.That(activePulses, Has.Some.Matches<InvestigationAttentionPulse>(pulse => pulse.IsPulsing));
            Assert.That(FindButton("MOTION: FULL"), Is.Not.Null);

            FindButton("MOTION: FULL").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(InvestigationMotionSettings.ReducedMotion, Is.True);
            Assert.That(FindButton("MOTION: REDUCED"), Is.Not.Null);
            InvestigationAttentionPulse[] reducedPulses = Object.FindObjectsByType<InvestigationAttentionPulse>();
            Assert.That(reducedPulses, Has.None.Matches<InvestigationAttentionPulse>(pulse => pulse.IsPulsing));
            Assert.That(Object.FindAnyObjectByType<InvestigationStatusBannerView>().enabled, Is.False);

            FindButton("MOTION: REDUCED").onClick.Invoke();
            yield return null;
            Assert.That(InvestigationMotionSettings.ReducedMotion, Is.False);
            Assert.That(Object.FindObjectsByType<InvestigationAttentionPulse>(), Has.Some.Matches<InvestigationAttentionPulse>(pulse => pulse.IsPulsing));
            LogAssert.NoUnexpectedReceived();
        }


        private static Button FindButton(string label)
        {
            Button[] buttons = Object.FindObjectsByType<Button>();
            for (int index = 0; index < buttons.Length; index++)
            {
                Text text = buttons[index].GetComponentInChildren<Text>();
                if (text != null && text.text == label)
                {
                    return buttons[index];
                }
            }

            return null;
        }

        private static Text FindText(string value)
        {
            Text[] labels = Object.FindObjectsByType<Text>();
            for (int index = 0; index < labels.Length; index++)
            {
                if (labels[index].text.Contains(value))
                {
                    return labels[index];
                }
            }

            return null;
        }

        private static Text FindNamedText(string objectName)
        {
            Text[] labels = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < labels.Length; index++)
            {
                if (labels[index].name == objectName)
                {
                    return labels[index];
                }
            }

            return null;
        }

        private static InvestigationButtonView FindButtonView(string label)
        {
            InvestigationButtonView[] buttons = Object.FindObjectsByType<InvestigationButtonView>();
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].Label == label)
                {
                    return buttons[index];
                }
            }

            return null;
        }

        private static InvestigationStepperView FindStepper(string category)
        {
            InvestigationStepperView[] steppers = Object.FindObjectsByType<InvestigationStepperView>();
            for (int index = 0; index < steppers.Length; index++)
            {
                if (steppers[index].CategoryLabel == category)
                {
                    return steppers[index];
                }
            }

            return null;
        }

        private static SpeciesComparisonCardView FindCard(string nameFragment)
        {
            SpeciesComparisonCardView[] cards = Object.FindObjectsByType<SpeciesComparisonCardView>();
            for (int index = 0; index < cards.Length; index++)
            {
                if (cards[index].name.Contains(nameFragment))
                {
                    return cards[index];
                }
            }

            return null;
        }
    }
}
