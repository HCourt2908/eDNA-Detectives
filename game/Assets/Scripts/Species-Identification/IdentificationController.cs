using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
    public float symbolSize = 75f;



    public void Start()
    {
        puzzleGenerator.GeneratePuzzle();

        currentSequence = new List<SymbolType>(puzzleGenerator.CurrentSequence);
        correctOption = puzzleGenerator.CorrectSpecies.name;

        buttonTexts = SpeciesDatabase.AllSpecies.Select(s => s.name).ToList();

        DisplaySymbols();
        DisplayButtons();

    }

    public void DisplaySymbols()
    {
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

            float xValue = 0f;
            if (count == 1) xValue = width / 2f;
            else xValue = Mathf.Lerp(firstX, lastX, (float)i / (count - 1));

            rect.anchoredPosition = new Vector2(xValue - width / 2f, 0f);
        }
    }

    public void DisplayButtons()
    {
        foreach (Transform child in buttonContent)
        {
            Destroy(child.gameObject);
        }

        buttons.Clear();

        foreach (string buttonText in buttonTexts)
        {
            Button button = Instantiate(buttonPrefab, buttonContent);

            button.GetComponentInChildren<TMPro.TMP_Text>().text = buttonText;

            button.onClick.AddListener(() => CheckCorrectness(button, buttonText));

            buttons.Add(button);
        }

        if (buttons.Count > 0) EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);

    }

    public void CheckCorrectness(Button guessButton, string guess)
    {
        if (guess == correctOption) 
        {
            Debug.Log("Correct");
            StartCoroutine(flashCorrect(guessButton));
        }
        else Debug.Log("Incorrect");
    }

    public IEnumerator flashCorrect(Button button)
    {
        float duration = 0.5f;
        Image image = button.GetComponent<Image>();
        image.color = Color.green;
        
        yield return new WaitForSeconds(duration);

        image.color = Color.white;
    }


    public GameObject GetSymbol(SymbolType type)
    {
        return symbolPrefabs.Find(x => x.type == type).prefab;
    }
}
