using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private ReportKeyClue reportCrossCheck;
        private ReportKeyClue reportStagedClue;
        private string ArgumentCrossCheckResponse()
        {
            switch (reportCrossCheck)
            {
                case ReportKeyClue.Seafloor: return " Your cross-check tests the seabed damage expected from trawling.";
                case ReportKeyClue.FoodWeb: return " Your food-web cross-check is shared by both fishing models, so it cannot separate them on its own.";
                case ReportKeyClue.FishingLine: return " Your cross-check adds physical evidence of fishing; the seafloor still helps distinguish the methods.";
                default: return string.Empty;
            }
        }
        private void AttachArgumentClue(string role, string value)
        {
            if (!System.Enum.TryParse(value, out ReportKeyClue clue) || clue == ReportKeyClue.None) return;
            if ((role == "main" && clue == reportCrossCheck) || (role == "cross" && clue == reportKeyClue))
            {
                workbenchFeedback = "Use a different clue for the cross-check. It should add another kind of evidence.";
                RefreshPresentationOnly(); return;
            }
            if (role == "main") reportKeyClue = clue; else reportCrossCheck = clue;
            reportStagedClue = ReportKeyClue.None; workbenchFeedback = string.Empty;
            RefreshPresentationOnly();
        }
        private void RenderArgumentBoard(Transform parent)
        {
            RectTransform board = CreatePanel("Argument Board", parent, InvestigationTheme.PaperRaised, InvestigationTheme.SmallRadius);
            VerticalLayoutGroup layout = board.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12); layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            Text title = CreateText("Argument Board Title", board, "CONNECT YOUR CLUES TO YOUR EXPLANATION", 12, FontStyle.Bold,
                InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleLeft, InvestigationTheme.DataFont);
            ConfigureContentDrivenText(title);
            foreach (string role in new[] { "main", "cross" })
            {
                ReportKeyClue attached = role == "main" ? reportKeyClue : reportCrossCheck;
                string label = (role == "main" ? "Main clue" : "Cross-check") + "  →  "
                    + (attached == ReportKeyClue.None ? "Drop a clue here, or select a card then this slot" : ReportKeyClueLabel(attached));
                Button slot = CreateButton("Argument Slot " + role, board, label, ButtonVisualStyle.PaperChoice,
                    () =>
                    {
                        if (reportStagedClue == ReportKeyClue.None)
                        {
                            if (role == "main") reportKeyClue = ReportKeyClue.None; else reportCrossCheck = ReportKeyClue.None;
                            RefreshPresentationOnly();
                        }
                        else AttachArgumentClue(role, reportStagedClue.ToString());
                    }, out Text text);
                ConfigureWrappingChoice(slot, text);
                slot.gameObject.AddComponent<InvestigationWorkbenchDrop>().Configure("report-clue", value => AttachArgumentClue(role, value));
                if (attached != ReportKeyClue.None) slot.targetGraphic.color = InvestigationTheme.PaperSelected;
            }
            if (!string.IsNullOrEmpty(workbenchFeedback))
            {
                Text feedback = CreateText("Argument Feedback", board, workbenchFeedback, 13, FontStyle.Bold,
                    InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                ConfigureContentDrivenText(feedback);
            }
        }
    }
}
