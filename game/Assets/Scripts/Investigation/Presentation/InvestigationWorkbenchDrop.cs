using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EDNA.Investigation
{
    public sealed class InvestigationWorkbenchDrop : MonoBehaviour, IDropHandler
    {
        private string kind;
        private Action<string> receive;
        public void Configure(string acceptedKind, Action<string> onDrop) { kind = acceptedKind; receive = onDrop; }
        public void OnDrop(PointerEventData eventData)
        {
            InvestigationWorkbenchDrag source = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<InvestigationWorkbenchDrag>();
            if (source != null && source.Kind == kind) receive?.Invoke(source.Value);
        }
    }
}
