using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectFirstUIElement : MonoBehaviour
{

    [SerializeField] private Selectable firstElement;

    private void OnEnable()
    {
        StartCoroutine(SelectElement());
    }

    private IEnumerator SelectElement()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
    }
}
