using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TextCore.LowLevel;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationTextMeshProTests
    {
        private static void AssertSdfText()
        {
            Assert.That(CurrentView.GetComponentsInChildren<UnityEngine.UI.Text>(true), Is.Empty);
            var labels = CurrentView.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.That(labels.Length, Is.GreaterThan(10));
            foreach (var label in labels)
            {
                Assert.That(label.font, Is.Not.Null, label.name);
                Assert.That(label.font.atlasRenderMode, Is.EqualTo(GlyphRenderMode.SDFAA), label.name);
                Assert.That(label.enableAutoSizing, Is.False, label.name);
            }
        }

        [UnityTest]
        public IEnumerator EveryStageUsesSdfTextIncludingGuidanceAndConclusion()
        {
            yield return LoadCurrent(); AssertSdfText();
            InvestigationWorkbenchTestActions.BeginObserveQuestions(); yield return null; AssertSdfText();
            CurrentPress("Toggle Comparison View");
            GameObject.Find("Survey Time Lens").GetComponent<UnityEngine.UI.Slider>().value = 0f;
            CurrentPress("Species Marker krill"); AssertSdfText(); CurrentPress("Close Species Facts");
            CurrentPress("Toggle Comparison View");
            EnterCurrentModels(); yield return null; AssertSdfText();
            CurrentPress("Run Scenario longline"); CurrentPress("Finish Scenario Animation");
            yield return null; yield return null; AssertSdfText();
            SkipCurrentGuide();
            CurrentPress("Detail Cause longline"); AssertSdfText(); CurrentPress("Close Scenario Details");
            OpenCurrentSummary(); AssertSdfText();
            CurrentPress("Complete Scenario Investigation"); AssertSdfText();
        }

        [UnityTest]
        public IEnumerator TextKeepsItsSdfFontAndGeometryWhenScaled()
        {
            yield return LoadCurrent();
            var label = GameObject.Find("Brand").GetComponent<TextMeshProUGUI>();
            var font = label.font; var material = label.fontSharedMaterial;
            Vector3 originalScale = label.rectTransform.localScale;
            try
            {
                foreach (float zoom in new[] { .5f, 1f, 2f, 3f })
                {
                    label.rectTransform.localScale = originalScale * zoom;
                    label.ForceMeshUpdate();
                    Assert.That(label.font, Is.SameAs(font));
                    Assert.That(label.fontSharedMaterial, Is.SameAs(material));
                    Assert.That(label.textInfo.meshInfo[0].vertexCount, Is.GreaterThan(0));
                    Assert.That(label.isTextTruncated, Is.False);
                    yield return null;
                }
            }
            finally { label.rectTransform.localScale = originalScale; }
        }
    }
}
