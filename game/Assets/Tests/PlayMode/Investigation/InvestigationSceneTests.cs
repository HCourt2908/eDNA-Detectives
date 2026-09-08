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
        private static void AssertChoiceArrows(params string[] ids)
        {
            foreach (string id in new[] { "plastic", "longline", "bottom_trawling" })
                Assert.That(FindButton("Threat " + id).transform.Find("Choose Cause Arrow") != null,
                    Is.EqualTo(Array.IndexOf(ids, id) >= 0), id);
        }

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
                foreach (InvestigationQaCheckpoint checkpoint in new[] { InvestigationQaCheckpoint.ObserveReady, InvestigationQaCheckpoint.SimulateStart, InvestigationQaCheckpoint.FinalReportReady })
                {
                    controller.ApplyQaCheckpoint(checkpoint);
                    yield return null;
                    Assert.That(FindButton("Motion Toggle"), Is.Null);
                    Assert.That(InvestigationMotionSettings.ReducedMotion, Is.False, "The old saved Reduced preference must be ignored.");
                    InvestigationDifficulty before = controller.State.Difficulty;
                    Click("Difficulty Toggle");
                    Assert.That(controller.State.Difficulty, Is.Not.EqualTo(before));
                    Assert.That(InvestigationMotionSettings.ReducedMotion, Is.False);
                    Assert.That(FindButton("Difficulty Toggle").GetComponentInChildren<Text>().text, Is.EqualTo(controller.State.Difficulty.ToString()));
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
        public IEnumerator InvestigationScene_LensHandlePulsesUntilUsedAndRespectsMotionPreference()
        {
            bool previous = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotionForTests(false);
            try
            {
                yield return LoadInvestigationScene();
                CanvasGroup cue = GameObject.Find("Survey Lens Handle Cue").GetComponent<CanvasGroup>();
                float alpha = cue.alpha;
                yield return WaitForCondition(() => Mathf.Abs(cue.alpha - alpha) > .25f, 2f, "The slide handle should visibly pulse.");
                Assert.That(cue.blocksRaycasts, Is.False);
                InvestigationMotionSettings.SetReducedMotionForTests(true);
                yield return null; yield return null;
                Assert.That(cue.alpha, Is.EqualTo(1f).Within(.001f));
                GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = .85f;
                Assert.That(cue.gameObject.activeSelf, Is.False);
                Record("Species Marker shark"); yield return null;
                Assert.That(GameObject.Find("Survey Lens Handle Cue"), Is.Null, "The introduction must stay complete after a question refresh.");
                foreach (string id in new[] { "tuna", "sea_star", "mussel" })
                    Assert.That(FindButton("Species Marker " + id).transform.Find("Unrecorded Finding Cue"), Is.Null);
            }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(previous); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_EvidenceGuidePersistsUntilAComparisonIsSettled()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.EvidenceReady);
            yield return null;
            Assert.That(GameObject.Find("Edna Speech Text").GetComponent<Text>().text, Does.Contain("WHAT WE FOUND"));
            Assert.That(GameObject.Find("Choose Evidence Arrow"), Is.Not.Null);
            AssertTextFitsItsRect(GameObject.Find("Edna Speech Text").GetComponent<Text>());
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
            Assert.That(GameObject.Find("Observe Question"), Is.Not.Null);
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FirstFinding);
            Assert.That(GameObject.Find("Notebook Finding Count Text").GetComponent<Text>().text, Is.EqualTo("1"));
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateStart);
            AssertChoiceArrows("plastic", "longline", "bottom_trawling");
            Assert.That(controller.State.TriedThreatIds, Is.Empty);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.EvidenceReady);
            Assert.That(GameObject.Find("Edna Speech Text").GetComponent<Text>().text, Does.Contain("WHAT WE FOUND"));
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportQuestions);
            Assert.That(GameObject.Find("Report Review"), Is.Not.Null);
            Assert.That(controller.State.SelectedReportEvidenceIds, Has.Count.EqualTo(7));
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
        public IEnumerator InvestigationScene_FirstAnswerRevealsNotebookAndImportedFindingsSkipAnsweredQuestions()
        {
            InvestigationSessionBridge.Clear();
            try
            {
                yield return LoadInvestigationScene();
                Assert.That(FindButton("Toggle Notebook Drawer"), Is.Null);
                ClickThroughPointer(FindButton("Historical Species Marker shark"));
                yield return null;
                Assert.That(FindButton("Toggle Notebook Drawer"), Is.Null, "Reading facts cannot record a finding.");
                Record("Species Marker shark"); yield return null;
                Click("Toggle Notebook Drawer"); yield return null;
                Assert.That(GameObject.Find("Notebook Drawer"), Is.Not.Null);
                Assert.That(GameObject.Find("Notebook E01_SHARK_NONDETECTION"), Is.Not.Null);
                Click("Close Notebook Drawer"); yield return null;
                Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Does.Contain("Tuna"));
                InvestigationGameInput input = new InvestigationGameInput();
                input.discoveredObservationIds.Add("E04_BENTHIC_STABLE");
                InvestigationSessionBridge.SetInput(input); yield return LoadInvestigationScene();
                Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Does.Contain("Shark"));
                Click("Toggle Notebook Drawer"); yield return null;
                Assert.That(GameObject.Find("Notebook E04_BENTHIC_STABLE"), Is.Not.Null);
                Click("Close Notebook Drawer");
                Record("Species Marker shark"); Record("Species Marker tuna"); Record("Species Marker krill");
                Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Does.Contain("mussel"));
            }
            finally { InvestigationSessionBridge.Clear(); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ActionArrowMovesFromCauseToRunAndRespectsMotionPreference()
        {
            bool previousMotion = InvestigationMotionSettings.ReducedMotion;
            InvestigationMotionSettings.SetReducedMotionForTests(false);
            try
            {
                yield return LoadInvestigationScene();
                Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
                EnterSimulate("Stage Simulate");
                yield return null;
                InvestigationGuideArrowGraphic arrow = GameObject.Find("Choose Cause Arrow").GetComponent<InvestigationGuideArrowGraphic>();
                AssertChoiceArrows("plastic", "longline", "bottom_trawling");
                Assert.That(arrow.raycastTarget, Is.False);
                AssertInsideViewport(arrow.rectTransform, GameObject.Find("Investigation Content").GetComponent<ScrollRect>().viewport);
                float initialAlpha = arrow.GetComponent<CanvasGroup>().alpha;
                yield return WaitForCondition(() => Mathf.Abs(arrow.GetComponent<CanvasGroup>().alpha - initialAlpha) > .12f,
                    2f, "The current-action arrow should gently pulse in Full Motion.");
                InvestigationMotionSettings.SetReducedMotionForTests(!InvestigationMotionSettings.ReducedMotion);
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
                InvestigationMotionSettings.SetReducedMotionForTests(!InvestigationMotionSettings.ReducedMotion);
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
                Assert.That(GameObject.Find("Edna Speech Text").GetComponent<Text>().text, Does.Contain("Plastic"));
                Click("Talk To Edna");
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Not.Null);
                Click("Dismiss Edna");
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Null);
                Click("Talk To Edna");
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Not.Null);
                Click("Difficulty Toggle");
                Assert.That(GameObject.Find("Run Model Arrow"), Is.Null);
            }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(previousMotion); }
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ModelCheckProgressIsSeparateFromEvidenceRelationship()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            EnterSimulate("Stage Simulate");
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
            AssertThreatProgress("longline", "TO CHECK · 3/4", "SUPPORTS", false);
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            EnterSimulate("Stage Simulate");
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
            Assert.That(GameObject.Find("Report Edna Speech").GetComponent<Text>().text, Does.Contain("Which would you like"));
            Assert.That(FindButton("Review ROV Follow-up").GetComponentInChildren<Text>().text, Is.EqualTo("Inspect former shark habitat"));
            Assert.That(FindButton("Review ROV Seafloor").GetComponentInChildren<Text>().text, Is.EqualTo("Inspect the seafloor"));
            InspectRov("Review ROV Follow-up");
            Click("Discuss Explanation");
            Assert.That(GameObject.Find("Report Edna Speech").GetComponent<Text>().text, Does.Contain("main clue"));
            ConnectCluesToArgument("Report Key Clue Seafloor");
            Assert.That(GameObject.Find("Report Edna Speech").GetComponent<Text>().text, Does.Contain("change your explanation"));
            Click("Keep Report Explanation");
            Assert.That(FindButton("Submit Final Report").GetComponentInChildren<Text>().text, Is.EqualTo("Send report"));
            Assert.That(FindButton("Re-test Fishing Models"), Is.Null);
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
                InvestigationSeamountBackdrop owner = Object.FindAnyObjectByType<InvestigationSeamountBackdrop>();
                Assert.That(owner.RenderMaterial, Is.Not.Null);
                Material shared = owner.RenderMaterial;
                AssertSharedSeamountMaterial(shared);
                yield return new WaitForSecondsRealtime(1.3f);
                double beforeRefresh = owner.ElapsedSeconds;
                ClickThroughPointer(FindButton("Observe Answer NotDetected"));
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
            Assert.That(FindButton("Species Marker tuna").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            RectTransform contentPanel = FindGameObject("Investigation Content").GetComponent<RectTransform>();
            ScrollRect scroll = contentPanel.GetComponent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.verticalScrollbar, Is.Not.Null);
            Assert.That(scroll.viewport.GetComponent<Image>().raycastTarget, Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.content.rect.width, Is.LessThanOrEqualTo(scroll.viewport.rect.width + 1f));
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
            RectTransform firstFinding = FindGameObject("Observe Question").GetComponent<RectTransform>();
            Assert.That(historicalMap.position.x, Is.EqualTo(currentMap.position.x).Within(.01f), "The lens compares two aligned maps.");
            Assert.That(Mathf.Abs(historicalMap.rect.width - currentMap.rect.width), Is.LessThan(2f));
            Assert.That(firstFinding.rect.height, Is.GreaterThanOrEqualTo(currentMap.rect.height));
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
            Assert.That(FindButton("Historical Species Marker tuna").interactable, Is.True);
            Assert.That(FindButton("Species Marker tuna").interactable, Is.True);
            RectTransform historicalSharkMarker = FindButton("Historical Species Marker tuna").GetComponent<RectTransform>();
            RectTransform currentSharkMarker = FindButton("Species Marker tuna").GetComponent<RectTransform>();
            Assert.That(Vector2.Distance(historicalSharkMarker.anchorMin, currentSharkMarker.anchorMin), Is.EqualTo(0f).Within(.001f),
                "The sliding lens must align the same organism in both surveys.");
            Assert.That(currentSharkMarker.anchorMin.y, Is.InRange(0.90f, 0.96f));
            Assert.That(FindButton("Historical Species Marker tuna").transform.parent, Is.SameAs(historicalPlot));
            Assert.That(FindButton("Species Marker tuna").transform.parent, Is.SameAs(currentPlot));
            Assert.That(FindButton("Species Marker tuna").transform.IsChildOf(currentVisualClip), Is.False);
            Assert.That(FindButton("Species Marker tuna").transform.Find("Marker Halo"), Is.Null);
            Assert.That(FindButton("Species Marker shark"), Is.Null);
            Assert.That(FindButton("Species Marker tuna").transform.Find("Group Member Left"), Is.Not.Null);
            Assert.That(GameObject.Find("Continue To Simulate"), Is.Null,
                "The primary action should not appear until the required findings are recorded.");
            Assert.That(GameObject.Find("Observe Finding Progress Text"), Is.Null);
            Assert.That(FindGameObject("Edna Introduction Portrait").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(FindGameObject("Observe Question Text").GetComponent<Text>().text, Does.Contain("today"));
            Assert.That(FindGameObject("Edna Avatar").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(FindButton("Talk To Edna").GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(40f));
            Assert.That(GameObject.Find("Survey Lens Handle Cue"), Is.Not.Null);
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
            AssertImageOnlyMarker("Historical Species Marker tuna");
            AssertImageOnlyMarker("Species Marker tuna");
            AssertImageOnlyMarker("Species Marker tuna");
            Assert.That(FindButton("Species Marker krill"), Is.Null);
            AssertImageOnlyMarker("Species Marker sea_star");
            AssertImageOnlyMarker("Species Marker mussel");
            float seabedTop = Mathf.Max(WorldRect(FindButton("Species Marker sea_star").GetComponent<RectTransform>()).yMax,
                WorldRect(FindButton("Species Marker mussel").GetComponent<RectTransform>()).yMax);
            foreach (string species in new[] { "tuna" })
                Assert.That(WorldRect(FindButton("Species Marker " + species).GetComponent<RectTransform>()).yMin,
                    Is.GreaterThan(seabedTop), "Pelagic findings must remain above the seabed findings.");
            Texture2D terrainMask = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(terrainMask,
                File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources/Investigation/Seamount/surface-mask.png")), false), Is.True);
            try
            {
                foreach (string species in new[] { "tuna" })
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
        public IEnumerator InvestigationScene_ObserveQuestionPersistsAcrossHelpSettingsAndNotebook()
        {
            yield return LoadInvestigationScene();
            Record("Species Marker shark");
            string question = GameObject.Find("Observe Question Text").GetComponent<Text>().text;
            Click("Talk To Edna");
            Assert.That(GameObject.Find("Observe Question Feedback").GetComponent<Text>().text, Does.Contain("slider"));
            Click("Difficulty Toggle");
            Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Is.EqualTo(question));
            Click("Toggle Notebook Drawer"); yield return null;
            Click("Close Notebook Drawer"); yield return null;
            Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Is.EqualTo(question));
            Record("Species Marker tuna"); Record("Species Marker krill"); Record("Species Marker sea_star"); Record("Species Marker mussel");
            Assert.That(FindButton("Continue To Simulate"), Is.Not.Null);
            Click("Dismiss Edna");
            Assert.That(GameObject.Find("Edna Speech Text"), Is.Null);
            Click("Difficulty Toggle");
            Assert.That(GameObject.Find("Edna Speech Text"), Is.Null);
            Click("Talk To Edna");
            Assert.That(GameObject.Find("Edna Speech Text"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_WarningToastAppearsOnlyForInvalidActionAndExpires()
        {
            yield return LoadInvestigationScene();
            GameObject toast = FindGameObject("Status Toast");
            Assert.That(toast, Is.Not.Null);
            Assert.That(toast.activeSelf, Is.False);

            Record("Species Marker shark");
            Assert.That(toast.activeSelf, Is.False);
            EnterSimulate("Stage Simulate");
            toast = FindGameObject("Status Toast");
            Assert.That(toast.activeSelf, Is.True);
            Assert.That(toast.GetComponentInChildren<Text>().text, Does.Contain("Record at least"));

            yield return new WaitForSecondsRealtime(3.2f);
            Assert.That(toast.activeSelf, Is.False);

            Record("Species Marker tuna");
            Record("Species Marker krill");
            Record("Species Marker sea_star");
            Record("Species Marker mussel");
            EnterSimulate("Continue To Simulate");
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
        public IEnumerator InvestigationScene_SpeciesFactsTooltipUsesOneSecondHoverAndKeyboardFocus()
        {
            yield return LoadInvestigationScene();
            Button shark = FindButton("Historical Species Marker shark");
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
        public IEnumerator InvestigationScene_NotebookEntriesScrollWithoutCoveringFixedCta()
        {
            yield return LoadInvestigationScene();
            Record("Species Marker shark");
            Record("Species Marker tuna");
            Record("Species Marker krill");
            Record("Species Marker sea_star");
            Record("Species Marker mussel");
            EnterSimulate("Continue To Simulate");
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

            Record("Species Marker shark");
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
            InvestigationMotionSettings.SetReducedMotionForTests(false);
            yield return LoadInvestigationScene();
            Record("Species Marker shark");
            Record("Species Marker tuna");
            Record("Species Marker krill");
            Record("Species Marker sea_star");
            Record("Species Marker mussel");
            EnterSimulate("Continue To Simulate");
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
            Assert.That(seaStar.interactable, Is.True);
            Assert.That(seaStar.transform.Find("Crowd Member 1"), Is.Not.Null);

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
            yield return WaitForCondition(() => seaStarState.text == "Decrease", 2f, "The Sea star model should animate before ROV.");
            Assert.That(musselState.text, Is.EqualTo("Stable"));
            Assert.That(tuna.transform.Find("Species Artwork").localScale.x, Is.GreaterThan(1.05f));
            Assert.That(krill.transform.Find("Species Artwork").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(tuna.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.GreaterThan(0.5f));
            Assert.That(shark.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(krill.transform.Find("Crowd Member 1").GetComponent<Image>().color.a, Is.LessThan(0.01f));
            Assert.That(seaStar.transform.Find("Crowd Member 1"), Is.Not.Null);
            Assert.That(GameObject.Find("Workbench Evidence Heading"), Is.Not.Null);
            CanvasGroup pageGroup = FindGameObject("Page Content").GetComponent<CanvasGroup>();
            Assert.That(pageGroup, Is.Not.Null);
            Click("Prediction shark");
            pageGroup = FindGameObject("Page Content").GetComponent<CanvasGroup>();
            Assert.That(pageGroup.alpha, Is.EqualTo(1f).Within(0.001f));
            InvestigationMotionSettings.SetReducedMotionForTests(reducedMotionBefore);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_CompletePlayerFacingWorkflow_ReachesCorrectReport()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            foreach (string species in new[] { "shark", "tuna", "krill", "sea_star", "mussel" }) Record("Species Marker " + species);
            EnterSimulate("Continue To Simulate");
            Click("Threat plastic");
            Click("Run Selected Model");
            Compare("mussel", "E06_PLASTIC_INDICATOR_STABLE", ComparisonJudgement.Mismatch);
            Click("Threat longline");
            Click("Run Selected Model");
            Assert.That(FindButton("Prediction sea_star").interactable, Is.True);
            Compare("sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Match);
            Compare("shark", "E01_SHARK_NONDETECTION", ComparisonJudgement.Match);
            Compare("tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match);
            Compare("krill", "E03_KRILL_NONDETECTION", ComparisonJudgement.Match);
            Click("Threat bottom_trawling");
            Click("Run Selected Model");
            Compare("tuna", "E02_TUNA_WIDER_DETECTION", ComparisonJudgement.Match);
            Assert.That(FindButton("Write Provisional Report"), Is.Null, "Finish the final Sea star check before leaving Simulate.");
            Compare("sea_star", "E04_BENTHIC_STABLE", ComparisonJudgement.Mismatch);
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(7));
            Assert.That(controller.State.ConfirmationReviewed, Is.False);
            Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("CHECKS 7/7"));
            AssertThreatProgress("longline", "CHECKED · 4/4", "SUPPORTS", true);
            AssertThreatProgress("bottom_trawling", "CHECKED · 2/2", "MIXED EVIDENCE", true);
            Click("Write Provisional Report");
            Click("Provisional Cause bottom_trawling");
            Click("Confirm Provisional Idea");
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(GameObject.Find("Survey Report Paper"), Is.Null);
            Assert.That(controller.State.FinalThreatId, Is.Empty);
            InspectRov("Review ROV Follow-up");
            yield return null;
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(controller.State.FinalThreatId, Is.EqualTo("bottom_trawling"));
            Assert.That(controller.State.SelectedReportEvidenceIds, Has.Count.EqualTo(7));
            Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(7));
            Click("Discuss Explanation");
            ConnectCluesToArgument("Report Key Clue Seafloor");
            Assert.That(GameObject.Find("Report Edna Speech").GetComponent<Text>().text, Does.Contain("bottom trawling"));
            Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("2/3"));
            Assert.That(GameObject.Find("Report Review"), Is.Null);
            Click("Edit Report Cause");
            Click("Final Cause longline");
            Assert.That(GameObject.Find("Report Conflict"), Is.Null);
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
            Click("Submit Final Report");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(InvestigationSessionBridge.LastResult.completed, Is.True);
            Assert.That(InvestigationSessionBridge.LastResult.correct, Is.True);
            Assert.That(GameObject.Find("Case Closed Summary"), Is.Not.Null);
            Click("Restart Completed Case");
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Observe));
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_DifficultyAndRestartRemainAvailable()
        {
            bool before = InvestigationMotionSettings.ReducedMotion;
            InvestigationSessionBridge.Clear();
            InvestigationMotionSettings.SetReducedMotionForTests(false);
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
            InvestigationMotionSettings.SetReducedMotionForTests(!InvestigationMotionSettings.ReducedMotion);
            Assert.That(InvestigationMotionSettings.ReducedMotion, Is.True);
            Record("Species Marker shark");
            Record("Species Marker tuna");
            Record("Species Marker krill");
            Record("Species Marker sea_star");
            Record("Species Marker mussel");
            Assert.That(controller.State.DiscoveredObservationIds, Has.Count.EqualTo(5));
            EnterSimulate("Continue To Simulate");
            Click("Threat plastic");
            Click("Run Selected Model");
            Assert.That(GameObject.Find("Case Questions"), Is.Null);
            Assert.That(FindGameObject("Edna Name").GetComponent<Text>().text, Does.Contain("plastic"));
            Assert.That(GameObject.Find("Comparison Gate"), Is.Null);
            Assert.That(FindButton("Edna Continue"), Is.Null, "Hard mode lets the player choose the prediction independently.");
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
            InvestigationMotionSettings.SetReducedMotionForTests(before);
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
                "E03_KRILL_NONDETECTION", "E04_BENTHIC_STABLE", "L01_NONDETECTION_LIMITATION" });
            try
            {
                InvestigationSessionBridge.SetInput(input);
                yield return LoadInvestigationScene();
                Assert.That(FindGameObject("Edna Name").GetComponent<Text>().text, Does.Contain("5/5"));
                Assert.That(FindButton("Continue To Simulate"), Is.Null);
                EnterSimulate("Stage Simulate");
                Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.Phase, Is.EqualTo(InvestigationPhase.Observe));
                Record("Species Marker mussel");
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
        public IEnumerator InvestigationScene_HypothesisSummaryReopensSavedComparisonsWithoutSpoilers()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.SimulateComplete);
            Click("Toggle Notebook Drawer");
            Click("Toggle Hypothesis Summary");
            yield return null;
            Assert.That(FindGameObject("Hypothesis Summary longline").GetComponent<Text>().text, Does.Contain("SUPPORT 4"));
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
            Assert.That(FindButton("Revisit Comparison longline sea_star"), Is.Not.Null);
            Assert.That(FindGameObject("Notebook E07_FISHING_LINE"), Is.Null);
            int completed = controller.State.CompletedObjectiveCount;
            Click("Revisit Comparison longline shark");
            yield return null;
            Assert.That(FindGameObject("Notebook Drawer"), Is.Null);
            Assert.That(FindButton("Prediction shark").transform.Find("Comparison Locked"), Is.Not.Null);
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
        public IEnumerator InvestigationScene_EdnaAnswersFollowTheSelectedPredictionWithoutRepeatingTheTutorial()
        {
            yield return LoadInvestigationScene();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            EnterSimulate("Stage Simulate");
            Click("Threat longline");
            Click("Run Selected Model");
            Click("Prediction shark");
            Click("Talk To Edna");
            Click("Edna Why");
            Assert.That(FindGameObject("Edna Speech Text").GetComponent<Text>().text, Does.Contain("same species"));
            Click("Prediction tuna");
            Assert.That(GameObject.Find("Edna Speech Text").GetComponent<Text>().text, Does.Contain("Tuna"));
            Click("Talk To Edna");
            Click("Edna Next Step");
            Assert.That(FindGameObject("Edna Speech Text").GetComponent<Text>().text, Does.Contain("WHAT WE FOUND"));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_EdnaAsksOneQuestionAtATimeAndExplainsNonDetection()
        {
            yield return LoadInvestigationScene();
            Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Does.Contain("Shark"));
            Record("Species Marker shark");
            Assert.That(GameObject.Find("Observe Question Feedback").GetComponent<Text>().text, Does.Contain("Not detected does not mean gone"));
            Assert.That(GameObject.Find("Observe Notebook Tip").GetComponent<Text>().text, Does.Contain("notebook"));
            Record("Species Marker tuna");
            Assert.That(GameObject.Find("Observe Question Text").GetComponent<Text>().text, Does.Contain("Krill"));
            Assert.That(GameObject.Find("Observe Inspection"), Is.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_RovPanelRevealsFindingsAndTheCompletedReportWithoutReturningToSimulate()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ReportReady);
            yield return null;
            Canvas.ForceUpdateCanvases();
            ScrollRect page = GameObject.Find("Investigation Content").GetComponent<ScrollRect>();
            AssertInsideViewport(GameObject.Find("Report Edna Conversation").GetComponent<RectTransform>(), page.viewport);
            Assert.That(GameObject.Find("Confirmation E07_FISHING_LINE"), Is.Null);
            Button open = FindButton("Review ROV Follow-up");
            AssertInsideViewport(open.GetComponent<RectTransform>(), page.viewport);
            bool fitBeforeReveal = page.content.rect.height <= page.viewport.rect.height + 1f;
            ClickThroughPointer(open);
            CompleteCameraCapture();
            // A settings refresh during the reveal must leave the new findings visible.
            Click("Difficulty Toggle");
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (fitBeforeReveal && page.content.rect.height > page.viewport.rect.height + 1f) Assert.That(page.verticalNormalizedPosition, Is.EqualTo(1f).Within(.01f),
                "The revealed findings must stay at the top when the report grows a previously unscrollable page.");
            AssertInsideViewport(GameObject.Find("Report Edna Conversation").GetComponent<RectTransform>(), page.viewport);
            Assert.That(controller.State.ConfirmationReviewed, Is.True);
            Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Report));
            Assert.That(GameObject.Find("Sealed ROV Finding 1"), Is.Null);
            foreach (string id in new[] { "E07_FISHING_LINE", "E08_SEAFLOOR_INTACT" })
            {
                GameObject finding = GameObject.Find("Confirmation " + id);
                Assert.That(finding, Is.Not.Null);
                CanvasGroup group = finding.GetComponent<CanvasGroup>();
                Assert.That(group == null || group.alpha > .99f, Is.True);
                foreach (Text text in finding.GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
            }
            Assert.That(FindButton("Re-test Fishing Models"), Is.Null);
            Assert.That(GameObject.Find("Report Review"), Is.Null);
            Click("Discuss Explanation");
            ConnectCluesToArgument("Report Key Clue FoodWeb");
            Click("Keep Report Explanation");
            Assert.That(GameObject.Find("Report Review"), Is.Not.Null);
            Assert.That(controller.State.SelectedReportEvidenceIds, Has.Count.EqualTo(7));
            Assert.That(FindButton("Submit Final Report"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_EdnaConversationControlsReceivePointerInputAcrossStages()
        {
            yield return LoadInvestigationScene();
            foreach (InvestigationQaCheckpoint checkpoint in new[] { InvestigationQaCheckpoint.Start, InvestigationQaCheckpoint.SimulateStart, InvestigationQaCheckpoint.ReportReady })
            {
                Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(checkpoint);
                yield return null;
                if (checkpoint == InvestigationQaCheckpoint.Start)
                {
                    ClickThroughPointer(FindButton("Talk To Edna")); yield return null;
                    AssertGuideControlVisible(FindButton("Observe Answer NotDetected"));
                    ClickThroughPointer(FindButton("Observe Answer NotDetected")); yield return null;
                    Assert.That(Object.FindAnyObjectByType<InvestigationController>().State.DiscoveredObservationIds, Has.Count.EqualTo(1));
                    continue;
                }
                if (checkpoint == InvestigationQaCheckpoint.ReportReady)
                {
                    Assert.That(GameObject.Find("Edna Speech"), Is.Null);
                    AssertGuideControlVisible(FindButton("Review ROV Follow-up"));
                    ClickThroughPointer(FindButton("Review ROV Follow-up"));
                    CompleteCameraCapture();
                    yield return null;
                    Assert.That(GameObject.Find("Report Edna Speech").GetComponent<Text>().text, Does.Contain("camera found fishing line"));
                    continue;
                }
                Click("Talk To Edna");
                yield return null;
                AssertGuideControlVisible(FindButton("Edna Why"));
                ClickThroughPointer(FindButton("Edna Why"));
                yield return null;
                foreach (Text text in GameObject.Find("Edna Speech").GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
                ClickThroughPointer(FindButton("Dismiss Edna"));
                yield return null;
                Assert.That(GameObject.Find("Edna Speech Text"), Is.Null);
                ClickThroughPointer(FindButton("Talk To Edna"));
                yield return null;
                Assert.That(GameObject.Find("Edna Speech Text"), Is.Not.Null);
            }
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
            string reasoning = controller.State.SelectedReasoningId;
            string limitation = controller.State.SelectedLimitationId;
            var evidence = new System.Collections.Generic.List<string>(controller.State.SelectedReportEvidenceIds);
            Click("Edit Report Cause");
            Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("2/3"));
            Click("Final Cause plastic");
            Assert.That(GameObject.Find("Report Explanation").GetComponent<Text>().text, Is.EqualTo("Plastic pollution"));
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.Zero);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
            Click("Submit Final Report");
            yield return null;
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Incorrect));
            Assert.That(GameObject.Find("Report Edna Speech").GetComponent<Text>().text, Does.Contain("mussel"));
            Assert.That(FindButton("Final Cause longline"), Is.Null, "EDNA first explains the conflict and offers revision.");
            Click("Edit Report Cause");
            Assert.That(FindButton("Final Cause longline"), Is.Not.Null);
            Click("Cancel Report Cause");
            Assert.That(FindButton("Final Cause longline"), Is.Null);
            Click("Edit Report Cause");
            Click("Final Cause longline");
            Assert.That(controller.State.SelectedReasoningId, Is.EqualTo(reasoning));
            Assert.That(controller.State.SelectedLimitationId, Is.EqualTo(limitation));
            CollectionAssert.AreEqual(evidence, controller.State.SelectedReportEvidenceIds);
            Assert.That(InvestigationSessionBridge.LastResult, Is.Null);
            Click("Submit Final Report");
            Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
            Assert.That(controller.State.FinalSubmissionAttemptCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReportKeepsItsDraftAndReflowsInAShortNarrowCanvas()
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
                Click("Edit Report Cause");
                AssertChildrenStayInsideLayout("Report Conversation Actions");
                Click("Final Cause bottom_trawling");
                Click("Stage Observe");
                Click("Stage Report");
                yield return null;
                yield return null;
                Assert.That(controller.State.FinalThreatId, Is.EqualTo("bottom_trawling"));
                Assert.That(controller.State.SelectedReportEvidenceIds, Has.Count.EqualTo(7));
                Assert.That(GameObject.Find("Report Dialogue Progress").GetComponent<Text>().text, Does.Contain("3/3"));
                AssertChildrenStayInsideLayout("Report Findings");
                ScrollRect scroll = GameObject.Find("Investigation Content").GetComponent<ScrollRect>();
                Assert.That(scroll.content.rect.width, Is.LessThanOrEqualTo(scroll.viewport.rect.width + 1f));
                foreach (Text text in GameObject.Find("Survey Report Paper").GetComponentsInChildren<Text>()) AssertTextFitsItsRect(text);
                Click("Edit Report Cause");
                Click("Final Cause longline");
                yield return null; // Let rebuilt graphics acquire their raycast depth.
                ClickThroughPointer(FindButton("Submit Final Report"));
                Assert.That(controller.State.ConclusionStatus, Is.EqualTo(InvestigationConclusionStatus.Correct));
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
            InspectRov("Review ROV Follow-up");
            Click("Discuss Explanation");
            ConnectCluesToArgument("Report Key Clue FoodWeb");
            Click("Keep Report Explanation");
            yield return null;
        }

        [UnityTest]
        public IEnumerator InvestigationScene_ReportDiagnosticsRemainLocalInHardMode()
        {
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
            Click("Difficulty Toggle");
            Click("Edit Report Cause");
            Click("Final Cause bottom_trawling");
            Click("Submit Final Report");
            yield return null;
            Transform conversation = FindGameObject("Report Edna Conversation").transform;
            string message = FindGameObject("Report Edna Speech").GetComponent<Text>().text;
            Assert.That(message, Does.Contain("seafloor"));
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(conversation), Is.True);
            Assert.That(controller.State.SelectedReportEvidenceIds, Has.Count.EqualTo(7));
            Click("Difficulty Toggle");
            Assert.That(FindGameObject("Report Edna Speech").GetComponent<Text>().text, Is.EqualTo(message));
            Click("Edit Report Cause");
            Click("Final Cause longline");
            Assert.That(GameObject.Find("Report Edna Speech").GetComponent<Text>().text, Does.Not.Contain(message));
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
                InvestigationMotionSettings.SetReducedMotionForTests(false);
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
                InvestigationMotionSettings.SetReducedMotionForTests(true);
                controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
                Click("Submit Final Report");
                stamp = FindGameObject("Case Closed Stamp").GetComponent<RectTransform>();
                Assert.That(stamp.localScale, Is.EqualTo(Vector3.one));
                Quaternion pose = stamp.localRotation;
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(stamp.localRotation, Is.EqualTo(pose));
            }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(previous); }
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
        public IEnumerator InvestigationScene_SimulateNotebookOpensBeforeAndAfterCaseClosure()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            InvestigationRuntimeView view = Object.FindAnyObjectByType<InvestigationRuntimeView>();
            foreach (InvestigationQaCheckpoint checkpoint in new[]
            {
                InvestigationQaCheckpoint.EvidenceReady,
                InvestigationQaCheckpoint.SimulateComplete,
                InvestigationQaCheckpoint.CaseClosed
            })
            {
                controller.ApplyQaCheckpoint(checkpoint);
                EnterSimulate("Stage Simulate");
                yield return null;
                InvestigationConclusionStatus status = controller.State.ConclusionStatus;
                int checks = controller.State.CompletedObjectiveCount;
                InvestigationGameResult published = InvestigationSessionBridge.LastResult;
                ClickThroughPointer(FindButton("Toggle Notebook Drawer"));
                yield return null;
                AssertActiveNotebookCount(view, 1);
                Assert.That(FindGameObject("Notebook E04_BENTHIC_STABLE"), Is.Not.Null);
                Click("Close Notebook Drawer");
                yield return null;
                AssertActiveNotebookCount(view, 0);
                Button notebook = FindButton("Toggle Notebook Drawer");
                EventSystem.current.SetSelectedGameObject(notebook.gameObject);
                ExecuteEvents.Execute(notebook.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                yield return null;
                AssertActiveNotebookCount(view, 1);
                Click("Close Notebook Drawer");
                yield return null;
                AssertActiveNotebookCount(view, 0);
                Assert.That(controller.State.Phase, Is.EqualTo(InvestigationPhase.Simulate));
                Assert.That(controller.State.ConclusionStatus, Is.EqualTo(status));
                Assert.That(controller.State.CompletedObjectiveCount, Is.EqualTo(checks));
                Assert.That(InvestigationSessionBridge.LastResult, Is.SameAs(published));
            }
            InvestigationSessionBridge.Clear();
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
                Assert.That(FindButton("Motion Toggle"), Is.Null);

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
            Assert.That(FindButton("Observe Answer NotDetected"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_EvidenceSelectionChecksImmediatelyWithKeyboardAndPointer()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            controller.ApplyQaCheckpoint(InvestigationQaCheckpoint.ObserveReady);
            EnterSimulate("Stage Simulate");
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
            InspectRov("Review ROV Follow-up");
            Assert.That(controller.State.ConfirmationReviewed, Is.True);

            EnterSimulate("Stage Simulate");
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

            SetLensFor("Species Marker tuna");
            RectTransform shark = FindButton("Species Marker tuna").GetComponent<RectTransform>();
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
            Assert.That(GameObject.Find("Species Facts Tooltip"), Is.Not.Null);
            Assert.That(controller.State.DiscoveredObservationIds, Is.Empty);
            Record("Species Marker shark");
            Assert.That(controller.State.HasDiscoveredObservation("E01_SHARK_NONDETECTION"), Is.True);
        }

        [UnityTest]
        public IEnumerator InvestigationScene_NotebookScrollsIndependentlyAndRestoresBackgroundScrolling()
        {
            InvestigationSessionBridge.Clear();
            yield return LoadInvestigationScene();
            Object.FindAnyObjectByType<InvestigationController>().ApplyQaCheckpoint(InvestigationQaCheckpoint.FinalReportReady);
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

                Assert.That(FindGameObject("Edna Name").GetComponent<Text>().text, Does.Contain("6/6"));
                Assert.That(FindGameObject("Metrics").GetComponent<Text>().text, Does.Contain("FINDINGS 5/6"));
                Click("Talk To Edna");
                Assert.That(FindGameObject("Observe Question Text").GetComponent<Text>().text, Does.Contain("Additional survey reading"));
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
