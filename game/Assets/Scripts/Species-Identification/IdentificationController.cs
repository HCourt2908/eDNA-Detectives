using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.ComponentModel.Design;

public class sampleController : MonoBehaviour
{
    [SerializeField] List<SymbolPrefab> symbolPrefabs;
    [SerializeField] RectTransform symbolSpace;

    List<string> buttonTexts;
    [SerializeField] RectTransform buttonContent;
    [SerializeField] Button buttonPrefab;
    public List<Button> buttons = new();

    [SerializeField] string correctOption;

    [SerializeField] List<SymbolType> currentSequence;
    [SerializeField] private PuzzleGenerator puzzleGenerator;

    public float padding = 25f;
    public float symbolSize = 150f;
    [SerializeField] float symbolFadeDuration = 0.5f;
    public bool puzzleCorrect = false;

    Coroutine symbolTransition;

    List<Species> speciesList;
    Dictionary<Species, SpeciesFrequency> frequencyMap;

    [SerializeField] Image silhouetteImage;
    [SerializeField] Image colouredImage;
    private Coroutine colourFadeCoroutine;
    [SerializeField] TMPro.TMP_Text speciesIdentifiedText;
    [SerializeField] Button identifiedContinueButton;
    [SerializeField] GameObject speciesIdentifiedPanel;
    bool identifiedContinuePressed;



    public void Start()
    {
        speciesList = GameManager.Instance.speciesList;
        frequencyMap = GameManager.Instance.frequencyMap;
        identifiedContinueButton.onClick.AddListener(() => identifiedContinuePressed = true);
        speciesIdentifiedPanel.SetActive(false);
        StartCoroutine(LoadPuzzles());
    }

    private static List<Species> BuildSampleQueue(List<Species> speciesList, Dictionary<Species, SpeciesFrequency> frequencyMap)
    {
        var samplesFound = new List<Species>();
        foreach (var species in speciesList)
        {
            SpeciesFrequency frequency = frequencyMap[species];
            if (frequency == SpeciesFrequency.Missing) continue;
            samplesFound.Add(species);
            if (frequency == SpeciesFrequency.MoreFrequent) samplesFound.Add(species);
        }
        return samplesFound;
    }

    public IEnumerator LoadPuzzles()
    {
        List<Species> samplesFound = BuildSampleQueue(speciesList, frequencyMap);

        samplesFound = samplesFound.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < samplesFound.Count; i++)
        {
            if (i == 0 || i == 1) puzzleGenerator.difficulty = SpeciesPuzzleDifficulty.Easy;
            else if (i == 2) puzzleGenerator.difficulty = SpeciesPuzzleDifficulty.Medium;
            else if (i == 3) puzzleGenerator.difficulty = SpeciesPuzzleDifficulty.Hard;
            else puzzleGenerator.difficulty = SpeciesPuzzleDifficulty.VeryHard;

            CreateSpecificPuzzle(samplesFound[i].name);
            yield return new WaitUntil(() => puzzleCorrect);
            puzzleCorrect = false;
        }

        yield return new WaitForSeconds(3f);

