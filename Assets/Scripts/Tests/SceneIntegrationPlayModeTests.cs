using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LumenLoom.Core;
using LumenLoom.Puzzle;
using LumenLoom.Rendering;
using LumenLoom.Interaction;
using LumenLoom.UI;
using LumenLoom.Data;

namespace LumenLoom.Tests
{
    public class SceneIntegrationPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainPuzzleScene");
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test01_InitialScene_EntitiesAndChamberInitialized()
        {
            Assert.IsNotNull(GameManager.Instance, "GameManager instance should be active.");

            PuzzleManager puzzleManager = Object.FindFirstObjectByType<PuzzleManager>();
            Assert.IsNotNull(puzzleManager, "PuzzleManager should be active in scene.");

            EnergySource source = Object.FindFirstObjectByType<EnergySource>();
            Assert.IsNotNull(source, "EnergySource should be present in scene.");

            TargetReceiver target = Object.FindFirstObjectByType<TargetReceiver>();
            Assert.IsNotNull(target, "TargetReceiver should be present in scene.");

            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            Assert.IsTrue(nodes.Length >= 2, "Chamber should contain at least two PuzzleNodes.");

            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            Assert.IsNotNull(beamSystem, "BeamSystem should be present in scene.");

            BeamRenderer beamRenderer = Object.FindFirstObjectByType<BeamRenderer>();
            Assert.IsNotNull(beamRenderer, "BeamRenderer should be present in scene.");

            NodeInteractionController controller = Object.FindFirstObjectByType<NodeInteractionController>();
            Assert.IsNotNull(controller, "NodeInteractionController should be present in scene.");

            GameObject floor = GameObject.Find("Chamber_Floor");
            Assert.IsNotNull(floor, "Chamber floor should exist.");

            GameObject boundary = GameObject.Find("Chamber_Boundary");
            Assert.IsNotNull(boundary, "Chamber boundary rails should exist.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test02_NodeSelection_HighlightsAndSwitchesActiveNode()
        {
            NodeInteractionController controller = Object.FindFirstObjectByType<NodeInteractionController>();
            Assert.IsNotNull(controller);

            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            Assert.IsTrue(nodes.Length >= 2);

            PuzzleNode nodeAlpha = nodes[0];
            PuzzleNode nodeBeta = nodes[1];

            IInputProvider inputProvider = controller.InputProvider;
            Assert.IsNotNull(inputProvider);

            // Select Alpha
            inputProvider.SelectNode(nodeAlpha);
            yield return null;
            Assert.AreEqual(nodeAlpha, controller.ActiveNode, "Node Alpha should be the active node in controller.");

            // Switch to Beta
            inputProvider.SelectNode(nodeBeta);
            yield return null;
            Assert.AreEqual(nodeBeta, controller.ActiveNode, "Controller active node should switch to Node Beta.");

            // Deselect
            inputProvider.DeselectCurrentNode();
            yield return null;
            Assert.IsNull(controller.ActiveNode, "Active node should be cleared after deselect.");
        }

        [UnityTest]
        public IEnumerator Test03_DraggingAndBoundaryClamping_EnforcesLimits()
        {
            NodeInteractionController controller = Object.FindFirstObjectByType<NodeInteractionController>();
            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            PuzzleNode node = nodes[0];

            controller.InputProvider.SelectNode(node);
            yield return null;

            // Attempt to move far beyond the positive boundary (+15 on X and Z)
            node.MoveToClamped(new Vector3(15.0f, 0.5f, 15.0f));
            yield return null;
            Assert.LessOrEqual(node.transform.position.x, 4.5f + 0.001f, "X coordinate must be clamped inside positive boundary.");
            Assert.LessOrEqual(node.transform.position.z, 4.5f + 0.001f, "Z coordinate must be clamped inside positive boundary.");
            Assert.AreEqual(0.5f, node.transform.position.y, 0.001f, "Y height must remain fixed at interaction plane.");

            // Attempt to move far beyond the negative boundary (-15 on X and Z)
            node.MoveToClamped(new Vector3(-15.0f, 0.5f, -15.0f));
            yield return null;
            Assert.GreaterOrEqual(node.transform.position.x, -4.5f - 0.001f, "X coordinate must be clamped inside negative boundary.");
            Assert.GreaterOrEqual(node.transform.position.z, -4.5f - 0.001f, "Z coordinate must be clamped inside negative boundary.");
        }

        [UnityTest]
        public IEnumerator Test04_NodeRotation_UpdatesForwardVectorAndOrientation()
        {
            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            PuzzleNode node = nodes[0];

            node.transform.rotation = Quaternion.Euler(0, 0, 0); // forward is (0, 0, 1)
            yield return null;

            node.RotateNode(90f);
            yield return null;
            Assert.AreEqual(1.0f, node.transform.forward.x, 0.01f, "Rotating 90 deg clockwise should aim forward along +X.");
            Assert.AreEqual(0.0f, node.transform.forward.z, 0.01f, "Rotating 90 deg clockwise should zero out Z forward.");

            node.RotateNode(-90f);
            yield return null;
            Assert.AreEqual(0.0f, node.transform.forward.x, 0.01f, "Rotating back should restore X to 0.");
            Assert.AreEqual(1.0f, node.transform.forward.z, 0.01f, "Rotating back should restore Z forward to 1.");
        }

        [UnityTest]
        public IEnumerator Test05_BeamPropagation_AndLoopDetection_SafelyTerminates()
        {
            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            Assert.IsTrue(nodes.Length >= 2);

            PuzzleNode nodeA = nodes[0];
            PuzzleNode nodeB = nodes[1];

            // Position Node A and Node B facing each other directly to create an optical feedback loop
            nodeA.transform.position = new Vector3(-1.0f, 0.5f, 0.0f);
            nodeA.transform.forward = Vector3.right; // points towards B

            nodeB.transform.position = new Vector3(1.0f, 0.5f, 0.0f);
            nodeB.transform.forward = Vector3.left; // points back at A

            // Aim energy source at Node A
            EnergySource source = Object.FindFirstObjectByType<EnergySource>();
            source.transform.position = new Vector3(-3.0f, 0.5f, 0.0f);
            source.transform.forward = Vector3.right;

            beamSystem.RecalculateBeam();
            yield return null;

            Assert.IsTrue(beamSystem.CurrentPath.HasLoopDetected, "BeamSystem should detect cyclical reflection loop and terminate cleanly.");
            Assert.LessOrEqual(beamSystem.CurrentPath.Points.Count, 16, "Points list must remain bounded without runaway allocation.");
        }

        [UnityTest]
        public IEnumerator Test06_FullPuzzleSolve_TargetEnergizesAndStatusConnects()
        {
            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            BeamValidator validator = Object.FindFirstObjectByType<BeamValidator>();
            TargetReceiver target = Object.FindFirstObjectByType<TargetReceiver>();
            EnergySource source = Object.FindFirstObjectByType<EnergySource>();

            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            Assert.IsTrue(nodes.Length >= 2);

            PuzzleNode nodeAlpha = System.Array.Find(nodes, n => n.name.Contains("Alpha")) ?? nodes[0];
            PuzzleNode nodeBeta = System.Array.Find(nodes, n => n.name.Contains("Beta")) ?? (nodes.Length > 1 ? nodes[1] : nodes[0]);

            // 1. Position Source: (-3.5, 0.5, -3.0), pointing +Z forward
            source.transform.position = new Vector3(-3.5f, 0.5f, -3.0f);
            source.transform.forward = Vector3.forward;

            // 2. Position Node Alpha: (-3.5, 0.5, 1.0), rotated 90 deg Y to shoot +X right
            nodeAlpha.transform.position = new Vector3(-3.5f, 0.5f, 1.0f);
            nodeAlpha.transform.rotation = Quaternion.Euler(0, 90f, 0);

            // 3. Position Node Beta: (2.5, 0.5, 1.0), rotated 0 deg Y to shoot +Z forward
            nodeBeta.transform.position = new Vector3(2.5f, 0.5f, 1.0f);
            nodeBeta.transform.rotation = Quaternion.Euler(0, 0f, 0);

            // 4. Position Target: (2.5, 0.5, 3.0), facing back (0, 0, -1) to receive incoming +Z beam
            target.transform.position = new Vector3(2.5f, 0.5f, 3.0f);
            target.transform.forward = Vector3.back;

            // Recalculate beam and allow frame to update
            beamSystem.RecalculateBeam();
            yield return null;

            Debug.Log($"[Test06 Trace] ReachedTarget={beamSystem.CurrentPath.HasReachedTarget}, HitTarget={(beamSystem.CurrentPath.HitTarget != null ? beamSystem.CurrentPath.HitTarget.name : "null")}, Loop={beamSystem.CurrentPath.HasLoopDetected}, Obstacle={beamSystem.CurrentPath.HitObstacle}, VisitedNodes={beamSystem.CurrentPath.VisitedNodes.Count}, Points={beamSystem.CurrentPath.Points.Count}");
            for (int p = 0; p < beamSystem.CurrentPath.Points.Count; p++)
            {
                Debug.Log($"[Test06 Point {p}] {beamSystem.CurrentPath.Points[p]}");
            }

            // Verify solve state
            Assert.IsTrue(beamSystem.CurrentPath.HasReachedTarget, "Beam path must reach target receiver.");
            Assert.AreEqual(2, beamSystem.CurrentPath.VisitedNodes.Count, "Path must traverse both Node Alpha and Node Beta.");
            Assert.IsTrue(target.IsEnergized, "TargetReceiver must be energized.");
            Assert.IsTrue(validator.IsSolved, "BeamValidator must confirm puzzle is solved.");
            Assert.AreEqual("TARGET CONNECTED", validator.StatusMessage, "Status message must confirm TARGET CONNECTED.");
        }

        [UnityTest]
        public IEnumerator Test07_Misalignment_DeactivatesTargetAndClearsSolvedState()
        {
            // Solve puzzle first
            yield return Test06_FullPuzzleSolve_TargetEnergizesAndStatusConnects();

            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            BeamValidator validator = Object.FindFirstObjectByType<BeamValidator>();
            TargetReceiver target = Object.FindFirstObjectByType<TargetReceiver>();
            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            PuzzleNode nodeBeta = System.Array.Find(nodes, n => n.name.Contains("Beta")) ?? (nodes.Length > 1 ? nodes[1] : nodes[0]);

            // Deliberately misalign Node Beta by 45 degrees
            nodeBeta.RotateNode(45f);
            beamSystem.RecalculateBeam();
            yield return null;

            Assert.IsFalse(beamSystem.CurrentPath.HasReachedTarget, "Misaligned beam must not reach target.");
            Assert.IsFalse(target.IsEnergized, "TargetReceiver must deactivate when beam is broken.");
            Assert.IsFalse(validator.IsSolved, "BeamValidator must switch to unsolved.");
            Assert.AreNotEqual("TARGET CONNECTED", validator.StatusMessage, "Status message must clear success message.");
        }

        [UnityTest]
        public IEnumerator Test08_ResetChamber_RestoresInitialStateAndAllowsResolving()
        {
            PuzzleManager puzzleManager = Object.FindFirstObjectByType<PuzzleManager>();
            TargetReceiver target = Object.FindFirstObjectByType<TargetReceiver>();

            // Misalign and reset
            puzzleManager.ResetPuzzle();
            yield return null;

            Assert.IsFalse(target.IsEnergized, "Target must be de-energized on reset.");
            Assert.IsFalse(puzzleManager.IsSolved, "PuzzleManager must not report solved on reset.");

            // Verify chamber can be solved again after reset
            yield return Test06_FullPuzzleSolve_TargetEnergizesAndStatusConnects();
            Assert.IsTrue(target.IsEnergized, "Target should energize again when re-solved.");
        }

        [UnityTest]
        public IEnumerator Test09_Deselect_ClearsActiveSelection()
        {
            NodeInteractionController controller = Object.FindFirstObjectByType<NodeInteractionController>();
            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);

            controller.InputProvider.SelectNode(nodes[0]);
            yield return null;
            Assert.IsNotNull(controller.ActiveNode, "Node should be selected.");

            controller.InputProvider.DeselectCurrentNode();
            yield return null;
            Assert.IsNull(controller.ActiveNode, "Controller active node should be null after deselect.");
        }

        [UnityTest]
        public IEnumerator Test10_Stability_RepeatedTransformationsDoNotThrowOrLeak()
        {
            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);

            // Rapidly rotate and translate nodes over 30 simulation steps
            for (int i = 0; i < 30; i++)
            {
                nodes[0].RotateNode(15f);
                nodes[1].RotateNode(-15f);
                nodes[0].MoveToClamped(new Vector3(-3.5f + Mathf.Sin(i) * 0.5f, 0.5f, 1.0f));
                beamSystem.RecalculateBeam();
                yield return null;
            }

            Assert.IsNotNull(beamSystem.CurrentPath);
            Assert.Greater(beamSystem.CurrentPath.Points.Count, 0, "Beam path must continue generating valid points.");
        }

