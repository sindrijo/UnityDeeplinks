 param ([string]$jsonArg )

### functions
function CreateDirectoryIfNeeded($Path, $m) {
    if(-Not(Test-Path $Path)) {
        New-Item $Path -ItemType Directory | Out-Null
    }
}
### /end-functions
Write-Output "JSON-----"

if($jsonArg)
{
    Write-Output "Using json arg: $jsonArg"
    $buildConfig = ConvertFrom-Json $jsonArg
}
else {
    Write-Output "Reading json from file"
    $buildConfig = Get-Content $pwd'\Editor\Resources\deeplink-build-conf.json' | ConvertFrom-Json    
}


$env:UNITY_LIBS = $buildConfig.UnityEditorDataPath + '\PlaybackEngines\AndroidPlayer\Variations\mono\Release\Classes\classes.jar'
if(-Not(Test-Path $env:UNITY_LIBS)) {
    throw "Unity libs not found @ $env:UNITY_LIBS"
}

$env:JDK_HOME = $buildConfig.JdkPath
if(-Not(Test-Path $env:JDK_HOME)) {
    throw "JDK not found @ $env:JDK_HOME"
}

$env:ANDROID_SDK_ROOT = $buildConfig.AndroidSdkRoot
if(-Not(Test-Path $env:ANDROID_SDK_ROOT)) {
    throw "Android SDK not found @ $env:ANDROID_SDK_ROOT"
}

$targetAndroidApiLevel = $buildConfig.AndroidTargetSdkVersion
$env:ANDROID_PLATFORM_JAR = "$env:ANDROID_SDK_ROOT\platforms\android-$targetAndroidApiLevel\android.jar"

if($targetAndroidApiLevel -eq 0){

    Write-Output "Target Android Api-Level is $targetAndroidApiLevel, using latest Android SDK"

    #determine latest version available, descending sort should give us the latest version
    $latestAndroidApiLevelFolderName = (Get-ChildItem -Path ($env:ANDROID_SDK_ROOT + "\platforms\") -Directory |
       Where-Object {$_.Name -Match 'android-([0-9]{2,})$'} | 
       Sort-Object -Descending |
       Select-Object -First 1).Name

    $env:ANDROID_PLATFORM_JAR = "$env:ANDROID_SDK_ROOT\platforms\$latestAndroidApiLevelFolderName\android.jar"
}

if(-Not(Test-Path $env:ANDROID_PLATFORM_JAR)) {
    throw "Target Android SDK not found @ $env:ANDROID_PLATFORM_JAR"
} else {
    Write-Output "Using Android SDK: $env:ANDROID_PLATFORM_JAR"
}

$env:BOOTCLASSPATH = $env:JDK_HOME + "jre\lib"
$env:CLASSPATH = "$env:UNITY_LIBS;$env:ANDROID_PLATFORM_JAR"

$unityProjectRootPath = $buildConfig.UnityProjectRootPath
$javaTempCompileRootPath = "$unityProjectRootPath\Temp\DeeplinkCompile\"
CreateDirectoryIfNeeded -Path $javaTempCompileRootPath -m "Creating compile root directory"

# copy the template(s) into the temp-folder
Write-Output "Copying .java.template files to temp..."
Copy-Item -Path "*.java.template" -Destination $javaTempCompileRootPath -Force
$javaTemplateFile = Get-ChildItem -Path $javaTempCompileRootPath*.java.template | Select-Object -First 1 

# insert company name from build settings and save to non-template java file
Write-Output "Filling out template variables..."
(Get-Content $javaTemplateFile).Replace("{packageName}", $buildConfig.AndroidPackageName) | Set-Content (($javaTemplateFile) -replace ".template$","") -Force 

# Create a directory to do our compiling in
$javaTempCompilePath = "$javaTempCompileRootPath\bin\"
CreateDirectoryIfNeeded -Path $javaTempCompilePath -m "Creating compile pkg directory ..."

Write-Output "Compiling ..."
#Write-Output $env:BOOTCLASSPATH
#Write-Output $env:CLASSPATH
#Unity fails to build with target 1.8
& $env:JDK_HOME'\bin\javac' $javaTempCompileRootPath*.java -source 1.7 -target 1.7 -bootclasspath $env:BOOTCLASSPATH -classpath $env:CLASSPATH -d $javaTempCompilePath
Write-Output "Manifest-Version: 1.0" > MANIFEST.MF
Write-Output "Creating jar file..."
$jarPath = "$javaTempCompileRootPath\UnityDeeplinks.jar"
# slash means "here"
& $env:JDK_HOME'\bin\jar' cvfM $jarPath -C $javaTempCompilePath /
Copy-Item -Path $jarPath -Destination UnityDeeplinks.jar
Write-Output ".jar created @ UnityDeeplinks.jar"

# Clean up
Write-Output "Cleaning up temorary files..."
Remove-Item $javaTempCompileRootPath -Force  -Recurse -ErrorAction SilentlyContinue
Write-Output "Done!"