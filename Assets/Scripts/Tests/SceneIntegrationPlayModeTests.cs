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
    }
}
