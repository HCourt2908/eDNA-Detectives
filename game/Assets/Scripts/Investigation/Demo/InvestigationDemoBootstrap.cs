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
            if (viewPrefab == null)
            {
                Debug.LogError("The Investigation UI prefab is missing from the demo bootstrap.");
                return;
            }

            GameObject viewObject = Instantiate(viewPrefab, transform);
            InvestigationRuntimeView view = viewObject.GetComponent<InvestigationRuntimeView>();
            if (view == null)
            {
                Debug.LogError("The Investigation UI prefab has no InvestigationRuntimeView component.");
                return;
            }

            InvestigationController controller = gameObject.AddComponent<InvestigationController>();
            controller.Initialize(caseDefinition, view);
        }
    }
}
