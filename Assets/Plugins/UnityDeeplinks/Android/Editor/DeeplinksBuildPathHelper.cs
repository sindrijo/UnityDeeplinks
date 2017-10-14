using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DeeplinksBuildPathHelper 
{
    [InitializeOnLoadMethod]
    private static void Init()
    {
        const string FileName = "deeplink-build-conf.json";
        string scriptName = typeof(DeeplinksBuildPathHelper).Name + ".cs";
        var scriptPath = Directory.GetFiles(Application.dataPath, scriptName, SearchOption.AllDirectories).First();
        var pathsJsonPath = Path.Combine(scriptPath.Replace('/', '\\').Replace(scriptName, ""), "Resources\\" + FileName);

        if (File.Exists(pathsJsonPath))
        {
            return;
        }

        if (PlayerSettings.applicationIdentifier.StartsWith("com.company", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("Didn't create JSON helper file for build script because PlayerSettings.applicationIdentifier is the default value");
            return;
        }

        var p = new Paths
        {
            UnityProjectRootPath = Directory.GetParent(Application.dataPath).FullName,
            UnityEditorDataPath = EditorApplication.applicationContentsPath.Replace('/', '\\'),
            AndroidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot").Replace('/', '\\'),
            JdkPath = EditorPrefs.GetString("JdkPath").Replace('/', '\\'),
            AndroidPackageName = new string(PlayerSettings.applicationIdentifier.Reverse().SkipWhile(c => c != '.').Skip(1).Reverse().ToArray())
        };

        var json = EditorJsonUtility.ToJson(p, true);
        Debug.Log(FileName + " : " + json);

        Debug.LogWarning("Writing to : " + pathsJsonPath);
        File.WriteAllText(pathsJsonPath, json);
    }

    [Serializable]
    private class Paths
    {
        public string UnityProjectRootPath;

        public string AndroidPackageName;

        public string UnityEditorDataPath;

        public string AndroidSdkRoot;

        public string JdkPath;
    }
}
