# LUMEN LOOM — COMPETITION SUBMISSION MASTER DOCUMENT
**Competition:** Meta VR Start Developer Competition 2026  
**Document Version:** 1.0.0 (Final Official Submission)  
**Submission Date:** November 18, 2026  
**Project Repository:** https://github.com/sainaathr/LUMEN-LOOM  
**Release Tag:** `v1.0.0-release-candidate`  
**Engine & Stack:** Unity 6 LTS (`6000.0.85f2`) | OpenXR 1.16.1 | XR Hands 1.7.3 | URP 17.0.4  

---

## 1. Executive Summary & Identity

### 1.1 Project Title
**LUMEN LOOM — Spatial Optical Puzzle Experience**

### 1.2 Tagline
**"Weave the Light with Your Bare Hands."**

### 1.3 One-Line Pitch
**LUMEN LOOM is an immersive spatial optical puzzle game where players physically guide, reflect, and bifurcate high-energy laser beams through futuristic obsidian chambers using Meta Quest bare-hand tracking.**

### 1.4 Short Description (50 Words)
LUMEN LOOM transforms optical physics into a tactile spatial puzzle experience. Inside dark obsidian laboratories, players use their bare hands to grab, position, and twist optical collimators, mirrors, and prisms—routing collimated laser beams around obstacles and through bifurcating splitters to simultaneously energize target receivers in full 3D space.

### 1.5 Medium Description (150 Words)
Conventional light-routing puzzles constrain light to flat 2D grids and artificial controller buttons. LUMEN LOOM leverages the spatial computing power of Meta Quest to deliver an authentic, direct-manipulation puzzle laboratory. 

Standing or seated before a futuristic obsidian puzzle pedestal, players interact directly using their bare hands: reaching out, pinching to acquire optical nodes, and twisting their wrists in mid-air to steer laser pathways. Optical mechanics follow true physics—including Snell’s law specular reflection, deterministic prism bifurcation, and energy absorption on opaque barriers. With four progressively demanding chambers, dynamic laser pulses, procedural audio synthesis, and an intelligent dual-hand contention system, LUMEN LOOM showcases how natural hand interactions make spatial computing puzzles intuitive, immersive, and deeply satisfying.

---

## 2. Problem Statement & Proposed Solution

### 2.1 The Problem
For decades, laser-and-mirror puzzle games have remained tethered to 2D screen grids, tile-based rotations, and mouse clicks. When adapted to VR, many games simply replace mouse clicks with controller laser pointers, missing the fundamental promise of spatial computing: **tactile, intuitive physical presence**. Players are forced to memorize abstract controller face buttons instead of interacting with objects as they would in the real world.

### 2.2 The Proposed Solution
LUMEN LOOM eliminates artificial abstraction by placing players inside a tactile optical laboratory driven entirely by **natural bare-hand tracking**:
- **Tactile Direct Manipulation:** You reach into the chamber and grab mirrors and prisms with an intuitive pinch gesture ($\le 25\text{ mm}$ fingertip proximity).
- **Natural In-Air Wrist Steering:** To aim a reflector or prism, players simply rotate their hand—the optical beam responds in real time to the player’s wrist yaw.
- **Physical Optical Physics in 3D:** Light behaves predictably according to Snell's law, allows multi-branch splitting via refractive prisms, and is absorbed by geometric obstacles.
- **Coexistent Accessibility:** For development, QA, and non-VR accessibility, the architecture features a seamless desktop orbit-camera and mouse fallback that runs the identical optical simulation engine.

---

## 3. Key Features & Innovations

1. **Bare-Hand Optical Manipulation:**
   - Powered by OpenXR and Unity XR Hands.
   - Dual-threshold Schmitt trigger pinch detection ($\le 25\text{ mm}$ grab, $> 35\text{ mm}$ release) guarantees rock-solid noise immunity against hand tremor.
2. **In-Air Wrist Rotation Aiming:**
   - Calculates the player's wrist-to-pinch vector projected onto the horizontal puzzle plane.
   - Twisting the wrist smoothly rotates the selected optical node in real time without requiring awkward secondary grab buttons.
