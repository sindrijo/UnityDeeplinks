using UnityEngine;
using UnityEngine.Events;

namespace Deeplinks.Internal
{
    public interface IInitDeeplinkReceiver
    {
        void SetDeeplinkHandler(UnityAction<string> handler);
    }

    [DisallowMultipleComponent]
    public class UnityDeeplinkReceiver : MonoBehaviour, IInitDeeplinkReceiver
    {
        public const string ErrorMessage ="This script should not be attached manually to a GameObject, because it is added automatically at runtime.";

        private const string C_gameObjectName = "[UnityDeeplinks]";

        private static bool s_isAutoInitSelf = false;

        private static UnityDeeplinkReceiver s_deeplinkReceiver;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitSelf()
        {
            Debug.Log("UnityDeeplinkReceiver.AutoInitSelf");

            s_isAutoInitSelf = true;
            DontDestroyOnLoad(new GameObject(C_gameObjectName, typeof(UnityDeeplinkReceiver))
                              {
                                  //hideFlags = HideFlags.HideInHierarchy
                              });
            s_isAutoInitSelf = false;
        }

#if UNITY_IOS
	    [DllImport("__Internal")]
	    private static extern void UnityDeeplinks_init(string gameObject = null, string deeplinkMethod = null);
#endif

        public static IInitDeeplinkReceiver Instance
        {
            get { return s_deeplinkReceiver; }
        }

        private UnityAction<string> deeplinkHandler;

        private void Awake()
        {
            if (!s_isAutoInitSelf)
            {
                Debug.LogError(ErrorMessage);
                Destroy(this);
                return;
            }

            if (s_deeplinkReceiver != null)
            {
                Debug.LogError("Duplicate instances of " + GetType().Name, this);
                gameObject.name += " (Duplicate)";
                return;
            }
            s_deeplinkReceiver = this;
        }

        private void Start()
        {

#if UNITY_IOS
		    if (Application.platform == RuntimePlatform.IPhonePlayer) 
            {
			    UnityDeeplinks_init(gameObject.name);
		    }
#endif
        }

        private void OnDestroy()
        {
            deeplinkHandler = null;

            if (ReferenceEquals(s_deeplinkReceiver, this))
            {
                s_deeplinkReceiver = null;
            }
        }

        private void OnDeeplink(string deeplink)
        {
            Debug.Log("UNITY: OnDeepLink-> " + deeplink);
            var handler = deeplinkHandler;
            if (handler != null)
            {
                handler.Invoke(deeplink);
            }
        }

        void IInitDeeplinkReceiver.SetDeeplinkHandler(UnityAction<string> handler)
        {
            Debug.Log("Set Deeplink Handler");

            if (deeplinkHandler == null)
            {
                deeplinkHandler = handler;
            }
        }
    }
}