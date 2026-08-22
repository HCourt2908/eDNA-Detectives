using EDNA.Investigation.V2.Domain;
using UnityEngine;

namespace EDNA.Investigation.V2
{
    [DisallowMultipleComponent]
    public sealed class InvestigationV2DemoBootstrap : MonoBehaviour
    {
        [SerializeField] private InvestigationV2CaseDefinition caseDefinition;
        [SerializeField] private GameObject viewPrefab;

        private void Awake()
        {
            if (caseDefinition == null)
            {
                Debug.LogError("Investigation V2 case asset is missing.");
                return;
            }
            if (viewPrefab == null)
            {
                Debug.LogError("Investigation V2 UI prefab is missing.");
                return;
            }

            GameObject viewObject = Instantiate(viewPrefab);
            viewObject.name = "Investigation V2 UI";
            InvestigationV2RuntimeView view = viewObject.GetComponent<InvestigationV2RuntimeView>();
            InvestigationV2Controller controller = viewObject.GetComponent<InvestigationV2Controller>();
            if (view == null || controller == null)
            {
                Debug.LogError("Investigation V2 prefab requires both the runtime view and controller.");
                return;
            }
            controller.Initialize(caseDefinition, view);
        }
    }
}
