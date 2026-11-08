# LUMEN LOOM — COMPETITION SUBMISSION GUIDE
**Event:** Meta VR Start Developer Competition 2026  
**Project:** LUMEN LOOM — Spatial Optical VR Puzzle Game  
**Target Platform:** Meta Quest 2 / Quest 3 / Quest Pro  
**Submission Version:** 0.4.0-preview  
**Engine & Pipeline:** Unity 6 LTS (`6000.0.32f1`), Universal Render Pipeline (URP 17.0.3)  
**Primary Interaction Paradigm:** Direct Optical Hand Tracking + Spatial Grabbing (Meta Interaction SDK ready)  

---

## 1. Project High Concept & Gameplay Pitch

**LUMEN LOOM** is a pure spatial VR optical puzzle experience where the player becomes an artisan of light. Manipulating floating optical nodes, prisms, and mirrors in three dimensions with their bare hands, players bend, split, and reflect coherent laser energy through complex atmospheric test chambers.

### Key Pillars:
1. **Intangible Light, Tangible Interaction:** Direct hand tracking allows natural pinching, translation along spatial planes, and precise wrist twisting to aim optics.
2. **Deep Optical Mechanics:** Specular reflection (Snell's Law), refractive prism splitting (multi-beam routing), and occlusion navigation create escalating spatial challenges.
3. **Cyber-Zen Atmosphere:** Dark obsidian minimalism contrasted against high-luminance coherent cyan/magenta laser beams and responsive spatial audio cues.

---

## 2. Playable Chambers Overview

- **Chamber 01: Divergence**
  - *Concept:* Introduction to spatial optical manipulation.
  - *Mechanic:* Aligning two basic redirector nodes to guide a laser from source to target.
- **Chamber 02: Occlusion Barrier**
  - *Concept:* Navigation around unyielding geometry.
  - *Mechanic:* Overcoming a central light-absorbing obsidian monolith using detour routing.
- **Chamber 03: Prism Bifurcation**
  - *Concept:* Multi-path concurrent energy distribution.
  - *Mechanic:* Using a crystal prism node to split a single laser beam into dual orthogonal vectors, simultaneously energizing two separate receivers.
- **Chamber 04: Reflection Protocol**
  - *Concept:* Multi-surface reflection cascading and splitting.
  - *Mechanic:* Three mirror/prism nodes bouncing energy around complex physical occlusion baffles to target dual receivers at opposing chamber coordinates.

---

## 3. XR Readiness & Architecture Validation

- **Decoupled Input Model:** `IInputProvider` architecture cleanly abstracts desktop simulation (`MouseInputProvider`) and native XR tracking (`HandInputProvider`).
- **Zero Broken Dependencies:** Game compiles cleanly without requiring proprietary XR packages on development machines lacking VR GPUs.
- **Tested Test Suites:**
  - Standalone Algorithmic Suite: **16/16 Passed**
  - Unity EditMode Suite: **12/12 Passed**
  - Unity PlayMode Integration Suite: **18/18 Passed**
  - Total Verified Automated Tests: **46/46 Passed**

---

## 4. Setup & Build Instructions for Competition Judges

### A. Windows Desktop Simulation PlayMode:
1. Open Unity Hub and add the project at `D:\Unity\Projects\LUMEN LOOM` (or workspace folder).
2. Open scene `Assets/Scenes/MainPuzzleScene.unity`.
3. Press **Play**.
4. Left-click to select and drag optical nodes on the puzzle plane.
5. Right-click and drag horizontally or use Q/E keys to rotate nodes.
6. Press **R** to reset puzzle chamber.

### B. Meta Quest APK Build (from XR-Ready machine):
1. In Unity Editor, switch platform to **Android**.
2. In Project Settings > XR Plug-in Management, enable **OpenXR** with **Meta Quest Feature Group**.
3. Import `com.meta.xr.sdk.core` and `com.meta.xr.sdk.interaction` (v69.0+).
4. Run **Build and Run** targeting connected Quest 2/3 via USB-C or Meta Quest Link.
