using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

public sealed class DeeplinkSettings : ScriptableObject
{
    private static DeeplinkSettings s_instance;

    private static DeeplinkSettings Instance
    {
        get
        {
            if (s_instance != null)
            {
                return s_instance;
            }

            var objects = InternalEditorUtility.LoadSerializedFileAndForget(FilePath);
            if (objects != null && objects.Length == 1)
            {
                s_instance = objects[0] as DeeplinkSettings;
                return s_instance;
            }

            s_instance = CreateInstance<DeeplinkSettings>();
            s_instance.hideFlags = HideFlags.HideAndDontSave;
            Save();
            return s_instance;
        }
    }


    public static void Save()
    {
        InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { s_instance }, FilePath, true);
    }

    public static string UrlScheme
    {
        get { return Instance.urlScheme; }
        set
        {
            if (Instance.urlScheme != value)
            {
                Undo.RecordObject(Instance, "Change Url-Scheme");
                Instance.urlScheme = value;
            }
        }
    }

    private static string FilePath
    {
        get
        {
            if (string.IsNullOrEmpty(s_filePath))
            {
                s_filePath = Application.dataPath.Replace("/Assets", "") + "/ProjectSettings/" + "DeeplinkSettings.asset";
            }

            return s_filePath;
        }
    }

    private static string s_filePath;

    [SerializeField] private string urlScheme;
}


public class DeeplinkSettingsWindow : EditorWindow
{
    
    [MenuItem("Deeplinks/Settings")]
    private static void _Show()
    {
        var w = GetWindow<DeeplinkSettingsWindow>();
        w.titleContent = new GUIContent("Deeplink");
        w.ShowTab();
    }

    private void OnGUI()
    {
        var newSchemeValue = Sanitized(EditorGUILayout.DelayedTextField("Url Scheme", DeeplinkSettings.UrlScheme));
        if (!string.Equals(newSchemeValue, DeeplinkSettings.UrlScheme))
        {
            var old = DeeplinkSettings.UrlScheme;
            DeeplinkSettings.UrlScheme = newSchemeValue.Trim();
            DeeplinkSettings.Save();
            UpdateIOSUrlScheme(old, DeeplinkSettings.UrlScheme);
            Debug.Log("Deeplinks.UrlScheme Set: " + DeeplinkSettings.UrlScheme);
        }
    }

    private static string Sanitized(string stringToSanitize)
    {
        if(string.IsNullOrEmpty(stringToSanitize))
        {
            return stringToSanitize;
        }

        var sanitized = stringToSanitize.Where(ValidateChar).ToArray();
        var offset = char.IsLetter(sanitized[0]) ? 0 : 1;
        return new string(sanitized, offset, sanitized.Length - offset);
    }

    private static readonly char[] s_ExtraLegalChars = { '-', '.', '_' };

    private static bool ValidateChar(char c)
    {
        return s_ExtraLegalChars.Any(legalChar => legalChar == c) || char.IsLetterOrDigit(c);
    }

    private static void UpdateIOSUrlScheme(string oldScheme, string newScheme){

        string desiredUrlScheme = newScheme;

        var getPlayerSettingsObject = typeof(PlayerSettings).GetMethod("GetSerializedObject", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var settingsObject = getPlayerSettingsObject.Invoke(null, null) as SerializedObject;

        SerializedProperty iosUrlSchemes = settingsObject.FindProperty("iOSURLSchemes");

        int targetArrayIndex = 0;
        bool shouldInsertNewElement = true;

        for (int i = 0; i < iosUrlSchemes.arraySize; i++)
        {
            var arrayElement = iosUrlSchemes.GetArrayElementAtIndex(i);
            if (arrayElement.stringValue == desiredUrlScheme)
            {
                Debug.Log("Desired url scheme already set at index " + i);
                return;
            }

            if (arrayElement.stringValue == oldScheme)
            {
                Debug.Log("Updating old scheme at index " + i);
                targetArrayIndex = i;
                shouldInsertNewElement = false;
                break;
            }
        }

        if (shouldInsertNewElement)
        {
            targetArrayIndex = iosUrlSchemes.arraySize;
            iosUrlSchemes.InsertArrayElementAtIndex(iosUrlSchemes.arraySize);
        }

        SerializedProperty targetUrlSchemeArrayElement = iosUrlSchemes.GetArrayElementAtIndex(targetArrayIndex);
        targetUrlSchemeArrayElement.stringValue = desiredUrlScheme;
        targetUrlSchemeArrayElement.serializedObject.ApplyModifiedProperties();
        Debug.Log("UrlScheme set to " + targetUrlSchemeArrayElement.stringValue);
    }

}
