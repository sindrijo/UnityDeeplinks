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

            var t = CreateInstance<DeeplinkSettings>();
            t.hideFlags = HideFlags.HideAndDontSave;
            return s_instance;
        }
    }


    public static void Save()
    {
        InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { s_instance }, FilePath, true);
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
            DeeplinkSettings.UrlScheme = newSchemeValue.Trim();
            DeeplinkSettings.Save();
            Debug.Log("Deeplinks.UrlScheme Set: " + DeeplinkSettings.UrlScheme);
        }
    }

    private static string Sanitized(string stringToSanitize)
    {
        var sanitized = stringToSanitize.Where(ValidateChar).ToArray();
        var offset = char.IsLetter(sanitized[0]) ? 0 : 1;
        return new string(sanitized, offset, sanitized.Length - offset);
    }

    private static readonly char[] s_ExtraLegalChars = { '-', '.', '_' };

    private static bool ValidateChar(char c)
    {
        return s_ExtraLegalChars.Any(legalChar => legalChar == c) || char.IsLetterOrDigit(c);
    }
}
