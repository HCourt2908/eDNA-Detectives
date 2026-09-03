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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        leftTutorialButton.onClick.AddListener(() => { nextButtonPressed = true; });
        rightTutorialButton.onClick.AddListener(() => { nextButtonPressed = true; });
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

        rightTutorialText.text = "Now that we've collected the eDNA samples, we need to identify the species present around the seamount. \nFirst, let's take a look at the information we received from a sample.";

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;

        yield return new WaitForSeconds(0.2f);

        (Transform, int) ednaHighlight = HighlightElement(ednaPanel, highlightedUI.transform);

        rightTutorialText.text = "Here is this particular sample's symbol combination.\nThis may not always be complete, but it will always give us enough information to determine the species present!";

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;

        yield return new WaitForSeconds(0.2f);

        (Transform, int) posterHighlight = HighlightElement(poster, highlightedUI.transform);
        (Transform, int) posterOverlayHighlight = HighlightElement(posterOverlay, highlightedUI.transform);

        rightTutorialText.text = "You'll notice that this sample's symbols also appear on this handy poster here. \nFollowing the symbol sequence will help you identify the species!";

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;
        yield return new WaitForSeconds(0.2f);

        rightTutorialText.text = "Click on the poster to take a closer look, then click again anywhere to put it back. \nTry to follow the sequence now and figure out our first species!";

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;
        yield return new WaitForSeconds(0.2f);

        RestoreElement(ednaPanel, ednaHighlight.Item1, ednaHighlight.Item2);
        RestoreElement(poster, posterHighlight.Item1, posterHighlight.Item2);
        RestoreElement(posterOverlay, posterOverlayHighlight.Item1, posterOverlayHighlight.Item2);

        rightTutorialPanel.SetActive(false);
        leftTutorialPanel.SetActive(true);

        (Transform, int) computerHighlight = HighlightElement(computer, highlightedUI.transform);

        leftTutorialText.text = "When you figure out the species, find it in the list on the computer and click that button to check your answer.\nIf you get it right, you'll move on to the next sample";

        yield return new WaitUntil(() => nextButtonPressed);
        nextButtonPressed = false;
        yield return new WaitForSeconds(0.2f);

        RestoreElement(computer, computerHighlight.Item1, computerHighlight.Item2);
        
        leftTutorialText.text = "That's it! Now you can begin identifying species.\nI'll enable the computer for you now. If you need a refresher, just hit the tutorial button and we'll go through this again.";

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
    // 3. Move back to original place
    uiElement.transform.SetParent(originalParent, worldPositionStays: true);
    uiElement.transform.SetSiblingIndex(originalSiblingIndex);
}

}