        [UnityTest]
        public IEnumerator Test11_Chamber02_ObstacleOcclusion_BlocksDirectBeam()
        {
            LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            BeamValidator validator = Object.FindFirstObjectByType<BeamValidator>();

            Assert.IsNotNull(levelManager);
            levelManager.LoadLevel(1, true); // Force unlock Chamber 02
            yield return null;

            Assert.AreEqual("Chamber 02: Occlusion Barrier", levelManager.CurrentLevel.levelTitle);

            // Obstacle exists in scene
            Obstacle obstacle = Object.FindFirstObjectByType<Obstacle>();
            Assert.IsNotNull(obstacle, "Chamber 02 must contain an Obstacle component.");

            // Directly aim EnergySource into the central obstacle at (0, 0.5, 0)
            EnergySource source = Object.FindFirstObjectByType<EnergySource>();
            source.transform.position = new Vector3(0, 0.5f, -3.0f);
            source.transform.forward = Vector3.forward;

            beamSystem.RecalculateBeam();
            yield return null;

            Assert.IsTrue(beamSystem.CurrentPath.HitObstacle, "Beam hitting central obstacle must register HitObstacle.");
            Assert.IsFalse(validator.IsSolved, "Validator must report unsolved when beam is blocked by obstacle.");
            Assert.AreEqual("BEAM BLOCKED BY OBSTACLE", validator.StatusMessage, "Status message must reflect obstacle blocking.");
        }

