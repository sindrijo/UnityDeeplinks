using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class DeeplinksBuildPathHelper 
{
    [InitializeOnLoadMethod]
    private static void Init()
    {
        CreateHelperFile();
    }

    private static string GetScriptName()
    {
        return typeof(DeeplinksBuildPathHelper).Name + ".cs";
    }

    private static string GetContainingDirectoryPath()
    {
        var scriptPath = Directory.GetFiles(Application.dataPath, GetScriptName(), SearchOption.AllDirectories).First();
        return scriptPath.Replace(GetScriptName(), "");
    }

    [MenuItem("Tools/Deeplinks/Build Android Plugin")]
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
                                 .Replace(@"""", @"\""");  // escape ["] in json with [\"]

        var quoteWrappedEscapedJson = string.Format(@"""{0}""", escapedJson);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = string.Format(@"& '{0}' '{1}'", parentPath.FullName + "\\" + "build_jar.ps1", quoteWrappedEscapedJson),
            WorkingDirectory = parentPath.FullName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using (var process = new Process { StartInfo = psi })
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



    private static void CreateHelperFile()
    {
        const string FileName = "deeplink-build-conf.json";
        var pathsJsonPath = Path.Combine(GetContainingDirectoryPath(), "Resources\\" + FileName);

        if (File.Exists(pathsJsonPath))
        {
            return;
        }

        if (PlayerSettings.applicationIdentifier.StartsWith("com.company", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("Didn't create JSON helper file for build script because PlayerSettings.applicationIdentifier is the default value");
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
            UnityEditorDataPath = EditorApplication.applicationContentsPath.Replace('/', '\\'),
            AndroidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot").Replace('/', '\\'),
            JdkPath = EditorPrefs.GetString("JdkPath").Replace('/', '\\'),
            AndroidPackageName = new string(PlayerSettings.applicationIdentifier.Reverse().SkipWhile(c => c != '.').Skip(1).Reverse().ToArray())
        };

        return p;
    }

    [Serializable]
    private class JarBuildConfig
    {
        public string UnityProjectRootPath;

        public string AndroidPackageName;

        public string UnityEditorDataPath;

        public string AndroidSdkRoot;

        public string JdkPath;
    }
}
