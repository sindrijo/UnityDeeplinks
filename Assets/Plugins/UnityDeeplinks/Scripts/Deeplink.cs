using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Deeplinks
{
    public static class Deeplink
    {
        /// <summary>
        /// When the native Android or iOS application receives a deeplink it is forwarded and invoked through on this event.
        /// </summary>
        public static event UnityAction<string> Received
        {
            add
            {
                InitOrGetUnityDeeplinkEvent().AddListener(value);
                while (s_deferredDeeplinks.Count > 0)
                {
                    s_deeplinkReceived.Invoke(s_deferredDeeplinks.Dequeue());
                }
            }

            remove
            {
                InitOrGetUnityDeeplinkEvent().RemoveListener(value);
            }
        }

        private static Queue<string> s_deferredDeeplinks = new Queue<string>();

        private static UnityDeeplinkEvent s_deeplinkReceived;

        private static UnityDeeplinkEvent InitOrGetUnityDeeplinkEvent()
        {
            return s_deeplinkReceived ?? (s_deeplinkReceived = new UnityDeeplinkEvent());
        }

        private static void OnDeeplinkReceived(string deepLink)
        {
            if (s_deeplinkReceived == null)
            {
                s_deferredDeeplinks.Enqueue(deepLink);
            }
            else
            {
                s_deeplinkReceived.Invoke(deepLink);
            }
        }

        private class UnityDeeplinkEvent : UnityEvent<string>
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            Debug.Log("Deeplink.AutoInit");
            Internal.UnityDeeplinkReceiver.Instance.SetDeeplinkHandler(OnDeeplinkReceived);
        }
    }
}