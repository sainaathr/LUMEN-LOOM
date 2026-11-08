# META XR SDK & UNITY 6 COMPATIBILITY REPORT
**Project:** LUMEN LOOM  
**Target Hardware:** Meta Quest 2 / Quest 3 / Quest Pro  
**Development Host Machine:** Intel Iris Xe / 16GB RAM / Windows 11 Pro  
**Dedicated XR Build Machine:** RTX 3050 Laptop / Windows 11  
**Unity Engine Version:** `6000.0.32f1` (Unity 6 LTS)  
**Render Pipeline:** Universal Render Pipeline (URP 17.0.3)  
**Date:** November 8, 2026  

---

## 1. Executive Summary

LUMEN LOOM is designed from inception as a native spatial VR puzzle game targeting Meta Quest headsets via hand tracking and 6DOF interactions.

Due to the GPU limitations of the current development machine (Intel Iris Xe integrated graphics), **Meta XR SDK packages are intentionally not imported into the active working tree on this laptop**. Importing them would risk instability, simulator failure, and shader compilation bottlenecks on non-discrete hardware. 

Instead, the codebase utilizes a strict **decoupled architectural bridge** (`IInputProvider`, `HandInputProvider`, `NodeInteractionController`), enabling 100% logic readiness, PlayMode simulation, and clean integration on the dedicated RTX 3050 XR machine.

---

## 2. Meta XR SDK Version Compatibility Matrix for Unity 6 (6000.0.x)

| Package Name | Recommended Version | Unity 6 LTS Support | Notes |
|---|---|---|---|
| `com.meta.xr.sdk.core` | **v69.0.0** or **v71.0.0** | Full | Required core runtime, display manager, OVRCameraRig, anchor API. |
| `com.meta.xr.sdk.interaction` | **v69.0.0** or **v71.0.0** | Full | Required for RayInteractable, GrabInteractable, HandPoke, synthetic gestures. |
| `com.meta.xr.sdk.interaction.ovr` | **v69.0.0** or **v71.0.0** | Full | Bridges OVRPlugin hand tracking data to Interaction SDK. |
| `com.unity.xr.openxr` | **1.12.1+** | Full | OpenXR runtime backend for Unity 6 XR Plug-in Management. |
| `com.meta.xr.simulator` | **v69.0.0+** | Requires Vulkan / D3D12 dedicated GPU | Do not run on Intel Iris Xe. |

---

## 3. Project Configuration Checklist for Quest Deployment

When migrating the project to the RTX 3050 XR machine:

1. **XR Plug-in Management:**
   - Enable **OpenXR** for Android build target.
   - Under OpenXR feature groups, enable **Meta Quest Support**, **Hand Tracking Subsystem**, and **Meta XR Feature**.
2. **Quality & URP Settings:**
   - Color Space: **Linear**
   - Graphics API: **Vulkan** (Primary), GLES3 (Fallback disabled)
   - Texture Compression: **ASTC**
   - Rendering Path: **Forward+** or **Forward** (Forward recommended for mobile tiling GPU)
   - Target Frame Rate: **72 Hz** (Quest 2 minimum) / **90 Hz** (Quest 3 default)
   - MSAA: **4x MSAA** (essential for clean laser / beam edges in VR)
   - Foveated Rendering: **Quest Fixed Foveated Rendering (FFR) Level 2 / Dynamic**
3. **Android Platform Settings:**
   - Min SDK: **API Level 29 (Android 10.0)**
   - Target SDK: **API Level 34**
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64 only** (disable ARMv7)

---

## 4. Integration Blueprint for Hand Tracking

The LUMEN LOOM game architecture connects to Meta Interaction SDK via:
- `HandInputProvider.cs` implementing `IInputProvider`.
- `HandGrabInteractable` attached to each `PuzzleNode` cylinder prefab.
- Position updates routed through `PuzzleNode.MoveToClamped()`.
- Rotation updates routed through wrist twist delta.
- Tracking loss gracefully handled via `HandleMetaTrackingLost()`.

---

## 5. Verification Verdict

- **Architecture:** `VERIFIED` (Zero compilation coupling to proprietary SDK stubs).
- **Standalone Logic:** `VERIFIED` (All 16 core tests pass).
- **Physical Headset Deployment:** `DEFERRED` (Awaiting RTX 3050 machine + Quest 3 device).

