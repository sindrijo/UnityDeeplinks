using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Deeplinks
{
    public static class AndroidDeeplinkPluginHelper
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            CreateHelperFile();
        }

        private static string GetScriptName()
        {
            return typeof(AndroidDeeplinkPluginHelper).Name + ".cs";
        }

        private static string GetContainingDirectoryPath()
        {
            var scriptPath = Directory.GetFiles(Application.dataPath, GetScriptName(), SearchOption.AllDirectories).First();
            return scriptPath.Replace(GetScriptName(), "");
        }

        [MenuItem("Tools/Deeplinks/Android/Build Android Plugin", priority = Constants.BaseMenuPriority + 1)]
        private static void BuildAndroidPluginFromScript()
        {
            CreateHelperFile();

            var parentPath = Directory.GetParent(GetContainingDirectoryPath().Replace("/", @"\")).Parent;
            if (parentPath == null)
            {
                Debug.LogError("Path error.");
                return;
            }

            var rawJson = EditorJsonUtility.ToJson(ConstructJarConfig(), false);
            var escapedJson = rawJson.Replace(@"\""", @"\\\""") // escape [\"] at end of string with [\\\"]
                .Replace(@"""", @"\"""); // escape ["] in json with [\"]

            var quoteWrappedEscapedJson = string.Format(@"""{0}""", escapedJson);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = string.Format(@"& '{0}' '{1}'", parentPath.FullName + "\\" + "build_jar.ps1",
                    quoteWrappedEscapedJson),
                WorkingDirectory = parentPath.FullName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = new Process {StartInfo = psi})
            {
                process.Start();
                string standardOutput;
                while ((standardOutput = process.StandardOutput.ReadLine()) != null)
                {
                    Debug.Log(standardOutput);
                }

                process.WaitForExit();
            }
        }

        [MenuItem("Tools/Deeplinks/Android/Refresh Build Config", priority = Constants.BaseMenuPriority + 1)]
        private static void CreateHelperFile()
        {
            const string FileName = "deeplink-build-conf.json";
            var pathsJsonPath = PathEx.Combine(GetContainingDirectoryPath(), "Resources", FileName);

            if (PlayerSettings.applicationIdentifier.StartsWith("com.company", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(typeof(AndroidDeeplinkPluginHelper).Name + ": Didn't create JSON helper file for build script because PlayerSettings.applicationIdentifier is the default value");
                return;
            }

            var json = EditorJsonUtility.ToJson(ConstructJarConfig(), true);
            Debug.Log(FileName + " : " + json);
            Debug.LogWarning("Writing to : " + pathsJsonPath);
            File.WriteAllText(pathsJsonPath, json);
        }

        private static JarBuildConfig ConstructJarConfig()
        {
            var p = new JarBuildConfig
            {
                UnityProjectRootPath = Directory.GetParent(Application.dataPath).FullName,
                UnityEditorDataPath = EditorApplication.applicationContentsPath.AsNativePath(),
                JdkPath = EditorPrefs.GetString("JdkPath").AsNativePath(),
                AndroidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot").AsNativePath(),
                AndroidPackageName = PlayerSettings.applicationIdentifier,
                AndroidMinSdkVersion = (int) PlayerSettings.Android.minSdkVersion,
                AndroidTargetSdkVersion = (int) PlayerSettings.Android.targetSdkVersion
            };

            return p;
        }

        [MenuItem("Tools/Deeplinks/Android/Test/Check Android Sdk Version - Minimum presence", priority = Constants.BaseMenuPriority + 1)]
        private static void TestHasAndroidMinSdk()
        {
            HasAndroidSdkVersion((int) PlayerSettings.Android.minSdkVersion);
        }

        [MenuItem("Tools/Deeplinks/Android/Test/Check Android Sdk Version - Target presence", priority = Constants.BaseMenuPriority + 1)]
        private static void TestHasAndroidTargetSdk()
        {
            HasAndroidSdkVersion((int) PlayerSettings.Android.targetSdkVersion);
        }

        private static bool HasAndroidSdkVersion(int versionNumber)
        {
            var androidSdkPath = EditorPrefs.GetString("AndroidSdkRoot").AsNativePath();
            var targetPlatformPath = PathEx.Combine(androidSdkPath, "platforms", "android-" + versionNumber);
            var exists = Directory.Exists(targetPlatformPath);
            Debug.Log("Android SDK ( " + versionNumber + " ) " + (exists ? "FOUND" : "NOT FOUND") + " @ " +
                      targetPlatformPath);
            return exists;

        }

        [Serializable]
        private class JarBuildConfig
        {
            public string UnityProjectRootPath;

            public string AndroidPackageName;

            public string UnityEditorDataPath;

            public string AndroidSdkRoot;

            public string JdkPath;

            public int AndroidMinSdkVersion;

            public int AndroidTargetSdkVersion;
        }
    }

    public static class PathEx
    {
        public static string Combine(string first, string second)
        {
            return Path.Combine(first, second);
        }

        public static string Combine(string first, string second, params string[] subsequentParts)
        {
            return Path.Combine(Path.Combine(first, second), Combine(subsequentParts));
        }

        public static string Combine(IEnumerable<string> pathParts)
        {
            return pathParts.Aggregate(string.Empty, Path.Combine);
        }

        public static string AsNativePath(this string path)
        {
            const char unsupportedPathSeparator
    #if UNITY_EDITOR_WIN
                = '/';
    #else
            = '\\';
    #endif

            if (path.IndexOf(unsupportedPathSeparator) > 0)
            {
                path = path.Replace(unsupportedPathSeparator, Path.DirectorySeparatorChar);
            }

            return path;
        }
    }
}