# LUMEN LOOM — INSTALLATION & QUICK START GUIDE
**Document Version:** 1.0.0  
**Target Platform:** Meta Quest 3 / Meta Quest Pro / Meta Quest 2 / Windows PC  
**Release Binary:** `Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk`  
**Package:** `com.DefaultCompany.LUMENLOOM`  

---

## 1. Quick Installation on Meta Quest (Standalone)

### Pre-Requisites
1. Meta Quest headset (Quest 3, Quest Pro, or Quest 2).
2. Developer Mode enabled on the headset (via the Meta Quest mobile app).
3. USB-C 3.0 data cable connecting headset to PC.
4. Android SDK Platform-Tools (`adb`) installed on the host PC.

---

### Method A: Command-Line Installation via ADB (Recommended)

1. **Verify Headset Connection:**
   Open PowerShell or Terminal and check connected devices:
   ```powershell
   adb devices -l
   ```
   *Expected output: Device serial followed by `device product:eureka model:Quest_3`.*

2. **Install / Sideload the APK:**
   ```powershell
   adb install -r "Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk"
   ```
   *Expected output: `Performing Streamed Install` $\rightarrow$ `Success`.*

3. **Launch the Game:**
   You can launch the game directly via ADB:
   ```powershell
   adb shell am start -n com.DefaultCompany.LUMENLOOM/com.unity3d.player.UnityPlayerActivity
   ```
   *Or launch manually in VR from Quest App Library $\rightarrow$ Filter: "Unknown Sources" $\rightarrow$ **LUMEN LOOM**.*

4. **Monitor Diagnostics (Optional):**
   ```powershell
   adb logcat -s Unity OpenXR
   ```

5. **Uninstall (If Needed):**
   ```powershell
   adb uninstall com.DefaultCompany.LUMENLOOM
   ```

---

### Method B: Graphical Installation via SideQuest

1. Open **SideQuest** on PC.
2. Connect your Quest headset via USB-C and confirm the green connection dot at top-left.
3. Click the **"Install APK file from folder on computer"** icon in the top toolbar.
4. Select `Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk`.
5. Wait for the green notification: `All tasks completed!`.
6. Put on your headset, navigate to **App Library $\rightarrow$ Unknown Sources**, and launch **LUMEN LOOM**.

---

## 2. Quick Start in Unity 6 Editor (Desktop & XR PlayMode)

1. **Prerequisites:** Unity 6 LTS (`6000.0.32f1`) with Android Build Support and OpenXR.
2. **Open Project:** In Unity Hub, click **Add** and select the `LUMEN-LOOM` directory.
3. **Open Scene:** In Project view, open `Assets/Scenes/MainPuzzleScene.unity`.
4. **Play:** Press the Unity **Play** button!
   - **With Quest connected via Quest Link / AirLink:** The game automatically initializes OpenXR in full 6DoF stereoscopic VR with hand tracking.
   - **Without VR headset:** The game automatically defaults to Desktop Mode with high-angle orbit camera and mouse controls.

---

## 3. Controls Reference Guide

### Natural Hand Tracking (Meta Quest VR)

| Player Action | Physical Hand Gesture | Result in Game |
| :--- | :--- | :--- |
| **Select Node** | Bring open hand within $0.15\text{ m}$ of node $\rightarrow$ pinch thumb & index ($\le 25\text{ mm}$) | Grabs optical node with cyan pulse and audio chime. |
| **Move Node** | Maintain pinch and move hand horizontally across table | Translates node smoothly across obsidian table plane. |
| **Rotate Node** | Maintain pinch and twist wrist clockwise / counter-clockwise | Steers reflector / prism and dynamically redirects beam. |
| **Release Node** | Open thumb and index fingers ($> 35\text{ mm}$) | Drops node in place with confirmation audio tick. |
| **Dual-Hand Play** | Grab separate nodes with Left and Right hands | Manipulate two optical nodes simultaneously and independently. |
| **Reset Chamber** | Tap the floating holographic **RESET** pedestal in VR | Restores default node positions for current chamber. |

---

### Desktop Controls (Mouse & Keyboard Fallback)

| Action | Control Key / Mouse Button |
| :--- | :--- |
| **Select Node** | Left Mouse Click (LMB) on node |
| **Move Node** | Hold LMB & drag across chamber floor |
| **Rotate Node** | **Q** (Counter-Clockwise) / **E** (Clockwise) or **Mouse Scroll Wheel** |
| **Orbit Camera** | Hold Right Mouse Button (RMB) & move mouse |
| **Deselect** | **Escape** key or click empty space |
| **Reset Chamber** | **R** key or on-screen **RESET** button |
| **Chamber Select** | Click on-screen **<< PREV** / **NEXT >>** buttons (unlocked on solve) |
| **Toggle Guide** | Click **GUIDE / HELP [?]** button at bottom right |

---

## 4. Troubleshooting & FAQ

- **Q: Sideload install fails with `INSTALL_FAILED_UPDATE_INCOMPATIBLE`?**  
  *A: Uninstall any previous development build using `adb uninstall com.DefaultCompany.LUMENLOOM`, then reinstall.*
- **Q: Hands not showing up in VR?**  
  *A: Ensure Hand Tracking is enabled in Quest System Settings $\rightarrow$ Movement tracking $\rightarrow$ Hand tracking. Put down controllers to let Quest switch to hand tracking.*
- **Q: Table is slightly too low or high?**  
  *A: Use the in-game table height calibration in the VR HUD settings to adjust elevation by $\pm 0.4\text{ m}$, or perform a standard Quest Guardian reset.*