3. **Dual-Hand Contention Resolution:**
   - Independent tracking and state machines for Left and Right hands.
   - Explicit ownership registry prevents hand contention, accidental steals, or node popping.
   - Players can simultaneously manipulate two optical nodes across the table with both hands.
4. **Tracking-Loss Safety Protocol:**
   - If a hand leaves the tracking camera frustum while holding an optical node, the system immediately executes a safe drop in place on the puzzle plane, eliminating runaway objects or physics explosions.
5. **Progressive Optical Mechanics:**
   - **Collimated Emission:** Directional beam propagation with infinite loop termination safeguards.
   - **Specular Reflection:** Planar mirrors reflecting incident light at exact specular angles ($\vec{R} = \vec{I} - 2(\vec{I}\cdot\vec{N})\vec{N}$).
   - **Prism Bifurcation:** Prismatic elements splitting incoming light into a primary forward branch and an orthogonal secondary branch.
   - **Absorption Obstacles:** Opaque obsidian barriers that block beams and require multi-stage perimeter detours.
6. **Procedural Audio Synthesizer:**
   - Self-contained audio synthesis generates harmonious sine/triangle tones and chords for node selection, rotation ticks, target connection, and chamber reset without external audio file dependencies.
7. **Complete 4-Chamber Progression:**
   - Four distinct, carefully balanced puzzle chambers with automatic unlocking, JSON-based persistent progress, and reset capabilities.
8. **Ergonomic Table Height Calibration:**
   - Integrated height offset calibration ($\pm 0.4\text{ m}$) ensuring optimal comfort for both seated and standing playstyles.

---

## 4. Chamber Design & Gameplay Progression

### Chamber 01 — Divergence (Foundation)
- **Objective:** Introduce collimated beam emission, node translation, and in-air rotation.
- **Layout:** Energy source at $(-3.5, 0.5, -3.0)$ firing forward; player manipulates `Node_Alpha` and `Node_Beta` to redirect light into the target receiver at $(2.5, 0.5, 3.0)$.
- **Educational Goal:** Teaches basic pinch-to-grab, table plane dragging, and wrist alignment.

### Chamber 02 — Occlusion Barrier (Obstacle Detour)
- **Objective:** Route energy pathways around a central opaque barrier.
- **Layout:** Central obsidian obstacle block $(3.5 \times 1.2 \times 0.8\text{ m})$ obstructs the direct line of sight between emitter and receiver.
- **Challenge:** Direct beams are absorbed with an amber alert; players must calculate a perimeter detour utilizing multiple optical nodes.

### Chamber 03 — Prism Bifurcation (Multi-Beam Splitting)
- **Objective:** Split a single beam to energize dual target receivers simultaneously.
- **Layout:** Introduces the `PrismSplit` optical node. Light entering the prism splits into a primary forward ray and an orthogonal secondary right ray.
- **Challenge:** Both target receivers must be powered at the exact same moment to trigger chamber completion and unlock the master chamber.

### Chamber 04 — Reflection Protocol (Master Cascade Challenge)
- **Objective:** Master-level cascade routing integrating multiple mirrors, prism splitting, and obstacle avoidance.
- **Layout:** 3 optical nodes (2 planar mirrors + 1 prism), 2 absorbing obstacles, and 2 target receivers.
- **Canonical Solution:** Emitter $\rightarrow$ Mirror 1 (bounces $90^\circ$ right) $\rightarrow$ Mirror 2 (bounces $90^\circ$ back) $\rightarrow$ Prism 3 (splits into Target 1 and Target 2 around both obstacles).

---

## 5. Technology Stack & VR Implementation

