# LUMEN LOOM — Next Steps & Development Roadmap

This document outlines the exact, prioritized tasks for the next development sprints as we transition the desktop prototype into a full Meta Quest VR vertical slice for the **Meta VR Start Developer Competition 2026**.

---

## Task Priority Matrix

| Priority | Task Name | Area | Assignee | Primary Target |
|---|---|---|---|---|
| **P0** | Meta XR Core & Interaction SDK Import | XR Pipeline | Teammate (RTX 3050) | VR Hand Tracking |
| **P0** | Wire `HandInputProvider` to Meta Interaction SDK | Interaction | Teammate & Lead | Hand-Pinch / Grab |
| **P1** | Haptic & Spatial Audio Refinement | Audio / Feedback | Laptop Dev | Quest Audio Spatializer |
| **P1** | Additional Puzzle Chambers (Chambers 02 & 03) | Level Design | Both | LevelData Assets |
| **P2** | Optical Prisms & Color Splitting | Mechanics | Lead Architect | RGB Beam Mechanics |
| **P2** | Sci-Fi Chamber Environmental Modeling | Visual Polish | Teammate / 3D Artist | Low-Poly Sci-Fi Assets |

---

## Detailed Task Breakdown

### 1. [P0] Meta XR SDK Import & OpenXR Configuration
- **Objective:** Enable OpenXR and import Meta XR SDK onto the RTX 3050 machine.
- **Files to Modify:**
  - `Packages/manifest.json` (add `com.meta.xr.sdk.core`, `com.meta.xr.sdk.interaction`)
  - `ProjectSettings/ProjectSettings.asset`
- **Expected Output:**
  - Meta Quest feature group active in Project Settings > XR Plug-in Management > OpenXR.
  - Meta XR Simulator running successfully inside the Unity Editor without headset.

### 2. [P0] Hooking Up `HandInputProvider.cs`
- **Objective:** Map physical hand pinch and palm position directly to `IInputProvider` events.
- **Files to Modify:**
  - `Assets/Scripts/Interaction/HandInputProvider.cs`
  - `Assets/Prefabs/PuzzleNode.prefab` (attach Meta `RayInteractable` and `GrabInteractable`)
- **Action Steps:**
  1. Add Meta `OVRCameraRig` or `BuildingBlocks > Hand Tracking` rig to `MainPuzzleScene.unity`.
  2. Implement `HandGrabInteractable` on each `PuzzleNode`.
  3. When hand grab begins, invoke `HandInputProvider.HandleMetaHandGrabStart(node)`.
  4. While held, stream pinch anchor position into `HandleMetaHandMove(handPos)`.
  5. Map wrist tilt / two-finger roll to `HandleMetaHandRotate(deltaAngle)`.
  6. On grab release, call `HandleMetaHandGrabEnd()`.
- **Expected Output:**
  - Player can reach out, pinch a node with their natural hand in Quest / Simulator, drag it across the table, rotate it, and redirect the light beam seamlessly.

### 3. [P1] Spatial Audio & Haptic Feedback
- **Objective:** Add Meta Quest controller / hand haptics and spatial 3D audio reflections.
- **Files to Modify:**
  - `Assets/Scripts/Audio/AudioFeedbackManager.cs`
- **Expected Output:**
  - Resonance hum increases in pitch when beam approaches the receiver axis.
  - Spatialized audio click when snapping into alignment.

### 4. [P1] Level Progression (Chambers 02, 03, 04)
- **Objective:** Create escalating spatial challenges.
- **Files to Create:**
  - `Assets/Resources/Levels/Level_02_Obstacles.asset` (introducing blocking walls)
  - `Assets/Resources/Levels/Level_03_Prism_Split.asset` (bifurcating beams)
- **Expected Output:**
  - When target is energized, a pleasant transition plays, and the chamber dissolves/morphs to Level 02.

### 5. [P2] Lightweight URP Visual Polish for Mobile XR
- **Objective:** Maintain stable 72/90 FPS on Meta Quest 3 while providing futuristic aesthetic.
- **Action Steps:**
  1. Add subtle emissive bloom using URP Volume (optimized single-pass bloom).
  2. Add floating dust motes or ambient energy particles caught in the beam path.
  3. Use vertex-colored or unlit additive shaders for the laser core.
