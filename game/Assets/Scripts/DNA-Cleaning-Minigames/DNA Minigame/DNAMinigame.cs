using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DNAMinigame : MonoBehaviour
{

    [SerializeField] private string dnaSequence;


    [Header("DNA Display")]
    [SerializeField] private GameObject dnaBasePrefab;
    [SerializeField] private Transform originalSequence;
    [SerializeField] private Transform answerSequence;
    [SerializeField] private RectTransform positionArrow;

    [Header("Buttons")]
    [SerializeField] private Button aButton;
    [SerializeField] private Button tButton;
    [SerializeField] private Button gButton;
    [SerializeField] private Button cButton;

    private int currentPosition;

    private List<RectTransform> originalBases;
    private List<TMP_Text> answerBaseTexts;
    private List<Image> answerBaseImages;

    [SerializeField] private MinigameLoader minigameLoader;

    private Coroutine incorrectFeedbackCoroutine;


    void OnEnable()
    {

        currentPosition = 0;

        foreach (Transform child in originalSequence)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in answerSequence)
        {
            Destroy(child.gameObject);
        }

        aButton.onClick.RemoveAllListeners();
        tButton.onClick.RemoveAllListeners();
        gButton.onClick.RemoveAllListeners();
        cButton.onClick.RemoveAllListeners();

        originalBases = new List<RectTransform>();
        answerBaseTexts = new List<TMP_Text>();
        answerBaseImages = new List<Image>();
        positionArrow.gameObject.SetActive(true);

        dnaSequence = GenerateDNASequence();

        CreateDNASequence();

        aButton.onClick.AddListener(() => SelectBase('A', aButton));
        tButton.onClick.AddListener(() => SelectBase('T', tButton));
        gButton.onClick.AddListener(() => SelectBase('G', gButton));
        cButton.onClick.AddListener(() => SelectBase('C', cButton));

        StartCoroutine(SetupArrow());

    }

    private IEnumerator SetupArrow()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            originalSequence.GetComponent<RectTransform>()
        );

        positionArrow.position = new Vector2(originalBases[0].position.x, positionArrow.position.y);
    }

    private string GenerateDNASequence()
    {
        int length = Random.Range(5, 11);
        string seq = "";

        for (int i = 0; i < length; i++)
        {
            int rand = Random.Range(1, 5);

            switch (rand)
            {
                case 1:
                seq += "A";
                break;

                case 2:
                seq += "T";
                break;
                
                case 3:
                seq += "G";
                break;

                case 4:
                seq += "C";
                break;

                default:
                seq += "?";
                break;
            }
        }

        return seq;
    }

    private void CreateDNASequence()
    {
        for (int i  = 0; i < dnaSequence.Length; i++)
        {
            GameObject originalBase = Instantiate(dnaBasePrefab, originalSequence);
            TMP_Text originalText = originalBase.GetComponentInChildren<TMP_Text>();
            originalText.text = dnaSequence[i].ToString();

            originalBases.Add(originalBase.GetComponent<RectTransform>());

            GameObject answerBase = Instantiate(dnaBasePrefab, answerSequence);

            TMP_Text answerText = answerBase.GetComponentInChildren<TMP_Text>();
            answerText.text = "";
            answerBaseTexts.Add(answerText);

            Image answerImage = answerBase.GetComponent<Image>();
            answerBaseImages.Add(answerImage);
        }
    }

    private void SelectBase(char selectedBase, Button button)
    {
        char correctBase = GetComplementaryBase(dnaSequence[currentPosition]);

        if (selectedBase == correctBase)
        {
            answerBaseTexts[currentPosition].text = selectedBase.ToString();

            StartCoroutine(CorrectFeedback(answerBaseTexts[currentPosition]));

            currentPosition++;

            if (currentPosition >= dnaSequence.Length)
            {
                StartCoroutine(CompleteMinigame());
            } else
            {
                StartCoroutine(MoveArrow());
            }
        } else
        {
            if (incorrectFeedbackCoroutine != null)
            {
                StopCoroutine(incorrectFeedbackCoroutine);
            }
            incorrectFeedbackCoroutine = StartCoroutine(IncorrectFeedback(button));
        }
    }

    private IEnumerator CorrectFeedback(TMP_Text text)
    {
        text.color = Color.green;

        yield return new WaitForSeconds(0.5f);

        text.color = Color.white;
    }

    private IEnumerator IncorrectFeedback(Button button)
    {
        Image buttonImage = button.GetComponent<Image>();

        Color originalColor = new Color(191, 191, 191);

        buttonImage.color = Color.red;

        yield return new WaitForSeconds(0.2f);

        buttonImage.color = originalColor;

        incorrectFeedbackCoroutine = null;
    }
    
    private char GetComplementaryBase(char basePair)
    {
        switch(basePair)
        {
            case 'A':
            return 'T';

            case 'T':
            return 'A';

            case 'G':
            return 'C';
            
            case 'C':
            return 'G';

            default:
            return '?';
        }
    }

    private IEnumerator MoveArrow()
    {
        float startPosition = positionArrow.position.x;

        RectTransform currentBase = originalBases[currentPosition];
        float goalPosition = currentBase.position.x;

        float time = 0f;
        float duration = 0.5f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float newXPosition = Mathf.Lerp(startPosition, goalPosition, time / duration);

            positionArrow.position = new Vector2(newXPosition, positionArrow.position.y);

            yield return null;
        }

        positionArrow.position = new Vector2(goalPosition, positionArrow.position.y);
    }

    private IEnumerator CompleteMinigame()
    {
        yield return new WaitForSeconds(1f); 
        
        yield return StartCoroutine(successScene());

        yield return new WaitForSeconds(1f);

        minigameLoader.CompletedLoadingScreen();
    }

    private IEnumerator successScene()
    {
        float time = 0f;
        float duration = 0.5f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress = 255 * (time / duration);

            foreach (Image image in answerBaseImages)
            {
                image.color = new Color(image.color.r, progress, image.color.b);
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress = 255 * (1 - (time / duration));

            foreach (Image image in answerBaseImages)
            {
                image.color = new Color(image.color.r, progress, image.color.b);
            }

            yield return null;
        }
    }
}
