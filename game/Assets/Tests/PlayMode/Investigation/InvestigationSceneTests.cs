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
        private static void AssertChoiceArrows(params string[] ids)
        {
            foreach (string id in new[] { "plastic", "longline", "bottom_trawling" })
                Assert.That(FindButton("Threat " + id).transform.Find("Choose Cause Arrow") != null,
                    Is.EqualTo(Array.IndexOf(ids, id) >= 0), id);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_AllUnrecordedSpeciesHaveSynchronizedPulsingFrames()
        {
            bool previous = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotion(false);
            try
            {
                yield return LoadInvestigationScene();
                string[] ids = { "shark", "tuna", "krill", "sea_star", "mussel" };
                CanvasGroup first = FindButton("Species Marker shark").transform.Find("Unrecorded Finding Cue").GetComponent<CanvasGroup>();
                float alpha = first.alpha;
                yield return WaitForCondition(() => Mathf.Abs(first.alpha - alpha) > .25f, 2f, "Species frames should visibly pulse.");
                foreach (string id in ids)
                {
                    CanvasGroup cue = FindButton("Species Marker " + id).transform.Find("Unrecorded Finding Cue").GetComponent<CanvasGroup>();
                    Assert.That(cue.alpha, Is.EqualTo(first.alpha).Within(.001f));
                    Assert.That(cue.blocksRaycasts, Is.False);
                    Assert.That(FindButton("Historical Species Marker " + id).transform.Find("Unrecorded Finding Cue"), Is.Null);
                }
                ClickThroughPointer(FindButton("Species Marker mussel"));
                yield return null;
                Assert.That(FindButton("Species Marker mussel").transform.Find("Unrecorded Finding Cue"), Is.Null);
                Assert.That(FindButton("Species Marker shark").transform.Find("Unrecorded Finding Cue"), Is.Not.Null);
                Click("Motion Toggle");
                yield return null;
                yield return null;
                foreach (InvestigationGuidancePulse pulse in Object.FindObjectsByType<InvestigationGuidancePulse>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    Assert.That(pulse.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(.001f));
            }
            finally { InvestigationMotionSettings.SetReducedMotion(previous); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_EvidenceGuidePersistsUntilAComparisonIsSettled()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.EvidenceReady);
            yield return null;
            Assert.That(GameObject.Find("Evidence Action Prompt"), Is.Not.Null);
            Assert.That(GameObject.Find("Choose Evidence Arrow"), Is.Not.Null);
            foreach (Text text in GameObject.Find("Evidence Action Prompt").GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
            int before = controller.State.CompletedObjectiveCount;
            ClickThroughPointer(FindButton("Observation E03_KRILL_NONDETECTION"));
            yield return null;
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(before));
            Assert.That(GameObject.Find("Evidence Choice Cue"), Is.Not.Null);
            ClickThroughPointer(FindButton("Observation E06_PLASTIC_INDICATOR_STABLE"));
            yield return null;
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(before + 1));
            Assert.That(GameObject.Find("Evidence Action Prompt"), Is.Null);
            Assert.That(GameObject.Find("Choose Evidence Arrow"), Is.Null);
            Assert.That(GameObject.Find("Judgement Row"), Is.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_QaCheckpointsCoverCurrentProgressiveFlowAndPublishCompletion()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            foreach (InvestigationQaCheckpoint checkpoint in Enum.GetValues(typeof(InvestigationQaCheckpoint)))
            {
                controller.ApplyQaCheckpoint(checkpoint);
                yield return null;
                Assert.That(GameObject.Find("Fatal Error"), Is.Null, checkpoint.ToString());
                Assert.That(controller.State, Is.Not.Null);
            }
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start);
            Assert.That(GameObject.Find("Observe First Finding"), Is.Not.Null);
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FirstFinding);
            Assert.That(GameObject.Find("Notebook Title").GetComponent<Text>().text, Does.Contain("· 1"));
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            AssertChoiceArrows("plastic", "longline", "bottom_trawling");
            Assert.That(controller.State.TriedThreatIds, Is.Empty);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.EvidenceReady);
            Assert.That(GameObject.Find("Evidence Action Prompt"), Is.Not.Null);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportQuestions);
            AssertReportQuestion("Cause", 1);
            Assert.That(controller.State.SelectedReportEvidenceIds, Is.Empty);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            Assert.That(GameObject.Find("Report Review"), Is.Not.Null);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.CaseClosed);
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(GameObject.Find("Case Closed Summary"), Is.Not.Null);
            Assert.That(InvestigationSessionBridge.LastResult.completed, Is.True);
            Assert.That(InvestigationSessionBridge.LastResult.correct, Is.True);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.Start);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
            Click("Difficulty Toggle");
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.EvidenceReady);
            Assert.That(controller.State.Difficulty, Is.EqualTo(InvestigationDifficulty.Hard));
            Assert.That(GameObject.Find("Choose Evidence Arrow"), Is.Null);
            InvestigationSessionBridge.Clear();
        }

        [UnityTest]
        public IEnumerator InvestigationScene_FirstFindingRevealsNotebookAndImportsSkipThePrompt()
        {
            InvestigationSessionBridge.Clear();
            try
            {
                yield return LoadInvestigationScene();
                Rect reserved = WorldRect(GameObject.Find("Observe First Finding").GetComponent<RectTransform>());
                Assert.That(GameObject.Find("Investigation Notebook"), Is.Null);
                ClickThroughPointer(FindButton("Historical Species Marker shark"));
                yield return null;
                Assert.That(GameObject.Find("Observe First Finding"), Is.Not.Null);
                Assert.That(GameObject.Find("Investigation Notebook"), Is.Null,
                    "Reading the historical baseline must not invent a recorded finding.");
                ClickThroughPointer(FindButton("Species Marker shark"));
                yield return null;
                Click("Increase Edna Hint"); // A refresh during the reveal must not leave the paper transparent.
                yield return null;
                GameObject notebook = GameObject.Find("Investigation Notebook");
                Assert.That(notebook, Is.Not.Null);
                Assert.That(GameObject.Find("Observe First Finding"), Is.Null);
                Assert.That(GameObject.Find("Notebook E01_SHARK_NONDETECTION"), Is.Not.Null);
                Assert.That(GameObject.Find("Notebook Title").GetComponent<Text>().text, Does.Contain("· 1"));
                CanvasGroup group = notebook.GetComponent<CanvasGroup>();
                Assert.That(group == null || group.alpha >= .99f, Is.True);
                Rect shown = WorldRect(notebook.GetComponent<RectTransform>());
                Assert.That(shown.xMin, Is.EqualTo(reserved.xMin).Within(1f));
                Assert.That(shown.width, Is.EqualTo(reserved.width).Within(1f));
                Assert.That(notebook.GetComponent<Image>().color, Is.EqualTo((Color)InvestigationTheme.Paper));

                yield return LoadInvestigationScene();
                Assert.That(GameObject.Find("Observe First Finding"), Is.Not.Null);
                Assert.That(GameObject.Find("Investigation Notebook"), Is.Null);
                InvestigationGameInput input = new InvestigationGameInput();
                input.discoveredObservationIds.Add("E04_BENTHIC_STABLE");
                InvestigationSessionBridge.SetInput(input);
                yield return LoadInvestigationScene();
                Assert.That(GameObject.Find("Observe First Finding"), Is.Null);
                Assert.That(GameObject.Find("Notebook E04_BENTHIC_STABLE"), Is.Not.Null);
            }
            finally { InvestigationSessionBridge.Clear(); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ActionArrowMovesFromCauseToRunAndRespectsMotionPreference()
        {
            bool previousMotion = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotion(false);
            try
            {
                yield return LoadInvestigationScene();
                Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
                Click("Stage Simulate");
                yield return null;
                InvestigationGuideArrowGraphic arrow = GameObject.Find("Choose Cause Arrow").GetComponent<InvestigationGuideArrowGraphic>();
                AssertChoiceArrows("plastic", "longline", "bottom_trawling");
                Assert.That(arrow.raycastTarget, Is.False);
                AssertInsideViewport(arrow.rectTransform, GameObject.Find("Investigation Content").GetComponent<ScrollRect>().viewport);
                float initialAlpha = arrow.GetComponent<CanvasGroup>().alpha;
                yield return WaitForCondition(() => Mathf.Abs(arrow.GetComponent<CanvasGroup>().alpha - initialAlpha) > .12f,
                    2f, "The current-action arrow should gently pulse in Full Motion.");
                ClickThroughPointer(FindButton("Motion Toggle"));
                yield return null;
                yield return null;
                arrow = GameObject.Find("Choose Cause Arrow").GetComponent<InvestigationGuideArrowGraphic>();
                Assert.That(arrow.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(.001f));
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(arrow.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(.001f));
                ClickThroughPointer(FindButton("Threat longline")); // A different cause remains selectable.
                yield return null;
                Assert.That(GameObject.Find("Choose Cause Arrow"), Is.Null);
                arrow = GameObject.Find("Run Model Arrow").GetComponent<InvestigationGuideArrowGraphic>();
                Assert.That(arrow.transform.parent, Is.SameAs(FindButton("Run Selected Model").transform));
                AssertInsideViewport(arrow.rectTransform, GameObject.Find("Investigation Content").GetComponent<ScrollRect>().viewport);
                Click("Motion Toggle");
                yield return null;
                arrow = GameObject.Find("Run Model Arrow").GetComponent<InvestigationGuideArrowGraphic>();
                float runAlpha = arrow.GetComponent<CanvasGroup>().alpha;
                yield return WaitForCondition(() => Mathf.Abs(arrow.GetComponent<CanvasGroup>().alpha - runAlpha) > .25f,
                    2f, "Run simulation should also have a visibly pulsing arrow.");
                ClickThroughPointer(FindButton("Run Selected Model"));
                yield return null;
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Null);
                Assert.That(GameObject.Find("Choose Cause Arrow"), Is.Null);
                Click("Threat plastic");
                yield return null;
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Not.Null);
                Click("Toggle Edna Guide");
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Null);
                Click("Toggle Edna Guide");
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Not.Null);
                Click("Difficulty Toggle");
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Null);
            }
            finally { InvestigationMotionSettings.SetReducedMotion(previousMotion); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ModelCheckProgressIsSeparateFromEvidenceRelationship()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            Click("Stage Simulate");
            AssertThreatProgress("plastic", "NOT RUN", string.Empty, false);
            Click("Threat plastic");
            Click("Run Selected Model");
            AssertThreatProgress("plastic", "TO CHECK · 0/1", string.Empty, false);
            Compare("mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch);
            AssertThreatProgress("plastic", "CHECKED · 1/1", "CHALLENGES", true);
            AssertChoiceArrows("longline", "bottom_trawling");
            Click("Threat longline");
            Click("Run Selected Model");
            AssertThreatProgress("longline", "TO CHECK · 0/4", string.Empty, false);
            Compare("shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match);
            Compare("tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match);
            Compare("krill", "E03_KRILL_NONDETECTION", ComparisonJudgement.Match);
            AssertThreatProgress("longline", "ROV NEXT · 3/4", "SUPPORTS", false);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            Click("Stage Simulate");
            AssertThreatProgress("longline", "CHECKED · 4/4", "SUPPORTS", true);
            AssertThreatProgress("bottom_trawling", "CHECKED · 2/2", "MIXED EVIDENCE", true);
        }

        private static void AssertThreatProgress(string id, string progress, string relationship, bool complete)
        {
            Transform card = FindButton("Threat " + id).transform;
            Assert.That(card.Find("Threat Status").GetComponent<Text>().text, Is.EqualTo(progress));
            Assert.That(card.Find("Threat Evidence Relationship").GetComponent<Text>().text, Is.EqualTo(relationship));
            Assert.That(card.Find("Threat Checks Complete") != null, Is.EqualTo(complete));
            foreach (Text text in card.GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_RovActionHintsUseTheVisibleButtonNames()
        {
            yield return LoadInvestigationScene();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
            Click("Increase Edna Hint");
            Click("Increase Edna Hint");
            string action = FindButton("Review ROV Follow-up").GetComponentInChildren<Text>().text;
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain(action));
            Click("Review ROV Follow-up");
            Click("Increase Edna Hint");
            Click("Increase Edna Hint");
            action = FindButton("Re-test Fishing Models").GetComponentInChildren<Text>().text.TrimEnd(' ', '→');
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain(action));
        }

        [Test]
        public void SeamountCycle_FollowsTheApprovedTwentySecondSequence()
        {
            double[] seconds = { 0, 1, 3, 5, 5.5, 7.5, 9.5, 10.5, 12.5, 14.5, 15, 17, 19, 20, 23 };
            Vector2[] expected =
            {
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(.5f, 0),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, .5f),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, .5f),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(.5f, 0),
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(.5f, 0)
            };
            for (int index = 0; index < seconds.Length; index++)
            {
                Assert.That(Vector2.Distance(InvestigationSeamountBackdrop.EvaluateBlends(seconds[index], false), expected[index]),
                    Is.LessThan(.0001f), $"Look at {seconds[index]} seconds");
                Assert.That(InvestigationSeamountBackdrop.EvaluateBlends(seconds[index], true), Is.EqualTo(Vector2.zero));
            }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_SeamountLoopStaysSyncedAcrossRefreshAndCleansUp()
        {
            bool previousMotion = InvestigationMotionSettings.ReducedMotion;
            float previousTimeScale = Time.timeScale;
            InvestigationSessionBridge.Clear();
            InvestigationMotionSettings.SetReducedMotion(false);
            try
            {
                yield return LoadInvestigationScene();
                InvestigationSeamountBackdrop owner = Object.FindAnyObjectByType<InvestigationSeamountBackdrop>();
                Assert.That(owner.RenderMaterial, Is.Not.Null);
                Material shared = owner.RenderMaterial;
                AssertSharedSeamountMaterial(shared);
                yield return new WaitForSecondsRealtime(1.3f);
                double beforeRefresh = owner.ElapsedSeconds;
                ClickThroughPointer(FindButton("Species Marker shark"));
                yield return null;
                Assert.That(Object.FindAnyObjectByType<InvestigationSeamountBackdrop>(), Is.SameAs(owner));
                Assert.That(owner.ElapsedSeconds, Is.GreaterThanOrEqualTo(beforeRefresh));
                AssertSharedSeamountMaterial(shared);
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Has.Count.EqualTo(1));

                double beforePause = owner.ElapsedSeconds;
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(owner.ElapsedSeconds - beforePause, Is.GreaterThan(.15d));
                Time.timeScale = previousTimeScale;

                ClickThroughPointer(FindButton("Motion Toggle"));
                yield return null;
                yield return null;
                Assert.That(owner.BlendWeights, Is.EqualTo(Vector2.zero));
                AssertSharedSeamountMaterial(shared);
                ClickThroughPointer(FindButton("Motion Toggle"));
                yield return null;
                yield return null;
                Assert.That(owner.ElapsedSeconds, Is.GreaterThan(beforePause));
                Assert.That(Vector2.Distance(owner.BlendWeights,
                    InvestigationSeamountBackdrop.EvaluateBlends(owner.ElapsedSeconds, false)), Is.LessThan(.05f));

                yield return LoadInvestigationScene();
                Assert.That(owner == null, Is.True);
                Assert.That(shared == null, Is.True, "The old view must release its runtime material on scene reload.");
                AssertSharedSeamountMaterial(Object.FindAnyObjectByType<InvestigationSeamountBackdrop>().RenderMaterial);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                InvestigationMotionSettings.SetReducedMotion(previousMotion);
                InvestigationSessionBridge.Clear();
            }
        }

        private static void AssertSharedSeamountMaterial(Material expected)
        {
            foreach (string map in new[] { "Historical Seamount", "Current Seamount" })
            {
                RawImage background = GameObject.Find(map).GetComponentInChildren<RawImage>();
                Assert.That(background, Is.Not.Null);
                Assert.That(background.material, Is.SameAs(expected));
                Assert.That(background.materialForRendering, Is.SameAs(expected));
                Assert.That(background.raycastTarget, Is.False);
            }
        }

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
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State, Is.Null);
            }
            finally
            {
                InvestigationSessionBridge.Clear();
            }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ImportedSpeciesWithoutArtworkUsesItsNameInsteadOfAWrongGlyph()
        {
            InvestigationGameInput input = new InvestigationGameInput { caseId = "investigation_longline_01" };
            EDNAResultData result = new EDNAResultData();
            result.detectedSpeciesIds.Add("moon_jellyfish");
            input.ednaResults.Add(result);
            try
            {
                InvestigationSessionBridge.SetInput(input);
                yield return LoadInvestigationScene();

                Button marker = FindButton("Species Marker moon_jellyfish");
                Assert.That(marker, Is.Not.Null);
                Assert.That(FindButton("Historical Species Marker moon_jellyfish"), Is.Null);
                Text fallback = marker.transform.Find("Species Artwork").GetComponent<Text>();
                Assert.That(fallback, Is.Not.Null);
                Assert.That(fallback.text, Is.EqualTo("Moon Jellyfish"));
                Assert.That(marker.transform.Find("Species Artwork").GetComponent<InvestigationGlyphGraphic>(), Is.Null);
            }
            finally
            {
                InvestigationSessionBridge.Clear();
            }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_DetailedUpstreamObservationControlsEraDepthAndGhostState()
        {
            InvestigationGameInput input = new InvestigationGameInput { caseId = "investigation_longline_01" };
            EDNAResultData result = new EDNAResultData { sampleId = "deep-sample", siteId = "ridge" };
            result.speciesObservations.Add(new EDNASpeciesObservationData
            {
                speciesId = "moon_jellyfish",
                depthBand = DepthBand.Deep,
                surveyTimepoint = SurveyTimepoint.Current,
                detectionState = SpeciesDetectionState.NotDetected,
                confidence = SurveyConfidence.High,
                sampleQuality = SampleQuality.High,
                source = "eDNA"
            });
            input.ednaResults.Add(result);
            try
            {
                InvestigationSessionBridge.SetInput(input);
                yield return LoadInvestigationScene();

                Button marker = FindButton("Species Marker moon_jellyfish");
                Assert.That(marker, Is.Not.Null);
                Assert.That(FindButton("Historical Species Marker moon_jellyfish"), Is.Null);
                Assert.That(marker.transform.Find("Missing Signal"), Is.Not.Null);
                Assert.That(marker.GetComponent<RectTransform>().anchorMin.y, Is.InRange(0.31f, 0.36f));
                PointerEventData tap = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                ExecuteEvents.Execute(marker.gameObject, tap, ExecuteEvents.pointerClickHandler);
                yield return null;
                Text details = GameObject.Find("Species Facts Tooltip").transform.Find("Tooltip Details").GetComponent<Text>();
                Assert.That(details.text, Does.Contain("Not detected"));
                Assert.That(details.text, Does.Contain("DEPTH  Deep"));
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
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.StartWith("FINDINGS"));
            AssertTextFitsItsRect(FindGameObject("Brand").GetComponent<Text>());
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
            RectTransform firstFinding = FindGameObject("Observe First Finding").GetComponent<RectTransform>();
            Assert.That(historicalMap.position.x, Is.LessThan(currentMap.position.x));
            Assert.That(Mathf.Abs(historicalMap.rect.width - currentMap.rect.width), Is.LessThan(2f));
            Assert.That(Mathf.Abs(currentMap.rect.height - firstFinding.rect.height), Is.LessThan(2f));
            Assert.That(GameObject.Find("Investigation Notebook"), Is.Null);
            Assert.That(GameObject.Find("Notebook Title"), Is.Null);
            foreach (Text text in firstFinding.GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
            RectTransform historicalPlot = historicalMap.Find("Seamount Plot Area").GetComponent<RectTransform>();
            RectTransform currentPlot = currentMap.Find("Seamount Plot Area").GetComponent<RectTransform>();
            RectTransform historicalVisualClip = historicalPlot.Find("Seamount Visual Clip").GetComponent<RectTransform>();
            RectTransform currentVisualClip = currentPlot.Find("Seamount Visual Clip").GetComponent<RectTransform>();
            RawImage historicalMountain = historicalVisualClip.Find("Seamount Backdrop").GetComponent<RawImage>();
            RawImage currentMountain = currentVisualClip.Find("Seamount Backdrop").GetComponent<RawImage>();
            InvestigationSeamountBackdrop backdrop = view.GetComponent<InvestigationSeamountBackdrop>();
            Assert.That(backdrop.NaturalTexture, Is.Not.Null);
            Assert.That(historicalMountain.texture, Is.SameAs(backdrop.NaturalTexture));
            Assert.That(currentMountain.texture, Is.SameAs(backdrop.NaturalTexture));
            Assert.That(historicalMountain.material, Is.SameAs(currentMountain.material));
            Assert.That(currentMountain.material.shader.isSupported, Is.True);
            Assert.That(historicalMountain.raycastTarget || currentMountain.raycastTarget, Is.False);
            Assert.That(historicalPlot.anchorMin, Is.EqualTo(currentPlot.anchorMin));
            Assert.That(historicalPlot.anchorMax, Is.EqualTo(currentPlot.anchorMax));
            Assert.That(historicalVisualClip.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(currentVisualClip.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(historicalVisualClip.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(historicalVisualClip.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(historicalVisualClip.Find("Seamount Sprite"), Is.Null);
            Assert.That(currentVisualClip.Find("Seamount Sprite"), Is.Null);
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
            Assert.That(GameObject.Find("Continue To Simulate"), Is.Null,
                "The primary action should not appear until the required findings are recorded.");
            Assert.That(GameObject.Find("Observe Finding Progress Text"), Is.Null);
            Assert.That(FindGameObject("First Finding Title").GetComponent<Text>().text, Does.Contain("TODAY"));
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("TODAY"));
            Assert.That(FindGameObject("Edna Icon").GetComponent<Image>().sprite, Is.EqualTo(InvestigationScenarioIconLibrary.Investigate));
            Assert.That(FindButton("Toggle Edna Guide").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(40f));
            Assert.That(FindButton("Species Marker shark").transform.Find("Unrecorded Finding Cue"), Is.Not.Null,
                "Easy mode should make one first action discoverable without adding a permanent label.");
            Assert.That(GameObject.Find("Species Facts Hint"), Is.Null);
            foreach (string look in new[] { "natural", "illustrated", "bathymetry", "surface-mask" })
            {
                Texture2D texture = Resources.Load<Texture2D>("Investigation/Seamount/" + look);
                Assert.That(texture, Is.Not.Null, look);
                Assert.That(texture.width, Is.LessThanOrEqualTo(1024));
                Assert.That(texture.width, Is.EqualTo(backdrop.NaturalTexture.width));
                Assert.That(texture.height, Is.EqualTo(backdrop.NaturalTexture.height));
                Assert.That(texture.mipmapCount, Is.EqualTo(1));
                Assert.That(texture.isReadable, Is.False);
            }
            AssertImageOnlyMarker("Historical Species Marker shark");
            AssertImageOnlyMarker("Species Marker shark");
            AssertImageOnlyMarker("Species Marker tuna");
            AssertImageOnlyMarker("Species Marker krill");
            AssertImageOnlyMarker("Species Marker sea_star");
            AssertImageOnlyMarker("Species Marker mussel");
            float seabedTop = Mathf.Max(WorldRect(FindButton("Species Marker sea_star").GetComponent<RectTransform>()).yMax,
                WorldRect(FindButton("Species Marker mussel").GetComponent<RectTransform>()).yMax);
            foreach (string species in new[] { "shark", "tuna", "krill" })
                Assert.That(WorldRect(FindButton("Species Marker " + species).GetComponent<RectTransform>()).yMin,
                    Is.GreaterThan(seabedTop), "Pelagic findings must remain above the seabed findings.");
            Texture2D terrainMask = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(terrainMask,
                File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources/Investigation/Seamount/surface-mask.png")), false), Is.True);
            try
            {
                foreach (string species in new[] { "shark", "tuna", "krill" })
                    AssertMarkerHabitat("Species Marker " + species, currentMountain, terrainMask, false);
                foreach (string species in new[] { "sea_star", "mussel" })
                    AssertMarkerHabitat("Species Marker " + species, currentMountain, terrainMask, true);
            }
            finally { Object.DestroyImmediate(terrainMask); }
            Button observeStage = FindButton("Stage Observe");
            Assert.That(observeStage.GetComponent<Outline>(), Is.Null);
            Assert.That(observeStage.transform.Find("Active Stage Accent").GetComponent<Image>().color, Is.EqualTo((Color)InvestigationTheme.Primary));
            Assert.That(FindButton("Stage Simulate").GetComponent<Outline>(), Is.Null);
            Assert.That(FindButton("Stage Simulate").transform.Find("Active Stage Accent"), Is.Null);
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
        public IEnumerator InvestigationScene_EdnaGuidanceTracksTheFirstObserveActionsAndCanBeCollapsed()
        {
            yield return LoadInvestigationScene();

            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("Record one finding"));
            Assert.That(FindGameObject("Edna Identity ObserveFirstFinding").GetComponent<Text>().text, Does.Contain("HINT 1/3"));
            Click("Increase Edna Hint");
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("any framed species"));
            Assert.That(FindGameObject("Edna Identity ObserveFirstFinding").GetComponent<Text>().text, Does.Contain("HINT 2/3"));
            Click("Increase Edna Hint");
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("Select any framed species"));
            Assert.That(FindButton("Increase Edna Hint").GetComponentInChildren<Text>().text, Is.EqualTo("Less help"));
            Click("Increase Edna Hint");
            Assert.That(FindGameObject("Edna Identity ObserveFirstFinding").GetComponent<Text>().text, Does.Contain("HINT 1/3"));
            Click("Toggle Edna Guide");
            Assert.That(GameObject.Find("Edna Prompt"), Is.Null);
            Assert.That(FindGameObject("Edna Prompt Hidden").GetComponent<Text>().text, Does.Contain("hidden"));
            Assert.That(FindButton("Toggle Edna Guide").GetComponentInChildren<Text>().text, Is.EqualTo("Show task"));
            Click("Toggle Edna Guide");
            Assert.That(FindGameObject("Edna Prompt"), Is.Not.Null);

            Click("Species Marker shark");
            Assert.That(FindButton("Species Marker shark").transform.Find("Unrecorded Finding Cue"), Is.Null);
            Assert.That(FindButton("Species Marker tuna").transform.Find("Unrecorded Finding Cue"), Is.Not.Null);
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("Record 4 more"));
            Assert.That(FindGameObject("Edna Identity ObserveRemainingFindings").GetComponent<Text>().text, Does.Contain("HINT 1/3"),
                "Moving to a new task should reset Edna to the least explicit hint.");
            Assert.That(FindGameObject("Observe Finding Progress Text").GetComponent<Text>().text, Does.Contain("FINDINGS 1 / 5"));
            Assert.That(FindGameObject("Notebook E01_SHARK_NONDETECTION").transform.Find("Evidence Status/Evidence Status Text").GetComponent<Text>().text,
                Is.EqualTo("NEW"));

            Click("Species Marker tuna");
            Click("Species Marker krill");
            Click("Species Marker sea_star");
            Click("Species Marker mussel");
            Assert.That(GameObject.Find("Observe Finding Progress"), Is.Null);
            Button continueButton = FindButton("Continue To Simulate");
            Assert.That(continueButton.GetComponentInChildren<Text>().text, Is.EqualTo("Test possible causes →"));
            Assert.That(continueButton.transform.parent.name, Is.EqualTo("Investigation Notebook"));
            Assert.That(continueButton.GetComponent<RectTransform>().anchorMin.x, Is.EqualTo(0.22f).Within(0.001f));
            Assert.That(continueButton.GetComponent<RectTransform>().anchorMax.x, Is.EqualTo(0.78f).Within(0.001f));
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
            Click("Prediction shark");
            Click("Observation E04_BENTHIC_STABLE");
            Assert.That(FindGameObject("Status Toast").activeSelf, Is.False);
            Assert.That(FindGameObject("Comparison Open Text").GetComponent<Text>().text, Does.Contain("cannot settle"));
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.CompletedObjectiveCount, Is.Zero);
            Assert.That(GameObject.Find("Judgement Row"), Is.Null);
            Assert.That(FindButton("Observation E01_SHARK_NONDETECTION").interactable, Is.True);
            Click("Toggle Notebook Drawer");
            yield return null;
            Assert.That(FindGameObject("Notebook E04_BENTHIC_STABLE").transform.Find("Evidence Status/Evidence Status Text").GetComponent<Text>().text,
                Is.EqualTo("OPEN"),
                "An inconclusive evidence selection should remain visibly unresolved in the Notebook.");
            Click("Close Notebook Drawer");
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ClickedSpeciesDetailsAndHighlightsExpireAfterTheLatestTap()
        {
            bool previousMotion = InvestigationMotionSettings.ReducedMotion;
            try
            {
                InvestigationMotionSettings.SetReducedMotion(true);
                yield return LoadInvestigationScene();
                Click("Species Marker shark");
                yield return new WaitForSecondsRealtime(0.75f);
                Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
                Click("Species Marker tuna");
                yield return new WaitForSecondsRealtime(0.85f);
                Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null,
                    "The earlier click's timer must not dismiss a newer finding.");
                Assert.That(FindButton("Species Marker tuna").transform.Find("Paired Species Focus").gameObject.activeSelf, Is.True);
                yield return new WaitForSecondsRealtime(0.85f);
                Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
                foreach (string name in new[] { "Species Marker tuna", "Historical Species Marker tuna", "Species Marker shark", "Historical Species Marker shark" })
                    Assert.That(FindButton(name).transform.Find("Paired Species Focus").gameObject.activeSelf, Is.False, name);
                Assert.That(FindButton("Species Marker shark").transform.Find("Recorded Finding"), Is.Not.Null);
                Assert.That(FindButton("Species Marker tuna").transform.Find("Recorded Finding"), Is.Not.Null);
                Click("Historical Species Marker shark");
                yield return new WaitForSecondsRealtime(1.65f);
                Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Has.Count.EqualTo(2));
            }
            finally { InvestigationMotionSettings.SetReducedMotion(previousMotion); }
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
            Assert.That(GameObject.Find("Species Facts Tooltip").GetComponent<Image>().raycastTarget, Is.False,
                "An unpinned hover preview must still allow the pointer to move between markers.");

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
            historicalShark.GetComponent<InvestigationHoverTooltipTrigger>().OnPointerExit(tap);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(GameObject.Find("Species Facts Tooltip").transform.Find("Tooltip Title").GetComponent<Text>().text, Is.EqualTo("Great Hammerhead Shark"));
            Assert.That(FindButton("Close Species Facts"), Is.Not.Null);
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Assert.That(FindGameObject("Status Toast").activeSelf, Is.True);
            Assert.That(FindGameObject("Status Message").GetComponent<Text>().text, Does.Contain("reference survey"));

            Button currentShark = FindButton("Species Marker shark");
            Vector2 positionBeforeRecording = currentShark.GetComponent<RectTransform>().anchorMin;
            ExecuteEvents.Execute(currentShark.gameObject, tap, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(controller.State.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
            Assert.That(FindButton("Species Marker shark").GetComponent<RectTransform>().anchorMin,
                Is.EqualTo(positionBeforeRecording),
                "Recording a finding must not reshuffle the deterministic survey layout.");
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(GameObject.Find("Species Facts Tooltip").transform.Find("Tooltip Title").GetComponent<Text>().text, Is.EqualTo("Great Hammerhead Shark"));
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
            Click("Threat plastic");
            Click("Run Selected Model");
            Click("Stage Observe");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            ScrollRect notebookScroll = FindGameObject("Notebook Entry Scroll").GetComponent<ScrollRect>();
            Button cta = FindButton("Continue To Simulate");
            Assert.That(notebookScroll.content.childCount, Is.EqualTo(7));
            Assert.That(FindGameObject("Notebook Group FOOD-WEB PATTERN"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Group STABLE CONTROLS"), Is.Not.Null);
            Assert.That(GameObject.Find("Notebook Group FOLLOW-UP CLUES"), Is.Null,
                "Follow-up clues should not appear before the provisional report and ROV review.");
            Transform firstEntry = FindGameObject("Notebook E01_SHARK_NONDETECTION").transform;
            Assert.That(firstEntry.Find("Evidence Bullet"), Is.Not.Null);
            Assert.That(firstEntry.Find("Paper Line"), Is.Not.Null);
            Assert.That(firstEntry.GetComponent<Image>().color.a, Is.Zero);
            Text firstNotebookTitle = firstEntry.Find("Observation").GetComponent<Text>();
            Assert.That(firstNotebookTitle.supportRichText, Is.True);
            Assert.That(firstNotebookTitle.text, Does.Contain("<b><color=#"));
            AssertTextFitsItsRect(firstNotebookTitle);
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
            Assert.That(FindGameObject("Case Subtitle").GetComponent<Text>().text, Does.Contain("Seamount A"));
            Assert.That(FindGameObject("Case Subtitle").GetComponent<Text>().text, Does.Contain("Survey 12"));
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
            Button observePrimary = FindButton("Continue To Simulate");
            Assert.That(observePrimary.colors.highlightedColor, Is.EqualTo(new Color(0.96f, 0.96f, 0.96f, 1f)),
                "Paper buttons should darken slightly on hover.");
            Assert.That(observePrimary.colors.selectedColor, Is.EqualTo(observePrimary.colors.normalColor),
                "A clicked paper button must not retain its hover tint.");
            Assert.That(observePrimary.GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(observePrimary.GetComponent<Shadow>().effectColor, Is.EqualTo((Color)InvestigationTheme.PaperShadow));
            Assert.That(observePrimary.GetComponent<Outline>(), Is.Null);
            Click("Continue To Simulate");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            AssertSimulateColumnsUseIndependentHeaderHeights();
            Assert.That(FindGameObject("Investigation Footer").activeSelf, Is.False);
            Button notebookToggle = FindButton("Toggle Notebook Drawer");
            Assert.That(notebookToggle.transform.Find("Label").gameObject.activeSelf, Is.False);
            Assert.That(notebookToggle.transform.Find("Notebook Button Artwork/Notebook Cover"), Is.Not.Null);
            Assert.That(notebookToggle.transform.Find("Notebook Finding Count/Notebook Finding Count Text").GetComponent<Text>().text, Is.EqualTo("5"));
            Assert.That(notebookToggle.transform.Find("Notebook Button Tooltip").gameObject.activeSelf, Is.False);
            Assert.That(FindGameObject("Investigation Content").GetComponent<RectTransform>().offsetMin.y, Is.EqualTo(8f).Within(0.1f));
            RectTransform modelColumn = FindGameObject("Simulation Models").GetComponent<RectTransform>();
            RectTransform comparisonColumn = FindGameObject("Comparison Workspace").GetComponent<RectTransform>();
            Assert.That(modelColumn.position.x, Is.LessThan(comparisonColumn.position.x));
            Assert.That(Mathf.Abs(modelColumn.rect.width - comparisonColumn.rect.width), Is.LessThan(2f));
            Assert.That(FindGameObject("Threat Choices").transform.IsChildOf(modelColumn), Is.True);
            Assert.That(FindGameObject("Simulation Navigation").transform.IsChildOf(comparisonColumn), Is.True);
            Assert.That(notebookToggle.transform.IsChildOf(FindGameObject("Simulation Navigation").transform), Is.True);
            Assert.That(FindGameObject("Simulation Navigation").GetComponent<RectTransform>().rect.height, Is.EqualTo(52f).Within(0.1f));
            Assert.That(notebookToggle.GetComponent<RectTransform>().rect.height, Is.EqualTo(50f).Within(0.1f));
            Assert.That(notebookToggle.GetComponent<RectTransform>().rect.width, Is.EqualTo(62f).Within(0.1f));
            AssertBottomAligned(notebookToggle.GetComponent<RectTransform>(), FindGameObject("Simulation Navigation").GetComponent<RectTransform>());
            Click("Toggle Notebook Drawer");
            yield return null;
            Assert.That(FindGameObject("Notebook Drawer"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Drawer Scrim"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Drawer Entries").transform.Find("Notebook E01_SHARK_NONDETECTION"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Drawer Entries").transform.Find("Notebook E01_SHARK_NONDETECTION/Evidence Status/Evidence Status Text").GetComponent<Text>().text,
                Is.EqualTo("NEW"));
            Assert.That(FindGameObject("Page Content").GetComponent<CanvasGroup>().interactable, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Close Notebook Drawer"));
            Click("Close Notebook Drawer");
            yield return null;
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Null);
            Assert.That(FindGameObject("Page Content").GetComponent<CanvasGroup>().interactable, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Toggle Notebook Drawer"));
            Assert.That(FindGameObject("Report Gate Hint").GetComponent<RectTransform>().rect.height, Is.EqualTo(28f).Within(0.1f));
            AssertBottomAligned(FindGameObject("Report Gate Hint").GetComponent<RectTransform>(), FindGameObject("Simulation Navigation").GetComponent<RectTransform>());
            comparisonColumn = FindGameObject("Comparison Workspace").GetComponent<RectTransform>();
            Assert.That(comparisonColumn.GetComponent<VerticalLayoutGroup>().padding.bottom, Is.EqualTo(4));
            Assert.That(FindButton("Threat longline").transform.Find("Threat Status"), Is.Not.Null);
            Assert.That(FindGameObject("Case Questions"), Is.Not.Null);
            Assert.That(FindGameObject("Case Questions Heading").GetComponent<Text>().text, Does.Contain("GUIDED"));
            Assert.That(FindButton("Threat warming"), Is.Null);
            Assert.That(GameObject.Find("Case Question warming"), Is.Null);
            Assert.That(FindGameObject("Threat Choices").GetComponentsInChildren<Button>(), Has.Length.EqualTo(3));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("MODELS 0/3"));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("CHECKS 0/5"));
            Assert.That(FindGameObject("Case Question plastic").GetComponent<Outline>(), Is.Not.Null,
                "With no cause preselected, Easy guidance should begin with the first screening question.");
            Assert.That(GameObject.Find("Case Question benthic"), Is.Null,
                "The decisive benthic question must remain hidden until after the ROV follow-up.");
            Assert.That(FindGameObject("Case Question Grid").transform.childCount, Is.EqualTo(1),
                "Easy mode should reveal only the current Case Question.");
            Assert.That(FindGameObject("Comparison Gate").GetComponent<Text>().text, Does.Contain("Choose a cause"));
            Assert.That(FindGameObject("Model Title").GetComponent<Text>().text, Is.EqualTo("Choose a cause"));
            Assert.That(GameObject.Find("Run Selected Model"), Is.Null,
                "The Run action should appear only after the player chooses a cause.");
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("Choose one possible cause"));
            GameObject scenarioPlaceholder = FindGameObject("Scenario Placeholder Artwork");
            Assert.That(scenarioPlaceholder.GetComponent<Image>().sprite,
                Is.EqualTo(InvestigationScenarioIconLibrary.Investigate));
            Assert.That(scenarioPlaceholder.GetComponent<InvestigationGlyphGraphic>(), Is.Null,
                "The empty scenario must use neutral artwork before a cause is selected.");
            Assert.That(GameObject.Find("Scenario Artwork"), Is.Null);
            Assert.That(FindButton("Threat longline").GetComponent<Image>().color,
                Is.Not.EqualTo(new Color32(70, 48, 48, 255)),
                "The correct cause must not be selected before the player investigates it.");
            Assert.That(FindButton("Threat longline").GetComponent<Outline>(), Is.Null);
            AssertSimulateWorkspaceWidthFits();
            AssertSimulatePageFitsViewportHeight();

            Click("Threat plastic");
            Assert.That(GameObject.Find("Scenario Placeholder Artwork"), Is.Null);
            Assert.That(FindGameObject("Scenario Artwork").GetComponent<Image>(), Is.Not.Null,
                "Selecting a real cause should show that cause's imported transparent artwork.");
            Assert.That(FindButton("Run Selected Model"), Is.Not.Null);
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("Run the Plastic pollution model"));
            Assert.That(FindGameObject("Case Question plastic").GetComponent<Outline>(), Is.Not.Null,
                "Changing cause should move Easy guidance to that cause's next objective.");
            Assert.That(GameObject.Find("Case Question food_web"), Is.Null,
                "Easy mode should keep future Case Questions collapsed until they become current.");
            Assert.That(FindGameObject("Comparison Gate").GetComponent<Text>().text, Does.Contain("Run Plastic pollution"));
            Click("Run Selected Model");
            Assert.That(controller.State.HasDiscoveredObservation("E05_TEMPERATURE_NORMAL"), Is.False);
            Assert.That(GameObject.Find("Prediction Target Temperature temperature"), Is.Null);
            Compare("mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch);

            Click("Threat longline");
            Click("Run Selected Model");
            Assert.That(controller.State.HasTriedThreat("longline"), Is.True);
            Assert.That(FindButton("Threat longline").transform.Find("Threat Checks Pending"), Is.Not.Null);
            Assert.That(FindButton("Threat longline").transform.Find("Threat Checks Complete"), Is.Null);
            Assert.That(GameObject.Find("Food Web Prediction"), Is.Not.Null);
            RectTransform environmentalPredictions = FindGameObject("Environmental Predictions").GetComponent<RectTransform>();
            Assert.That(environmentalPredictions.rect.height, Is.EqualTo(44f).Within(0.1f),
                "Larger indicator typography must not change the environment-row height.");
            Assert.That(environmentalPredictions.childCount, Is.EqualTo(2));
            Assert.That(GameObject.Find("Model Indicator TEMP"), Is.Null);
            Transform seafloorIndicator = FindGameObject("Model Indicator SEAFLOOR").transform;
            Text seafloorLabel = seafloorIndicator.Find("Indicator Label").GetComponent<Text>();
            Text seafloorValue = seafloorIndicator.Find("Indicator Value").GetComponent<Text>();
            Assert.That(seafloorLabel.fontSize, Is.EqualTo(11));
            Assert.That(seafloorValue.fontSize, Is.EqualTo(14));
            Assert.That(seafloorLabel.rectTransform.TransformPoint(seafloorLabel.rectTransform.rect.center).y,
                Is.GreaterThan(seafloorValue.rectTransform.TransformPoint(seafloorValue.rectTransform.rect.center).y + 4f));
            AssertModelIndicatorTextVisible("SEAFLOOR");
            AssertModelIndicatorTextVisible("LOOK FOR");
            Assert.That(FindGameObject("Model Indicator SEAFLOOR").GetComponent<Outline>(), Is.Null);
            Assert.That(FindGameObject("Model Indicator LOOK FOR").GetComponent<Outline>(), Is.Null);
            Assert.That(seafloorIndicator.Find("Indicator Accent"), Is.Null);
            Assert.That(FindGameObject("Choose Prediction Step"), Is.Not.Null);
            Assert.That(GameObject.Find("Prediction Observation Pairing"), Is.Null,
                "Evidence candidates should stay hidden until a prediction is selected.");
            Assert.That(GameObject.Find("Judgement Row"), Is.Null,
                "Evidence checks should not require separate judgement controls.");
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("Select one prediction"));
            Click("Prediction shark");
            Transform predictionSelection = FindGameObject("Prediction Selection").transform;
            Transform observationSelection = FindGameObject("Observation Selection").transform;
            Assert.That(predictionSelection.Find("Panel Accent").GetComponent<Image>().color, Is.EqualTo((Color)InvestigationTheme.Primary));
            Assert.That(observationSelection.Find("Panel Accent").GetComponent<Image>().color, Is.EqualTo((Color)InvestigationTheme.TextSecondary));
            Assert.That(predictionSelection.GetComponent<Outline>(), Is.Null);
            Assert.That(observationSelection.GetComponent<Outline>(), Is.Null);
            Assert.That(GameObject.Find("Judgement Row"), Is.Null);
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("Select a finding in WHAT WE FOUND"));
            Click("Toggle Notebook Drawer");
            yield return null;
            Assert.That(FindGameObject("Notebook E01_SHARK_NONDETECTION").GetComponent<Image>().color.a, Is.GreaterThan(0.5f),
                "The Notebook should highlight evidence directly related to the selected prediction.");
            Assert.That(FindGameObject("Notebook E02_TUNA_WIDER_DETECTION").GetComponent<Image>().color.a, Is.Zero);
            Click("Close Notebook Drawer");
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
            Assert.That(sharkObservationLabel.text, Does.Contain(ColorUtility.ToHtmlStringRGB(InvestigationTheme.TextSecondary)));
            Assert.That(FindButton("Observation E01_SHARK_NONDETECTION").GetComponent<RectTransform>().rect.height, Is.EqualTo(44f).Within(0.1f));
            Click("Prediction tuna");
            Text tunaObservationLabel = FindButton("Observation E02_TUNA_WIDER_DETECTION").GetComponentInChildren<Text>();
            Assert.That(tunaObservationLabel.text, Does.Contain("detected at more sites</color></b>").IgnoreCase);
            Assert.That(tunaObservationLabel.text, Does.Contain(ColorUtility.ToHtmlStringRGB(InvestigationTheme.Primary)));
            Assert.That(GameObject.Find("Comparison Guide"), Is.Null);
            Click("Observation E02_TUNA_WIDER_DETECTION");
            Assert.That(FindGameObject("Comparison Saved Text").GetComponent<Text>().text, Does.Contain("supports the model"));
            Assert.That(FindButton("Observation E02_TUNA_WIDER_DETECTION").interactable, Is.False);
            Assert.That(FindButton("Observation E02_TUNA_WIDER_DETECTION").transform.Find("Selected Evidence Check"), Is.Not.Null);
            Assert.That(FindGameObject("Food Web Prediction").transform.IsChildOf(FindGameObject("Simulation Models").transform), Is.True);
            Assert.That(FindGameObject("Prediction Observation Pairing").transform.IsChildOf(FindGameObject("Comparison Workspace").transform), Is.True);
            Assert.That(GameObject.Find("Judgement Row"), Is.Null);
            Assert.That(FindGameObject("Comparison Saved Summary").transform.IsChildOf(FindGameObject("Comparison Workspace").transform), Is.True);
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
            Compare("shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match);
            Assert.That(FindButton("Prediction shark").transform.Find("Comparison Locked").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(FindGameObject("Comparison Saved Summary"), Is.Not.Null);
            Assert.That(GameObject.Find("Judgement Row"), Is.Null,
                "Direct evidence selection should show confirmation without adding judgement controls.");
            Click("Toggle Notebook Drawer");
            yield return null;
            Assert.That(FindGameObject("Notebook E01_SHARK_NONDETECTION").transform.Find("Evidence Status/Evidence Status Text").GetComponent<Text>().text,
                Is.EqualTo("USED"));
            Click("Close Notebook Drawer");
            Click("Prediction tuna");
            Assert.That(FindGameObject("Comparison Saved Summary"), Is.Not.Null,
                "The earlier evidence selection should already have saved the Tuna comparison.");
            Compare("krill", "E03_KRILL_NONDETECTION", ComparisonJudgement.Match);

            Click("Threat bottom_trawling");
            Click("Run Selected Model");
            Assert.That(controller.State.HasTriedThreat("bottom_trawling"), Is.True);
            Compare("tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match);

            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(5));
            Assert.That(controller.State.AcceptedComparisonCount, Is.EqualTo(5));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("MODELS 3/3"));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("CHECKS 5/5"));
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
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            Assert.That(FindButton("Provisional Cause warming"), Is.Null);
            Assert.That(FindGameObject("Provisional Cause Choices").GetComponentsInChildren<Button>(), Has.Length.EqualTo(3));
            Click("Confirm Provisional Idea");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            AssertActivePageHeadingSharesRow();
            Assert.That(FindGameObject("Investigation Footer").GetComponent<RectTransform>().rect.height, Is.EqualTo(60f).Within(0.1f));
            Assert.That(FindButton("Back To Simulator").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(126f, 30f)));
            Assert.That(FindButton("Toggle Notebook Drawer").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(62f, 50f)));
            Assert.That(FindButton("Toggle Notebook Drawer").transform.parent, Is.EqualTo(FindGameObject("Footer Right").transform));
            Assert.That(FindButton("Restart Case").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(100f, 30f)));
            Assert.That(FindButton("Review ROV Follow-up").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            Assert.That(FindButton("Review ROV Follow-up").transform.IsChildOf(FindGameObject("ROV Confirmation").transform), Is.True,
                "The primary ROV action must be next to its evidence panel.");
            Canvas.ForceUpdateCanvases();
            Transform sealedConfirmation = FindGameObject("ROV Confirmation").transform;
            AssertFullBorderAccent(sealedConfirmation, InvestigationTheme.Primary);
            Assert.That(sealedConfirmation.Find("Panel Accent"), Is.Null);
            Canvas.ForceUpdateCanvases();
            Assert.That(GameObject.Find("Survey Report Paper"), Is.Null,
                "The report questions must stay hidden until the ROV follow-up has been checked.");
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
            bool reportHasOverflow = reportScroll.content.rect.height > reportScroll.viewport.rect.height + 1f;
            float previousReportPosition = reportScroll.verticalNormalizedPosition;
            Click("Stage Report");
            if (reportHasOverflow)
                Assert.That(reportScroll.verticalNormalizedPosition, Is.EqualTo(previousReportPosition).Within(0.02f),
                    "Refreshing a scrollable Report stage must preserve the reader's place.");

            EventSystem.current.SetSelectedGameObject(FindButton("Review ROV Follow-up").gameObject);
            Click("Review ROV Follow-up");
            Assert.That(controller.State.ConfirmationReviewed, Is.True);
            Assert.That(controller.State.HasDiscoveredObservation("E07_FISHING_LINE"), Is.True);
            Assert.That(controller.State.HasDiscoveredObservation("E08_SEAFLOOR_INTACT"), Is.True);
            if (reportHasOverflow)
                Assert.That(reportScroll.verticalNormalizedPosition, Is.EqualTo(previousReportPosition).Within(0.02f),
                    "Reviewing the ROV follow-up must preserve a scrollable Report's reading position.");
            Assert.That(WorldRect(FindGameObject("ROV Title").GetComponent<RectTransform>()).yMin,
                Is.GreaterThan(WorldRect(FindGameObject("ROV Detail").GetComponent<RectTransform>()).yMax));
            Assert.That(FindGameObject("ROV Title").GetComponent<Text>().text, Is.EqualTo("New ROV evidence received"));
            Assert.That(FindGameObject("ROV Detail").GetComponent<Text>().text, Does.Contain("added to your notebook"));
            Assert.That(FindGameObject("ROV Confirmation").GetComponent<RectTransform>().rect.height, Is.GreaterThan(300f));
            Transform reviewedConfirmation = FindGameObject("ROV Confirmation").transform;
            AssertFullBorderAccent(reviewedConfirmation, InvestigationTheme.Success);
            Assert.That(reviewedConfirmation.Find("Panel Accent"), Is.Null);
            Rect rovSummary = WorldRect(FindGameObject("ROV Detail").GetComponent<RectTransform>());
            Rect rovEvidence = WorldRect(FindGameObject("ROV Evidence").GetComponent<RectTransform>());
            float scaledRovSpacing = 12f * reviewedConfirmation.lossyScale.y;
            Assert.That(rovSummary.yMin - rovEvidence.yMax, Is.EqualTo(scaledRovSpacing).Within(1f),
                "The ROV summary and evidence cards should be compact without overlapping.");
            Assert.That(FindGameObject("Confirmation E07_FISHING_LINE").transform.Find("Confirmation Icon").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(FindGameObject("Confirmation E07_FISHING_LINE").transform.Find("Confirmation Icon").GetComponent<InvestigationGlyphGraphic>(), Is.Null);
            Assert.That(FindGameObject("Confirmation E07_FISHING_LINE").transform.Find("Notebook Added").GetComponent<Text>().text, Is.EqualTo("ADDED TO NOTEBOOK · ROV"));
            if (!InvestigationMotionSettings.ReducedMotion)
            {
                yield return WaitForCondition(
                    () => FindGameObject("Confirmation E07_FISHING_LINE").GetComponent<CanvasGroup>().alpha >= 0.99f
                        && FindGameObject("Confirmation E08_SEAFLOOR_INTACT").GetComponent<CanvasGroup>().alpha >= 0.99f,
                    1.5f,
                    "ROV evidence cards did not complete their sequential reveal.");
            }
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Re-test Fishing Models"),
                "Keyboard focus should move from Review ROV to the new primary re-test action.");
            Assert.That(InvestigationEvidenceIconLibrary.FishingLine, Is.Not.Null);
            Assert.That(InvestigationEvidenceIconLibrary.Seafloor, Is.Not.Null);
            Assert.That(InvestigationEvidenceIconLibrary.Laboratory, Is.Not.Null);
            Assert.That(InvestigationEvidenceIconLibrary.EDNASignal, Is.Not.Null);
            Assert.That(FindButton("Re-test Fishing Models"), Is.Not.Null);
            Assert.That(FindButton("Re-test Fishing Models").GetComponentInChildren<Text>().text, Is.EqualTo("Compare fishing models →"));
            Assert.That(FindButton("Toggle Notebook Drawer").transform.Find("Notebook Finding Count/Notebook Finding Count Text").GetComponent<Text>().text, Is.EqualTo("7"));
            Click("Toggle Notebook Drawer");
            yield return null;
            Assert.That(FindGameObject("Notebook Drawer Title").GetComponent<Text>().text, Is.EqualTo("MY NOTEBOOK · 7"));
            Assert.That(FindGameObject("Notebook Drawer Entries").transform.Find("Notebook Group FOLLOW-UP CLUES"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook Drawer Entries").transform.Find("Notebook E07_FISHING_LINE"), Is.Not.Null);
            ScrollRect drawerScroll = FindGameObject("Notebook Drawer Scroll").GetComponent<ScrollRect>();
            bool drawerHasOverflow = drawerScroll.content.rect.height > drawerScroll.viewport.rect.height + 1f;
            drawerScroll.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            Click("Close Notebook Drawer");
            yield return null;
            Click("Toggle Notebook Drawer");
            yield return null;
            if (drawerHasOverflow)
                Assert.That(FindGameObject("Notebook Drawer Scroll").GetComponent<ScrollRect>().verticalNormalizedPosition, Is.LessThanOrEqualTo(0.02f),
                    "Closing and reopening a scrollable Notebook should preserve its reading position.");
            Click("Close Notebook Drawer");
            Assert.That(FindButton("Submit Final Report"), Is.Null,
                "The final report action must remain unavailable until the post-ROV benthic comparison is complete.");

            Click("Re-test Fishing Models");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("FINDINGS 5/5"));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("CHECKS 5/7"));
            Assert.That(FindGameObject("Case Question benthic"), Is.Not.Null);
            Assert.That(FindButton("Prediction sea_star").interactable, Is.True);
            Assert.That(FindButton("Prediction sea_star").transform.Find("Prediction").GetComponent<Text>().text, Is.EqualTo("Decrease"));
            Compare("sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch);

            Click("Threat longline");
            Assert.That(FindButton("Prediction sea_star").interactable, Is.True);
            Assert.That(FindButton("Prediction sea_star").transform.Find("Prediction").GetComponent<Text>().text, Is.EqualTo("Stable"));
            Compare("sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match);
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(7));
            Assert.That(controller.State.AcceptedComparisonCount, Is.EqualTo(7));
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("CHECKS 7/7"));
            Assert.That(FindButton("Return To Final Report"), Is.Not.Null);
            Click("Return To Final Report");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            AssertReportQuestion("Cause", 1);
            Assert.That(GameObject.Find("ROV Confirmation"), Is.Null,
                "The reviewed ROV banner should make room for the current question.");
            Assert.That(FindButton("Submit Final Report"), Is.Null);
            Assert.That(FindButton("Toggle Notebook Drawer").transform.position.x,
                Is.LessThan(FindButton("Continue Report Dialogue").transform.position.x));
            AssertSameRow("Report Title", "Report Metadata");
            Text reportMetadata = FindGameObject("Report Metadata").GetComponent<Text>();
            AssertTextFitsItsRect(reportMetadata);
            Assert.That(reportMetadata.resizeTextForBestFit, Is.True);
            AssertTextFitsItsRect(FindGameObject("Report Title").GetComponent<Text>());
            AssertTextFitsItsRect(FindGameObject("Provisional Reminder").GetComponent<Text>());
            Assert.That(FindGameObject("Provisional Reminder").GetComponent<Text>().text, Does.Contain("Bottom trawling"));
            AssertChildrenStayInsideLayout("Cause Choices");
            Assert.That(FindButton("Final Cause longline").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Final Cause longline").transform.Find("Paper Choice Face"), Is.Not.Null);
            Assert.That(FindButton("Final Cause warming"), Is.Null);

            Click("Continue Report Dialogue");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.InsufficientEvidence));
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
            Assert.That(FindGameObject("Report Section Feedback").GetComponent<Text>().text, Does.Contain("Choose a final cause"));
            Assert.That(FindGameObject("Status Toast").activeSelf, Is.False,
                "A question's diagnostic should appear once beside the answer.");
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(GameObject.Find("Report Cause Section").transform), Is.True);

            Click("Final Cause longline");
            yield return null;
            AssertReportQuestion("Reasoning", 2);
            Assert.That(controller.State.FinalThreatId, Is.EqualTo("longline"));
            Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("Long-line fishing"));
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(GameObject.Find("Report Reasoning Section").transform), Is.True);
            Click("Reasoning shared_habitat_shift");
            AssertReportQuestion("Reasoning", 2);
            Assert.That(FindGameObject("Report Section Feedback").GetComponent<Text>().text, Does.Contain("food-web cascade"));
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
            Click("Reasoning food_web_cascade");
            AssertReportQuestion("Evidence", 3);
            Assert.That(controller.State.SelectedReasoningId, Is.EqualTo("food_web_cascade"));
            AssertChildrenStayInsideLayout("Evidence Choices");
            Assert.That(FindButton("Report Evidence E07_FISHING_LINE").transform.Find("Evidence Icon").GetComponent<Image>().sprite, Is.Not.Null);
            Click("Continue Report Dialogue");
            Assert.That(FindGameObject("Report Section Feedback").GetComponent<Text>().text, Does.Contain("Select at least 4 observations"));
            Click("Report Evidence E01_SHARK_NONDETECTION");
            yield return null;
            Assert.That(FindButton("Report Evidence E01_SHARK_NONDETECTION").transform.Find("Selected Check"), Is.Not.Null);
            Click("Toggle Notebook Drawer");
            yield return null;
            Assert.That(FindGameObject("Notebook E01_SHARK_NONDETECTION").transform.Find("Evidence Status/Evidence Status Text").GetComponent<Text>().text,
                Is.EqualTo("REPORT"));
            Assert.That(FindGameObject("Notebook E07_FISHING_LINE").transform.Find("Evidence Status/Evidence Status Text").GetComponent<Text>().text,
                Is.EqualTo("NEW"));
            Click("Close Notebook Drawer");
            Click("Report Evidence E02_TUNA_WIDER_DETECTION");
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("Selected 2 / 4"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("FOOD WEB 2 / 2"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("BENTHIC 0 / 1"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("ROV 0 / 1"));
            Click("Continue Report Dialogue");
            Assert.That(FindGameObject("Report Section Feedback").GetComponent<Text>().text, Does.Contain("Select at least 4 observations"));
            Click("Report Evidence E04_BENTHIC_STABLE");
            Click("Report Evidence E07_FISHING_LINE");
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("Selected 4 / 4"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("BENTHIC 1 / 1"));
            Assert.That(FindGameObject("Evidence Progress").GetComponent<Text>().text, Does.Contain("ROV 1 / 1"));
            Assert.That(GameObject.Find("Report Limitation Section"), Is.Null,
                "Multi-select evidence stays open until the player continues.");
            Click("Toggle Notebook Drawer");
            Click("Continue Report Dialogue");
            AssertReportQuestion("Limitation", 4);
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Null,
                "Continuing must reveal the next question even when the Notebook was open.");
            Click("Continue Report Dialogue");
            Assert.That(FindGameObject("Report Section Feedback").GetComponent<Text>().text, Does.Contain("scientific limitation"));
            Click("Limitation L01_NONDETECTION_LIMITATION");
            Assert.That(GameObject.Find("Report Review"), Is.Not.Null);
            Assert.That(GameObject.Find("Report Limitation Section"), Is.Null);
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null,
                "Completing the dialogue should prepare a review, not send the report automatically.");
            Assert.That(GameObject.Find("Report Review Cause").GetComponentInChildren<Text>().text, Is.EqualTo("Your explanation"));
            foreach (Text text in GameObject.Find("Report Review").GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
            Assert.That(FindButton("Edit Report Cause"), Is.Not.Null);
            Assert.That(FindButton("Submit Final Report").GetComponentInChildren<Text>().text, Is.EqualTo("Send report"));
            Assert.That(FindButton("Submit Final Report").GetComponents<Shadow>().Length, Is.EqualTo(1));
            Assert.That(FindButton("Submit Final Report").GetComponent<Outline>(), Is.Null);
            Assert.That(FindButton("Submit Final Report").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(104f, 30f)));
            reportScroll.verticalNormalizedPosition = 0.15f;
            Click("Submit Final Report");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            if (reportScroll.content.rect.height > reportScroll.viewport.rect.height + 1f)
                Assert.That(reportScroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.02f),
                    "A scrollable Case Closed page must start at the top.");
            else
            {
                Rect summaryBounds = WorldRect(GameObject.Find("Case Closed Summary").GetComponent<RectTransform>());
                Rect viewportBounds = WorldRect(reportScroll.viewport);
                Assert.That(summaryBounds.yMax, Is.LessThanOrEqualTo(viewportBounds.yMax + 1f));
                Assert.That(summaryBounds.yMin, Is.GreaterThanOrEqualTo(viewportBounds.yMin - 1f),
                    "A compact Case Closed summary should be fully visible without scrolling.");
            }
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
            Assert.That(GameObject.Find("Increase Edna Hint"), Is.Null,
                "Hard mode should not expose escalating guided hints.");
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
            Assert.That(FindGameObject("Case Questions Heading").GetComponent<Text>().text, Does.Contain("INDEPENDENT 0/3"));
            Assert.That(FindGameObject("Case Question plastic").GetComponent<Outline>(), Is.Null,
                "Hard mode should keep the Case Questions but remove the guided next-question highlight.");
            Assert.That(FindGameObject("Comparison Gate").GetComponent<Text>().text, Does.Contain("Use the remaining Case Questions to choose a comparison"));
            Assert.That(FindButton("Threat plastic").transform.Find("Threat Status").GetComponent<Text>().text,
                Is.EqualTo("TO CHECK · 0/1"));
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

        [UnityTest]
        public IEnumerator InvestigationScene_ConflictingInputCanRecoverToAnExplicitStandaloneCase()
        {
            InvestigationGameInput input = new InvestigationGameInput();
            input.ednaResults.Add(new EDNAResultData { detectedSpeciesIds = new System.Collections.Generic.List<string> { "shark" } });
            try
            {
                InvestigationSessionBridge.SetInput(input);
                yield return LoadInvestigationScene();
                Assert.That(FindGameObject("Fatal Error").GetComponent<Text>().text, Does.Contain("fixed case"));
                Click("Start Standalone Case");
                yield return null;
                Assert.That(FindGameObject("Fatal Error"), Is.Null);
                Assert.That(FindButton("Species Marker shark").transform.Find("Missing Signal"), Is.Not.Null);
                Assert.That(InvestigationSessionBridge.PendingInput, Is.SameAs(input));
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Is.Empty);
            }
            finally { InvestigationSessionBridge.Clear(); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ObserveProgressAndStageGateUseOnlyCoreFindings()
        {
            InvestigationGameInput input = new InvestigationGameInput();
            input.discoveredObservationIds.AddRange(new[] { "E01_SHARK_NONDETECTION", "E02_TUNA_WIDER_DETECTION",
                "E03_KRILL_NONDETECTION", "E04_BENTHIC_STABLE", "L01_NONDETECTION_LIMITATION" });
            try
            {
                InvestigationSessionBridge.SetInput(input);
                yield return LoadInvestigationScene();
                Assert.That(FindGameObject("Observe Finding Progress Text").GetComponent<Text>().text, Does.Contain("4 / 5"));
                Assert.That(FindButton("Continue To Simulate"), Is.Null);
                Click("Stage Simulate");
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.Phase, Is.EqualTo(InvestigationPhase.Observe));
                Click("Species Marker mussel");
                Click("Continue To Simulate");
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            }
            finally { InvestigationSessionBridge.Clear(); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_RecordedMarkersLinkBothSurveyMaps()
        {
            yield return LoadInvestigationScene();
            Button shark = FindButton("Species Marker shark");
            Assert.That(shark.transform.Find("Recorded Finding"), Is.Null);
            ExecuteEvents.Execute(shark.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.selectHandler);
            Assert.That(shark.transform.Find("Paired Species Focus").gameObject.activeSelf, Is.True);
            Assert.That(FindButton("Historical Species Marker shark").transform.Find("Paired Species Focus").gameObject.activeSelf, Is.True);
            Assert.That(FindButton("Species Marker tuna").transform.Find("Paired Species Focus").gameObject.activeSelf, Is.False);
            Click("Species Marker shark");
            yield return null;
            Assert.That(FindButton("Species Marker shark").transform.Find("Recorded Finding"), Is.Not.Null);
            Assert.That(FindGameObject("Tooltip Details").GetComponent<Text>().text, Does.Contain("RECORDED IN NOTEBOOK"));
            Assert.That(FindGameObject("Tooltip Details").GetComponent<Text>().text, Does.Contain("Shark repeatedly not detected"));
            Click("Species Marker shark");
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_HistoricalSupplementShowsNonDetectionAndReadOnlyStatus()
        {
            InvestigationGameInput input = new InvestigationGameInput();
            EDNAResultData result = new EDNAResultData();
            result.speciesObservations.Add(new EDNASpeciesObservationData { speciesId = "moon_jellyfish", surveyTimepoint = SurveyTimepoint.Historical,
                detectionState = SpeciesDetectionState.NotDetected, depthBand = DepthBand.Deep });
            input.ednaResults.Add(result);
            try
            {
                InvestigationSessionBridge.SetInput(input);
                yield return LoadInvestigationScene();
                Assert.That(FindButton("Species Marker moon_jellyfish"), Is.Null);
                Assert.That(FindButton("Historical Species Marker moon_jellyfish").transform.Find("Missing Signal"), Is.Not.Null);
                Click("Historical Species Marker moon_jellyfish");
                Assert.That(FindGameObject("Tooltip Details").GetComponent<Text>().text, Does.Contain("Not detected"));
                Assert.That(FindGameObject("Tooltip Details").GetComponent<Text>().text, Does.Contain("read only"));
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Is.Empty);
            }
            finally { InvestigationSessionBridge.Clear(); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_HypothesisSummaryReopensSavedComparisonsWithoutSpoilers()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            Click("Toggle Notebook Drawer");
            Click("Toggle Hypothesis Summary");
            yield return null;
            Assert.That(FindGameObject("Hypothesis Summary longline").GetComponent<Text>().text, Does.Contain("SUPPORT 3"));
            Assert.That(FindGameObject("Hypothesis Summary plastic").GetComponent<Text>().text, Does.Contain("CHALLENGE 1"));
            Assert.That(GameObject.Find("Hypothesis Summary warming"), Is.Null);
            Assert.That(FindButton("Revisit Comparison longline shark"), Is.Null,
                "The overview should show compact cards until a cause is expanded.");
            foreach (string id in new[] { "plastic", "longline", "bottom_trawling" })
            {
                AssertTextFitsItsRect(GameObject.Find("Hypothesis Summary " + id).GetComponent<Text>());
                Assert.That(FindButton("Hypothesis Card " + id), Is.Not.Null);
            }
            Click("Hypothesis Card longline");
            Assert.That(FindButton("Revisit Comparison longline shark"), Is.Not.Null);
            Assert.That(FindButton("Revisit Comparison plastic mussel"), Is.Null);
            Click("Hypothesis Card plastic");
            Assert.That(FindButton("Revisit Comparison longline shark"), Is.Null);
            Assert.That(FindButton("Revisit Comparison plastic mussel"), Is.Not.Null);
            Click("Hypothesis Card longline");
            Assert.That(FindButton("Revisit Comparison longline sea_star"), Is.Null);
            Assert.That(FindGameObject("Notebook E07_FISHING_LINE"), Is.Null);
            int completed = controller.State.CompletedObjectiveCount;
            Click("Revisit Comparison longline shark");
            yield return null;
            Assert.That(FindGameObject("Notebook Drawer"), Is.Null);
            Assert.That(FindGameObject("Comparison Feedback").GetComponent<Text>().text, Does.Contain("Shark"));
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(completed));
            Assert.That(FindGameObject("Comparison Saved Summary"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ProvisionalReviewRequiresAnExplicitChoiceConfirmation()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            Click("Write Provisional Report");
            Assert.That(controller.State.ProvisionalThreatId, Is.Empty);
            Assert.That(FindGameObject("Provisional Selected Summary").GetComponent<Text>().text, Does.Contain("Bottom trawling"));
            Click("Provisional Cause plastic");
            Assert.That(FindGameObject("Provisional Selected Summary").GetComponent<Text>().text, Does.Contain("Plastic pollution"));
            Click("Cancel Provisional Idea");
            Assert.That(controller.State.ProvisionalThreatId, Is.Empty);
            Click("Write Provisional Report");
            Click("Provisional Cause longline");
            Click("Confirm Provisional Idea");
            yield return null;
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo("longline"));
            Assert.That(controller.State.ConfirmationReviewed, Is.False);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_EdnaResetsHelpWhenPredictionChangesWithinTheSameStep()
        {
            yield return LoadInvestigationScene();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            Click("Stage Simulate");
            Click("Threat longline");
            Click("Run Selected Model");
            Click("Prediction shark");
            Click("Increase Edna Hint");
            Assert.That(FindGameObject("Edna Identity ChooseObservation").GetComponent<Text>().text, Does.Contain("HINT 2/3"));
            Click("Prediction tuna");
            Assert.That(FindGameObject("Edna Identity ChooseObservation").GetComponent<Text>().text, Does.Contain("HINT 1/3"));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_EdnaIntroducesTheTaskAndRespondsToSpecificFindings()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain("I'm Edna"));
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain("what changed here"));
            Assert.That(GameObject.Find("Metrics").GetComponent<Text>().text, Does.Contain("CHECKS 0/5"));
            Click("Species Marker sea_star");
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain("Sea star stayed stable"));
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain("Record 4 more"));
            Click("Species Marker shark");
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain("Shark wasn't detected"));
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain("survey's limits"));
            Click("Species Marker shark");
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Has.Count.EqualTo(2));
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain("Record 3 more"));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_RovPanelShowsVisibleFindingsAndAnInPanelNextAction()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            yield return null;
            ClickThroughPointer(FindButton("Stage Report"));
            yield return null;
            ScrollRect provisional = GameObject.Find("Provisional Review Overlay").GetComponent<ScrollRect>();
            AssertInsideViewport(GameObject.Find("Provisional Review").GetComponent<RectTransform>(), provisional.viewport);
            foreach (Text text in provisional.content.GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
            ClickThroughPointer(FindButton("Provisional Cause longline"));
            yield return null;
            ClickThroughPointer(FindButton("Confirm Provisional Idea"));
            yield return null;
            Canvas.ForceUpdateCanvases();
            ScrollRect page = GameObject.Find("Investigation Content").GetComponent<ScrollRect>();
            AssertInsideViewport(GameObject.Find("Sealed ROV Finding 1").GetComponent<RectTransform>(), page.viewport);
            AssertInsideViewport(GameObject.Find("Sealed ROV Finding 2").GetComponent<RectTransform>(), page.viewport);
            Assert.That(GameObject.Find("Confirmation E07_FISHING_LINE"), Is.Null);
            Assert.That(GameObject.Find("ROV Detail").GetComponent<Text>().text, Does.Contain("first idea is saved"));
            Button open = FindButton("Review ROV Follow-up");
            AssertInsideViewport(open.GetComponent<RectTransform>(), page.viewport);
            ClickThroughPointer(open);
            // Interrupt the reveal with routine UI refreshes; new cards must stay visible.
            Click("Difficulty Toggle");
            Click("Toggle Edna Guide");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(controller.State.ConfirmationReviewed, Is.True);
            Assert.That(GameObject.Find("Sealed ROV Finding 1"), Is.Null);
            foreach (string id in new[] { "E07_FISHING_LINE", "E08_SEAFLOOR_INTACT" })
            {
                GameObject finding = GameObject.Find("Confirmation " + id);
                Assert.That(finding, Is.Not.Null);
                AssertInsideViewport(finding.GetComponent<RectTransform>(), page.viewport);
                CanvasGroup group = finding.GetComponent<CanvasGroup>();
                Assert.That(group == null || group.alpha > 0.99f, Is.True);
                foreach (Text text in finding.GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
            }
            Button next = FindButton("Re-test Fishing Models");
            Assert.That(next.transform.IsChildOf(GameObject.Find("ROV Confirmation").transform), Is.True);
            AssertInsideViewport(next.GetComponent<RectTransform>(), page.viewport);
            ClickThroughPointer(next);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            Assert.That(FindButton("Prediction sea_star").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_HelpControlsRemainVisibleAndReceivePointerInput()
        {
            yield return LoadInvestigationScene();
            Button help = FindButton("Increase Edna Hint");
            AssertGuideControlVisible(help);
            ClickThroughPointer(help);
            yield return null; // Rebuilt graphics receive their raycast depth on the next rendered frame.
            Assert.That(GameObject.Find("Edna Identity ObserveFirstFinding").GetComponent<Text>().text, Does.Contain("HINT 2/3"));
            Button minimise = FindButton("Toggle Edna Guide");
            AssertGuideControlVisible(minimise);
            ClickThroughPointer(minimise);
            yield return null;
            Assert.That(GameObject.Find("Edna Prompt Hidden"), Is.Not.Null);
            AssertGuideControlVisible(FindButton("Toggle Edna Guide"));
            ClickThroughPointer(FindButton("Toggle Edna Guide"));
            yield return null;
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            Click("Stage Simulate");
            yield return null;
            AssertGuideControlVisible(FindButton("Increase Edna Hint"));
            ClickThroughPointer(FindButton("Increase Edna Hint"));
            yield return null;
            ClickThroughPointer(FindButton("Toggle Edna Guide"));
            yield return null;
            Assert.That(GameObject.Find("Edna Prompt Hidden"), Is.Not.Null);
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            Click("Edit Report Cause");
            yield return null;
            AssertGuideControlVisible(FindButton("Increase Edna Hint"));
            ClickThroughPointer(FindButton("Increase Edna Hint"));
            Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Does.Contain("food-web pattern"));
        }

        private static void AssertGuideControlVisible(Button button)
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(button.targetGraphic.color.a, Is.GreaterThan(0.99f));
            Text text = button.GetComponentInChildren<Text>();
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(13));
            Assert.That(ContrastRatio(text.color, button.targetGraphic.color), Is.GreaterThanOrEqualTo(4.5f));
            AssertTextFitsItsRect(text);
        }

        private static void ClickThroughPointer(Button button)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform rect = button.GetComponent<RectTransform>();
            PointerEventData pointer = PointerAtWorldPoint(rect.TransformPoint(rect.rect.center));
            GameObject firstHit = FirstPointerHit(pointer);
            GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(firstHit);
            Assert.That(target, Is.EqualTo(button.gameObject),
                $"{button.name}: hit {firstHit?.name ?? "nothing"}, bounds {WorldRect(rect)}, graphic depth {button.targetGraphic.depth}, culled {button.targetGraphic.canvasRenderer.cull}");
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static void AssertInsideViewport(RectTransform rect, RectTransform viewport)
        {
            Rect bounds = WorldRect(rect);
            Rect visible = WorldRect(viewport);
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(visible.xMin - 1f), rect.name);
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(visible.xMax + 1f), rect.name);
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(visible.yMin - 1f), rect.name);
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(visible.yMax + 1f), rect.name);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReportReviewCanReviseTheCauseWithoutLosingOtherAnswers()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            Assert.That(GameObject.Find("Report Review"), Is.Not.Null);
            string reasoning = controller.State.SelectedReasoningId;
            string limitation = controller.State.SelectedLimitationId;
            var evidence = new System.Collections.Generic.List<string>(controller.State.SelectedReportEvidenceIds);
            Click("Edit Report Cause");
            AssertReportQuestion("Cause", 1);
            Click("Final Cause plastic");
            Assert.That(GameObject.Find("Report Review"), Is.Not.Null);
            Assert.That(GameObject.Find("Report Review Cause").transform.Find("Answer/Answer Text").GetComponent<Text>().text, Is.EqualTo("Plastic pollution"));
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
            Click("Submit Final Report");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Incorrect));
            AssertReportQuestion("Cause", 1);
            Assert.That(GameObject.Find("Report Section Feedback"), Is.Not.Null);
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.EqualTo(1));
            Click("Final Cause longline");
            Assert.That(GameObject.Find("Report Review"), Is.Not.Null);
            Assert.That(controller.State.SelectedReasoningId, Is.EqualTo(reasoning));
            Assert.That(controller.State.SelectedLimitationId, Is.EqualTo(limitation));
            CollectionAssert.AreEqual(evidence, controller.State.SelectedReportEvidenceIds);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
            Click("Submit Final Report");
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReportDialoguePreservesDraftAndReflowsInAShortNarrowCanvas()
        {
            yield return LoadReportQuestions();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            Canvas canvas = view.GetComponent<Canvas>();
            CanvasScaler scaler = view.GetComponent<CanvasScaler>();
            bool wasEnabled = scaler.enabled;
            float previousScale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false;
                canvas.scaleFactor = Screen.width / 720f;
                yield return null;
                yield return null;
                AssertReportQuestion("Cause", 1);
                Click("Final Cause longline");
                AssertReportQuestion("Reasoning", 2);
                Click("Previous Report Question");
                AssertReportQuestion("Cause", 1);
                Assert.That(FindButton("Final Cause longline").transform.Find("Selected Check"), Is.Not.Null);
                Click("Continue Report Dialogue");
                AssertReportQuestion("Reasoning", 2);

                RectTransform choices = GameObject.Find("Reasoning Choices").GetComponent<RectTransform>();
                Button first = choices.GetChild(0).GetComponent<Button>();
                Text label = first.GetComponentInChildren<Text>();
                Canvas.ForceUpdateCanvases();
                float beforeHeight = first.GetComponent<RectTransform>().rect.height;
                string original = label.text;
                label.text += " This longer scientific explanation checks that the answer grows with the available width and remains readable without smaller text. It must stay inside its button.";
                LayoutRebuilder.MarkLayoutForRebuild(choices);
                Canvas.ForceUpdateCanvases();
                Assert.That(first.GetComponent<RectTransform>().rect.height, Is.GreaterThan(beforeHeight));
                AssertTextFitsItsRect(label);
                label.text = original;
                LayoutRebuilder.MarkLayoutForRebuild(choices);
                Canvas.ForceUpdateCanvases();

                Click("Continue Report Dialogue");
                yield return null;
                yield return null;
                Text diagnostic = GameObject.Find("Report Section Feedback").GetComponent<Text>();
                Assert.That(diagnostic.text, Does.Contain("food-web cascade"));
                float diagnosticHeight = diagnostic.rectTransform.rect.height;
                diagnostic.text += " Re-check every part of the food-web explanation against your findings. Keep the observations, model prediction and limits of the survey distinct before continuing to the next question.";
                LayoutRebuilder.MarkLayoutForRebuild(diagnostic.rectTransform);
                Canvas.ForceUpdateCanvases();
                Assert.That(diagnostic.rectTransform.rect.height, Is.GreaterThan(diagnosticHeight));
                AssertTextFitsItsRect(diagnostic);
                Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
                Click("Reasoning food_web_cascade");
                Click("Report Evidence E01_SHARK_NONDETECTION");
                Click("Report Evidence E02_TUNA_WIDER_DETECTION");
                string reportPrompt = GameObject.Find("Edna Prompt").GetComponent<Text>().text;
                Click("Stage Observe");
                Click("Increase Edna Hint");
                Click("Increase Edna Hint");
                Click("Stage Report");
                yield return null;
                yield return null;
                AssertReportQuestion("Evidence", 3);
                Assert.That(GameObject.Find("Edna Prompt").GetComponent<Text>().text, Is.EqualTo(reportPrompt),
                    "Help requested in another stage must not change this report question's help level.");
                Assert.That(controller.State.FinalThreatId, Is.EqualTo("longline"));
                Assert.That(controller.State.SelectedReasoningId, Is.EqualTo("food_web_cascade"));
                Assert.That(controller.State.SelectedReportEvidenceIds, Has.Count.EqualTo(2));
                AssertChildrenStayInsideLayout("Evidence Choices");
                ScrollRect scroll = GameObject.Find("Investigation Content").GetComponent<ScrollRect>();
                Assert.That(scroll.content.rect.width, Is.LessThanOrEqualTo(scroll.viewport.rect.width + 1f));
                foreach (Text text in GameObject.Find("Survey Report Paper").GetComponentsInChildren<Text>())
                    if (text.name != "Focus Ring") AssertTextFitsItsRect(text);
            }
            finally
            {
                canvas.scaleFactor = previousScale;
                scaler.enabled = wasEnabled;
                InvestigationSessionBridge.Clear();
            }
        }

        private static IEnumerator LoadReportQuestions()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
            Click("Review ROV Follow-up");
            Click("Re-test Fishing Models");
            Click("Threat bottom_trawling");
            Compare("sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch);
            Click("Threat longline");
            Compare("sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match);
            Click("Return To Final Report");
            yield return null;
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReportDiagnosticsRemainLocalInHardMode()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            Click("Difficulty Toggle");
            Click("Edit Report Evidence");
            Click("Report Evidence E07_FISHING_LINE");
            Click("Continue Report Dialogue");
            yield return null;
            Transform evidence = FindGameObject("Report Evidence Section").transform;
            Text feedback = evidence.Find("Report Section Feedback").GetComponent<Text>();
            string message = feedback.text;
            Assert.That(message, Does.Contain("Select at least 4"));
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(evidence), Is.True);
            Assert.That(controller.State.FinalThreatId, Is.EqualTo("longline"));
            Assert.That(controller.State.SelectedReasoningId, Is.EqualTo("food_web_cascade"));
            Click("Difficulty Toggle");
            Assert.That(FindGameObject("Report Section Feedback").GetComponent<Text>().text, Is.EqualTo(message));
            Click("Report Evidence E07_FISHING_LINE");
            Assert.That(GameObject.Find("Report Section Feedback"), Is.Null);
            Click("Continue Report Dialogue");
            Click("Submit Final Report");
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ProvisionalKeyboardFocusScrollsIntoAShortViewport()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            Click("Write Provisional Report");
            yield return null;
            RectTransform overlay = FindGameObject("Provisional Review Overlay").GetComponent<RectTransform>();
            overlay.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 220f);
            Canvas.ForceUpdateCanvases();
            Button confirm = FindButton("Confirm Provisional Idea");
            EventSystem.current.SetSelectedGameObject(confirm.gameObject);
            yield return null;
            yield return null;
            Rect viewport = WorldRect(overlay);
            Rect control = WorldRect(confirm.GetComponent<RectTransform>());
            Assert.That(control.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1f));
            Assert.That(control.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1f));
            Assert.That(controller.State.ProvisionalThreatId, Is.Empty);
            Click("Cancel Provisional Idea");
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ClosingStampSettlesAndRespectsReducedMotion()
        {
            bool previous = InvestigationMotionSettings.ReducedMotion;
            try
            {
                InvestigationMotionSettings.SetReducedMotion(false);
                yield return LoadInvestigationScene();
                InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
                Click("Submit Final Report");
                yield return new WaitForSecondsRealtime(0.4f);
                RectTransform stamp = FindGameObject("Case Closed Stamp").GetComponent<RectTransform>();
                Assert.That(stamp.localScale.x, Is.EqualTo(1f).Within(0.001f));
                Assert.That(stamp.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.001f));
                Assert.That(FindGameObject("Stamp Title").GetComponent<Text>().text, Is.EqualTo("CASE CLOSED"));
                Assert.That(FindButton("Stage Report").transform.Find("Stage Complete"), Is.Not.Null);
                InvestigationMotionSettings.SetReducedMotion(true);
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
                Click("Submit Final Report");
                stamp = FindGameObject("Case Closed Stamp").GetComponent<RectTransform>();
                Assert.That(stamp.localScale, Is.EqualTo(Vector3.one));
                Quaternion pose = stamp.localRotation;
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(stamp.localRotation, Is.EqualTo(pose));
            }
            finally { InvestigationMotionSettings.SetReducedMotion(previous); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReportSubmitReceivesPointerHits()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Button submit = FindButton("Submit Final Report");
            RectTransform rect = submit.GetComponent<RectTransform>();
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, "The visible report action must receive pointer input.");
            GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(target, Is.EqualTo(submit.gameObject), $"Pointer was intercepted by {hits[0].gameObject.name}.");
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_RepeatedSameFrameRefreshesKeepOneClosableNotebook()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);

            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Intentionally do not yield between these actions: Destroy has not run yet.
                Click("Toggle Notebook Drawer");
                Click("Difficulty Toggle");
                Click("Difficulty Toggle");
                AssertActiveNotebookCount(view, 1);
                Click("Close Notebook Drawer");
                AssertActiveNotebookCount(view, 0);
            }
            yield return null;
            AssertActiveNotebookCount(view, 0);
            Assert.That(GameObject.Find("Page Content").GetComponent<CanvasGroup>().blocksRaycasts, Is.True);

            Click("Toggle Notebook Drawer");
            yield return null;
            AssertActiveNotebookCount(view, 1);
            Click("Close Notebook Drawer");
            yield return null;
            AssertActiveNotebookCount(view, 0);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_NotebookPaperInterceptsClicksAndOutsideScrimClosesIt()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            Click("Toggle Notebook Drawer");
            yield return new WaitForSecondsRealtime(0.25f);
            Canvas.ForceUpdateCanvases();

            GameObject drawer = GameObject.Find("Notebook Drawer");
            foreach (string region in new[] { "Notebook Drawer Title", "Notebook Binding Margin", "Notebook Binding Ring 2" })
            {
                RectTransform rect = GameObject.Find(region).GetComponent<RectTransform>();
                PointerEventData pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                    button = PointerEventData.InputButton.Left
                };
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits, Is.Not.Empty, region);
                Assert.That(hits[0].gameObject, Is.EqualTo(drawer), $"{region} must intercept clicks on the paper.");
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(drawer.activeInHierarchy, Is.True, $"Clicking {region} must leave the notebook open.");
            }

            RectTransform scrim = FindButton("Notebook Drawer Scrim").GetComponent<RectTransform>();
            // Upper left is outside the drawer in both landscape and portrait layouts.
            Vector2 outside = new Vector2(scrim.rect.xMin + 4f, scrim.rect.yMax - 4f);
            PointerEventData outsidePointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, scrim.TransformPoint(outside)),
                button = PointerEventData.InputButton.Left
            };
            var outsideHits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(outsidePointer, outsideHits);
            Assert.That(outsideHits, Is.Not.Empty);
            GameObject outsideTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(outsideHits[0].gameObject);
            Assert.That(outsideTarget, Is.EqualTo(scrim.gameObject));
            ExecuteEvents.Execute(outsideTarget, outsidePointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(GameObject.Find("Notebook Drawer"), Is.Null);
            Assert.That(GameObject.Find("Notebook Drawer Scrim"), Is.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_FailedImportStaysBlockedUntilExplicitStandaloneRecovery()
        {
            bool previousMotion = InvestigationMotionSettings.ReducedMotion;
            InvestigationGameInput invalidInput = new InvestigationGameInput { caseId = "wrong_case" };
            try
            {
                InvestigationSessionBridge.Clear();
                yield return LoadInvestigationScene();
                InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
                InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
                Click("Toggle Notebook Drawer");
                InvestigationSessionBridge.SetInput(invalidInput);
                controller.SendMessage("HandleRestart", SendMessageOptions.RequireReceiver);
                yield return null;

                GameObject error = GameObject.Find("Fatal Error");
                Assert.That(error, Is.Not.Null);
                Assert.That(error.GetComponent<Text>().text, Does.Contain("wrong_case"));
                Assert.That(controller.State, Is.Null);
                Assert.That(view.State, Is.Null);
                AssertActiveNotebookCount(view, 0);
                Assert.That(FindButton("Difficulty Toggle").interactable, Is.False);
                for (int toggle = 0; toggle < 2; toggle++)
                {
                    bool expectedMotion = !InvestigationMotionSettings.ReducedMotion;
                    Click("Motion Toggle");
                    Assert.That(InvestigationMotionSettings.ReducedMotion, Is.EqualTo(expectedMotion));
                    Assert.That(FindButton("Motion Toggle").GetComponentInChildren<Text>().text,
                        Is.EqualTo(expectedMotion ? "Motion: Reduced" : "Motion: Full"));
                    Assert.That(GameObject.Find("Fatal Error"), Is.SameAs(error));
                    Assert.That(controller.State, Is.Null);
                    Assert.That(view.State, Is.Null);
                }

                // Stale gameplay callbacks and development shortcuts must obey the same session boundary.
                controller.SendMessage("HandleSetPhase", InvestigationPhase.Simulate, SendMessageOptions.RequireReceiver);
                controller.SendMessage("HandleSetDifficulty", InvestigationDifficulty.Hard, SendMessageOptions.RequireReceiver);
                controller.SendMessage("HandleSubmitFinal", SendMessageOptions.RequireReceiver);
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
                yield return null;
                Assert.That(GameObject.Find("Fatal Error"), Is.SameAs(error));
                Assert.That(controller.State, Is.Null);
                Assert.That(view.State, Is.Null);
                Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
                Assert.That(InvestigationSessionBridge.PendingInput, Is.SameAs(invalidInput));

                Click("Start Standalone Case");
                yield return null;
                Assert.That(GameObject.Find("Fatal Error"), Is.Null);
                Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
                Assert.That(view.State, Is.SameAs(controller.State));
                Assert.That(FindButton("Difficulty Toggle").interactable, Is.True);
                Click("Motion Toggle");
                Assert.That(view.State, Is.SameAs(controller.State));
                Assert.That(InvestigationSessionBridge.PendingInput, Is.SameAs(invalidInput));
            }
            finally
            {
                InvestigationMotionSettings.SetReducedMotion(previousMotion);
                InvestigationSessionBridge.Clear();
            }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_DefaultBuildEntryStartsPlayableInvestigation()
        {
            InvestigationSessionBridge.Clear();
            Assert.That(SceneUtility.GetScenePathByBuildIndex(0), Is.EqualTo("Assets/Scenes/InvestigationScene.unity"));
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            yield return null;
            yield return null;
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(FindButton("Species Marker shark"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_EvidenceSelectionChecksImmediatelyWithKeyboardAndPointer()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            Click("Stage Simulate");
            Click("Threat longline");
            Click("Run Selected Model");
            Click("Prediction shark");
            Button finding = FindButton("Observation E01_SHARK_NONDETECTION");
            EventSystem.current.SetSelectedGameObject(finding.gameObject);
            BaseEventData submit = new BaseEventData(EventSystem.current);
            ExecuteEvents.Execute(finding.gameObject, submit, ExecuteEvents.submitHandler);
            ExecuteEvents.Execute(finding.gameObject, submit, ExecuteEvents.submitHandler);
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(1));
            Assert.That(controller.State.FindComparison("longline", "shark").Judgement, Is.EqualTo(ComparisonJudgement.Match));
            Assert.That(FindGameObject("Comparison Saved Text").GetComponent<Text>().text, Does.Contain("supports the model"));
            yield return null;
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Prediction shark"),
                "After saving evidence, keyboard focus should return to the prediction controls.");

            Click("Threat plastic");
            Click("Run Selected Model");
            Click("Prediction mussel");
            yield return null;
            Canvas.ForceUpdateCanvases();
            finding = FindButton("Observation E06_PLASTIC_INDICATOR_STABLE");
            RectTransform rect = finding.GetComponent<RectTransform>();
            PointerEventData pointer = PointerAtWorldPoint(rect.TransformPoint(rect.rect.center));
            GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(FirstPointerHit(pointer));
            Assert.That(target, Is.EqualTo(finding.gameObject));
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(controller.State.FindComparison("plastic", "mussel").Judgement, Is.EqualTo(ComparisonJudgement.Mismatch));
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(2));
            Assert.That(FindGameObject("Comparison Saved Text").GetComponent<Text>().text, Does.Contain("challenges the model"));
            Assert.That(FindButton("Judge Match"), Is.Null);
            Assert.That(FindButton("Judge Mismatch"), Is.Null);
            Assert.That(FindButton("Judge NotEnoughEvidence"), Is.Null);
            Assert.That(GameObject.Find("Judgement Row"), Is.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReportNavigationOpensTheRequiredProvisionalChoice()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Click("Stage Report");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(FindButton("Confirm Provisional Idea"), Is.Null);

            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            Click("Stage Observe");
            Click("Stage Report");
            yield return null;
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            Assert.That(controller.State.ProvisionalThreatId, Is.Empty);
            Assert.That(FindButton("Confirm Provisional Idea"), Is.Not.Null);
            Assert.That(FindButton("Review ROV Follow-up"), Is.Null);
            Click("Cancel Provisional Idea");
            Assert.That(controller.State.ProvisionalThreatId, Is.Empty);

            Click("Toggle Notebook Drawer");
            Click("Stage Report");
            AssertActiveNotebookCount(Object.FindAnyObjectByType<InvestigationRuntimeView>(), 0);
            Click("Provisional Cause longline");
            Click("Confirm Provisional Idea");
            yield return null;
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo("longline"));
            Click("Review ROV Follow-up");
            Assert.That(controller.State.ConfirmationReviewed, Is.True);

            Click("Stage Simulate");
            Click("Stage Report");
            yield return null;
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(controller.State.ProvisionalThreatId, Is.EqualTo("longline"));
            Assert.That(GameObject.Find("Provisional Review Overlay"), Is.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_PinnedSpeciesFactsBlockCoveredMarkersAfterDelayedHover()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Button historicalTuna = FindButton("Historical Species Marker tuna");
            PointerEventData tap = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            historicalTuna.GetComponent<InvestigationHoverTooltipTrigger>().OnPointerEnter(tap);
            ExecuteEvents.Execute(historicalTuna.gameObject, tap, ExecuteEvents.pointerClickHandler);
            GameObject pinnedCard = GameObject.Find("Species Facts Tooltip");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.SameAs(pinnedCard),
                "The delayed hover must not replace a card that was pinned by a tap.");

            RectTransform shark = FindButton("Species Marker shark").GetComponent<RectTransform>();
            Vector3 coveredPoint = shark.TransformPoint(shark.rect.center);
            RectTransform cardRect = pinnedCard.GetComponent<RectTransform>();
            // Keep a marker under the card regardless of the map's small random placement offsets.
            cardRect.position += coveredPoint - cardRect.TransformPoint(cardRect.rect.center);
            Canvas.ForceUpdateCanvases();
            PointerEventData pointer = PointerAtWorldPoint(coveredPoint);
            GameObject hit = FirstPointerHit(pointer);
            Assert.That(hit, Is.EqualTo(pinnedCard));
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);

            RectTransform close = FindButton("Close Species Facts").GetComponent<RectTransform>();
            PointerEventData closePointer = PointerAtWorldPoint(close.TransformPoint(close.rect.center));
            ExecuteEvents.ExecuteHierarchy(FirstPointerHit(closePointer), closePointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            ExecuteEvents.ExecuteHierarchy(FirstPointerHit(pointer), pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(controller.State.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_NotebookScrollsIndependentlyAndRestoresBackgroundScrolling()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            Click("Edit Report Evidence");
            Click("Report Evidence E03_KRILL_NONDETECTION");
            Click("Report Evidence E06_PLASTIC_INDICATOR_STABLE");
            Click("Report Evidence E08_SEAFLOOR_INTACT");
            Click("Continue Report Dialogue");
            yield return null;
            Canvas.ForceUpdateCanvases();
            ScrollRect page = GameObject.Find("Investigation Content").GetComponent<ScrollRect>();
            // A compact review may fit a large screen. Use a short viewport to
            // exercise scroll isolation and restoration independently of that layout.
            page.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            page.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 180f);
            Canvas.ForceUpdateCanvases();
            Assert.That(page.content.rect.height, Is.GreaterThan(page.viewport.rect.height + 1f));
            page.verticalNormalizedPosition = 0.65f;
            float savedPagePosition = page.verticalNormalizedPosition;
            Click("Toggle Notebook Drawer");
            yield return new WaitForSecondsRealtime(0.3f);
            Canvas.ForceUpdateCanvases();
            RectTransform title = GameObject.Find("Notebook Drawer Title").GetComponent<RectTransform>();
            PointerEventData pointer = PointerAtWorldPoint(title.TransformPoint(title.rect.center));
            pointer.scrollDelta = new Vector2(0f, -8f);
            GameObject paper = FirstPointerHit(pointer);
            ExecuteEvents.ExecuteHierarchy(paper, pointer, ExecuteEvents.scrollHandler);
            ExecuteEvents.ExecuteHierarchy(paper, pointer, ExecuteEvents.beginDragHandler);
            pointer.position += new Vector2(0f, 120f);
            ExecuteEvents.ExecuteHierarchy(paper, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.ExecuteHierarchy(paper, pointer, ExecuteEvents.endDragHandler);
            Assert.That(page.verticalNormalizedPosition, Is.EqualTo(savedPagePosition).Within(0.001f));

            ScrollRect notebook = GameObject.Find("Notebook Drawer Scroll").GetComponent<ScrollRect>();
            notebook.verticalNormalizedPosition = 1f;
            PointerEventData notebookPointer = PointerAtWorldPoint(notebook.viewport.TransformPoint(notebook.viewport.rect.center));
            notebookPointer.scrollDelta = new Vector2(0f, -8f);
            ExecuteEvents.ExecuteHierarchy(FirstPointerHit(notebookPointer), notebookPointer, ExecuteEvents.scrollHandler);
            Assert.That(notebook.verticalNormalizedPosition, Is.LessThan(1f));
            Assert.That(page.verticalNormalizedPosition, Is.EqualTo(savedPagePosition).Within(0.001f));
            Click("Difficulty Toggle");
            Click("Difficulty Toggle");
            Click("Close Notebook Drawer");
            yield return null;
            Assert.That(page.verticalNormalizedPosition, Is.EqualTo(savedPagePosition).Within(0.001f));
            ExecuteEvents.Execute(page.gameObject, notebookPointer, ExecuteEvents.scrollHandler);
            Assert.That(page.verticalNormalizedPosition, Is.LessThan(savedPagePosition),
                "Closing the notebook must restore normal page scrolling.");
        }

        private static PointerEventData PointerAtWorldPoint(Vector3 point)
        {
            return new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, point),
                button = PointerEventData.InputButton.Left
            };
        }

        private static GameObject FirstPointerHit(PointerEventData pointer)
        {
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            return hits[0].gameObject;
        }

        private static void AssertActiveNotebookCount(InvestigationRuntimeView view, int expected)
        {
            int drawers = 0;
            int scrims = 0;
            foreach (Transform child in view.GetComponentsInChildren<Transform>())
            {
                if (child.name == "Notebook Drawer") drawers++;
                if (child.name == "Notebook Drawer Scrim") scrims++;
            }
            Assert.That(drawers, Is.EqualTo(expected), "Unexpected number of active notebook drawers.");
            Assert.That(scrims, Is.EqualTo(expected), "Each active drawer must have exactly one scrim.");
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator InvestigationScene_SixthCoreFindingUpdatesAllObserveProgressAndGates()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            InvestigationCaseDefinition definition = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<InvestigationCaseDefinition>(
                "Assets/Data/Investigation/LongLineCase/InvestigationCase_LongLine.asset"));
            try
            {
                UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(definition);
                UnityEditor.SerializedProperty observations = serialized.FindProperty("observations");
                int addedIndex = observations.arraySize++;
                UnityEditor.SerializedProperty added = observations.GetArrayElementAtIndex(addedIndex);
                added.FindPropertyRelative("evidenceId").stringValue = "E_TEST_SIXTH_FINDING";
                added.FindPropertyRelative("displayName").stringValue = "Additional survey reading";
                added.FindPropertyRelative("relatedSpeciesId").stringValue = string.Empty;
                added.FindPropertyRelative("source").enumValueIndex = (int)ObservationSource.CTDLog;
                added.FindPropertyRelative("unlockStage").enumValueIndex = (int)EvidenceUnlockStage.Observe;
                added.FindPropertyRelative("claimType").enumValueIndex = (int)ObservationClaimType.EnvironmentalReading;
                added.FindPropertyRelative("category").enumValueIndex = (int)EvidenceCategory.Environmental;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(definition.MinimumObserveDiscoveries, Is.EqualTo(5));
                Assert.That(InvestigationObserveEvaluator.RequiredCount(definition), Is.EqualTo(6));

                controller.Initialize(definition, view);
                InvestigationStateUpdater updater = new InvestigationStateUpdater(definition);
                InvestigationState state = controller.State;
                foreach (string id in new[] { "E01_SHARK_NONDETECTION", "E02_TUNA_WIDER_DETECTION", "E03_KRILL_NONDETECTION",
                    "E04_BENTHIC_STABLE", "E06_PLASTIC_INDICATOR_STABLE" })
                    Assert.That(updater.TryDiscoverObservation(state, id, out _), Is.True);
                view.Refresh(state, string.Empty, InvestigationStatusTone.Guide);

                Assert.That(FindGameObject("Observe Finding Progress Text").GetComponent<Text>().text, Does.Contain("FINDINGS 5 / 6"));
                Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("FINDINGS 5/6"));
                Assert.That(FindGameObject("Edna Prompt").GetComponent<Text>().text, Does.Contain("1 more finding"));
                Assert.That(FindButton("Continue To Simulate"), Is.Null);
                Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out string phaseFeedback), Is.False);
                Assert.That(phaseFeedback, Does.Contain("at least 6 observations"));
                Assert.That(updater.TryRunThreat(state, "longline", out _, out string modelFeedback), Is.False);
                Assert.That(modelFeedback, Does.Contain("at least 6 observations"));

                Assert.That(updater.TryDiscoverObservation(state, "E_TEST_SIXTH_FINDING", out _), Is.True);
                view.Refresh(state, string.Empty, InvestigationStatusTone.Guide);
                Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("FINDINGS 6/6"));
                Assert.That(FindButton("Continue To Simulate"), Is.Not.Null);
                Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out _), Is.True);
                Assert.That(updater.TryRunThreat(state, "longline", out _, out _), Is.True);
            }
            finally
            {
                if (controller != null) Object.Destroy(controller.gameObject);
                Object.Destroy(definition);
                InvestigationSessionBridge.Clear();
            }
        }
