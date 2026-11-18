# LUMEN LOOM — FINAL BUILD INFORMATION
**Document Version:** 1.0.2 (Post-Unity Verification Build Sign-Off)  
**Build Date:** November 18, 2026 (21:15 IST)  
**Build Pipeline:** `Assets/Editor/AndroidBuildPipeline.cs`  
**Engine Version:** Unity 6 LTS (`6000.0.85f2`)  

---

## 1. Application & Package Metadata

| Property | Value | Notes |
| :--- | :--- | :--- |
| **Application Name** | **LUMEN LOOM** | Spatial Optical Puzzle Experience |
| **Package Identifier** | `com.DefaultCompany.LUMENLOOM` | Android Application ID |
| **Version Name** | `1.0.0` | Release Candidate 1 (v1.0.0-RC1) |
| **Version Code** | `1` | Canonical initial release build number |
| **Scripting Backend** | **IL2CPP** | High-performance Ahead-Of-Time C++ compilation |
| **Target Architecture** | **ARM64 (`arm64-v8a`)** | Required 64-bit architecture for Meta Quest |
| **Minimum Android API** | **API 29 (Android 10)** | Minimum supported Quest OS base |
| **Target Android API** | **API 34 (Android 14)** | Compliant with Google Play & Quest Store standards |
| **Render Pipeline** | **URP 17.0.4** | Universal Render Pipeline Mobile Optimized |
| **Color Space** | **Linear** | Accurate optical photometric falloff |
| **Stereo Rendering Mode** | **Single Pass Instanced (Multiview)**| Minimizes mobile CPU draw call overhead |

---

## 2. Generated Release Artifact Details

### 2.1 Meta Quest Android Standalone APK (Freshly Built)
```
Artifact Path: Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk
File Size:     37,371,596 bytes (~35.64 MB)
Architecture:  ARM64 (arm64-v8a)
Backend:       IL2CPP / Android Gradle
SHA-256:       CA6B6C417A956C47BA9592BB8B232BFED8E557A87EC5E40F883F996D763F0FBF
Build Time:    719.3 seconds
Build Status:  SUCCESS (Exit Code 0)
```

### 2.2 Windows Standalone x86_64 Executable (Freshly Built)
```
Artifact Path: Builds/LUMEN_LOOM_Windows/LumenLoom.exe
File Size:     671,744 bytes (Executable) + 34,446,256 bytes (UnityPlayer.dll)
Architecture:  x86_64
Backend:       Mono / Windows Standalone 64-bit
Build Status:  SUCCESS (Exit Code 0)
```

---

## 3. Package Dependencies (Locked in Unity 6000.0.85f2)

| Package Identifier | Version | Description |
| :--- | :---: | :--- |
| `com.unity.xr.openxr` | `1.16.1` | Khronos OpenXR runtime backend for Meta Quest |
| `com.unity.xr.hands` | `1.7.3` | Low-allocation joint pose ingestion & hand tracking |
| `com.unity.xr.management` | `4.6.1` | XR loader lifecycle and subsystem orchestration |
| `com.unity.xr.core-utils` | `2.4.0` | Shared XR spatial math, pose drivers & origin utilities |
| `com.unity.inputsystem` | `1.20.0` | Unity Input System package |
| `com.unity.render-pipelines.universal` | `17.0.4` | URP graphics pipeline |
| `com.unity.test-framework` | `1.6.0` | Unity Test Framework for automated QA |

---

## 4. Android Manifest Security & Hardware Permissions

The Android Gradle post-generation processor (`QuestPostGenerateGradleAndroidProject.cs`) verified and injected all required Oculus / Meta Quest OS metadata during build:

### 4.1 Supported Devices Metadata
```xml
<meta-data android:name="com.oculus.supportedDevices" 
           android:value="quest|quest2|cambria|eureka|quest3" />
```
*Note: Canonical identifiers `eureka` and `quest3` are registered to ensure native recognition on Meta Quest 3 hardware.*

### 4.2 Hand Tracking & VR Intent Permissions
```xml
<!-- Hand Tracking Permissions -->
<uses-feature android:name="oculus.software.handtracking" android:required="false" />
<uses-permission android:name="com.oculus.permission.HAND_TRACKING" />

<!-- VR Headset Intent Filter -->
<intent-filter>
    <action android:name="android.intent.action.MAIN" />
    <category android:name="android.intent.category.LAUNCHER" />
    <category android:name="com.oculus.intent.category.VR" />
</intent-filter>

<!-- System Focus & Low-Latency Audio -->
<meta-data android:name="com.oculus.vr.focusaware" android:value="true" />
```

---

## 5. Build Pipeline Execution Commands

### 5.1 Standalone Quest Android APK Build
```bash
Unity.exe -quit -batchmode -projectPath "C:/Path/To/LUMEN-LOOM" -executeMethod LumenLoom.Editor.AndroidBuildPipeline.BuildQuestApk -logFile "build_quest.log"
```
The pipeline automatically configures Android ARM64, enables the OpenXR loader, validates the scene list, processes the Gradle manifest, and compiles the release APK to `Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk`.

### 5.2 Standalone Windows (x86_64) PC Build
```bash
Unity.exe -quit -batchmode -projectPath "C:/Path/To/LUMEN-LOOM" -executeMethod LumenLoom.Editor.AndroidBuildPipeline.BuildWindowsStandalone -logFile "build_windows.log"
```
Compiles the Windows desktop standalone executable to `Builds/LUMEN_LOOM_Windows/LumenLoom.exe`.
