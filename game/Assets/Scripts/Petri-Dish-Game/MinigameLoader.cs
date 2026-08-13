using System.Collections;
using UnityEngine;

public class MinigameLoader : MonoBehaviour
{
    [SerializeField] private GameObject mainGame;
    [SerializeField] private GameObject minigame;
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

    public void BeginningLoadingScreen()
    {
        fadeInOverlay.SetActive(true);
        StartCoroutine(screenFadeIn.FadeToBlack(0.5f));
        mainGame.SetActive(false);
    }

    public void BeginButtonClick()
    {
        StartCoroutine(BeginMinigame());
    }

    public IEnumerator BeginMinigame()
    {
        minigame.SetActive(true);

        yield return StartCoroutine(screenFadeIn.FadeFromBlack(0.5f));

        fadeInOverlay.SetActive(false);
    }

    public void CompletedLoadingScreen()
    {
        fadeOutOverlay.SetActive(true);
        StartCoroutine(screenFadeOut.FadeToBlack(0.5f));
        minigame.SetActive(false);
    }

    public void CompletedButtonClick()
    {
        StartCoroutine(CloseMinigame());
    }

    public IEnumerator CloseMinigame()
    {
        mainGame.SetActive(true);
        
        yield return StartCoroutine(screenFadeOut.FadeFromBlack(0.5f));

        fadeOutOverlay.SetActive(false);

        identificationTreeManager.RevealNext();
    }
}
