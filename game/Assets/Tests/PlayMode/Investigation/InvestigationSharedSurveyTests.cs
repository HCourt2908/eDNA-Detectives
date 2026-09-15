using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static EDNA.Investigation.Tests.InvestigationCurrentFlowTestActions;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationSharedSurveyTests
    {
        // These two shared components live in Unity's default assembly. Resolve
        // their public state without introducing an assembly dependency cycle.
        [UnityTest]
        public IEnumerator IdentificationUsesTheDetectiveSurveyAndIncludesEveryDetection()
        {
            yield return LoadCurrent();
            Type managerType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("GameManager")).First(t => t != null);
            Type identificationType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("sampleController")).First(t => t != null);
            var instance = managerType.GetProperty("Instance").GetValue(null);
            GameObject created = null;
            if (instance == null)
            {
                created = new GameObject("Shared Survey Test");
                instance = created.AddComponent(managerType);
            }
            try
            {
                var species = (IList)managerType.GetField("speciesList").GetValue(instance);
                var frequencies = (IDictionary)managerType.GetField("frequencyMap").GetValue(instance);
                var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<InvestigationCaseDefinition>(
                    "Assets/Data/Investigation/LongLineCase/InvestigationCase_LongLine.asset");
                Assert.That(species.Count, Is.EqualTo(definition.Species.Count));
                foreach (object item in species)
                {
                    string name = (string)item.GetType().GetField("name").GetValue(item);
                    var entry = definition.Species.Single(s => s.MatchesIdentifier(name));
                    var finding = definition.Observations.Single(o => o.RelatedSpeciesId == entry.SpeciesId
                        && InvestigationObserveEvaluator.IsInitialFinding(o));
                    string expected = finding.ClaimType == ObservationClaimType.NotDetected ? "Missing"
                        : finding.ClaimType == ObservationClaimType.ReducedDetection ? "LessFrequent"
                        : finding.ClaimType == ObservationClaimType.ChangedDepthOrDistribution ? "MoreFrequent" : "SameFrequent";
                    Assert.That(frequencies[item].ToString(), Is.EqualTo(expected), name);
                }
                // Ordering must not decide which detected species gets omitted.
                var reversed = (IList)Activator.CreateInstance(species.GetType());
                foreach (object item in species.Cast<object>().Reverse()) reversed.Add(item);
                var build = identificationType.GetMethod("BuildSampleQueue", BindingFlags.NonPublic | BindingFlags.Static);
                foreach (IList order in new[] { species, reversed })
                {
                    var queue = (IList)build.Invoke(null, new object[] { order, frequencies });
                    foreach (object item in species)
                    {
                        string frequency = frequencies[item].ToString();
                        int expected = frequency == "Missing" ? 0 : frequency == "MoreFrequent" ? 2 : 1;
                        Assert.That(queue.Cast<object>().Count(s => ReferenceEquals(s, item)), Is.EqualTo(expected));
                    }
                }
            }
            finally { if (created != null) UnityEngine.Object.Destroy(created); }
            yield return null;
        }
    }
}