        [UnityTest]
        public IEnumerator Test12_Chamber02_FullPuzzleSolve_DetoursAroundObstacle()
        {
            LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            BeamValidator validator = Object.FindFirstObjectByType<BeamValidator>();
            TargetReceiver target = Object.FindFirstObjectByType<TargetReceiver>();
            EnergySource source = Object.FindFirstObjectByType<EnergySource>();

            levelManager.LoadLevel(1, true);
            yield return null;

            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            Assert.IsTrue(nodes.Length >= 2);

            PuzzleNode nodeAlpha = System.Array.Find(nodes, n => n.name.Contains("Alpha")) ?? nodes[0];
            PuzzleNode nodeBeta = System.Array.Find(nodes, n => n.name.Contains("Beta")) ?? nodes[1];

            // 1. Source at (-3.5, 0.5, -3.0) aiming +Z
            source.transform.position = new Vector3(-3.5f, 0.5f, -3.0f);
            source.transform.forward = Vector3.forward;

            // 2. Node Alpha at (-3.5, 0.5, 2.5) rotated 90 deg aiming +X
            nodeAlpha.transform.position = new Vector3(-3.5f, 0.5f, 2.5f);
            nodeAlpha.transform.rotation = Quaternion.Euler(0, 90f, 0);

            // 3. Node Beta at (3.5, 0.5, 2.5) rotated 180 deg aiming -Z
            nodeBeta.transform.position = new Vector3(3.5f, 0.5f, 2.5f);
            nodeBeta.transform.rotation = Quaternion.Euler(0, 180f, 0);

            // 4. Target at (3.5, 0.5, -3.0) facing +Z (forward)
            target.transform.position = new Vector3(3.5f, 0.5f, -3.0f);
            target.transform.forward = Vector3.forward;

            beamSystem.RecalculateBeam();
            yield return null;

            Assert.IsTrue(beamSystem.CurrentPath.HasReachedTarget, "Chamber 02 beam must detour around obstacle and reach target.");
            Assert.IsFalse(beamSystem.CurrentPath.HitObstacle, "Successful detour must not collide with obstacle.");
            Assert.IsTrue(target.IsEnergized, "TargetReceiver must be energized.");
            Assert.IsTrue(validator.IsSolved, "Chamber 02 must be marked solved.");
        }

