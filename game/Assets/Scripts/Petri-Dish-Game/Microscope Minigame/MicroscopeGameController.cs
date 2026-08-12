using System;
using System.Collections;
using UnityEngine;

public class MicroscopeGameController : MonoBehaviour
{
    [SerializeField] private GameObject mainGame;
    [SerializeField] private GameObject microscopeMinigame;
    [SerializeField] private GameObject fadeInOverlay;
    [SerializeField] private ScreenFade screenFadeIn;
    [SerializeField] private GameObject fadeOutOverlay;
    [SerializeField] private ScreenFade screenFadeOut;
    [SerializeField] private IdentificationTreeManager identificationTreeManager;

    public void Awake()
    {
        fadeInOverlay.SetActive(false);
        fadeOutOverlay.SetActive(false);
    }

    public void OpenMicroscope()
    {
        fadeInOverlay.SetActive(true);
        StartCoroutine(screenFadeIn.FadeToBlack(0.5f));
    }

    public void buttonOpenClick()
    {
        StartCoroutine(OpenMicroscopeGame());
    }

    public IEnumerator OpenMicroscopeGame()
    {
        mainGame.SetActive(false);
        microscopeMinigame.SetActive(true);
        
        yield return StartCoroutine(screenFadeIn.FadeFromBlack(0.5f));
        
        fadeInOverlay.SetActive(false);
    }

    public void CloseMicroscope()
    {
        fadeOutOverlay.SetActive(true);
        StartCoroutine(screenFadeOut.FadeToBlack(0.5f));
    }

    public void buttonCloseClick()
    {
        StartCoroutine(CloseMicroscopeGame());
    }

    public IEnumerator CloseMicroscopeGame()
    {
        microscopeMinigame.SetActive(false);
        mainGame.SetActive(true);

        yield return StartCoroutine(screenFadeOut.FadeFromBlack(0.5f));

        fadeOutOverlay.SetActive(false);

        identificationTreeManager.RevealNext();
    }
}
