using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private RectTransform restartDialog;
        private int restartDialogVersion;
        private double restartPausedAt = -1d;

        private void CreateHeaderRestart(RectTransform header)
        {
            restartButton = CreateButton("Restart Case", header, string.Empty, ButtonVisualStyle.Tertiary,
                RequestRestartConfirmation, out _);
            Anchor(restartButton.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -56f, -50f, -12f, -6f);
            // Keep restart reachable while EDNA spotlights cover the rest of the screen.
            Canvas layer = restartButton.gameObject.AddComponent<Canvas>();
            layer.overrideSorting = true;
            layer.sortingOrder = 90;
            restartButton.gameObject.AddComponent<GraphicRaycaster>();
            Image icon = CreateStatusIcon("Restart Icon", restartButton.transform,
                InvestigationStatusIconLibrary.Restart, InvestigationTheme.TextPrimary);
            Stretch(icon.rectTransform, 7f, 7f, -7f, -7f);
            restartButton.targetGraphic = icon;
            Text hint = CreateText("Restart Label", restartButton.transform, "Restart investigation", 13,
                FontStyle.Bold, InvestigationTheme.TextPrimary, TextAnchor.MiddleRight, InvestigationTheme.BodyFont);
            Anchor(hint.rectTransform, 0f, 0f, 0f, 1f, -160f, 0f, -8f, 0f);
            hint.raycastTarget = false;
            hint.gameObject.SetActive(false);
            restartButton.gameObject.AddComponent<InvestigationHoverTooltipTrigger>().Configure(.3f,
                () => { if (hint != null && restartButton.interactable) hint.gameObject.SetActive(true); },
                () => { if (hint != null) hint.gameObject.SetActive(false); });
        }

        private void RequestRestartConfirmation()
        {
            if (state == null || restartConfirmationPending) return;
            bool detailsPaused = scenarioDetailPausedAt >= 0d;
            CloseScenarioDetail(false);
            restartConfirmationPending = true;
            if (ScenarioWorkspaceActive && scenarioBriefingPausedAt < 0d)
                restartPausedAt = Time.unscaledTimeAsDouble;
            restartButton.interactable = false;
            if (detailsPaused) RefreshPresentationOnly();
            else RenderRestartDialog();
        }

        private void RemoveRestartDialog()
        {
            if (restartDialog == null) return;
            restartDialog.gameObject.SetActive(false);
            Destroy(restartDialog.gameObject);
            restartDialog = null;
            restartDialogVersion++;
        }

        private void RenderRestartDialog()
        {
            if (!restartConfirmationPending || state == null) { RemoveRestartDialog(); return; }
            if (restartDialog != null) return;
            int version = ++restartDialogVersion;
            restartDialog = CreatePanel("Restart Confirmation", transform, new Color(0f, 0f, 0f, .72f), 0f);
            Stretch(restartDialog, 0f, 0f, 0f, 0f);
            Canvas layer = restartDialog.gameObject.AddComponent<Canvas>();
            layer.overrideSorting = true;
            layer.sortingOrder = 100;
            restartDialog.gameObject.AddComponent<GraphicRaycaster>();
            restartDialog.GetComponent<Image>().raycastTarget = true;
            RectTransform panel = CreatePanel("Restart Dialog", restartDialog, InvestigationTheme.Paper, InvestigationTheme.CardRadius);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(440f, 208f);
            Text heading = CreateText("Restart Heading", panel, "Start this investigation again?", 22,
                FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleLeft, InvestigationTheme.BodyFont);
            Anchor(heading.rectTransform, 0f, 1f, 1f, 1f, 22f, -60f, -22f, -16f);
            Text detail = CreateText("Restart Detail", panel, "Your notebook and model progress will be cleared.", 16,
                FontStyle.Normal, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            Anchor(detail.rectTransform, 0f, 1f, 1f, 1f, 22f, -130f, -22f, -74f);
            Button cancel = CreateButton("Cancel Restart Case", panel, "Keep investigating", ButtonVisualStyle.PaperChoice,
                () => { if (version == restartDialogVersion) CancelRestartConfirmation(); }, out _);
            Anchor(cancel.GetComponent<RectTransform>(), 0f, 0f, .5f, 0f, 22f, 20f, -6f, 68f);
            Button confirm = CreateButton("Confirm Restart Case", panel, "Restart", ButtonVisualStyle.PaperPrimary,
                () => { if (version == restartDialogVersion) ConfirmRestart(); }, out _);
            Anchor(confirm.GetComponent<RectTransform>(), .5f, 0f, 1f, 0f, 6f, 20f, -22f, 68f);
            foreach (Button button in new[] { cancel, confirm })
            {
                Button other = button == cancel ? confirm : cancel;
                button.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnLeft = other, selectOnRight = other, selectOnUp = other, selectOnDown = other };
                var trigger = button.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.Cancel };
                entry.callback.AddListener(_ => { if (version == restartDialogVersion) CancelRestartConfirmation(); });
                trigger.triggers.Add(entry);
            }
            EventSystem.current?.SetSelectedGameObject(cancel.gameObject);
        }

        private void CancelRestartConfirmation()
        {
            if (!restartConfirmationPending) return;
            restartConfirmationPending = false;
            if (restartPausedAt >= 0d) scenarioStarted += Time.unscaledTimeAsDouble - restartPausedAt;
            restartPausedAt = -1d;
            RemoveRestartDialog();
            // Playback components captured the old start timestamp; rebuild them
            // with the adjusted timestamp before resuming the model.
            RefreshPresentationOnly();
            restartButton.interactable = state != null;
            EventSystem.current?.SetSelectedGameObject(restartButton.gameObject);
        }

        private void ConfirmRestart()
        {
            if (!restartConfirmationPending || state == null) return;
            restartConfirmationPending = false;
            restartPausedAt = -1d;
            RemoveRestartDialog();
            restart?.Invoke();
        }
    }
}
