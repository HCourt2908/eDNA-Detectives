using EDNA.Investigation.Domain;
using UnityEngine;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private InvestigationCaseDefinition caseDefinition;
        [SerializeField] private GameObject viewPrefab;

        private void Awake()
        {
            if (caseDefinition == null)
            {
                Debug.LogError("Investigation case asset is missing.");
                return;
            }
            if (viewPrefab == null)
            {
                Debug.LogError("Investigation UI prefab is missing.");
                return;
            }

            GameObject viewObject = Instantiate(viewPrefab);
            viewObject.name = "Investigation UI";
            InvestigationRuntimeView view = viewObject.GetComponent<InvestigationRuntimeView>();
            InvestigationController controller = viewObject.GetComponent<InvestigationController>();
            if (view == null || controller == null)
            {
                Debug.LogError("Investigation prefab requires both the runtime view and controller.");
                return;
            }
            controller.Initialize(caseDefinition, view);
        }
    }
}
