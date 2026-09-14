using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds the retracting cable controller to the already-authored recovery panel
/// without rebuilding or repositioning the rest of the CTD scene.
/// </summary>
public static class RecoveryCableInstaller
{
    private const string ScenePath = "Assets/Scenes/CTD-Minigame.unity";

    [MenuItem("OceanX/Apply Recovery Cable Reel")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CTDGameManager manager = UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            throw new InvalidOperationException("The CTD scene is missing CTDGameManager.");
        }

        InstallIntoScene(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("RECOVERY_CABLE_REEL_OK: cable length now follows the recovery Rosette.");
    }

    public static void InstallIntoScene(CTDGameManager manager)
    {
        if (manager.recoveryPanel == null || manager.recoveryRosette == null)
        {
            throw new InvalidOperationException("The recovery panel or Rosette reference is missing.");
        }

        Transform cableTransform = manager.recoveryPanel.transform.Find("Cable");
        if (cableTransform == null)
        {
            throw new InvalidOperationException("The recovery panel is missing its Cable object.");
        }

        RecoveryCableController cable = cableTransform.GetComponent<RecoveryCableController>();
        if (cable == null)
        {
            cable = cableTransform.gameObject.AddComponent<RecoveryCableController>();
        }

        cable.cableRect = cableTransform.GetComponent<RectTransform>();
        cable.recoveredRosette = manager.recoveryRosette;
        cable.cableImage = cableTransform.GetComponent<Image>();
        cable.minimumLength = 18f;
        cable.rosetteOverlap = 2f;
        cable.fadeAtEnd = 0.92f;
        manager.recoveryCable = cable;
        EditorUtility.SetDirty(cable);
        EditorUtility.SetDirty(manager);
    }
}