        [UnityTest]
        public IEnumerator Test13_Chamber03_PrismSplit_DualTargetsEnergizedSimultaneously()
        {
            LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            BeamValidator validator = Object.FindFirstObjectByType<BeamValidator>();
            EnergySource source = Object.FindFirstObjectByType<EnergySource>();

            levelManager.LoadLevel(2, true); // Force unlock Chamber 03
            yield return null;

            Assert.AreEqual("Chamber 03: Prism Bifurcation", levelManager.CurrentLevel.levelTitle);
            Assert.IsTrue(levelManager.CurrentLevel.useDualTargets, "Chamber 03 must use dual targets.");

            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            Assert.IsTrue(nodes.Length >= 2);

            PuzzleNode nodeAlpha = System.Array.Find(nodes, n => n.name.Contains("Alpha")) ?? nodes[0];
            PuzzleNode nodeBeta = System.Array.Find(nodes, n => n.name.Contains("Beta")) ?? nodes[1];

            // 1. Source at (-3.5, 0.5, -3.0) aiming +Z
            source.transform.position = new Vector3(-3.5f, 0.5f, -3.0f);
            source.transform.forward = Vector3.forward;

            // 2. Node Alpha: PrismSplit at (-3.5, 0.5, 0.0), facing +Z (rotation 0)
            nodeAlpha.transform.position = new Vector3(-3.5f, 0.5f, 0.0f);
            nodeAlpha.transform.rotation = Quaternion.Euler(0, 0f, 0);
            nodeAlpha.SetRedirectType(NodeRedirectType.PrismSplit);

            // 3. Node Beta: Diverter at (3.5, 0.5, 0.0), facing +Z (rotation 0)
            nodeBeta.transform.position = new Vector3(3.5f, 0.5f, 0.0f);
            nodeBeta.transform.rotation = Quaternion.Euler(0, 0f, 0);

            beamSystem.RecalculateBeam();
            yield return null;

            Assert.IsTrue(beamSystem.CurrentPath.Branches.Count >= 1, "Prism node must create secondary beam branch.");
            Assert.AreEqual(2, beamSystem.CurrentPath.HitTargets.Count, "Both Target 1 and Target 2 must be hit simultaneously.");
            Assert.IsTrue(validator.IsSolved, "Chamber 03 must be marked solved when both targets are energized.");
            Assert.AreEqual("ALL TARGETS ENERGIZED", validator.StatusMessage, "Status must confirm all targets energized.");
        }