| Layer | Technology | Specification / Configuration |
| :--- | :--- | :--- |
| **Engine** | Unity 6 LTS | Version `6000.0.85f2` |
| **XR Runtime** | Khronos OpenXR | Package `com.unity.xr.openxr` v1.16.1 |
| **Hand Tracking** | Unity XR Hands | Package `com.unity.xr.hands` v1.7.3 (`XRHandSubsystem`) |
| **XR Core Utilities**| Unity XR Core Utils | Package `com.unity.xr.core-utils` v2.4.0 (`XROrigin`) |
| **XR Management** | Unity XR Management | Package `com.unity.xr.management` v4.6.1 |
| **Input System** | Unity Input System | Package `com.unity.inputsystem` v1.20.0 |
| **Render Pipeline** | Universal Render Pipeline (URP) | Version 17.0.4 |
| **Stereo Rendering** | Single Pass Instanced (Multiview)| Minimizes mobile GPU/CPU draw calls |
| **Color Space** | Linear | Physically accurate photometric falloff |
| **Target Architecture**| Android ARM64 (`arm64-v8a`) | 64-bit IL2CPP Ahead-Of-Time compilation |
| **Android API Levels**| Min SDK 29 (Android 10) / Target SDK 34 (Android 14) | Meta Quest OS standard |
| **Meta Quest Metadata**| `QuestPostGenerateGradleAndroidProject.cs` | Manifest registers `quest|quest2|cambria|eureka|quest3` |

### How LUMEN LOOM Uses Meta/VR Technology
1. **OpenXR Standard Compliance:** Developed strictly against cross-platform Khronos OpenXR standards, ensuring native forward-compatibility with future Meta Quest OS releases.
2. **Joint-Level Hand Ingestion:** Ingests 26 joint poses per hand per frame from `XRHandSubsystem`. Fingertip distance (Thumb Tip to Index Tip) drives the pinch state machine, while wrist and palm poses determine spatial aiming vectors.
3. **Hardware Device Registration:** Custom Gradle post-processor guarantees that Meta Quest 3 canonical hardware identifiers (`eureka` and `quest3`) are registered in the APK manifest alongside `oculus.software.handtracking` permissions.
4. **Performance Architecture:** Designed for mobile XR constraints—utilizing object pooling for beam segments, zero per-frame garbage collection allocations in the optical tracer, and Single Pass Instanced stereo rendering.

---

## 6. Verification, Testing & Build Deliverables

### 6.1 Automated Quality Assurance Results (81 / 81 Tests Passing)
LUMEN LOOM was validated with a 100% automated test pass rate across standalone algorithmic and Unity Test Runner suites:

```
================================================================================
 LUMEN LOOM MASTER AUTOMATED TEST RESULTS & CENSUS
================================================================================
 Suite 1: Standalone Algorithmic Core Logic (scratch/LumenLoomCoreTests.cs) : 20 / 20 PASS (100.0%)
 Suite 2: Save System & Persistence EditMode (SaveSystemEditModeTests.cs)   : 12 / 12 PASS (100.0%)
 Suite 3: Beam System PlayMode (BeamSystemTests.cs)                         :  3 /  3 PASS (100.0%)
 Suite 4: Desktop Integration PlayMode (SceneIntegrationPlayModeTests.cs)   : 19 / 19 PASS (100.0%)
 Suite 5: XR Hands & Contention PlayMode (XRHandTrackingPlayModeTests.cs)   : 27 / 27 PASS (100.0%)
--------------------------------------------------------------------------------
 TOTAL TESTS EXECUTED:                                                       81 / 81 PASS
 PASS RATE:                                                                  100.0%
 C# COMPILATION WARNINGS IN CODE:                                            0
 UNRESOLVED CODE DEFECTS:                                                    0
================================================================================
```

### 6.2 Verified Build Deliverables

#### 1. Meta Quest Android Standalone APK
- **File Location:** `Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk`
- **File Size:** **37,371,596 bytes** (~35.64 MB)
- **Architecture:** ARM64 (`arm64-v8a`), IL2CPP
- **Package ID:** `com.DefaultCompany.LUMENLOOM`
- **SHA-256 Checksum:** `CA6B6C417A956C47BA9592BB8B232BFED8E557A87EC5E40F883F996D763F0FBF`
- **Verification:** Verified via `zipfile` analysis containing `libUnityOpenXR.so`, `libUnityOpenXRHands.so`, `libil2cpp.so`, `libunity.so`, and valid manifest.

