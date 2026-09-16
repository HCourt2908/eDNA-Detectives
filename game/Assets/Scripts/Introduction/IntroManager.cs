using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{

    bool buttonPressed = false;
    bool typing = false;
    public Button playButton;
    public Button nextButton;
    public GameObject titleScreen;
    public GameObject loreScreen;
    public TMPro.TextMeshProUGUI text;
    public Image loreBackground;
    public Sprite basicBackground;
    public Sprite handUpBackground;
    public GameObject seamountMap;
    [SerializeField] List<GameObject> animals;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playButton.onClick.AddListener(() => {if (!typing) buttonPressed = true;});
        nextButton.onClick.AddListener(() => {if (!typing) buttonPressed = true;});
        loreScreen.SetActive(false);
        titleScreen.SetActive(true);
        seamountMap.SetActive(false);

        StartCoroutine(Introduction());
    }

    public IEnumerator Introduction()
    {
        yield return new WaitUntil(() => buttonPressed);
        foreach (GameObject animal in animals) animal.SetActive(false);
        buttonPressed = false;

        yield return StartCoroutine(TitleFade());
        
        typing = true;
        yield return TextTyper.TypeText(text, "Welcome aboard the OceanXplorer! I'm Professor Edna, the resident eDNA researcher on this ship.");
        typing = false;

        yield return new WaitUntil(() => buttonPressed);
        buttonPressed = false;

        loreBackground.sprite = handUpBackground;
        seamountMap.SetActive(true);
        
        typing = true;
        yield return TextTyper.TypeText(text, "This seamount here was surveyed 20 years ago, and found to be thriving with all kinds of species.");
        typing = false;

        yield return new WaitUntil(() => buttonPressed);
        buttonPressed = false;

        typing = true;  
        yield return TextTyper.TypeText(text, "But when we surveyed it again last week, we found a lot of unexpected changes on the seamount...");
        typing = false;

        yield return new WaitUntil(() => buttonPressed);
        buttonPressed = false;

        loreBackground.sprite = basicBackground;
        seamountMap.SetActive(false);

        typing = true;
        yield return TextTyper.TypeText(text, "Your job is to use our eDNA equipment to get to the root of the problem.");
        typing = false;

        yield return new WaitUntil(() => buttonPressed);
        buttonPressed = false;

        typing = true;
        yield return TextTyper.TypeText(text, "I've been told you're the best analyst we've got - let's see if you can solve the mystery behind the seamount!");
        typing = false;

        yield return new WaitUntil(() => buttonPressed);
        buttonPressed = false;

        typing = true;
        yield return TextTyper.TypeText(text, "And don't worry if this is all new to you. I'll be around to help you out if you get stuck!");
        typing = false;

        yield return new WaitUntil(() => buttonPressed);
        buttonPressed = false;

        SceneLoader.Instance.LoadSceneAdditive("CTD-Minigame");
        SceneLoader.Instance.UnloadScene("Introduction");
    }

    public IEnumerator TitleFade()
    {
        CanvasGroup titleCanvas = titleScreen.GetComponent<CanvasGroup>();
        CanvasGroup loreCanvas = loreScreen.GetComponent<CanvasGroup>();

        loreScreen.SetActive(true);
        loreCanvas.alpha = 0f;

        float duration = 1f;
        float time = 0f;
        
        while (time < duration)
        {
            time += Time.deltaTime;

            titleCanvas.alpha = 1 - time / duration;
            loreCanvas.alpha = time / duration;
            yield return null;
        }
        loreCanvas.alpha = 1f;
        titleScreen.SetActive(false);
    }
}
