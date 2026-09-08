using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EDNA.Investigation.Domain;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private string rovScanId = string.Empty;
        private float rovScanProgress;
        private int rovScanRegions;
        private bool rovScanFound;
        private void BeginWorkbenchRovScan(string id)
        {
            if (state.ConfirmationReviewed) { InspectRovClue(id); return; }
            rovScanId = id; rovScanProgress = 0f; rovScanRegions = 0; rovScanFound = false;
            navigationRevealTarget = "ROV Scan Panel"; navigationRevealAtTop = true;
            RefreshPresentationOnly();
            RectTransform sweep = FindNamedRect(contentRoot, "ROV Camera Sweep");
            if (sweep != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(sweep.gameObject);
        }
        private void RecordWorkbenchRovScan()
        {
            if (!rovScanFound || string.IsNullOrEmpty(rovScanId)) return;
            string id = rovScanId; rovScanId = string.Empty;
            InspectRovClue(id);
        }
        private void RenderWorkbenchRovScan()
        {
            RectTransform panel = CreatePanel("ROV Scan Panel", contentRoot, InvestigationTheme.SurfaceQuiet, InvestigationTheme.CardRadius);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12); layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            Text title = CreateText("ROV Scan Instructions", panel, "EDNA · 1/3 · Move the camera across the frame. Inspect each area, then pin the finding.",
                15, FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(title);
            RectTransform frame = CreatePanel("ROV Scan Surface", panel, InvestigationTheme.Deep, InvestigationTheme.SmallRadius);
            AddLayout(frame, 160f, 1f); frame.GetComponent<Image>().raycastTarget = true;
            Image[] patches = new Image[3];
            int targetPatch = 1 + (observeLayoutSessionSeed.GetHashCode() & 1);
            for (int i = 0; i < 3; i++)
            {
                RectTransform region = CreatePanel("ROV Region " + i, frame, InvestigationTheme.Surface, InvestigationTheme.SmallRadius);
                Anchor(region, i / 3f, 0f, (i + 1f) / 3f, 1f, 5f, 6f, -5f, -6f);
                Image image = CreateStatusIcon("ROV Detail " + i, region,
                    rovScanId == "E07_FISHING_LINE" && i == targetPatch ? InvestigationEvidenceIconLibrary.FishingLine : InvestigationEvidenceIconLibrary.Seafloor,
                    InvestigationTheme.Primary);
                Stretch(image.rectTransform, 24f, 24f, -24f, -24f); patches[i] = image;
            }
            RectTransform beam = CreatePanel("ROV Camera Beam", frame, new Color(.5f, 1f, .94f, .18f), 8f);
            beam.GetComponent<Image>().raycastTarget = false;
            Text feedback = CreateText("ROV Scan Feedback", panel, string.Empty, 14, FontStyle.Bold,
                InvestigationTheme.Primary, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            AddLayout(feedback.rectTransform, 36f, 1f);
            Slider sweep = CreateWorkbenchSlider("ROV Camera Sweep", panel, rovScanProgress, null);
            frame.gameObject.AddComponent<InvestigationScanSurface>().Configure(sweep);
            RectTransform actions = CreatePanel("ROV Scan Actions", panel, Color.clear, 0f);
            HorizontalLayoutGroup actionLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 10f;
            actionLayout.childControlWidth = actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = actionLayout.childForceExpandHeight = false;
            Button pin = CreateButton("Pin ROV Finding", actions, "Pin finding and collect the ROV field notes", ButtonVisualStyle.Primary, RecordWorkbenchRovScan, out Text pinText);
            ConfigureWrappingChoice(pin, pinText);
            pin.GetComponent<LayoutElement>().flexibleWidth = 1f;
            Button back = CreateButton("Change ROV Area", actions, "Another area", ButtonVisualStyle.Tertiary,
                () => { rovScanId = string.Empty; reportConversationFocusPending = true; RefreshPresentationOnly(); }, out _);
            LayoutElement backSize = back.GetComponent<LayoutElement>();
            backSize.minWidth = backSize.preferredWidth = 140f;
            void RefreshScan(float value, bool inspect)
            {
                rovScanProgress = value;
                if (inspect) rovScanRegions |= 1 << Mathf.Clamp(Mathf.FloorToInt(value * 3f), 0, 2);
                rovScanFound = rovScanId == "E07_FISHING_LINE" ? (rovScanRegions & (1 << targetPatch)) != 0 : rovScanRegions == 7;
                Anchor(beam, value, 0f, value, 1f, -42f, 0f, 42f, 0f);
                for (int i = 0; i < 3; i++) patches[i].color = new Color(.45f, .83f, .79f, (rovScanRegions & (1 << i)) != 0 ? 1f : .04f);
                feedback.text = rovScanFound ? caseDefinition.FindObservation(rovScanId).DisplayName
                    : "Camera search · " + (((rovScanRegions & 1) != 0 ? 1 : 0) + ((rovScanRegions & 2) != 0 ? 1 : 0) + ((rovScanRegions & 4) != 0 ? 1 : 0)) + "/3 areas examined";
                pin.interactable = rovScanFound;
            }
            sweep.onValueChanged.AddListener(value => RefreshScan(value, true));
            RefreshScan(rovScanProgress, false);
        }
    }
}
