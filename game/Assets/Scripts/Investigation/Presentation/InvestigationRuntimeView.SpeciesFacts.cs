using EDNA.Investigation.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private void BuildSpeciesFactsContent(InvestigationSpeciesDefinition species, SurveyEra era, bool pinned)
        {
            float y = 12f;
            AddSpeciesFactsText("Tooltip Eyebrow", "SPECIES FACTS", 11, FontStyle.Bold, InvestigationTheme.Primary, ref y, 18f);
            y = Mathf.Max(y, 46f);
            if (pinned)
            {
                Button close = CreateButton("Close Species Facts", speciesTooltip, "Close", ButtonVisualStyle.Tertiary, HideSpeciesTooltip, out Text label);
                close.GetComponent<LayoutElement>().ignoreLayout = true;
                label.fontSize = 12;
                Anchor(close.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -72f, -42f, -8f, -4f);
            }
            AddSpeciesFactsText("Tooltip Title", species.DisplayName, 19, FontStyle.Bold, InvestigationTheme.TextPrimary, ref y);
            AddSpeciesFactsText("Tooltip Scientific Name", species.ScientificName, 13, FontStyle.Italic, InvestigationTheme.Primary, ref y);
            y += 6f;
            AddSpeciesFactsText("Tooltip Description", species.Description, 13, FontStyle.Normal, InvestigationTheme.TextSecondary, ref y);
            y += 12f;
            bool example = FindObserveObservationForSpecies(species.SpeciesId) != null;
            AddSpeciesFactsText("Tooltip Survey Heading", example ? "THIS EXAMPLE SURVEY" : "IMPORTED SURVEY RECORD", 11,
                FontStyle.Bold, InvestigationTheme.Primary, ref y);
            AddSpeciesFactsText("Tooltip Details", BuildSpeciesTooltipDetails(species, era), 11, FontStyle.Normal,
                InvestigationTheme.TextSecondary, ref y);
            speciesTooltip.sizeDelta = new Vector2(speciesTooltip.sizeDelta.x, y + 12f);
        }

        private void AddSpeciesFactsText(string objectName, string value, int size, FontStyle style, Color color, ref float top, float minimumHeight = 0f)
        {
            Text text = CreateText(objectName, speciesTooltip, value, size, style, color, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            float width = speciesTooltip.sizeDelta.x - 32f;
            float height = Mathf.Max(minimumHeight, Mathf.Ceil(text.cachedTextGeneratorForLayout.GetPreferredHeight(value,
                text.GetGenerationSettings(new Vector2(width, 0f))) / text.pixelsPerUnit) + 2f);
            Anchor(text.rectTransform, 0f, 1f, 1f, 1f, 16f, -top - height, -16f, -top);
            top += height + 4f;
        }
    }
}
