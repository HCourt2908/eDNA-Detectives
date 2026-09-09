# Investigation validation and test migration

Current specification: [ECOSYSTEM_DETECTIVE.md](../ECOSYSTEM_DETECTIVE.md).

## Migration policy

The previous full PlayMode run had 131 tests: 74 passed and 57 failed against
superseded UI contracts. This migration preserves all 74 passing tests, updates
18 meaningful checks, and retires 39 checks for removed interactions with explicit
replacement coverage below. No Ignore attributes, blanket skips or failure suppression
are used. Legacy scientific/full-report rules remain covered in EditMode.

The former ReportReasoning fixture is now ConclusionSummary; the remaining passing
Workbench lens-input check is now SurveyLensInput. These renames reflect current scope.

The removed FoodChainDesk test fixture exercised the superseded connection/drag desk.
The active cards, animations, interruptions and completion gates are covered by the
Scenario fixtures. Input, viewport, notebook and full-player-route checks are retained.

## Commands

Run from the repository root, preferably against an isolated project copy while another
Unity Editor is in use. The project path below is the normal checkout path.

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.4.6f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -projectPath "$PWD/game" -runTests -testPlatform EditMode -testResults /private/tmp/edna-edit.xml -logFile /private/tmp/edna-edit.log
"$UNITY" -batchmode -projectPath "$PWD/game" -runTests -testPlatform PlayMode -testResults /private/tmp/edna-play.xml -logFile /private/tmp/edna-play.log
"$UNITY" -batchmode -quit -projectPath "$PWD/game" -executeMethod EDNA.Investigation.Editor.InvestigationBuildValidator.BuildMacOsFromCommandLine -logFile /private/tmp/edna-build.log
```

The build validator writes `/private/tmp/edna-investigation-macos.app`. WebGL has
a separate `BuildWebGlFromCommandLine` entry point and requires its platform module.

## Current validation

- PlayMode: **92/92 passed**, full repository suite.
- EditMode: **59/59 passed**, full repository suite.
- macOS Development build: **succeeded** for `InvestigationScene` with Unity 6000.4.6f1 on 2026-09-09.
- Metadata: no missing test-script `.meta` files or duplicate asset GUIDs.
- Markdown links in the current documentation resolve locally.
- WebGL/device/performance checks were not part of this validation run.

The previous review's 62 EditMode results included three temporary identification
probe tests that existed only in the old validation copy, not in the repository.
The clean, source-synchronised suite contains 59 tests; no repository EditMode test
was removed in this migration. Import/roster validation remains covered by the
permanent domain suite.

One preserved tooltip-input check also had a random-position fixture problem: it
moved its own Close button off-screen. The fixture now clamps the moved test card
to the canvas without weakening its pointer-blocking or closing assertions.
Notebook scroll checks use a compact canvas to guarantee real overflow, and the
navigation check verifies book-button focus plus keyboard movement back to Next.

## Old-to-new coverage mapping

| Previous test | Decision | Current coverage / reason |
|---|---|---|
| `InvestigationEdnaTests.Edna_FollowsEachComparisonAndNeverBlocksTheTarget` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaTests.Edna_CanLeadEveryRequiredComparisonWithoutASeparateQuestionPanel` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaTests.Edna_HintFollowsThePlayersSelectedSpecies` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaTests.ReportConversation_AllThreeRoundsSupportEitherClueFirstAndRevisiting` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationEdnaTests.PredictionCues_PulseWithoutMovingOrBlockingCardsAndExcludeSavedChecks` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaTests.NotebookIntroduction_AppearsAfterFirstCheckAndTeachesReviewWithoutReplaying` | retired | `InvestigationNavigationTests + InvestigationScenarioDockTests + InvestigationScenarioEndingTests` — Removed hypothesis-summary/per-prediction notebook controls or obsolete layout; current picture scrolling, pinned controls and summary persistence are covered. |
| `InvestigationEdnaTests.Edna_SimulateDialogueSitsBelowThePlayableViewport` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaTests.Edna_TogglingHelpKeepsSimulatorCardsAndScrollStable` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaTests.EvidenceChoices_AreShuffledButStayStableDuringTheInvestigation` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaTests.Edna_HidesForNotebookAndRestoresWithoutLosingProgress` | updated | `Edna_StaysVisibleWhileNotebookPreservesPlaybackProgress` — Retain the behavioural check using the current two-act controls. |
| `InvestigationEdnaTests.Edna_KeyboardHelpAndReducedMotionDoNotChangeTheInvestigation` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaTests.Edna_QuestionsFitNarrowScreensAndDoNotReplayOnResize` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationEdnaVisibilityTests.FloatingGuide_CloseReopenAndNotebookRestoreOnlyUsefulControls` | updated | `PersistentGuide_HighlightAndNotebookKeepUsefulControls` — Retain the behavioural check using the current two-act controls. |
| `InvestigationEdnaVisibilityTests.ClosedCase_ReopenControlStillWorksAndDoesNotLeakIntoRestart` | updated | `ClosedCase_NotebookRemainsAvailableAndDockDoesNotLeakIntoRestart` — Retain the behavioural check using the current two-act controls. |
| `InvestigationFoodChainDeskTests.Desk_RecordsStayFixedThroughConnectingPredictionAndReset` | retired | `InvestigationScenarioCardsTests + InvestigationScenarioPopulationTests + InvestigationScenarioFlowTests` — Removed connection/drag intervention desk; explicit Play/Replay, population changes, interruption and conclusion gates are covered. |
| `InvestigationFoodChainDeskTests.Desk_ControlsAndFoodWebRecordsSettleTheThreeExplanations` | retired | `InvestigationScenarioCardsTests + InvestigationScenarioPopulationTests + InvestigationScenarioFlowTests` — Removed connection/drag intervention desk; explicit Play/Replay, population changes, interruption and conclusion gates are covered. |
| `InvestigationFoodChainDeskTests.Desk_IndividualCheckAndConnectionReviewPreserveTheSameRecords` | retired | `InvestigationScenarioCardsTests + InvestigationScenarioPopulationTests + InvestigationScenarioFlowTests` — Removed connection/drag intervention desk; explicit Play/Replay, population changes, interruption and conclusion gates are covered. |
| `InvestigationNavigationTests.Navigation_NotebookReturnRestoresTheActiveEdnaTask` | updated | `Navigation_NotebookReturnsToTheSameSpotlightStep` — Retain the behavioural check using the current two-act controls. |
| `InvestigationNavigationTests.Navigation_ReportCompareActionShowsTheCurrentCause` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationNavigationTests.Navigation_NarrowNextCheckRevealsTheEvidenceChoices` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationNavigationTests.Navigation_NotebookPreservesTheReplyAndRespectsDismissal` | retired | `InvestigationNavigationTests + InvestigationScenarioDockTests + InvestigationScenarioEndingTests` — Removed hypothesis-summary/per-prediction notebook controls or obsolete layout; current picture scrolling, pinned controls and summary persistence are covered. |
| `InvestigationNavigationTests.Navigation_OrdinaryNotebookOpeningKeepsTheReadingPosition` | updated | `Navigation_NotebookReopensAtItsSavedPicturePosition` — Retain the behavioural check using the current two-act controls. |
| `InvestigationReportReasoningTests.ReportReasoning_EveryClueReceivesFeedbackWithoutChangingScoresOrThePlayersCause` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationReportReasoningTests.ReportReasoning_PreservesAndReconsidersTheClueAcrossNotebookAndReportRevision` | updated | `ConclusionSummary_ReplayDoesNotSilentlyChangeTheSelectedCause` — Retain the behavioural check using the current two-act controls. |
| `InvestigationReportReasoningTests.ReportReasoning_DiscussionTextAndControlsFitAcrossCanvasWidths` | updated | `ConclusionSummary_TextAndControlsFitAcrossCanvasWidths` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_LensHandlePulsesUntilUsedAndRespectsMotionPreference` | updated | `InvestigationScene_HistoryHandleCueStopsAfterUse` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_EvidenceGuidePersistsUntilAComparisonIsSettled` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationSceneTests.InvestigationScene_QaCheckpointsCoverCurrentProgressiveFlowAndPublishCompletion` | updated | `InvestigationScene_QaCheckpointsReachTheTwoActConclusionWithoutRov` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_ActionArrowMovesFromCauseToRunAndRespectsMotionPreference` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationSceneTests.InvestigationScene_ModelCheckProgressIsSeparateFromEvidenceRelationship` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationSceneTests.InvestigationScene_RovActionHintsUseTheVisibleButtonNames` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationSceneTests.InvestigationScene_BootstrapsObserveCanvasAndAccessibleControls` | updated | `InvestigationScene_BootstrapsTwoActNavigation` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_WarningToastAppearsOnlyForInvalidActionAndExpires` | updated | `InvestigationScene_PrematureNavigationWarnsWithoutInventingProgress` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_NotebookEntriesScrollWithoutCoveringFixedCta` | retired | `InvestigationNavigationTests + InvestigationScenarioDockTests + InvestigationScenarioEndingTests` — Removed hypothesis-summary/per-prediction notebook controls or obsolete layout; current picture scrolling, pinned controls and summary persistence are covered. |
| `InvestigationSceneTests.InvestigationScene_SimulatorAnimatesStableIntoFoodWebChanges` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationSceneTests.InvestigationScene_CompletePlayerFacingWorkflow_ReachesCorrectReport` | updated | `InvestigationScene_CompleteTwoActPlayerRouteRecordsOnlySurveyEvidence` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_DifficultyAndRestartRemainAvailable` | updated | `InvestigationScene_DifficultyAndRestartPreserveTheCurrentContract` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_HypothesisSummaryReopensSavedComparisonsWithoutSpoilers` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationSceneTests.InvestigationScene_ProvisionalReviewRequiresAnExplicitChoiceConfirmation` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationSceneTests.InvestigationScene_EdnaAnswersFollowTheSelectedPredictionWithoutRepeatingTheTutorial` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationSceneTests.InvestigationScene_RovPanelRevealsFindingsAndTheCompletedReportWithoutReturningToSimulate` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationSceneTests.InvestigationScene_EdnaConversationControlsReceivePointerInputAcrossStages` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationSceneTests.InvestigationScene_ReportReviewCanReviseTheCauseWithoutLosingOtherAnswers` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationSceneTests.InvestigationScene_ReportKeepsItsDraftAndReflowsInAShortNarrowCanvas` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationSceneTests.InvestigationScene_ReportDiagnosticsRemainLocalInHardMode` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationSceneTests.InvestigationScene_ProvisionalKeyboardFocusScrollsIntoAShortViewport` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationSceneTests.InvestigationScene_ClosingStampSettlesAndRespectsReducedMotion` | updated | `InvestigationScene_ConclusionMarkSettlesAndRespectsReducedMotion` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_ReportSubmitReceivesPointerHits` | updated | `InvestigationScene_RecordConclusionReceivesPointerInputOnce` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_SimulateNotebookOpensBeforeAndAfterCaseClosure` | updated | `InvestigationScene_NotebookOpensWithPointerAndKeyboardBeforeAndAfterConclusion` — Retain the behavioural check using the current two-act controls. |
| `InvestigationSceneTests.InvestigationScene_NotebookPaperInterceptsClicksAndOutsideScrimClosesIt` | updated | `InvestigationScene_NotebookPaperInterceptsClicksAndOutsideScrimClosesIt` — Scope pointer targets to the open notebook, not the workbench paper. |
| `InvestigationSceneTests.InvestigationScene_EvidenceSelectionChecksImmediatelyWithKeyboardAndPointer` | retired | `InvestigationScenarioBriefingTests + InvestigationScenarioCardsTests + InvestigationScenarioFlowTests` — Removed floating hint/per-prediction evidence workflow; live spotlight, current controls, mode changes and playback are covered. |
| `InvestigationSceneTests.InvestigationScene_ReportNavigationOpensTheRequiredProvisionalChoice` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
| `InvestigationSceneTests.InvestigationScene_NotebookScrollsIndependentlyAndRestoresBackgroundScrolling` | updated | `InvestigationScene_NotebookScrollIsIndependentOfTheFixedWorkbench` — Retain the behavioural check using the current two-act controls. |
| `InvestigationWorkbenchTests.Workbench_ObserveQuestionsRecordAnswersAndKeepMissingSpeciesOffToday` | retired | `InvestigationObserveArrivalTests + InvestigationObserveComparisonTests + InvestigationSurveyStoryTests` — Removed sequential quiz/notebook text-entry contract; current collection, classification and visual summary have coverage. |
| `InvestigationWorkbenchTests.Workbench_ConnectionsArePlayableAndInvalidLinksDoNotUnlockExperiments` | retired | `InvestigationScenarioCardsTests + InvestigationScenarioPopulationTests + InvestigationScenarioFlowTests` — Removed connection/drag intervention desk; explicit Play/Replay, population changes, interruption and conclusion gates are covered. |
| `InvestigationWorkbenchTests.Workbench_InterventionAndPatternDropsCompleteTheInvestigationAndCanBeUndone` | retired | `InvestigationScenarioCardsTests + InvestigationScenarioPopulationTests + InvestigationScenarioFlowTests` — Removed connection/drag intervention desk; explicit Play/Replay, population changes, interruption and conclusion gates are covered. |
| `InvestigationWorkbenchTests.Workbench_RovRequiresScanningBeforeRecordingAndReportNeedsTwoDistinctClues` | retired | `InvestigationScenarioEndingTests + InvestigationDomainTests` — Removed ROV/report-question/provisional-modal flow; current summary, confirmation, export and legacy domain rules have explicit coverage. |
