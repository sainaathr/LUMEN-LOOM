# LUMEN LOOM — FINAL BUILD INFORMATION
**Document Version:** 1.0.0  
**Build Target:** Meta Quest VR (Standalone Android ARM64)  
**Release Candidate:** v1.0.0-RC1  
**Build Date:** December 1, 2026  
**Build Pipeline:** `Assets/Editor/AndroidBuildPipeline.cs`  

---

## 1. Application & Package Metadata

| Property | Value | Notes |
| :--- | :--- | :--- |
| **Application Name** | **LUMEN LOOM** | Spatial Optical Puzzle Experience |
| **Package Identifier** | `com.DefaultCompany.LUMENLOOM` | Android Application ID |
| **Version Name** | `1.0.0` | Release Candidate 1 (v1.0.0-RC1) |
| **Version Code** | `1` | Canonical initial release build number |
| **Engine Version** | **Unity 6 LTS (`6000.0.32f1`)** | Official Long-Term Support Release |
| **Scripting Backend** | **IL2CPP** | High-performance Ahead-Of-Time C++ compilation |
| **Target Architecture** | **ARM64 (`arm64-v8a`)** | Required 64-bit architecture for Meta Quest |
| **Minimum Android API** | **API 29 (Android 10)** | Minimum supported Quest OS base |
| **Target Android API** | **API 34 (Android 14)** | Compliant with Google Play & Quest Store standards |
| **Render Pipeline** | **URP 17.0.3** | Universal Render Pipeline Mobile Optimized |
| **Color Space** | **Linear** | Accurate optical photometric falloff |
| **Stereo Rendering Mode** | **Single Pass Instanced (Multiview)**| Minimizes mobile CPU draw call overhead |

---

## 2. Binary Artifact Details

```
Artifact File: Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk
File Size:     35,740,506 bytes (~34.08 MB)
Architecture:  arm64-v8a
SHA-256:       75C1621556001C5A1C5552F4C2DF465501710DFCEEE4E5CA11F2CCEEFFEA141A
MD5:           9D8E6F0C3348C9A8E3036F9B890F3D0C
```

---

## 3. Package Dependencies & Lockfile Inventory

Extracted directly from `Packages/manifest.json` and `Packages/packages-lock.json`:

| Package Identifier | Version | Description |
| :--- | :---: | :--- |
| `com.unity.xr.openxr` | `1.12.1` | Khronos OpenXR runtime backend for Meta Quest |
| `com.unity.xr.hands` | `1.4.1` | Low-allocation joint pose ingestion & hand tracking |
| `com.unity.xr.management` | `4.4.0` | XR loader lifecycle and subsystem orchestration |
| `com.unity.xr.core-utils` | `2.4.0` | Shared XR spatial math, pose drivers & origin utilities |
| `com.unity.inputsystem` | `1.11.2` | Unity Input System package |
| `com.unity.render-pipelines.universal` | `17.0.3` | URP graphics pipeline |
| `com.unity.test-framework` | `1.4.5` | Unity Test Framework for automated QA |

---

## 4. Android Manifest Security & Hardware Permissions

The Android Gradle post-generation processor (`QuestPostGenerateGradleAndroidProject.cs`) verifies and injects all required Oculus / Meta Quest OS metadata:

### 4.1 Supported Devices Metadata
```xml
<meta-data android:name="com.oculus.supportedDevices" 
           android:value="quest|quest2|cambria|eureka|quest3" />
```
*Note: Canonical identifier `eureka` and `quest3` are registered to ensure native recognition on Meta Quest 3 hardware.*

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

## 5. Build Pipeline Execution Command

To reproduce this exact binary from Unity Editor in headless batchmode:
```bash
Unity.exe -quit -batchmode -projectPath "C:/Path/To/LUMEN-LOOM" -executeMethod LumenLoom.Editor.AndroidBuildPipeline.BuildAndroidQuest -logFile "build.log"
```
The pipeline automatically configures Android ARM64, enables the OpenXR loader, validates the scene list, processes the Gradle manifest, and compiles the signed release APK to the project-relative path `Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk`.
