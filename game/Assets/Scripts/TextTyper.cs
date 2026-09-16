using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public static class TextTyper
{

    public static IEnumerator TypeText(TextMeshProUGUI text, string message, float delay = 0.025f)
    {
        text.text = message;
        text.maxVisibleCharacters = 0;

        for(int i = 0; i <= message.Length; i++)
        {
            text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
    }
    
}
