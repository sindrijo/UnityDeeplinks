using System.IO;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Deeplinks.Build
{
    internal static partial class DeeplinkPluginBuildHelpers
    {
        private class AndroidManifestHelper
    : IPreprocessBuild, IPostprocessBuild
        {

            private static readonly string s_androidManifestFullPath = PathUtil.Combine(Application.dataPath, "Plugins", "Android", "AndroidManifest.xml").AsNativePath();
            private static readonly string s_androidManifestBackupFullPath = PathUtil.Combine(PathUtil.ProjectRootPath, "Temp", "AndroidManifest.xml.backup").AsNativePath();

            //[MenuItem("Tools/Deeplinks/Android/AndroidManifest/Backup")]
            private static void BackupAndroidManifest()
            {
                if (File.Exists(s_androidManifestFullPath))
                {
                    File.Copy(s_androidManifestFullPath, s_androidManifestBackupFullPath, true);
                }
                else
                {
                    Debug.LogError("No AndroidManifest to back up");
                }
            }

            //[MenuItem("Tools/Deeplinks/Android/AndroidManifest/Restore")]
            private static void RestoreAndroidManifest()
            {
                if (File.Exists(s_androidManifestFullPath))
                {
                    File.Copy(s_androidManifestBackupFullPath, s_androidManifestFullPath, true);
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

                var manifestText = File.ReadAllText(s_androidManifestFullPath);
                if (manifestText.Contains(deeplinkSchemeVariable))
                {
                    BackupAndroidManifest();
                    var modifiedText = manifestText.Replace(deeplinkSchemeVariable, deeplinkScheme);
                    if (EditorUserBuildSettings.androidBuildSystem != AndroidBuildSystem.Gradle)
                    {
                        modifiedText = modifiedText.Replace("${applicationId}", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
                    }

                    if (PathUtil.EnsureFileIsWriteable(s_androidManifestFullPath))
                    {
                        File.WriteAllText(s_androidManifestFullPath, modifiedText);
                    }
                    else
                    {
                        Debug.LogError("Cannot write to file : " + s_androidManifestFullPath);
                    }
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
    }
}