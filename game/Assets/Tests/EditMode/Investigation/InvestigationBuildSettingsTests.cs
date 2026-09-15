using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace EDNA.Investigation.Tests
{
    public sealed class InvestigationBuildSettingsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RegisterInvestigation_PreservesSharedStartupAndDoesNotDuplicateScenes(bool alreadyPresent)
        {
            var original = EditorBuildSettings.scenes;
            const string shared = "Assets/Scenes/CTD-Minigame.unity";
            const string detective = "Assets/Scenes/InvestigationScene.unity";
            try
            {
                EditorBuildSettings.scenes = alreadyPresent
                    ? new[] { new EditorBuildSettingsScene(shared, true), new EditorBuildSettingsScene(detective, false) }
                    : new[] { new EditorBuildSettingsScene(shared, true) };
                // The builder belongs to Unity's default editor assembly.
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("EDNA.Investigation.Editor.InvestigationBuilder"))
                    .FirstOrDefault(t => t != null);
                Assert.That(type, Is.Not.Null);
                var register = type.GetMethod("AddSceneToBuildSettings");
                Assert.That(register, Is.Not.Null);
                register.Invoke(null, null); register.Invoke(null, null);
                Assert.That(EditorBuildSettings.scenes.Select(s => s.path), Is.EqualTo(new[] { shared, detective }));
                Assert.That(EditorBuildSettings.scenes.All(s => s.enabled), Is.True);
            }
            finally { EditorBuildSettings.scenes = original; }
        }
    }
}
