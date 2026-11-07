# LUMEN LOOM

**A spatial 3D light-routing puzzle game designed for Meta Quest VR & natural hand interactions.**  
*Developed for the Meta VR Start Developer Competition 2026.*

---

## 🌟 Game Concept

In **LUMEN LOOM**, you step into a mysterious futuristic chamber surrounded by dark obsidian pedestals and humming energy conduits. 

By physically reaching out, grabbing, rotating, and aligning glowing optical energy nodes, you redirect focused beams of collimated light from an energy source to an awaiting receiver. As the beams weave through complex chambers, you must navigate obstacles, avoid destructive energy loops, and solve intricate spatial puzzles.

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

---

## 🚀 Quick Start Guide

1. **Unity Version:** Unity 6 LTS (`6000.0.x`).
2. **Open Project:** In Unity Hub, click **Add** > select this folder (`LUMEN LOOM`).
3. **Open Scene:** Open `Assets/Scenes/MainPuzzleScene.unity`.
4. **Play:** Press the Unity **Play** button!
   - Grab the interactive nodes.
   - Rotate them using `Q` and `E` to route the light from the Energy Source to the Target Receiver.
   - When aligned, the target energizes and displays **"TARGET CONNECTED"**!

---

## 📁 Project Structure

- `Assets/Scripts/Core/`: Game state manager, camera controller, and automatic scene initializer.
- `Assets/Scripts/Puzzle/`: Energy source, interactive nodes, target receiver, obstacles, and chamber manager.
- `Assets/Scripts/Interaction/`: Decoupled input architecture (`IInputProvider`, `MouseInputProvider`, `HandInputProvider`, `NodeInteractionController`).
- `Assets/Scripts/Rendering/`: Deterministic ray-marching beam system, pulsing line renderer, and validation logic.
- `Assets/Scripts/Audio/`: Procedural audio synthesis system generating real-time sound cues without external asset files.
- `Assets/Scripts/UI/`: In-game sci-fi HUD with automatic OnGUI fallback.
- `Documentation/`: Complete developer handoff guides, architecture specifications, and next sprint tasks.

---

## 🤝 Hand-Tracking & VR Integration

The core gameplay is completely decoupled from hardware APIs:
- The desktop prototype runs smoothly on standard laptops (including Intel Iris Xe integrated graphics).
- Teammate instructions for importing the **Meta XR SDK**, running the **Meta XR Simulator**, and building to **Meta Quest 3** are located in [Documentation/HANDOFF.md](file:///c:/Users/Administrator/Desktop/Projects/LUMEN LOOM/Documentation/HANDOFF.md).
- Detailed technical design and math specifications can be found in [Documentation/TECHNICAL_ARCHITECTURE.md](file:///c:/Users/Administrator/Desktop/Projects/LUMEN LOOM/Documentation/TECHNICAL_ARCHITECTURE.md).

---

## 🧪 Testing

The algorithmic raycaster, reflection math, boundary clamping, and loop detection are verified by a standalone test suite:
- Run `.\scratch\LumenLoomCoreTests.exe` to execute the core tests.
- Results: **7 / 7 Tests Passing**.
