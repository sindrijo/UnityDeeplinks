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
                var oldScheme = Instance.urlScheme;
                Instance.urlScheme = value;
                UrlSchemeUpdated(oldScheme, Instance.urlScheme);
            }
        }
    }

    private static string FilePath
    {
        get
        {
            if (string.IsNullOrEmpty(s_filePath))
            {
                s_filePath = PathEx.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "ProjectSettings", "DeeplinkSettings.asset");
            }

            return s_filePath;
        }
    }

    private static string s_filePath;

    [SerializeField] private string urlScheme;

    private static void UrlSchemeUpdated(string oldScheme, string newScheme)
    {
        UpdateUrlSchemeIos(oldScheme, newScheme);
    }

    private static void UpdateUrlSchemeIos(string oldScheme, string newScheme)
    {
        if (string.Equals(oldScheme, newScheme))
        {
            return;
        }

        var findPropertyMethod = typeof(PlayerSettings).GetMethod("FindProperty", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Debug.Assert(findPropertyMethod != null, "findPropertyMethod != null");
        SerializedProperty iosUrlSchemesSerialized = (SerializedProperty)findPropertyMethod.Invoke(null, new object[] { "iOSURLSchemes" });

        // Make sure we are updating the current object
        iosUrlSchemesSerialized.serializedObject.Update();

        for (int i = 0; i < iosUrlSchemesSerialized.arraySize; i++)
        {
            var arrayElement = iosUrlSchemesSerialized.GetArrayElementAtIndex(i);
            if (arrayElement.stringValue == oldScheme)
            {
                //Debug.Log("Updating old scheme at index " + i);
                arrayElement.stringValue = newScheme;
                arrayElement.serializedObject.ApplyModifiedProperties();
                return;
            }

            if (arrayElement.stringValue == newScheme)
            {
                //Debug.LogWarning("Desired url scheme already set at index " + i);
                return;
            }
        }

        // Add new entry because entry with the old scheme was not found
        var newIndex = iosUrlSchemesSerialized.arraySize;
        iosUrlSchemesSerialized.InsertArrayElementAtIndex(newIndex);

        var newArrayElement = iosUrlSchemesSerialized.GetArrayElementAtIndex(newIndex);
        newArrayElement.stringValue = newScheme;

        // Flush changes
        newArrayElement.serializedObject.ApplyModifiedProperties();

        //Debug.Log("iOS.UrlScheme added " + newArrayElement.stringValue);
    }
}

public class DeeplinkSettingsWindow : EditorWindow
{
    
    [MenuItem("Tools/Deeplinks/Settings", priority = Constants.BaseMenuPriority)]
    private static void _Show()
    {
        var w = GetWindow<DeeplinkSettingsWindow>();
        w.titleContent = new GUIContent("Deeplink");
        w.ShowTab();
    }

    private void OnGUI()
    {
        var newSchemeValue = UriSchemeValidator.ValidatedSanitized(EditorGUILayout.DelayedTextField("Url Scheme", DeeplinkSettings.UrlScheme));
        if (!string.Equals(newSchemeValue, DeeplinkSettings.UrlScheme))
        {
            DeeplinkSettings.UrlScheme = newSchemeValue.Trim();
            DeeplinkSettings.Save();
            Debug.Log("Deeplinks.UrlScheme Set: " + DeeplinkSettings.UrlScheme);
        }
    }

    private static class UriSchemeValidator
    {
        /*
            https://tools.ietf.org/html/rfc3986#section-3.1

           Scheme names consist of a sequence of characters beginning with a
           letter and followed by any combination of letters, digits, plus
           ("+"), period ("."), or hyphen ("-").  Although schemes are case-
           insensitive, the canonical form is lowercase and documents that
           specify schemes must do so with lowercase letters. 

         */

        public static string ValidatedSanitized(string stringToSanitize)
        {
            if (string.IsNullOrEmpty(stringToSanitize))
            {
                return stringToSanitize;
            }

            return new string(stringToSanitize.SkipWhile(IsInvalidLeadingChar).Where(IsValidTailChar).ToArray());
        }

        private static readonly char[] ExtraLegalChars = { '+', '-', '.' };

        private static bool IsInvalidLeadingChar(char c)
        {
            return !char.IsLetter(c);
        }

        private static bool IsValidTailChar(char c)
        {
            return char.IsLetterOrDigit(c) || ExtraLegalChars.Any(legalChar => legalChar == c);
        }
    }
}

public static class Constants
{
    public const int BaseMenuPriority = 5000;
}
