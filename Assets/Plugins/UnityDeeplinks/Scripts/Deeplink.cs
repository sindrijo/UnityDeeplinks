using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

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
                Internal.DeeplinkReceived.AddListener(value);

                while (Internal.DeferredDeeplinks.Count > 0)
                {
                    Internal.DeeplinkReceived.Invoke(Internal.DeferredDeeplinks.Dequeue());
                }
            }

            remove
            {
                Internal.DeeplinkReceived.RemoveListener(value);
            }
        }

        private static class Internal
        {
            private static Queue<string> s_deferredDeeplinks;

            private static UnityDeeplinkEvent s_deeplinkReceived;

            public static Queue<string> DeferredDeeplinks
            {
                get { return s_deferredDeeplinks ?? (s_deferredDeeplinks = new Queue<string>()); }
            }

            public static UnityDeeplinkEvent DeeplinkReceived
            {
                get { return s_deeplinkReceived ?? (s_deeplinkReceived = new UnityDeeplinkEvent()); }
            }

            public static void OnDeeplinkReceived(string deepLink)
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
        }

        private class UnityDeeplinkEvent : UnityEvent<string>
        {
        }

        private static void OnDeeplinkRecieved(string deeplink)
        {
            Internal.OnDeeplinkReceived(deeplink);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Deeplink.AutoInit");
#endif
            UnityDeeplinkReceiver.Initialize(OnDeeplinkRecieved);
        }

        [DisallowMultipleComponent]
        private class UnityDeeplinkReceiver : MonoBehaviour
        {
            private static UnityDeeplinkReceiver s_self;

            public static void Initialize(UnityAction<string> deeplinkHandler)
            {
                if (s_self == null)
                {
                    var receiverObject = new GameObject("[UnityDeeplinks]", typeof(UnityDeeplinkReceiver))
                    {
                        hideFlags = HideFlags.HideInHierarchy
                    };
                    DontDestroyOnLoad(receiverObject);
                }

                Assert.IsNotNull(s_self, typeof(UnityDeeplinkReceiver).Name + " singleton value was not assigned in Awake()");
                s_self.deeplinkHandler = deeplinkHandler;
            }

            private UnityAction<string> deeplinkHandler;

            private void Awake()
            {
                if (s_self != null)
                {
                    Debug.LogError("Duplicate instances of " + GetType().Name, this);
                    gameObject.name += " (Duplicate)";
                    return;
                }
                s_self = this;
            }

#if UNITY_IOS
	        [DllImport("__Internal")]
	        private static extern void UnityDeeplinks_init(string gameObject = null, string deeplinkMethod = null);

            private void Start()
            {
		        if (Application.platform == RuntimePlatform.IPhonePlayer) 
                {
			        UnityDeeplinks_init(gameObject.name);
		        }
            }
#endif
            private void OnDestroy()
            {
                deeplinkHandler = null;

                if (ReferenceEquals(s_self, this))
                {
                    s_self = null;
                }
            }

            [UsedImplicitly]
            private void OnDeeplink(string deeplink)
            {
#if DEVELOPMENT_BUILD
                Debug.Log("UNITY: OnDeepLink() -> " + deeplink);
#endif
                var handler = deeplinkHandler;
                if (handler != null)
                {
                    handler.Invoke(deeplink);
                }
            }
        }
    }
}