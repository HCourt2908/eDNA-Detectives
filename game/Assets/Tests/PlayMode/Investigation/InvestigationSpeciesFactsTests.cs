using TMPro;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationSpeciesFactsTests
    {
        [UnityTest] public IEnumerator Facts_ShowScientificIdentityAndSeparateTheIllustrativeSurveyOnBothCanvasSizes()
        {
            yield return LoadCurrent();
            InvestigationWorkbenchTestActions.BeginObserveQuestions();
            CurrentPress("Toggle Comparison View");
            var canvas = CurrentView.GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            float previous = canvas.scaleFactor; bool enabled = scaler.enabled;
            try
            {
                scaler.enabled = false;
                foreach (float width in new[] { 1280f, 720f })
                {
                    canvas.scaleFactor = Screen.width / width; yield return null; yield return null;
                    GameObject.Find("Survey Time Lens").GetComponent<Slider>().value = 1f;
                    foreach (string id in new[] { "shark", "tuna", "atlantic_herring", "krill", "phytoplankton" })
                    {
                        CurrentPress("Historical Species Marker " + id); Canvas.ForceUpdateCanvases();
                        Assert.That(GameObject.Find("Tooltip Scientific Name").GetComponent<TextMeshProUGUI>().text, Is.Not.Empty);
                        string description = GameObject.Find("Tooltip Description").GetComponent<TextMeshProUGUI>().text;
                        Assert.That(description, Does.Not.Contain("Figma").And.Not.Contain("teaching assumption").And.Not.Contain("food-chain branch"));
                        Assert.That(GameObject.Find("Tooltip Survey Heading").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("THIS EXAMPLE SURVEY"));
                        string details = GameObject.Find("Tooltip Details").GetComponent<TextMeshProUGUI>().text;
                        Assert.That(details, Does.Contain("Illustrative case record").And.Contain("Map layer:").And.Not.Contain("High confidence"));
                        var panel = GameObject.Find("Species Facts Tooltip").GetComponent<RectTransform>();
                        Assert.That(panel.rect.height, Is.LessThanOrEqualTo(canvas.GetComponent<RectTransform>().rect.height - 24f));
                        foreach (TextMeshProUGUI text in panel.GetComponentsInChildren<TextMeshProUGUI>())
                            Assert.That(text.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(text.preferredHeight), id + "/" + text.name);
                        CurrentPress("Close Species Facts");
                    }
                }
                Assert.That(CurrentState.DiscoveredObservationIds, Is.Empty);
            }
            finally { canvas.scaleFactor = previous; scaler.enabled = enabled; }
        }
    }
}
