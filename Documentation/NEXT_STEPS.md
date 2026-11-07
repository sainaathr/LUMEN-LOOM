# LUMEN LOOM — Next Steps & November 8 Roadmap

This document outlines the exact, prioritized sprint tasks for **November 8, 2026** as development continues on the dedicated VR workstation (NVIDIA RTX 3050 Laptop GPU / Quest Link).

---

## 1. Priority Sprint Matrix (November 8)

| Priority | Task Code | Area | Description | Target Environment |
| :--- | :--- | :--- | :--- | :--- |
| **P0** | `XR-01` | Meta XR SDK | Import Meta XR Core and Interaction SDK packages | Dedicated VR Rig (RTX 3050) |
| **P0** | `XR-02` | Hand Tracking | Wire `HandInputProvider.cs` to Meta `HandGrabInteractable` | Meta XR Simulator / Quest 3 |
| **P1** | `ENV-01`| Presentation | Polish Chamber 01 visual framing, materials, and lighting | Unity Editor / URP 17.0.3 |
| **P2** | `LVL-01`| Progression | Author Chambers 02 & 03 (introducing Obstacles and multi-node routing) | ScriptableObject LevelData |
| **P3** | `TUT-01`| Onboarding | In-world holographic tooltips and spatial interaction hints | Canvas / World Space UI |
| **P4** | `AUD-01`| Spatial SFX | Integrate Meta XR Audio Spatializer for 3D localized beam hums | Quest Audio Plugin |

---

## 2. Exact Files to Modify for November 8 Tasks

### Task P0: Meta XR Pipeline & Hand Tracking
- **`Packages/manifest.json`**:
  Add Meta XR dependencies:
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
  Replace `[Input_Provider_Mouse]` with `[Input_Provider_Hand]` (or let `NodeInteractionController` toggle between them based on XR device presence).

### Task P1: Chamber 01 Presentation
- **`Assets/Materials/Mat_SciFi_DarkFloor.mat`**: Add subtle normal maps or grid textures for sci-fi tiling.
- **`Assets/Materials/Mat_Beam_Laser.mat`**: Tune emission and core thickness for VR eye-comfort.
- **`Assets/DefaultVolumeProfile.asset`**: Enable lightweight ACES tonemapping and low-radius bloom.

### Task P2: Additional Puzzle Chambers
- **`Assets/Scripts/Data/LevelData.cs`**: Instantiate new ScriptableObjects:
  - `Assets/Resources/Levels/Level_02_Obstacles.asset` (introducing blocking barriers).
  - `Assets/Resources/Levels/Level_03_Prism_Split.asset` (bifurcating beams).
- **`Assets/Scripts/Puzzle/LevelManager.cs`**: Verify automated level loading and scene re-anchoring upon chamber solve.

---

## 3. Architecture Protection Rules

1. **Do not modify the reflection/redirection math** in `BeamSystem.cs` or `PuzzleNode.cs`.
2. **Do not remove `Physics.SyncTransforms()`** from `BeamSystem.RecalculateBeam()`.
3. **Preserve `IInputProvider` contract**: Any VR interaction must fire `IInputProvider` events rather than directly mutating node transforms.
4. **Maintain automated test suite**: Any new mechanic must be accompanied by an EditMode/PlayMode unit test in `LumenLoom.Tests`.