        SceneLoader.Instance.LoadScene("InvestigationScene");
        //SceneLoader.Instance.UnloadScene("Species-Identification");
    }

    public void CreateRandomPuzzle()
    {
        puzzleGenerator.GenerateRandomPuzzle();

        currentSequence = new List<SymbolType>(puzzleGenerator.CurrentSequence);
        correctOption = puzzleGenerator.CorrectSpecies.name;

        buttonTexts = SpeciesDatabase.AllSpecies.Select(s => s.name).ToList();

        DisplaySymbols();
        DisplayButtons();
    }

    public void CreateSpecificPuzzle(string speciesName)
    {
        puzzleGenerator.GenerateSpecificPuzzle(speciesName);

        currentSequence = new List<SymbolType>(puzzleGenerator.CurrentSequence);
        correctOption = puzzleGenerator.CorrectSpecies.name;

        buttonTexts = SpeciesDatabase.AllSpecies.Select(s => s.name).ToList();

        DisplaySymbols();
        DisplayButtons();
    }

    public void DisplaySymbols()
    {
        if (symbolTransition != null) StopCoroutine(symbolTransition);
        symbolTransition = StartCoroutine(DisplaySymbolsWithFade());
    }

    IEnumerator DisplaySymbolsWithFade()
    {
        List<CanvasGroup> oldSymbolGroups = new();
        foreach (Transform child in symbolSpace)
        {
            CanvasGroup group = child.GetComponent<CanvasGroup>();
            if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            oldSymbolGroups.Add(group);
        }

        float elapsed = 0f;
        while (elapsed < symbolFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / symbolFadeDuration);
            foreach (CanvasGroup group in oldSymbolGroups) group.alpha = alpha;
            yield return null;
        }

        foreach (Transform child in symbolSpace) Destroy(child.gameObject);

        int count = currentSequence.Count;

        float width = symbolSpace.rect.width;
    
        float firstX = padding + symbolSize / 2f;
        float lastX = width - padding - symbolSize / 2f;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = GetSymbol(currentSequence[i]);
            GameObject symbol = Instantiate(prefab, symbolSpace);

            RectTransform rect = symbol.GetComponent<RectTransform>();

            rect.sizeDelta = new Vector2(symbolSize, symbolSize);

            float xValue = 0f;
            if (count == 1) xValue = width / 2f;
            else xValue = Mathf.Lerp(firstX, lastX, (float)i / (count - 1));

            rect.anchoredPosition = new Vector2(xValue - width / 2f, 0f);

            CanvasGroup group = symbol.GetComponent<CanvasGroup>();
            if (group == null) group = symbol.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        elapsed = 0f;
        while (elapsed < symbolFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / symbolFadeDuration);
            foreach (Transform child in symbolSpace)
            {
                CanvasGroup group = child.GetComponent<CanvasGroup>();
                group.alpha = alpha;
            }
            yield return null;
        }

        foreach (Transform child in symbolSpace)
        {
            child.GetComponent<CanvasGroup>().alpha = 1f;
        }
        symbolTransition = null;
    }

    public void DisplayButtons()
    {
        foreach (Transform child in buttonContent)
        {
            Destroy(child.gameObject);
        }

        buttons.Clear();

        foreach (Species species in SpeciesDatabase.AllSpecies)
        {
            Button button = Instantiate(buttonPrefab, buttonContent);

            button.GetComponentInChildren<TMPro.TMP_Text>().text = "";
            
            Image silhouetteImage = button.transform.Find("SilhouetteImage").GetComponent<Image>();
            silhouetteImage.sprite = species.GetSilhouetteImage();
            silhouetteImage.preserveAspect = true;
            silhouetteImage.type = Image.Type.Simple;

            button.onClick.AddListener(() => CheckCorrectness(button, species.name));

            buttons.Add(button);
        }

        if (buttons.Count > 0 && Gamepad.current != null) EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);

    }

    public void CheckCorrectness(Button guessButton, string guess)
    {
        if (guess == correctOption) 
        {
            StartCoroutine(flashCorrect(guessButton));
            StartCoroutine(SpeciesIdentified(correctOption));
        }
        else
        {
            StartCoroutine(flashIncorrect(guessButton));
        }
    }

    public IEnumerator flashCorrect(Button button)
    {
        float duration = 0.5f;

        Image image = button.GetComponent<Image>();
        Color originalColor = image.color;

        image.color = Color.green;

        yield return new WaitForSeconds(duration);

        image.color = originalColor;

        puzzleCorrect = true;
        }

    public IEnumerator flashIncorrect(Button button)
    {
        float duration = 0.5f;

        Image image = button.GetComponent<Image>();
        Color originalColor = image.color;

        image.color = Color.red;

        yield return new WaitForSeconds(duration);

        image.color = originalColor;
        }

    public IEnumerator SpeciesIdentified(string name)
    {
        identifiedContinuePressed = false;
        speciesIdentifiedText.text = name;

        Species species = SpeciesDatabase.AllSpecies.Find(s => s.name == name);
        Sprite silhouette = species.GetSilhouetteImage();
        Sprite colour = species.GetColouredImage();

        silhouetteImage.sprite = silhouette;
        colouredImage.sprite = colour;
        CanvasGroup colourGroup = colouredImage.GetComponent<CanvasGroup>();
        colourGroup.alpha = 0f;

        speciesIdentifiedPanel.SetActive(true);

        CanvasGroup canvasGroup = speciesIdentifiedPanel.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        StartCoroutine(FadeInColour(silhouette, colour));


        yield return new WaitUntil(() => identifiedContinuePressed);
        identifiedContinuePressed = false;

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        speciesIdentifiedPanel.SetActive(false);
    }


    public GameObject GetSymbol(SymbolType type)
    {
        return symbolPrefabs.Find(x => x.type == type).prefab;
    }

    private IEnumerator FadeInColour(Sprite silhouette, Sprite colour)
    {
        // Stop any existing fade
        if (colourFadeCoroutine != null)
        {
            StopCoroutine(colourFadeCoroutine);
            colourFadeCoroutine = null;
        }

        // Set both sprites immediately
        silhouetteImage.sprite = silhouette;
        colouredImage.sprite = colour;

        silhouetteImage.preserveAspect = true;
        colouredImage.preserveAspect = true;

        // Ensure both images are visible
        silhouetteImage.gameObject.SetActive(true);
        colouredImage.gameObject.SetActive(true);

        // Reset the coloured image alpha
        CanvasGroup colourGroup = colouredImage.GetComponent<CanvasGroup>();
        colourGroup.alpha = 0f;

        // Make sure the UI has updated before starting
        yield return new WaitForSeconds(0.5f);

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            colourGroup.alpha = Mathf.Clamp01(elapsed / duration);

            yield return null;
        }

        colourGroup.alpha = 1f;
        colourFadeCoroutine = null;
    }
}
