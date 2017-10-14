# UnityDeeplinks
A set of tools for Unity to allow handling deeplink activation from within Unity scripts, for Android and iOS, including iOS Universal Links.
### Known Issues/Limitations
* Check out the repo's *issues* section
## Usage
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
 * If there are no handlers subscribed to `Deeplink.Received` when deeplink is received the plugin will store them in a queue until a handler is subcribed. The first handler that is subscribed it will then immediately receive any queued deeplinks in the order they were original received, this is to ensure that no deeplink events are 'lost' because a handler wasn't registered in time. This shouldn't be an issue as long as you subscribe to `Deeplink.Received` in `Awake()` in a script that is placed in your startup scene.
## Integration
* Clone/download the repository
* Copy the entire Assets folder into your Unity project folder
## Android
Subclass the default *UnityPlayerActivity* in order to add deeplink-handling code that marshals deeplinks into your Unity script:

* Replace the default UnityPlayerActivity in your Assets/Plugins/Android/AndroidManifest.xml with com.{yourCompany}.MyUnityPlayerActivity:
* If you don't have your own AndroidManifest.xml yet you can fetch a unity-generated one by first triggering a build and then copying it from *UnityProject\Temp\StagingArea\AndroidManifest.xml* into *Assets/Plugins/Android/AndroidManifest.xml*

 ```xml
 <!--
 <activity android:name="com.unity3d.player.UnityPlayerActivity" ...
 -->
 <activity android:name="com.{yourCompany}.SharedUnityPlayerActivity" ...
 ```

* Add the following inside the same *activity* tag, assuming your deeplink URL scheme is myapp://

 ```xml
 <intent-filter>
     <action android:name="android.intent.action.VIEW" />
     <category android:name="android.intent.category.DEFAULT" />
     <category android:name="android.intent.category.BROWSABLE" />
     <data android:scheme="myapp" />
 </intent-filter>
 ```

* Notes:
 * If you already subclassed your Unity activity, merge the code from within *SharedUnityPlayerActivity* into your existing subclass

### Why not handle deeplinks in a second activity?
Some might suggest having a "side-activity" e.g. *SharedUnityPlayerActivity* to handle the deeplink and start the main Unity player activity. This way, the main Unity player activity remains clean of "outside code", right? Wrong. Consider the Unity app is currently not running. Then:
* A deeplink gets activated
* SharedUnityPlayerActivity starts
* Tries to access the UnityPlayer object in order to send a message to a Unity script with the deeplink information
* At this point, since the Unity native libraries are not yet initialized, the call would fail with the error:
 ```
 Native libraries not loaded - dropping message for ...
 ```
Bottom line: you need the Unity player activity initialized in order to call Unity functions from native code. The only way to handle the scenario above would be to have the Unity player activity itself handle the deeplink. Unity will make sure it's initialized prior to the call.

### Building the UnityDeeplinks.jar file
Only perform this step if you made changes to any .java file under *Assets/UnityDeeplinks/Android/* or would like to rebuild it using an updated version of Unity classes, Android SDK, JDK and so on.

#### Prerequisites
* Go to your Unity => Preferences => External tools menu and find your Android SDK and JDK home folders
* Edit *UnityDeeplinks/Android/bulid_jar.sh*
* Ensure ANDROID_SDK_ROOT points to your Android SDK root folder
* Ensure JDK_HOME points to your JDK root folder
* Ensure UNITY_LIBS points to the Unity classes.jar file in your development environment

#### Build instructions
Run the build script:
```
cd MY_UNITY_PROJECT_ROOT/Assets/Plugins/UnityDeeplinks/Android
./build_jar.sh
```

This creates/updates a *UnityDeeplinks.jar* file under your Unity project's Assets/Plugins/UnityDeeplinks folder.

Finally, continue to build and test your Unity project as usual in order for any jar changes to take effect

## iOS
UnityDeeplinks implements a native plugin for iOS, initialized by *Assets/Plugins/Scripts/Internal/UnityDeeplinkReceiver.cs*. The plugin listens for URL/Univeral Link activations and relayes them to the Unity script for processing. It, too, uses a similar approach as the one used for Android: the main Unity app controller gets subclassed.

Also, like in the Android case, if the app is currently not running, we can't simply have the native low-level deeplinking code make calls to Unity scripts until the Unity libraries are initialized. Therefore, we store the deeplink in a variable, wait for the app to initialize the plugin (an indication that the Unity native libraries are ready), and then send the stored deeplink to the Unity script.

* To support URL schemes, go to your Unity project's Build => iOS Player Settings => Other Settings => Supported URL Schemes and set:
 * Size: 1
 * Element 0: your URL scheme, e.g. myapp
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
