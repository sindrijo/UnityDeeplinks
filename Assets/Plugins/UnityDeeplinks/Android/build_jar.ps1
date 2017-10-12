$env:UNITY_LIBS="C:\Program Files\Unity\Editor\Data\PlaybackEngines\AndroidPlayer\Variations\mono\Release\Classes\classes.jar"
$env:ANDROID_SDK_ROOT="C:\Program Files (x86)\Android\android-sdk"
$env:JDK_HOME="C:\Program Files\Java\jdk1.8.0_121"
$env:BOOTCLASSPATH="$env:JDK_HOME\jre\lib"
$env:CLASSPATH="$env:UNITY_LIBS;$env:ANDROID_SDK_ROOT\platforms\android-25\android.jar"

echo "Compiling ..."
echo $env:BOOTCLASSPATH
echo $env:CLASSPATH

#Unity fails to build with target 1.8
& $env:JDK_HOME'\bin\javac' *.java -source 1.7 -target 1.7 -bootclasspath $env:BOOTCLASSPATH -classpath $env:CLASSPATH -d .

echo "Manifest-Version: 1.0" > MANIFEST.MF

echo "Creating jar file..."
& $env:JDK_HOME'\bin\jar' cvfM ..\UnityDeeplinks.jar com/