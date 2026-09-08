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
            Slider lens = GameObject.Find("Survey Time Lens")?.GetComponent<Slider>();
            if (lens != null && name.StartsWith("Species Marker ")) lens.value = 0f;
            if (lens != null && name.StartsWith("Historical Species Marker ")) lens.value = 1f;
            Canvas.ForceUpdateCanvases();
        }
        public static void Record(string name)
        {
            string id = name.Replace("Species Marker ", "");
            var controller = Object.FindAnyObjectByType<InvestigationController>();
            string evidence = id == "shark" ? "E01_SHARK_NONDETECTION" : id == "tuna" ? "E02_TUNA_WIDER_DETECTION"
                : id == "krill" ? "E03_KRILL_NONDETECTION" : id == "sea_star" ? "E04_BENTHIC_STABLE" : "E06_PLASTIC_INDICATOR_STABLE";
            if (controller.State.HasDiscoveredObservation(evidence)) return;
            Press("Observe Answer " + (id == "shark" || id == "krill" ? "NotDetected" : id == "tuna" ? "MoreSites" : "Stable"));
            Assert.That(controller.State.HasDiscoveredObservation(evidence), Is.True, "Answer the current Edna question: " + id);
        }
        public static void EnterSimulate(string name)
        {
            Press(name);
            if (GameObject.Find("Food Web Assembly") == null) return;
            Press("Connect Species shark"); Press("Connect Species tuna");
            Press("Connect Species tuna"); Press("Connect Species krill");
        }
        public static void CompleteCameraCapture()
        {
            Slider sweep = GameObject.Find("ROV Camera Sweep")?.GetComponent<Slider>();
            if (sweep == null) return;
            sweep.value = .15f; sweep.value = .5f; sweep.value = .85f;
            Press("Pin ROV Finding");
        }
        public static void InspectRov(string name) { Press(name); CompleteCameraCapture(); }
        public static void ConnectCluesToArgument(string name)
        {
            // Clear existing attachments before reconsidering their roles.
            Press("Argument Slot main"); Press("Argument Slot cross");
            Press(name); Press("Argument Slot main");
            Press(name.EndsWith("Seafloor") ? "Report Key Clue FoodWeb" : "Report Key Clue Seafloor");
            Press("Argument Slot cross"); Press("Discuss Connected Clues");
        }
    }
}
