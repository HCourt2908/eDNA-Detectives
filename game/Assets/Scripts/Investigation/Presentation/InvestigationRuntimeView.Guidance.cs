using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private void ShowReferenceSurveyNotice()
        {
            statusMessage = "This is the 20-year reference survey. Return to the record table to compare it with today.";
            statusTone = InvestigationStatusTone.Notice;
            RenderChrome();
        }

        private void AddChoiceBorderCue(string name, Transform target)
        {
            InvestigationBorderGraphic border = CreateGraphic<InvestigationBorderGraphic>(name, target);
            Stretch(border.rectTransform, 1f, 1f, -1f, -1f);
            border.Configure(InvestigationTheme.SmallRadius, 1.5f);
            Color color = InvestigationTheme.Primary;
            color.a = 0.75f;
            border.color = color;
            border.gameObject.AddComponent<InvestigationGuidancePulse>();
        }
    }
}
