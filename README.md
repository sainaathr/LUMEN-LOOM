# LUMEN LOOM

**A spatial 3D light-routing puzzle game designed for Meta Quest VR & natural hand interactions.**  
*Developed for the Meta VR Start Developer Competition 2026.*  
*Release Candidate: v1.0.0-RC1 | Engine: Unity 6 LTS (`6000.0.32f1`)*

---

## 🌟 Game Concept

In **LUMEN LOOM**, you step into a mysterious futuristic optical laboratory surrounded by dark obsidian pedestals and humming energy conduits. 

By physically reaching out with your bare hands, grabbing, rotating, and aligning glowing optical nodes, you redirect focused beams of collimated light from an energy source to awaiting receivers. As the beams weave through complex chambers, you must navigate obstacles, split light via optical prisms, cascade specular reflections off mirrors, avoid destructive energy loops, and solve intricate spatial puzzles.

---

## 🔮 Playable Chambers

1. **Chamber 01: Divergence**
   - Foundation puzzle introducing collimated light emission, node translation, and in-air wrist alignment to the primary receiver.
2. **Chamber 02: Occlusion Barrier**
   - An energy-absorbing obsidian barrier occupies the central line of sight. Direct beams are blocked and flash amber/red; players must route a multi-node detour around the perimeter.
3. **Chamber 03: Prism Bifurcation**
   - Optical prism mechanic: incoming light bifurcates into a primary forward branch and an orthogonal secondary branch. Both branches must be guided to simultaneously energize dual targets.
4. **Chamber 04: Reflection Protocol**
   - Advanced master puzzle combining multi-stage specular reflection ($90^\circ$ mirror bounces) and prism bifurcation around two absorbing obstacles to simultaneously energize dual target receivers.

---

## 🎮 Controls Reference

### 🖐️ Meta Quest VR Mode (Natural Hand Tracking)

| Input Action | Physical Hand Gesture | Result in Game |
|---|---|---|
| **Hover Node** | Bring open hand within $0.15\text{ m}$ of node | Target node emits a soft cyan proximity hover glow. |
| **Grab Node** | Close thumb and index fingertips together ($\le 25\text{ mm}$) | Grabs optical node with cyan pulse and audio chime. |
| **Move Node** | Maintain pinch and move hand across table | Translates node smoothly across chamber table plane. |
| **Rotate Node** | Maintain pinch and twist wrist clockwise / counter-clockwise | Steers reflector / prism and dynamically redirects beam. |
| **Release Node** | Open thumb and index fingertips apart ($> 35\text{ mm}$) | Drops node in place with confirmation audio tick. |
| **Dual-Hand Play** | Grab separate nodes with Left and Right hands | Manipulate two optical nodes simultaneously and independently. |
| **Reset Chamber** | Tap the floating holographic **RESET** pedestal in VR | Restores default node positions for current chamber. |

### 🖥️ Desktop Mode (Mouse & Keyboard Fallback)

| Input Action | Control |
|---|---|
| **Select Node** | Left Mouse Click (LMB) on node |
| **Move Node** | Hold LMB & drag across chamber floor |
| **Rotate Node** | **Q** (Counter-Clockwise) / **E** (Clockwise) or **Mouse Scroll Wheel** |
| **Orbit Camera** | Hold Right Mouse Button (RMB) & move mouse |
| **Deselect** | **Escape** key or click empty space |
| **Reset Chamber** | **R** key or on-screen **RESET** button |
| **Next / Prev Chamber** | Click on-screen **NEXT >>** / **<< PREV** buttons (unlocked on solve) |
| **Toggle Tutorial Guide** | Click **GUIDE / HELP [?]** at bottom right |

---

## 🚀 Quick Start Guide

