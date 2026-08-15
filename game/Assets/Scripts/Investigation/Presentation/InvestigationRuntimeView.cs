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

        private static readonly Color Muted = new Color32(169, 201, 207, 255);
        private static readonly Color Warning = new Color32(255, 190, 90, 255);
        private static readonly Color Success = new Color32(92, 214, 157, 255);

        [Header("Prefab-owned UI references")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text statusText;
        [SerializeField] private InvestigationStatusBannerView statusBanner;
        [SerializeField] private Text bodyText;
        [SerializeField] private RectTransform navigationRoot;
        [SerializeField] private RectTransform actionRoot;
        [SerializeField] private RectTransform contentViewport;
        [SerializeField] private ScrollRect contentScrollRect;

        [Header("Reusable presentation prefabs")]
        [SerializeField] private InvestigationButtonView buttonPrefab;
        [SerializeField] private SampleComparisonBoardView comparisonBoardPrefab;
        [SerializeField] private SpeciesComparisonCardView comparisonCardPrefab;

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
        private int actionSlotCount;
        private bool navigationBuilt;
        private bool preserveScrollOnNextRefresh;
        private SampleComparisonBoardView comparisonBoardInstance;

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
            SpeciesComparisonCardView cardReference)
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
            int previousResultCount = state == null ? 0 : state.AllResults.Count;
            bool isNewState = !ReferenceEquals(state, investigationState);
            state = investigationState;
            if (isNewState)
            {
                resultIndex = 0;
                selectedComparisonEvidenceId = string.Empty;
            }
            else if (state != null && state.AllResults.Count > previousResultCount)
            {
                resultIndex = state.AllResults.Count - 1;
                selectedComparisonEvidenceId = string.Empty;
                currentPage = Page.CompareData;
            }

            statusMessage = message ?? string.Empty;
            ClampSelections();
            bool preserveScroll = preserveScrollOnNextRefresh
                && !isNewState
                && currentPage == Page.CompareData;
            preserveScrollOnNextRefresh = false;
            RenderCurrentPage(preserveScroll);
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
            AddButton(navigationRoot, "1  CASE FILES", () => ChangePage(Page.CaseFiles), InvestigationButtonStyle.Navigation);
            AddButton(navigationRoot, "2  COMPARE DATA", () => ChangePage(Page.CompareData), InvestigationButtonStyle.Navigation);
            AddButton(navigationRoot, "3  BUILD HYPOTHESIS", () => ChangePage(Page.BuildHypothesis), InvestigationButtonStyle.Navigation);
            AddButton(navigationRoot, "4  PLAN SAMPLE", () => ChangePage(Page.PlanSample), InvestigationButtonStyle.Navigation);
            AddButton(navigationRoot, "5  CONCLUSION", () => ChangePage(Page.Conclusion), InvestigationButtonStyle.Navigation);
        }

        private void RenderCurrentPage(bool preserveContentPosition = false)
        {
            if (titleText == null || state == null || caseDefinition == null) return;
            float previousScrollPosition = preserveContentPosition && contentScrollRect != null
                ? contentScrollRect.verticalNormalizedPosition
                : 1f;
            titleText.text = $"eDNA DETECTIVES  /  {GetPageTitle()}";
            progressText.text = $"R{state.CurrentRound}    SAMPLES {state.AvailableSampleSlots}    FOUND {state.IdentifiedEvidenceIds.Count}/{state.UnlockedEvidence.Count}    MISSTEPS {state.MisclassificationCount}";
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
            ShowBodyContent();
            SpeciesDefinition species = GetSelectedSpecies();
            StringBuilder text = new StringBuilder();
            text.AppendLine(caseDefinition.DisplayName.ToUpperInvariant());
            text.AppendLine(caseDefinition.Briefing);
            text.AppendLine();
            text.AppendLine("SPECIES FILE");
            if (species != null)
            {
                text.AppendLine($"{speciesIndex + 1} / {caseDefinition.Species.Count}   {species.DisplayName}");
                text.AppendLine(species.Description);
                text.AppendLine($"Preferred depths: {Join(species.PreferredDepths)}");
                text.AppendLine($"Temperature: {species.TemperaturePreference}");
                text.AppendLine($"Habitat tags: {Join(species.HabitatTags)}");
                text.AppendLine($"Sensitivity tags: {Join(species.SensitivityTags)}");
            }
            text.AppendLine();
            text.AppendLine("MISSION");
            text.AppendLine("Inspect one present-day sample at a time. Compare each species with records from 20 years ago, select a card, and classify what changed. Correct findings can then be used to build and test a hypothesis.");
            bodyText.text = text.ToString();
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

            AddBrowseButton("PREVIOUS SAMPLE", () => ChangeResult(-1));
            AddBrowseButton("NEXT SAMPLE", () => ChangeResult(1));
            AddClassificationButton("NEW ARRIVAL", AnomalyClaimType.NewArrival);
            AddClassificationButton("EXPECTED BUT MISSING", AnomalyClaimType.ExpectedButMissing);
            AddClassificationButton("DIFFERENT DEPTH", AnomalyClaimType.DifferentDepth);
            AddClassificationButton("RESULT WARNING", AnomalyClaimType.ResultWarning);
            AddClassificationButton("MATCHES BASELINE", AnomalyClaimType.MatchesBaseline);
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
                species == null ? "Species details are not available." : $"Temperature: {species.TemperaturePreference}\nPreferred depth: {Join(species.PreferredDepths)}\nTraits: {Join(species.SensitivityTags)}",
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
            ShowBodyContent();
            HypothesisDefinition hypothesis = GetSelectedHypothesis();
            List<EvidenceRecord> identifiedEvidence = state.GetIdentifiedEvidence();
            EvidenceRecord evidence = GetSelectedEvidence();
            StringBuilder text = new StringBuilder();
            text.AppendLine("WORKING HYPOTHESIS");
            if (hypothesis != null)
            {
                HypothesisEvaluation evaluation = new HypothesisEvaluator().Evaluate(hypothesis, state);
                text.AppendLine($"{hypothesisIndex + 1} / {caseDefinition.Hypotheses.Count}   {hypothesis.DisplayName}");
                text.AppendLine(hypothesis.Explanation);
                text.AppendLine($"Status: {evaluation.Status}");
                text.AppendLine($"Assigned support: {evaluation.SupportingEvidenceCount}   Assigned opposition: {evaluation.OpposingEvidenceCount}");
                text.AppendLine($"Required evidence patterns: {Join(hypothesis.RequiredEvidenceTags)}");
                text.AppendLine($"Minimum confidence: {hypothesis.MinimumConfidence}");
                if (string.Equals(state.SelectedHypothesisId, hypothesis.HypothesisId, StringComparison.Ordinal)) text.AppendLine("SELECTED FOR CONCLUSION");
            }
            text.AppendLine();
            text.AppendLine("IDENTIFIED FINDING TO ASSIGN");
            if (evidence == null) text.AppendLine("No findings have been identified yet. Return to Compare Data and correctly classify a comparison card.");
            else
            {
                text.AppendLine($"{evidenceIndex + 1} / {identifiedEvidence.Count}   [{evidence.Confidence}] {evidence.DisplayText}");
                text.AppendLine(evidence.ConfidenceReason);
                text.AppendLine($"Current assignment: {DescribeAssignment(evidence.EvidenceId, hypothesis?.HypothesisId)}");
            }
            text.AppendLine();
            text.AppendLine("Assigning a finding records whether it supports or challenges the selected explanation. It never changes the raw eDNA result.");
            bodyText.text = text.ToString();

            AddStageBackButton("COMPARE DATA", () => ChangePage(Page.CompareData));
            AddBrowseButton("PREVIOUS THEORY", () => ChangeHypothesis(-1));
            AddBrowseButton("NEXT THEORY", () => ChangeHypothesis(1));
            AddBrowseButton("PREVIOUS FINDING", () => ChangeEvidence(-1));
            AddBrowseButton("NEXT FINDING", () => ChangeEvidence(1));
            AddActionButton("ASSIGN SUPPORT", () => AssignSelected(EvidenceAssignmentKind.Supports));
            AddActionButton("ASSIGN CHALLENGE", () => AssignSelected(EvidenceAssignmentKind.Opposes));
            AddActionButton("SELECT THEORY", SelectCurrentHypothesis);
        }

        private void RenderSamplePlanner()
        {
            ShowBodyContent();
            SampleSiteDefinition site = GetSelectedSite();
            DepthBand depth = GetSelectedDepth();
            HypothesisDefinition hypothesis = GetSelectedHypothesis();
            EvidenceRecord evidence = GetSelectedEvidence();
            StringBuilder text = new StringBuilder();
            text.AppendLine("FOLLOW-UP SAMPLE PLAN");
            text.AppendLine("Choose a site and depth that can test an identified finding. This prototype returns an immediate Mock Lab result through the shared sampling contract.");
            text.AppendLine();
            if (site != null)
            {
                text.AppendLine($"Site: {site.DisplayName}");
                text.AppendLine(site.Description);
                text.AppendLine($"Available depths: {Join(site.AvailableDepths)}");
                text.AppendLine($"Selected depth: {depth}");
            }
            text.AppendLine($"Related hypothesis: {(hypothesis == null ? "None" : hypothesis.DisplayName)}");
            text.AppendLine($"Sampling reason: {(evidence == null ? "General investigation" : evidence.DisplayText)}");
            text.AppendLine();
            text.AppendLine($"Follow-up samples available: {state.AvailableSampleSlots}");
            if (state.PendingSampleCount > 0) text.AppendLine($"Samples awaiting results: {state.PendingSampleCount}");
            bodyText.text = text.ToString();
            AddStageBackButton("COMPARE RESULTS", () => ChangePage(Page.CompareData));
            AddBrowseButton("PREVIOUS SITE", () => ChangeSite(-1));
            AddBrowseButton("NEXT SITE", () => ChangeSite(1));
            AddBrowseButton("PREVIOUS DEPTH", () => ChangeDepth(-1));
            AddBrowseButton("NEXT DEPTH", () => ChangeDepth(1));
            AddBrowseButton("PREVIOUS REASON", () => ChangeEvidence(-1));
            AddBrowseButton("NEXT REASON", () => ChangeEvidence(1));
            AddActionButton("COLLECT MOCK SAMPLE", RequestSelectedSample);
        }

        private void RenderConclusion()
        {
            ShowBodyContent();
            StringBuilder text = new StringBuilder();
            text.AppendLine("CASE CONCLUSION");
            HypothesisDefinition selected = caseDefinition.FindHypothesis(state.SelectedHypothesisId);
            if (selected == null)
            {
                text.AppendLine("Selected hypothesis: None");
                text.AppendLine("Choose a working hypothesis on the Build Hypothesis page.");
            }
            else
            {
                HypothesisEvaluation evaluation = new HypothesisEvaluator().Evaluate(selected, state);
                text.AppendLine($"Selected hypothesis: {selected.DisplayName}");
                text.AppendLine($"Evidence status: {evaluation.Status}");
                text.AppendLine($"Support: {evaluation.SupportingEvidenceCount}   Opposition/uncertainty: {evaluation.OpposingEvidenceCount}");
                text.AppendLine(evaluation.Explanation);
            }
            text.AppendLine();
            text.AppendLine($"Identified findings: {state.IdentifiedEvidenceIds.Count}");
            text.AppendLine($"Follow-up samples completed: {state.CompletedSampleCount}");
            text.AppendLine($"Misclassifications recorded: {state.MisclassificationCount}");
            text.AppendLine($"Submission status: {state.ConclusionStatus}");
            text.AppendLine();
            text.AppendLine("A defensible conclusion needs a supported hypothesis, at least one targeted follow-up sample, and at least one challenging or uncertain finding.");
            bodyText.text = text.ToString();
            AddStageBackButton("PLAN SAMPLE", () => ChangePage(Page.PlanSample));
            AddActionButton("BUILD HYPOTHESIS", () => ChangePage(Page.BuildHypothesis));
            AddActionButton("COMPARE DATA", () => ChangePage(Page.CompareData));
            AddStageForwardButton("SUBMIT CONCLUSION", () => submitConclusion?.Invoke());
            AddActionButton("RESTART CASE", () => restartCase?.Invoke());
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
            bodyText.gameObject.SetActive(true);
            contentScrollRect.content = bodyText.rectTransform;
        }

        private void ShowComparisonBoard()
        {
            DestroyComparisonBoard();
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

        private void DestroyComparisonBoard()
        {
            if (comparisonBoardInstance == null) return;
            comparisonBoardInstance.gameObject.SetActive(false);
            Destroy(comparisonBoardInstance.gameObject);
            comparisonBoardInstance = null;
        }

        private void AddClassificationButton(string label, AnomalyClaimType claimType)
        {
            bool hasSelection = !string.IsNullOrEmpty(selectedComparisonEvidenceId);
            bool alreadyIdentified = hasSelection && state.IsEvidenceIdentified(selectedComparisonEvidenceId);
            bool ruledOut = hasSelection && state.HasRejectedClassification(selectedComparisonEvidenceId, claimType);
            string visibleLabel = ruledOut ? $"RULED OUT: {label}" : label;
            AddActionButton(
                visibleLabel,
                () => ClassifySelected(claimType),
                hasSelection && !alreadyIdentified && !ruledOut);
        }

        private void AddActionButton(string label, Action action, bool isInteractable = true)
        {
            AddActionSlotButton(label, action, InvestigationButtonStyle.Primary, isInteractable);
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
            AddButton(actionRoot, label, action, style, isInteractable);
            actionSlotCount++;
        }

        private void PadActionsToColumn(int targetColumn)
        {
            int columnCount = GetActionColumnCount();
            int safeTarget = Mathf.Clamp(targetColumn, 0, columnCount - 1);
            while (actionSlotCount % columnCount != safeTarget)
            {
                GameObject spacer = new GameObject("Action Spacer", typeof(RectTransform));
                spacer.transform.SetParent(actionRoot, false);
                actionSlotCount++;
            }
        }

        private int GetActionColumnCount()
        {
            if (actionRoot == null) return 1;
            GridLayoutGroup grid = actionRoot.GetComponent<GridLayoutGroup>();
            return grid != null
                && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.Max(1, grid.constraintCount)
                : 1;
        }

        private void AddButton(
            Transform parent,
            string label,
            Action action,
            InvestigationButtonStyle style,
            bool isInteractable = true)
        {
            if (parent == null || buttonPrefab == null) return;
            InvestigationButtonView button = Instantiate(buttonPrefab, parent);
            button.name = label;
            button.Bind(label, action, style, isInteractable);
        }

        private void ChangePage(Page page) { currentPage = page; statusMessage = string.Empty; RenderCurrentPage(); }
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

        private void SelectCurrentHypothesis()
        {
            HypothesisDefinition hypothesis = GetSelectedHypothesis();
            if (hypothesis != null) selectHypothesis?.Invoke(hypothesis.HypothesisId);
        }

        private void RequestSelectedSample()
        {
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
                if (string.Equals(assignment.EvidenceId, evidenceId, StringComparison.Ordinal) && string.Equals(assignment.HypothesisId, hypothesisId, StringComparison.Ordinal)) return assignment.AssignmentKind.ToString();
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
            switch (type)
            {
                case EvidenceType.NewDetection: return "New Arrival";
                case EvidenceType.NotDetectedInSample:
                case EvidenceType.RepeatedNonDetection: return "Expected but Missing";
                case EvidenceType.DepthShift: return "Different Depth";
                case EvidenceType.LowQualityResult:
                case EvidenceType.ContaminationWarning: return "Result Warning";
                case EvidenceType.StableIndicator:
                case EvidenceType.RepeatedDetection: return "Matches Baseline";
                default: return type.ToString();
            }
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

        private void ClearActions()
        {
            if (actionRoot == null) return;
            actionSlotCount = 0;
            for (int index = actionRoot.childCount - 1; index >= 0; index--)
            {
                GameObject child = actionRoot.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
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
                    return "Choose a theory and assign identified findings as support or challenge.";
                case Page.PlanSample:
                    return "Choose a site and depth that can test your working hypothesis.";
                case Page.Conclusion:
                    return "Review your evidence, then submit a conclusion when the requirements are met.";
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
