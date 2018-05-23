using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

public class AndroidManifestHelper 
    : IPreprocessBuild, IPostprocessBuild
{
    private static readonly string s_androidManifestPath = PathEx.Combine(Application.dataPath, "Plugins", "Android", "AndroidManifest.xml").AsNativePath();
    private static readonly string s_androidManifestBackupPath = Path.Combine(Path.Combine(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1), "Temp"), "AndroidManifest.xml.backup").AsNativePath();

    [MenuItem("Deeplinks/AndroidManifest/Backup")]
    public static void BackupAndroidManifest()
    {
        if (File.Exists(s_androidManifestPath))
        {
            File.Copy(s_androidManifestPath, s_androidManifestBackupPath, true);
        }
        else
        {
            Debug.LogError("No AndroidManifest to back up");
        }
    }

    [MenuItem("Deeplinks/AndroidManifest/Restore")]
    public static void RestoreAndroidManifest()
    {
        if (File.Exists(s_androidManifestBackupPath))
        {
            File.Copy(s_androidManifestBackupPath, s_androidManifestPath, true);
        }
        else
        {
            Debug.LogWarning("No AndroidManifest to restore");
        }
    }

    int IOrderedCallback.callbackOrder
    {
        get { return 0; }
    }

    void IPreprocessBuild.OnPreprocessBuild(BuildTarget target, string path)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            return;
        }
        EditorPrefs.SetBool("ShouldRestoreAndroidManifest", true);

        const string deeplinkSchemeVariable = "${deeplinkScheme}";
        string deeplinkScheme = DeeplinkSettings.UrlScheme;
        Debug.Log("Applying deeplink scheme to AndroidManifest: " + deeplinkScheme);

        var manifestText = File.ReadAllText(s_androidManifestPath);
        if (manifestText.Contains(deeplinkSchemeVariable))
        {
            BackupAndroidManifest();
            var modifiedText = manifestText.Replace(deeplinkSchemeVariable, deeplinkScheme);
            if (EditorUserBuildSettings.androidBuildSystem != AndroidBuildSystem.Gradle)
            {
                modifiedText = modifiedText.Replace("${applicationId}", PlayerSettings.applicationIdentifier);
            }
            File.WriteAllText(s_androidManifestPath, modifiedText);
        }
        else
        {
            Debug.LogError("Required placeholder variable not found in AndroidManifest: " + deeplinkSchemeVariable);
        }
    }

    void IPostprocessBuild.OnPostprocessBuild(BuildTarget target, string path)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            return;
        }
        EditorPrefs.SetBool("ShouldRestoreAndroidManifest", false);
        RestoreAndroidManifest();
    }

    [DidReloadScripts]
    private static void OnDidReloadScripts()
    {
        if (EditorPrefs.GetBool("ShouldRestoreAndroidManifest", false))
        {
            Debug.LogWarning("Something probably went wrong, so we are restoring the android manifest...");
            EditorPrefs.SetBool("ShouldRestoreAndroidManifest", false);
            RestoreAndroidManifest();
        }
    }
}