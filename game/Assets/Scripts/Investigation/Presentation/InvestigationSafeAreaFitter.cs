using UnityEngine;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class InvestigationSafeAreaFitter : MonoBehaviour
    {
        private RectTransform target;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void OnEnable()
        {
            target = GetComponent<RectTransform>();
            ApplySafeArea(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplySafeArea(false);
        }

        private void LateUpdate()
        {
            ApplySafeArea(false);
        }

        private void ApplySafeArea(bool force)
        {
            if (target == null || Screen.width <= 0 || Screen.height <= 0) return;

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize) return;

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}
