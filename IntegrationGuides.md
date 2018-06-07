# Integration Guide
The following steps are only required if the _main unity-container_ is (already) overridden in your project, by you or a plugin.
More specifically, something is subclassing _UnityPlayerActivity_ (the main, 'container' activity) on **Android** and/or _UnityAppController_ on **iOS**, respectively.

# Android
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

# iOS
Merge the code from _UnityDeeplinks.mm_ into your own app-controller, taking care to either delete _UnityDeeplinks.mm_ from your project or comment out ```IMPL_APP_CONTROLLER_SUBCLASS(UnityDeeplinksAppController)``` in the _UnityDeeplinks.mm_ file.
