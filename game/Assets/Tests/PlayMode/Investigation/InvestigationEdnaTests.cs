using static EDNA.Investigation.Tests.InvestigationWorkbenchTestActions;
using System.Collections;
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
    public sealed class InvestigationEdnaTests
    {
        private static Button Button(string name) => GameObject.Find(name)?.GetComponent<Button>();
        private static Text Speech => GameObject.Find("Edna Speech Text")?.GetComponent<Text>();
        private static void Click(string name)
        {
            Button button = Button(name);
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
        }
        private static IEnumerator Load()
        {
            InvestigationSessionBridge.Clear();
            yield return SceneManager.LoadSceneAsync("InvestigationScene");
            yield return null;
        }
        private static void PointerClick(string name)
        {
            SetLensFor(name);
            Canvas.ForceUpdateCanvases();
            Button button = Button(name);
            Assert.That(button, Is.Not.Null, name);
            RectTransform rect = button.GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, name);
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(handler, Is.EqualTo(button.gameObject), $"EDNA or another surface blocks {name}.");
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
            if (name == "Continue To Simulate" && GameObject.Find("Food Web Assembly") != null)
            {
                Click("Connect Species shark"); Click("Connect Species tuna");
                Click("Connect Species tuna"); Click("Connect Species krill");
            }
        }

        [UnityTest]
        public IEnumerator Edna_FollowsEachComparisonAndNeverBlocksTheTarget()
        {
            yield return Load();
            Assert.That(GameObject.Find("Edna Introduction Portrait").GetComponent<Image>().sprite, Is.Not.Null);
            PointerClick("Observe Answer NotDetected"); yield return null;
            Assert.That(Button("Toggle Notebook Drawer"), Is.Not.Null);
            Assert.That(GameObject.Find("Observe Question Feedback").GetComponent<Text>().text, Does.Contain("Recorded"));
            Record("Species Marker tuna"); Record("Species Marker krill"); Record("Species Marker sea_star"); Record("Species Marker mussel");
            yield return null;
            PointerClick("Continue To Simulate");
            yield return null;
            Assert.That(Speech.text, Does.Contain("any of these three"));
            PointerClick("Threat longline");
            yield return null;
            PointerClick("Run Selected Model");
            yield return null;
            PointerClick("Prediction shark");
            yield return null;
            PointerClick("Observation E01_SHARK_NONDETECTION");
            yield return null;
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.CompletedObjectiveCount, Is.EqualTo(1));
            PointerClick("Prediction tuna");
            yield return null;
            Assert.That(Speech.text, Does.Contain("Tuna"), "EDNA keeps the current comparison visible as the investigation advances.");
            PointerClick("Talk To Edna");
            yield return null;
            PointerClick("Edna Next Step");
            yield return null;
            Assert.That(Speech.text, Does.Contain("WHAT WE FOUND"));
        }

        [UnityTest]
        public IEnumerator Edna_CanLeadEveryRequiredComparisonWithoutASeparateQuestionPanel()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            Click("Threat bottom_trawling");
            for (int step = 0; step < 30 && controller.State.CompletedObjectiveCount < 7; step++)
            {
                yield return null;
                Assert.That(GameObject.Find("Case Questions"), Is.Null);
                Assert.That(GameObject.Find("Report Gate Hint"), Is.Null);
                Assert.That(GameObject.Find("Evidence Action Prompt"), Is.Null);
                Assert.That(Speech, Is.Not.Null);
                Assert.That(GameObject.Find("Edna Name").GetComponent<Text>().text, Does.Contain("EDNA"));
                if (Button("Edna Continue") == null && GameObject.Find("Workbench Evidence Heading") != null)
                {
                    Click("Talk To Edna");
                    yield return null;
                }
                if (Button("Edna Continue") != null)
                {
                    int before = controller.State.CompletedObjectiveCount;
                    PointerClick("Edna Continue");
                    Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(before), "Guidance must not answer the evidence check for the player.");
                }
                else
                {
                    GameObject clue = GameObject.Find("Guided Clue Badge");
                    Assert.That(clue, Is.Not.Null, "EDNA must lead to an actionable evidence choice.");
                    PointerClick(clue.transform.parent.name);
                }
            }
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(7));
            Assert.That(controller.State.TriedThreatIds.Count, Is.EqualTo(3));
            Assert.That(Button("Edna Continue").GetComponentInChildren<Text>().text, Is.EqualTo("Write first idea"));
            Click("Edna Continue");
            Click("Provisional Cause longline");
            Click("Confirm Provisional Idea");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("1/3"));
        }

        [UnityTest]
        public IEnumerator Edna_HintFollowsThePlayersSelectedSpecies()
        {
            yield return Load();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            Click("Threat plastic");
            Click("Edna Continue");
            Click("Prediction sea_star");
            Assert.That(Speech.text, Does.Contain("Sea star"));
            Click("Edna Next Step");
            Assert.That(Speech.text, Does.Contain("Sea star"));
            Assert.That(Speech.text, Does.Not.Contain("mussel"));
        }

        [UnityTest]
        public IEnumerator ReportConversation_AllThreeRoundsSupportEitherClueFirstAndRevisiting()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            foreach (string first in new[] { "Review ROV Follow-up", "Review ROV Seafloor" })
            {
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
                yield return null;
                Assert.That(GameObject.Find("Survey Report Paper"), Is.Null);
                Assert.That(Button("Submit Final Report"), Is.Null);
                PointerClick(first);
                CompleteCameraCapture();
                yield return null;
                Assert.That(controller.State.ConfirmationReviewed, Is.True);
                Assert.That(controller.State.SelectedReportEvidenceIds.Count, Is.EqualTo(7));
                Assert.That(GameObject.Find("ROV Evidence").transform.GetChild(0).name,
                    Is.EqualTo(first == "Review ROV Seafloor" ? "Confirmation E08_SEAFLOOR_INTACT" : "Confirmation E07_FISHING_LINE"));
                PointerClick("Discuss Explanation");
                yield return null;
                Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("2/3"));
                PointerClick("Review Report Comparisons");
                yield return null;
                Assert.That(Button("Hypothesis Card longline"), Is.Not.Null);
                Click("Close Notebook Drawer");
                yield return null;
                Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("2/3"));
                Assert.That(Button("Keep Report Explanation"), Is.Null, "Discuss one key clue before deciding.");
                ConnectCluesToArgument("Report Key Clue Seafloor");
                yield return null;
                PointerClick("Keep Report Explanation");
                yield return null;
                Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("3/3"));
                Assert.That(GameObject.Find("Report Review"), Is.Not.Null);
                PointerClick("Revisit ROV Clues");
                yield return null;
                Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("1/3"));
                Click("Discuss Explanation");
                Click("Keep Report Explanation");
                yield return null;
                PointerClick("Submit Final Report");
                yield return null;
                Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
                Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(7));
            }
        }

        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator PredictionCues_PulseWithoutMovingOrBlockingCardsAndExcludeSavedChecks()
        {
            yield return Load();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            Click("Threat bottom_trawling");
            Click("Run Selected Model");
            yield return null;
            string[] species = { "shark", "tuna", "krill", "sea_star", "mussel" };
            Rect original = Bounds(Button("Prediction sea_star").GetComponent<RectTransform>());
            float minimum = 1f, maximum = 0f;
            for (int frame = 0; frame < 12; frame++)
            {
                foreach (string id in species)
                {
                    Transform cue = Button("Prediction " + id).transform.Find("Choose Prediction Cue");
                    Assert.That(cue, Is.Not.Null, id);
                    Assert.That(cue.GetComponent<InvestigationBorderGraphic>().raycastTarget, Is.False);
                    CanvasGroup pulse = cue.GetComponent<CanvasGroup>();
                    Assert.That(pulse.blocksRaycasts, Is.False);
                    minimum = Mathf.Min(minimum, pulse.alpha);
                    maximum = Mathf.Max(maximum, pulse.alpha);
                }
                Rect current = Bounds(Button("Prediction sea_star").GetComponent<RectTransform>());
                Assert.That(current, Is.EqualTo(original));
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.That(maximum - minimum, Is.GreaterThan(.4f));
            Click("Dismiss Edna");
            yield return null;
            Assert.That(GameObject.Find("Choose Prediction Cue"), Is.Not.Null, "Card affordances survive dismissing the dialogue.");
            PointerClick("Prediction sea_star");
            yield return null;
            Assert.That(GameObject.Find("Choose Prediction Cue"), Is.Null, "Focus moves to choosing evidence.");
            Click("Observation E04_BENTHIC_STABLE");
            Click("Threat bottom_trawling");
            yield return null;
            foreach (string id in species)
                Assert.That(Button("Prediction " + id).transform.Find("Choose Prediction Cue") != null,
                    Is.EqualTo(id != "sea_star"), "Completed checks should not invite another check: " + id);
        }

        [UnityTest]
        public IEnumerator NotebookIntroduction_AppearsAfterFirstCheckAndTeachesReviewWithoutReplaying()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            Assert.That(GameObject.Find("Open Notebook Cue"), Is.Null);
            Click("Threat longline");
            Click("Run Selected Model");
            Click("Prediction shark");
            Click("Observation E01_SHARK_NONDETECTION");
            yield return null;
            Assert.That(Speech.text, Does.Contain("Open your notebook"));
            Assert.That(GameObject.Find("Open Notebook Cue"), Is.Not.Null);
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Null, "Opening is optional.");
            // The comparison feedback can push the original icon below the fold.
            // EDNA provides a direct action in the always-visible conversation.
            Rect openButton = Bounds(Button("Edna Open Notebook").GetComponent<RectTransform>());
            Rect conversation = Bounds(GameObject.Find("Edna Conversation").GetComponent<RectTransform>());
            Assert.That(openButton.yMin, Is.GreaterThanOrEqualTo(conversation.yMin));
            Assert.That(openButton.yMax, Is.LessThanOrEqualTo(conversation.yMax));
            PointerClick("Edna Open Notebook");
            yield return null;
            Assert.That(Speech, Is.Null, "The floating dialogue must not cover the notebook.");
            Text instructions = GameObject.Find("Notebook Edna Instructions").GetComponent<Text>();
            Assert.That(instructions.text, Does.Contain("Scroll below"));
            Assert.That(instructions.text, Does.Contain("Compare causes"));
            Assert.That(GameObject.Find("Open Notebook Cue"), Is.Null);
            Canvas canvas = Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<Canvas>();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            bool originalEnabled = scaler.enabled;
            float originalScale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false;
                foreach (float width in new[] { 1280f, 720f })
                {
                    canvas.scaleFactor = Screen.width / width;
                    yield return null;
                    yield return null;
                    instructions = GameObject.Find("Notebook Edna Instructions").GetComponent<Text>();
                    Assert.That(instructions.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(instructions.preferredHeight));
                    Rect avatar = Bounds(GameObject.Find("Notebook Edna Avatar").GetComponent<RectTransform>());
                    Assert.That(avatar.xMin, Is.GreaterThan(Bounds(instructions.rectTransform).xMax));
                }
            }
            finally
            {
                canvas.scaleFactor = originalScale;
                scaler.enabled = originalEnabled;
            }
            Click("Toggle Hypothesis Summary");
            yield return null;
            Assert.That(GameObject.Find("Notebook Edna Instructions").GetComponent<Text>().text, Does.Contain("reopen it in the model"));
            Click("Hypothesis Card longline");
            Click("Revisit Comparison longline shark");
            yield return null;
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Null);
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(1));
            Assert.That(Button("Prediction shark").transform.Find("Comparison Locked"), Is.Not.Null);
            Click("Toggle Notebook Drawer");
            yield return null;
            Assert.That(GameObject.Find("Notebook Edna Introduction"), Is.Null, "No repeated tutorial on reopening.");
            Click("Close Notebook Drawer");
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            Click("Toggle Notebook Drawer");
            yield return null;
            Assert.That(GameObject.Find("Notebook Edna Introduction"), Is.Not.Null, "A new case resets onboarding and supports opening before any checks.");
        }

        [UnityTest]
        public IEnumerator Edna_UsesCompleteAvatarBelowSettingsAndAnIntegratedPortraitOnTheRight()
        {
            yield return Load();
            Rect dock = Bounds(Button("Talk To Edna").GetComponent<RectTransform>());
            Rect stages = Bounds(GameObject.Find("Stage Navigation").GetComponent<RectTransform>());
            Assert.That(dock.xMin, Is.GreaterThan(stages.xMax));
            Rect settings = Bounds(Button("Difficulty Toggle").GetComponent<RectTransform>());
            Assert.That(dock.yMax, Is.LessThan(settings.yMin));
            Assert.That(dock.xMax, Is.EqualTo(settings.xMax).Within(.5f));
            Rect subtitle = Bounds(GameObject.Find("Case Subtitle").GetComponent<RectTransform>());
            Assert.That(dock.yMax, Is.LessThanOrEqualTo(subtitle.yMin + .5f));
            Image avatar = GameObject.Find("Edna Avatar").GetComponent<Image>();
            Assert.That(avatar.sprite, Is.SameAs(Resources.Load<Sprite>("Investigation/Edna/edna-avatar")));
            Assert.That(avatar.sprite.rect.width, Is.EqualTo(avatar.sprite.rect.height));
            Assert.That(GameObject.Find("Edna Intro Locator").GetComponent<Text>().text, Does.Contain("top right"));
            Image introduction = GameObject.Find("Edna Introduction Portrait").GetComponent<Image>();
            Assert.That(introduction.transform.parent.name, Is.EqualTo("Observe Edna Header"));
            Assert.That(Bounds(introduction.rectTransform).center.x, Is.GreaterThan(Bounds(GameObject.Find("Observe Question").GetComponent<RectTransform>()).center.x));
            Assert.That(Button("Edna Next Step"), Is.Null);
            Assert.That(Button("Edna Why"), Is.Null);
            PointerClick("Talk To Edna");
            yield return null;
            Assert.That(GameObject.Find("Observe Question Feedback").GetComponent<Text>().text, Does.Contain("slider"));
            Assert.That(Button("Observe Answer NotDetected"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Edna_SimulateDialogueSitsBelowThePlayableViewport()
        {
            yield return Load();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Rect dialogue = Bounds(GameObject.Find("Edna Conversation").GetComponent<RectTransform>());
            Rect viewport = Bounds(GameObject.Find("Investigation Content").GetComponent<ScrollRect>().viewport);
            Assert.That(dialogue.yMax, Is.LessThanOrEqualTo(viewport.yMin));
            Assert.That(Button("Edna Next Step"), Is.Null);
            Assert.That(Button("Edna Why"), Is.Null);
            PointerClick("Threat longline");
            yield return null;
            PointerClick("Run Selected Model");
            yield return null;
            Image portrait = GameObject.Find("Edna Portrait").GetComponent<Image>();
            Assert.That(portrait.transform.parent.name, Is.EqualTo("Edna Speech"));
            Assert.That(Bounds(portrait.rectTransform).xMin, Is.GreaterThan(Bounds(Speech.rectTransform).xMax));
            Assert.That(portrait.raycastTarget, Is.False);
            foreach (string id in new[] { "sea_star", "mussel" })
            {
                Rect target = Bounds(Button("Prediction " + id).GetComponent<RectTransform>());
                Assert.That(target.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1f), id + " must remain fully visible above EDNA.");
                Assert.That(target.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1f));
            }
        }

        [UnityTest]
        public IEnumerator Edna_TogglingHelpKeepsSimulatorCardsAndScrollStable()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            EnterSimulate("Stage Simulate");
            Click("Threat plastic");
            Click("Run Selected Model");
            Click("Prediction tuna");
            Click("Dismiss Edna");
            Canvas canvas = view.GetComponent<Canvas>();
            CanvasScaler scaler = view.GetComponent<CanvasScaler>();
            bool beforeEnabled = scaler.enabled;
            float beforeScale = canvas.scaleFactor;
            string[] names = { "Threat plastic", "Threat longline", "Threat bottom_trawling", "Model Workspace",
                "Food Web Prediction", "Reference Indicators", "Prediction tuna", "Prediction sea_star" };
            try
            {
                scaler.enabled = false;
                foreach (float width in new[] { 1280f, 1000f, 720f })
                {
                    canvas.scaleFactor = Screen.width / width;
                    yield return null;
                    yield return null;
                    ScrollRect page = GameObject.Find("Investigation Content").GetComponent<ScrollRect>();
                    page.StopMovement();
                    page.verticalNormalizedPosition = page.content.rect.height > page.viewport.rect.height + 1f ? .45f : 1f;
                    yield return null;
                    var original = new System.Collections.Generic.Dictionary<string, Rect>();
                    foreach (string name in names) original[name] = Bounds(GameObject.Find(name).GetComponent<RectTransform>());
                    foreach (string action in new[] { "Talk To Edna", "Edna Why", "Edna Next Step", "Dismiss Edna" })
                    {
                        if (Button(action) == null) continue;
                        Click(action);
                        yield return null;
                        yield return null;
                        foreach (string name in names)
                        {
                            Rect current = Bounds(GameObject.Find(name).GetComponent<RectTransform>());
                            Assert.That(current.width, Is.EqualTo(original[name].width).Within(.8f), name + " width after " + action);
                            Assert.That(current.height, Is.EqualTo(original[name].height).Within(.8f), name + " height after " + action);
                            Assert.That(current.x, Is.EqualTo(original[name].x).Within(.8f), name + " x after " + action);
                            Assert.That(current.y, Is.EqualTo(original[name].y).Within(.8f), name + " y after " + action);
                        }
                    }
                    Assert.That(controller.State.ActiveThreatId, Is.EqualTo("plastic"));
                    Assert.That(controller.State.CompletedObjectiveCount, Is.Zero);
                }
            }
            finally
            {
                canvas.scaleFactor = beforeScale;
                scaler.enabled = beforeEnabled;
                InvestigationSessionBridge.Clear();
            }
        }

        private static string[] EvidenceOrder()
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (Transform child in GameObject.Find("Observation Selection").transform)
                if (child.GetComponent<Button>() != null && child.name.StartsWith("Observation ", System.StringComparison.Ordinal)) names.Add(child.name);
            return names.ToArray();
        }

        [UnityTest]
        public IEnumerator EvidenceChoices_AreShuffledButStayStableDuringTheInvestigation()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            // Use a repeatable random stream so this regression test cannot fail by chance.
            typeof(InvestigationRuntimeView).GetField("observationChoiceRandom", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(view, new System.Random(513));
            EnterSimulate("Stage Simulate");
            string[] species = { "shark", "tuna", "krill", "sea_star", "mussel" };
            string[] findings = { "E01_SHARK_NONDETECTION", "E02_TUNA_WIDER_DETECTION", "E03_KRILL_NONDETECTION", "E04_BENTHIC_STABLE", "E06_PLASTIC_INDICATOR_STABLE" };
            var positions = new System.Collections.Generic.HashSet<int>();
            var recordedOrders = new System.Collections.Generic.Dictionary<string, string[]>();
            foreach (string threat in new[] { "plastic", "longline", "bottom_trawling" })
            {
                Click("Threat " + threat);
                Click("Run Selected Model");
                for (int index = 0; index < species.Length; index++)
                {
                    Click("Prediction " + species[index]);
                    yield return null;
                    string[] order = EvidenceOrder();
                    Assert.That(order, Has.Length.EqualTo(3));
                    int directPosition = System.Array.IndexOf(order, "Observation " + findings[index]);
                    Assert.That(directPosition, Is.GreaterThanOrEqualTo(0), "The shuffle must not remove the relevant evidence.");
                    positions.Add(directPosition);
                    recordedOrders[threat + "/" + species[index]] = order;
                    Click("Talk To Edna");
                    yield return null;
                    CollectionAssert.AreEqual(order, EvidenceOrder(), "Opening help must not move the options.");
                    Click("Dismiss Edna");
                    yield return null;
                    CollectionAssert.AreEqual(order, EvidenceOrder(), "Closing help must not move the options.");
                }
                Click("Prediction shark");
                yield return null;
                CollectionAssert.AreEqual(recordedOrders[threat + "/shark"], EvidenceOrder(), "Revisiting a comparison keeps its option order.");
            }
            Assert.That(positions.Count, Is.GreaterThan(1), "The relevant answer must not occupy the same position in every question.");
            Click("Threat plastic");
            Click("Prediction mussel");
            string[] before = EvidenceOrder();
            Click("Observation E06_PLASTIC_INDICATOR_STABLE");
            yield return null;
            CollectionAssert.AreEqual(before, EvidenceOrder());
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Edna_ShortRepliesAndAvailableQuestionsShareOneRow()
        {
            yield return Load();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            yield return null;
            Assert.That(Button("Edna Next Step"), Is.Null);
            Assert.That(Button("Edna Why"), Is.Null);
            Click("Talk To Edna");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(Button("Edna Next Step"), Is.Null, "The completion message already explains the next action.");
            Rect message = Bounds(Speech.rectTransform);
            Rect question = Bounds(Button("Edna Why").GetComponent<RectTransform>());
            Assert.That(message.xMax, Is.LessThan(question.xMin));
            Assert.That(message.center.y, Is.EqualTo(question.center.y).Within(2f));
            Assert.That(Speech.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(Speech.preferredHeight));
        }

        [UnityTest]
        public IEnumerator Edna_HidesForNotebookAndRestoresWithoutLosingProgress()
        {
            yield return Load();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            yield return null;
            PointerClick("Toggle Notebook Drawer");
            yield return null;
            Assert.That(Speech, Is.Null);
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Not.Null);
            Click("Close Notebook Drawer");
            yield return null;
            Assert.That(Button("Talk To Edna"), Is.Not.Null);
            Assert.That(controller.State.DiscoveredObservationIds.Count, Is.EqualTo(5));
            Assert.That(controller.State.CompletedObjectiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Edna_KeyboardHelpAndReducedMotionDoNotChangeTheInvestigation()
        {
            bool before = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotionForTests(true);
            try
            {
                yield return Load();
                InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
                yield return null;
                Click("Dismiss Edna");
                yield return null;
                Button avatar = Button("Talk To Edna");
                EventSystem.current.SetSelectedGameObject(avatar.gameObject);
                ExecuteEvents.Execute(avatar.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                yield return null;
                Click("Edna Why");
                yield return null;
                Assert.That(Speech.text, Does.Contain("Different causes"));
                CanvasGroup group = GameObject.Find("Edna Conversation").GetComponent<CanvasGroup>();
                Assert.That(group == null || group.alpha == 1f, Is.True);
                Assert.That(controller.State.TriedThreatIds, Is.Empty);
                Assert.That(controller.State.CompletedObjectiveCount, Is.Zero);
                Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
            }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(before); }
        }

        [UnityTest]
        public IEnumerator Edna_QuestionsFitNarrowScreensAndDoNotReplayOnResize()
        {
            yield return Load();
            InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            Canvas canvas = view.GetComponent<Canvas>();
            CanvasScaler scaler = view.GetComponent<CanvasScaler>();
            float beforeScale = canvas.scaleFactor;
            bool beforeEnabled = scaler.enabled;
            try
            {
                scaler.enabled = false;
                canvas.scaleFactor = Screen.width / 720f;
                foreach (InvestigationQaCheckpoint checkpoint in new[] { InvestigationQaCheckpoint.Start, InvestigationQaCheckpoint.SimulateStart, InvestigationQaCheckpoint.ReportReady, InvestigationQaCheckpoint.FinalReportReady })
                {
                    Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(checkpoint);
                    yield return null;
                    yield return null;
                    if (checkpoint == InvestigationQaCheckpoint.ReportReady || checkpoint == InvestigationQaCheckpoint.FinalReportReady)
                    {
                        foreach (Text text in GameObject.Find("Report Edna Conversation").GetComponentsInChildren<Text>())
                            Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name + ": " + text.text);
                        Assert.That(GameObject.Find("Edna Speech"), Is.Null);
                        continue;
                    }
                    if (checkpoint == InvestigationQaCheckpoint.Start)
                    {
                        Click("Talk To Edna"); yield return null;
                        foreach (Text text in GameObject.Find("Observe Question").GetComponentsInChildren<Text>())
                            Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name);
                        string question = GameObject.Find("Observe Question Text").GetComponent<Text>().text;
                        canvas.scaleFactor *= .99f; yield return null; yield return null;
                        Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Is.EqualTo(question));
                        continue;
                    }
                    Click("Talk To Edna");
                    yield return null;
                    foreach (string question in new[] { "Edna Next Step", "Edna Why" })
                    {
                        if (Button(question) == null) continue;
                        Click(question);
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        foreach (Text text in GameObject.Find("Edna Speech").GetComponentsInChildren<Text>())
                            Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), text.name + ": " + text.text);
                    }
                    Click("Dismiss Edna");
                    yield return null;
                    canvas.scaleFactor *= .99f;
                    yield return null;
                    yield return null;
                    Assert.That(Speech, Is.Null);
                }
            }
            finally
            {
                canvas.scaleFactor = beforeScale;
                scaler.enabled = beforeEnabled;
                InvestigationSessionBridge.Clear();
            }
        }
    }
}
