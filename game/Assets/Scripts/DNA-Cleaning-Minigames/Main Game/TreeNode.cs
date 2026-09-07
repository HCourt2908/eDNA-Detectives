using TMPro;
using UnityEngine;

public class TreeNode : MonoBehaviour
{
    
    public TMP_Text nodeText;

    public void SetText(string text)
    {
        nodeText.text = text;
    }
}
