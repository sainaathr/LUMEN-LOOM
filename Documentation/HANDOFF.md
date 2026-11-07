# LUMEN LOOM — Teammate Handoff & Setup Guide

**Welcome to LUMEN LOOM!**  
This guide will get you from repository clone to testing hand tracking in the Meta XR Simulator and building to a Meta Quest headset.

---

## 1. Prerequisites & Environment

- **Unity Editor:** Unity 6 LTS (`6000.0.32f1` or any `6000.0.x` LTS release).
- **Target OS:** Windows 10/11 with dedicated GPU (NVIDIA RTX 3050+ recommended for Meta XR Simulator).
- **Platform Modules:** Android Build Support (with OpenJDK, Android SDK & NDK tools installed via Unity Hub).
- **Git & Git LFS:** Installed.

---

## 2. Cloning & First Launch

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-team/lumen-loom.git
   cd lumen-loom
   ```
2. **Open Unity Hub:**
   - Click **Add** > **Add project from disk**.
   - Select the `LUMEN LOOM` root folder.
   - Choose Unity **6000.0.32f1** (or your installed Unity 6 LTS version).
3. **Open the Project:**
   - Unity will import assets and resolve packages from `Packages/manifest.json`.
4. **Open the Main Scene:**
   - In Project View, navigate to `Assets/Scenes/MainPuzzleScene.unity` and double-click to open.
5. **Press Play (Desktop Test):**
   - Click the Unity **Play** button.
   - You will see the chamber floor, Energy Source, 2 interactive nodes, and Target Receiver.
   - **Controls:**
     - **Left-Click & Drag:** Grab and move a node across the puzzle plane.
     - **Q / E:** Rotate selected node counter-clockwise / clockwise.
     - **Mouse Scroll Wheel:** Incremental fine-rotation.
     - **R:** Reset puzzle chamber.
     - **Right-Click & Drag:** Orbit/tilt the camera.
   - Align the two nodes so that the light beam from the Energy Source passes through Node 1, reaches Node 2, and strikes the Target Receiver.
   - Observe the target energize, banner change to `"TARGET CONNECTED"`, and celestial chime audio play!

---

## 3. Integrating Meta XR SDK & OpenXR

Follow these exact steps on your RTX 3050 laptop:

### Step 3.1: Install XR Plug-in Management & OpenXR
1. In Unity, go to **Edit** > **Project Settings** > **XR Plug-in Management**.
2. Click **Install XR Plug-in Management** if prompted.
3. Under the **Windows (Standalone)** tab and **Android** tab:
   - Check the **OpenXR** checkbox.
4. Under **XR Plug-in Management** > **OpenXR**:
   - Under **Interaction Profiles**, add **Oculus Touch Controller Profile** and **Meta Hand Interaction Profile**.
   - Under **OpenXR Feature Groups**, enable **Meta Quest Support**.

### Step 3.2: Install Meta XR Packages
1. Open **Window** > **Package Manager**.
2. Click the `+` icon > **Add package by name...**:
   - Add: `com.meta.xr.sdk.core` (v69.0.0 or latest compatible with Unity 6)
   - Add: `com.meta.xr.sdk.interaction`
   - Add: `com.meta.xr.sdk.interaction.ovr`
3. If using Unity Asset Store Meta XR All-in-One SDK, you can also import that package directly.

---

## 4. Setting Up Meta XR Simulator (No Headset Required!)

The Meta XR Simulator allows testing 6DoF head movement and realistic hand-tracking gestures directly on your PC:

1. In the Unity menu, select **Meta** > **XR Simulator** > **Show Simulator Controls**.
2. In the Simulator window, choose **Meta Quest 3** as the device.
3. In Unity Editor, press **Play**.
4. The Simulator window will render the stereo view.
5. In the Simulator toolbar, select **Hands** mode:
   - Use keyboard/mouse controls to simulate hand reach, pinch gestures, and grab interactions.

---

## 5. Wiring Hand Tracking to `HandInputProvider.cs`

Our game architecture was built specifically for this step! The gameplay layer does not care whether input comes from a mouse or a human hand:

1. In `MainPuzzleScene`:
   - Add an `OVRCameraRig` or `BuildingBlocks > Hand Tracking` prefab.
2. In the GameObject `[Input_Provider_Mouse]`:
   - Disable or remove `MouseInputProvider`.
   - Add `HandInputProvider` component.
3. In `[Interaction_Controller]`:
   - Assign the `HandInputProvider` into `Input Provider Component`.
4. Configure Node Interactables:
   - Add `HandGrabInteractable` (from Meta Interaction SDK) to each `PuzzleNode`.
   - On the `HandGrabInteractable` events:
     - `When Selecting`: Call `HandInputProvider.HandleMetaHandGrabStart(thisNode)`
     - `When Moving`: Call `HandInputProvider.HandleMetaHandMove(handAnchorPos)`
     - `When Unselecting`: Call `HandInputProvider.HandleMetaHandGrabEnd()`
5. You can also map wrist rotation to `HandInputProvider.HandleMetaHandRotate(deltaAngle)`.

---

## 6. Building for Meta Quest (Standalone APK)

1. Connect Meta Quest 3/2 to your laptop via USB-C (ensure Developer Mode is enabled in the Meta Horizon mobile app).
2. In Unity, open **File** > **Build Settings**.
3. Select **Android** platform and click **Switch Platform**.
4. Configure Build Settings:
   - **Texture Compression:** ASTC
   - **Target API Level:** Android 12L / 13 (API Level 32+)
5. In **Player Settings**:
   - **Color Space:** Linear
   - **Scripting Backend:** IL2CPP
   - **Target Architectures:** ARM64
   - **Graphics API:** OpenGLES3 or Vulkan
6. Click **Build and Run** to package and install the APK onto your Quest headset!

