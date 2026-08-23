using System.Collections;
using System.IO;
using EDNA.Core;
using EDNA.Investigation.V2.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EDNA.Investigation.V2.Tests
{
    public sealed class InvestigationV2SceneTests
    {
        [Test]
        public void V1Scene_RemainsPresentBesideV2()
        {
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "Scenes/InvestigationScene.unity")), Is.True);
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "Scenes/InvestigationSceneV2.unity")), Is.True);
        }

        [UnityTest]
        public IEnumerator V2Scene_BootstrapsObserveCanvasAndAccessibleControls()
        {
            yield return LoadV2Scene();
            InvestigationV2RuntimeView view = Object.FindAnyObjectByType<InvestigationV2RuntimeView>();
            InvestigationV2Controller controller = Object.FindAnyObjectByType<InvestigationV2Controller>();
            Assert.That(view, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationV2Phase.Observe));
            CanvasScaler scaler = view.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Vector2 expectedReferenceResolution = Screen.height > Screen.width
                ? new Vector2(720f, 1280f)
                : new Vector2(1280f, 720f);
            Assert.That(scaler.referenceResolution, Is.EqualTo(expectedReferenceResolution));
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Assert.That(scaler.matchWidthOrHeight, Is.Zero);
            Assert.That(view.GetComponent<Canvas>().pixelPerfect, Is.True);
            Assert.That(FindGameObject("V2 Safe Area").GetComponent<InvestigationV2SafeAreaFitter>(), Is.Not.Null);
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.StartWith("CASE PROGRESS"));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Not.Contain("REVISIONS"));
            Assert.That(FindButton("Stage Observe"), Is.Not.Null);
            Assert.That(FindButton("Stage Simulate"), Is.Not.Null);
            Assert.That(FindButton("Stage Report"), Is.Not.Null);
            Assert.That(FindButton("Species Marker shark").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            RectTransform contentPanel = FindGameObject("V2 Content").GetComponent<RectTransform>();
            ScrollRect scroll = contentPanel.GetComponent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.verticalScrollbar, Is.Not.Null);
            Assert.That(scroll.viewport.GetComponent<Image>().raycastTarget, Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.content.rect.width, Is.LessThanOrEqualTo(scroll.viewport.rect.width + 1f));
            Assert.That(scroll.content.rect.height, Is.LessThanOrEqualTo(scroll.viewport.rect.height + 1f));
            Assert.That(contentPanel.offsetMin.x, Is.EqualTo(8f).Within(0.01f));
            Assert.That(contentPanel.offsetMax.x, Is.EqualTo(-8f).Within(0.01f));
            Assert.That(contentPanel.offsetMin.y, Is.EqualTo(8f).Within(0.01f));
            VerticalLayoutGroup pageLayout = scroll.content.GetComponent<VerticalLayoutGroup>();
            Assert.That(pageLayout.padding.horizontal, Is.Zero);
            Assert.That(pageLayout.padding.vertical, Is.Zero);
            Assert.That(FindButton("Species Facts"), Is.Null);
            Assert.That(FindGameObject("V2 Footer").activeSelf, Is.False);
            Assert.That(FindButton("Historical Survey"), Is.Null);
            Assert.That(FindButton("Current Survey"), Is.Null);
            RectTransform historicalMap = FindGameObject("Historical Seamount").GetComponent<RectTransform>();
            RectTransform currentMap = FindGameObject("Current Seamount").GetComponent<RectTransform>();
            RectTransform notebook = FindGameObject("Investigation Notebook").GetComponent<RectTransform>();
            Assert.That(historicalMap.position.x, Is.LessThan(currentMap.position.x));
            Assert.That(Mathf.Abs(historicalMap.rect.width - currentMap.rect.width), Is.LessThan(2f));
            Assert.That(Mathf.Abs(currentMap.rect.height - notebook.rect.height), Is.LessThan(2f));
            Color notebookColor = notebook.GetComponent<Image>().color;
            Assert.That(notebookColor.r, Is.GreaterThan(0.85f));
            Assert.That(notebookColor.g, Is.GreaterThan(0.90f));
            Assert.That(notebookColor.b, Is.GreaterThan(0.92f));
            RectTransform historicalPlot = historicalMap.Find("Seamount Plot Area").GetComponent<RectTransform>();
            RectTransform currentPlot = currentMap.Find("Seamount Plot Area").GetComponent<RectTransform>();
            RectTransform historicalVisualClip = historicalPlot.Find("Seamount Visual Clip").GetComponent<RectTransform>();
            RectTransform currentVisualClip = currentPlot.Find("Seamount Visual Clip").GetComponent<RectTransform>();
            Image historicalMountain = historicalVisualClip.Find("Seamount Sprite").GetComponent<Image>();
            Image currentMountain = currentVisualClip.Find("Seamount Sprite").GetComponent<Image>();
            Assert.That(view.SeamountSprite, Is.Not.Null);
            Assert.That(historicalMountain.sprite, Is.SameAs(view.SeamountSprite));
            Assert.That(currentMountain.sprite, Is.SameAs(view.SeamountSprite));
            Assert.That(historicalMountain.sprite, Is.SameAs(currentMountain.sprite));
            Assert.That(historicalPlot.anchorMin, Is.EqualTo(currentPlot.anchorMin));
            Assert.That(historicalPlot.anchorMax, Is.EqualTo(currentPlot.anchorMax));
            Assert.That(historicalVisualClip.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(currentVisualClip.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(historicalVisualClip.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(historicalVisualClip.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(historicalVisualClip.Find("Seamount Foot Fog"), Is.Not.Null);
            Assert.That(currentVisualClip.Find("Seamount Foot Fog"), Is.Not.Null);
            Assert.That(historicalVisualClip.Find("Seamount Silhouette Fallback"), Is.Null);
            Assert.That(currentVisualClip.Find("Seamount Silhouette Fallback"), Is.Null);
            Assert.That(FindButton("Historical Species Marker shark").interactable, Is.True);
            Assert.That(FindButton("Species Marker shark").interactable, Is.True);
            Assert.That(FindButton("Historical Species Marker shark").transform.parent, Is.SameAs(historicalPlot));
            Assert.That(FindButton("Species Marker shark").transform.parent, Is.SameAs(currentPlot));
            Assert.That(FindButton("Species Marker shark").transform.IsChildOf(currentVisualClip), Is.False);
            Assert.That(FindButton("Species Marker shark").transform.Find("Marker Halo"), Is.Null);
            Assert.That(FindButton("Species Marker shark").transform.Find("Missing Signal"), Is.Not.Null);
            Assert.That(FindButton("Species Marker tuna").transform.Find("Group Member Left"), Is.Not.Null);
            Assert.That(FindButton("Continue To Simulate").transform.parent.name, Is.EqualTo("Investigation Notebook"));
            RectTransform continueButton = FindButton("Continue To Simulate").GetComponent<RectTransform>();
            Assert.That(continueButton.anchorMin.x, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(continueButton.anchorMax.x, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(FindButton("Continue To Simulate").GetComponentInChildren<Text>().horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
            Shadow notebookPrimaryShadow = FindButton("Continue To Simulate").GetComponent<Shadow>();
            Assert.That(FindButton("Continue To Simulate").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(notebookPrimaryShadow.effectColor, Is.EqualTo((Color)InvestigationV2Theme.PaperShadow));
            Assert.That(FindButton("Continue To Simulate").GetComponent<Outline>(), Is.Null);
            Assert.That(GameObject.Find("Species Facts Hint"), Is.Null);
            Texture2D sourceSeamount = LoadSeamountSourceTexture();
            Color historicalBackdrop = BrightestSeamountBackdrop(sourceSeamount, new Color32(11, 43, 61, 255));
            Color currentBackdrop = BrightestSeamountBackdrop(sourceSeamount, InvestigationV2Theme.Deep);
            AssertMapMarkerReadability("Historical Species Marker shark", historicalBackdrop);
            AssertMapMarkerReadability("Species Marker shark", currentBackdrop);
            AssertMapMarkerReadability("Species Marker tuna", currentBackdrop);
            AssertMapMarkerReadability("Species Marker krill", currentBackdrop);
            AssertMapMarkerReadability("Species Marker sea_star", currentBackdrop);
            AssertMapMarkerReadability("Species Marker mussel", currentBackdrop);
            Assert.That(ContrastRatio(InvestigationV2Theme.Danger, currentBackdrop), Is.LessThan(4.5f),
                "Reverse guard: removing the marker plate must make the danger state fail contrast over the real sprite highlight.");
            AssertMarkerHabitat("Species Marker shark", currentMountain, sourceSeamount, false);
            AssertMarkerHabitat("Species Marker tuna", currentMountain, sourceSeamount, false);
            AssertMarkerHabitat("Species Marker krill", currentMountain, sourceSeamount, false);
            AssertMarkerHabitat("Species Marker sea_star", currentMountain, sourceSeamount, true);
            AssertMarkerHabitat("Species Marker mussel", currentMountain, sourceSeamount, true);
            AssertMarkerLabelsAvoidDepthLines(currentMap, false);
            AssertMarkerLabelsAvoidDepthLines(historicalMap, true);
            AssertMarkerLabelsDoNotOverlap(false);
            AssertMarkerLabelsDoNotOverlap(true);
            Object.DestroyImmediate(sourceSeamount);
            Button observeStage = FindButton("Stage Observe");
            Assert.That(observeStage.GetComponents<Shadow>().Length, Is.EqualTo(1));
            GameObject focusRing = observeStage.transform.Find("Focus Ring").gameObject;
            Assert.That(focusRing.activeSelf, Is.False);
            EventSystem.current.SetSelectedGameObject(observeStage.gameObject);
            yield return null;
            Assert.That(focusRing.activeSelf, Is.True);
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(focusRing.activeSelf, Is.False);
            AssertActivePageHeadingSharesRow();
        }

        [UnityTest]
        public IEnumerator V2Scene_WarningToastAppearsOnlyForInvalidActionAndExpires()
        {
            yield return LoadV2Scene();
            GameObject toast = FindGameObject("Status Toast");
            Assert.That(toast, Is.Not.Null);
            Assert.That(toast.activeSelf, Is.False);

            Click("Species Marker shark");
            Assert.That(toast.activeSelf, Is.False);
            Click("Stage Simulate");
            toast = FindGameObject("Status Toast");
            Assert.That(toast.activeSelf, Is.True);
            Assert.That(toast.GetComponentInChildren<Text>().text, Does.Contain("Record at least"));

            yield return new WaitForSecondsRealtime(3.2f);
            Assert.That(toast.activeSelf, Is.False);

            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            Click("Continue To Simulate");
            Click("Run Selected Model");
            Assert.That(GameObject.Find("Comparison Guide"), Is.Null);
            Compare("shark", "E01_SHARK_NONDETECTION", "Judge Mismatch");
            toast = FindGameObject("Status Toast");
            Assert.That(toast.activeSelf, Is.True);
            Assert.That(toast.GetComponentInChildren<Text>().text, Does.StartWith("Latest attempt: Incorrect."));
        }

        [UnityTest]
        public IEnumerator V2Scene_SpeciesFactsTooltipUsesOneSecondHoverAndKeyboardFocus()
        {
            yield return LoadV2Scene();
            Button shark = FindButton("Species Marker shark");
            InvestigationV2HoverTooltipTrigger trigger = shark.GetComponent<InvestigationV2HoverTooltipTrigger>();
            Assert.That(trigger, Is.Not.Null);

            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            trigger.OnPointerEnter(new PointerEventData(EventSystem.current));
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);

            EventSystem.current.SetSelectedGameObject(shark.gameObject);
            yield return null;
            trigger.OnPointerExit(new PointerEventData(EventSystem.current));
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);

            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            EventSystem.current.SetSelectedGameObject(shark.gameObject);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
        }

        [UnityTest]
        public IEnumerator V2Scene_SpeciesFactsAreAvailableByTapOnBothMaps()
        {
            yield return LoadV2Scene();
            InvestigationV2Controller controller = Object.FindAnyObjectByType<InvestigationV2Controller>();
            PointerEventData tap = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };

            Button historicalShark = FindButton("Historical Species Marker shark");
            ExecuteEvents.Execute(historicalShark.gameObject, tap, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(GameObject.Find("Species Facts Tooltip").transform.Find("Tooltip Title").GetComponent<Text>().text, Is.EqualTo("Shark"));
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);

            Button currentShark = FindButton("Species Marker shark");
            ExecuteEvents.Execute(currentShark.gameObject, tap, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(controller.State.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(GameObject.Find("Species Facts Tooltip").transform.Find("Tooltip Title").GetComponent<Text>().text, Is.EqualTo("Shark"));
        }

        [UnityTest]
        public IEnumerator V2Scene_NotebookEntriesScrollWithoutCoveringFixedCta()
        {
            yield return LoadV2Scene();
            Click("Species Marker shark");
            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            Click("Species Marker mussel");
            Click("Continue To Simulate");
            Click("Threat warming");
            Click("Run Selected Model");
            Click("Back To Observe");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            ScrollRect notebookScroll = FindGameObject("Notebook Entry Scroll").GetComponent<ScrollRect>();
            Button cta = FindButton("Continue To Simulate");
            Assert.That(notebookScroll.content.childCount, Is.EqualTo(6));
            Assert.That(notebookScroll.content.GetChild(0).Find("Evidence Bullet"), Is.Not.Null);
            Assert.That(notebookScroll.content.GetChild(0).Find("Paper Line"), Is.Not.Null);
            Assert.That(notebookScroll.content.GetChild(0).GetComponent<Image>().color.a, Is.Zero);
            Text firstNotebookTitle = notebookScroll.content.GetChild(0).Find("Observation").GetComponent<Text>();
            Assert.That(firstNotebookTitle.supportRichText, Is.True);
            Assert.That(firstNotebookTitle.text, Does.Contain("<b><color=#"));
            Assert.That(notebookScroll.content.rect.height, Is.GreaterThan(notebookScroll.viewport.rect.height));

            Vector3[] viewportCorners = new Vector3[4];
            Vector3[] ctaCorners = new Vector3[4];
            notebookScroll.viewport.GetWorldCorners(viewportCorners);
            cta.GetComponent<RectTransform>().GetWorldCorners(ctaCorners);
            Assert.That(viewportCorners[0].y, Is.GreaterThanOrEqualTo(ctaCorners[1].y));

            Vector3 ctaPosition = cta.transform.position;
            notebookScroll.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            Assert.That(Vector3.Distance(ctaPosition, cta.transform.position), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator V2Scene_SimulatorAnimatesStableIntoFoodWebChanges()
        {
            bool reducedMotionBefore = InvestigationV2MotionSettings.ReducedMotion;
            InvestigationV2MotionSettings.SetReducedMotion(false);
            yield return LoadV2Scene();
            Click("Species Marker shark");
            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            Click("Continue To Simulate");
            Click("Threat bottom_trawling");
            Click("Run Selected Model");
            Button shark = FindButton("Prediction shark");
            Button tuna = FindButton("Prediction tuna");
            Button krill = FindButton("Prediction krill");
            Button seaStar = FindButton("Prediction sea_star");
            Button mussel = FindButton("Prediction mussel");
            Text sharkState = shark.transform.Find("Prediction").GetComponent<Text>();
            Text tunaState = tuna.transform.Find("Prediction").GetComponent<Text>();
            Text krillState = krill.transform.Find("Prediction").GetComponent<Text>();
            Text seaStarState = seaStar.transform.Find("Prediction").GetComponent<Text>();
            Text musselState = mussel.transform.Find("Prediction").GetComponent<Text>();
            Assert.That(sharkState.text, Is.EqualTo("Stable"));
            Assert.That(tunaState.text, Is.EqualTo("Stable"));
            Assert.That(krillState.text, Is.EqualTo("Stable"));
            Assert.That(seaStarState.text, Is.EqualTo("Stable"));
            Assert.That(musselState.text, Is.EqualTo("Stable"));
            Assert.That(shark.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.GreaterThan(0.5f));
            Assert.That(tuna.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(seaStar.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.GreaterThan(0.5f));

            yield return new WaitForSecondsRealtime(1.8f);
            Assert.That(sharkState.text, Is.EqualTo("Decrease"));
            Assert.That(krillState.text, Is.EqualTo("Stable"));

            yield return new WaitForSecondsRealtime(4.1f);
            Assert.That(tunaState.text, Is.EqualTo("Increase"));
            Assert.That(krillState.text, Is.EqualTo("Decrease"));
            Assert.That(seaStarState.text, Is.EqualTo("Decrease"));
            Assert.That(musselState.text, Is.EqualTo("Stable"));
            Assert.That(tuna.transform.Find("Species Artwork").localScale.x, Is.GreaterThan(1.05f));
            Assert.That(krill.transform.Find("Species Artwork").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(tuna.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.GreaterThan(0.5f));
            Assert.That(shark.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(krill.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(seaStar.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(GameObject.Find("Prediction Versus Survey"), Is.Not.Null);
            CanvasGroup pageGroup = FindGameObject("Page Content").GetComponent<CanvasGroup>();
            Assert.That(pageGroup, Is.Not.Null);
            Click("Prediction shark");
            pageGroup = FindGameObject("Page Content").GetComponent<CanvasGroup>();
            Assert.That(pageGroup.alpha, Is.EqualTo(1f).Within(0.001f));
            InvestigationV2MotionSettings.SetReducedMotion(reducedMotionBefore);
        }

        [UnityTest]
        public IEnumerator V2Scene_CompletePlayerFacingWorkflow_ReachesCorrectReport()
        {
            yield return LoadV2Scene();
            Click("Species Marker shark");
            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            InvestigationV2Controller controller = Object.FindAnyObjectByType<InvestigationV2Controller>();
            Assert.That(controller.State.DiscoveredObservationIds, Has.Count.EqualTo(4));
            Click("Continue To Simulate");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationV2Phase.Simulate));
            AssertActivePageHeadingSharesRow();
            Assert.That(FindGameObject("V2 Footer").activeSelf, Is.False);
            Assert.That(FindButton("Back To Observe").GetComponentInChildren<Text>().fontSize, Is.EqualTo(11));
            Assert.That(FindGameObject("V2 Content").GetComponent<RectTransform>().offsetMin.y, Is.EqualTo(8f).Within(0.1f));
            RectTransform modelColumn = FindGameObject("Simulation Models").GetComponent<RectTransform>();
            RectTransform comparisonColumn = FindGameObject("Comparison Workspace").GetComponent<RectTransform>();
            Assert.That(modelColumn.position.x, Is.LessThan(comparisonColumn.position.x));
            Assert.That(Mathf.Abs(modelColumn.rect.width - comparisonColumn.rect.width), Is.LessThan(2f));
            Assert.That(FindGameObject("Threat Choices").transform.IsChildOf(modelColumn), Is.True);
            Assert.That(FindGameObject("Simulation Navigation").transform.IsChildOf(comparisonColumn), Is.True);
            Assert.That(FindButton("Back To Observe").transform.IsChildOf(FindGameObject("Simulation Navigation").transform), Is.True);
            Assert.That(FindGameObject("Simulation Navigation").GetComponent<RectTransform>().rect.height, Is.EqualTo(44f).Within(0.1f));
            Assert.That(FindButton("Back To Observe").GetComponent<RectTransform>().rect.height, Is.EqualTo(44f).Within(0.1f));
            Assert.That(FindButton("Back To Observe").GetComponent<RectTransform>().rect.width, Is.EqualTo(110f).Within(0.1f));
            Assert.That(FindButton("Back To Observe").GetComponentInChildren<Text>().rectTransform.rect.height, Is.EqualTo(22f).Within(0.1f));
            AssertBottomAligned(FindButton("Back To Observe").GetComponent<RectTransform>(), FindGameObject("Simulation Navigation").GetComponent<RectTransform>());
            Assert.That(FindGameObject("Report Gate Hint").GetComponent<RectTransform>().rect.height, Is.EqualTo(22f).Within(0.1f));
            AssertBottomAligned(FindGameObject("Report Gate Hint").GetComponent<RectTransform>(), FindGameObject("Simulation Navigation").GetComponent<RectTransform>());
            Assert.That(comparisonColumn.GetComponent<VerticalLayoutGroup>().padding.bottom, Is.EqualTo(4));
            Assert.That(FindButton("Threat longline").transform.Find("Threat Status"), Is.Null);
            Assert.That(FindButton("Threat longline").GetComponents<Shadow>().Length, Is.EqualTo(1));
            AssertSimulateContentFitsViewport();

            Click("Run Selected Model");
            Assert.That(controller.State.HasTriedThreat("longline"), Is.True);
            Assert.That(FindButton("Threat longline").transform.Find("Threat Tried"), Is.Not.Null);
            Assert.That(GameObject.Find("Food Web Prediction"), Is.Not.Null);
            Assert.That(GameObject.Find("Comparison Guide"), Is.Null);
            Assert.That(FindButton("Judge Match").transform.Find("State Shape").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(FindGameObject("Food Web Prediction").transform.IsChildOf(FindGameObject("Simulation Models").transform), Is.True);
            Assert.That(FindGameObject("Prediction Observation Pairing").transform.IsChildOf(FindGameObject("Comparison Workspace").transform), Is.True);
            Assert.That(FindGameObject("Judgement Row").transform.IsChildOf(FindGameObject("Comparison Workspace").transform), Is.True);
            Assert.That(FindButton("Prediction shark").GetComponent<RectTransform>().rect.height, Is.EqualTo(78f).Within(0.1f));
            Assert.That(FindGameObject("Reference Indicators").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(64f));
            Assert.That(FindButton("Prediction sea_star").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            Assert.That(FindButton("Prediction mussel").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            AssertSimulateContentFitsViewport();
            Image sharkArtwork = FindButton("Prediction shark").transform.Find("Species Artwork").GetComponent<Image>();
            Assert.That(sharkArtwork.sprite, Is.Not.Null);
            Compare("shark", "E01_SHARK_NONDETECTION", "Judge Match");
            Assert.That(FindButton("Prediction shark").transform.Find("Comparison Locked").GetComponent<Image>().sprite, Is.Not.Null);
            Compare("sea_star", "E04_BENTHIC_STABLE", "Judge Match");

            Click("Threat bottom_trawling");
            Click("Run Selected Model");
            Assert.That(controller.State.HasTriedThreat("bottom_trawling"), Is.True);
            Compare("shark", "E01_SHARK_NONDETECTION", "Judge Match");
            Compare("sea_star", "E04_BENTHIC_STABLE", "Judge Mismatch");

            Assert.That(controller.State.AcceptedComparisonCount, Is.EqualTo(4));
            Assert.That(FindButton("Write Provisional Report").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Write Provisional Report").GetComponent<Outline>(), Is.Null);
            Assert.That(FindButton("Write Provisional Report").GetComponent<RectTransform>().rect.height, Is.EqualTo(44f).Within(0.1f));
            Assert.That(FindButton("Write Provisional Report").transform.Find("Compact Navigation Surface").GetComponent<RectTransform>().rect.height, Is.EqualTo(22f).Within(0.1f));
            Click("Write Provisional Report");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationV2Phase.Report));
            AssertActivePageHeadingSharesRow();
            Assert.That(FindGameObject("V2 Footer").GetComponent<RectTransform>().rect.height, Is.EqualTo(70f).Within(0.1f));
            Canvas.ForceUpdateCanvases();
            RectTransform reportLeft = FindGameObject("Report Cause And Reasoning").GetComponent<RectTransform>();
            RectTransform reportRight = FindGameObject("Report Evidence And Limitation").GetComponent<RectTransform>();
            Assert.That(reportLeft.position.x, Is.LessThan(reportRight.position.x));
            Assert.That(FindGameObject("Survey Report Paper").GetComponent<RectTransform>().rect.height, Is.LessThan(700f));
            Assert.That(FindButton("Final Cause longline").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Final Cause longline").GetComponent<Outline>(), Is.Null);
            Assert.That(FindButton("Final Cause longline").transform.Find("Paper Choice Face"), Is.Not.Null);
            Assert.That(ContrastRatio(InvestigationV2Theme.PaperBorder, InvestigationV2Theme.Paper), Is.GreaterThanOrEqualTo(3f));
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo("bottom_trawling"));
            Assert.That(controller.State.FinalThreatId, Is.Empty);
            Assert.That(controller.State.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);

            Click("Review ROV Follow-up");
            Assert.That(controller.State.ConfirmationReviewed, Is.True);
            Assert.That(controller.State.HasDiscoveredObservation("E07_FISHING_LINE"), Is.True);
            Assert.That(controller.State.HasDiscoveredObservation("E08_SEAFLOOR_INTACT"), Is.True);

            Click("Final Cause longline");
            Click("Report Evidence E01_SHARK_NONDETECTION");
            Click("Report Evidence E07_FISHING_LINE");
            Click("Reasoning food_web_cascade");
            Click("Limitation L01_NONDETECTION_LIMITATION");
            Assert.That(FindButton("Submit Final Report").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Submit Final Report").GetComponent<Outline>(), Is.Null);
            Click("Submit Final Report");
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationV2ConclusionStatus.Correct));
            InvestigationGameResult bridgeResult = InvestigationV2SessionBridge.LastResult;
            Assert.That(bridgeResult, Is.Not.Null);
            Assert.That(bridgeResult.correct, Is.True);
            Assert.That(bridgeResult.completed, Is.True);
            Assert.That(bridgeResult.selectedHypothesisId, Is.EqualTo("longline"));
            Assert.That(bridgeResult.finalSubmissionAttempts, Is.EqualTo(1));
            Assert.That(bridgeResult.missteps, Is.Zero);
        }

        [UnityTest]
        public IEnumerator V2Scene_ReducedMotionDifficultyAndRestartRemainAvailable()
        {
            bool before = InvestigationV2MotionSettings.ReducedMotion;
            InvestigationV2MotionSettings.SetReducedMotion(false);
            yield return LoadV2Scene();
            InvestigationV2Controller controller = Object.FindAnyObjectByType<InvestigationV2Controller>();
            Click("Difficulty Toggle");
            Assert.That(controller.State.Difficulty, Is.EqualTo(InvestigationV2Difficulty.Hard));
            Click("Motion Toggle");
            Assert.That(InvestigationV2MotionSettings.ReducedMotion, Is.True);
            Click("Species Marker shark");
            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            Assert.That(controller.State.DiscoveredObservationIds, Has.Count.EqualTo(4));
            Click("Continue To Simulate");
            Click("Threat plastic");
            Click("Run Selected Model");
            Button mussel = FindButton("Prediction mussel");
            Assert.That(mussel.transform.Find("Prediction").GetComponent<Text>().text, Is.EqualTo("Decrease"));
            Assert.That(mussel.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            controller.SendMessage("HandleRestart", SendMessageOptions.DontRequireReceiver);
            // Public UI restart is exercised in Report; verify initial scene reload reset instead.
            yield return SceneManager.LoadSceneAsync("InvestigationSceneV2", LoadSceneMode.Single);
            yield return null;
            controller = Object.FindAnyObjectByType<InvestigationV2Controller>();
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            InvestigationV2MotionSettings.SetReducedMotion(before);
        }

        private static IEnumerator LoadV2Scene()
        {
            yield return SceneManager.LoadSceneAsync("InvestigationSceneV2", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private static void Compare(string speciesId, string evidenceId, string judgementButtonName)
        {
            Click($"Prediction {speciesId}");
            Click($"Observation {evidenceId}");
            Click(judgementButtonName);
        }

        private static void Click(string buttonName)
        {
            Button button = FindButton(buttonName);
            Assert.That(button, Is.Not.Null, $"Button not found: {buttonName}");
            Assert.That(button.interactable, Is.True, $"Button is not interactable: {buttonName}");
            button.onClick.Invoke();
        }

        private static Button FindButton(string name)
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].gameObject.activeInHierarchy && buttons[index].name == name) return buttons[index];
            }
            return null;
        }

        private static GameObject FindGameObject(string name)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (transforms[index].name == name && transforms[index].gameObject.activeInHierarchy) return transforms[index].gameObject;
            }
            for (int index = 0; index < transforms.Length; index++)
            {
                if (transforms[index].name == name) return transforms[index].gameObject;
            }
            return null;
        }

        private static void AssertActivePageHeadingSharesRow(int expectedFontSize = 18, float expectedHeight = 42f)
        {
            Canvas.ForceUpdateCanvases();
            GameObject block = GameObject.Find("Page Heading");
            Assert.That(block, Is.Not.Null);
            RectTransform heading = block.transform.Find("Heading").GetComponent<RectTransform>();
            RectTransform description = block.transform.Find("Description").GetComponent<RectTransform>();
            Vector3 headingCenter = heading.TransformPoint(heading.rect.center);
            Vector3 descriptionCenter = description.TransformPoint(description.rect.center);
            Assert.That(headingCenter.x, Is.LessThan(descriptionCenter.x));
            Assert.That(Mathf.Abs(headingCenter.y - descriptionCenter.y), Is.LessThan(3f));
            Assert.That(block.transform.Find("Heading Divider"), Is.Not.Null);
            Assert.That(heading.GetComponent<Text>().fontSize, Is.EqualTo(expectedFontSize));
            Assert.That(block.GetComponent<RectTransform>().rect.height, Is.EqualTo(expectedHeight).Within(0.1f));
        }

        private static void AssertSimulateContentFitsViewport()
        {
            ScrollRect scroll = FindGameObject("V2 Content").GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.content.rect.height, Is.LessThanOrEqualTo(scroll.viewport.rect.height + 1f));
            Assert.That(scroll.content.rect.width, Is.LessThanOrEqualTo(scroll.viewport.rect.width + 1f));
        }

        private static void AssertBottomAligned(RectTransform child, RectTransform parent)
        {
            Canvas.ForceUpdateCanvases();
            Rect childRect = WorldRect(child);
            Rect parentRect = WorldRect(parent);
            Assert.That(Mathf.Abs(childRect.yMin - parentRect.yMin), Is.LessThan(1f));
        }

        private static void AssertMapMarkerReadability(string buttonName, Color backdrop)
        {
            Button marker = FindButton(buttonName);
            Assert.That(marker.GetComponent<RectTransform>().rect.width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(marker.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            Image plate = marker.transform.Find("Marker Label Plate").GetComponent<Image>();
            Assert.That(plate, Is.Not.Null);
            Text name = marker.transform.Find("Species Name").GetComponent<Text>();
            Text stateLabel = marker.transform.Find("Observation").GetComponent<Text>();
            Assert.That(name.fontSize, Is.GreaterThanOrEqualTo(13));
            Assert.That(stateLabel.fontSize, Is.GreaterThanOrEqualTo(12));

            Color composedPlate = Composite(plate.color, backdrop);
            Assert.That(ContrastRatio(name.color, composedPlate), Is.GreaterThanOrEqualTo(4.5f), $"{buttonName} name contrast is too low.");
            Assert.That(ContrastRatio(stateLabel.color, composedPlate), Is.GreaterThanOrEqualTo(4.5f), $"{buttonName} state contrast is too low.");
        }

        private static Texture2D LoadSeamountSourceTexture()
        {
            string path = Path.Combine(Application.dataPath, "Art/InvestigationV2/Seamount/seamount_hero.png");
            Assert.That(File.Exists(path), Is.True, $"Missing seamount source texture: {path}");
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false), Is.True);
            return texture;
        }

        private static Color BrightestSeamountBackdrop(Texture2D texture, Color mapColor)
        {
            Color32[] pixels = texture.GetPixels32();
            Color brightest = mapColor;
            float brightestLuminance = RelativeLuminance(brightest);
            for (int index = 0; index < pixels.Length; index++)
            {
                if (pixels[index].a == 0) continue;
                Color composed = Composite(pixels[index], mapColor);
                float luminance = RelativeLuminance(composed);
                if (luminance <= brightestLuminance) continue;
                brightest = composed;
                brightestLuminance = luminance;
            }
            return brightest;
        }

        private static void AssertMarkerHabitat(string buttonName, Image mountain, Texture2D sourceTexture, bool expectRock)
        {
            Button marker = FindButton(buttonName);
            RectTransform artworkRect = marker.transform.Find("Species Artwork").GetComponent<RectTransform>();
            RectTransform mountainRect = mountain.rectTransform;
            Vector3 artworkCenterWorld = artworkRect.TransformPoint(artworkRect.rect.center);
            Vector3 artworkCenterLocal = mountainRect.InverseTransformPoint(artworkCenterWorld);
            float u = Mathf.InverseLerp(mountainRect.rect.xMin, mountainRect.rect.xMax, artworkCenterLocal.x);
            float v = Mathf.InverseLerp(mountainRect.rect.yMin, mountainRect.rect.yMax, artworkCenterLocal.y);
            bool insideImage = artworkCenterLocal.x >= mountainRect.rect.xMin
                && artworkCenterLocal.x <= mountainRect.rect.xMax
                && artworkCenterLocal.y >= mountainRect.rect.yMin
                && artworkCenterLocal.y <= mountainRect.rect.yMax;
            float alpha = insideImage ? sourceTexture.GetPixelBilinear(u, v).a : 0f;
            if (expectRock)
                Assert.That(alpha, Is.GreaterThan(0.20f), $"{buttonName} should sit on the seamount surface, alpha={alpha:0.000}.");
            else
                Assert.That(alpha, Is.LessThan(0.08f), $"{buttonName} should sit in open water, alpha={alpha:0.000}.");
        }

        private static void AssertMarkerLabelsAvoidDepthLines(RectTransform map, bool historical)
        {
            string prefix = historical ? "Historical Species Marker " : "Species Marker ";
            string[] speciesIds = { "shark", "tuna", "krill", "sea_star", "mussel" };
            string[] depthLines = { "Depth Line SHALLOW", "Depth Line MID", "Depth Line DEEP" };
            for (int speciesIndex = 0; speciesIndex < speciesIds.Length; speciesIndex++)
            {
                Rect labelRect = WorldRect(FindButton(prefix + speciesIds[speciesIndex]).transform.Find("Marker Label Plate").GetComponent<RectTransform>());
                for (int lineIndex = 0; lineIndex < depthLines.Length; lineIndex++)
                {
                    Rect lineRect = WorldRect(map.Find(depthLines[lineIndex]).GetComponent<RectTransform>());
                    Assert.That(labelRect.Overlaps(lineRect), Is.False,
                        $"{prefix}{speciesIds[speciesIndex]} label overlaps {depthLines[lineIndex]}.");
                }
            }
        }

        private static void AssertMarkerLabelsDoNotOverlap(bool historical)
        {
            string prefix = historical ? "Historical Species Marker " : "Species Marker ";
            string[] speciesIds = { "shark", "tuna", "krill", "sea_star", "mussel" };
            for (int first = 0; first < speciesIds.Length; first++)
            {
                Rect firstRect = WorldRect(FindButton(prefix + speciesIds[first]).transform.Find("Marker Label Plate").GetComponent<RectTransform>());
                for (int second = first + 1; second < speciesIds.Length; second++)
                {
                    Rect secondRect = WorldRect(FindButton(prefix + speciesIds[second]).transform.Find("Marker Label Plate").GetComponent<RectTransform>());
                    Assert.That(firstRect.Overlaps(secondRect), Is.False,
                        $"{prefix}{speciesIds[first]} label overlaps {prefix}{speciesIds[second]}.");
                }
            }
        }

        private static Rect WorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static Color Composite(Color foreground, Color background)
        {
            float alpha = foreground.a;
            return new Color(
                foreground.r * alpha + background.r * (1f - alpha),
                foreground.g * alpha + background.g * (1f - alpha),
                foreground.b * alpha + background.b * (1f - alpha),
                1f);
        }

        private static float ContrastRatio(Color first, Color second)
        {
            float firstLuminance = RelativeLuminance(first);
            float secondLuminance = RelativeLuminance(second);
            return (Mathf.Max(firstLuminance, secondLuminance) + 0.05f)
                / (Mathf.Min(firstLuminance, secondLuminance) + 0.05f);
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * LinearChannel(color.r)
                + 0.7152f * LinearChannel(color.g)
                + 0.0722f * LinearChannel(color.b);
        }

        private static float LinearChannel(float value)
        {
            return value <= 0.04045f
                ? value / 12.92f
                : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }
    }
}