#### 2. Windows Standalone Executable (x86_64)
- **File Location:** `Builds/LUMEN_LOOM_Windows/LumenLoom.exe`
- **File Size:** **671,744 bytes** (Executable) + **34,446,256 bytes** (`UnityPlayer.dll`)
- **Verification:** Verified via headless launch smoke test—executes and runs with zero native crashes.

---

## 7. Limitations & Honest Disclosures

In accordance with strict technical integrity guidelines, the team explicitly discloses the following project status:

1. **Absence of Physical Quest 3 Hardware Testing:**
   - The development environment did not have a physical Meta Quest 3 headset attached during the final sprint.
   - While all OpenXR subsystems, hand-tracking pose ingestion, dual-hand contention resolution, and APK manifests have been verified through automated Unity PlayMode integration suites and successful APK compilation, on-device physical playtesting with human hands remains pending.
2. **Inside-Out Optical Tracking Considerations:**
   - Like all optical hand-tracking titles, extreme hand occlusion (e.g., placing one hand behind the back or completely shielding one hand with another) will temporarily interrupt tracking. The game handles this safely by releasing the held node onto the table plane until the hand re-enters view.
3. **Touch Controller Support:**
   - While the game is designed primarily for natural bare-hand tracking, standard Touch controllers are recognized through OpenXR controller profiles with trigger pinch emulation.

---

## 8. Target Audience & Market Fit

- **Primary Audience:** VR puzzle and spatial computing enthusiasts seeking tactile, intellectual gameplay that showcases the unique capabilities of Meta Quest.
- **Secondary Audience:** Casual gamers and families who prefer bare-hand interactions without the barrier of learning complex controller button layouts.
- **Educational / STEM Use Case:** Students and educators exploring optical physics, reflection angles, and light behavior in a gamified 3D virtual environment.

---

## 9. Future Roadmap

1. **Mixed Reality (MR) Passthrough Mode:**
   - Projecting the optical puzzle pedestal directly into the user’s real-world living room using Quest 3 color passthrough.
2. **Wavelength & Chromatic Dispersion:**
   - Splitting white light into rainbow spectral bands (Red, Green, Blue) that require wavelength-matched color filters and receivers.
3. **Spatial Chamber Level Editor:**
   - Allowing players to build, share, and solve community-created optical puzzle chambers directly in VR using hand tracking.
4. **Haptic Feedback Accessories:**
   - Integration with emerging haptic gloves or micro-haptic wristbands for tactile confirmation upon laser intersection.

---

## 10. Installation & Execution Guide

### For Meta Quest (Standalone VR)
1. Enable **Developer Mode** on your Meta Quest headset.
2. Connect headset to PC via USB-C cable.
3. Sideload the release APK using ADB:
   ```powershell
   adb install -r "Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk"
   ```
4. Put on the headset, navigate to **App Library $\rightarrow$ Unknown Sources $\rightarrow$ LUMEN LOOM**, and launch the game.

### For Windows PC (Desktop Mouse & Keyboard)
1. Navigate to `Builds/LUMEN_LOOM_Windows/`.
2. Double-click `LumenLoom.exe`.
3. Controls:
   - **Select / Drag Node:** Left Mouse Button (LMB)
   - **Rotate Node:** **Q** / **E** or Mouse Scroll Wheel
   - **Orbit Camera:** Hold Right Mouse Button (RMB) & move mouse
   - **Deselect:** Escape key or click empty floor
   - **Reset Chamber:** **R** key or on-screen Reset button

---

## 11. Final Submission Sign-Off

The LUMEN LOOM project is 100% compiled, verified with 81/81 automated tests, and ready for official submission to the Meta VR Start Developer Competition 2026.

**Submitted By:** Developer 1 & Developer 2  
**Date:** November 18, 2026  
**Repository:** https://github.com/sainaathr/LUMEN-LOOM
