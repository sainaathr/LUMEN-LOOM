# LUMEN LOOM — Teammate Handoff & Setup Guide

**Welcome to LUMEN LOOM!**  
Day 1 development has delivered a fully verified, clean, zero-error playable prototype in Unity 6 LTS (`6000.0.32f1`). All core systems, beam raycasting, node clamping, rotation, solving, resetting, and UI HUD have been tested and verified (20/20 automated tests passing).

---

## 1. Prerequisites & Environment

- **Unity Editor:** Unity 6 LTS (`6000.0.32f1` or later).
- **Target OS:** Windows 10/11 with dedicated GPU (NVIDIA RTX 3050+ recommended for Meta XR Simulator).
- **Installed Modules:** Android Build Support (OpenJDK 17, Android NDK r23b, Android SDK Platform 33/34/35).
- **Git:** All work committed and synchronized.

---

## 2. Launching and Playing Desktop Prototype

1. Open **Unity Hub**.
2. Select **Add project from disk** and choose the `LUMEN LOOM` folder (`D:\Unity\Projects\LUMEN LOOM`).
3. Open `Assets/Scenes/MainPuzzleScene.unity`.
4. Click **Play** (`Ctrl+P`):
   - **Left-Click & Drag:** Move nodes across the puzzle plane.
   - **Q / E Keys / Scroll Wheel:** Rotate selected node to aim its beam.
   - **R Key:** Reset puzzle chamber.
   - **Escape:** Deselect current node.
5. Align Node Alpha to shoot Right ($+X$) and Node Beta to shoot Forward ($+Z$) into the Target Receiver to observe the solve sequence!

---

## 3. Dedicated VR Workstation Steps (Meta XR Integration)

Follow these exact steps on your RTX 3050 machine:

### Step 3.1: Install Meta XR Packages
1. In Unity, open **Window** > **Package Manager**.
2. Add the following dependencies in `Packages/manifest.json`:
   - `com.meta.xr.sdk.core` (`69.0.0`)
   - `com.meta.xr.sdk.interaction` (`69.0.0`)
   - `com.meta.xr.sdk.interaction.ovr` (`69.0.0`)
3. Enable **OpenXR** in **Project Settings** > **XR Plug-in Management** under both Windows and Android tabs.

### Step 3.2: Wire Hand Tracking to `HandInputProvider.cs`
Our game architecture was built specifically for this step! The gameplay layer does not care whether input comes from a mouse or a human hand:
1. In `MainPuzzleScene.unity`, add the Meta Hand Tracking rig.
2. In `NodeInteractionController`, set `inputProviderComponent` to `HandInputProvider`.
3. Add `HandGrabInteractable` to `PuzzleNode` prefabs.
4. Route hand grab callbacks to `HandInputProvider`:
   - `When Selecting`: Call `HandInputProvider.HandleMetaHandGrabStart(thisNode)`
   - `When Moving`: Call `HandInputProvider.HandleMetaHandMove(handAnchorPos)`
   - `When Unselecting`: Call `HandInputProvider.HandleMetaHandGrabEnd()`
   - `Wrist Turn`: Call `HandInputProvider.HandleMetaHandRotate(deltaAngle)`

---

## 4. Key Verification & Test Commands

Run the full automated test suite anytime using Unity batchmode:
```powershell
# PlayMode Integration Test Suite (10 gameplay tests)
& "D:\Unity\Editors\6000.0.32f1\Editor\Unity.com" -batchmode -runTests -projectPath "D:\Unity\Projects\LUMEN LOOM" -testPlatform PlayMode -testResults "D:\Unity\Projects\LUMEN LOOM\Logs\PlayModeResults.xml" -logFile "D:\Unity\Projects\LUMEN LOOM\Logs\PlayModeTestRun.log"

# EditMode Unit Tests (3 tests)
& "D:\Unity\Editors\6000.0.32f1\Editor\Unity.com" -batchmode -runTests -projectPath "D:\Unity\Projects\LUMEN LOOM" -testPlatform EditMode -testResults "D:\Unity\Projects\LUMEN LOOM\Logs\EditModeResults.xml" -logFile "D:\Unity\Projects\LUMEN LOOM\Logs\TestRun.log"

# Standalone C# Math Suite (7 algorithmic tests)
& "scratch/LumenLoomCoreTests.exe"
```
