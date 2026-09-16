using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed partial class InvestigationRuntimeView
    {
        private bool notebookFoodWebReferenceOpen;

        private void RenderFoodWebReference(Transform parent)
        {
            Button toggle = CreateButton("Notebook Food Web Reference", parent,
                notebookFoodWebReferenceOpen ? "Hide food-chain examples" : "Food-chain examples →",
                ButtonVisualStyle.PaperChoice, () =>
                {
                    notebookFoodWebReferenceOpen = !notebookFoodWebReferenceOpen;
                    notebookFocusTargetAfterRender = "Notebook Food Web Reference";
                    RefreshPresentationOnly();
                }, out _);
            AddLayout(toggle.GetComponent<RectTransform>(), 44f, 0f);
            if (!notebookFoodWebReferenceOpen) return;
            TextMeshProUGUI note = CreateText("Food Web Reference Note", parent,
                "Arrows mean eats. These are illustrative food webs, not additional survey findings. Organisms are not to scale.",
                12, FontStyle.Normal, InvestigationTheme.PaperInk, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
            ConfigureContentDrivenText(note);
            var networks = new[] { "reference_main", "manta_branch", "deep_branch", "benthic_branch" };
            var labels = new[] { "Main food-chain example", "Manta example", "Deep-water example", "Seafloor example" };
            for (int n = 0; n < networks.Length; n++)
            {
                var ids = new List<string>();
                foreach (var edge in caseDefinition.FoodWebEdges)
                {
                    if (edge.NetworkId != networks[n]) continue;
                    if (!ids.Contains(edge.PredatorSpeciesId)) ids.Add(edge.PredatorSpeciesId);
                    if (!ids.Contains(edge.PreySpeciesId)) ids.Add(edge.PreySpeciesId);
                }
                if (ids.Count == 0) continue;
                RectTransform row = CreatePanel("Food Web Reference " + networks[n], parent,
                    InvestigationTheme.PaperRaised, InvestigationTheme.SmallRadius);
                AddLayout(row, 106f, 0f);
                TextMeshProUGUI heading = CreateText("Reference Network Name", row, labels[n], 12,
                    FontStyle.Bold, InvestigationTheme.PaperSelectedBorder, TextAnchor.UpperLeft, InvestigationTheme.BodyFont);
                Anchor(heading.rectTransform, 0f, 1f, 1f, 1f, 8f, -22f, -8f, -3f);
                for (int i = 0; i < ids.Count; i++)
                {
                    var species = caseDefinition.FindSpecies(ids[i]);
                    if (species == null) continue;
                    if (species.Icon != null)
                    {
                        var art = CreateStatusIcon("Reference Species " + ids[i], row, species.Icon, Color.white);
                        Anchor(art.rectTransform, i / (float)ids.Count, .42f, (i + 1f) / ids.Count, .78f, 5f, 0f, -8f, 0f);
                    }
                    TextMeshProUGUI name = CreateText("Reference Species Name", row, species.GameplayName, 10,
                        FontStyle.Bold, InvestigationTheme.PaperInk, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                    Anchor(name.rectTransform, i / (float)ids.Count, .02f, (i + 1f) / ids.Count, species.Icon != null ? .43f : .78f, 2f, 0f, -6f, 0f);
                    if (i == ids.Count - 1) continue;
                    TextMeshProUGUI arrow = CreateText("Reference Feeding Arrow", row, "→", 14, FontStyle.Bold,
                        InvestigationTheme.PaperSelectedBorder, TextAnchor.MiddleCenter, InvestigationTheme.BodyFont);
                    Anchor(arrow.rectTransform, (i + 1f) / ids.Count, .46f, (i + 1f) / ids.Count, .70f, -9f, 0f, 7f, 0f);
                }
            }
        }
    }
}
