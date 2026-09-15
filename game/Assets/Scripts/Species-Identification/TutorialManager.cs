using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{

    [SerializeField] private GameObject leftTutorialPanel;
    [SerializeField] private TMPro.TextMeshProUGUI leftTutorialText;
    [SerializeField] private Button leftTutorialButton;

    [SerializeField] private GameObject rightTutorialPanel;
    [SerializeField] private TMPro.TextMeshProUGUI rightTutorialText;
    [SerializeField] private Button rightTutorialButton;

    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private GameObject ednaPanel;
    [SerializeField] private GameObject poster;
    [SerializeField] private GameObject posterOverlay;
    [SerializeField] private GameObject computer;
    [SerializeField] private GameObject highlightedUI;
    [SerializeField] private GameObject tutorialBlocker;
    [SerializeField] private Button redoTutorialButton;

    bool nextButtonPressed = false;
    bool typing = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        leftTutorialButton.onClick.AddListener(() => { if (!typing) nextButtonPressed = true; });
        rightTutorialButton.onClick.AddListener(() => { if (!typing) nextButtonPressed = true; });
        redoTutorialButton.onClick.AddListener(() => { StartCoroutine(GameTutorial()); });
        StartCoroutine(GameTutorial());
    }

    public IEnumerator GameTutorial()
    {
        tutorialUI.SetActive(true);
        leftTutorialPanel.SetActive(false);
        rightTutorialPanel.SetActive(true);
        redoTutorialButton.gameObject.SetActive(false);
        tutorialBlocker.SetActive(true);

        typing = true;
        yield return TextTyper.TypeText(rightTutorialText, "We now need to identify which species our eDNA samples are from. First, let's take a look at the information we received from a sample.");
        typing = false;

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;

        yield return new WaitForSeconds(0.2f);

        (Transform, int) ednaHighlight = HighlightElement(ednaPanel, highlightedUI.transform);

        typing = true;
        yield return TextTyper.TypeText(rightTutorialText, "Each sample has a symbol combination. This may not always be complete, but it will always give us enough information to determine the species present!");
        typing = false;

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;

        yield return new WaitForSeconds(0.2f);

        (Transform, int) posterHighlight = HighlightElement(poster, highlightedUI.transform);
        (Transform, int) posterOverlayHighlight = HighlightElement(posterOverlay, highlightedUI.transform);

        typing = true;
        yield return TextTyper.TypeText(rightTutorialText, "Following the symbol sequence on this poster here will help you identify the species!");
        typing = false;

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;
        yield return new WaitForSeconds(0.2f);

        typing = true;
        yield return TextTyper.TypeText(rightTutorialText, "Click on the poster to take a closer look, then click again anywhere to put it back.");
        typing = false;

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;
        yield return new WaitForSeconds(0.2f);


        RestoreElement(posterOverlay, posterOverlayHighlight.Item1, posterOverlayHighlight.Item2);
        RestoreElement(poster, posterHighlight.Item1, posterHighlight.Item2);
        RestoreElement(ednaPanel, ednaHighlight.Item1, ednaHighlight.Item2);

        rightTutorialPanel.SetActive(false);
        leftTutorialPanel.SetActive(true);

        (Transform, int) computerHighlight = HighlightElement(computer, highlightedUI.transform);

        typing = true;
        yield return TextTyper.TypeText(leftTutorialText, "When you figure out the species, find and click on it's silhouette on the computer.\nIf you get it right, you'll move on to the next sample");
        typing = false;

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;
        yield return new WaitForSeconds(0.2f);

        RestoreElement(computer, computerHighlight.Item1, computerHighlight.Item2);
        
        typing = true;
        yield return TextTyper.TypeText(leftTutorialText, "That's it! Now you can begin identifying species. I'll enable the computer for you now, good luck!");
        typing = false;

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;
        yield return new WaitForSeconds(0.2f);

        tutorialBlocker.SetActive(false);
        tutorialUI.SetActive(false);
        redoTutorialButton.gameObject.SetActive(true);
    }

    (Transform, int) HighlightElement(GameObject uiElement, Transform highlightContainer) 
{
    Transform originalParent = uiElement.transform.parent;
    int originalSiblingIndex = uiElement.transform.GetSiblingIndex();

    uiElement.transform.SetParent(highlightContainer, worldPositionStays: true);

    return (originalParent, originalSiblingIndex);
}

void RestoreElement(GameObject uiElement, Transform originalParent, int originalSiblingIndex) 
{
    uiElement.transform.SetParent(originalParent, worldPositionStays: true);
    uiElement.transform.SetSiblingIndex(originalSiblingIndex);
}

}
