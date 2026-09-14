using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{

    bool buttonPressed = false;
    public Button playButton;

    public GameObject titleScreen;
    public GameObject loreScreen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playButton.onClick.AddListener(() => buttonPressed = true);
        loreScreen.SetActive(false);
    }

    public IEnumerator Introduction()
    {
        yield return new WaitUntil(() => buttonPressed);
        buttonPressed = false;
        titleScreen.SetActive(false);
        loreScreen.SetActive(true);
    }
}
