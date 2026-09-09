using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed class InvestigationScenarioTerrain : MonoBehaviour
    {
        private Material material;
        public void Freeze(RawImage image)
        {
            if (image == null || image.material == null) return;
            material = new Material(image.material) { name = "Scenario Stable Baseline", hideFlags = HideFlags.HideAndDontSave };
            material.SetFloat("_BlendB", 0f);
            material.SetFloat("_BlendC", 0f);
            image.material = material;
        }
        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
