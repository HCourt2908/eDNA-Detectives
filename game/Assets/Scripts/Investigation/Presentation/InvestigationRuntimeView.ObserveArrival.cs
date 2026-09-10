using System.Collections;
using System.Collections.Generic;
using EDNA.Core;
using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private enum ObserveArrivalStage { Welcome, Recording, Ready, HistoricalPreview, HistoricalRecording, HistoricalReady, Questions }
        private ObserveArrivalStage observeArrivalStage;
        private int todayRecordsArrived;
        private RectTransform todayRecordFlight;
        private int historicalRecordsArrived;
        private Button historyRecordButton;
        private Text historyRecordGuide;
        private GameObject historyRecordCue;
        private Coroutine todayRecordingAnimation;
        private bool ObserveArrivalActive => state != null && state.Phase == InvestigationPhase.Observe
            && CountInitialFindings() == 0 && observeArrivalStage != ObserveArrivalStage.Questions;
        private bool HasTodaySurveyNotes => todayRecordsArrived > 0 && observeArrivalStage >= ObserveArrivalStage.Ready;
        private bool HasHistoricalSurveyNotes => historicalRecordsArrived > 0 && observeArrivalStage >= ObserveArrivalStage.HistoricalReady;
        private bool IsRecordingSurvey => observeArrivalStage == ObserveArrivalStage.Recording || observeArrivalStage == ObserveArrivalStage.HistoricalRecording;
        private bool IsSurveyReady => observeArrivalStage == ObserveArrivalStage.Ready || observeArrivalStage == ObserveArrivalStage.HistoricalReady;
        private bool CanSlideSurveyLens => !ObserveArrivalActive || observeArrivalStage == ObserveArrivalStage.HistoricalPreview
            || observeArrivalStage == ObserveArrivalStage.HistoricalReady;
        private SurveyEra RecordingEra => observeArrivalStage == ObserveArrivalStage.HistoricalRecording || observeArrivalStage == ObserveArrivalStage.HistoricalReady
            ? SurveyEra.Historical : SurveyEra.Current;
        private int RecordingCount => RecordingEra == SurveyEra.Historical ? historicalRecordsArrived : todayRecordsArrived;
        private static string SurveyRowPrefix(SurveyEra era) => era == SurveyEra.Historical ? "Historical" : "Today";
        private static string SurveyName(SurveyEra era) => era == SurveyEra.Historical ? "20 years ago" : "today";

        private void SetRecordingCount(int count)
        {
            if (RecordingEra == SurveyEra.Historical) historicalRecordsArrived = count;
            else todayRecordsArrived = count;
        }

        private void MarkSurveyReady()
        {
            observeArrivalStage = RecordingEra == SurveyEra.Historical ? ObserveArrivalStage.HistoricalReady : ObserveArrivalStage.Ready;
            // Recording removes its action button. Focus the next useful control,
            // rather than a species marker that would open unrelated facts.
            navigationRevealTarget = observeArrivalStage == ObserveArrivalStage.HistoricalReady ? "Survey Time Lens" : "Compare With History";
            navigationRevealAtTop = false;
        }

        private void ResetObserveArrival()
        {
            CancelTodayRecordingVisuals();
            observeArrivalStage = ObserveArrivalStage.Welcome;
            arrivalBriefingStep = 0;
            historyLensBriefingPending = false;
            todayRecordsArrived = 0;
            historicalRecordsArrived = 0;
            historyRecordButton = null; historyRecordGuide = null; historyRecordCue = null;
        }

        private void RemoveTodayRecordFlight()
        {
            if (todayRecordFlight == null) return;
            todayRecordFlight.gameObject.SetActive(false);
            Destroy(todayRecordFlight.gameObject);
            todayRecordFlight = null;
        }

        private void CancelTodayRecordingVisuals()
        {
            if (todayRecordingAnimation != null) StopCoroutine(todayRecordingAnimation);
            todayRecordingAnimation = null;
            RemoveTodayRecordFlight();

        }

        private void OnDisable() { RemoveObserveSummaryFlight(); CancelTodayRecordingVisuals(); RemoveComparisonBriefingPresentation(); }
        private void OnEnable() { if (contentRoot != null) { if (ComparisonBriefingActive || ArrivalBriefingActive || HistoryLensBriefingActive || observeSummarySaving || ObserveSummaryTransitioning) RefreshPresentationOnly(); else ResumeTodayRecording(); } }

        private List<InvestigationSpeciesDefinition> RecordedSurveySpecies(SurveyEra era)
        {
            var result = GetVisibleObserveSpecies();
            // Record only organisms actually detected in this era. Absence from
            // today's picture becomes a question when the player sees the past.
            result.RemoveAll(species => !ShouldDisplaySpeciesInEra(species, era)
                || ResolveSurveySummary(species, era)?.Detection != SpeciesDetectionState.Detected);
            result.Sort((a, b) => string.CompareOrdinal(a.SpeciesId, b.SpeciesId));
            return result;
        }

        private RectTransform CreateObserveArrivalPaper(Transform parent, string objectName)
        {
            RectTransform paper = CreatePanel(objectName, parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            VerticalLayoutGroup layout = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12); layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            EnsureEdnaArtwork();
            RectTransform header = CreatePanel("Observe Edna Header", paper, Color.clear, 0f);
            bool compact = objectName != "Observe Welcome" && GetComponent<RectTransform>().rect.width < 930f;
            AddLayout(header, objectName == "Observe Welcome" ? 56f : compact ? 28f : 44f, 0f);
            Image portrait = CreateStatusIcon("Edna Introduction Portrait", header, ednaAvatar ?? ednaPortrait, Color.white);
            Anchor(portrait.rectTransform, 0f, 0f, 0f, 1f, 0f, 0f, 56f, 3f);
            Text name = CreateText("Edna Name", header, objectName == "Observe Welcome" ? "EDNA · TODAY'S SURVEY" : "EDNA · MY NOTEBOOK", 13, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            Stretch(name.rectTransform, 64f, 0f, 0f, 0f);
            name.alignment = TextAnchor.MiddleLeft;
            if (compact)
            {
                Stretch(name.rectTransform, 38f, 0f, 0f, 0f);
                name.alignment = TextAnchor.MiddleLeft;
                Anchor(portrait.rectTransform, 0f, 0f, 0f, 1f, 0f, 0f, 30f, 2f);
            }
            return paper;
        }

        private RectTransform RenderObserveWelcome(Transform parent)
        {
            RectTransform paper = CreatePanel("Observe Welcome", parent, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            CreateNotebookBinding(paper);
            // Binding rings are decorations, not rows in the empty notebook.
            foreach (RectTransform decoration in paper)
                decoration.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var layout = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(42, 18, 20, 20); layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            Text title = CreateText("Observe Welcome Title", paper, "MY NOTEBOOK", 21, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(title.rectTransform, 38f, 0f);
            Text date = CreateText("Observe Welcome Date", paper, "TODAY'S SURVEY", 14, FontStyle.Bold,
                InvestigationTheme.PaperMuted, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(date.rectTransform, 28f, 0f);
            for (int i = 0; i < 3; i++)
            {
                RectTransform slot = CreatePanel("Empty Survey Record " + i, paper, InvestigationTheme.PaperSelected, 8f);
                AddLayout(slot, 56f, 0f);
                Text pending = CreateText("Awaiting Record", slot, "– – –", 16, FontStyle.Normal,
                    InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                Stretch(pending.rectTransform, 8f, 4f, -8f, -4f);
            }
            Text note = CreateText("Observe Welcome Note", paper, "Ready for today's pictures", 14, FontStyle.Bold,
                InvestigationTheme.PaperMuted, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(note);
            if (!ArrivalBriefingActive)
            {
                Button start = CreateButton("Start Recording Today", paper, "Start recording →", ButtonVisualStyle.PaperPrimary,
                    StartRecordingToday, out Text label);
                label.fontSize = 14;
                ConfigureWrappingChoice(start, label);
                AddChoiceBorderCue("Start Recording Cue", start.transform);
            }
            return paper;
        }

        private void StartRecordingToday()
        {
            if (!ObserveArrivalActive || observeArrivalStage != ObserveArrivalStage.Welcome || ArrivalBriefingActive) return;
            StartSurveyRecording(SurveyEra.Current);
        }

        private void StartRecordingHistory()
        {
            if (!ObserveArrivalActive || observeArrivalStage != ObserveArrivalStage.HistoricalPreview || HistoryLensBriefingActive || surveyLensValue < .98f) return;
            StartSurveyRecording(SurveyEra.Historical);
        }

        private void StartSurveyRecording(SurveyEra era)
        {
            observeArrivalStage = era == SurveyEra.Historical ? ObserveArrivalStage.HistoricalRecording : ObserveArrivalStage.Recording;
            surveyLensValue = era == SurveyEra.Historical ? 1f : 0f;
            SetRecordingCount(0);
            if (InvestigationMotionSettings.ReducedMotion)
            {
                SetRecordingCount(RecordedSurveySpecies(era).Count);
                MarkSurveyReady();
            }
            RefreshPresentationOnly();
        }

        private void FinishTodayRecording()
        {
            if (!IsRecordingSurvey) return;
            SetRecordingCount(RecordedSurveySpecies(RecordingEra).Count);
            MarkSurveyReady();
            RemoveTodayRecordFlight();
            RefreshPresentationOnly();
        }

        private void BeginHistoricalComparison()
        {
            if (!ObserveArrivalActive || observeArrivalStage != ObserveArrivalStage.Ready) return;
            observeArrivalStage = ObserveArrivalStage.HistoricalPreview;
            historyLensBriefingPending = true;
            surveyLensExplored = false;
            navigationRevealTarget = "Observe History Welcome";
            navigationRevealAtTop = true;
            RefreshPresentationOnly();
        }

        private void BeginSurveyQuestions()
        {
            if (!ObserveArrivalActive || observeArrivalStage != ObserveArrivalStage.HistoricalReady) return;
            observeArrivalStage = ObserveArrivalStage.Questions;
            comparisonBriefingStep = ComparisonBriefingStep.Notebook;
            observeMapOpen = false;
            observeComparisonFeedback = "Both surveys are in your notebook. Compare them, then sort each species by its change in detection.";
            navigationRevealTarget = "Observe Comparison Board";
            navigationRevealAtTop = true;
            RefreshPresentationOnly();
        }

        private RectTransform RenderHistoryRecordingPrompt(Transform parent)
        {
            RectTransform paper = CreateObserveArrivalPaper(parent, "Observe History Welcome");
            Text title = CreateText("History Recording Title", paper, "Let's look back 20 years", 23, FontStyle.Bold,
                InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(title);
            Text message = CreateText("History Recording Message", paper,
                "Today's detections are saved. Move the slider all the way right to reveal the older survey.\n\nThen record the organisms you find there.", 17,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(message);
            Button record = CreateButton("Start Recording History", paper, "Record 20 years ago →", ButtonVisualStyle.PaperPrimary,
                StartRecordingHistory, out Text label);
            ConfigureWrappingChoice(record, label);
            AddChoiceBorderCue("Record History Cue", record.transform);
            Text guidance = CreateText("History Recording Guide", paper, string.Empty, 14, FontStyle.Bold,
                InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(guidance);
            historyRecordButton = record;
            historyRecordGuide = guidance;
            historyRecordCue = record.transform.Find("Record History Cue").gameObject;
            UpdateHistoryRecordingPrompt();
            return paper;
        }

        private void UpdateHistoryRecordingPrompt()
        {
            if (observeArrivalStage != ObserveArrivalStage.HistoricalPreview || historyRecordButton == null) return;
            bool ready = surveyLensValue >= .98f;
            historyRecordButton.interactable = ready;
            historyRecordCue.SetActive(ready);
            historyRecordGuide.text = ready
                ? "Here is the older survey. Record it in your notebook." : "Slide right to reach 20 years ago.";
        }

        private RectTransform RenderObserveRecording(Transform parent)
        {
            RectTransform paper = CreateObserveArrivalPaper(parent, "Observe Recording Notebook");
            paper.GetComponent<VerticalLayoutGroup>().spacing = 4f;
            SurveyEra era = RecordingEra;
            List<InvestigationSpeciesDefinition> species = RecordedSurveySpecies(era);
            bool ready = IsSurveyReady;
            Text heading = CreateText("Today Recording Status", paper,
                ready ? $"Recorded {SurveyName(era)} · {species.Count}/{species.Count}" : $"Recording {SurveyName(era)} · {RecordingCount}/{species.Count}", 17,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(heading);
            for (int i = 0; i < species.Count; i++) CreateSurveyRecordRow(paper, species[i], era, ready || i < RecordingCount);
            Text next = CreateText("Today Recording Next", paper, ready
                ? (era == SurveyEra.Historical ? "Both surveys are saved. Slide either way to look again, then compare the two surveys when you're ready." : "Today's detections are saved. Now let's look at 20 years ago.") : "Watch each survey result move into your notebook.", 14,
                FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(next);
            if (ready)
            {
                Button compare = CreateButton(era == SurveyEra.Historical ? "Compare Recorded Surveys" : "Compare With History", paper,
                    era == SurveyEra.Historical ? "Compare the two surveys →" : "Compare with the past →", ButtonVisualStyle.PaperPrimary,
                    () => { if (era == SurveyEra.Historical) BeginSurveyQuestions(); else BeginHistoricalComparison(); }, out Text label);
                label.fontSize = 14;
            }
            else CreateButton(era == SurveyEra.Historical ? "Skip History Recording Animation" : "Skip Today Recording Animation", paper, "Finish recording", ButtonVisualStyle.PaperChoice, FinishTodayRecording, out _);
            return paper;
        }

        private RectTransform CreateSurveyRecordRow(Transform parent, InvestigationSpeciesDefinition species, SurveyEra era, bool arrived)
        {
            RectTransform row = CreatePanel(SurveyRowPrefix(era) + " Notebook Row " + species.SpeciesId, parent,
                arrived ? InvestigationTheme.PaperSelected : InvestigationTheme.PaperRaised, InvestigationTheme.SmallRadius);
            bool compact = ObserveArrivalActive && GetComponent<RectTransform>().rect.width < 930f;
            AddLayout(row, compact ? 30f : 40f, 0f);
            RectTransform art = CreateSurveyArtwork(SurveyRowPrefix(era) + " Notebook Icon " + species.SpeciesId, row, species, era);
            Anchor(art, 0f, 0f, 0f, 1f, 6f, 5f, 78f, -5f);
            EnsureCanvasGroup(art).alpha = arrived ? 1f : .12f;
            Text name = CreateText("Today Notebook Species", row, species.GameplayName, 13, FontStyle.Bold,
                InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(name.rectTransform, 0f, .47f, 1f, 1f, 86f, 0f, -6f, 0f);
            Text result = CreateText("Today Notebook Result", row, arrived ? "Detected " + SurveyName(era) : "Waiting to record", 12,
                FontStyle.Bold, InvestigationTheme.PaperMuted, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(result.rectTransform, 0f, 0f, 1f, .48f, 86f, 2f, -6f, 0f);
            return row;
        }

        private void RenderTodaySurveyNotes(Transform parent)
        {
            if (!HasTodaySurveyNotes || state.Phase != InvestigationPhase.Observe) return;
            Text title = CreateText("Today Survey Notes Title", parent, "TODAY · SURVEY RECORDS", 13, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(title);
            foreach (var species in RecordedSurveySpecies(SurveyEra.Current)) CreateSurveyRecordRow(parent, species, SurveyEra.Current, true);
            if (HasHistoricalSurveyNotes)
            {
                Text historical = CreateText("Historical Survey Notes Title", parent, "20 YEARS AGO · SURVEY RECORDS", 13, FontStyle.Bold,
                    InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                ConfigureContentDrivenText(historical);
                foreach (var species in RecordedSurveySpecies(SurveyEra.Historical)) CreateSurveyRecordRow(parent, species, SurveyEra.Historical, true);
            }
        }

        private void ResumeTodayRecording()
        {
            if (ObserveArrivalActive && IsRecordingSurvey && todayRecordingAnimation == null)
                todayRecordingAnimation = StartCoroutine(AnimateTodayRecording());
        }

        private IEnumerator AnimateTodayRecording()
        {
            // Wait for layout and focus scrolling before converting A/B positions.
            yield return null;
            Canvas.ForceUpdateCanvases();
            SurveyEra era = RecordingEra;
            var speciesList = RecordedSurveySpecies(era);
            RectTransform canvas = GetComponent<Canvas>().rootCanvas.GetComponent<RectTransform>();
            for (int i = RecordingCount; i < speciesList.Count; i++)
            {
                var species = speciesList[i];
                RectTransform row = FindNamedRect(contentRoot, SurveyRowPrefix(era) + " Notebook Row " + species.SpeciesId);
                RectTransform target = FindNamedRect(row, SurveyRowPrefix(era) + " Notebook Icon " + species.SpeciesId);
                RectTransform source = FindNamedRect(contentRoot, (era == SurveyEra.Historical ? "Historical Species Marker " : "Species Marker ") + species.SpeciesId);
                todayRecordFlight = CreatePanel("Today Record In Flight", canvas, Color.clear, 8f);
                EnsureCanvasGroup(todayRecordFlight).blocksRaycasts = false;
                RectTransform art = CreateSurveyArtwork("Flying Survey Species", todayRecordFlight, species, era);
                Stretch(art, 0f, 0f, 0f, 0f);
                Vector2 start = canvas.InverseTransformPoint(source.TransformPoint(source.rect.center));
                Vector2 end = canvas.InverseTransformPoint(target.TransformPoint(target.rect.center));
                Vector2 startSize = new Vector2(100f, 70f);
                float elapsed = 0f;
                const float duration = .64f;
                while (elapsed < duration)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    todayRecordFlight.anchoredPosition = Vector2.Lerp(start, end, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 55f;
                    todayRecordFlight.sizeDelta = Vector2.Lerp(startSize, new Vector2(72f, 36f), t);
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                RemoveTodayRecordFlight();
                SetRecordingCount(i + 1);
                EnsureCanvasGroup(target).alpha = 1f;
                row.GetComponent<Image>().color = InvestigationTheme.PaperSelected;
                FindNamedRect(row, "Today Notebook Result").GetComponent<Text>().text = "Detected " + SurveyName(era);
                FindNamedRect(contentRoot, "Today Recording Status").GetComponent<Text>().text = $"Recording {SurveyName(era)} · {RecordingCount}/{speciesList.Count}";
                yield return new WaitForSecondsRealtime(.08f);
            }
            MarkSurveyReady();
            todayRecordingAnimation = null;
            RefreshPresentationOnly();
        }
    }
}
