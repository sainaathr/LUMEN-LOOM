# LUMEN LOOM

**A spatial 3D light-routing puzzle game designed for Meta Quest VR & natural hand interactions.**  
*Developed for the Meta VR Start Developer Competition 2026.*

---

## 🌟 Game Concept

In **LUMEN LOOM**, you step into a mysterious futuristic chamber surrounded by dark obsidian pedestals and humming energy conduits. 

By physically reaching out, grabbing, rotating, and aligning glowing optical energy nodes, you redirect focused beams of collimated light from an energy source to awaiting receivers. As the beams weave through complex chambers, you must navigate obstacles, split light via optical prisms, avoid destructive energy loops, and solve intricate spatial puzzles.

---

## 🕹️ Controls (Desktop Prototype Mode)

| Input Action | Control |
|---|---|
| **Select Node** | Left Mouse Click on node |
| **Move Node** | Hold Left Mouse Button & drag across chamber floor |
| **Rotate Node** | **Q** (Counter-Clockwise) / **E** (Clockwise) or **Mouse Scroll Wheel** |
| **Orbit Camera** | Hold Right Mouse Button & move mouse |
| **Deselect** | **Escape** key or click empty space |
| **Reset Chamber** | **R** key or on-screen **RESET** button |
| **Next / Prev Chamber** | Click on-screen **NEXT >>** / **<< PREV** buttons (unlocked on solve) |
| **Toggle Tutorial Guide** | Click **GUIDE / HELP [?]** at bottom right |

---

## 🔮 Playable Chambers

1. **Chamber 01: Divergence**
   - Foundation puzzle introducing collimated light emission, node translation, and directional alignment to the primary receiver.
2. **Chamber 02: Occlusion Barrier**
   - An energy-absorbing obsidian barrier occupies the central line of sight. Direct beams are blocked and flash amber/red; players must route a multi-node detour around the perimeter.
3. **Chamber 03: Prism Bifurcation**
   - Optical prism mechanic: incoming light bifurcates into a primary forward branch and an orthogonal secondary branch. Both branches must be guided to simultaneously energize dual targets.

---

## 🚀 Quick Start Guide

1. **Unity Version:** Unity 6 LTS (`6000.0.32f1`).
2. **Open Project:** In Unity Hub, click **Add** > select this folder (`LUMEN LOOM`).
3. **Open Scene:** Open `Assets/Scenes/MainPuzzleScene.unity`.
4. **Play:** Press the Unity **Play** button!
   - Follow the step-by-step holographic guide.
   - Route the light from the Energy Source to energize target receivers.
   - Solve Chamber 01 to unlock Chamber 02, and Chamber 02 to unlock Chamber 03!

---

## 📁 Project Structure

- `Assets/Scripts/Core/`: Game state manager, camera controller, and automatic scene orchestrator.
- `Assets/Scripts/Puzzle/`: Energy source, interactive nodes, target receivers, obstacles, and chamber manager.
- `Assets/Scripts/Interaction/`: Decoupled input architecture (`IInputProvider`, `MouseInputProvider`, `HandInputProvider`, `NodeInteractionController`).
- `Assets/Scripts/Rendering/`: Deterministic multi-branch ray-marching beam system, pulsing line renderers, and validation logic.
- `Assets/Scripts/Audio/`: Procedural audio synthesis system generating real-time sound cues (clicks, hums, chords, buzzes) without external asset files.
- `Assets/Scripts/UI/`: In-game sci-fi HUD with automatic OnGUI fallback, level progression buttons, and holographic tutorial card.
- `Documentation/`: Complete developer reports, architecture specifications, handoffs, and verification suites.

---

## 🧪 Testing

The algorithmic raycaster, reflection math, boundary clamping, loop detection, obstacle blocking, and prism splitting are verified by comprehensive test suites:
- Run `.\scratch\LumenLoomCoreTests.exe` to execute standalone core tests: **11 / 11 Passed**.
- Run Unity PlayMode test suite: **18 / 18 Passed**.
- Total Automated Test Coverage: **29 / 29 Tests Passing (100%)**.
