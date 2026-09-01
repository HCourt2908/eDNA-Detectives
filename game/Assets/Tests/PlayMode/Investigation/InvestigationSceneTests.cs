using System;
using System.Collections;
using System.IO;
using System.Reflection;
using EDNA.Core;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationSceneTests
    {
        [Test]
        public void OnlyCurrentInvestigationScene_Remains()
        {
            string scenePath = Path.Combine(Application.dataPath, "Scenes/InvestigationScene.unity");
            Assert.That(File.Exists(scenePath), Is.True);
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(scenePath), "InvestigationScene*.unity"), Has.Length.EqualTo(1));
        }

        [Test]
        public void SessionBridge_ClearResultPreservesPendingInput()
        {
            InvestigationGameInput input = new InvestigationGameInput { caseId = "bridge-test" };
            try
            {
                InvestigationSessionBridge.SetInput(input);
                InvestigationSessionBridge.PublishResult(new InvestigationGameResult { completed = true });
                InvestigationSessionBridge.ClearResult();
                Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
                Assert.That(InvestigationSessionBridge.PendingInput, Is.SameAs(input));
            }
            finally
            {
                InvestigationSessionBridge.Clear();
            }
        }

        [Test]
        public void ReportMetadata_CompactsUnboundedExternalDisplayNames()
        {
            MethodInfo compact = typeof(InvestigationRuntimeView).GetMethod(
                "CompactReportMetadataValue",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(compact, Is.Not.Null);
            string value = (string)compact.Invoke(null, new object[]
            {
                "North-East Seamount Expedition Waypoint C",
                15,
                "Survey site"
            });
            Assert.That(value, Has.Length.EqualTo(15));
            Assert.That(value, Does.EndWith("…"));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ExternalInputReportsImportStatusAndRejectsWrongCase()
        {
            InvestigationGameInput validInput = new InvestigationGameInput
            {
                caseId = "investigation_longline_01",
                surveyContext = new InvestigationSurveyContextData
                {
                    surveyId = "survey_from_ctd",
                    surveyDisplayName = "CTD Survey",
                    siteId = "waypoint_b",
                    siteDisplayName = "Waypoint B"
                }
            };
            validInput.discoveredObservationIds.Add("E01_SHARK_NONDETECTION");

            try
            {
                InvestigationSessionBridge.SetInput(validInput);
                yield return LoadInvestigationScene();
                InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
                Assert.That(controller.State.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
                Assert.That(controller.State.SurveyId, Is.EqualTo("survey_from_ctd"));
                GameObject notice = FindGameObject("Status Toast");
                Assert.That(notice.activeSelf, Is.True);
                Assert.That(notice.transform.Find("Status Message").GetComponent<Text>().text, Does.Contain("Imported 1 observation"));
                Assert.That(notice.transform.Find("Status Accent").GetComponent<Image>().color,
                    Is.EqualTo((Color)InvestigationTheme.Primary));

                InvestigationSessionBridge.SetInput(new InvestigationGameInput { caseId = "wrong_case" });
                yield return LoadInvestigationScene();
                Assert.That(FindGameObject("Fatal Error").GetComponent<Text>().text,
                    Does.Contain("External input targets case wrong_case"));
            }
            finally
            {
                InvestigationSessionBridge.Clear();
            }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_BootstrapsObserveCanvasAndAccessibleControls()
        {
            yield return LoadInvestigationScene();
            InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(view, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            CanvasScaler scaler = view.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Vector2 expectedReferenceResolution = Screen.height > Screen.width
                ? new Vector2(720f, 1280f)
                : new Vector2(1280f, 720f);
            Assert.That(scaler.referenceResolution, Is.EqualTo(expectedReferenceResolution));
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Assert.That(scaler.matchWidthOrHeight, Is.Zero);
            Assert.That(view.GetComponent<Canvas>().pixelPerfect, Is.True);
            Assert.That(FindGameObject("Safe Area").GetComponent<InvestigationSafeAreaFitter>(), Is.Not.Null);
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.StartWith("CASE PROGRESS"));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Not.Contain("REVISIONS"));
            Assert.That(FindButton("Stage Observe"), Is.Not.Null);
            Assert.That(FindButton("Stage Simulate"), Is.Not.Null);
            Assert.That(FindButton("Stage Report"), Is.Not.Null);
            Assert.That(FindButton("Species Marker shark").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            RectTransform contentPanel = FindGameObject("Investigation Content").GetComponent<RectTransform>();
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
            Assert.That(FindGameObject("Investigation Footer").activeSelf, Is.False);
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
            RectTransform historicalSharkMarker = FindButton("Historical Species Marker shark").GetComponent<RectTransform>();
            RectTransform currentSharkMarker = FindButton("Species Marker shark").GetComponent<RectTransform>();
            Assert.That(Vector2.Distance(historicalSharkMarker.anchorMin, currentSharkMarker.anchorMin), Is.InRange(0.001f, 0.08f),
                "The two eras should use slightly different but spatially comparable deterministic placements.");
            Assert.That(currentSharkMarker.anchorMin.y, Is.InRange(0.90f, 0.96f));
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
            Assert.That(notebookPrimaryShadow.effectColor, Is.EqualTo((Color)InvestigationTheme.PaperShadow));
            Assert.That(FindButton("Continue To Simulate").GetComponent<Outline>(), Is.Null);
            Assert.That(GameObject.Find("Species Facts Hint"), Is.Null);
            Texture2D sourceSeamount = LoadSeamountSourceTexture();
            float seamountToWaterLuminance = MeanOpaqueSpriteLuminance(sourceSeamount, 0.85f)
                / RelativeLuminance(InvestigationTheme.WaterUpper);
            Assert.That(seamountToWaterLuminance, Is.InRange(0.70f, 0.85f),
                "The approved seamount lighting should sit close to, but slightly below, the surrounding water luminance.");
            AssertImageOnlyMarker("Historical Species Marker shark");
            AssertImageOnlyMarker("Species Marker shark");
            AssertImageOnlyMarker("Species Marker tuna");
            AssertImageOnlyMarker("Species Marker krill");
            AssertImageOnlyMarker("Species Marker sea_star");
            AssertImageOnlyMarker("Species Marker mussel");
            AssertMarkerHabitat("Species Marker shark", currentMountain, sourceSeamount, false);
            AssertMarkerHabitat("Species Marker tuna", currentMountain, sourceSeamount, false);
            AssertMarkerHabitat("Species Marker krill", currentMountain, sourceSeamount, false);
            AssertMarkerHabitat("Species Marker sea_star", currentMountain, sourceSeamount, true);
            AssertMarkerHabitat("Species Marker mussel", currentMountain, sourceSeamount, true);
            Object.DestroyImmediate(sourceSeamount);
            Button observeStage = FindButton("Stage Observe");
            Assert.That(observeStage.GetComponents<Shadow>().Length, Is.EqualTo(1));
            Outline activeStageOutline = observeStage.GetComponent<Outline>();
            Assert.That(activeStageOutline.effectColor, Is.EqualTo((Color)InvestigationTheme.Primary));
            Assert.That(activeStageOutline.effectDistance, Is.EqualTo(new Vector2(3f, -3f)));
            Assert.That(observeStage.transform.Find("Active Stage Accent"), Is.Null,
                "The active stage should use a complete thick border rather than a bottom-only accent.");
            Outline inactiveStageOutline = FindButton("Stage Simulate").GetComponent<Outline>();
            Assert.That(inactiveStageOutline.effectColor, Is.EqualTo((Color)InvestigationTheme.BorderStrong));
            Assert.That(inactiveStageOutline.effectDistance, Is.EqualTo(new Vector2(2f, -2f)));
            GameObject focusRing = observeStage.transform.Find("Focus Ring").gameObject;
            Assert.That(focusRing.activeSelf, Is.False);
            EventSystem.current.SetSelectedGameObject(observeStage.gameObject);
            yield return null;
            Assert.That(focusRing.activeSelf, Is.True);
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(focusRing.activeSelf, Is.False);
            AssertActivePageHeadingSharesRow();
            FieldInfo layoutSeed = typeof(InvestigationRuntimeView).GetField(
                "observeLayoutSessionSeed",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(layoutSeed, Is.Not.Null);
            string previousLayoutSeed = (string)layoutSeed.GetValue(view);
            view.ResetPresentationState();
            string restartedLayoutSeed = (string)layoutSeed.GetValue(view);
            Assert.That(restartedLayoutSeed, Is.Not.EqualTo(previousLayoutSeed),
                "Starting or restarting a case must generate a fresh survey layout seed.");
        }

        [UnityTest]
        public IEnumerator InvestigationScene_WarningToastAppearsOnlyForInvalidActionAndExpires()
        {
            yield return LoadInvestigationScene();
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
            Click("Species Marker mussel");
            Click("Continue To Simulate");
            Click("Threat longline");
            Click("Run Selected Model");
            Assert.That(GameObject.Find("Comparison Guide"), Is.Null);
            Compare("shark", "E01_SHARK_NONDETECTION", "Judge Mismatch");
            toast = FindGameObject("Status Toast");
            Assert.That(toast.activeSelf, Is.True);
            Assert.That(toast.GetComponentInChildren<Text>().text, Does.StartWith("Latest attempt: Incorrect."));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_SpeciesFactsTooltipUsesOneSecondHoverAndKeyboardFocus()
        {
            yield return LoadInvestigationScene();
            Button shark = FindButton("Species Marker shark");
            InvestigationHoverTooltipTrigger trigger = shark.GetComponent<InvestigationHoverTooltipTrigger>();
            Assert.That(trigger, Is.Not.Null);

            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            trigger.OnPointerEnter(new PointerEventData(EventSystem.current));
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            yield return WaitForCondition(
                () => GameObject.Find("Species Facts Tooltip") != null,
                1f,
                "Species facts did not appear after the one-second hover delay.");

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
        public IEnumerator InvestigationScene_SpeciesFactsAreAvailableByTapOnBothMaps()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            PointerEventData tap = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };

            Button historicalShark = FindButton("Historical Species Marker shark");
            ExecuteEvents.Execute(historicalShark.gameObject, tap, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(GameObject.Find("Species Facts Tooltip").transform.Find("Tooltip Title").GetComponent<Text>().text, Is.EqualTo("Shark"));
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);

            Button currentShark = FindButton("Species Marker shark");
            Vector2 positionBeforeRecording = currentShark.GetComponent<RectTransform>().anchorMin;
            ExecuteEvents.Execute(currentShark.gameObject, tap, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(controller.State.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
            Assert.That(FindButton("Species Marker shark").GetComponent<RectTransform>().anchorMin,
                Is.EqualTo(positionBeforeRecording),
                "Recording a finding must not reshuffle the deterministic survey layout.");
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(GameObject.Find("Species Facts Tooltip").transform.Find("Tooltip Title").GetComponent<Text>().text, Is.EqualTo("Shark"));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_NotebookEntriesScrollWithoutCoveringFixedCta()
        {
            yield return LoadInvestigationScene();
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
            Assert.That(notebookScroll.content.childCount, Is.EqualTo(9));
            Assert.That(FindGameObject("Notebook Group FOOD-WEB PATTERN"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Group STABLE CONTROLS"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Group FOLLOW-UP CLUES"), Is.Not.Null);
            Transform firstEntry = FindGameObject("Notebook E01_SHARK_NONDETECTION").transform;
            Assert.That(firstEntry.Find("Evidence Bullet"), Is.Not.Null);
            Assert.That(firstEntry.Find("Paper Line"), Is.Not.Null);
            Assert.That(firstEntry.GetComponent<Image>().color.a, Is.Zero);
            Text firstNotebookTitle = firstEntry.Find("Observation").GetComponent<Text>();
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

            Click("Species Marker shark");
            yield return null;
            yield return null;
            ScrollRect refreshedNotebookScroll = FindGameObject("Notebook Entry Scroll").GetComponent<ScrollRect>();
            Assert.That(refreshedNotebookScroll.verticalNormalizedPosition, Is.LessThanOrEqualTo(0.02f),
                "Recording or reopening an observation must not jump the Notebook back to the top.");
        }

        [UnityTest]
        public IEnumerator InvestigationScene_SimulatorAnimatesStableIntoFoodWebChanges()
        {
            bool reducedMotionBefore = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotion(false);
            yield return LoadInvestigationScene();
            Click("Species Marker shark");
            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            Click("Species Marker mussel");
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
            Assert.That(seaStarState.text, Is.EqualTo("ROV first"));
            Assert.That(musselState.text, Is.EqualTo("Stable"));
            Assert.That(shark.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.GreaterThan(0.5f));
            Assert.That(tuna.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(seaStar.interactable, Is.False);
            Assert.That(seaStar.transform.Find("Crowd Member 1"), Is.Null);

            yield return WaitForCondition(
                () => sharkState.text == "Decrease",
                2.8f,
                "The shark prediction animation did not reach Decrease.");
            Assert.That(krillState.text, Is.EqualTo("Stable"));

            yield return WaitForCondition(
                () => tunaState.text == "Increase"
                    && krillState.text == "Decrease",
                5.5f,
                "The food-web animation sequence did not reach its final state.");
            Assert.That(tunaState.text, Is.EqualTo("Increase"));
            Assert.That(krillState.text, Is.EqualTo("Decrease"));
            Assert.That(seaStarState.text, Is.EqualTo("ROV first"));
            Assert.That(musselState.text, Is.EqualTo("Stable"));
            Assert.That(tuna.transform.Find("Species Artwork").localScale.x, Is.GreaterThan(1.05f));
            Assert.That(krill.transform.Find("Species Artwork").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(tuna.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.GreaterThan(0.5f));
            Assert.That(shark.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(krill.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(seaStar.transform.Find("Crowd Member 1"), Is.Null);
            Assert.That(GameObject.Find("Prediction Versus Survey"), Is.Not.Null);
            CanvasGroup pageGroup = FindGameObject("Page Content").GetComponent<CanvasGroup>();
            Assert.That(pageGroup, Is.Not.Null);
            Click("Prediction shark");
            pageGroup = FindGameObject("Page Content").GetComponent<CanvasGroup>();
            Assert.That(pageGroup.alpha, Is.EqualTo(1f).Within(0.001f));
            InvestigationMotionSettings.SetReducedMotion(reducedMotionBefore);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_CompletePlayerFacingWorkflow_ReachesCorrectReport()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            Assert.That(FindGameObject("Case Subtitle").GetComponent<Text>().text, Does.Contain("SEAMOUNT A"));
            Assert.That(FindGameObject("Case Subtitle").GetComponent<Text>().text, Does.Contain("SURVEY 12"));
            Button observePrimary = FindButton("Continue To Simulate");
            Assert.That(observePrimary.colors.highlightedColor, Is.EqualTo(new Color(0.96f, 0.96f, 0.96f, 1f)),
                "Paper buttons should darken slightly on hover.");
            Assert.That(observePrimary.colors.selectedColor, Is.EqualTo(observePrimary.colors.normalColor),
                "A clicked paper button must not retain its hover tint.");
            Click("Species Marker shark");
            Assert.That(FindGameObject("Notebook E01_SHARK_NONDETECTION").transform.Find("Source").GetComponent<Text>().text,
                Does.StartWith("eDNA ·"));
            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            Click("Species Marker mussel");
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(controller.State.DiscoveredObservationIds, Has.Count.EqualTo(5));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("FINDINGS 5/5"));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Not.Contain("OBS "));
            Assert.That(FindGameObject("Notebook Group FOOD-WEB PATTERN"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Group STABLE CONTROLS"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Group FOOD-WEB PATTERN").transform.GetSiblingIndex(),
                Is.LessThan(FindGameObject("Notebook E01_SHARK_NONDETECTION").transform.GetSiblingIndex()));
            Assert.That(FindGameObject("Notebook Group STABLE CONTROLS").transform.GetSiblingIndex(),
                Is.LessThan(FindGameObject("Notebook E04_BENTHIC_STABLE").transform.GetSiblingIndex()));
            Click("Continue To Simulate");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            AssertSimulateColumnsUseIndependentHeaderHeights();
            Assert.That(FindGameObject("Investigation Footer").activeSelf, Is.False);
            Assert.That(FindButton("Back To Observe").GetComponentInChildren<Text>().fontSize, Is.EqualTo(11));
            Assert.That(FindButton("Back To Observe").GetComponentInChildren<Text>().text, Is.EqualTo("← Back to notebook"));
            Assert.That(FindGameObject("Investigation Content").GetComponent<RectTransform>().offsetMin.y, Is.EqualTo(8f).Within(0.1f));
            RectTransform modelColumn = FindGameObject("Simulation Models").GetComponent<RectTransform>();
            RectTransform comparisonColumn = FindGameObject("Comparison Workspace").GetComponent<RectTransform>();
            Assert.That(modelColumn.position.x, Is.LessThan(comparisonColumn.position.x));
            Assert.That(Mathf.Abs(modelColumn.rect.width - comparisonColumn.rect.width), Is.LessThan(2f));
            Assert.That(FindGameObject("Threat Choices").transform.IsChildOf(modelColumn), Is.True);
            Assert.That(FindGameObject("Simulation Navigation").transform.IsChildOf(comparisonColumn), Is.True);
            Assert.That(FindButton("Back To Observe").transform.IsChildOf(FindGameObject("Simulation Navigation").transform), Is.True);
            Assert.That(FindGameObject("Simulation Navigation").GetComponent<RectTransform>().rect.height, Is.EqualTo(28f).Within(0.1f));
            Assert.That(FindButton("Back To Observe").GetComponent<RectTransform>().rect.height, Is.EqualTo(28f).Within(0.1f));
            Assert.That(FindButton("Back To Observe").GetComponent<RectTransform>().rect.width, Is.EqualTo(124f).Within(0.1f));
            Assert.That(FindButton("Back To Observe").GetComponentInChildren<Text>().rectTransform.rect.height, Is.EqualTo(24f).Within(0.1f));
            AssertBottomAligned(FindButton("Back To Observe").GetComponent<RectTransform>(), FindGameObject("Simulation Navigation").GetComponent<RectTransform>());
            Assert.That(FindGameObject("Report Gate Hint").GetComponent<RectTransform>().rect.height, Is.EqualTo(28f).Within(0.1f));
            AssertBottomAligned(FindGameObject("Report Gate Hint").GetComponent<RectTransform>(), FindGameObject("Simulation Navigation").GetComponent<RectTransform>());
            Assert.That(comparisonColumn.GetComponent<VerticalLayoutGroup>().padding.bottom, Is.EqualTo(4));
            Assert.That(FindButton("Threat longline").transform.Find("Threat Status"), Is.Not.Null);
            Assert.That(FindGameObject("Case Questions"), Is.Not.Null);
            Assert.That(FindGameObject("Case Questions Heading").GetComponent<Text>().text, Does.Contain("GUIDED"));
            Assert.That(FindGameObject("Case Question warming").GetComponent<Outline>(), Is.Not.Null,
                "With no cause preselected, Easy guidance should begin with the first screening question.");
            Assert.That(GameObject.Find("Case Question benthic"), Is.Null,
                "The decisive benthic question must remain hidden until after the ROV follow-up.");
            Assert.That(FindGameObject("Case Question Grid").transform.childCount, Is.EqualTo(4));
            Assert.That(FindGameObject("Comparison Gate").GetComponent<Text>().text, Does.Contain("Choose a cause"));
            Assert.That(FindGameObject("Model Title").GetComponent<Text>().text, Is.EqualTo("Choose a cause"));
            Assert.That(FindButton("Run Selected Model").interactable, Is.False);
            GameObject scenarioPlaceholder = FindGameObject("Scenario Placeholder Artwork");
            Assert.That(scenarioPlaceholder.GetComponent<Image>().sprite,
                Is.EqualTo(InvestigationScenarioIconLibrary.Investigate));
            Assert.That(scenarioPlaceholder.GetComponent<InvestigationGlyphGraphic>(), Is.Null,
                "The empty scenario must use transparent artwork rather than the procedural warming glyph.");
            Assert.That(GameObject.Find("Scenario Artwork"), Is.Null);
            Assert.That(FindButton("Threat longline").GetComponent<Image>().color,
                Is.Not.EqualTo(new Color32(70, 48, 48, 255)),
                "The correct cause must not be selected before the player investigates it.");
            Assert.That(FindButton("Threat longline").GetComponents<Shadow>().Length, Is.EqualTo(1));
            AssertSimulateWorkspaceWidthFits();
            AssertSimulatePageFitsViewportHeight();

            Click("Threat warming");
            Assert.That(GameObject.Find("Scenario Placeholder Artwork"), Is.Null);
            Assert.That(FindGameObject("Scenario Artwork").GetComponent<Image>(), Is.Not.Null,
                "Selecting a real cause should show that cause's imported transparent artwork.");
            Assert.That(FindGameObject("Case Question warming").GetComponent<Outline>(), Is.Not.Null,
                "Changing cause should move Easy guidance to that cause's next objective.");
            Assert.That(FindGameObject("Case Question food_web").GetComponent<Outline>(), Is.Null);
            Assert.That(FindGameObject("Comparison Gate").GetComponent<Text>().text, Does.Contain("Run Ocean warming"));
            Click("Run Selected Model");
            Assert.That(controller.State.HasDiscoveredObservation("E05_TEMPERATURE_NORMAL"), Is.True);
            Assert.That(FindButton("Prediction Target Temperature temperature"), Is.Not.Null);
            CompareTarget("Prediction Target Temperature temperature", "E05_TEMPERATURE_NORMAL", "Judge Mismatch");

            Click("Threat plastic");
            Click("Run Selected Model");
            Compare("mussel", "E06_PLASTIC_INDICATOR_STABLE", "Judge Mismatch");

            Click("Threat longline");
            Click("Run Selected Model");
            Assert.That(controller.State.HasTriedThreat("longline"), Is.True);
            Assert.That(FindButton("Threat longline").transform.Find("Threat Tried"), Is.Not.Null);
            Assert.That(GameObject.Find("Food Web Prediction"), Is.Not.Null);
            RectTransform environmentalPredictions = FindGameObject("Environmental Predictions").GetComponent<RectTransform>();
            Assert.That(environmentalPredictions.rect.height, Is.EqualTo(44f).Within(0.1f),
                "Larger indicator typography must not change the environment-row height.");
            Transform temperatureIndicator = FindGameObject("Model Indicator TEMP").transform;
            Text temperatureLabel = temperatureIndicator.Find("Indicator Label").GetComponent<Text>();
            Text temperatureValue = temperatureIndicator.Find("Indicator Value").GetComponent<Text>();
            Assert.That(temperatureLabel.fontSize, Is.EqualTo(12));
            Assert.That(temperatureValue.fontSize, Is.EqualTo(14));
            Assert.That(temperatureLabel.rectTransform.TransformPoint(temperatureLabel.rectTransform.rect.center).y,
                Is.GreaterThan(temperatureValue.rectTransform.TransformPoint(temperatureValue.rectTransform.rect.center).y + 4f));
            AssertModelIndicatorTextVisible("TEMP");
            AssertModelIndicatorTextVisible("SEAFLOOR");
            AssertModelIndicatorTextVisible("LOOK FOR");
            AssertFullBorderAccent(temperatureIndicator, InvestigationTheme.Primary);
            AssertFullBorderAccent(FindGameObject("Model Indicator SEAFLOOR").transform, InvestigationTheme.Success);
            AssertFullBorderAccent(FindGameObject("Model Indicator LOOK FOR").transform, InvestigationTheme.Accent);
            Assert.That(temperatureIndicator.Find("Indicator Accent"), Is.Null);
            Transform predictionSelection = FindGameObject("Prediction Selection").transform;
            Transform observationSelection = FindGameObject("Observation Selection").transform;
            AssertFullBorderAccent(predictionSelection, InvestigationTheme.Primary);
            AssertFullBorderAccent(observationSelection, InvestigationTheme.Accent);
            Assert.That(predictionSelection.Find("Panel Accent"), Is.Null);
            Assert.That(observationSelection.Find("Panel Accent"), Is.Null);
            Click("Prediction shark");
            Button guidedSharkObservation = FindButton("Observation E01_SHARK_NONDETECTION");
            Assert.That(guidedSharkObservation.GetComponent<Image>().color,
                Is.Not.EqualTo((Color)InvestigationTheme.SurfaceRaised),
                "A suggested clue must not use the selected-state fill.");
            Assert.That(guidedSharkObservation.transform.Find("Guided Clue Badge"), Is.Not.Null);
            Assert.That(guidedSharkObservation.transform.Find("Selected Evidence Check"), Is.Null);
            Text sharkObservationLabel = FindButton("Observation E01_SHARK_NONDETECTION").GetComponentInChildren<Text>();
            Assert.That(sharkObservationLabel.supportRichText, Is.True);
            Assert.That(sharkObservationLabel.text, Does.Contain("<b><color=#"));
            Assert.That(sharkObservationLabel.text, Does.Contain("not detected</color></b>").IgnoreCase);
            Assert.That(sharkObservationLabel.text, Does.Contain(ColorUtility.ToHtmlStringRGB(InvestigationTheme.Danger)));
            Assert.That(FindButton("Observation E01_SHARK_NONDETECTION").GetComponent<RectTransform>().rect.height, Is.EqualTo(44f).Within(0.1f));
            Click("Prediction tuna");
            Text tunaObservationLabel = FindButton("Observation E02_TUNA_WIDER_DETECTION").GetComponentInChildren<Text>();
            Assert.That(tunaObservationLabel.text, Does.Contain("detected at more sites</color></b>").IgnoreCase);
            Assert.That(tunaObservationLabel.text, Does.Contain(ColorUtility.ToHtmlStringRGB(InvestigationTheme.Accent)));
            Assert.That(GameObject.Find("Comparison Guide"), Is.Null);
            Assert.That(FindButton("Judge Match").transform.Find("State Shape").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(FindGameObject("Food Web Prediction").transform.IsChildOf(FindGameObject("Simulation Models").transform), Is.True);
            Assert.That(FindGameObject("Prediction Observation Pairing").transform.IsChildOf(FindGameObject("Comparison Workspace").transform), Is.True);
            Assert.That(FindGameObject("Judgement Row").transform.IsChildOf(FindGameObject("Comparison Workspace").transform), Is.True);
            Assert.That(FindButton("Prediction shark").GetComponent<RectTransform>().rect.height, Is.EqualTo(78f).Within(0.1f));
            Assert.That(FindGameObject("Reference Indicators").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(64f));
            Assert.That(FindButton("Prediction sea_star").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            Assert.That(FindButton("Prediction sea_star").interactable, Is.False);
            Assert.That(FindButton("Prediction sea_star").transform.Find("Prediction").GetComponent<Text>().text, Is.EqualTo("ROV first"));
            Assert.That(FindGameObject("Indicator Heading").GetComponent<Text>().text, Does.Contain("SEA STAR UNLOCKS AFTER ROV"));
            Assert.That(FindButton("Prediction mussel").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            AssertSimulateWorkspaceWidthFits();
            AssertSimulatePageFitsViewportHeight();
            Image sharkArtwork = FindButton("Prediction shark").transform.Find("Species Artwork").GetComponent<Image>();
            Assert.That(sharkArtwork.sprite, Is.Not.Null);
            Compare("shark", "E01_SHARK_NONDETECTION", "Judge Match");
            Assert.That(FindButton("Prediction shark").transform.Find("Comparison Locked").GetComponent<Image>().sprite, Is.Not.Null);
            Compare("tuna", "E02_TUNA_WIDER_DETECTION", "Judge Match");
            Compare("krill", "E03_KRILL_NONDETECTION", "Judge Match");

            Click("Threat bottom_trawling");
            Click("Run Selected Model");
            Assert.That(controller.State.HasTriedThreat("bottom_trawling"), Is.True);
            Compare("tuna", "E02_TUNA_WIDER_DETECTION", "Judge Match");

            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(6));
            Assert.That(controller.State.AcceptedComparisonCount, Is.EqualTo(6));
            Transform completedQuestionGrid = FindGameObject("Case Question Grid").transform;
            for (int questionIndex = 0; questionIndex < completedQuestionGrid.childCount; questionIndex++)
            {
                Transform child = completedQuestionGrid.GetChild(questionIndex);
                if (!child.name.StartsWith("Case Question ", StringComparison.Ordinal)) continue;
                Assert.That(child.Find("Question Status").GetComponent<Image>().sprite, Is.EqualTo(InvestigationStatusIconLibrary.Check));
            }
            Assert.That(FindButton("Write Provisional Report").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Write Provisional Report").GetComponent<Outline>(), Is.Null);
            Assert.That(FindButton("Write Provisional Report").GetComponent<RectTransform>().rect.height, Is.EqualTo(28f).Within(0.1f));
            Assert.That(FindButton("Write Provisional Report").GetComponent<RectTransform>().rect.width, Is.EqualTo(148f).Within(0.1f));
            Assert.That(FindButton("Write Provisional Report").transform.Find("Compact Navigation Surface").GetComponent<RectTransform>().rect.height, Is.EqualTo(28f).Within(0.1f));
            Click("Write Provisional Report");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            AssertActivePageHeadingSharesRow();
            Assert.That(FindGameObject("Investigation Footer").GetComponent<RectTransform>().rect.height, Is.EqualTo(44f).Within(0.1f));
            Assert.That(FindButton("Back To Simulator").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(126f, 30f)));
            Assert.That(FindButton("Restart Case").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(100f, 30f)));
            Assert.That(FindButton("Review ROV Follow-up").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(104f, 30f)));
            Transform sealedConfirmation = FindGameObject("ROV Confirmation").transform;
            AssertFullBorderAccent(sealedConfirmation, InvestigationTheme.Primary);
            Assert.That(sealedConfirmation.Find("Panel Accent"), Is.Null);
            Canvas.ForceUpdateCanvases();
            AssertSameRow("Report Title", "Report Metadata");
            Assert.That(FindGameObject("Report Title").transform.parent.name, Is.EqualTo("Report Header Row"));
            Assert.That(FindGameObject("Report Metadata").transform.parent.name, Is.EqualTo("Report Header Row"));
            Assert.That(FindGameObject("Report Header Row").GetComponent<RectTransform>().rect.height, Is.EqualTo(44f).Within(0.1f));
            AssertTextFitsItsRect(FindGameObject("Report Title").GetComponent<Text>());
            Text reportMetadata = FindGameObject("Report Metadata").GetComponent<Text>();
            AssertTextFitsItsRect(reportMetadata);
            Assert.That(reportMetadata.resizeTextForBestFit, Is.True);
            Assert.That(reportMetadata.resizeTextMinSize, Is.EqualTo(10));
            Assert.That(reportMetadata.resizeTextMaxSize, Is.EqualTo(13));
            RectTransform reportLeft = FindGameObject("Report Cause And Reasoning").GetComponent<RectTransform>();
            RectTransform reportRight = FindGameObject("Report Evidence And Limitation").GetComponent<RectTransform>();
            Assert.That(reportLeft.position.x, Is.LessThan(reportRight.position.x));
            Assert.That(FindGameObject("Survey Report Paper").GetComponent<RectTransform>().rect.height, Is.LessThan(700f));
            Assert.That(FindButton("Final Cause longline").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Final Cause longline").GetComponent<Outline>(), Is.Null);
            Assert.That(FindButton("Final Cause longline").transform.Find("Paper Choice Face"), Is.Not.Null);
            Assert.That(FindButton("Final Cause longline").colors.highlightedColor,
                Is.EqualTo(new Color(0.96f, 0.96f, 0.96f, 1f)));
            Assert.That(FindButton("Final Cause longline").colors.selectedColor,
                Is.EqualTo(FindButton("Final Cause longline").colors.normalColor));
            Assert.That(ContrastRatio(InvestigationTheme.PaperBorder, InvestigationTheme.Paper), Is.GreaterThanOrEqualTo(3f));
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo("bottom_trawling"));
            Assert.That(controller.State.FinalThreatId, Is.Empty);
            Assert.That(controller.State.HasDiscoveredObservation("E07_FISHING_LINE"), Is.False);

            string provisionalBeforeRestartPrompt = controller.State.ProvisionalThreatId;
            Click("Restart Case");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo(provisionalBeforeRestartPrompt),
                "The first Restart click must not clear progress.");
            Assert.That(FindButton("Restart Case"), Is.Null);
            Assert.That(FindButton("Cancel Restart Case"), Is.Not.Null);
            Assert.That(FindButton("Confirm Restart Case"), Is.Not.Null);
            Assert.That(FindGameObject("Status Toast").activeSelf, Is.True);

            Click("Stage Simulate");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            Click("Stage Report");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo(provisionalBeforeRestartPrompt));
            Assert.That(FindButton("Restart Case"), Is.Not.Null,
                "Leaving Report must cancel its pending restart confirmation.");
            Assert.That(FindButton("Confirm Restart Case"), Is.Null,
                "A stale destructive confirmation must not survive a phase change.");

            Click("Restart Case");
            Click("Cancel Restart Case");
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo(provisionalBeforeRestartPrompt));
            Assert.That(FindButton("Restart Case"), Is.Not.Null);
            Assert.That(FindButton("Confirm Restart Case"), Is.Null);

            ScrollRect reportScroll = FindGameObject("Investigation Content").GetComponent<ScrollRect>();
            reportScroll.verticalNormalizedPosition = 0.35f;
            Canvas.ForceUpdateCanvases();
            Click("Stage Report");
            Assert.That(reportScroll.verticalNormalizedPosition, Is.EqualTo(0.35f).Within(0.02f),
                "Refreshing the active Report stage must preserve the reader's place.");

            Click("Review ROV Follow-up");
            Assert.That(controller.State.ConfirmationReviewed, Is.True);
            Assert.That(controller.State.HasDiscoveredObservation("E07_FISHING_LINE"), Is.True);
            Assert.That(controller.State.HasDiscoveredObservation("E08_SEAFLOOR_INTACT"), Is.True);
            Assert.That(reportScroll.verticalNormalizedPosition, Is.EqualTo(0.35f).Within(0.02f),
                "Reviewing the ROV follow-up must not jump the Report back to the top.");
            AssertSameRow("ROV Title", "ROV Detail");
            Assert.That(FindGameObject("ROV Confirmation").GetComponent<RectTransform>().rect.height, Is.EqualTo(154f).Within(0.1f));
            Transform reviewedConfirmation = FindGameObject("ROV Confirmation").transform;
            AssertFullBorderAccent(reviewedConfirmation, InvestigationTheme.Success);
            Assert.That(reviewedConfirmation.Find("Panel Accent"), Is.Null);
            Rect rovSummary = WorldRect(FindGameObject("ROV Title").GetComponent<RectTransform>());
            Rect rovEvidence = WorldRect(FindGameObject("ROV Evidence").GetComponent<RectTransform>());
            Assert.That(rovSummary.yMin - rovEvidence.yMax, Is.InRange(0f, 9f),
                "The ROV summary and evidence cards should be compact without overlapping.");
            AssertSectionsDoNotOverlap("Report Evidence Section", "Report Limitation Section");
            AssertChildrenStayInsideLayout("Cause Choices");
            AssertChildrenStayInsideLayout("Evidence Choices");
            Assert.That(FindGameObject("Confirmation E07_FISHING_LINE").transform.Find("Confirmation Icon").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(FindGameObject("Confirmation E07_FISHING_LINE").transform.Find("Confirmation Icon").GetComponent<InvestigationGlyphGraphic>(), Is.Null);
            Assert.That(FindButton("Report Evidence E07_FISHING_LINE").transform.Find("Evidence Icon").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(InvestigationEvidenceIconLibrary.FishingLine, Is.Not.Null);
            Assert.That(InvestigationEvidenceIconLibrary.Seafloor, Is.Not.Null);
            Assert.That(InvestigationEvidenceIconLibrary.Laboratory, Is.Not.Null);
            Assert.That(InvestigationEvidenceIconLibrary.EDNASignal, Is.Not.Null);
            Assert.That(FindButton("Re-test Fishing Models"), Is.Not.Null);
            Assert.That(FindButton("Submit Final Report"), Is.Null,
                "The final report action must remain unavailable until the post-ROV benthic comparison is complete.");

            Click("Re-test Fishing Models");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("FINDINGS 5/5"));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("QUESTIONS 6/8"));
            Assert.That(FindGameObject("Case Question benthic"), Is.Not.Null);
            Assert.That(FindButton("Prediction sea_star").interactable, Is.True);
            Assert.That(FindButton("Prediction sea_star").transform.Find("Prediction").GetComponent<Text>().text, Is.EqualTo("Decrease"));
            Compare("sea_star", "E04_BENTHIC_STABLE", "Judge Mismatch");

            Click("Threat longline");
            Assert.That(FindButton("Prediction sea_star").interactable, Is.True);
            Assert.That(FindButton("Prediction sea_star").transform.Find("Prediction").GetComponent<Text>().text, Is.EqualTo("Stable"));
            Compare("sea_star", "E04_BENTHIC_STABLE", "Judge Match");
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(8));
            Assert.That(controller.State.AcceptedComparisonCount, Is.EqualTo(8));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("QUESTIONS 8/8"));
            Assert.That(FindButton("Return To Final Report"), Is.Not.Null);
            Click("Return To Final Report");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(FindButton("Submit Final Report"), Is.Not.Null);
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(reportScroll.content);
            reportScroll.verticalNormalizedPosition = 0.35f;
            Canvas.ForceUpdateCanvases();

            Text provisionalReminder = FindGameObject("Provisional Reminder").GetComponent<Text>();
            Text evidenceProgress = FindGameObject("Evidence Progress").GetComponent<Text>();
            Text reportQuestion = FindGameObject("Report Cause Section").transform.Find("Question").GetComponent<Text>();
            Assert.That(provisionalReminder.GetComponent<LayoutElement>(), Is.Null,
                "The provisional note must use Text.preferredHeight rather than a fixed LayoutElement height.");
            Assert.That(evidenceProgress.GetComponent<LayoutElement>(), Is.Null,
                "Evidence progress must remain readable when its counters wrap.");
            Assert.That(reportQuestion.GetComponent<LayoutElement>(), Is.Null,
                "Report questions must grow for narrow layouts and localisation.");
            Assert.That(provisionalReminder.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow));
            Assert.That(evidenceProgress.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow));
            AssertTextFitsItsRect(provisionalReminder);
            AssertTextFitsItsRect(evidenceProgress);
            AssertTextFitsItsRect(reportQuestion);

            RectTransform reasoningChoices = FindGameObject("Reasoning Choices").GetComponent<RectTransform>();
            RectTransform limitationChoices = FindGameObject("Limitation Choices").GetComponent<RectTransform>();
            Assert.That(reasoningChoices.GetComponent<VerticalLayoutGroup>(), Is.Not.Null,
                "Long reasoning statements should stack instead of sharing one cramped row.");
            Assert.That(reasoningChoices.GetComponent<HorizontalLayoutGroup>(), Is.Null);
            Assert.That(limitationChoices.GetComponent<VerticalLayoutGroup>(), Is.Not.Null,
                "Limitations should use the same reflow-safe stacked choice pattern.");
            Assert.That(limitationChoices.GetComponent<HorizontalLayoutGroup>(), Is.Null);
            Assert.That(reasoningChoices.GetComponent<LayoutElement>(), Is.Null);
            Assert.That(limitationChoices.GetComponent<LayoutElement>(), Is.Null);

            Button wrappingReasoning = reasoningChoices.GetChild(1).GetComponent<Button>();
            Text wrappingReasoningLabel = wrappingReasoning.GetComponentInChildren<Text>();
            float originalReasoningHeight = wrappingReasoning.GetComponent<RectTransform>().rect.height;
            string originalReasoningText = wrappingReasoningLabel.text;
            wrappingReasoningLabel.text = originalReasoningText
                + " This deliberately longer explanation verifies that wrapped scientific reasoning remains fully inside its choice.";
            LayoutRebuilder.MarkLayoutForRebuild(reasoningChoices);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(reasoningChoices);
            Canvas.ForceUpdateCanvases();
            Assert.That(wrappingReasoning.GetComponent<RectTransform>().rect.height,
                Is.GreaterThan(originalReasoningHeight),
                "A reasoning choice must become taller when its label needs additional lines.");
            AssertTextFitsItsRect(wrappingReasoningLabel);
            wrappingReasoningLabel.text = originalReasoningText;
            LayoutRebuilder.MarkLayoutForRebuild(reasoningChoices);
            Canvas.ForceUpdateCanvases();

            InvestigationRuntimeView runtimeView = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            GameObject reportPaperBeforeResize = FindGameObject("Survey Report Paper");
            float reflowScrollPosition = reportScroll.content.rect.height > reportScroll.viewport.rect.height + 1f ? 0.35f : 1f;
            reportScroll.verticalNormalizedPosition = reflowScrollPosition;
            Canvas.ForceUpdateCanvases();
            float expectedReflowPosition = reportScroll.verticalNormalizedPosition;
            FieldInfo viewportSize = typeof(InvestigationRuntimeView).GetField(
                "lastViewportSize",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(viewportSize, Is.Not.Null);
            viewportSize.SetValue(runtimeView, new Vector2(-1000f, -1000f));
            runtimeView.SendMessage("OnRectTransformDimensionsChange", SendMessageOptions.DontRequireReceiver);
            yield return null;
            yield return null;
            Assert.That(FindGameObject("Survey Report Paper"), Is.Not.SameAs(reportPaperBeforeResize),
                "A viewport change must rebuild the active Report with fresh responsive heights.");
            AssertChildrenStayInsideLayout("Cause Choices");
            AssertChildrenStayInsideLayout("Evidence Choices");
            Assert.That(reportScroll.verticalNormalizedPosition, Is.EqualTo(expectedReflowPosition).Within(0.02f),
                "Responsive reflow must preserve the Report reading position when scrolling is available and remain at the top when all content fits.");

            Button reportCheck = FindButton("Submit Final Report");
            Assert.That(reportCheck.interactable, Is.True);
            Assert.That(reportCheck.GetComponentInChildren<Text>().text, Is.EqualTo("Check my report"));
            EventSystem.current.SetSelectedGameObject(reportCheck.gameObject);
            Click("Submit Final Report");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.InsufficientEvidence));
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null,
                "Checking an incomplete report is diagnostic and must not publish a session result.");
            GameObject diagnosticOutcome = FindGameObject("Report Outcome");
            Assert.That(diagnosticOutcome.GetComponentInChildren<Text>().text, Does.Contain("Choose a final cause"));
            Assert.That(diagnosticOutcome.GetComponent<Image>().color, Is.EqualTo((Color)InvestigationTheme.ReportGuide),
                "An incomplete report is guidance, not an error state.");
            Assert.That(diagnosticOutcome.transform.GetSiblingIndex(),
                Is.LessThan(FindGameObject("Report Columns").transform.GetSiblingIndex()),
                "Report feedback must appear before the form rather than below the scrollable report.");
            Image diagnosticIcon = diagnosticOutcome.transform.Find("Outcome Icon").GetComponent<Image>();
            Assert.That(diagnosticIcon.sprite, Is.EqualTo(InvestigationStatusIconLibrary.Question));
            Assert.That(diagnosticOutcome.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null,
                "The result panel must derive its height from the icon and wrapped diagnostic text.");
            Assert.That(diagnosticOutcome.GetComponent<LayoutElement>(), Is.Null);
            Text diagnosticOutcomeText = diagnosticOutcome.transform.Find("Outcome Text").GetComponent<Text>();
            AssertTextFitsItsRect(diagnosticOutcomeText);
            float originalOutcomeHeight = diagnosticOutcome.GetComponent<RectTransform>().rect.height;
            string originalOutcomeText = diagnosticOutcomeText.text;
            diagnosticOutcomeText.text = originalOutcomeText
                + " Re-check every observation against the model prediction, include the ROV confirmation, explain the complete food-web cascade, and record the scientific limitation before sending the report.";
            LayoutRebuilder.MarkLayoutForRebuild(diagnosticOutcome.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(FindGameObject("Survey Report Paper").GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
            Assert.That(diagnosticOutcome.GetComponent<RectTransform>().rect.height,
                Is.GreaterThan(originalOutcomeHeight),
                "The result panel must grow when a diagnostic wraps onto additional lines.");
            AssertTextFitsItsRect(diagnosticOutcomeText);
            diagnosticOutcomeText.text = originalOutcomeText;
            GameObject diagnosticToast = FindGameObject("Status Toast");
            Assert.That(diagnosticToast.activeSelf, Is.True,
                "Checking an incomplete report must surface its diagnostic in the current viewport.");
            Assert.That(diagnosticToast.transform.Find("Status Message").GetComponent<Text>().text,
                Does.Contain("Choose a final cause"));
            Assert.That(diagnosticToast.transform.Find("Status Accent").GetComponent<Image>().color,
                Is.EqualTo((Color)InvestigationTheme.Primary),
                "Incomplete-report guidance must use the neutral guide treatment, not red warning styling.");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Submit Final Report"));

            Button finalCause = FindButton("Final Cause longline");
            EventSystem.current.SetSelectedGameObject(finalCause.gameObject);
            Click("Final Cause longline");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.NotSubmitted));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Final Cause longline"));
            Assert.That(FindButton("Final Cause longline").transform.Find("Selected Check"), Is.Not.Null);

            Click("Submit Final Report");
            Assert.That(FindGameObject("Report Outcome").GetComponentInChildren<Text>().text, Does.Contain("Select at least 4 observations"));
            Click("Report Evidence E01_SHARK_NONDETECTION");
            yield return null;
            Assert.That(FindButton("Report Evidence E01_SHARK_NONDETECTION").transform.Find("Selected Check"), Is.Not.Null);
            Click("Report Evidence E02_TUNA_WIDER_DETECTION");
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("Selected 2 / 4"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("FOOD WEB 2 / 2"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("BENTHIC 0 / 1"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("ROV 0 / 1"));
            Click("Submit Final Report");
            Assert.That(FindGameObject("Report Outcome").GetComponentInChildren<Text>().text, Does.Contain("Select at least 4 observations"));
            Click("Report Evidence E04_BENTHIC_STABLE");
            Click("Report Evidence E07_FISHING_LINE");
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("Selected 4 / 4"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("BENTHIC 1 / 1"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("ROV 1 / 1"));
            Click("Submit Final Report");
            Assert.That(FindGameObject("Report Outcome").GetComponentInChildren<Text>().text, Does.Contain("food-web cascade"));
            Click("Reasoning food_web_cascade");
            Assert.That(FindButton("Reasoning food_web_cascade").transform.Find("Selected Check"), Is.Not.Null);
            Click("Submit Final Report");
            Assert.That(FindGameObject("Report Outcome").GetComponentInChildren<Text>().text, Does.Contain("scientific limitation"));
            Click("Limitation L01_NONDETECTION_LIMITATION");
            Assert.That(FindButton("Limitation L01_NONDETECTION_LIMITATION").transform.Find("Selected Check"), Is.Not.Null);
            Assert.That(reportScroll.verticalNormalizedPosition, Is.EqualTo(0.35f).Within(0.02f),
                "Selecting Report answers must preserve the current scroll position.");
            Assert.That(FindButton("Submit Final Report").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Submit Final Report").GetComponent<Outline>(), Is.Null);
            Assert.That(FindButton("Submit Final Report").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(104f, 30f)));
            reportScroll.verticalNormalizedPosition = 0.15f;
            Click("Submit Final Report");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(reportScroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.02f),
                "Entering Case Closed must start at the top instead of preserving the editable report's scroll position.");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Restart Completed Case"),
                "Keyboard focus should move to the only Case Closed action after submission.");
            InvestigationGameResult bridgeResult = InvestigationSessionBridge.LastResult;
            Assert.That(bridgeResult, Is.Not.Null);
            Assert.That(bridgeResult.correct, Is.True);
            Assert.That(bridgeResult.completed, Is.True);
            Assert.That(bridgeResult.selectedHypothesisId, Is.EqualTo("longline"));
            Assert.That(bridgeResult.surveyId, Is.EqualTo("survey_12"));
            Assert.That(bridgeResult.siteId, Is.EqualTo("seamount_a"));
            Assert.That(bridgeResult.finalSubmissionAttempts, Is.EqualTo(1));
            Assert.That(bridgeResult.missteps, Is.Zero);
            Assert.That(FindGameObject("Report Outcome").GetComponent<Image>().color, Is.EqualTo((Color)InvestigationTheme.ReportSuccess));
            Assert.That(FindGameObject("Report Outcome").transform.Find("Outcome Icon").GetComponent<Image>().sprite,
                Is.EqualTo(InvestigationStatusIconLibrary.Check));
            Assert.That(FindGameObject("Case Closed Summary"), Is.Not.Null);
            Text[] debriefTexts = FindGameObject("Case Closed Summary").GetComponentsInChildren<Text>();
            for (int textIndex = 0; textIndex < debriefTexts.Length; textIndex++) AssertTextFitsItsRect(debriefTexts[textIndex]);
            Assert.That(WorldRect(FindGameObject("Case Closed Summary").GetComponent<RectTransform>()).width,
                Is.LessThanOrEqualTo(WorldRect(reportScroll.viewport).width + 1f));
            Assert.That(FindGameObject("Case Closed Title").GetComponent<Text>().text, Does.Contain("Long-line fishing"));
            Assert.That(FindGameObject("Debrief FOOD-WEB MECHANISM").transform.Find("Debrief Statement").GetComponent<Text>().text,
                Does.Contain("Fewer sharks"));
            Assert.That(FindGameObject("Debrief BENTHIC CHECK").transform.Find("Debrief Statement").GetComponent<Text>().text,
                Does.Contain("Sea star remains stable"));
            Assert.That(FindGameObject("Debrief ROV FOLLOW-UP").transform.Find("Debrief Statement").GetComponent<Text>().text,
                Does.Contain("Fishing line recorded"));
            Assert.That(FindGameObject("Debrief SCIENTIFIC CAUTION").transform.Find("Debrief Statement").GetComponent<Text>().text,
                Does.Contain("Not detected does not mean gone"));
            Assert.That(GameObject.Find("Report Columns"), Is.Null,
                "A solved case should show the focused learning debrief instead of the editable report form.");
            Assert.That(FindGameObject("Report Metadata").GetComponent<Text>().text, Does.Contain("Revision 1"));
            Assert.That(FindGameObject("Report Metadata").GetComponent<Text>().text, Does.Not.Contain("Final attempts"));
            Assert.That(FindButton("Back To Simulator"), Is.Null);
            Assert.That(FindButton("Restart Case"), Is.Null);
            Assert.That(FindButton("Restart Completed Case"), Is.Not.Null);
            Click("Restart Completed Case");
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null,
                "Starting another investigation must clear the previous completed result.");
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReducedMotionDifficultyAndRestartRemainAvailable()
        {
            bool before = InvestigationMotionSettings.ReducedMotion;
            InvestigationSessionBridge.Clear();
            InvestigationMotionSettings.SetReducedMotion(false);
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Button difficulty = FindButton("Difficulty Toggle");
            Color difficultyBaseColor = difficulty.targetGraphic.color;
            Assert.That(difficulty.colors.highlightedColor, Is.EqualTo(new Color(1.12f, 1.12f, 1.12f, 1f)),
                "Dark-surface controls should brighten slightly on hover.");
            Assert.That(difficulty.colors.selectedColor, Is.EqualTo(difficulty.colors.normalColor),
                "Selection focus must not leave a persistent tint on a clicked button.");
            Assert.That(difficulty.colors.pressedColor, Is.Not.EqualTo(difficulty.colors.normalColor),
                "The short press feedback must remain visible.");
            EventSystem.current.SetSelectedGameObject(difficulty.gameObject);
            yield return null;
            Assert.That(difficulty.transform.Find("Focus Ring").gameObject.activeSelf, Is.True,
                "Keyboard selection must show the focus ring.");
            EventSystem.current.SetSelectedGameObject(null);
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, difficulty.transform.position)
            };
            ExecuteEvents.Execute(difficulty.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(difficulty.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(difficulty.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(controller.State.Difficulty, Is.EqualTo(InvestigationDifficulty.Hard));
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(difficulty.targetGraphic.color, Is.EqualTo(difficultyBaseColor),
                "Difficulty must return to its normal background after the press ends.");
            Assert.That(difficulty.transform.Find("Focus Ring").gameObject.activeSelf, Is.False,
                "Pointer selection must not leave a persistent focus highlight.");
            Click("Motion Toggle");
            Assert.That(InvestigationMotionSettings.ReducedMotion, Is.True);
            Click("Species Marker shark");
            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            Click("Species Marker mussel");
            Assert.That(controller.State.DiscoveredObservationIds, Has.Count.EqualTo(5));
            Click("Continue To Simulate");
            Click("Threat plastic");
            Click("Run Selected Model");
            Assert.That(FindGameObject("Case Questions Heading").GetComponent<Text>().text, Does.Contain("INDEPENDENT"));
            Assert.That(FindGameObject("Case Question warming").GetComponent<Outline>(), Is.Null,
                "Hard mode should keep the Case Questions but remove the guided next-question highlight.");
            Assert.That(FindGameObject("Comparison Gate").GetComponent<Text>().text, Does.Contain("Run another untested cause"));
            Assert.That(FindButton("Threat plastic").transform.Find("Threat Status").GetComponent<Text>().text,
                Is.EqualTo("CHECK EVIDENCE"));
            Button mussel = FindButton("Prediction mussel");
            Assert.That(mussel.transform.Find("Prediction").GetComponent<Text>().text, Is.EqualTo("Decrease"));
            Assert.That(mussel.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Click("Prediction mussel");
            Assert.That(FindButton("Observation E06_PLASTIC_INDICATOR_STABLE").transform.Find("Guided Clue Badge"), Is.Null,
                "Hard mode should not label a candidate as the guided clue.");
            controller.SendMessage("HandleRestart", SendMessageOptions.DontRequireReceiver);
            Assert.That(controller.State.Difficulty, Is.EqualTo(InvestigationDifficulty.Hard),
                "Restarting the current case should preserve the player's difficulty preference.");
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(FindButton("Difficulty Toggle").GetComponentInChildren<Text>().text, Is.EqualTo("Hard"));

            // A new scene session has no previous state to retain and starts from the authored Easy default.
            yield return SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            yield return null;
            controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Assert.That(controller.State.Difficulty, Is.EqualTo(InvestigationDifficulty.Easy));
            InvestigationMotionSettings.SetReducedMotion(before);
            InvestigationSessionBridge.Clear();
        }

        private static IEnumerator LoadInvestigationScene()
        {
            yield return SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private static IEnumerator WaitForCondition(Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.That(condition(), Is.True, failureMessage);
        }

        private static void Compare(string speciesId, string evidenceId, string judgementButtonName)
        {
            Click($"Prediction {speciesId}");
            Click($"Observation {evidenceId}");
            Click(judgementButtonName);
        }

        private static void CompareTarget(string targetButtonName, string evidenceId, string judgementButtonName)
        {
            Click(targetButtonName);
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

        [UnityTest]
        public IEnumerator InvestigationScene_WaterLayersSurroundTheInterfaceAndPassInput()
        {
            yield return LoadInvestigationScene();
            Transform background = FindGameObject("Deep Sea Background").transform;
            int safeArea = background.Find("Safe Area").GetSiblingIndex();

            Assert.That(background.Find("Water Column").GetSiblingIndex(), Is.LessThan(safeArea));
            Assert.That(background.Find("God Rays").GetSiblingIndex(), Is.LessThan(safeArea));
            Assert.That(background.Find("Marine Snow Far").GetSiblingIndex(), Is.LessThan(safeArea));
            Assert.That(background.Find("Marine Snow").GetSiblingIndex(), Is.LessThan(safeArea));
            Assert.That(background.Find("Water Vignette").GetSiblingIndex(), Is.LessThan(safeArea),
                "The vignette belongs behind the interface; in front it dims the Notebook paper and header text.");
            Assert.That(background.Find("Marine Snow Foreground").GetSiblingIndex(), Is.GreaterThan(safeArea),
                "Particulate has to pass in front of the scene too, or the seamount reads as a sticker on glass.");

            // Every water layer is decoration: none of it may swallow a click.
            string[] waterLayers =
            {
                "Water Column", "God Rays", "Marine Snow Far", "Marine Snow",
                "Water Vignette", "Marine Snow Foreground",
            };
            for (int index = 0; index < waterLayers.Length; index++)
            {
                Graphic layer = background.Find(waterLayers[index]).GetComponent<Graphic>();
                Assert.That(layer.raycastTarget, Is.False, $"{waterLayers[index]} must not block input.");
            }

            // The survey maps stay translucent so the water column and its
            // particles carry through instead of stopping at the panel edge.
            Assert.That(FindGameObject("Current Seamount").GetComponent<Image>().color.a, Is.LessThan(1f));
            Assert.That(FindGameObject("Historical Seamount").GetComponent<Image>().color.a, Is.LessThan(1f));

            InvestigationVignetteGraphic vignette = background.Find("Water Vignette")
                .GetComponent<InvestigationVignetteGraphic>();
            AssertGraphicMeshCoversRectCorners(vignette);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReducedMotionFreezesAmbientWaterAnimation()
        {
            bool reducedMotionBefore = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotion(true);
            yield return LoadInvestigationScene();
            yield return null;

            string[] animatedLayers =
            {
                "God Rays", "Marine Snow Far", "Marine Snow", "Marine Snow Foreground",
            };
            Vector3[][] frozenVertices = new Vector3[animatedLayers.Length][];
            for (int index = 0; index < animatedLayers.Length; index++)
            {
                Graphic graphic = FindGameObject(animatedLayers[index]).GetComponent<Graphic>();
                frozenVertices[index] = ReadGraphicVertices(graphic);
                Assert.That(frozenVertices[index].Length, Is.GreaterThan(0));
            }

            yield return new WaitForSecondsRealtime(0.2f);
            for (int index = 0; index < animatedLayers.Length; index++)
            {
                Graphic graphic = FindGameObject(animatedLayers[index]).GetComponent<Graphic>();
                AssertVerticesUnchanged(frozenVertices[index], ReadGraphicVertices(graphic), animatedLayers[index]);
            }

            InvestigationMotionSettings.SetReducedMotion(false);
            yield return new WaitForSecondsRealtime(0.2f);
            for (int index = 0; index < animatedLayers.Length; index++)
            {
                Graphic graphic = FindGameObject(animatedLayers[index]).GetComponent<Graphic>();
                AssertAnyVertexMoved(frozenVertices[index], ReadGraphicVertices(graphic), animatedLayers[index]);
            }

            InvestigationMotionSettings.SetReducedMotion(reducedMotionBefore);
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

        private static void AssertSimulateWorkspaceWidthFits()
        {
            ScrollRect scroll = FindGameObject("Investigation Content").GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.content.rect.width, Is.LessThanOrEqualTo(scroll.viewport.rect.width + 1f));
        }

        private static void AssertSimulatePageFitsViewportHeight()
        {
            ScrollRect scroll = FindGameObject("Investigation Content").GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            Rect viewport = WorldRect(scroll.viewport);
            Rect workspace = WorldRect(FindGameObject("Simulate Workspace").GetComponent<RectTransform>());
            Assert.That(workspace.height, Is.LessThanOrEqualTo(viewport.height + 1f),
                "The compact Simulate workspace should fit in one landscape viewport.");
            Assert.That(workspace.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1f));
            Assert.That(WorldRect(FindGameObject("Simulation Models").GetComponent<RectTransform>()).yMin,
                Is.GreaterThanOrEqualTo(viewport.yMin - 1f));
            Assert.That(WorldRect(FindGameObject("Simulation Navigation").GetComponent<RectTransform>()).yMin,
                Is.GreaterThanOrEqualTo(viewport.yMin - 1f));
        }

        private static void AssertSimulateColumnsUseIndependentHeaderHeights()
        {
            Canvas.ForceUpdateCanvases();
            RectTransform workspace = FindGameObject("Simulate Workspace").GetComponent<RectTransform>();
            RectTransform leftColumn = FindGameObject("Simulate Left Column").GetComponent<RectTransform>();
            RectTransform rightColumn = FindGameObject("Simulate Right Column").GetComponent<RectTransform>();
            RectTransform introduction = FindGameObject("Simulate Introduction").GetComponent<RectTransform>();
            RectTransform questions = FindGameObject("Case Questions").GetComponent<RectTransform>();
            RectTransform models = FindGameObject("Simulation Models").GetComponent<RectTransform>();
            RectTransform comparison = FindGameObject("Comparison Workspace").GetComponent<RectTransform>();
            RectTransform title = FindGameObject("Simulate Title").GetComponent<RectTransform>();
            RectTransform description = FindGameObject("Simulate Description").GetComponent<RectTransform>();
            RectTransform questionGrid = FindGameObject("Case Question Grid").GetComponent<RectTransform>();
            Assert.That(leftColumn.parent, Is.EqualTo(workspace));
            Assert.That(rightColumn.parent, Is.EqualTo(workspace));
            Assert.That(introduction.parent, Is.EqualTo(leftColumn));
            Assert.That(questions.parent, Is.EqualTo(rightColumn));
            Assert.That(models.parent, Is.EqualTo(leftColumn));
            Assert.That(comparison.parent, Is.EqualTo(rightColumn));
            Assert.That(leftColumn.position.x, Is.LessThan(rightColumn.position.x));
            Assert.That(Mathf.Abs(leftColumn.rect.width - rightColumn.rect.width), Is.LessThan(3f));
            Assert.That(introduction.rect.height, Is.EqualTo(42f).Within(0.5f));
            Assert.That(questions.rect.height, Is.EqualTo(110f).Within(0.5f));
            Assert.That(WorldRect(introduction).yMax, Is.EqualTo(WorldRect(questions).yMax).Within(1f));
            Assert.That(WorldRect(models).yMax, Is.LessThan(WorldRect(introduction).yMin));
            Assert.That(WorldRect(comparison).yMax, Is.LessThan(WorldRect(questions).yMin));
            Assert.That(title.rect.width, Is.LessThanOrEqualTo(introduction.rect.width + 0.5f));
            Assert.That(description.rect.width, Is.LessThanOrEqualTo(introduction.rect.width + 0.5f));
            Assert.That(questionGrid.parent, Is.EqualTo(questions));
            Assert.That(questionGrid.childCount, Is.EqualTo(4));
            Assert.That(questionGrid.GetComponent<InvestigationResponsiveGridLayout>(), Is.Not.Null);
            Rect firstQuestion = WorldRect(questionGrid.GetChild(0).GetComponent<RectTransform>());
            Rect secondQuestion = WorldRect(questionGrid.GetChild(1).GetComponent<RectTransform>());
            Rect thirdQuestion = WorldRect(questionGrid.GetChild(2).GetComponent<RectTransform>());
            Rect fourthQuestion = WorldRect(questionGrid.GetChild(3).GetComponent<RectTransform>());
            Assert.That(firstQuestion.center.y, Is.EqualTo(secondQuestion.center.y).Within(1f));
            Assert.That(thirdQuestion.center.y, Is.EqualTo(fourthQuestion.center.y).Within(1f));
            Assert.That(firstQuestion.center.x, Is.LessThan(secondQuestion.center.x));
            Assert.That(thirdQuestion.center.x, Is.LessThan(fourthQuestion.center.x));
            Assert.That(thirdQuestion.center.y, Is.LessThan(firstQuestion.center.y));
            for (int index = 0; index < questionGrid.childCount; index++)
            {
                Transform child = questionGrid.GetChild(index);
                if (!child.name.StartsWith("Case Question ", StringComparison.Ordinal)) continue;
                Text prompt = child.Find("Question Prompt").GetComponent<Text>();
                Assert.That(prompt.text, Is.Not.Empty);
                Assert.That(prompt.fontSize, Is.EqualTo(13));
                Assert.That(prompt.rectTransform.rect.height + 0.5f, Is.GreaterThanOrEqualTo(prompt.preferredHeight),
                    $"Case question text is clipped: {prompt.text}");
            }
        }

        private static void AssertModelIndicatorTextVisible(string indicatorId)
        {
            Transform indicator = FindGameObject($"Model Indicator {indicatorId}").transform;
            Text label = indicator.Find("Indicator Label").GetComponent<Text>();
            Text value = indicator.Find("Indicator Value").GetComponent<Text>();
            Assert.That(label.text, Is.Not.Empty);
            Assert.That(value.text, Is.Not.Empty);
            Assert.That(label.rectTransform.rect.height, Is.GreaterThan(8f));
            Assert.That(value.rectTransform.rect.height, Is.GreaterThan(8f));
        }

        private static void AssertFullBorderAccent(Transform target, Color expectedColor)
        {
            Outline outline = target.GetComponent<Outline>();
            Assert.That(outline, Is.Not.Null);
            Assert.That(outline.effectColor, Is.EqualTo(expectedColor));
            Assert.That(outline.effectDistance, Is.EqualTo(new Vector2(2f, -2f)));
        }

        private static void AssertBottomAligned(RectTransform child, RectTransform parent)
        {
            Canvas.ForceUpdateCanvases();
            Rect childRect = WorldRect(child);
            Rect parentRect = WorldRect(parent);
            Assert.That(Mathf.Abs(childRect.yMin - parentRect.yMin), Is.LessThan(1f));
        }

        private static void AssertSameRow(string leftName, string rightName)
        {
            RectTransform left = FindGameObject(leftName).GetComponent<RectTransform>();
            RectTransform right = FindGameObject(rightName).GetComponent<RectTransform>();
            Vector3 leftCenter = left.TransformPoint(left.rect.center);
            Vector3 rightCenter = right.TransformPoint(right.rect.center);
            Assert.That(leftCenter.x, Is.LessThan(rightCenter.x));
            Assert.That(Mathf.Abs(leftCenter.y - rightCenter.y), Is.LessThan(3f));
        }

        private static void AssertSectionsDoNotOverlap(string firstName, string secondName)
        {
            Rect first = WorldRect(FindGameObject(firstName).GetComponent<RectTransform>());
            Rect second = WorldRect(FindGameObject(secondName).GetComponent<RectTransform>());
            Assert.That(first.Overlaps(second), Is.False, $"{firstName} overlaps {secondName}.");
        }

        private static void AssertChildrenStayInsideLayout(string layoutName)
        {
            RectTransform layout = FindGameObject(layoutName).GetComponent<RectTransform>();
            Rect parentRect = WorldRect(layout);
            for (int index = 0; index < layout.childCount; index++)
            {
                Rect childRect = WorldRect(layout.GetChild(index).GetComponent<RectTransform>());
                Assert.That(childRect.xMin, Is.GreaterThanOrEqualTo(parentRect.xMin - 0.5f), $"{layoutName} child {index} overflows left.");
                Assert.That(childRect.xMax, Is.LessThanOrEqualTo(parentRect.xMax + 0.5f), $"{layoutName} child {index} overflows right.");
                Assert.That(childRect.yMin, Is.GreaterThanOrEqualTo(parentRect.yMin - 0.5f), $"{layoutName} child {index} overflows bottom.");
                Assert.That(childRect.yMax, Is.LessThanOrEqualTo(parentRect.yMax + 0.5f), $"{layoutName} child {index} overflows top.");
            }
        }

        private static void AssertTextFitsItsRect(Text text)
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(text.rectTransform.rect.height + 0.5f, Is.GreaterThanOrEqualTo(text.preferredHeight),
                $"{text.name} needs {text.preferredHeight:0.0}px but only received {text.rectTransform.rect.height:0.0}px.");
        }

        private static void AssertImageOnlyMarker(string buttonName)
        {
            Button marker = FindButton(buttonName);
            Assert.That(marker.GetComponent<RectTransform>().rect.width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(marker.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            Assert.That(marker.transform.Find("Species Name"), Is.Null);
            Assert.That(marker.transform.Find("Observation"), Is.Null);
            Assert.That(marker.transform.Find("Species Artwork"), Is.Not.Null);
            Assert.That(marker.GetComponent<InvestigationHoverTooltipTrigger>(), Is.Not.Null);
            Assert.That(marker.GetComponent<InvestigationFocusRing>(), Is.Not.Null);
        }

        private static Texture2D LoadSeamountSourceTexture()
        {
            string path = Path.Combine(Application.dataPath, "Art/Investigation/Seamount/seamount_hero.png");
            Assert.That(File.Exists(path), Is.True, $"Missing seamount source texture: {path}");
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false), Is.True);
            return texture;
        }

        private static float MeanOpaqueSpriteLuminance(Texture2D texture, float alphaThreshold)
        {
            Color32[] pixels = texture.GetPixels32();
            double total = 0d;
            int count = 0;
            for (int index = 0; index < pixels.Length; index++)
            {
                if (pixels[index].a / 255f <= alphaThreshold) continue;
                total += RelativeLuminance(pixels[index]);
                count++;
            }

            Assert.That(count, Is.GreaterThan(0), "The seamount texture contains no opaque rock pixels.");
            return (float)(total / count);
        }

        private static void AssertGraphicMeshCoversRectCorners(Graphic graphic)
        {
            Vector3[] vertices = ReadGraphicVertices(graphic);
            Rect rect = graphic.rectTransform.rect;
            Vector2[] corners =
            {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMin, rect.yMax),
            };

            for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                bool found = false;
                for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    if (((Vector2)vertices[vertexIndex] - corners[cornerIndex]).sqrMagnitude > 0.01f) continue;
                    found = true;
                    break;
                }
                Assert.That(found, Is.True, $"The vignette mesh does not cover rect corner {corners[cornerIndex]}.");
            }
        }

        private static Vector3[] ReadGraphicVertices(Graphic graphic)
        {
            Canvas.ForceUpdateCanvases();
            Mesh mesh = graphic.canvasRenderer.GetMesh();
            Assert.That(mesh, Is.Not.Null, $"{graphic.name} has no rendered mesh.");
            return mesh.vertices;
        }

        private static void AssertVerticesUnchanged(Vector3[] expected, Vector3[] actual, string layerName)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length), $"{layerName} mesh size changed under Reduced Motion.");
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That((actual[index] - expected[index]).sqrMagnitude, Is.LessThan(0.0001f),
                    $"{layerName} continued moving under Reduced Motion.");
            }
        }

        private static void AssertAnyVertexMoved(Vector3[] before, Vector3[] after, string layerName)
        {
            Assert.That(after.Length, Is.EqualTo(before.Length), $"{layerName} mesh size changed after resuming motion.");
            for (int index = 0; index < before.Length; index++)
            {
                if ((after[index] - before[index]).sqrMagnitude <= 0.0001f) continue;
                return;
            }
            Assert.Fail($"{layerName} did not resume moving after Reduced Motion was disabled.");
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

        private static Rect WorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
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
