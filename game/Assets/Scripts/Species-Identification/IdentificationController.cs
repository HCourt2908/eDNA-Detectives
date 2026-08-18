using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum SymbolType
{
    Square,
    Diamond,
    Rhombus,
    Triangle,
    Semicircle,
    Circle,
    Plus,
    Cross,
    Hexagon,
    Star,
    Pentagon
}

public class sampleController : MonoBehaviour
{
    [SerializeField] List<SymbolPrefab> symbolPrefabs;
    [SerializeField] RectTransform symbolSpace;

    [SerializeField] List<string> buttonTexts;
    [SerializeField] RectTransform buttonSpace;
    [SerializeField] Button buttonPrefab;
    public List<Button> buttons;

    [SerializeField] string correctOption;

    [SerializeField] List<SymbolType> currentSequence;

    public float padding = 25f;
    public float symbolSize = 75f;
    float buttonSize = 100f;

    private Dictionary<List<SymbolType>, string> possibleSequences = new()
    {
        {new() {SymbolType.Diamond, SymbolType.Rhombus}, "Algae"},
        {new() {SymbolType.Square, SymbolType.Triangle, SymbolType.Pentagon}, "Shark"},
        {new() {SymbolType.Square, SymbolType.Triangle, SymbolType.Star}, "Tuna"},
        {new() {SymbolType.Square, SymbolType.Semicircle, SymbolType.Hexagon}, "Sea Lion"},
        {new() {SymbolType.Square, SymbolType.Circle, SymbolType.Cross}, "Seagull"},
        {new() {SymbolType.Square, SymbolType.Circle, SymbolType.Plus}, "Albatross"}
    };

    public void Start()
    {
        KeyValuePair<List<SymbolType>, string> selected = possibleSequences.ElementAt(Random.Range(0, possibleSequences.Count));

        currentSequence = new List<SymbolType>(selected.Key);
        correctOption = selected.Value;

        buttonTexts = new List<string>();
        buttonTexts.AddRange(new[] {"Albatross", "Seagull", "Sea Lion", "Tuna", "Shark", "Algae"});

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
        foreach (Transform child in buttonSpace) Destroy(child.gameObject);

        int count = buttonTexts.Count;

        if (count == 0) return;

        float width = buttonSpace.rect.width;
        float height = buttonSpace.rect.height;

        int columns = 2;
        int rows = Mathf.CeilToInt(count / 2f);
        
        float leftX = padding + buttonSize / 2f;
        float rightX = width - padding - buttonSize /2f;

        float firstY = height - padding - buttonSize / 2f;
        float lastY = padding + buttonSize / 2f;

        for (int i = 0; i < count; i++)
        {
            Button button = Instantiate(buttonPrefab, buttonSpace);
            RectTransform rect = button.GetComponent<RectTransform>();

            int row = i / columns;
            int column = i % columns;

            float x = 0f;
            if (row == rows - 1 && count % 2 == 1) x = width / 2f;
            else if (column == 0) x = leftX;
            else x = rightX;

            float y = 0f;
            if (rows == 1) y = height / 2f;
            else y = Mathf.Lerp(firstY, lastY, (float)row / (rows-1));

            rect.anchoredPosition = new Vector2(x - width / 2f, y - height / 2f);

            string buttonText = buttonTexts[i];
            button.GetComponentInChildren<TMPro.TMP_Text>().text = buttonText;
            button.onClick.AddListener(() => CheckCorrectness(button, buttonText));
            buttons.Add(button);
        }

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