### Sideloading onto Meta Quest 3 (Standalone)
```powershell
# 1. Verify Quest connection over USB-C
adb devices -l

# 2. Sideload Release Candidate APK
adb install -r "Builds/LUMEN_LOOM_Quest/LumenLoom_Quest.apk"

# 3. Launch the game in VR
adb shell am start -n com.DefaultCompany.LUMENLOOM/com.unity3d.player.UnityPlayerActivity
```
*For detailed installation instructions, see [INSTALLATION_GUIDE.md](INSTALLATION_GUIDE.md).*

### Running in Unity 6 Editor
1. **Unity Version:** Unity 6 LTS (`6000.0.32f1`).
2. **Open Project:** In Unity Hub, click **Add** > select this folder (`LUMEN-LOOM`).
3. **Open Scene:** Open `Assets/Scenes/MainPuzzleScene.unity`.
4. **Play:** Press the Unity **Play** button!
   - Connected VR headsets boot directly into full 6DoF stereoscopic VR with hand tracking.
   - Non-VR systems seamlessly fall back to Desktop Orbit Mode with mouse controls.

---

## 📐 Technical Architecture

- `Assets/Scripts/Core/`: Game state manager, `XRCameraManager` dual-rig orchestrator, and `SceneInitializer`.
- `Assets/Scripts/Puzzle/`: Energy source, interactive nodes, target receivers, obstacles, and `LevelManager`.
- `Assets/Scripts/Interaction/`: Decoupled input architecture (`IInputProvider`, `MouseInputProvider`, `HandInputProvider`, `NodeInteractionController`).
- `Assets/Scripts/Rendering/`: Deterministic multi-branch ray-marching beam system (`BeamSystem.cs`), pulsing line renderers, and validation logic.
- `Assets/Scripts/Audio/`: Procedural audio synthesis system (`AudioFeedbackManager.cs`) generating real-time sound cues without external asset files.
- `Assets/Scripts/UI/`: World-Space Holographic HUD and in-game UI with level progression buttons.
- `Assets/Editor/`: Android Quest build pipeline and manifest post-generation processor (`QuestPostGenerateGradleAndroidProject.cs`).

---

## 🧪 Testing & Quality Assurance

LUMEN LOOM maintains an exhaustive automated testing suite with a **100.0% pass rate across 81 tests**:
- **Core Algorithmic Logic Suite:** `20 / 20 Passed` (Standalone via `dotnet run`).
- **Save & Persistence EditMode Suite:** `12 / 12 Passed`.
- **Beam System PlayMode Suite:** `3 / 3 Passed`.
- **Desktop Scene Integration PlayMode Suite:** `19 / 19 Passed`.
- **XR Hands & Dual Contention PlayMode Suite:** `27 / 27 Passed`.
- **Total Automated Test Coverage:** **81 / 81 Tests Passing (100.0%)**.

Run standalone algorithmic tests:
```powershell
dotnet run --project "scratch/LumenLoomCoreTests.csproj"
```

---

## 📄 Release & Competition Documentation

1. [FINAL_RELEASE_REPORT.md](FINAL_RELEASE_REPORT.md) — Master Release Candidate & Multi-Developer Sign-Off.
2. [QUEST3_FINAL_VALIDATION_REPORT.md](QUEST3_FINAL_VALIDATION_REPORT.md) — 18-Point Physical Hardware Validation Report.
3. [FINAL_BUILD_INFORMATION.md](FINAL_BUILD_INFORMATION.md) — APK Metadata, Architecture, and SHA-256 Checksum.
4. [FINAL_TEST_RESULTS.md](FINAL_TEST_RESULTS.md) — Comprehensive 81-Test Catalog & Assertions.
5. [KNOWN_ISSUES.md](KNOWN_ISSUES.md) — Operational Considerations & Zero Critical Defect Audit.
6. [INSTALLATION_GUIDE.md](INSTALLATION_GUIDE.md) — Complete Sideloading and Quick-Start Guide.
7. [COMPETITION_SUBMISSION_CHECKLIST.md](COMPETITION_SUBMISSION_CHECKLIST.md) — Meta VR Start Developer Competition 2026 Compliance.

---

**LUMEN LOOM — The art of guiding light through space.**
