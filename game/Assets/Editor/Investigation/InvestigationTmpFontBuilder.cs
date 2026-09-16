using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace EDNA.Investigation.Editor
{
    public static class InvestigationTmpFontBuilder
    {
        private const string Root = "Assets/Resources/Investigation/Fonts/";

        [MenuItem("eDNA Detectives/Generate Missing TMP Fonts")]
        public static void GenerateMissingFonts()
        {
            var characters = new HashSet<char>(Enumerable.Range(32, 95).Select(i => (char)i));
            foreach (string file in Directory.GetFiles("Assets/Scripts/Investigation", "*.cs", SearchOption.AllDirectories))
                foreach (char character in File.ReadAllText(file))
                    if (character >= 160) characters.Add(character);
            string glyphs = new string(characters.OrderBy(c => c).ToArray());
            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var body = Create("NunitoSans-Variable.ttf", "NunitoSans SDF", glyphs);
            var data = Create("FiraMono-Medium.ttf", "FiraMono SDF", glyphs);
            body.fallbackFontAssetTable = new List<TMP_FontAsset> { data, fallback };
            data.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(body); EditorUtility.SetDirty(data);
            foreach (var asset in new[] { body, data })
            {
                asset.HasCharacters(glyphs, out uint[] missing, true, true);
                Debug.Log("TMP_FONT_GLYPHS " + asset.name + " missing: " + string.Join(", ", (missing ?? System.Array.Empty<uint>()).Select(c => "U+" + c.ToString("X4"))));
            }
            AssetDatabase.SaveAssets();
        }

        private static TMP_FontAsset Create(string source, string name, string characters)
        {
            string path = Root + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + source);
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = name;
            asset.TryAddCharacters(characters, out string missing);
            AssetDatabase.CreateAsset(asset, path);
            asset.material.name = name + " Material";
            asset.material.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var atlas in asset.atlasTextures)
            {
                atlas.name = name + " Atlas";
                atlas.hideFlags = HideFlags.None;
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }
            var serialized = new SerializedObject(asset);
            var clearOnBuild = serialized.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuild != null) clearOnBuild.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
