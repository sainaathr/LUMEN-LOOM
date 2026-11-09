# LUMEN LOOM — META QUEST 3 PHYSICAL HARDWARE VALIDATION CHECKLIST
**Document Version:** 1.0.0  
**Date:** November 9, 2026  
**Target Hardware:** Meta Quest 3 / Meta Quest 2 / Meta Quest Pro  
**Target OS:** Meta Quest OS (Android 12L / 14, API 32-34)  
**Package:** `com.DefaultCompany.LUMENLOOM`  
**Binary Artifact:** `D:\Unity\Builds\LUMEN_LOOM_Quest\LumenLoom_Quest.apk`  
**Classification:** Manual On-Device Quality Assurance Protocol

---

## 1. Safety & Operational Notice

> [!IMPORTANT]
> This checklist contains manual QA validation steps that **CANNOT** be executed in automated batchmode or emulated without a physical Meta Quest 3 headset. Every test in this document requires actual hardware execution and on-device observation.
> Do NOT mark any test as passed until observed and verified on physical hardware by the QA tester.

---

## 2. Pre-Requisites & Deployment Instructions

### Required Equipment
- 1x Meta Quest 3 Headset (Developer Mode enabled)
- 1x USB-C 3.0 data cable
- Host PC running Windows 11 with Android SDK Platform-Tools (`adb.exe`)

### Step-by-Step Installation
1. Connect the Quest 3 headset to the PC via USB-C.
2. Put on the headset and accept the "Allow USB Debugging" prompt.
3. Verify connection in PowerShell:
   ```powershell
   adb devices -l
   ```
   *Expected output: Device ID followed by `device product:eureka model:Quest_3`.*
4. Sideload the release APK:
   ```powershell
   adb install -r "D:\Unity\Builds\LUMEN_LOOM_Quest\LumenLoom_Quest.apk"
   ```
5. Launch the application via ADB or Quest App Library ("Unknown Sources"):
   ```powershell
   adb shell am start -n com.DefaultCompany.LUMENLOOM/com.unity3d.player.UnityPlayerActivity
   ```
6. Monitor live Logcat stream for errors:
   ```powershell
   adb logcat -s Unity OpenXR
   ```

---

## 3. Physical Hardware Test Matrix

| ID | Category | Test Case Description | Expected Result | Pass / Fail |
| :--- | :--- | :--- | :--- | :---: |
| **Q3-01** | **Application Launch** | Launch LUMEN LOOM from Quest Unknown Sources menu. | Unity splash screen appears cleanly without flickering, black screen, or crashes. | [ ] |
| **Q3-02** | **Immersive VR Entry** | Transition from 2D system window into full 6DoF immersive VR space. | Headset switches to stereoscopic 3D view; chamber environment renders with skybox, floor table, and optical nodes at 72/90/120 Hz. | [ ] |
| **Q3-03** | **Head Tracking (6DoF)** | Move head in all 6 degrees of freedom (pitch, yaw, roll, surge, sway, heave). | Virtual camera tracks 1:1 with physical head movement without jitter, drift, latency, or nausea-inducing lag. | [ ] |
| **Q3-04** | **Floor Height & Guardian Alignment** | Stand at normal height and observe puzzle table level. | Puzzle table is positioned at waist level ($Y \approx 0.5\text{ m}$ relative to floor origin) aligned with Quest floor-level boundary. | [ ] |
| **Q3-05** | **Left Hand Acquisition** | Bring left hand into headset field of view (FOV). | Hand is detected by Quest OS; Unity XR Hands reports `IsTracked = true`; left-hand joint poses update in real time. | [ ] |
| **Q3-06** | **Right Hand Acquisition** | Bring right hand into headset FOV. | Hand is detected by Quest OS; Unity XR Hands reports `IsTracked = true`; right-hand joint poses update in real time. | [ ] |
| **Q3-07** | **Pinch Detection (Engage)** | Close right thumb and index fingertips together ($\le 25\text{ mm}$). | Pinch engages smoothly; haptic/audio click or visual pinch confirmation triggers reliably. | [ ] |
| **Q3-08** | **Pinch Release** | Open thumb and index fingertips apart ($> 35\text{ mm}$). | Pinch releases cleanly; hysteresis prevents flickering between 25mm and 35mm. | [ ] |
| **Q3-09** | **Node Selection (Proximity Grab)** | Position pinched fingers within $0.15\text{ m}$ of interactive optical node. | Target node emits hover glow upon proximity and selects (holographic cyan pulse) upon pinch engagement. | [ ] |
| **Q3-10** | **Node Translation (Dragging)** | Maintain pinch on node and move hand horizontally along table plane. | Optical node follows hand position smoothly, clamped within chamber boundaries $[-4.5\text{ m}, +4.5\text{ m}]$. | [ ] |
| **Q3-11** | **Real-Time Laser Beam Conduction** | Drag optical node into active laser beam path. | Beam reflects/splits dynamically and updates visually in real time at full framerate without perceptible lag. | [ ] |
| **Q3-12** | **Simultaneous Two-Hand Interaction** | Grab Node Alpha with Left hand and Node Beta with Right hand simultaneously. | Both hands manipulate their respective nodes independently; no hand overrides or steals the other hand's node. | [ ] |
| **Q3-13** | **Contention Protection** | Attempt to pinch and grab Left hand's held node with the Right hand. | Right hand is blocked from stealing Left hand's node; ownership remains strictly with Left hand. | [ ] |
| **Q3-14** | **Single-Hand Release Independence** | Release Left hand while maintaining pinch and drag on Right hand. | Left hand drops its node; Right hand continues dragging its node uninterrupted. | [ ] |
| **Q3-15** | **Hand Tracking Loss & Recovery** | Move Left hand behind back or outside FOV while holding a node. | Left hand safely drops node without sticking; upon returning to FOV, hand re-acquires without phantom grabs. | [ ] |
| **Q3-16** | **Puzzle Target Reception** | Guide laser beam into Target Receiver using optical nodes. | Receiver charges up with harmonic audio chime, particle burst, and triggers chamber victory sequence. | [ ] |
| **Q3-17** | **Chamber Progression** | Complete Chamber 01 and observe transition to Chamber 02. | Scene / chamber resets cleanly; new chamber layout loads; beam re-routes without leftover geometry. | [ ] |
| **Q3-18** | **Framerate & Performance** | Play continuously for 10 minutes across all 4 chambers. | Stable framerate ($\ge 72\text{ fps}$), zero thermal throttling warnings, no severe GC stutter. | [ ] |

---

## 4. Hardware Verification Sign-Off

**Tester Name:** ____________________________________  
**Device Serial Number:** ___________________________  
**OS Version / Build Number:** _______________________  
**Date of Execution:** _______________________________  
**Overall Verdict:** [ ] PASS  /  [ ] FAIL / [ ] CONDITIONAL PASS (Defects logged)  
**Notes / Observed Irregularities:**  
__________________________________________________________________________________________
__________________________________________________________________________________________

