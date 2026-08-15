using System;
using System.Collections.Generic;
using System.Text;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationRuntimeView : MonoBehaviour
    {
        private enum Page { CaseFiles, CompareData, BuildHypothesis, PlanSample, Conclusion }

        private static readonly Color Muted = InvestigationTheme.TextSecondary;
        private static readonly Color Warning = InvestigationTheme.Warning;
        private static readonly Color Success = InvestigationTheme.Success;

        [Header("Prefab-owned UI references")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text statusText;
        [SerializeField] private InvestigationStatusBannerView statusBanner;
        [SerializeField] private Text bodyText;
        [SerializeField] private RectTransform navigationRoot;
        [SerializeField] private RectTransform actionRoot;
        [SerializeField] private RectTransform compareNavigationRoot;
        [SerializeField] private RectTransform classificationPanel;
        [SerializeField] private RectTransform classificationRoot;
        [SerializeField] private Text classificationPromptText;
        [SerializeField] private RectTransform contentViewport;
        [SerializeField] private ScrollRect contentScrollRect;

        [Header("Reusable presentation prefabs")]
        [SerializeField] private InvestigationButtonView buttonPrefab;
        [SerializeField] private SampleComparisonBoardView comparisonBoardPrefab;
        [SerializeField] private SpeciesComparisonCardView comparisonCardPrefab;
        [SerializeField] private InvestigationCaseFilesPanelView caseFilesPanelPrefab;
        [SerializeField] private InvestigationHypothesisPanelView hypothesisPanelPrefab;
        [SerializeField] private InvestigationSamplePlannerPanelView samplePlannerPanelPrefab;
        [SerializeField] private InvestigationStepperView stepperPrefab;

        private InvestigationCaseDefinition caseDefinition;
        private InvestigationState state;
        private Action<string> selectHypothesis;
        private Action<string, string, EvidenceAssignmentKind> assignEvidence;
        private Action<string, AnomalyClaimType> identifyAnomaly;
        private Action<string, DepthBand, string, string> requestSample;
        private Action submitConclusion;
        private Action restartCase;
        private Page currentPage;
        private string statusMessage = string.Empty;
        private string selectedComparisonEvidenceId = string.Empty;
        private int speciesIndex;
        private int hypothesisIndex;
        private int evidenceIndex;
        private int siteIndex;
        private int depthIndex;
        private int resultIndex;
        private int renderedResultCount;
        private int actionSlotCount;
        private bool navigationBuilt;
        private bool preserveScrollOnNextRefresh;
        private bool comparisonActionMode;
        private int highestVisitedPage;
        private readonly List<InvestigationButtonView> navigationButtons = new List<InvestigationButtonView>();
        private SampleComparisonBoardView comparisonBoardInstance;
        private InvestigationCaseFilesPanelView caseFilesPanelInstance;
        private InvestigationHypothesisPanelView hypothesisPanelInstance;
        private InvestigationSamplePlannerPanelView samplePlannerPanelInstance;

        public void ConfigureReferences(
            Text titleReference,
            Text progressReference,
            Text statusReference,
            InvestigationStatusBannerView statusBannerReference,
            Text bodyReference,
            RectTransform navigationReference,
            RectTransform actionReference,
            RectTransform viewportReference,
            ScrollRect scrollReference,
            InvestigationButtonView buttonReference,
            SampleComparisonBoardView boardReference,
            SpeciesComparisonCardView cardReference,
            InvestigationCaseFilesPanelView caseFilesReference = null,
            InvestigationSamplePlannerPanelView samplePlannerReference = null,
            InvestigationStepperView stepperReference = null)
        {
            titleText = titleReference;
            progressText = progressReference;
            statusText = statusReference;
            statusBanner = statusBannerReference;
            bodyText = bodyReference;
            navigationRoot = navigationReference;
            actionRoot = actionReference;
            contentViewport = viewportReference;
            contentScrollRect = scrollReference;
            buttonPrefab = buttonReference;
            comparisonBoardPrefab = boardReference;
            comparisonCardPrefab = cardReference;
            caseFilesPanelPrefab = caseFilesReference;
            samplePlannerPanelPrefab = samplePlannerReference;
            stepperPrefab = stepperReference;
        }

        public void Bind(
            InvestigationCaseDefinition definition,
            Action<string> onSelectHypothesis,
            Action<string, string, EvidenceAssignmentKind> onAssignEvidence,
            Action<string, AnomalyClaimType> onIdentifyAnomaly,
            Action<string, DepthBand, string, string> onRequestSample,
            Action onSubmitConclusion,
            Action onRestartCase)
        {
            caseDefinition = definition;
            selectHypothesis = onSelectHypothesis;
            assignEvidence = onAssignEvidence;
            identifyAnomaly = onIdentifyAnomaly;
            requestSample = onRequestSample;
            submitConclusion = onSubmitConclusion;
            restartCase = onRestartCase;
            EnsureEventSystem();
            BuildNavigation();
        }

        public void Refresh(InvestigationState investigationState, string message)
        {
            bool isNewState = !ReferenceEquals(state, investigationState);
            int resultCount = investigationState == null ? 0 : investigationState.AllResults.Count;
            bool hasNewResults = !isNewState && resultCount > renderedResultCount;
            state = investigationState;
            if (isNewState)
            {
                ResetViewState();
            }
            else if (hasNewResults)
            {
                resultIndex = resultCount - 1;
                selectedComparisonEvidenceId = string.Empty;
                currentPage = Page.CompareData;
            }

            renderedResultCount = resultCount;
            statusMessage = message ?? string.Empty;
            ClampSelections();
            bool preserveScroll = preserveScrollOnNextRefresh
                && !isNewState
                && currentPage == Page.CompareData;
            preserveScrollOnNextRefresh = false;
            RenderCurrentPage(preserveScroll);
        }

        private void ResetViewState()
        {
            currentPage = Page.CaseFiles;
            highestVisitedPage = 0;
            speciesIndex = 0;
            hypothesisIndex = 0;
            evidenceIndex = 0;
            siteIndex = 0;
            depthIndex = 0;
            resultIndex = 0;
            selectedComparisonEvidenceId = string.Empty;
            preserveScrollOnNextRefresh = false;
            comparisonActionMode = false;
        }

        public void ShowFatalError(string message)
        {
            if (titleText != null) titleText.text = "INVESTIGATION UNAVAILABLE";
            if (bodyText != null)
            {
                ShowBodyContent();
                bodyText.text = message;
            }
            ShowStatus(
                "TRY AGAIN",
                "Open the Unity Console for case validation details.",
                InvestigationStatusTone.Warning);
            ClearActions();
            Debug.LogError(message);
        }

        private void BuildNavigation()
        {
            if (navigationBuilt || navigationRoot == null || buttonPrefab == null) return;
            navigationBuilt = true;
            navigationButtons.Clear();
            navigationButtons.Add(AddButton(navigationRoot, "1  CASE FILES", () => ChangePage(Page.CaseFiles), InvestigationButtonStyle.Navigation));
            navigationButtons.Add(AddButton(navigationRoot, "2  COMPARE DATA", () => ChangePage(Page.CompareData), InvestigationButtonStyle.Navigation));
            navigationButtons.Add(AddButton(navigationRoot, "3  BUILD HYPOTHESIS", () => ChangePage(Page.BuildHypothesis), InvestigationButtonStyle.Navigation));
            navigationButtons.Add(AddButton(navigationRoot, "4  PLAN SAMPLE", () => ChangePage(Page.PlanSample), InvestigationButtonStyle.Navigation));
            navigationButtons.Add(AddButton(navigationRoot, "5  CONCLUSION", () => ChangePage(Page.Conclusion), InvestigationButtonStyle.Navigation));
        }

        private void RenderCurrentPage(bool preserveContentPosition = false)
        {
            if (titleText == null || state == null || caseDefinition == null) return;
            float previousScrollPosition = preserveContentPosition && contentScrollRect != null
                ? contentScrollRect.verticalNormalizedPosition
                : 1f;
            titleText.text = $"eDNA DETECTIVES  /  {GetPageTitle()}";
            progressText.text = $"R{state.CurrentRound}    SAMPLES {state.AvailableSampleSlots}    FOUND {state.IdentifiedEvidenceIds.Count}/{state.UnlockedEvidence.Count}    MISSTEPS {state.MisclassificationCount}";
            highestVisitedPage = Mathf.Max(highestVisitedPage, (int)currentPage);
            RefreshNavigationState();
            RenderStatus();
            ClearActions();

            switch (currentPage)
            {
                case Page.CaseFiles: RenderCaseFiles(); break;
                case Page.CompareData: RenderCompareData(); break;
                case Page.BuildHypothesis: RenderHypothesisBuilder(); break;
                case Page.PlanSample: RenderSamplePlanner(); break;
                case Page.Conclusion: RenderConclusion(); break;
            }

            if (bodyText.gameObject.activeSelf) LayoutRebuilder.ForceRebuildLayoutImmediate(bodyText.rectTransform);
            if (preserveContentPosition && contentScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                contentScrollRect.StopMovement();
                contentScrollRect.verticalNormalizedPosition = previousScrollPosition;
            }
        }

        private void RenderCaseFiles()
        {
            SpeciesDefinition species = GetSelectedSpecies();
            if (caseFilesPanelPrefab == null)
            {
                ShowBodyContent();
                bodyText.text = "The case-file interface is not configured.";
                return;
            }

            ShowCaseFilesPanel();
            caseFilesPanelInstance.Bind(
                caseDefinition.DisplayName,
                caseDefinition.Briefing,
                species == null ? 0 : speciesIndex + 1,
                caseDefinition.Species.Count,
                species == null ? "No species record available" : species.DisplayName,
                species == null ? "Species details are not available." : species.Description,
                species == null ? "None recorded" : Join(species.PreferredDepths),
                species == null ? "Unknown" : species.TemperaturePreference,
                species == null ? "Unknown" : InvestigationDisplayNames.Traits(species.HabitatTags),
                species == null ? "Unknown" : InvestigationDisplayNames.Traits(species.SensitivityTags),
                "Inspect the present-day samples, compare each species with the 20-year baseline, and classify what changed. Correct findings become evidence for a testable explanation.");
            LayoutRebuilder.ForceRebuildLayoutImmediate(caseFilesPanelInstance.GetComponent<RectTransform>());
            AddBrowseButton("PREVIOUS SPECIES", () => ChangeSpecies(-1));
            AddBrowseButton("NEXT SPECIES", () => ChangeSpecies(1));
            AddStageForwardButton("START COMPARISON", () => ChangePage(Page.CompareData));
        }

        private void RenderCompareData()
        {
            if (state.AllResults.Count == 0 || comparisonBoardPrefab == null || comparisonCardPrefab == null)
            {
                ShowBodyContent();
                bodyText.text = "No sample results are available for comparison.";
                return;
            }

            ShowComparisonBoard();
            EDNAResultData result = state.AllResults[resultIndex];
            string sourceId = GetSourceId(result);
            comparisonBoardInstance.SetContent(
                $"SAMPLE {resultIndex + 1} / {state.AllResults.Count}    {SiteName(result.siteId).ToUpperInvariant()}    {result.depthBand.ToString().ToUpperInvariant()} DEPTH    QUALITY: {result.sampleQuality.ToString().ToUpperInvariant()}",
                "Compare each card with the record from 20 years ago. Select one card, then classify it with the buttons below. A dark silhouette means an expected species was not detected in this sample.",
                $"Identified findings: {state.IdentifiedEvidenceIds.Count} of {state.UnlockedEvidence.Count}. Misclassifications: {state.MisclassificationCount}. Only identified findings are available in Build Hypothesis.");

            List<string> speciesIds = BuildComparisonSpeciesIds(result, sourceId);
            for (int index = 0; index < speciesIds.Count; index++) AddSpeciesComparisonCard(result, sourceId, speciesIds[index]);
            AddWarningCards(sourceId);
            LayoutRebuilder.ForceRebuildLayoutImmediate(comparisonBoardInstance.GetComponent<RectTransform>());

            BeginComparisonActions();
            AddBrowseButton("PREVIOUS SAMPLE", () => ChangeResult(-1));
            AddBrowseButton("NEXT SAMPLE", () => ChangeResult(1));
            AddClassificationButton(AnomalyClaimType.NewArrival);
            AddClassificationButton(AnomalyClaimType.ExpectedButMissing);
            AddClassificationButton(AnomalyClaimType.DifferentDepth);
            AddClassificationButton(AnomalyClaimType.ResultWarning);
            AddClassificationButton(AnomalyClaimType.MatchesBaseline);
            AddStageForwardButton("BUILD HYPOTHESIS", () => ChangePage(Page.BuildHypothesis));
        }

        private void AddSpeciesComparisonCard(EDNAResultData result, string sourceId, string speciesId)
        {
            SpeciesDefinition species = caseDefinition.FindSpecies(speciesId);
            EvidenceRecord evidence = FindSpeciesEvidence(speciesId, sourceId);
            bool historicallyExpected = IsHistoricallyExpected(speciesId, result.siteId, result.depthBand);
            bool currentlyDetected = result.detectedSpeciesIds != null && result.detectedSpeciesIds.Contains(speciesId);
            bool missing = historicallyExpected && !currentlyDetected;
            string cardEvidenceId = evidence == null ? string.Empty : evidence.EvidenceId;
            bool identified = evidence != null && state.IsEvidenceIdentified(cardEvidenceId);
            bool selected = string.Equals(selectedComparisonEvidenceId, cardEvidenceId, StringComparison.Ordinal);
            string findingState = GetComparisonCardState(evidence, selected, identified);

            SpeciesComparisonCardView card = Instantiate(comparisonCardPrefab, comparisonBoardInstance.CardsRoot);
            card.name = $"Comparison - {(species == null ? speciesId : species.DisplayName)}";
            card.Bind(
                species == null ? speciesId : species.DisplayName,
                missing ? "FISH\nSILHOUETTE" : "eDNA\nDETECTED",
                historicallyExpected ? $"20 YEARS AGO\nExpected at {result.depthBand} depth" : $"20 YEARS AGO\nNot recorded at {result.depthBand} depth",
                currentlyDetected ? "CURRENT SAMPLE\nDetected" : "CURRENT SAMPLE\nNot detected",
                species == null ? "Species details are not available." : BuildSpeciesTraits(species),
                findingState,
                missing,
                selected,
                identified,
                evidence != null,
                () => SelectComparisonEvidence(cardEvidenceId));
        }

        private void AddWarningCards(string sourceId)
        {
            for (int index = 0; index < state.UnlockedEvidence.Count; index++)
            {
                EvidenceRecord evidence = state.UnlockedEvidence[index];
                if (!HasSource(evidence, sourceId) || (evidence.EvidenceType != EvidenceType.LowQualityResult && evidence.EvidenceType != EvidenceType.ContaminationWarning)) continue;

                string evidenceId = evidence.EvidenceId;
                bool identified = state.IsEvidenceIdentified(evidenceId);
                bool selected = string.Equals(selectedComparisonEvidenceId, evidenceId, StringComparison.Ordinal);
                SpeciesComparisonCardView card = Instantiate(comparisonCardPrefab, comparisonBoardInstance.CardsRoot);
                card.name = $"Comparison - {evidence.EvidenceType}";
                card.Bind(
                    evidence.EvidenceType == EvidenceType.ContaminationWarning ? "Contamination Check" : "Sample Quality Check",
                    "!",
                    "20 YEARS AGO\nNo laboratory warning",
                    $"CURRENT SAMPLE\n{evidence.DisplayText}",
                    $"Why it matters: {evidence.ConfidenceReason}",
                    GetComparisonCardState(evidence, selected, identified),
                    false,
                    selected,
                    identified,
                    true,
                    () => SelectComparisonEvidence(evidenceId));
            }
        }

        private void RenderHypothesisBuilder()
        {
            HypothesisDefinition hypothesis = GetSelectedHypothesis();
            if (hypothesis == null || hypothesisPanelPrefab == null)
            {
                ShowBodyContent();
                StringBuilder unavailable = new StringBuilder();
                AppendSectionHeading(unavailable, "WORKING HYPOTHESIS");
                AppendBody(unavailable, "No hypothesis definitions are available for this case.");
                bodyText.text = unavailable.ToString();
                return;
            }

            ShowHypothesisPanel();
            List<EvidenceRecord> identifiedEvidence = state.GetIdentifiedEvidence();
            EvidenceRecord evidence = GetSelectedEvidence();
            HypothesisEvaluation evaluation = new HypothesisEvaluator().Evaluate(hypothesis, state);
            string findingTitle = evidence == null
                ? "No identified finding yet"
                : $"FINDING {evidenceIndex + 1} OF {identifiedEvidence.Count}  ·  {evidence.DisplayText}";
            string findingDescription = evidence == null
                ? "Return to Compare Data and correctly classify a comparison card. Identified findings will appear here."
                : evidence.ConfidenceReason;
            string findingMetadata = evidence == null
                ? string.Empty
                : $"Confidence: {InvestigationDisplayNames.Confidence(evidence.Confidence)}    Current assignment: {InvestigationDisplayNames.FormatIdentifier(DescribeAssignment(evidence.EvidenceId, hypothesis.HypothesisId))}";

            string currentAssignment = evidence == null
                ? "None"
                : DescribeAssignment(evidence.EvidenceId, hypothesis.HypothesisId);
            hypothesisPanelInstance.Bind(
                hypothesisIndex + 1,
                caseDefinition.Hypotheses.Count,
                hypothesis.DisplayName,
                hypothesis.Explanation,
                evaluation.Status,
                evaluation.SupportingEvidenceCount,
                evaluation.OpposingEvidenceCount,
                InvestigationDisplayNames.EvidencePatterns(hypothesis.RequiredEvidenceTags),
                InvestigationDisplayNames.Confidence(hypothesis.MinimumConfidence),
                string.Equals(state.SelectedHypothesisId, hypothesis.HypothesisId, StringComparison.Ordinal),
                findingTitle,
                findingDescription,
                findingMetadata,
                currentAssignment);
            AddStepper(
                hypothesisPanelInstance.SelectorRoot,
                "Theory",
                hypothesis.DisplayName,
                hypothesisIndex,
                caseDefinition.Hypotheses.Count,
                () => ChangeHypothesis(-1),
                () => ChangeHypothesis(1));
            AddStepper(
                hypothesisPanelInstance.SelectorRoot,
                "Finding",
                evidence == null ? "No identified finding yet" : evidence.DisplayText,
                evidenceIndex,
                identifiedEvidence.Count,
                () => ChangeEvidence(-1),
                () => ChangeEvidence(1));
            LayoutRebuilder.ForceRebuildLayoutImmediate(hypothesisPanelInstance.GetComponent<RectTransform>());

            AddStageBackButton("COMPARE DATA", () => ChangePage(Page.CompareData));
            bool canAssignFinding = evidence != null;
            AddActionSlotButton("ASSIGN SUPPORT", () => AssignSelected(EvidenceAssignmentKind.Supports), InvestigationButtonStyle.Support, canAssignFinding);
            AddActionSlotButton("ASSIGN CHALLENGE", () => AssignSelected(EvidenceAssignmentKind.Opposes), InvestigationButtonStyle.Challenge, canAssignFinding);
            AddCommitButton("SELECT THEORY  >", SelectCurrentHypothesisAndPlanSample, hypothesis != null);
        }

        private void RenderSamplePlanner()
        {
            if (samplePlannerPanelPrefab == null || stepperPrefab == null)
            {
                ShowBodyContent();
                bodyText.text = "The sample planning interface is not configured.";
                return;
            }
            ShowSamplePlannerPanel();
            SampleSiteDefinition site = GetSelectedSite();
            DepthBand depth = GetSelectedDepth();
            HypothesisDefinition hypothesis = GetSelectedHypothesis();
            EvidenceRecord evidence = GetSelectedEvidence();
            List<EvidenceRecord> identifiedEvidence = state.GetIdentifiedEvidence();
            samplePlannerPanelInstance.Bind(
                site == null ? string.Empty : site.DisplayName,
                site == null ? string.Empty : site.Description,
                hypothesis == null ? string.Empty : hypothesis.DisplayName,
                state.AvailableSampleSlots,
                state.PendingSampleCount);
            AddStepper(
                samplePlannerPanelInstance.SelectorRoot,
                "Site",
                site == null ? "No site available" : site.DisplayName,
                siteIndex,
                caseDefinition.SampleSites.Count,
                () => ChangeSite(-1),
                () => ChangeSite(1));
            AddStepper(
                samplePlannerPanelInstance.SelectorRoot,
                "Depth",
                InvestigationDisplayNames.FormatIdentifier(depth.ToString()),
                depthIndex,
                site == null ? 0 : site.AvailableDepths.Count,
                () => ChangeDepth(-1),
                () => ChangeDepth(1));
            AddStepper(
                samplePlannerPanelInstance.SelectorRoot,
                "Test target",
                evidence == null ? "General investigation" : evidence.DisplayText,
                evidenceIndex,
                identifiedEvidence.Count,
                () => ChangeEvidence(-1),
                () => ChangeEvidence(1));
            LayoutRebuilder.ForceRebuildLayoutImmediate(samplePlannerPanelInstance.GetComponent<RectTransform>());
            AddStageBackButton("COMPARE RESULTS", () => ChangePage(Page.CompareData));
            bool canCollectSample = site != null
                && site.AvailableDepths.Count > 0
                && state.AvailableSampleSlots > 0;
            AddStageCommitButton("COLLECT SAMPLE  >", RequestSelectedSample, canCollectSample);
        }

        private void RenderConclusion()
        {
            ShowBodyContent();
            StringBuilder text = new StringBuilder();
            AppendSectionHeading(text, "CASE CONCLUSION");
            ConclusionEvaluator conclusionEvaluator = new ConclusionEvaluator(new HypothesisEvaluator());
            ConclusionReadiness readiness = conclusionEvaluator.EvaluateReadiness(caseDefinition, state);
            HypothesisDefinition selected = readiness.SelectedHypothesis;
            HypothesisEvaluation evaluation = readiness.HypothesisEvaluation;
            if (selected == null)
            {
                AppendTitle(text, "No theory selected");
                AppendBody(text, "Choose a working hypothesis on the Build Hypothesis page.");
            }
            else
            {
                AppendTitle(text, selected.DisplayName);
                AppendBody(text, evaluation.Explanation);
                AppendMetadata(
                    text,
                    $"Evidence status: {InvestigationDisplayNames.HypothesisStatus(evaluation.Status)}    " +
                    $"Support: {evaluation.SupportingEvidenceCount}    Challenge / uncertainty: {evaluation.OpposingEvidenceCount}");
            }
            AppendSectionHeading(text, "CASE CHECKLIST");
            AppendChecklistItem(
                text,
                readiness.HasSelectedHypothesis,
                "Theory selected",
                selected == null ? "Not selected" : selected.DisplayName);
            AppendChecklistItem(
                text,
                readiness.HasSupportedHypothesis,
                "Evidence supports it",
                evaluation == null
                    ? "Not evaluated"
                    : InvestigationDisplayNames.HypothesisStatus(evaluation.Status));
            AppendChecklistItem(
                text,
                readiness.HasRequiredFollowUpSample,
                "Follow-up sample completed",
                caseDefinition.RequireFollowUpSample
                    ? state.CompletedSampleCount.ToString()
                    : "Not required");
            AppendChecklistItem(
                text,
                readiness.HasRequiredOpposingEvidence,
                "At least one challenge assigned",
                caseDefinition.RequiredOpposingEvidence == 0
                    ? "Not required"
                    : $"{(evaluation == null ? 0 : evaluation.OpposingEvidenceCount)} / {caseDefinition.RequiredOpposingEvidence}");
            AppendMetadata(
                text,
                $"Case record: {state.MisclassificationCount} misclassification(s)    " +
                $"Submission: {InvestigationDisplayNames.ConclusionStatus(state.ConclusionStatus)}");
            bodyText.text = text.ToString();
            PadActionsToColumn(2);
            AddDestructiveButton("RESTART CASE", () => restartCase?.Invoke());
            AddCommitButton("SUBMIT CONCLUSION  >", () => submitConclusion?.Invoke(), readiness.CanSubmit);
        }

        private List<string> BuildComparisonSpeciesIds(EDNAResultData result, string sourceId)
        {
            List<string> speciesIds = new List<string>();
            for (int index = 0; index < caseDefinition.HistoricalBaseline.Count; index++)
            {
                HistoricalRecordDefinition record = caseDefinition.HistoricalBaseline[index];
                if (record != null && record.expectedPresence && string.Equals(record.siteId, result.siteId, StringComparison.Ordinal) && record.depthBand == result.depthBand) AddUnique(speciesIds, record.speciesId);
            }
            if (result.detectedSpeciesIds != null)
            {
                for (int index = 0; index < result.detectedSpeciesIds.Count; index++) AddUnique(speciesIds, result.detectedSpeciesIds[index]);
            }
            for (int index = 0; index < state.UnlockedEvidence.Count; index++)
            {
                EvidenceRecord evidence = state.UnlockedEvidence[index];
                if (evidence.EvidenceType != EvidenceType.DepthShift || !HasSource(evidence, sourceId)) continue;
                for (int speciesIndex = 0; speciesIndex < evidence.RelatedSpeciesIds.Count; speciesIndex++) AddUnique(speciesIds, evidence.RelatedSpeciesIds[speciesIndex]);
            }
            return speciesIds;
        }

        private EvidenceRecord FindSpeciesEvidence(string speciesId, string sourceId)
        {
            EvidenceRecord best = null;
            int bestPriority = -1;
            for (int index = 0; index < state.UnlockedEvidence.Count; index++)
            {
                EvidenceRecord evidence = state.UnlockedEvidence[index];
                if (!HasSource(evidence, sourceId) || !Contains(evidence.RelatedSpeciesIds, speciesId) || evidence.EvidenceType == EvidenceType.LowQualityResult || evidence.EvidenceType == EvidenceType.ContaminationWarning) continue;
                int priority = GetEvidencePriority(evidence.EvidenceType);
                if (priority > bestPriority) { best = evidence; bestPriority = priority; }
            }
            return best;
        }

        private bool IsHistoricallyExpected(string speciesId, string siteId, DepthBand depthBand)
        {
            for (int index = 0; index < caseDefinition.HistoricalBaseline.Count; index++)
            {
                HistoricalRecordDefinition record = caseDefinition.HistoricalBaseline[index];
                if (record != null && record.expectedPresence && string.Equals(record.speciesId, speciesId, StringComparison.Ordinal) && string.Equals(record.siteId, siteId, StringComparison.Ordinal) && record.depthBand == depthBand) return true;
            }
            return false;
        }

        private void ShowBodyContent()
        {
            DestroyComparisonBoard();
            DestroyCaseFilesPanel();
            DestroyHypothesisPanel();
            DestroySamplePlannerPanel();
            bodyText.gameObject.SetActive(true);
            bodyText.supportRichText = true;
            contentScrollRect.content = bodyText.rectTransform;
            contentScrollRect.verticalNormalizedPosition = 1f;
        }

        private void ShowComparisonBoard()
        {
            DestroyComparisonBoard();
            DestroyCaseFilesPanel();
            DestroyHypothesisPanel();
            DestroySamplePlannerPanel();
            bodyText.gameObject.SetActive(false);
            comparisonBoardInstance = Instantiate(comparisonBoardPrefab, contentViewport);
            RectTransform boardRect = comparisonBoardInstance.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0f, 1f);
            boardRect.anchorMax = new Vector2(1f, 1f);
            boardRect.pivot = new Vector2(0.5f, 1f);
            boardRect.anchoredPosition = Vector2.zero;
            boardRect.sizeDelta = Vector2.zero;
            comparisonBoardInstance.ClearCards();
            contentScrollRect.content = boardRect;
            contentScrollRect.verticalNormalizedPosition = 1f;
        }

        private void ShowHypothesisPanel()
        {
            DestroyComparisonBoard();
            DestroyCaseFilesPanel();
            DestroyHypothesisPanel();
            DestroySamplePlannerPanel();
            bodyText.gameObject.SetActive(false);
            hypothesisPanelInstance = Instantiate(hypothesisPanelPrefab, contentViewport);
            RectTransform panelRect = hypothesisPanelInstance.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;
            contentScrollRect.content = panelRect;
            contentScrollRect.verticalNormalizedPosition = 1f;
        }

        private void ShowSamplePlannerPanel()
        {
            DestroyComparisonBoard();
            DestroyCaseFilesPanel();
            DestroyHypothesisPanel();
            DestroySamplePlannerPanel();
            bodyText.gameObject.SetActive(false);
            samplePlannerPanelInstance = Instantiate(samplePlannerPanelPrefab, contentViewport);
            RectTransform panelRect = samplePlannerPanelInstance.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;
            contentScrollRect.content = panelRect;
            contentScrollRect.verticalNormalizedPosition = 1f;
        }

        private void DestroyComparisonBoard()
        {
            if (comparisonBoardInstance == null) return;
            comparisonBoardInstance.gameObject.SetActive(false);
            Destroy(comparisonBoardInstance.gameObject);
            comparisonBoardInstance = null;
        }

        private void ShowCaseFilesPanel()
        {
            DestroyComparisonBoard();
            DestroyCaseFilesPanel();
            DestroyHypothesisPanel();
            DestroySamplePlannerPanel();
            bodyText.gameObject.SetActive(false);
            caseFilesPanelInstance = Instantiate(caseFilesPanelPrefab, contentViewport);
            RectTransform panelRect = caseFilesPanelInstance.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;
            contentScrollRect.content = panelRect;
            contentScrollRect.verticalNormalizedPosition = 1f;
        }

        private void DestroyCaseFilesPanel()
        {
            if (caseFilesPanelInstance == null) return;
            caseFilesPanelInstance.gameObject.SetActive(false);
            Destroy(caseFilesPanelInstance.gameObject);
            caseFilesPanelInstance = null;
        }

        private void DestroyHypothesisPanel()
        {
            if (hypothesisPanelInstance == null) return;
            hypothesisPanelInstance.gameObject.SetActive(false);
            Destroy(hypothesisPanelInstance.gameObject);
            hypothesisPanelInstance = null;
        }

        private void DestroySamplePlannerPanel()
        {
            if (samplePlannerPanelInstance == null) return;
            samplePlannerPanelInstance.gameObject.SetActive(false);
            Destroy(samplePlannerPanelInstance.gameObject);
            samplePlannerPanelInstance = null;
        }

        private void AddStepper(
            RectTransform parent,
            string category,
            string value,
            int selectedIndex,
            int itemCount,
            Action onPrevious,
            Action onNext)
        {
            if (parent == null || stepperPrefab == null) return;
            InvestigationStepperView stepper = Instantiate(stepperPrefab, parent);
            stepper.name = $"{category} Stepper";
            stepper.Bind(category, value, selectedIndex, itemCount, onPrevious, onNext);
        }

        private void AddClassificationButton(AnomalyClaimType claimType)
        {
            bool hasSelection = !string.IsNullOrEmpty(selectedComparisonEvidenceId);
            bool alreadyIdentified = hasSelection && state.IsEvidenceIdentified(selectedComparisonEvidenceId);
            bool ruledOut = hasSelection && state.HasRejectedClassification(selectedComparisonEvidenceId, claimType);
            string label = InvestigationDisplayNames.Classification(claimType).ToUpperInvariant();
            string visibleLabel = ruledOut ? $"RULED OUT: {label}" : label;
            AddButton(
                classificationRoot == null ? ActiveActionRoot : classificationRoot,
                visibleLabel,
                () => ClassifySelected(claimType),
                InvestigationButtonStyle.Primary,
                hasSelection && !alreadyIdentified && !ruledOut);
        }

        private void BeginComparisonActions()
        {
            if (compareNavigationRoot == null || classificationPanel == null || classificationRoot == null)
            {
                comparisonActionMode = false;
                return;
            }

            comparisonActionMode = true;
            actionSlotCount = 0;
            if (actionRoot != null) actionRoot.gameObject.SetActive(false);
            compareNavigationRoot.gameObject.SetActive(true);
            classificationPanel.gameObject.SetActive(true);
            if (classificationPromptText != null)
            {
                classificationPromptText.text = "CLASSIFY THIS CARD";
            }
        }

        private void AddActionButton(string label, Action action, bool isInteractable = true)
        {
            AddActionSlotButton(label, action, InvestigationButtonStyle.Primary, isInteractable);
        }

        private void AddCommitButton(string label, Action action, bool isInteractable = true)
        {
            AddActionSlotButton(label, action, InvestigationButtonStyle.Commit, isInteractable);
        }

        private void AddDestructiveButton(string label, Action action, bool isInteractable = true)
        {
            AddActionSlotButton(label, action, InvestigationButtonStyle.Destructive, isInteractable);
        }

        private void AddStageCommitButton(string label, Action action, bool isInteractable = true)
        {
            PadActionsToColumn(GetActionColumnCount() - 1);
            AddCommitButton(label, action, isInteractable);
        }

        private void AddBrowseButton(string label, Action action, bool isInteractable = true)
        {
            string directionalLabel = label.StartsWith("PREVIOUS", StringComparison.Ordinal)
                ? $"<  {label}"
                : label.StartsWith("NEXT", StringComparison.Ordinal)
                    ? $"{label}  >"
                    : label;
            AddActionSlotButton(directionalLabel, action, InvestigationButtonStyle.Browse, isInteractable);
        }

        private void AddStageBackButton(string label, Action action, bool isInteractable = true)
        {
            PadActionsToColumn(0);
            AddActionSlotButton(label, action, InvestigationButtonStyle.Primary, isInteractable);
        }

        private void AddStageForwardButton(string label, Action action, bool isInteractable = true)
        {
            PadActionsToColumn(GetActionColumnCount() - 1);
            AddActionSlotButton(label, action, InvestigationButtonStyle.Primary, isInteractable);
        }

        private void AddActionSlotButton(
            string label,
            Action action,
            InvestigationButtonStyle style,
            bool isInteractable)
        {
            AddButton(ActiveActionRoot, label, action, style, isInteractable);
            actionSlotCount++;
        }

        private void PadActionsToColumn(int targetColumn)
        {
            int columnCount = GetActionColumnCount();
            int safeTarget = Mathf.Clamp(targetColumn, 0, columnCount - 1);
            while (actionSlotCount % columnCount != safeTarget)
            {
                GameObject spacer = new GameObject("Action Spacer", typeof(RectTransform));
                spacer.transform.SetParent(ActiveActionRoot, false);
                actionSlotCount++;
            }
        }

        private int GetActionColumnCount()
        {
            RectTransform activeRoot = ActiveActionRoot;
            if (activeRoot == null) return 1;
            GridLayoutGroup grid = activeRoot.GetComponent<GridLayoutGroup>();
            return grid != null
                && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.Max(1, grid.constraintCount)
                : 1;
        }

        private RectTransform ActiveActionRoot => comparisonActionMode && compareNavigationRoot != null
            ? compareNavigationRoot
            : actionRoot;

        private InvestigationButtonView AddButton(
            Transform parent,
            string label,
            Action action,
            InvestigationButtonStyle style,
            bool isInteractable = true)
        {
            if (parent == null || buttonPrefab == null) return null;
            InvestigationButtonView button = Instantiate(buttonPrefab, parent);
            button.name = label;
            button.Bind(label, action, style, isInteractable);
            return button;
        }

        private void ChangePage(Page page) { currentPage = page; highestVisitedPage = Mathf.Max(highestVisitedPage, (int)page); statusMessage = string.Empty; RenderCurrentPage(); }

        private void RefreshNavigationState()
        {
            for (int index = 0; index < navigationButtons.Count; index++)
            {
                InvestigationButtonView button = navigationButtons[index];
                if (button == null) continue;
                button.SetNavigationState(
                    index == (int)currentPage,
                    index < highestVisitedPage,
                    GetNavigationGlyph(index));
            }
        }

        private static InvestigationGlyph GetNavigationGlyph(int index)
        {
            switch (index)
            {
                case 0: return InvestigationGlyph.CaseFile;
                case 1: return InvestigationGlyph.Compare;
                case 2: return InvestigationGlyph.Hypothesis;
                case 3: return InvestigationGlyph.Sample;
                case 4: return InvestigationGlyph.Conclusion;
                default: return InvestigationGlyph.None;
            }
        }
        private void ChangeSpecies(int delta) { speciesIndex = Wrap(speciesIndex + delta, caseDefinition.Species.Count); RenderCurrentPage(); }
        private void ChangeHypothesis(int delta) { hypothesisIndex = Wrap(hypothesisIndex + delta, caseDefinition.Hypotheses.Count); RenderCurrentPage(); }
        private void ChangeEvidence(int delta) { evidenceIndex = Wrap(evidenceIndex + delta, state.GetIdentifiedEvidence().Count); RenderCurrentPage(); }
        private void ChangeSite(int delta) { siteIndex = Wrap(siteIndex + delta, caseDefinition.SampleSites.Count); depthIndex = 0; RenderCurrentPage(); }
        private void ChangeDepth(int delta) { SampleSiteDefinition site = GetSelectedSite(); depthIndex = Wrap(depthIndex + delta, site == null ? 0 : site.AvailableDepths.Count); RenderCurrentPage(); }

        private void ChangeResult(int delta)
        {
            resultIndex = Wrap(resultIndex + delta, state.AllResults.Count);
            selectedComparisonEvidenceId = string.Empty;
            statusMessage = string.Empty;
            RenderCurrentPage();
        }

        private void SelectComparisonEvidence(string evidenceId)
        {
            selectedComparisonEvidenceId = evidenceId;
            statusMessage = "Comparison selected. Choose the classification that best describes the change.";
            RenderCurrentPage(true);
        }

        private void ClassifySelected(AnomalyClaimType claimType)
        {
            if (string.IsNullOrEmpty(selectedComparisonEvidenceId))
            {
                statusMessage = "Select a species or warning card before classifying it.";
                RenderCurrentPage(true);
                return;
            }
            preserveScrollOnNextRefresh = true;
            identifyAnomaly?.Invoke(selectedComparisonEvidenceId, claimType);
            preserveScrollOnNextRefresh = false;
        }

        private void AssignSelected(EvidenceAssignmentKind kind)
        {
            HypothesisDefinition hypothesis = GetSelectedHypothesis();
            EvidenceRecord evidence = GetSelectedEvidence();
            if (hypothesis == null || evidence == null)
            {
                statusMessage = "Choose a hypothesis and identify a finding first.";
                RenderCurrentPage();
                return;
            }
            assignEvidence?.Invoke(evidence.EvidenceId, hypothesis.HypothesisId, kind);
        }

        private void SelectCurrentHypothesisAndPlanSample()
        {
            HypothesisDefinition hypothesis = GetSelectedHypothesis();
            if (hypothesis == null) return;
            currentPage = Page.PlanSample;
            statusMessage = string.Empty;
            if (selectHypothesis != null)
            {
                selectHypothesis.Invoke(hypothesis.HypothesisId);
            }
            else
            {
                RenderCurrentPage();
            }
        }

        private void RequestSelectedSample()
        {
            if (state.AvailableSampleSlots <= 0)
            {
                statusMessage = "No follow-up sample slots remain.";
                RenderCurrentPage();
                return;
            }
            SampleSiteDefinition site = GetSelectedSite();
            if (site == null) { statusMessage = "Choose a valid sample site."; RenderCurrentPage(); return; }
            HypothesisDefinition hypothesis = GetSelectedHypothesis();
            EvidenceRecord evidence = GetSelectedEvidence();
            requestSample?.Invoke(site.SiteId, GetSelectedDepth(), hypothesis?.HypothesisId ?? string.Empty, evidence?.EvidenceId ?? string.Empty);
        }

        private void ClampSelections()
        {
            speciesIndex = ClampIndex(speciesIndex, caseDefinition.Species.Count);
            hypothesisIndex = ClampIndex(hypothesisIndex, caseDefinition.Hypotheses.Count);
            evidenceIndex = ClampIndex(evidenceIndex, state.GetIdentifiedEvidence().Count);
            siteIndex = ClampIndex(siteIndex, caseDefinition.SampleSites.Count);
            resultIndex = ClampIndex(resultIndex, state.AllResults.Count);
            SampleSiteDefinition site = GetSelectedSite();
            depthIndex = ClampIndex(depthIndex, site == null ? 0 : site.AvailableDepths.Count);
        }

        private SpeciesDefinition GetSelectedSpecies() { return caseDefinition.Species.Count == 0 ? null : caseDefinition.Species[speciesIndex]; }
        private HypothesisDefinition GetSelectedHypothesis() { return caseDefinition.Hypotheses.Count == 0 ? null : caseDefinition.Hypotheses[hypothesisIndex]; }
        private SampleSiteDefinition GetSelectedSite() { return caseDefinition.SampleSites.Count == 0 ? null : caseDefinition.SampleSites[siteIndex]; }

        private EvidenceRecord GetSelectedEvidence()
        {
            if (state == null) return null;
            List<EvidenceRecord> identified = state.GetIdentifiedEvidence();
            return identified.Count == 0 ? null : identified[evidenceIndex];
        }

        private DepthBand GetSelectedDepth()
        {
            SampleSiteDefinition site = GetSelectedSite();
            return site == null || site.AvailableDepths.Count == 0 ? DepthBand.Shallow : site.AvailableDepths[depthIndex];
        }

        private string DescribeAssignment(string evidenceId, string hypothesisId)
        {
            if (string.IsNullOrEmpty(hypothesisId)) return "None";
            for (int index = 0; index < state.EvidenceAssignments.Count; index++)
            {
                EvidenceAssignmentRecord assignment = state.EvidenceAssignments[index];
                if (!string.Equals(assignment.EvidenceId, evidenceId, StringComparison.Ordinal)
                    || !string.Equals(assignment.HypothesisId, hypothesisId, StringComparison.Ordinal)) continue;
                return assignment.AssignmentKind == EvidenceAssignmentKind.Supports ? "Support" : "Challenge";
            }
            return "None";
        }

        private string GetPageTitle()
        {
            switch (currentPage)
            {
                case Page.CaseFiles: return "CASE FILES";
                case Page.CompareData: return "COMPARE DATA";
                case Page.BuildHypothesis: return "BUILD HYPOTHESIS";
                case Page.PlanSample: return "PLAN SAMPLE";
                case Page.Conclusion: return "CONCLUSION";
                default: return "INVESTIGATION";
            }
        }

        private string SiteName(string siteId)
        {
            SampleSiteDefinition site = caseDefinition.FindSite(siteId);
            return site == null ? siteId : site.DisplayName;
        }

        private static string GetSourceId(EDNAResultData result) { return string.IsNullOrEmpty(result.sampleId) ? result.requestId : result.sampleId; }

        private static string GetClaimName(EvidenceType type)
        {
            return InvestigationDisplayNames.EvidencePattern(type.ToString());
        }

        private static int GetEvidencePriority(EvidenceType type)
        {
            switch (type)
            {
                case EvidenceType.DepthShift: return 50;
                case EvidenceType.NewDetection: return 40;
                case EvidenceType.RepeatedNonDetection: return 35;
                case EvidenceType.NotDetectedInSample: return 30;
                case EvidenceType.RepeatedDetection: return 20;
                case EvidenceType.StableIndicator: return 10;
                default: return 0;
            }
        }

        private static bool HasSource(EvidenceRecord evidence, string sourceId) { return Contains(evidence.SourceSampleIds, sourceId); }

        private static bool Contains(IReadOnlyList<string> values, string target)
        {
            if (values == null) return false;
            for (int index = 0; index < values.Count; index++) if (string.Equals(values[index], target, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (!string.IsNullOrEmpty(value) && !values.Contains(value)) values.Add(value);
        }

        private static string Join<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0) return "None";
            string[] strings = new string[values.Count];
            for (int index = 0; index < values.Count; index++) strings[index] = values[index] == null ? string.Empty : values[index].ToString();
            return string.Join(", ", strings);
        }

        private static string BuildSpeciesTraits(SpeciesDefinition species)
        {
            if (species == null) return "Species details are not available.";

            StringBuilder traits = new StringBuilder();
            AppendInlineTrait(traits, species.TemperaturePreference);
            AppendInlineTrait(traits, Join(species.PreferredDepths));
            AppendInlineTrait(traits, InvestigationDisplayNames.Traits(species.HabitatTags));
            AppendInlineTrait(traits, InvestigationDisplayNames.Traits(species.SensitivityTags));
            return traits.Length == 0 ? "No species traits recorded." : traits.ToString();
        }

        private static void AppendInlineTrait(StringBuilder text, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)) return;
            if (text.Length > 0) text.Append("  ·  ");
            text.Append(value);
        }

        private static void AppendSectionHeading(StringBuilder text, string value)
        {
            if (text.Length > 0) text.AppendLine();
            text.Append("<size=20><b><color=#F5E6BE>");
            text.Append(value.ToUpperInvariant());
            text.AppendLine("</color></b></size>");
        }

        private static void AppendTitle(StringBuilder text, string value)
        {
            text.Append("<size=18><b><color=#F5E6BE>");
            text.Append(value);
            text.AppendLine("</color></b></size>");
        }

        private static void AppendBody(StringBuilder text, string value)
        {
            text.Append("<size=16><color=#A9C9CF>");
            text.Append(value);
            text.AppendLine("</color></size>");
        }

        private static void AppendMetadata(StringBuilder text, string value)
        {
            text.Append("<size=13><color=#8EAEB5>");
            text.Append(value);
            text.AppendLine("</color></size>");
        }

        private static void AppendChecklistItem(
            StringBuilder text,
            bool isComplete,
            string label,
            string value)
        {
            text.Append("<size=16><b><color=");
            text.Append(isComplete ? "#5CD69D>✓  " : "#FFBE5A>✗  ");
            text.Append(label);
            text.Append("</color></b><color=#A9C9CF>    ");
            text.Append(value);
            text.AppendLine("</color></size>");
        }

        private void ClearActions()
        {
            actionSlotCount = 0;
            comparisonActionMode = false;
            ClearActionRoot(actionRoot);
            ClearActionRoot(compareNavigationRoot);
            ClearActionRoot(classificationRoot);
            if (actionRoot != null) actionRoot.gameObject.SetActive(true);
            if (compareNavigationRoot != null) compareNavigationRoot.gameObject.SetActive(false);
            if (classificationPanel != null) classificationPanel.gameObject.SetActive(false);
        }

        private static void ClearActionRoot(RectTransform root)
        {
            if (root == null) return;
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                GameObject child = root.GetChild(index).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        private static bool IsWarningMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;
            if (message.IndexOf("No incorrect classifications", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return message.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("does not", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("incorrect", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("ruled out", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("before", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsIdentifiedMessage(string message)
        {
            return !string.IsNullOrEmpty(message)
                && message.StartsWith("Finding identified:", StringComparison.OrdinalIgnoreCase);
        }

        private void RenderStatus()
        {
            string message = string.IsNullOrWhiteSpace(statusMessage)
                ? GetDefaultStatusMessage()
                : statusMessage;
            InvestigationStatusTone tone = IsIdentifiedMessage(message)
                ? InvestigationStatusTone.Success
                : IsWarningMessage(message)
                    ? InvestigationStatusTone.Warning
                    : InvestigationStatusTone.Guide;
            string label = tone == InvestigationStatusTone.Warning
                ? "TRY AGAIN"
                : tone == InvestigationStatusTone.Success
                    ? "FINDING IDENTIFIED"
                    : "NEXT STEP";
            ShowStatus(label, message, tone);
        }

        private void ShowStatus(string label, string message, InvestigationStatusTone tone)
        {
            if (statusBanner != null)
            {
                statusBanner.Show(label, message, tone);
                return;
            }

            if (statusText == null) return;
            statusText.text = $"{label}: {message}";
            statusText.color = tone == InvestigationStatusTone.Warning
                ? Warning
                : tone == InvestigationStatusTone.Success
                    ? Success
                    : Muted;
        }

        private string GetDefaultStatusMessage()
        {
            switch (currentPage)
            {
                case Page.CompareData:
                    return "Select a comparison card, then classify the change with the buttons below.";
                case Page.BuildHypothesis:
                    return "Use the in-panel selectors, assign the finding, then select a theory to continue.";
                case Page.PlanSample:
                    return "Choose a site, depth, and test target with the in-panel selectors.";
                case Page.Conclusion:
                    return "Complete every checklist item, then submit your conclusion.";
                default:
                    return "Review the case briefing and species records, then start the comparison.";
            }
        }

        private static string GetComparisonCardState(
            EvidenceRecord evidence,
            bool isSelected,
            bool isIdentified)
        {
            if (evidence == null)
            {
                return "REFERENCE CARD - NO CLASSIFICATION REQUIRED";
            }

            if (isIdentified)
            {
                return $"IDENTIFIED: {GetClaimName(evidence.EvidenceType).ToUpperInvariant()}";
            }

            return isSelected
                ? "SELECTED - CHOOSE A CLASSIFICATION BELOW"
                : "SELECT THIS CARD TO CLASSIFY";
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static int Wrap(int value, int count)
        {
            if (count <= 0) return 0;
            int wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static int ClampIndex(int value, int count) { return count <= 0 ? 0 : Mathf.Clamp(value, 0, count - 1); }
    }
}
