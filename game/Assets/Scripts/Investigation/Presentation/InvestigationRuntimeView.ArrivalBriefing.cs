using UnityEngine;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private int arrivalBriefingStep;
        private bool historyLensBriefingPending;
        private bool HistoryLensBriefingActive => ObserveArrivalActive
            && observeArrivalStage == ObserveArrivalStage.HistoricalPreview && historyLensBriefingPending;
        private bool ArrivalBriefingActive => ObserveArrivalActive
            && observeArrivalStage == ObserveArrivalStage.Welcome && arrivalBriefingStep < 2;

        private void RenderArrivalBriefingPresentation()
        {
            if (HistoryLensBriefingActive)
            {
                RenderSpotlightBriefing("Survey Lens Controls", "History Lens Briefing", "EDNA · LOOK BACK 20 YEARS",
                    "Drag the glowing handle to the right to reveal 20 years ago. You can slide back and forth to explore. When you're ready, record the old survey at the right-hand end.",
                    "Continue →", CloseHistoryLensBriefing, true, CloseHistoryLensBriefing);
                return;
            }
            if (!ArrivalBriefingActive) return;
            int shownStep = arrivalBriefingStep;
            // The recording prompt appears only when it is introduced.
            var welcome = FindNamedRect(contentRoot, "Observe Welcome");
            if (welcome != null) EnsureCanvasGroup(welcome).alpha = shownStep == 0 ? 0f : 1f;
            RenderSpotlightBriefing(shownStep == 0 ? "Survey Comparison" : "Observe Welcome",
                "Arrival Briefing", $"EDNA · {(shownStep + 1)}/2",
                shownStep == 0
                    ? "Welcome to our seamount. These pictures show the species detected in today's eDNA survey. Start with what we found today; we'll look back 20 years next."
                    : "Let's collect today's pictures in your notebook. Watch them move from the seamount into our records. We'll collect the old survey separately before comparing them.",
                shownStep == 0 ? "Next →" : "Let's record →", () =>
                {
                    if (!ArrivalBriefingActive || arrivalBriefingStep != shownStep) return;
                    arrivalBriefingStep++;
                    if (arrivalBriefingStep == 2) StartRecordingToday();
                    else RefreshPresentationOnly();
                }, skipAction: () => SkipArrivalBriefing(shownStep));
        }
        private void CloseHistoryLensBriefing()
        {
            if (!HistoryLensBriefingActive) return;
            historyLensBriefingPending = false;
            navigationRevealTarget = "Survey Time Lens";
            navigationRevealAtTop = false;
            RefreshPresentationOnly();
        }

        private void SkipArrivalBriefing(int shownStep)
        {
            if (!ArrivalBriefingActive || arrivalBriefingStep != shownStep) return;
            arrivalBriefingStep = 2;
            navigationRevealTarget = "Start Recording Today";
            navigationRevealAtTop = false;
            RefreshPresentationOnly();
        }
    }
}
