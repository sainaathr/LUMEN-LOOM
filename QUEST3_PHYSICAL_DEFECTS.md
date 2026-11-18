# LUMEN LOOM — META QUEST 3 PHYSICAL DEFECT INVENTORY
**Document Version:** 1.0.0  
**Phase:** Day 5 Phase 1 Hardware Validation  
**Date:** November 15, 2026  
**Auditor:** Developer 2 (Meta XR Specialist)  
**Target Device:** Meta Quest 3 (Model: Eureka, Android 12L/14, API 34)  
**Runtime:** Unity 6 LTS (`6000.0.32f1`), OpenXR 1.12.1, XR Hands 1.4.1  

---

## 1. Defect Severity Classification

- **CRITICAL (P0):** Prevents puzzle completion or crashes application in VR. Must be resolved immediately.
- **HIGH (P1):** Severe gameplay or interaction impairment affecting user experience.
- **MEDIUM (P2):** Noticeable usability, ergonomic, or visual inconsistency.
- **LOW (P3):** Minor visual, audio, or polish improvement.

---

## 2. Active Defect Registry

### DEF-01: Node Rotation Inoperable During Hand Tracking (P0 - CRITICAL)
- **Component:** `HandInputProvider.cs`, `NodeInteractionController.cs`
- **Checklist Item:** Q3-10, Q3-11
- **Severity:** CRITICAL
- **Description:**  
  While optical nodes can be translated across the chamber floor plane using pinch dragging, nodes cannot be rotated using hand tracking. As a result, players cannot aim redirected beams, rendering Chamber 01 through Chamber 04 unsolvable via pure hand input.
- **Root Cause:**  
  In `HandInputProvider.cs`:
  1. `enableWristTurnRotation` defaults to `false`.
  2. `ProcessHand()` extracts only `ThumbTip` and `IndexTip` positions for pinch calculation, but does not extract `Wrist` or `Palm` rotation.
  3. `HandleMetaHandRotate()` is declared but never called during the dynamic update loop.
- **Reproduction Steps:**
  1. Launch LUMEN LOOM on Quest 3 in hand-tracking mode.
  2. Pinch and hold optical node `Node_Alpha`.
  3. Move hand: node translates correctly.
  4. Rotate wrist/hand clockwise or counter-clockwise: node maintains static orientation ($0^\circ$).
  5. The optical beam does not change direction towards the receiver.
- **Remediation Plan:**
  - Extract `Wrist` and `Palm` joint poses in `ProcessHand()`.
  - Calculate angular delta around the vertical ($Y$) axis relative to the initial pinch orientation.
  - Apply yaw delta to the held node via `OnNodeRotationDelta` / `HandleMetaHandRotate()`.
  - Add deadzone and smoothing to prevent jitter.

---

### DEF-02: Missing Proximity Hover State Prior to Pinch (P1 - HIGH)
- **Component:** `HandInputProvider.cs`, `PuzzleNode.cs`
- **Checklist Item:** Q3-09
- **Severity:** HIGH
- **Description:**  
  When bringing a hand close to an optical node within the selection radius ($0.15\text{ m}$), the node provides no visual indicator that it is within grab range until the user actually engages a pinch. This causes players to "pinch thin air" without knowing if they are close enough.
- **Root Cause:**  
  `FindNearestNode()` is only invoked when `!state.IsPinched && distance <= pinchEngageThreshold`. No proximity evaluation occurs during the open-hand hovering phase.
- **Reproduction Steps:**
  1. Approach a node with open fingers.
  2. Observe node visuals: no hover emission or glow appears.
  3. Player is uncertain whether pinch will grab the node.
- **Remediation Plan:**
  - Evaluate proximity to nodes during open-hand tracking when $0.035\text{ m} < \text{pinchDistance} < 0.15\text{ m}$.
  - Set `_hoveredNode` and trigger `SetHovered(true)` on `PuzzleNode` to render a subtle cyan proximity halo.

---

### DEF-03: Lack of Micro-Audio Feedback on Hand Pinch & Release (P2 - MEDIUM)
- **Component:** `HandInputProvider.cs`, `AudioFeedbackManager.cs`
- **Checklist Item:** Q3-07, Q3-08
- **Severity:** MEDIUM
- **Description:**  
  In controller-free hand tracking, there is no physical motor rumble to confirm grab engagement or release. Without instant auditory confirmation, players experience slight cognitive uncertainty during fast interactions.
- **Root Cause:**  
  Grab and release events in `HandInputProvider` do not trigger micro-haptic audio chimes directly when pinch engages/releases.
- **Reproduction Steps:**
  1. Pinch to grab a node. Notice absence of instant audio confirmation on the exact pinch threshold crossing.
- **Remediation Plan:**
  - Wire instant audio tick (`PlayNodeSelectSound()` and `PlayNodeReleaseSound()`) upon pinch state machine transition in `EvaluatePinchState()`.

---

### DEF-04: Table Height Alignment in Seated vs Standing Boundary (P2 - MEDIUM)
- **Component:** `XRCameraManager.cs`, `SceneInitializer.cs`
- **Checklist Item:** Q3-04
- **Severity:** MEDIUM
- **Description:**  
  In seated Guardian boundaries, the optical chamber floor ($Y = 0.5\text{ m}$) can appear too low or high relative to the physical user's chest level.
- **Root Cause:**  
  XR Origin `CameraFloorOffset` uses floor tracking origin without an interactive table height calibration offset.
- **Remediation Plan:**
  - Introduce table height adjustment offset in `XRCameraManager` allowing players to calibrate puzzle plane height ($\pm 0.3\text{ m}$) via in-game UI.

---

### DEF-05: Laser Beam Specular Shimmer at Glancing Angles (P3 - LOW)
- **Component:** `BeamRenderer.cs`, `Mat_Beam_Laser.mat`
- **Checklist Item:** Q3-11
- **Severity:** LOW
- **Description:**  
  Under high-contrast obsidian lighting, the edge of the laser line renderer exhibits slight temporal subpixel aliasing when viewed from low glancing angles on Quest 3 pancake lenses.
- **Root Cause:**  
  LineRenderer width without soft alpha falloff edge shader.
- **Remediation Plan:**
  - Add subtle edge feathering in beam shader / URP material.

---

## 3. Defect Remediation Tracking

| Defect ID | Title | Priority | Status | Target Phase |
| :---: | :--- | :---: | :---: | :---: |
| **DEF-01** | Node Rotation Inoperable in VR | P0 (Critical) | **Open** | Phase 2 |
| **DEF-02** | Missing Proximity Hover State | P1 (High) | **Open** | Phase 2 |
| **DEF-03** | Missing Pinch Audio Feedback | P2 (Medium) | **Open** | Phase 2 |
| **DEF-04** | Seated/Standing Height Adjustment | P2 (Medium) | **Open** | Phase 2 |
| **DEF-05** | Laser Beam Edge Shimmer | P3 (Low) | **Open** | Phase 4 |
