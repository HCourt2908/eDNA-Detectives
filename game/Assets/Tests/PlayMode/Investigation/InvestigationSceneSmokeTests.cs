using System.Collections;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator InvestigationScene_BootstrapsCompleteEnglishWorkflow()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("InvestigationScene", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null, "InvestigationScene must be present in Build Settings.");
            yield return loadOperation;
            yield return null;

            InvestigationDemoBootstrap bootstrap = Object.FindAnyObjectByType<InvestigationDemoBootstrap>();
            InvestigationController controller = Object.FindAnyObjectByType<InvestigationController>();
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.State, Is.Not.Null);
            Assert.That(controller.State.AllResults.Count, Is.EqualTo(2));
            Assert.That(controller.State.RemainingSamples, Is.EqualTo(2));
            Assert.That(
                controller.State.UnlockedEvidence,
                Has.Some.Matches<EvidenceRecord>(evidence => evidence.EvidenceType == EvidenceType.NewDetection));
            Assert.That(
                controller.State.UnlockedEvidence,
                Has.Some.Matches<EvidenceRecord>(evidence => evidence.EvidenceType == EvidenceType.LowQualityResult));
            Assert.That(
                controller.State.UnlockedEvidence,
                Has.Some.Matches<EvidenceRecord>(evidence => evidence.EvidenceType == EvidenceType.ContaminationWarning));
            Assert.That(controller.State.IdentifiedEvidenceIds, Is.Empty);
            Assert.That(canvas, Is.Not.Null);

            InvestigationStatusBannerView statusBanner = Object.FindAnyObjectByType<InvestigationStatusBannerView>();
            Assert.That(statusBanner, Is.Not.Null);
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("NEXT STEP"));
            Assert.That(statusBanner.CurrentMessage, Does.Contain("Review the historical records"));
            Assert.That(statusBanner.CurrentTone, Is.EqualTo(InvestigationStatusTone.Guide));
            Assert.That(statusBanner.MessageText.fontSize, Is.GreaterThanOrEqualTo(18));

            Text[] labels = Object.FindObjectsByType<Text>();
            Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("CASE FILES")));
            Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("THE SHIFTING SEAMOUNT")));

            Button[] buttons = Object.FindObjectsByType<Button>();
            Assert.That(buttons.Length, Is.GreaterThanOrEqualTo(8));
            for (int index = 0; index < buttons.Length; index++)
            {
                Text buttonLabel = buttons[index].GetComponentInChildren<Text>();
                Assert.That(buttonLabel, Is.Not.Null, $"Button {buttons[index].name} must have a Text child.");
                Assert.That(
                    string.IsNullOrWhiteSpace(buttonLabel.text),
                    Is.False,
                    $"Button {buttons[index].name} must display its label.");
            }

            Text caseContent = FindText("THE SHIFTING SEAMOUNT");
            Assert.That(caseContent, Is.Not.Null);
            Canvas.ForceUpdateCanvases();
            Assert.That(caseContent.rectTransform.rect.height, Is.GreaterThan(1f), "Case content must have a visible layout height.");

            InvestigationButtonView previousSpecies = FindButtonView("<  PREVIOUS SPECIES");
            InvestigationButtonView nextSpecies = FindButtonView("NEXT SPECIES  >");
            InvestigationButtonView startComparison = FindButtonView("START COMPARISON");
            Assert.That(previousSpecies, Is.Not.Null);
            Assert.That(nextSpecies, Is.Not.Null);
            Assert.That(startComparison, Is.Not.Null);
            Assert.That(previousSpecies.CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Browse));
            Assert.That(nextSpecies.CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Browse));
            Assert.That(startComparison.CurrentStyle, Is.EqualTo(InvestigationButtonStyle.Primary));
            Assert.That(previousSpecies.BackgroundColor, Is.Not.EqualTo(startComparison.BackgroundColor));
            Assert.That(previousSpecies.LabelColor, Is.Not.EqualTo(startComparison.LabelColor));
            Assert.That(previousSpecies.transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(nextSpecies.transform.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(startComparison.transform.GetSiblingIndex(), Is.EqualTo(3), "Forward-stage actions belong in the rightmost column.");
            Assert.That(startComparison.transform.position.x, Is.GreaterThan(nextSpecies.transform.position.x + nextSpecies.GetComponent<RectTransform>().rect.width));

            FindButton("2  COMPARE DATA").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            SampleComparisonBoardView board = Object.FindAnyObjectByType<SampleComparisonBoardView>();
            SpeciesComparisonCardView[] cards = Object.FindObjectsByType<SpeciesComparisonCardView>();
            Assert.That(board, Is.Not.Null);
            Assert.That(cards.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(FindText("FISH\nSILHOUETTE"), Is.Not.Null);
            Assert.That(FindText("SELECT THIS CARD TO CLASSIFY"), Is.Not.Null);
            Assert.That(FindButton("EXPECTED BUT MISSING"), Is.Not.Null);
            Assert.That(FindButton("EXPECTED BUT MISSING").interactable, Is.False, "Classification choices stay disabled until a card is selected.");
            Assert.That(FindButton("MATCHES BASELINE"), Is.Not.Null);
            Assert.That(FindButtonView("BUILD HYPOTHESIS").transform.GetSiblingIndex() % 4, Is.EqualTo(3));
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("NEXT STEP"));
            Assert.That(statusBanner.CurrentMessage, Does.Contain("Select a comparison card"));

            ScrollRect contentScroll = Object.FindAnyObjectByType<ScrollRect>();
            Assert.That(contentScroll, Is.Not.Null);
            Assert.That(contentScroll.scrollSensitivity, Is.EqualTo(12f));

            SpeciesComparisonCardView coldFishCard = null;
            for (int index = 0; index < cards.Length; index++)
            {
                if (cards[index].name.Contains("Cold-water Fish A"))
                {
                    coldFishCard = cards[index];
                    break;
                }
            }

            Assert.That(coldFishCard, Is.Not.Null);
            Assert.That(coldFishCard.IsAwaitingSelection, Is.True);
            contentScroll.verticalNormalizedPosition = 0.35f;
            Canvas.ForceUpdateCanvases();
            float scrollBeforeSelection = contentScroll.verticalNormalizedPosition;
            coldFishCard.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(contentScroll.verticalNormalizedPosition, Is.EqualTo(scrollBeforeSelection).Within(0.02f), "Selecting a comparison card must not jump the board back to the top.");
            Assert.That(FindText("SELECTED - CHOOSE A CLASSIFICATION BELOW"), Is.Not.Null);
            Assert.That(FindCard("Cold-water Fish A").IsAwaitingSelection, Is.False);
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("NEXT STEP"));
            Assert.That(statusBanner.CurrentMessage, Does.Contain("Choose the classification"));

            FindButton("NEW ARRIVAL").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(controller.State.MisclassificationCount, Is.EqualTo(1));
            Assert.That(contentScroll.verticalNormalizedPosition, Is.EqualTo(scrollBeforeSelection).Within(0.02f), "A classification result must preserve the comparison position.");
            Button ruledOut = FindButton("RULED OUT: NEW ARRIVAL");
            Assert.That(ruledOut, Is.Not.Null);
            Assert.That(ruledOut.interactable, Is.False);
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("TRY AGAIN"));
            Assert.That(statusBanner.CurrentTone, Is.EqualTo(InvestigationStatusTone.Warning));

            FindButton("EXPECTED BUT MISSING").onClick.Invoke();
            yield return null;

            Assert.That(controller.State.IdentifiedEvidenceIds, Has.Count.EqualTo(1));
            Assert.That(FindText("IDENTIFIED: EXPECTED BUT MISSING"), Is.Not.Null);
            Assert.That(statusBanner.CurrentLabel, Is.EqualTo("FINDING IDENTIFIED"));
            Assert.That(statusBanner.CurrentTone, Is.EqualTo(InvestigationStatusTone.Success));

            FindButton("3  BUILD HYPOTHESIS").onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(FindButtonView("COMPARE DATA").transform.GetSiblingIndex(), Is.EqualTo(0), "Back-stage actions belong in the leftmost column.");

            FindButton("5  CONCLUSION").onClick.Invoke();
            yield return null;
            Assert.That(FindText("Misclassifications recorded: 1"), Is.Not.Null);
            Assert.That(FindButtonView("PLAN SAMPLE").transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(FindButtonView("SUBMIT CONCLUSION").transform.GetSiblingIndex() % 4, Is.EqualTo(3));

            LogAssert.NoUnexpectedReceived();
        }

        private static Button FindButton(string label)
        {
            Button[] buttons = Object.FindObjectsByType<Button>();
            for (int index = 0; index < buttons.Length; index++)
            {
                Text text = buttons[index].GetComponentInChildren<Text>();
                if (text != null && text.text == label)
                {
                    return buttons[index];
                }
            }

            return null;
        }

        private static Text FindText(string value)
        {
            Text[] labels = Object.FindObjectsByType<Text>();
            for (int index = 0; index < labels.Length; index++)
            {
                if (labels[index].text.Contains(value))
                {
                    return labels[index];
                }
            }

            return null;
        }

        private static InvestigationButtonView FindButtonView(string label)
        {
            InvestigationButtonView[] buttons = Object.FindObjectsByType<InvestigationButtonView>();
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].Label == label)
                {
                    return buttons[index];
                }
            }

            return null;
        }

        private static SpeciesComparisonCardView FindCard(string nameFragment)
        {
            SpeciesComparisonCardView[] cards = Object.FindObjectsByType<SpeciesComparisonCardView>();
            for (int index = 0; index < cards.Length; index++)
            {
                if (cards[index].name.Contains(nameFragment))
                {
                    return cards[index];
                }
            }

            return null;
        }
    }
}
