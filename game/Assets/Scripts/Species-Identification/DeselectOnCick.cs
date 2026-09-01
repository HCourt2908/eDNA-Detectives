using UnityEngine;
using UnityEngine.EventSystems;

public class DeselectOnClick : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}