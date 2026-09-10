using System;
using System.Collections.Generic;
using System.Reflection;
using EDNA.Investigation.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationSpeciesFactsSourceTests
    {
        [Serializable] private sealed class Facts { public Entry[] species; }
        [Serializable] private sealed class Entry { public string speciesId; public string scientificName; public string description; public string[] sources; }

        [Test] public void CatalogDescriptionsMatchTheSourcedFactsAndBuilderOverride()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Investigation/SpeciesFacts.json");
            var facts = JsonUtility.FromJson<Facts>(json.text);
            var definition = AssetDatabase.LoadAssetAtPath<InvestigationCaseDefinition>("Assets/Data/Investigation/LongLineCase/InvestigationCase_LongLine.asset");
            Assert.That(facts.species.Length, Is.EqualTo(definition.SpeciesCatalog.Count));
            var ids = new HashSet<string>();
            Type builder = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                builder = builder ?? assembly.GetType("EDNA.Investigation.Editor.InvestigationBuilder");
            var resolve = builder.GetMethod("ReviewedSpeciesDescription", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var entry in facts.species)
            {
                Assert.That(ids.Add(entry.speciesId), Is.True);
                var species = definition.FindCatalogSpecies(entry.speciesId);
                Assert.That(species, Is.Not.Null, entry.speciesId);
                Assert.That(species.ScientificName, Is.EqualTo(entry.scientificName));
                Assert.That(species.Description, Is.EqualTo(entry.description));
                Assert.That(entry.sources, Is.Not.Empty);
                foreach (string source in entry.sources) Assert.That(source, Does.StartWith("https://"));
                Assert.That(resolve.Invoke(null, new object[] { entry.speciesId, "obsolete model explanation" }), Is.EqualTo(entry.description),
                    "Regenerating the case must retain the reviewed description.");
            }
        }
    }
}
