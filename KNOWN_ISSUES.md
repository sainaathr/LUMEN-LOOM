# LUMEN LOOM — KNOWN ISSUES & OPERATIONAL CONSIDERATIONS
**Document Version:** 1.0.0  
**Release Candidate:** v1.0.0-RC1  
**Date:** December 1, 2026  
**Auditor / QA Lead:** Developer 2 (Meta XR Specialist)  

---

## 1. Defect Status Overview

- **Critical (P0) Issues:** **0**
- **High Priority (P1) Issues:** **0**
- **Medium Priority (P2) Issues:** **0**
- **Low / Informational Considerations:** **4**

All critical bugs and interaction blockers identified during development and hardware validation (including in-air node rotation, proximity hover glow, pinch audio, and table height calibration) were fully remediated during Phase 2. The operational considerations listed below represent expected physical constraints of inside-out optical tracking hardware rather than software defects.

---

## 2. Hardware & Tracking Environmental Considerations

### 2.1 Hand Tracking Field-of-View (FOV) Occlusion
- **Description:** Meta Quest 3 uses wide-angle grayscale SLAM cameras to track bare hands. If a player places their hand completely behind their back, below their hips, or physically covers one hand with the other, tracking is temporarily lost.
- **Handling in LUMEN LOOM:** The `HandInputProvider` implements an immediate fail-safe release upon `trackingLost`. If a node is held when tracking drops, it is safely dropped in place on the table plane without sticking or flying into infinity. When the hand re-enters the camera frustum, tracking reacquires cleanly without phantom grabs.
- **Player Recommendation:** Maintain natural hand positions in front of the chest while solving puzzles.

### 2.2 Low-Light Room Conditions
- **Description:** Like all optical inside-out tracked XR headsets, Meta Quest 3 requires adequate ambient lighting ($> 30\text{ lux}$) for its tracking cameras.
- **Handling in LUMEN LOOM:** If headset 6DoF tracking fails due to pitch darkness, Quest OS displays its standard system tracking warning. LUMEN LOOM's camera rig remains stable and resumes immediately upon tracking restoration.
- **Player Recommendation:** Play in a normally illuminated indoor room.

### 2.3 Desktop-to-VR Runtime Switching in Editor
- **Description:** In the Unity Editor, if `MainPuzzleScene.unity` is launched while no XR headset is connected, `XRCameraManager` defaults to Desktop Mode (orbit camera + mouse). If an XR headset is plugged in mid-session, Unity's OpenXR loader may require restarting Play Mode to initialize the native XR runtime.
- **Handling:** On standalone Quest hardware, this is non-applicable as the application always boots natively into full 6DoF VR mode.

### 2.4 Physical Controller Profile
- **Description:** LUMEN LOOM was engineered primarily for bare-hand tracking to deliver maximum tactile spatial immersion.
- **Handling:** Oculus Touch controllers can still be utilized; the system treats controller pinch triggers analogously to bare-hand pinch gestures.

---

## 3. Support & Incident Reporting

For inquiries, operational feedback, or technical assistance, contact the development team via the official repository:
- **Repository Issues:** https://github.com/sainaathr/LUMEN-LOOM/issues
- **Lead Developers:** Developer 1 & Developer 2
