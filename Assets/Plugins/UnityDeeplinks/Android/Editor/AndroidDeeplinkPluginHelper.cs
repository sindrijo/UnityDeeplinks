using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Deeplinks
{
    internal static partial class DeeplinkPluginBuildHelpers
    {
        private static class AndroidDeeplinkPluginHelper
        {
            private sealed class BuildHook : IPreprocessBuild
            {
                int IOrderedCallback.callbackOrder
                {
                    get { return -2; }
                }

                void IPreprocessBuild.OnPreprocessBuild(BuildTarget target, string path)
                {
                    BuildAndroidPluginFromScript();
                }
            }

            [MenuItem("Tools/Deeplinks/Android/Build Android Plugin", priority = Constants.BaseMenuPriority + 1)]
            private static void BuildAndroidPluginFromScript()
            {
                if (!CheckRequesities())
                {
                    Debug.LogError("Cannot build the Android Deeplink Jar because some prerequisites are missing, check the logs.");
                    return;
                }

                CreateBuildConfigFile();

                var parentPath = Directory.GetParent(GetContainingDirectoryPath().AsNativePath());
                if (parentPath == null)
                {
                    Debug.LogError("Path error.");
                    return;
                }

                var jarDestPath = PathUtil.Combine(parentPath.FullName, "UnityDeeplinks.jar");
                if (File.Exists(jarDestPath))
                {
                    PathUtil.EnsureFileIsWriteable(jarDestPath);
                }

                const string buildScriptName = "build_jar.ps1";
                var buildScriptPath = PathUtil.Combine(parentPath.FullName, buildScriptName).AsNativePath();
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

                            // Make sure Unity imports the newly created jar if it did not exist
                            AssetDatabase.Refresh();
                        }
                        else
                        {
                            Debug.Log(buildScriptName + " timed out...?");
                        }
                    }
                }
            }

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
                var configFilePath = GetConfigFilePath();
                var json = EditorJsonUtility.ToJson(ConstructJarConfig(), true);
                Debug.Log("Saving config @ " + configFilePath);
                Debug.Log("config values :\n" + json);
                File.WriteAllText(configFilePath, json);
            }

            private static string GetConfigFilePath()
            {
                // Using folder name 'tmp' which is commonly ignored by source control
                const string tempFolderName = "tmp";

                var tempDirectory = new DirectoryInfo(PathUtil.Combine(PathUtil.ProjectRootPath, tempFolderName).AsNativePath());
                if (!tempDirectory.Exists)
                {
                    tempDirectory.Create();
                    tempDirectory.Refresh();
                }

                // (re)add hidden attribute if is not present
                if ((tempDirectory.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    // Hidden folders are ignored by unity
                    tempDirectory.Attributes |= FileAttributes.Hidden;
                }

                return PathUtil.Combine(tempDirectory.FullName, "deeplink-android-build-config.json");
            }

            private static bool CheckRequesities()
            {
                if (string.IsNullOrEmpty(PathUtil.JdkPath))
                {
                    Debug.LogError("JDK Path missing! Set it in 'Edit/Preferences... -> External Tools -> Android -> JDK'");
                    return false;
                }

                if (string.IsNullOrEmpty(PathUtil.AndroidSdkPath))
                {
                    Debug.LogError("Android SDK path missing! Set it in 'Edit/Preferences...-> External Tools -> Android -> SDK'");
                    return false;
                }

                if (!HasAndroidSdkVersion((int)PlayerSettings.Android.targetSdkVersion))
                {
                    Debug.LogError("It appears you do not have the selected target version of the android sdk");
                    return false;
                }

                if (!HasAndroidSdkVersion((int)PlayerSettings.Android.minSdkVersion))
                {
                    Debug.LogError("It appears you do not have the selected minimum version of the android sdk");
                    return false;
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
                    JdkPath = PathUtil.JdkPath,
                    AndroidSdkRoot = PathUtil.AndroidSdkPath,
                    AndroidPackageName = GetAndroidPackageName(),
                    AndroidMinSdkVersion = (int)PlayerSettings.Android.minSdkVersion,
                    AndroidTargetSdkVersion = (int)PlayerSettings.Android.targetSdkVersion
                };
            }

            private static bool HasAndroidSdkVersion(int versionNumber)
            {
                var androidSdkPath = PathUtil.AndroidSdkPath;

                if (string.IsNullOrEmpty(androidSdkPath))
                {
                    Debug.LogError("Android SDK path missing! Set it in 'Edit/Preferences...-> External Tools -> Android -> SDK'");
                    return false;
                }

                // 'auto' in the android build settings corresponds to zero
                if (versionNumber == 0)
                {
                    if (Directory.GetDirectories(PathUtil.Combine(androidSdkPath, "platforms")).Any(s => s.Contains("android-")))
                    {
                        return true;
                    }

                    Debug.LogWarning("No android platform sdk detected, please install some.");
                    return false;

                }

                return Directory.Exists(PathUtil.Combine(androidSdkPath, "platforms", "android-" + versionNumber).AsNativePath());

            }

            private static string GetAndroidPackageName()
            {
                return PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            }

            private static string GetContainingDirectoryPath()
            {
                return Directory.GetParent(PathUtil.GetScriptPath(typeof(AndroidDeeplinkPluginHelper))).FullName;
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
    }

    public static class PathUtil
    {
        public static string Combine(string first, string second)
        {
            return Path.Combine(first, second);
        }

        public static string Combine(string first, string second, params string[] subsequentParts)
        {
            return Path.Combine(Path.Combine(first, second), subsequentParts.Aggregate(string.Empty, Path.Combine));
        }

        public static string AsNativePath(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

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

        public static string ProjectRootPath
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        public static string AndroidSdkPath
        {
            get { return EditorPrefs.GetString("AndroidSdkRoot").AsNativePath(); }
        }

        public static string JdkPath
        {
            get { return EditorPrefs.GetString("JdkPath").AsNativePath(); }
        }

        public static bool EnsureFileIsWriteable(string fileNamePath)
        {
            if (Directory.Exists(fileNamePath))
            {
                Debug.LogError("Path is a directory path: " + fileNamePath);
                return false;
            }

            var attributes = File.GetAttributes(fileNamePath);
            if ((attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
            {
                return true;
            }

            Debug.LogWarning("Making file non-readonly: " + fileNamePath);

            try
            {
                File.SetAttributes(fileNamePath, attributes & ~FileAttributes.ReadOnly);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
    }
}