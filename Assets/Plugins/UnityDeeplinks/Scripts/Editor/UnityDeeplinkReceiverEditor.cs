using Deeplinks.Internal;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The main purpose of this inspector is to inform the user not to manually add 
/// this script to a GameObject while also automatically removing it.
/// </summary>
[CustomEditor(typeof(UnityDeeplinkReceiver))]
public class UnityDeeplinkReceiverEditor : Editor
{
    private const string C_doNotAddManuallyMessage = "This script should not be attached manually to a GameObject, because it is added automatically at runtime.";

    private bool didAddNotification;

    public override void OnInspectorGUI()
    {
        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(C_doNotAddManuallyMessage, MessageType.Warning);
            return;
        }

        if (!didAddNotification)
        {
            EditorWindow.focusedWindow.ShowNotification(new GUIContent(C_doNotAddManuallyMessage));
            didAddNotification = true;
            Debug.LogWarning("Removing " + typeof(UnityDeeplinkReceiver).Name + " from GameObject!");
            EditorApplication.delayCall += () => DestroyImmediate(target);
        }
    }
}
