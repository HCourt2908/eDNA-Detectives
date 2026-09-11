using System;
using EDNA.Investigation.Domain;
using UnityEditor;
using UnityEngine;

namespace EDNA.Investigation.Editor
{
    public static partial class InvestigationBuilder
    {
        private const string SpeciesFactsPath = "Assets/Data/Investigation/SpeciesFacts.json";
        [Serializable] private sealed class SpeciesFactsFile { public SpeciesFact[] species; }
        [Serializable] private sealed class SpeciesFact { public string speciesId; public string description; }

        private static string ReviewedSpeciesDescription(string speciesId, string fallback)
        {
            var source = AssetDatabase.LoadAssetAtPath<TextAsset>(SpeciesFactsPath);
            if (source == null) throw new InvalidOperationException("Missing reviewed species facts: " + SpeciesFactsPath);
            var facts = JsonUtility.FromJson<SpeciesFactsFile>(source.text);
            foreach (var entry in facts.species)
                if (entry.speciesId == speciesId) return entry.description;
            return fallback;
        }

        [MenuItem("eDNA Detectives/Update Species Facts")]
        public static void UpdateSpeciesFacts()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InvestigationSpeciesDefinition", new[] { DataRoot }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<InvestigationSpeciesDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var data = new SerializedObject(asset);
                data.FindProperty("description").stringValue = ReviewedSpeciesDescription(asset.SpeciesId, asset.Description);
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
