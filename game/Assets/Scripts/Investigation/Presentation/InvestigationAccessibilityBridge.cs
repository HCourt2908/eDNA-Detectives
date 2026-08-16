using System;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationAccessibilityBridge : MonoBehaviour
    {
        [SerializeField] private RectTransform titleRoot;
        [SerializeField] private RectTransform progressRoot;
        [SerializeField] private RectTransform statusRoot;
        [SerializeField] private RectTransform navigationRoot;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform standardActionsRoot;
        [SerializeField] private RectTransform comparisonActionsRoot;
        [SerializeField] private RectTransform classificationActionsRoot;

        private AccessibilityHierarchy hierarchy;
        private AccessibilityNode titleNode;
        private string previousPageTitle = string.Empty;
        private string previousStatus = string.Empty;

        public int NodeCount { get; private set; }
        public string LastPageTitle { get; private set; } = string.Empty;
        public string LastAnnouncement { get; private set; } = string.Empty;

        public void ConfigureReferences(
            RectTransform title,
            RectTransform progress,
            RectTransform status,
            RectTransform navigation,
            RectTransform content,
            RectTransform standardActions,
            RectTransform comparisonActions,
            RectTransform classificationActions)
        {
            titleRoot = title;
            progressRoot = progress;
            statusRoot = status;
            navigationRoot = navigation;
            contentRoot = content;
            standardActionsRoot = standardActions;
            comparisonActionsRoot = comparisonActions;
            classificationActionsRoot = classificationActions;
        }

        public void Refresh(
            string pageTitle,
            string progressSummary,
            string statusLabel,
            string statusMessage)
        {
            pageTitle = pageTitle ?? string.Empty;
            progressSummary = progressSummary ?? string.Empty;
            statusLabel = statusLabel ?? string.Empty;
            statusMessage = statusMessage ?? string.Empty;
            bool pageChanged = !string.Equals(previousPageTitle, pageTitle, StringComparison.Ordinal);
            string statusAnnouncement = string.IsNullOrWhiteSpace(statusLabel)
                ? statusMessage
                : $"{statusLabel}. {statusMessage}";
            bool statusChanged = !string.Equals(previousStatus, statusAnnouncement, StringComparison.Ordinal);

            hierarchy = new AccessibilityHierarchy();
            NodeCount = 0;
            titleNode = AddNode(pageTitle, AccessibilityRole.Header, titleRoot, null);
            AddNode(progressSummary, AccessibilityRole.StaticText, progressRoot, null);
            AddNode(statusAnnouncement, AccessibilityRole.StaticText, statusRoot, null);

            AccessibilityNode navigationNode = AddNode("Investigation steps", AccessibilityRole.TabBar, navigationRoot, null);
            AddButtons(navigationRoot, navigationNode, true);

            AccessibilityNode contentNode = AddNode("Investigation content", AccessibilityRole.ScrollView, contentRoot, null);
            AddComparisonCards(contentRoot, contentNode);

            AccessibilityNode actionsNode = AddNode("Page actions", AccessibilityRole.Container, standardActionsRoot, null);
            AddButtons(standardActionsRoot, actionsNode, false);
            AddButtons(comparisonActionsRoot, actionsNode, false);
            AddButtons(classificationActionsRoot, actionsNode, false);

            AssistiveSupport.activeHierarchy = hierarchy;
            LastPageTitle = pageTitle;
            LastAnnouncement = statusAnnouncement;

            if (Application.isPlaying && AssistiveSupport.isScreenReaderEnabled)
            {
                if (pageChanged)
                {
                    AssistiveSupport.notificationDispatcher.SendScreenChanged(titleNode);
                }
                else if (statusChanged && !string.IsNullOrWhiteSpace(statusAnnouncement))
                {
                    AssistiveSupport.notificationDispatcher.SendAnnouncement(statusAnnouncement);
                }
            }

            previousPageTitle = pageTitle;
            previousStatus = statusAnnouncement;
        }

        private AccessibilityNode AddNode(
            string label,
            AccessibilityRole role,
            RectTransform rect,
            AccessibilityNode parent)
        {
            if (string.IsNullOrWhiteSpace(label) || rect == null || !rect.gameObject.activeInHierarchy) return null;
            AccessibilityNode node = hierarchy.AddNode(label, parent);
            node.role = role;
            node.frameGetter = () => GetScreenRect(rect);
            NodeCount++;
            return node;
        }

        private void AddButtons(RectTransform root, AccessibilityNode parent, bool navigationButtons)
        {
            if (root == null || !root.gameObject.activeInHierarchy) return;
            InvestigationButtonView[] views = root.GetComponentsInChildren<InvestigationButtonView>(true);
            for (int index = 0; index < views.Length; index++)
            {
                InvestigationButtonView view = views[index];
                if (!view.gameObject.activeInHierarchy) continue;
                Button control = view.GetComponent<Button>();
                AccessibilityNode node = AddNode(
                    view.Label,
                    navigationButtons ? AccessibilityRole.TabButton : AccessibilityRole.Button,
                    view.GetComponent<RectTransform>(),
                    parent);
                if (node == null) continue;

                node.state = control != null && !control.interactable
                    ? AccessibilityState.Disabled
                    : view.IsCurrentNavigation
                        ? AccessibilityState.Selected
                        : AccessibilityState.None;
                node.hint = GetButtonHint(view, control);
                node.invoked += () => Invoke(control);
            }
        }

        private void AddComparisonCards(RectTransform root, AccessibilityNode parent)
        {
            if (root == null || !root.gameObject.activeInHierarchy) return;
            SpeciesComparisonCardView[] cards = root.GetComponentsInChildren<SpeciesComparisonCardView>(true);
            for (int index = 0; index < cards.Length; index++)
            {
                SpeciesComparisonCardView card = cards[index];
                if (!card.gameObject.activeInHierarchy) continue;
                Button control = card.GetComponent<Button>();
                AccessibilityNode node = AddNode(
                    card.AccessibleLabel,
                    AccessibilityRole.Button,
                    card.GetComponent<RectTransform>(),
                    parent);
                if (node == null) continue;
                node.state = control != null && !control.interactable
                    ? AccessibilityState.Disabled
                    : card.IsSelected
                        ? AccessibilityState.Selected
                        : AccessibilityState.None;
                node.hint = card.IsAwaitingSelection
                    ? "Select this comparison card, then choose a classification."
                    : "Comparison card.";
                node.invoked += () => Invoke(control);
            }
        }

        private static string GetButtonHint(InvestigationButtonView view, Button control)
        {
            if (control != null && !control.interactable) return "Unavailable until its prerequisite is complete.";
            if (view.CurrentStyle == InvestigationButtonStyle.Navigation) return "Open this investigation step.";
            if (view.CurrentStyle == InvestigationButtonStyle.Destructive) return "This action can reset investigation progress.";
            return "Activate this action.";
        }

        private static bool Invoke(Button control)
        {
            if (control == null || !control.isActiveAndEnabled || !control.interactable) return false;
            control.onClick.Invoke();
            return true;
        }

        private static Rect GetScreenRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Rect(
                corners[0].x,
                corners[0].y,
                Mathf.Max(0f, corners[2].x - corners[0].x),
                Mathf.Max(0f, corners[2].y - corners[0].y));
        }

        private void OnDisable()
        {
            if (hierarchy != null && ReferenceEquals(AssistiveSupport.activeHierarchy, hierarchy))
            {
                AssistiveSupport.activeHierarchy = null;
            }
        }
    }
}