        [UnityTest]
        public IEnumerator Test14_LevelProgression_UnlockedOnSolve_AndRestrictedWhenLocked()
        {
            LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
            levelManager.LoadLevel(0, false);
            yield return null;

            Assert.AreEqual(0, levelManager.CurrentLevelIndex);

            // Verify Chamber 02 is locked initially if unlockedLevelIndex is 0
            if (levelManager.UnlockedLevelIndex == 0)
            {
                levelManager.LoadLevel(1, false);
                yield return null;
                Assert.AreEqual(0, levelManager.CurrentLevelIndex, "Locked Chamber 02 must not load without being unlocked.");
            }

            // Unlocking Chamber 02 via NextLevel when allowed
            levelManager.LoadLevel(1, true);
            yield return null;
            Assert.AreEqual(1, levelManager.CurrentLevelIndex, "Chamber 02 must load when unlocked.");

            // Previous level navigation
            levelManager.PreviousLevel();
            yield return null;
            Assert.AreEqual(0, levelManager.CurrentLevelIndex, "PreviousLevel should navigate back to Chamber 01.");
        }

        [UnityTest]
        public IEnumerator Test15_TutorialManager_AdvancesStepsAndToggles()
        {
            TutorialManager tutMgr = Object.FindFirstObjectByType<TutorialManager>();
            Assert.IsNotNull(tutMgr, "TutorialManager should be present in scene.");

            tutMgr.SetStep(TutorialStep.SelectNode);
            Assert.AreEqual(TutorialStep.SelectNode, tutMgr.CurrentStep);

            tutMgr.NextStep();
            Assert.AreEqual(TutorialStep.MoveNode, tutMgr.CurrentStep);

            tutMgr.NextStep();
            Assert.AreEqual(TutorialStep.RotateNode, tutMgr.CurrentStep);

            tutMgr.PreviousStep();
            Assert.AreEqual(TutorialStep.MoveNode, tutMgr.CurrentStep);

            bool wasActive = tutMgr.IsActive;
            tutMgr.ToggleTutorial();
            Assert.AreEqual(!wasActive, tutMgr.IsActive, "ToggleTutorial should flip active state.");
            tutMgr.SetTutorialActive(true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Chamber04_ReflectionProtocol_LoadsWithDualTargetsAndObstacles()
        {
            var levelMgr = Object.FindFirstObjectByType<LevelManager>();
            Assert.IsNotNull(levelMgr, "LevelManager must be present in scene.");

            // Chamber 04 is index 3 (4th chamber)
            levelMgr.LoadLevel(3, forceUnlock: true);
            yield return null;

            Assert.AreEqual(3, levelMgr.CurrentLevelIndex, "Current level index should be 3 (Chamber 04).");
            Assert.AreEqual("Chamber 04: Reflection Protocol", levelMgr.CurrentLevel.levelTitle);
            Assert.IsTrue(levelMgr.CurrentLevel.useDualTargets, "Chamber 04 must have useDualTargets enabled.");
            Assert.AreEqual(3, levelMgr.CurrentLevel.initialNodes.Count, "Chamber 04 requires 3 initial nodes.");
            Assert.AreEqual(2, levelMgr.CurrentLevel.obstacles.Count, "Chamber 04 requires 2 obstacle barriers.");

            var beamValidator = Object.FindFirstObjectByType<BeamValidator>();
            Assert.IsNotNull(beamValidator, "BeamValidator must be present.");
            Assert.AreEqual(2, beamValidator.RequiredTargets.Count, "Chamber 04 must have 2 targets populated in BeamValidator.");

            var obstacles = Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
            Assert.AreEqual(2, obstacles.Length, "Chamber 04 should spawn exactly 2 active obstacles in hierarchy.");
        }

        [UnityTest]
        public IEnumerator Chamber04_Reset_PreservesNodeTypesAndBoundaries()
        {
            var levelMgr = Object.FindFirstObjectByType<LevelManager>();
            Assert.IsNotNull(levelMgr, "LevelManager must be present.");
            levelMgr.LoadLevel(3, forceUnlock: true);
            yield return null;

            var puzzleMgr = Object.FindFirstObjectByType<PuzzleManager>();
            Assert.IsNotNull(puzzleMgr, "PuzzleManager must be present.");

            // Verify nodes in scene
            var nodes = puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 3, "Chamber 04 must have at least 3 nodes registered.");

            // Modify position of first node
            Vector3 originalPos = nodes[0].transform.position;
            nodes[0].MoveToClamped(new Vector3(0f, 0.5f, 0f));
            Assert.AreNotEqual(originalPos, nodes[0].transform.position);

            // Trigger reset
            puzzleMgr.ResetPuzzle();
            yield return null;

            // Re-fetch
            Assert.AreEqual(originalPos, nodes[0].transform.position, "ResetPuzzle should restore node to initial chamber spawn position.");
        }
    }
}
