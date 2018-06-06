# UnityDeeplinks
A set of tools for Unity to allow handling deeplink activation from within Unity scripts, for Android and iOS, including iOS Universal Links.
### Known Issues/Limitations
* No support for handling multiple url-schemes (yet), a large majority of apps only need to support one anyways.
* Check out the repo's *issues* section
## Usage Example
 ```cs
 using Deeplinks;
 
 private void Awake() 
 {
    Deeplink.Received += OnDeeplinkReceived;
 }
 
 private void OnDeeplinkReceived(string deepLink)
 {
    Debug.Log("Deeplink: " + deepLink);
 }
 ```
 * **Note:** If there are no handlers subscribed to `Deeplink.Received` when a deeplink is received they will be stored until a handler is subcribed. The first handler that is subscribed will receive any queued deeplinks in the order they were originally received by the application. No deeplink events are 'lost' because a handler wasn't registered in time. It is recommended to only subcribe one handler which then routes the events to other parts of your application. 
## Integration
* Clone/download the repository
* Export the Assets folder as a .unitypackage
* Import the exported .unitypackage into your Unity project

# Setup
## Android
### Prerequisites
* Go to '(Unity/Edit)' -> 'Preferences' -> 'External Tools' menu
* Scroll down to 'Android' and ensure the SDK and JDK folder paths are set correctly

**No other setup is required, unless the main Android activity has been overridden in your project by you or another plugin, if so then..**
1. Merge the code from _SharedUnityPlayerActivity_ into your own activity or
subclass _SharedUnityPlayerActivity_ instead and make sure to call *super.onCreate(Bundle savedInstanceState)* and/or _super.onNewIntent(intent)_ if you have overridden the _onCreate_ or _onNewIntent_ methods.
2. Modify your existing AndroidManifest.xml to include the required placeholder variables and manifest entries. 
    + A basic AndroidManifest.xml is included with the plugin, it has placeholder variables that are replaced as a pre-build step when building in Unity, you can use it as a guide of which changes you need to make to your existing _AndroidManifest.xml_.
    + Make sure the package is using a placeholder variable instead of a hard-coded package name, this placeholder will be replaced with whatever the bundle-id is set to in Unity's _PlayerSettings_ eg. _com.YourCompany.YourProduct_ 
      ```xml 
      <manifest xmlns:android="http://schemas.android.com/apk/res/android" package="${applicationId}" ...
      ```
    + In the main activity, replace "[YOUR_ACTIVITY_CLASS_NAME]" with the name of your custom activity name.
      ```xml
      activity android:name="${applicationId}.[YOUR_ACTIVITY_CLASS_NAME]" ...
      ```
     + Then add the following inside the same (main) *activity* tag, inside the `<intent-filter>` tag
       ```xml
       <!-- Deeplinks.Plugin[start] -->
       <action android:name="android.intent.action.VIEW" />
       <category android:name="android.intent.category.DEFAULT" />
       <category android:name="android.intent.category.BROWSABLE" />
       <data android:scheme="${deeplinkScheme}" />
       <!-- Deeplinks.Plugin[end] -->
       ```
If you don't have your own AndroidManifest.xml, yet you can fetch a unity-generated one by first triggering a build and then copying it from *UnityProject\Temp\StagingArea\AndroidManifest.xml* into *Assets/Plugins/Android/AndroidManifest.xml*

#### Why not handle deeplinks in a second activity?
Some might suggest having a "side-activity" e.g. *SharedUnityPlayerActivity* to handle the deeplink and start the main Unity player activity. This way, the main Unity player activity remains clean of "outside code", right? Wrong. Consider the Unity app is currently not running. Then:
* A deeplink gets activated
* SharedUnityPlayerActivity starts
* Tries to access the UnityPlayer object in order to send a message to a Unity script with the deeplink information
* At this point, since the Unity native libraries are not yet initialized, the call would fail with the error:
 ```
 Native libraries not loaded - dropping message for ...
 ```
Bottom line: you need the Unity player activity initialized in order to call Unity functions from native code. The only way to handle the scenario above would be to have the Unity player activity itself handle the deeplink. Unity will make sure it's initialized prior to the call.

## iOS
UnityDeeplinks implements a native plugin for iOS, initialized by a private class *UnityDeeplinkReceiver*. The plugin listens for URL/Univeral Link activations and relayes them to the Unity script for processing. It, too, uses a similar approach as the one used for Android: the main Unity app controller gets subclassed.

Also, like in the Android case, if the app is currently not running, we can't simply have the native low-level deeplinking code make calls to Unity scripts until the Unity libraries are initialized. Therefore, we store the deeplink in a variable, wait for the app to initialize the plugin (an indication that the Unity native libraries are ready), and then send the stored deeplink to the Unity script.

* To support Universal Links, set them up as per [their specification](https://developer.apple.com/library/content/documentation/General/Conceptual/AppSearch/UniversalLinks.html). *Note:* Universal Link settings in your XCode project are not removed during Unity rebuilds, unless you use the *Replace* option and overwrite your XCode project

## Testing

* Prepare a dummy web page that is accessible by your mobile device:

 ```xml
 <body>
 <a href="myapp://?a=b">deeplink test</a>
 </body>
 ```

* Open the web page on the device browser and click the deeplink
* The Unity app should open and the Deeplink.Received event should be invoked, notifying any subscribed listeners.
