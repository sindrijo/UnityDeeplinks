using Deeplinks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This is a simple example of subscribing to the Deeplink.Received event.
/// It logs the event to console and also forwards it to a serialized unity event.
/// </summary>
public class SimpleDeeplinkHandler : MonoBehaviour
{
    [SerializeField] private UnityStringEvent deepLinkReceived;

    [System.Serializable]
    private class UnityStringEvent : UnityEvent<string>
    {
    }

    private void OnEnable()
    {
        Deeplink.Received -= DeeplinkOnReceived;
        Deeplink.Received += DeeplinkOnReceived;
    }

    private void OnDisable()
    {
        Deeplink.Received -= DeeplinkOnReceived;
    }

    private void DeeplinkOnReceived(string deeplink)
    {
        Debug.Log("Received Deeplink: " + deeplink);
        var unEscapedDeeplink = WWW.UnEscapeURL(deeplink);
        deepLinkReceived.Invoke(unEscapedDeeplink);
    }
}
