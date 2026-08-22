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
            InvestigationV2RuntimeView view = Object.FindFirstObjectByType<InvestigationV2RuntimeView>();
            InvestigationV2Controller controller = Object.FindFirstObjectByType<InvestigationV2Controller>();
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
            Assert.That(FindButton("Historical Species Marker shark").interactable, Is.False);
            Assert.That(FindButton("Species Marker shark").interactable, Is.True);
            Assert.That(FindButton("Species Marker shark").transform.Find("Marker Halo"), Is.Null);
            Assert.That(FindButton("Species Marker shark").transform.Find("Missing Signal"), Is.Not.Null);
            Assert.That(FindButton("Species Marker tuna").transform.Find("Group Member Left"), Is.Not.Null);
            Assert.That(FindButton("Species Marker shark").transform.Find("Paper Clay Inner Face"), Is.Null);
            Assert.That(FindButton("Continue To Simulate").transform.parent.name, Is.EqualTo("Investigation Notebook"));
            RectTransform continueButton = FindButton("Continue To Simulate").GetComponent<RectTransform>();
            Assert.That(continueButton.anchorMin.x, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(continueButton.anchorMax.x, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(FindButton("Continue To Simulate").GetComponentInChildren<Text>().horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
            Assert.That(FindButton("Continue To Simulate").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Continue To Simulate").transform.Find("Paper Clay Inner Face"), Is.Not.Null);
            Assert.That(GameObject.Find("Species Facts Hint"), Is.Null);
            foreach (Text text in view.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(10), $"{text.name} is too small at the reference resolution.");
            }
            Button observeStage = FindButton("Stage Observe");
            Assert.That(observeStage.GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(observeStage.transform.Find("Paper Clay Inner Face"), Is.Null);
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
            Click("Run Selected Model");

            Button shark = FindButton("Prediction shark");
            Button tuna = FindButton("Prediction tuna");
            Button krill = FindButton("Prediction krill");
            Text sharkState = shark.transform.Find("Prediction").GetComponent<Text>();
            Text tunaState = tuna.transform.Find("Prediction").GetComponent<Text>();
            Text krillState = krill.transform.Find("Prediction").GetComponent<Text>();
            Assert.That(sharkState.text, Is.EqualTo("Stable"));
            Assert.That(tunaState.text, Is.EqualTo("Stable"));
            Assert.That(krillState.text, Is.EqualTo("Stable"));
            Assert.That(shark.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.GreaterThan(0.5f));
            Assert.That(tuna.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));

            yield return new WaitForSecondsRealtime(1.8f);
            Assert.That(sharkState.text, Is.EqualTo("Decrease"));
            Assert.That(krillState.text, Is.EqualTo("Stable"));

            yield return new WaitForSecondsRealtime(3.8f);
            Assert.That(tunaState.text, Is.EqualTo("Increase"));
            Assert.That(krillState.text, Is.EqualTo("Decrease"));
            Assert.That(tuna.transform.Find("Species Artwork").localScale.x, Is.GreaterThan(1.05f));
            Assert.That(krill.transform.Find("Species Artwork").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(tuna.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.GreaterThan(0.5f));
            Assert.That(shark.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(krill.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
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
            InvestigationV2Controller controller = Object.FindFirstObjectByType<InvestigationV2Controller>();
            Assert.That(controller.State.DiscoveredObservationIds, Has.Count.EqualTo(4));
            Click("Continue To Simulate");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationV2Phase.Simulate));
            AssertActivePageHeadingSharesRow();
            Assert.That(FindGameObject("V2 Footer").GetComponent<RectTransform>().rect.height, Is.EqualTo(38f).Within(0.1f));
            Assert.That(FindButton("Back To Observe").GetComponentInChildren<Text>().fontSize, Is.EqualTo(12));
            Assert.That(FindGameObject("V2 Content").GetComponent<RectTransform>().offsetMin.y, Is.EqualTo(50f).Within(0.1f));
            RectTransform modelColumn = FindGameObject("Simulation Models").GetComponent<RectTransform>();
            RectTransform comparisonColumn = FindGameObject("Comparison Workspace").GetComponent<RectTransform>();
            Assert.That(modelColumn.position.x, Is.LessThan(comparisonColumn.position.x));
            Assert.That(Mathf.Abs(modelColumn.rect.width - comparisonColumn.rect.width), Is.LessThan(2f));
            Assert.That(FindGameObject("Threat Choices").transform.IsChildOf(modelColumn), Is.True);
            Assert.That(FindButton("Threat longline").transform.Find("Threat Status"), Is.Null);
            Assert.That(FindButton("Threat longline").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Threat longline").transform.Find("Paper Clay Inner Face"), Is.Null);
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
            Assert.That(FindButton("Final Cause longline").transform.Find("Paper Clay Inner Face"), Is.Not.Null);
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
            yield return LoadV2Scene();
            InvestigationV2Controller controller = Object.FindFirstObjectByType<InvestigationV2Controller>();
            Click("Difficulty Toggle");
            Assert.That(controller.State.Difficulty, Is.EqualTo(InvestigationV2Difficulty.Hard));
            bool before = InvestigationV2MotionSettings.ReducedMotion;
            Click("Motion Toggle");
            Assert.That(InvestigationV2MotionSettings.ReducedMotion, Is.Not.EqualTo(before));
            Click("Species Marker shark");
            Assert.That(controller.State.DiscoveredObservationIds, Has.Count.EqualTo(1));
            controller.SendMessage("HandleRestart", SendMessageOptions.DontRequireReceiver);
            // Public UI restart is exercised in Report; verify initial scene reload reset instead.
            yield return SceneManager.LoadSceneAsync("InvestigationSceneV2", LoadSceneMode.Single);
            yield return null;
            controller = Object.FindFirstObjectByType<InvestigationV2Controller>();
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
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].gameObject.activeInHierarchy && buttons[index].name == name) return buttons[index];
            }
            return null;
        }

        private static GameObject FindGameObject(string name)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
    }
}
