using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Deeplinks
{
    public static class AndroidDeeplinkPluginHelper
    {
        [MenuItem("Tools/Deeplinks/Android/Build Android Plugin", priority = Constants.BaseMenuPriority + 1)]
        private static void BuildAndroidPluginFromScript()
        {
            if (!CheckRequesities())
            {
                Debug.LogError("Cannot build the Android Deeplink Jar because some prerequisites are missing, check the logs.");
                return;
            }

            CreateBuildConfigFile();

            var parentPath = Directory.GetParent(GetContainingDirectoryPath().Replace("/", @"\"));
            if (parentPath == null)
            {
                Debug.LogError("Path error.");
                return;
            }

            const string buildScriptName = "build_jar.ps1";
            var buildScriptPath = PathEx.Combine(parentPath.FullName, buildScriptName).AsNativePath();
            var configFilePath = GetConfigFilePath();
            Debug.Assert(File.Exists(buildScriptPath), "File.Exists(scriptPath)");
            Debug.Assert(File.Exists(configFilePath), "File.Exists(configFilePath)");

            int timeoutMilliseconds = 200 * 1000;
            using (var outputWaitHandle = new AutoResetEvent(false))
            using (var errorWaitHandle = new AutoResetEvent(false))
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = string.Format(@"{0} '{1}'", buildScriptPath, configFilePath),
                        WorkingDirectory = parentPath.FullName,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (e.Data == null)
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            Debug.Log(buildScriptName + ": " + e.Data);
                        }
                    };
                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (e.Data == null)
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            Debug.LogError(buildScriptName + " (error): " + e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(timeoutMilliseconds) && process.WaitForExit(0) 
                                                                 && outputWaitHandle.WaitOne(timeoutMilliseconds)
                                                                 && errorWaitHandle.WaitOne(timeoutMilliseconds))
                    {
                        Debug.Log(buildScriptName + " " + (process.ExitCode == 0 ? " complete!" : " failed!"));
                    }
                    else
                    {
                        Debug.Log(buildScriptName + " timed out...?");
                    }
                }
            }
        }

        [InitializeOnLoadMethod]
        [MenuItem("Tools/Deeplinks/Android/Refresh Build Config", priority = Constants.BaseMenuPriority + 1)]
        private static void RefreshBuildConfig()
        {
            if (!CheckRequesities())
            {
                Debug.LogError("Cannot refresh/regenerate the build config file because some prerequisites are missing, check the logs.");
                return;
            }

            CreateBuildConfigFile();
        }

        private static void CreateBuildConfigFile()
        {
            const string configFileName = "deeplink-android-build-config.json";
            var configPath = GetConfigFilePath();
            var json = EditorJsonUtility.ToJson(ConstructJarConfig(), true);
            Debug.Log(configFileName + " : " + json);
            Debug.LogWarning("Saving config @ " + configPath);
            File.WriteAllText(configPath, json);
        }

        private static string GetConfigFilePath()
        {
            const string configFileName = "deeplink-android-build-config.json";
            return PathEx.Combine(GetContainingDirectoryPath(), "Resources", configFileName);
        }

        private static bool CheckRequesities()
        {
            if (string.IsNullOrEmpty(EditorPrefs.GetString("JdkPath")))
            {
                Debug.LogError("JDK Path missing! Set it in 'Edit/Preferences... -> External Tools -> Android -> JDK'");
                return false;
            }

            if (string.IsNullOrEmpty(EditorPrefs.GetString("AndroidSdkRoot")))
            {
                Debug.LogError("Android SDK path missing! Set it in 'Edit/Preferences...-> External Tools -> Android -> SDK'");
                return false;
            }

            if (!HasAndroidSdkVersion((int) PlayerSettings.Android.targetSdkVersion))
            {
                Debug.LogWarning("It appears you do not have the selected minimum version of the android sdk, the");
            }

            if (!HasAndroidSdkVersion((int) PlayerSettings.Android.minSdkVersion))
            {
                Debug.LogWarning("It appears you do not have the selected minimum version of the android sdk");
            }

            // Do not create helper-file if using default application identifier
            if (GetAndroidPackageName().StartsWith("com.company", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("Default Android Package-Name in use: '" + GetAndroidPackageName() + "', it should be set to a non-default value");
                return false;
            }

            return true;
        }

        private static JarBuildConfig ConstructJarConfig()
        {
            return new JarBuildConfig
            {
                UnityProjectRootPath = Directory.GetParent(Application.dataPath).FullName,
                UnityEditorDataPath = EditorApplication.applicationContentsPath.AsNativePath(),
                JdkPath = EditorPrefs.GetString("JdkPath").AsNativePath(),
                AndroidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot").AsNativePath(),
                AndroidPackageName = GetAndroidPackageName(),
                AndroidMinSdkVersion = (int)PlayerSettings.Android.minSdkVersion,
                AndroidTargetSdkVersion = (int)PlayerSettings.Android.targetSdkVersion
            };
        }

        private static bool HasAndroidSdkVersion(int versionNumber)
        {
            var androidSdkPath = EditorPrefs.GetString("AndroidSdkRoot").AsNativePath();

            if (string.IsNullOrEmpty(androidSdkPath))
            {
                Debug.LogError("Android SDK path missing! Set it in 'Edit/Preferences...-> External Tools -> Android -> SDK'");
                return false;
            }

            // 'auto' in the android build settings corresponds to zero
            if (versionNumber == 0)
            {
                if (Directory.GetDirectories(PathEx.Combine(androidSdkPath, "platforms")).Any(s => s.Contains("android-")))
                {
                    return true;
                }

                Debug.LogWarning("No android platform sdk detected, please install some.");
                return false;

            }

            return Directory.Exists(PathEx.Combine(androidSdkPath, "platforms", "android-" + versionNumber));

        }

        private static string GetAndroidPackageName()
        {
            return PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        }

        private static string GetContainingDirectoryPath()
        {
            return Directory.GetParent(PathEx.GetScriptPath(typeof(AndroidDeeplinkPluginHelper))).FullName;
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

        public static string GetScriptPath(Type scriptType)
        {
            var path = Directory.GetFiles(Application.dataPath, scriptType.Name + ".cs", SearchOption.AllDirectories).FirstOrDefault();
            return path;
        }
    }
}