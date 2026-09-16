using UnityEngine;
using EDNA.Core;
using EDNA.Investigation;
using EDNA.Investigation.Domain;
using UnityEngine.UI;

public class CompleteGameButton : MonoBehaviour
{

    public Button completeGameButton;
    public GameObject buttonObject;
    private InvestigationRuntimeView runtimeView;

    private void OnEnable()
    {
        runtimeView = FindAnyObjectByType<InvestigationRuntimeView>();
        InvestigationSessionBridge.GameCompleted += HandleGameCompleted;
        if (runtimeView != null) runtimeView.PhaseChanged += HandlePhaseChanged;
        RefreshVisibility();
    }

    private void OnDisable()
    {
        InvestigationSessionBridge.GameCompleted -= HandleGameCompleted;
        if (runtimeView != null) runtimeView.PhaseChanged -= HandlePhaseChanged;
    }

    private void HandleGameCompleted(InvestigationGameResult result)
    {
        RefreshVisibility();
    }

    private void HandlePhaseChanged(InvestigationPhase phase)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool investigateTab = runtimeView != null
            && runtimeView.State != null
            && runtimeView.State.Phase != InvestigationPhase.Observe
            && !runtimeView.RestartConfirmationPending;
        bool visible = InvestigationSessionBridge.IsComplete && investigateTab;
        if (buttonObject != null) buttonObject.SetActive(visible);
        if (completeGameButton != null) completeGameButton.interactable = visible;
    }

    public void LoadEndScene()
    {
        Debug.Log("Investigation Complete");
        InvestigationSessionBridge.Clear();
        SceneLoader.Instance.LoadScene("End");
        SceneLoader.Instance.UnloadScene("InvestigationScene");
    }
}
