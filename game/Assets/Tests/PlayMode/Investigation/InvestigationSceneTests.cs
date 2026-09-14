using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;
using static EDNA.Investigation.Tests.InvestigationWorkbenchTestActions;
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


        [UnityTest]
        public IEnumerator InvestigationScene_OnlyDifficultyIsExposedAndLegacyReducedMotionIsIgnored()
        {
            const string legacyKey = "EDNA.Investigation.ReducedMotion";
            bool hadPreference = PlayerPrefs.HasKey(legacyKey);
            int previousPreference = PlayerPrefs.GetInt(legacyKey, 0);
            bool previousOverride = InvestigationMotionSettings.ReducedMotion;
            try
            {
                PlayerPrefs.SetInt(legacyKey, 1);
                InvestigationMotionSettings.SetReducedMotionForTests(false);
                InvestigationSessionBridge.Clear();
                yield return LoadInvestigationScene();
                InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
                foreach (InvestigationQaCheckpoint checkpoint in new[] { InvestigationQaCheckpoint.ObserveReady, InvestigationQaCheckpoint.SimulateStart, InvestigationQaCheckpoint.ConclusionReady })
                {
                    controller.ApplyQaCheckpoint(checkpoint);
                    yield return null;
                    Assert.That(FindButton("Motion Toggle"), Is.Null);
                    Assert.That(InvestigationMotionSettings.ReducedMotion, Is.False, "The old saved Reduced preference must be ignored.");
                    InvestigationDifficulty before = controller.State.Difficulty;
                    InvestigationCurrentFlowTestActions.ToggleGuidanceForTests();
                    Assert.That(controller.State.Difficulty, Is.Not.EqualTo(before));
                    Assert.That(InvestigationMotionSettings.ReducedMotion, Is.False);
                    Assert.That(FindButton("Difficulty Toggle"), Is.Null);
                    Assert.That(FindButton("Restart Case").interactable, Is.True);
                }
                controller.SendMessage("HandleRestart", SendMessageOptions.RequireReceiver);
                Assert.That(InvestigationMotionSettings.ReducedMotion, Is.False);
                Assert.That(FindButton("Motion Toggle"), Is.Null);
            }
            finally
            {
                if (hadPreference) PlayerPrefs.SetInt(legacyKey, previousPreference);
                else PlayerPrefs.DeleteKey(legacyKey);
                InvestigationMotionSettings.SetReducedMotionForTests(previousOverride);
                InvestigationSessionBridge.Clear();
            }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_HistoryHandleCueStopsAfterUse()
        {
            bool previous = InvestigationMotionSettings.ReducedMotion;
            try
            {
                InvestigationMotionSettings.SetReducedMotionForTests(false);
                yield return LoadCurrent(); InvestigationWorkbenchTestActions.BeginTodayRecording();
                CurrentPress("Skip Today Recording Animation"); CurrentPress("Compare With History");
                CurrentPress("History Lens Briefing Next"); yield return null; yield return null;
                var cue = GameObject.Find("Survey Lens Handle Cue").GetComponent<CanvasGroup>();
                Assert.That(cue.blocksRaycasts, Is.False);
                float before = cue.alpha; bool changed = false;
                for (int i = 0; i < 12 && !changed; i++) { yield return new WaitForSecondsRealtime(.1f); changed = Mathf.Abs(cue.alpha - before) > .1f; }
                Assert.That(changed, Is.True);
                InvestigationMotionSettings.SetReducedMotionForTests(true); yield return null; yield return null;
                Assert.That(GameObject.Find("Survey Lens Handle Cue").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(.01f));
                GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = 1f;
                yield return null; Assert.That(GameObject.Find("Survey Lens Handle Cue"), Is.Null);
            }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(previous); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_QaCheckpointsReachTheTwoActConclusionWithoutRov()
        {
            yield return LoadCurrent();
            foreach (InvestigationQaCheckpoint checkpoint in Enum.GetValues(typeof(InvestigationQaCheckpoint)))
            {
                CurrentController.ApplyQaCheckpoint(checkpoint); yield return null;
                Assert.That(GameObject.Find("Fatal Error"), Is.Null, checkpoint.ToString());
            }
            CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(5));
            CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete); yield return null; yield return null;
            Assert.That(GameObject.Find("Scenario Results"), Is.Not.Null);
            Assert.That(CurrentState.CompletedObjectiveCount, Is.EqualTo(7));
            CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.ConclusionReady); yield return null;
            Assert.That(CurrentButton("Complete Scenario Investigation").interactable, Is.True);
            Assert.That(CurrentState.ConfirmationReviewed, Is.False);
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.EqualTo(5));
            CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.CaseClosed); yield return null;
            Assert.That(InvestigationSessionBridge.LastResult.completed, Is.True);
            Assert.That(InvestigationSessionBridge.LastResult.evidenceIds.Count, Is.EqualTo(5));
            Assert.That(GameObject.Find("Stage Report"), Is.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_FirstAnswerRevealsNotebookAndImportedFindingsSkipAnsweredQuestions()
        {
            InvestigationSessionBridge.Clear();
            try
            {
                yield return LoadInvestigationScene();
                Click("Toggle Survey Map"); yield return null;
                Assert.That(FindButton("Toggle Comparison View"), Is.Not.Null);
                ClickThroughPointer(FindButton("Historical Species Marker shark"));
                yield return null;
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Is.Empty, "Reading facts cannot record a finding.");
                Record("Species Marker shark"); yield return null;

                Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null);
                Assert.That(GameObject.Find("Historical Notebook Row shark"), Is.Not.Null);
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);

                Assert.That(FindButton("Compare Species tuna").interactable, Is.True);
                InvestigationGameInput input = new InvestigationGameInput();
                input.discoveredObservationIds.Add("E03_HERRING_FEWER_SITES");
                InvestigationSessionBridge.SetInput(input); yield return LoadInvestigationScene();
                Assert.That(FindButton("Compare Species shark").interactable, Is.True);

                Assert.That(GameObject.Find("Historical Notebook Row atlantic_herring"), Is.Not.Null);

                Record("Species Marker shark"); Record("Species Marker tuna"); Record("Species Marker krill");
                Assert.That(FindButton("Compare Species phytoplankton").interactable, Is.True);
            }
            finally { InvestigationSessionBridge.Clear(); }
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
            InvestigationMotionSettings.SetReducedMotionForTests(false);
            try
            {
                yield return LoadInvestigationScene();
                Click("Toggle Survey Map"); yield return null;
                InvestigationSeamountBackdrop owner = Object.FindAnyObjectByType<InvestigationSeamountBackdrop>();
                Assert.That(owner.RenderMaterial, Is.Not.Null);
                Material shared = owner.RenderMaterial;
                AssertSharedSeamountMaterial(shared);
                yield return new WaitForSecondsRealtime(1.3f);
                double beforeRefresh = owner.ElapsedSeconds;
                Record("Species Marker shark");
                Click("Toggle Survey Map"); yield return null;
                Assert.That(Object.FindAnyObjectByType<InvestigationSeamountBackdrop>(), Is.SameAs(owner));
                Assert.That(owner.ElapsedSeconds, Is.GreaterThanOrEqualTo(beforeRefresh));
                AssertSharedSeamountMaterial(shared);
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Has.Count.EqualTo(1));

                double beforePause = owner.ElapsedSeconds;
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(owner.ElapsedSeconds - beforePause, Is.GreaterThan(.15d));
                Time.timeScale = previousTimeScale;

                InvestigationMotionSettings.SetReducedMotionForTests(!InvestigationMotionSettings.ReducedMotion);
                yield return null;
                yield return null;
                Assert.That(owner.BlendWeights, Is.EqualTo(Vector2.zero));
                AssertSharedSeamountMaterial(shared);
                InvestigationMotionSettings.SetReducedMotionForTests(!InvestigationMotionSettings.ReducedMotion);
                yield return null;
                yield return null;
                Assert.That(owner.ElapsedSeconds, Is.GreaterThan(beforePause));
                Assert.That(Vector2.Distance(owner.BlendWeights,
                    InvestigationSeamountBackdrop.EvaluateBlends(owner.ElapsedSeconds, false)), Is.LessThan(.05f));

                yield return LoadInvestigationScene();
                Click("Toggle Survey Map"); yield return null;
                Assert.That(owner == null, Is.True);
                Assert.That(shared == null, Is.True, "The old view must release its runtime material on scene reload.");
                AssertSharedSeamountMaterial(Object.FindAnyObjectByType<InvestigationSeamountBackdrop>().RenderMaterial);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                InvestigationMotionSettings.SetReducedMotionForTests(previousMotion);
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
                caseId = "investigation_foodchain_02",
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
            InvestigationGameInput input = new InvestigationGameInput { caseId = "investigation_foodchain_02" };
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
                Text fallback = marker.transform.Find("Species Artwork").GetComponentInChildren<Text>();
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
            InvestigationGameInput input = new InvestigationGameInput { caseId = "investigation_foodchain_02" };
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

                Assert.That(FindButton("Species Marker moon_jellyfish"), Is.Null);
                Assert.That(FindButton("Historical Species Marker moon_jellyfish"), Is.Null);
                Assert.That(GameObject.Find("Missing Signal"), Is.Null);
                result.speciesObservations[0].detectionState = SpeciesDetectionState.Detected;
                yield return LoadInvestigationScene();
                Button marker = FindButton("Species Marker moon_jellyfish");
                Assert.That(marker.GetComponent<RectTransform>().anchorMin.y, Is.InRange(.31f, .36f));

            }
            finally
            {
                InvestigationSessionBridge.Clear();
            }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_BootstrapsTwoActNavigation()
        {
            yield return LoadCurrent();
            Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(CurrentButton("Stage Observe"), Is.Not.Null);
            Assert.That(CurrentButton("Stage Simulate").GetComponentInChildren<Text>().text, Is.EqualTo("2 · Investigate"));
            Assert.That(GameObject.Find("Stage Report"), Is.Null);
            Assert.That(CurrentView.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(Screen.height > Screen.width ? new Vector2(720f, 1280f) : new Vector2(1280f, 720f)));
            Assert.That(CurrentView.GetComponent<Canvas>().pixelPerfect, Is.True);
            Assert.That(CurrentChild(CurrentView.transform, "Safe Area").GetComponent<InvestigationSafeAreaFitter>(), Is.Not.Null);
            Assert.That(CurrentButton("Arrival Briefing Next"), Is.Not.Null);
            Assert.That(GameObject.Find("Motion Toggle"), Is.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ObserveQuestionPersistsAcrossHelpSettingsAndNotebook()
        {
            yield return LoadInvestigationScene();
            Record("Species Marker shark"); Click("Compare Species tuna");
            Assert.That(FindButton("Talk To Edna"), Is.Null);
            Assert.That(GameObject.Find("Observe Comparison Instruction"), Is.Not.Null);
            InvestigationCurrentFlowTestActions.ToggleGuidanceForTests();
            Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null); yield return null;
            Click("Compare Change More");
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.HasDiscoveredObservation("E02_TUNA_WIDER_DETECTION"), Is.True);
            Record("Species Marker krill"); Record("Species Marker atlantic_herring"); Record("Species Marker phytoplankton");
            Assert.That(FindButton("Summarize Findings"), Is.Not.Null);
            Assert.That(GameObject.Find("Observe Comparison Board"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_PrematureNavigationWarnsWithoutInventingProgress()
        {
            yield return LoadCurrent();
            var toast = CurrentChild(CurrentView.transform, "Status Toast").gameObject;
            Assert.That(toast.activeSelf, Is.False);
            CurrentPress("Arrival Briefing Skip"); yield return null;
            CurrentPointerClick(CurrentButton("Stage Simulate")); yield return null;
            Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.Zero);
            Assert.That(toast.activeSelf, Is.True);
            yield return new WaitForSecondsRealtime(3.2f);
            Assert.That(toast.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ClickedSpeciesDetailsAndHighlightsExpireAfterTheLatestTap()
        {
            yield return LoadInvestigationScene();
            Click("Historical Species Marker shark");
            yield return new WaitForSecondsRealtime(.75f);
            Click("Historical Species Marker tuna");
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(FindButton("Historical Species Marker tuna").transform.Find("Paired Species Focus").gameObject.activeSelf, Is.True);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            Assert.That(FindButton("Historical Species Marker shark").transform.Find("Paired Species Focus").gameObject.activeSelf, Is.True,
                "After a facts preview closes, restore the organism in the current question.");
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Is.Empty);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_SpeciesFactsTooltipUsesShortHoverAndKeyboardFocus()
        {
            yield return LoadInvestigationScene();
                Click("Toggle Survey Map"); yield return null;
            Button shark = FindButton("Historical Species Marker shark");
            InvestigationHoverTooltipTrigger trigger = shark.GetComponent<InvestigationHoverTooltipTrigger>();
            Assert.That(trigger, Is.Not.Null);

            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            trigger.OnPointerEnter(new PointerEventData(EventSystem.current));
            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Null);
            yield return WaitForCondition(
                () => GameObject.Find("Species Facts Tooltip") != null,
                1f,
                "Species facts did not appear promptly on hover.");
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
                Click("Toggle Survey Map"); yield return null;
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            foreach (string name in new[] { "Historical Species Marker tuna", "Species Marker tuna" })
            {
                ClickThroughPointer(FindButton(name)); yield return null;
                Assert.That(GameObject.Find("Tooltip Title").GetComponent<Text>().text, Is.EqualTo("Atlantic Bluefin Tuna"));
                Assert.That(controller.State.DiscoveredObservationIds, Is.Empty, "Facts are optional; answering records evidence.");
                Click("Close Species Facts"); yield return null;
            }
            Record("Species Marker shark"); Record("Species Marker tuna");
            Click("Species Marker tuna"); yield return null;
            Assert.That(GameObject.Find("Tooltip Details").GetComponent<Text>().text, Does.Contain("RECORDED IN NOTEBOOK"));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_CompleteTwoActPlayerRouteRecordsOnlySurveyEvidence()
        {
            yield return LoadCurrent();
            RecordAllFindings();
            Assert.That(CurrentState.TriedThreatIds.Count, Is.Zero);
            EnterCurrentModels(); PlayAllModels();
            Assert.That(CurrentState.CompletedObjectiveCount, Is.Zero, "Playing alone must not choose an explanation");
            CurrentPress("Choose Scenario longline"); yield return null; yield return null;
            Assert.That(CurrentState.CompletedObjectiveCount, Is.EqualTo(7));
            Assert.That(CurrentState.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(CurrentState.ConfirmationReviewed, Is.False);
            ReviewRemainingExplanations(); yield return null; yield return null;
            CurrentPointerClick(CurrentButton("Complete Scenario Investigation")); yield return null;
            Assert.That(CurrentState.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(InvestigationSessionBridge.LastResult.completed, Is.True);
            Assert.That(InvestigationSessionBridge.LastResult.selectedHypothesisId, Is.EqualTo("longline"));
            Assert.That(InvestigationSessionBridge.LastResult.evidenceIds.Count, Is.EqualTo(5));
            Assert.That(CurrentState.ConfirmationReviewed, Is.False);
            Assert.That(CurrentState.MisstepCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_InternalGuidanceOverrideAndRestartPreserveTheCurrentContract()
        {
            yield return LoadCurrent(); InvestigationCurrentFlowTestActions.ToggleGuidanceForTests();
            Assert.That(CurrentState.Difficulty, Is.EqualTo(InvestigationDifficulty.Hard));
            EnterCurrentModels(); OpenCurrentSummary(); CurrentPress("Complete Scenario Investigation");
            yield return null; CurrentPress("Restart Completed Case"); yield return null;
            Assert.That(CurrentState.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(CurrentState.Difficulty, Is.EqualTo(InvestigationDifficulty.Hard));
            Assert.That(CurrentState.DiscoveredObservationIds.Count, Is.Zero);
            Assert.That(CurrentState.TriedThreatIds.Count, Is.Zero);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
            Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Null);
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
                Assert.That(FindButton("Species Marker shark"), Is.Null);
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
                "E04_KRILL_WIDER_DETECTION", "E03_HERRING_FEWER_SITES", "L01_NONDETECTION_LIMITATION" });
            try
            {
                InvestigationSessionBridge.SetInput(input);
                yield return LoadInvestigationScene();
                Assert.That(FindGameObject("Edna Name").GetComponent<Text>().text, Does.Contain("4/5"));
                Assert.That(FindButton("Continue To Simulate"), Is.Null);
                EnterSimulate("Stage Simulate");
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.Phase, Is.EqualTo(InvestigationPhase.Observe));
                Record("Species Marker phytoplankton");
                EnterSimulate("Continue To Simulate");
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
            }
            finally { InvestigationSessionBridge.Clear(); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_RecordedMarkersLinkBothSurveyMaps()
        {
            yield return LoadInvestigationScene();
            Record("Species Marker shark");
            Assert.That(FindButton("Species Marker shark"), Is.Null);
            foreach (string name in new[] { "Species Marker tuna", "Historical Species Marker tuna" })
                Assert.That(FindButton(name).transform.Find("Paired Species Focus").gameObject.activeSelf, Is.True);
            Record("Species Marker tuna"); yield return null;
            Assert.That(FindButton("Species Marker tuna").transform.Find("Recorded Finding"), Is.Not.Null);
            Record("Species Marker tuna");
            Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Has.Count.EqualTo(2));
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
        public IEnumerator InvestigationScene_EdnaAsksOneQuestionAtATimeAndExplainsNonDetection()
        {
            yield return LoadInvestigationScene();
            Assert.That(FindButton("Compare Species shark").interactable, Is.True);
            Record("Species Marker shark");
            Assert.That(GameObject.Find("Observe Comparison Feedback").GetComponent<Text>().text, Does.Contain("Not detected"));
            Assert.That(GameObject.Find("Comparison Notebook"), Is.Not.Null);
            Record("Species Marker tuna");
            Assert.That(FindButton("Compare Species krill").interactable, Is.True);
            Assert.That(GameObject.Find("Observe Inspection"), Is.Null);
        }

        private static void ClickThroughPointer(Button button)
        {
            string targetName = button.name;
            SetLensFor(targetName);
            Canvas.ForceUpdateCanvases();
            RectTransform rect = button.GetComponent<RectTransform>();
            PointerEventData pointer = PointerAtWorldPoint(rect.TransformPoint(rect.rect.center));
            GameObject firstHit = FirstPointerHit(pointer);
            GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(firstHit);
            Assert.That(target, Is.EqualTo(button.gameObject),
                $"{button.name}: hit {firstHit?.name ?? "nothing"}, bounds {WorldRect(rect)}, graphic depth {button.targetGraphic.depth}, culled {button.targetGraphic.canvasRenderer.cull}");
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ConclusionMarkSettlesAndRespectsReducedMotion()
        {
            bool previous = InvestigationMotionSettings.ReducedMotion;
            try
            {
                yield return LoadCurrent();
                foreach (bool reduced in new[] { false, true })
                {
                    InvestigationMotionSettings.SetReducedMotionForTests(reduced);
                    CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.ConclusionReady);
                    CurrentPress("Complete Scenario Investigation");
                    var mark = GameObject.Find("Scenario Case Stamp").GetComponent<RectTransform>();
                    if (reduced) Assert.That(mark.localScale, Is.EqualTo(Vector3.one));
                    else { Assert.That(mark.localScale.x, Is.LessThan(1f)); yield return new WaitForSecondsRealtime(.7f); }
                    Assert.That(mark.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(CurrentState.FinalSubmissionAttemptCount, Is.EqualTo(1));
                }
            }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(previous); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_RecordConclusionReceivesPointerInputOnce()
        {
            yield return LoadCurrent(); CurrentController.ApplyQaCheckpoint(InvestigationQaCheckpoint.ConclusionReady);
            yield return null; yield return null;
            var button = CurrentButton("Complete Scenario Investigation"); var submit = button.onClick;
            CurrentPointerClick(button); yield return null;
            Assert.That(CurrentState.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            submit.Invoke();
            Assert.That(CurrentState.FinalSubmissionAttemptCount, Is.EqualTo(1));
            Assert.That(InvestigationSessionBridge.LastResult.evidenceIds, Does.Not.Contain("E07_FISHING_LINE"));
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
                InvestigationCurrentFlowTestActions.ToggleGuidanceForTests();
                InvestigationCurrentFlowTestActions.ToggleGuidanceForTests();
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
        public IEnumerator InvestigationScene_NotebookOpensWithPointerAndKeyboardBeforeAndAfterConclusion()
        {
            yield return LoadCurrent();
            foreach (var checkpoint in new[] { InvestigationQaCheckpoint.ConclusionReady, InvestigationQaCheckpoint.CaseClosed })
            {
                CurrentController.ApplyQaCheckpoint(checkpoint); yield return null; yield return null;
                var before = CurrentState.ConclusionStatus;
                var exported = InvestigationSessionBridge.LastResult;
                CurrentPointerClick(CurrentButton("Toggle Notebook Drawer")); yield return null;
                Assert.That(GameObject.Find("Notebook Survey Story"), Is.Not.Null);
                Assert.That(GameObject.Find("Notebook ROV Pictures"), Is.Null);
                CurrentPress("Close Notebook Drawer"); yield return null;
                var button = CurrentButton("Toggle Notebook Drawer");
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                yield return null; Assert.That(GameObject.Find("Notebook Drawer"), Is.Not.Null);
                CurrentPress("Close Notebook Drawer"); yield return null;
                Assert.That(CurrentState.ConclusionStatus, Is.EqualTo(before));
                Assert.That(InvestigationSessionBridge.LastResult, Is.SameAs(exported));
            }
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
                RectTransform rect = CurrentChild(drawer.transform, region).GetComponent<RectTransform>();
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
                Assert.That(FindButton("Restart Case").interactable, Is.False);
                Assert.That(FindButton("Motion Toggle"), Is.Null);

                // Stale gameplay callbacks and development shortcuts must obey the same session boundary.
                controller.SendMessage("HandleSetPhase", InvestigationPhase.Simulate, SendMessageOptions.RequireReceiver);
                controller.SendMessage("HandleSetDifficulty", InvestigationDifficulty.Hard, SendMessageOptions.RequireReceiver);
                controller.SendMessage("HandleRecordModelConclusion", SendMessageOptions.RequireReceiver);
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ConclusionReady);
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
                Assert.That(FindButton("Restart Case").interactable, Is.True);
                InvestigationMotionSettings.SetReducedMotionForTests(!InvestigationMotionSettings.ReducedMotion);
                Assert.That(view.State, Is.SameAs(controller.State));
                Assert.That(InvestigationSessionBridge.PendingInput, Is.SameAs(invalidInput));
            }
            finally
            {
                InvestigationMotionSettings.SetReducedMotionForTests(previousMotion);
                InvestigationSessionBridge.Clear();
            }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_SharedStartupIsPreservedAndInvestigationLoadsByName()
        {
            InvestigationSessionBridge.Clear();
            string sharedEntry = SceneUtility.GetScenePathByBuildIndex(0);
            Assert.That(sharedEntry, Is.Not.Empty);
            Assert.That(sharedEntry, Is.Not.EqualTo("Assets/Scenes/InvestigationScene.unity"));
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(sharedEntry));
            Assert.That(Object.FindAnyObjectByType<InvestigationController>(), Is.Null);
            yield return SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            yield return null; yield return null;
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(FindButton("Arrival Briefing Next"), Is.Not.Null);
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

            SetLensFor("Species Marker tuna");
            ScrollRect page = GameObject.Find("Investigation Content").GetComponent<ScrollRect>();
            page.StopMovement(); page.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases(); yield return null;
            RectTransform shark = FindButton("Species Marker tuna").GetComponent<RectTransform>();
            Vector3 coveredPoint = shark.TransformPoint(shark.rect.center);
            RectTransform cardRect = pinnedCard.GetComponent<RectTransform>();
            // Keep a marker under the card regardless of the map's small random placement offsets.
            cardRect.position += coveredPoint - cardRect.TransformPoint(cardRect.rect.center);
            // The random map arrangement can put the marker near a screen edge.
            // Keep the moved test card (including its Close button) on-screen.
            Rect screenBounds = WorldRect(Object.FindAnyObjectByType<InvestigationRuntimeView>().GetComponent<RectTransform>());
            Rect movedBounds = WorldRect(cardRect);
            cardRect.position += new Vector3(
                Mathf.Max(screenBounds.xMin - movedBounds.xMin, Mathf.Min(0f, screenBounds.xMax - movedBounds.xMax)),
                Mathf.Max(screenBounds.yMin - movedBounds.yMin, Mathf.Min(0f, screenBounds.yMax - movedBounds.yMax)));
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
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Record("Species Marker shark");
            Assert.That(controller.State.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_NotebookScrollIsIndependentOfTheFixedWorkbench()
        {
            yield return LoadCurrent(); EnterCurrentModels(); yield return null; yield return null; SkipCurrentGuide();
            var canvas = CurrentView.GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            bool enabled = scaler.enabled; float scale = canvas.scaleFactor;
            try
            {
                scaler.enabled = false; canvas.scaleFactor = Screen.width / 720f;
                yield return null; yield return null;
                CurrentPress("Toggle Notebook Drawer"); yield return null; yield return null;
                var page = CurrentView.ContentRoot.GetComponentInParent<ScrollRect>();
                var book = CurrentNotebookScroll(); Canvas.ForceUpdateCanvases();
                Assert.That(page.vertical, Is.False);
                Assert.That(book.content.rect.height, Is.GreaterThan(book.viewport.rect.height));
                Vector2 pagePosition = page.content.anchoredPosition;
                book.verticalNormalizedPosition = .4f; yield return null;
                Assert.That(book.verticalNormalizedPosition, Is.EqualTo(.4f).Within(.02f));
                Assert.That(page.content.anchoredPosition, Is.EqualTo(pagePosition));
                CurrentPress("Close Notebook Drawer"); yield return null;
                Assert.That(page.enabled, Is.True); Assert.That(page.vertical, Is.False);
                Assert.That(GameObject.Find("Scenario EDNA Dock"), Is.Not.Null);
            }
            finally { canvas.scaleFactor = scale; scaler.enabled = enabled; }
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
                foreach (string id in new[] { "E01_SHARK_NONDETECTION", "E02_TUNA_WIDER_DETECTION", "E04_KRILL_WIDER_DETECTION",
                    "E03_HERRING_FEWER_SITES", "E05_PHYTOPLANKTON_FEWER_SITES" })
                    Assert.That(updater.TryDiscoverObservation(state, id, out _), Is.True);
                view.Refresh(state, string.Empty, InvestigationStatusTone.Guide);

                Assert.That(FindGameObject("Edna Name").GetComponent<Text>().text, Does.Contain("5/6"));
                Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("FINDINGS 5/6"));
                Assert.That(FindButton("Talk To Edna"), Is.Null);
                Assert.That(FindGameObject("Additional Comparison Record").GetComponent<Text>().text, Does.Contain("Additional survey reading"));
                Assert.That(FindButton("Continue To Simulate"), Is.Null);
                Assert.That(updater.TrySetPhase(state, InvestigationPhase.Simulate, out string phaseFeedback), Is.False);
                Assert.That(phaseFeedback, Does.Contain("at least 6 observations"));
                Assert.That(updater.TryRunThreat(state, "longline", out _, out string modelFeedback), Is.False);
                Assert.That(modelFeedback, Does.Contain("at least 6 observations"));

                Assert.That(updater.TryDiscoverObservation(state, "E_TEST_SIXTH_FINDING", out _), Is.True);
                view.Refresh(state, string.Empty, InvestigationStatusTone.Guide);
                Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("FINDINGS 6/6"));
                Assert.That(FindButton("Summarize Findings"), Is.Not.Null);
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
            BeginObserveQuestions();
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
            if (buttonName == "Toggle Survey Map" && GameObject.Find(buttonName) == null) buttonName = "Toggle Comparison View";
            Button button = FindButton(buttonName);
            Assert.That(button, Is.Not.Null, $"Button not found: {buttonName}");
            Assert.That(button.interactable, Is.True, $"Button is not interactable: {buttonName}");
            button.onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator InvestigationScene_WaterLayersStayBehindTheInterfaceAndPassInput()
        {
            yield return LoadInvestigationScene();
                Click("Toggle Survey Map"); yield return null;
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

            // The cover is a clipped child, otherwise it darkens the entire baseline.
            Assert.That(FindGameObject("Current Seamount").GetComponent<Image>().color.a, Is.Zero);
            Assert.That(FindGameObject("Current Survey Water").GetComponent<Image>().color.a, Is.EqualTo(1f));
            Assert.That(FindGameObject("Historical Seamount").GetComponent<Image>().color, Is.EqualTo((Color)InvestigationTheme.MapSurfaceHistorical));

            InvestigationVignetteGraphic vignette = background.Find("Water Vignette")
                .GetComponent<InvestigationVignetteGraphic>();
            AssertGraphicMeshCoversRectCorners(vignette);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReducedMotionFreezesAmbientWaterAnimation()
        {
            bool reducedMotionBefore = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotionForTests(true);
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

            InvestigationMotionSettings.SetReducedMotionForTests(false);
            yield return new WaitForSecondsRealtime(0.2f);
            for (int index = 0; index < animatedLayers.Length; index++)
            {
                Graphic graphic = FindGameObject(animatedLayers[index]).GetComponent<Graphic>();
                AssertAnyVertexMoved(frozenVertices[index], ReadGraphicVertices(graphic), animatedLayers[index]);
            }

            InvestigationMotionSettings.SetReducedMotionForTests(reducedMotionBefore);
        }

        private static Button FindButton(string name)
        {
            if ((name.StartsWith("Species Marker ") || name.StartsWith("Historical Species Marker "))
                && GameObject.Find("Survey Time Lens") == null && GameObject.Find("Toggle Comparison View") != null)
                GameObject.Find("Toggle Comparison View").GetComponent<Button>().onClick.Invoke();

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

    }
}
