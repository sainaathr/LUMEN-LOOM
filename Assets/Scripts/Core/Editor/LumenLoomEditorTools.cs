#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LumenLoom.Puzzle;
using LumenLoom.Rendering;
using LumenLoom.Interaction;
using LumenLoom.Audio;
using LumenLoom.UI;

namespace LumenLoom.Core.Editor
{
    public static class LumenLoomEditorTools
    {
        [MenuItem("Tools/LUMEN LOOM/Rebuild Playable Chamber Scene", false, 1)]
        public static void RebuildPlayableChamberScene()
        {
            if (EditorUtility.DisplayDialog("Rebuild LUMEN LOOM Scene",
                "This will configure the active scene with Energy Source, Nodes, Target, Beam System, and Camera. Continue?",
                "Rebuild", "Cancel"))
            {
                SceneInitializer initializer = Object.FindFirstObjectByType<SceneInitializer>();
                if (initializer == null)
                {
                    GameObject initObj = new GameObject("[Scene_Initializer]");
                    initializer = initObj.AddComponent<SceneInitializer>();
                }

                initializer.EnsureChamberBuilt();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[LUMEN LOOM] Scene successfully built with all systems wired!");
            }
        }

        [MenuItem("Tools/LUMEN LOOM/Validate Active Setup", false, 2)]
        public static void ValidateSetup()
        {
            EnergySource source = Object.FindFirstObjectByType<EnergySource>();
            TargetReceiver target = Object.FindFirstObjectByType<TargetReceiver>();
            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            BeamSystem beam = Object.FindFirstObjectByType<BeamSystem>();

            bool ok = true;
            if (source == null) { Debug.LogError("[LUMEN LOOM Validation] EnergySource missing!"); ok = false; }
            if (target == null) { Debug.LogError("[LUMEN LOOM Validation] TargetReceiver missing!"); ok = false; }
            if (nodes == null || nodes.Length < 2) { Debug.LogError("[LUMEN LOOM Validation] At least 2 PuzzleNodes required!"); ok = false; }
            if (beam == null) { Debug.LogError("[LUMEN LOOM Validation] BeamSystem missing!"); ok = false; }

            if (ok)
            {
                Debug.Log($"[LUMEN LOOM Validation] PASS: 1 Source, 1 Target, {nodes.Length} Interactive Nodes, BeamSystem active!");
            }
        }
    }
}
#endif