#endif

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

        private static void AssertReportQuestion(string section, int number)
        {
            Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain($"QUESTION {number} OF 4"));
            foreach (string candidate in new[] { "Cause", "Reasoning", "Evidence", "Limitation" })
                Assert.That(GameObject.Find($"Report {candidate} Section") != null, Is.EqualTo(candidate == section), candidate);
            Assert.That(GameObject.Find("Report Columns"), Is.Null);
            Assert.That(GameObject.Find("Report Review"), Is.Null);
            Assert.That(FindButton("Submit Final Report"), Is.Null);
        }

        private static void Compare(string speciesId, string evidenceId, ComparisonJudgement expectedRelationship)
        {
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            int before = controller.State.CompletedObjectiveCount;
            Click($"Prediction {speciesId}");
            Click($"Observation {evidenceId}");
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(before + 1),
                "Choosing the relevant finding must complete the comparison immediately.");
            Assert.That(FindGameObject("Comparison Saved Text").GetComponent<Text>().text,
                Does.Contain(expectedRelationship == ComparisonJudgement.Match ? "supports the model" : "challenges the model"));
            Assert.That(GameObject.Find("Judgement Row"), Is.Null);
        }

        private static void Click(string buttonName)
        {
            Button button = FindButton(buttonName);
            Assert.That(button, Is.Not.Null, $"Button not found: {buttonName}");
            Assert.That(button.interactable, Is.True, $"Button is not interactable: {buttonName}");
            button.onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator InvestigationScene_WaterLayersStayBehindTheInterfaceAndPassInput()
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
            Assert.That(background.Find("Marine Snow Large").GetSiblingIndex(), Is.LessThan(safeArea),
                "Water particles must stay behind reading and interaction surfaces.");

            // Every water layer is decoration: none of it may swallow a click.
            string[] waterLayers =
            {
                "Water Column", "God Rays", "Marine Snow Far", "Marine Snow",
                "Water Vignette", "Marine Snow Large",
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
                "God Rays", "Marine Snow Far", "Marine Snow", "Marine Snow Large",
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
            Transform descriptionTransform = block.transform.Find("Description") ?? block.transform.Find("Edna Prompt");
            Assert.That(descriptionTransform, Is.Not.Null);
            RectTransform description = descriptionTransform.GetComponent<RectTransform>();
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
            RectTransform description = FindGameObject("Edna Prompt").GetComponent<RectTransform>();
            RectTransform questionGrid = FindGameObject("Case Question Grid").GetComponent<RectTransform>();
            Assert.That(leftColumn.parent, Is.EqualTo(workspace));
            Assert.That(rightColumn.parent, Is.EqualTo(workspace));
            Assert.That(introduction.parent, Is.EqualTo(leftColumn));
            Assert.That(questions.parent, Is.EqualTo(rightColumn));
            Assert.That(models.parent, Is.EqualTo(leftColumn));
            Assert.That(comparison.parent, Is.EqualTo(rightColumn));
            Assert.That(leftColumn.position.x, Is.LessThan(rightColumn.position.x));
            Assert.That(Mathf.Abs(leftColumn.rect.width - rightColumn.rect.width), Is.LessThan(3f));
            Assert.That(introduction.rect.height, Is.EqualTo(96f).Within(0.5f));
            AssertTextFitsItsRect(description.GetComponent<Text>());
            Assert.That(FindGameObject("Simulate Heading Divider").GetComponent<RectTransform>().rect.width, Is.EqualTo(2f).Within(0.1f));
            Assert.That(questions.rect.height, Is.EqualTo(66f).Within(0.5f));
            Assert.That(WorldRect(introduction).yMax, Is.EqualTo(WorldRect(questions).yMax).Within(1f));
            Assert.That(WorldRect(models).yMax, Is.LessThan(WorldRect(introduction).yMin));
            Assert.That(WorldRect(comparison).yMax, Is.LessThan(WorldRect(questions).yMin));
            Assert.That(title.rect.width, Is.LessThanOrEqualTo(introduction.rect.width + 0.5f));
            Assert.That(description.rect.width, Is.LessThanOrEqualTo(introduction.rect.width + 0.5f));
            Assert.That(questionGrid.parent, Is.EqualTo(questions));
            Assert.That(questionGrid.childCount, Is.EqualTo(1));
            Assert.That(questionGrid.GetComponent<InvestigationResponsiveGridLayout>(), Is.Not.Null);
            Rect firstQuestion = WorldRect(questionGrid.GetChild(0).GetComponent<RectTransform>());
            Assert.That(firstQuestion.width, Is.GreaterThan(WorldRect(questionGrid).width * 0.9f));
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
            Assert.That(outline.effectDistance, Is.EqualTo(new Vector2(1f, -1f)));
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

        private static Rect WorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void AssertMarkerHabitat(string buttonName, RawImage mountain, Texture2D mask, bool expectRock)
        {
            RectTransform artwork = FindButton(buttonName).transform.Find("Species Artwork").GetComponent<RectTransform>();
            Vector3 local = mountain.rectTransform.InverseTransformPoint(artwork.TransformPoint(artwork.rect.center));
            Rect bounds = mountain.rectTransform.rect;
            float coverage = bounds.Contains(local)
                ? mask.GetPixelBilinear(Mathf.InverseLerp(bounds.xMin, bounds.xMax, local.x),
                    Mathf.InverseLerp(bounds.yMin, bounds.yMax, local.y)).r : 0f;
            if (expectRock) Assert.That(coverage, Is.GreaterThan(.20f), buttonName + " should sit on the terrain.");
            else Assert.That(coverage, Is.LessThan(.08f), buttonName + " should sit in open water.");
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
