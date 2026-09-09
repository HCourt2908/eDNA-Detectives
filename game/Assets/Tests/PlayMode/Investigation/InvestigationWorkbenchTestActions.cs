using UnityEngine;
using UnityEngine.UI;
using NUnit.Framework;

namespace EDNA.Investigation.Tests
{
    // Shared player actions used by the existing scenario tests after the
    // workbench replaces one-click observation and ROV capture.
    internal static class InvestigationWorkbenchTestActions
    {
        private static void Press(string name)
        {
            Button button = GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name); Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
        }
        public static void SetLensFor(string name)
        {
            if ((name.StartsWith("Species Marker ") || name.StartsWith("Historical Species Marker "))
                && GameObject.Find("Survey Time Lens") == null && GameObject.Find("Toggle Comparison View") != null) Press("Toggle Comparison View");
            Slider lens = GameObject.Find("Survey Time Lens")?.GetComponent<Slider>();
            if (lens != null && name.StartsWith("Species Marker ")) lens.value = 0f;
            if (lens != null && name.StartsWith("Historical Species Marker ")) lens.value = 1f;
            Canvas.ForceUpdateCanvases();
        }
        public static void BeginTodayRecording()
        {
            for (int step = 0; step < 2 && GameObject.Find("Arrival Briefing Next") != null; step++) Press("Arrival Briefing Next");
            if (GameObject.Find("Start Recording Today") != null) Press("Start Recording Today");
        }
        public static void BeginObserveQuestions(bool finishBriefing = true)
        {
            BeginTodayRecording();
            if (GameObject.Find("Skip Today Recording Animation") != null) Press("Skip Today Recording Animation");
            if (GameObject.Find("Compare With History") != null) Press("Compare With History");
            if (GameObject.Find("History Lens Briefing Next") != null) Press("History Lens Briefing Next");
            if (GameObject.Find("Start Recording History") != null)
            {
                GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = 1f;
                Press("Start Recording History");
            }
            if (GameObject.Find("Skip History Recording Animation") != null) Press("Skip History Recording Animation");
            if (GameObject.Find("Compare Recorded Surveys") != null)
            {
                Press("Compare Recorded Surveys");
            }
            if (finishBriefing)
                for (int step = 0; step < 3 && GameObject.Find("Comparison Briefing Next") != null; step++)
                    Press("Comparison Briefing Next");
        }
        public static void Record(string name)
        {
            BeginObserveQuestions();
            string id = name.Replace("Species Marker ", "");
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            string evidence = id == "shark" ? "E01_SHARK_NONDETECTION" : id == "tuna" ? "E02_TUNA_WIDER_DETECTION"
                : id == "krill" ? "E03_KRILL_NONDETECTION" : id == "sea_star" ? "E04_BENTHIC_STABLE" : "E06_PLASTIC_INDICATOR_STABLE";
            if (controller.State.HasDiscoveredObservation(evidence)) return;
            if (GameObject.Find("Comparison Seamount") != null) Press("Toggle Comparison View");
            Press("Compare Species " + id);
            Press("Compare Change " + (id == "shark" || id == "krill" ? "NotDetected" : id == "tuna" ? "More" : "Same"));
            Assert.That(controller.State.HasDiscoveredObservation(evidence), Is.True, "Classify the saved survey record: " + id);
        }
        public static void ShowSurveySummary()
        {
            if (GameObject.Find("Summarize Findings") == null) return;
            bool previous = InvestigationMotionSettings.ReducedMotion;
            try { InvestigationMotionSettings.SetReducedMotionForTests(true); Press("Summarize Findings"); }
            finally { InvestigationMotionSettings.SetReducedMotionForTests(previous); }
        }
        public static void EnterSimulate(string name)
        {
            ShowSurveySummary();
            Press(name);
            if (GameObject.Find("Finish Saving Summary") != null) Press("Finish Saving Summary");
            if (GameObject.Find("Continue To Simulate") != null && GameObject.Find("Observe Survey Story") != null) Press("Continue To Simulate");
        }
    }
}
