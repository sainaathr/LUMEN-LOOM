# LUMEN LOOM — Next Steps & Day 3 Roadmap

This document outlines the exact, prioritized sprint tasks for **Day 3** as development continues on the dedicated VR workstation (NVIDIA RTX 3050 Laptop GPU / Quest Link).

---

## 1. Priority Sprint Matrix (Day 3)

| Priority | Task Code | Area | Description | Target Environment |
| :--- | :--- | :--- | :--- | :--- |
| **P0** | `XR-01` | Meta XR SDK | Import Meta XR Core and Interaction SDK packages | Dedicated VR Rig (RTX 3050) |
| **P0** | `XR-02` | Hand Tracking | Wire `HandInputProvider.cs` to Meta `HandGrabInteractable` | Meta XR Simulator / Quest 3 |
| **P1** | `SAV-01`| Persistence | Persist `unlockedLevelIndex` via PlayerPrefs across sessions | Unity Editor / Runtime |
| **P2** | `LVL-02`| Chamber 04 | Design Chamber 04 introducing multi-reflecting mirrors and prisms | ScriptableObject LevelData |
| **P3** | `AUD-01`| Spatial SFX | Integrate Meta XR Audio Spatializer for 3D localized beam hums | Quest Audio Plugin |
| **P4** | `QST-01`| Build Validation | Standalone Android APK build deployment to Quest 3 | Meta Quest 3 Hardware |

---

## 2. Exact Files to Modify for Day 3 Tasks

### Task P0: Meta XR Pipeline & Hand Tracking
- **`Packages/manifest.json`**:
  Add Meta XR dependencies on the VR rig:
  ```json
  "com.meta.xr.sdk.core": "69.0.0",
  "com.meta.xr.sdk.interaction": "69.0.0",
  "com.meta.xr.sdk.interaction.ovr": "69.0.0"
  ```
- **`Assets/Scripts/Interaction/HandInputProvider.cs`**:
  Connect the existing event callbacks:
  - `HandleMetaHandGrabStart(PuzzleNode node)`
  - `HandleMetaHandMove(Vector3 handWorldPosition)`
  - `HandleMetaHandRotate(float angleDeltaDegrees)`
  - `HandleMetaHandGrabEnd()`
- **`Assets/Scenes/MainPuzzleScene.unity`**:
  Enable VR input provider when XR headset is detected.

### Task P1: Save System Persistence
- **`Assets/Scripts/Puzzle/LevelManager.cs`**:
  Save `unlockedLevelIndex` to `PlayerPrefs.SetInt("LumenLoom_UnlockedLevel", unlockedLevelIndex)` on solve, and restore on `Awake()`.

### Task P2: Chamber 04 Authoring
- **`Assets/Scripts/Puzzle/LevelManager.cs`**:
  Add Chamber 04 layout combining both specular reflection mirrors, obstacles, and prism splitters.

---

## 3. Architecture Protection Rules

1. **Do not modify the reflection/redirection math** in `BeamSystem.cs` or `PuzzleNode.cs`.
2. **Do not remove `Physics.SyncTransforms()`** from `BeamSystem.RecalculateBeam()`.
3. **Preserve `IInputProvider` contract**: Any VR interaction must fire `IInputProvider` events rather than directly mutating node transforms.
4. **Maintain automated test suite**: Any new mechanic must be accompanied by an automated PlayMode unit test in `LumenLoom.Tests`.
