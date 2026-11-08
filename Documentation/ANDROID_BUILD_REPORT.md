# ANDROID QUEST BUILD REPORT
**Project:** LUMEN LOOM  
**Target Platform:** Meta Quest 2 / Quest 3 / Quest Pro (Android ARM64)  
**Unity Engine Version:** `6000.0.32f1` (Unity 6 LTS)  
**Date:** November 8, 2026  
**Build Result:** `SUCCESS`  

---

## 1. Build Artifact Details

- **Output Path:** `D:\Unity\Builds\LUMEN_LOOM_Quest\LumenLoom_Quest.apk`
- **Output Size:** `33,489,822 bytes` (approx. **31.9 MB**)
- **Build Duration:** `848.8 seconds` (approx. 14.1 minutes)
- **Target Architecture:** `ARM64` (`AndroidArchitecture.ARM64`)
- **Scripting Backend:** `IL2CPP`
- **Minimum Android SDK:** `API Level 29 (Android 10.0)`
- **Target Android SDK:** `API Level 34 (Android 14.0)`
- **NDK Version Used:** `23.1.7779620` (bundled with Unity Android Player)
- **Application Identifier:** `com.DefaultCompany.LUMENLOOM`
- **Build Log:** `C:\Users\Administrator\Desktop\Projects\LUMEN LOOM\Logs\BuildAndroidApk.log`

---

## 2. Platform Compliance & Settings

| Setting | Value | Verification Status |
|---|---|---|
| Scripting Implementation | IL2CPP | `VERIFIED` |
| Target Architectures | ARM64 (Bit 1) | `VERIFIED` |
| Minimum API Level | 29 | `VERIFIED` |
| Target API Level | 34 | `VERIFIED` |
| Render Pipeline | Universal Render Pipeline (URP 17.0.3) | `VERIFIED` |
| Included Scenes | `Assets/Scenes/MainPuzzleScene.unity` | `VERIFIED` |

---

## 3. Deployment & Installation Instructions for Meta Quest

To sideload and test on a physical Meta Quest headset via Android Debug Bridge (ADB):

```bash
# Verify Quest device is connected and authorized
adb devices

# Install APK to device
adb install -r "D:\Unity\Builds\LUMEN_LOOM_Quest\LumenLoom_Quest.apk"

# Launch LUMEN LOOM on Quest
adb shell am start -n com.DefaultCompany.LUMENLOOM/com.unity3d.player.UnityPlayerActivity
```

*Note: Sideloaded build will appear under Quest Apps > Unknown Sources.*
