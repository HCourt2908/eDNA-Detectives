using EDNA.Investigation.Domain;
using UnityEngine;

namespace EDNA.Investigation
{
    public sealed class InvestigationCaseRepository : MonoBehaviour
    {
        [SerializeField] private InvestigationCaseDefinition caseDefinition;

        public InvestigationCaseDefinition CaseDefinition => caseDefinition;

        public void SetCase(InvestigationCaseDefinition definition)
        {
            caseDefinition = definition;
        }
    }
}
