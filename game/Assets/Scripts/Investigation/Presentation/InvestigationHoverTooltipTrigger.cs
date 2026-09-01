using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationHoverTooltipTrigger : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private float delaySeconds = 1f;
        private Action show;
        private Action hide;
        private Coroutine pendingShow;
        private bool pointerInside;
        private bool selected;

        public void Configure(float delay, Action onShow, Action onHide)
        {
            delaySeconds = Mathf.Max(0f, delay);
            show = onShow;
            hide = onHide;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            StartDelayedShow();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            CancelPendingShow();
            if (!selected) hide?.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            // Pointer clicks also select a Button. Hover owns presentation while
            // the pointer is inside; only pointer-free selection is keyboard focus.
            if (pointerInside) return;
            selected = true;
            CancelPendingShow();
            show?.Invoke();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            if (!pointerInside) hide?.Invoke();
        }

        private void OnDisable()
        {
            pointerInside = false;
            selected = false;
            CancelPendingShow();
            hide?.Invoke();
        }

        private void StartDelayedShow()
        {
            CancelPendingShow();
            pendingShow = StartCoroutine(ShowAfterDelay());
        }

        private IEnumerator ShowAfterDelay()
        {
            if (delaySeconds > 0f) yield return new WaitForSecondsRealtime(delaySeconds);
            pendingShow = null;
            if (pointerInside) show?.Invoke();
        }

        private void CancelPendingShow()
        {
            if (pendingShow == null) return;
            StopCoroutine(pendingShow);
            pendingShow = null;
        }
    }
}
