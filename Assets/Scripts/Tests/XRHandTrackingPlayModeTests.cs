using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Hands;
using LumenLoom.Core;
using LumenLoom.Puzzle;
using LumenLoom.Interaction;

namespace LumenLoom.Tests
{
    /// <summary>
    /// Automated integration and unit tests for genuine OpenXR / XR Hands interaction.
    /// Exercises pinch detection, dual-threshold hysteresis, coordinate mapping,
    /// proximity node selection, tracking loss safety, independent hand state, and desktop isolation.
    /// </summary>
    public class XRHandTrackingPlayModeTests
    {
        private HandInputProvider _handProvider;
        private NodeInteractionController _controller;
        private XRCameraManager _cameraMgr;
        private PuzzleManager _puzzleMgr;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainPuzzleScene");
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            yield return null;

            _handProvider = Object.FindFirstObjectByType<HandInputProvider>();
            _controller = Object.FindFirstObjectByType<NodeInteractionController>();
            _cameraMgr = Object.FindFirstObjectByType<XRCameraManager>();
            _puzzleMgr = Object.FindFirstObjectByType<PuzzleManager>();

            Assert.IsNotNull(_handProvider, "HandInputProvider must exist in scene.");
            Assert.IsNotNull(_controller, "NodeInteractionController must exist in scene.");
            Assert.IsNotNull(_cameraMgr, "XRCameraManager must exist in scene.");
            Assert.IsNotNull(_puzzleMgr, "PuzzleManager must exist in scene.");

            // Switch to XR mode for hand tracking tests
            _cameraMgr.SetRigMode(CameraRigMode.XR);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_cameraMgr != null)
            {
                _cameraMgr.SetRigMode(CameraRigMode.Desktop);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test01_SubsystemUnavailable_HandlesGracefullyWithoutThrowing()
        {
            // Verify HandSubsystem discovery and dormant behavior in desktop environment
            Assert.DoesNotThrow(() =>
            {
                _handProvider.TryAcquireSubsystem();
            });

            // Calling ProcessHand with untracked hand must be completely safe
            XRHand untrackedHand = default;
            Assert.DoesNotThrow(() =>
            {
                _handProvider.ProcessHand(untrackedHand, _handProvider.RightHandState);
            });

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test02_SubsystemDiscovery_LifecycleMethodsExecuteSafely()
        {
            Assert.DoesNotThrow(() =>
            {
                _handProvider.RegisterSubsystem(null);
                _handProvider.UnregisterSubsystem();
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test03_PinchEngagement_TriggersAtConfiguredThreshold()
        {
            // Position thumb and index fingertips 0.02m apart (<= 0.025m engage threshold)
            Pose thumbPose = new Pose(new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            Pose indexPose = new Pose(new Vector3(0.02f, 0.5f, 0f), Quaternion.identity);

            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexPose, isTracked: true);
            yield return null;

            Assert.IsTrue(_handProvider.RightHandState.IsPinched, "Pinch must engage when distance <= 0.025m.");
            Assert.AreEqual(0.02f, _handProvider.RightHandState.LastPinchDistance, 0.001f);
        }

        [UnityTest]
        public IEnumerator Test04_PinchRelease_TriggersAboveReleaseThreshold()
        {
            // 1. Engage pinch first at 0.02m
            Pose thumbPose = new Pose(new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            Pose indexEngage = new Pose(new Vector3(0.02f, 0.5f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexEngage, isTracked: true);
            yield return null;
            Assert.IsTrue(_handProvider.RightHandState.IsPinched);

            // 2. Open fingers to 0.04m (> 0.035m release threshold)
            Pose indexRelease = new Pose(new Vector3(0.04f, 0.5f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexRelease, isTracked: true);
            yield return null;

            Assert.IsFalse(_handProvider.RightHandState.IsPinched, "Pinch must release when distance > 0.035m.");
        }

        [UnityTest]
        public IEnumerator Test05_Hysteresis_MaintainsStateBetweenThresholds()
        {
            Pose thumbPose = new Pose(new Vector3(0f, 0.5f, 0f), Quaternion.identity);

            // Step 1: Start unpinched at 0.05m
            Pose p50mm = new Pose(new Vector3(0.05f, 0.5f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, p50mm, isTracked: true);
            Assert.IsFalse(_handProvider.RightHandState.IsPinched);

            // Step 2: Move fingers to 0.030m (in the hysteresis band: 0.025m < d <= 0.035m)
            Pose p30mm = new Pose(new Vector3(0.03f, 0.5f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, p30mm, isTracked: true);
            Assert.IsFalse(_handProvider.RightHandState.IsPinched, "Must remain unpinched until distance drops <= 0.025m.");

            // Step 3: Close fingers to 0.020m (<= 0.025m engage)
            Pose p20mm = new Pose(new Vector3(0.02f, 0.5f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, p20mm, isTracked: true);
            Assert.IsTrue(_handProvider.RightHandState.IsPinched, "Must engage pinch when distance <= 0.025m.");

            // Step 4: Open fingers back to 0.030m (in the hysteresis band while pinched)
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, p30mm, isTracked: true);
            Assert.IsTrue(_handProvider.RightHandState.IsPinched, "Must maintain pinched state while distance <= 0.035m.");

            // Step 5: Open fingers to 0.038m (> 0.035m release)
            Pose p38mm = new Pose(new Vector3(0.038f, 0.5f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, p38mm, isTracked: true);
            Assert.IsFalse(_handProvider.RightHandState.IsPinched, "Must release pinch when distance exceeds 0.035m.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test06_NodeSelection_WithinRadius_GrabsNearestNode()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 1);
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Pinch directly within 0.05m of the node (<= 0.15m selection radius)
            Pose thumbPose = new Pose(nodePos, Quaternion.identity);
            Pose indexPose = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);

            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexPose, isTracked: true);
            yield return null;

            Assert.IsTrue(_handProvider.IsDragging, "HandInputProvider must enter dragging state.");
            Assert.AreEqual(targetNode, _handProvider.SelectedNode, "Target node must be selected.");
            Assert.AreEqual(targetNode, _handProvider.RightHandState.HeldNode, "Right hand must hold target node.");
            Assert.AreEqual(targetNode, _controller.ActiveNode, "Controller active node must match target node.");
        }

        [UnityTest]
        public IEnumerator Test07_NoSelection_OutsideRadius()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 1);

            // Pinch at empty space far from any node (e.g. +3.0m offset)
            Vector3 emptySpace = new Vector3(0f, 2.5f, 0f);
            Pose thumbPose = new Pose(emptySpace, Quaternion.identity);
            Pose indexPose = new Pose(emptySpace + new Vector3(0.02f, 0f, 0f), Quaternion.identity);

            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexPose, isTracked: true);
            yield return null;

            Assert.IsTrue(_handProvider.RightHandState.IsPinched, "Pinch state is active.");
            Assert.IsNull(_handProvider.RightHandState.HeldNode, "No node should be held when outside selection radius.");
            Assert.IsFalse(_handProvider.IsDragging, "Provider must not be dragging.");
        }

        [UnityTest]
        public IEnumerator Test08_NoDuplicateGrabStartEvents()
        {
            var nodes = _puzzleMgr.Nodes;
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            int selectCount = 0;
            _handProvider.OnNodeSelected += (n) => selectCount++;

            // Initial pinch grab
            Pose thumbPose = new Pose(nodePos, Quaternion.identity);
            Pose indexPose = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexPose, isTracked: true);
            yield return null;

            // Move hand continuously while pinched
            for (int i = 1; i <= 5; i++)
            {
                Pose moveThumb = new Pose(nodePos + new Vector3(i * 0.1f, 0f, 0f), Quaternion.identity);
                Pose moveIndex = new Pose(nodePos + new Vector3(i * 0.1f + 0.02f, 0f, 0f), Quaternion.identity);
                _handProvider.SimulateTrackedJoints(Handedness.Right, moveThumb, moveIndex, isTracked: true);
                yield return null;
            }

            Assert.AreEqual(1, selectCount, "OnNodeSelected must be fired exactly once per pinch engagement.");
        }

        [UnityTest]
        public IEnumerator Test09_DragMovement_TranslatesNodeAndEnforcesBoundary()
        {
            var nodes = _puzzleMgr.Nodes;
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Grab node
            Pose thumbPose = new Pose(nodePos, Quaternion.identity);
            Pose indexPose = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexPose, isTracked: true);
            yield return null;

            // Move hand far past chamber boundary (+10m on X and Z)
            Vector3 farOut = new Vector3(10f, 0.5f, 10f);
            Pose dragThumb = new Pose(farOut, Quaternion.identity);
            Pose dragIndex = new Pose(farOut + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, dragThumb, dragIndex, isTracked: true);
            yield return null;

            // Clamping should enforce node is inside [-4.5, 4.5]
            Assert.LessOrEqual(targetNode.transform.position.x, 4.5f + 0.01f, "X position must be clamped within chamber boundary.");
            Assert.LessOrEqual(targetNode.transform.position.z, 4.5f + 0.01f, "Z position must be clamped within chamber boundary.");
            Assert.AreEqual(0.5f, targetNode.transform.position.y, 0.001f, "Height must remain on interaction plane.");
        }

        [UnityTest]
        public IEnumerator Test10_RightHandTrackingLoss_SafelyReleasesHeldNode()
        {
            var nodes = _puzzleMgr.Nodes;
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Grab node
            Pose thumbPose = new Pose(nodePos, Quaternion.identity);
            Pose indexPose = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexPose, isTracked: true);
            yield return null;
            Assert.IsTrue(_handProvider.IsDragging);

            // Abrupt tracking loss
            _handProvider.SimulateTrackingLost(Handedness.Right);
            yield return null;

            Assert.IsFalse(_handProvider.IsDragging, "Dragging must stop immediately on tracking loss.");
            Assert.IsNull(_handProvider.RightHandState.HeldNode, "Held node must be cleared on tracking loss.");
            Assert.IsFalse(_handProvider.RightHandState.IsPinched, "Pinch state must be cleared on tracking loss.");
            Assert.IsNull(_controller.ActiveNode, "Controller active node must be cleared on tracking loss.");
        }

        [UnityTest]
        public IEnumerator Test11_LeftHandTrackingLoss_SafelyReleasesHeldNode()
        {
            var nodes = _puzzleMgr.Nodes;
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Grab with left hand
            Pose thumbPose = new Pose(nodePos, Quaternion.identity);
            Pose indexPose = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, thumbPose, indexPose, isTracked: true);
            yield return null;
            Assert.IsTrue(_handProvider.IsDragging);

            // Abrupt tracking loss
            _handProvider.SimulateTrackingLost(Handedness.Left);
            yield return null;

            Assert.IsFalse(_handProvider.IsDragging);
            Assert.IsNull(_handProvider.LeftHandState.HeldNode);
            Assert.IsNull(_controller.ActiveNode);
        }

        [UnityTest]
        public IEnumerator Test12_TrackingRecovery_ReacquiresCleanlyWithoutPhantomGrab()
        {
            // Lose tracking first
            _handProvider.SimulateTrackingLost(Handedness.Right);
            yield return null;

            // Recover tracking with open hand (distance 0.08m)
            Vector3 pos = new Vector3(0f, 0.5f, 0f);
            Pose thumbPose = new Pose(pos, Quaternion.identity);
            Pose indexPose = new Pose(pos + new Vector3(0.08f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, thumbPose, indexPose, isTracked: true);
            yield return null;

            Assert.IsTrue(_handProvider.RightHandState.IsTracked, "Hand must report tracked.");
            Assert.IsFalse(_handProvider.RightHandState.IsPinched, "Hand must NOT report pinched with open fingers.");
            Assert.IsNull(_handProvider.RightHandState.HeldNode, "No node should be grabbed on recovery.");
        }

        [UnityTest]
        public IEnumerator Test13_IndependentHands_OneHandLossDoesNotCorruptOtherHand()
        {
            // Both hands tracked simultaneously
            Pose lThumb = new Pose(new Vector3(-1f, 0.5f, 0f), Quaternion.identity);
            Pose lIndex = new Pose(new Vector3(-1f + 0.02f, 0.5f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndex, isTracked: true);

            Pose rThumb = new Pose(new Vector3(1f, 0.5f, 0f), Quaternion.identity);
            Pose rIndex = new Pose(new Vector3(1f + 0.05f, 0.5f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);

            Assert.IsTrue(_handProvider.LeftHandState.IsPinched);
            Assert.IsFalse(_handProvider.RightHandState.IsPinched);

            // Left hand loses tracking
            _handProvider.SimulateTrackingLost(Handedness.Left);

            Assert.IsFalse(_handProvider.LeftHandState.IsTracked);
            Assert.IsFalse(_handProvider.LeftHandState.IsPinched);
            Assert.IsTrue(_handProvider.RightHandState.IsTracked, "Right hand must remain tracked.");
            Assert.IsFalse(_handProvider.RightHandState.IsPinched, "Right hand pinch state must not be corrupted.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test14_RepeatedInputProviderSwitching_NoDuplicateSubscriptions()
        {
            var mouseProvider = Object.FindFirstObjectByType<MouseInputProvider>();
            Assert.IsNotNull(mouseProvider);

            int callbackCount = 0;
            System.Action<PuzzleNode> testCallback = (n) => callbackCount++;

            _handProvider.OnNodeSelected += testCallback;

            // Rapidly toggle providers 5 times
            for (int i = 0; i < 5; i++)
            {
                _controller.SetInputProvider(mouseProvider);
                _controller.SetInputProvider(_handProvider);
            }

            // Trigger one select
            var targetNode = _puzzleMgr.Nodes[0];
            _handProvider.SelectNode(targetNode);
            yield return null;

            Assert.AreEqual(1, callbackCount, "Callback must fire exactly once despite repeated provider switching.");
            _handProvider.OnNodeSelected -= testCallback;
        }

        [UnityTest]
        public IEnumerator Test15_LeftHandGrabs_RightHandCannotStealSameNode()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 1, "Must have at least one node.");
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Step 1: Left hand pinches and grabs targetNode
            Pose lThumb = new Pose(nodePos, Quaternion.identity);
            Pose lIndex = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndex, isTracked: true);
            yield return null;

            Assert.AreEqual(targetNode, _handProvider.LeftHandState.HeldNode, "Left hand must hold target node.");
            Assert.IsTrue(_handProvider.IsNodeOwned(targetNode), "Target node must be registered as owned.");
            Assert.IsTrue(_handProvider.TryGetNodeOwner(targetNode, out Handedness owner) && owner == Handedness.Left);

            // Step 2: Right hand attempts to pinch and grab the exact same node
            Pose rThumb = new Pose(nodePos, Quaternion.identity);
            Pose rIndex = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);
            yield return null;

            Assert.IsTrue(_handProvider.RightHandState.IsPinched, "Right hand pinch is engaged.");
            Assert.IsNull(_handProvider.RightHandState.HeldNode, "Right hand must NOT steal node already owned by left hand.");
            Assert.AreEqual(targetNode, _handProvider.LeftHandState.HeldNode, "Left hand must retain ownership of target node.");
        }

        [UnityTest]
        public IEnumerator Test16_RightHandGrabs_LeftHandCannotStealSameNode()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 1);
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Step 1: Right hand pinches and grabs targetNode
            Pose rThumb = new Pose(nodePos, Quaternion.identity);
            Pose rIndex = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);
            yield return null;

            Assert.AreEqual(targetNode, _handProvider.RightHandState.HeldNode, "Right hand must hold target node.");
            Assert.IsTrue(_handProvider.TryGetNodeOwner(targetNode, out Handedness owner) && owner == Handedness.Right);

            // Step 2: Left hand attempts to pinch and grab the exact same node
            Pose lThumb = new Pose(nodePos, Quaternion.identity);
            Pose lIndex = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndex, isTracked: true);
            yield return null;

            Assert.IsTrue(_handProvider.LeftHandState.IsPinched, "Left hand pinch is engaged.");
            Assert.IsNull(_handProvider.LeftHandState.HeldNode, "Left hand must NOT steal node already owned by right hand.");
            Assert.AreEqual(targetNode, _handProvider.RightHandState.HeldNode, "Right hand must retain ownership of target node.");
        }

        [UnityTest]
        public IEnumerator Test17_BothHands_IndependentlyGrabDifferentNodes()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 2, "Test requires at least two nodes.");
            PuzzleNode nodeA = nodes[0];
            PuzzleNode nodeB = nodes[1];

            // Left hand grabs nodeA
            Pose lThumb = new Pose(nodeA.transform.position, Quaternion.identity);
            Pose lIndex = new Pose(nodeA.transform.position + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndex, isTracked: true);

            // Right hand grabs nodeB
            Pose rThumb = new Pose(nodeB.transform.position, Quaternion.identity);
            Pose rIndex = new Pose(nodeB.transform.position + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);
            yield return null;

            Assert.AreEqual(nodeA, _handProvider.LeftHandState.HeldNode, "Left hand must hold node A.");
            Assert.AreEqual(nodeB, _handProvider.RightHandState.HeldNode, "Right hand must hold node B.");
            Assert.IsTrue(_handProvider.TryGetNodeOwner(nodeA, out var ownerA) && ownerA == Handedness.Left);
            Assert.IsTrue(_handProvider.TryGetNodeOwner(nodeB, out var ownerB) && ownerB == Handedness.Right);
            Assert.IsTrue(_handProvider.IsDragging, "Provider must report active dragging state.");
        }

        [UnityTest]
        public IEnumerator Test18_OneHandReleases_OtherHandContinuesDragging()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 2);
            PuzzleNode nodeA = nodes[0];
            PuzzleNode nodeB = nodes[1];

            // Left grabs nodeA, Right grabs nodeB
            Pose lThumb = new Pose(nodeA.transform.position, Quaternion.identity);
            Pose lIndex = new Pose(nodeA.transform.position + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndex, isTracked: true);

            Pose rThumb = new Pose(nodeB.transform.position, Quaternion.identity);
            Pose rIndex = new Pose(nodeB.transform.position + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);
            yield return null;

            // Left hand opens fingers to 0.04m (> 0.035m release threshold)
            Pose lIndexOpen = new Pose(nodeA.transform.position + new Vector3(0.04f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndexOpen, isTracked: true);
            yield return null;

            Assert.IsNull(_handProvider.LeftHandState.HeldNode, "Left hand must have released its node.");
            Assert.IsFalse(_handProvider.IsNodeOwned(nodeA), "Node A must no longer be owned.");
            Assert.AreEqual(nodeB, _handProvider.RightHandState.HeldNode, "Right hand must continue holding node B.");
            Assert.IsTrue(_handProvider.IsNodeOwned(nodeB), "Node B must remain owned by right hand.");
            Assert.IsTrue(_handProvider.IsDragging, "Provider must maintain dragging state while right hand still holds a node.");
        }

        [UnityTest]
        public IEnumerator Test19_OneHandTrackingLost_OtherHandContinuesHoldingNode()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 2);
            PuzzleNode nodeA = nodes[0];
            PuzzleNode nodeB = nodes[1];

            // Both grab their respective nodes
            Pose lThumb = new Pose(nodeA.transform.position, Quaternion.identity);
            Pose lIndex = new Pose(nodeA.transform.position + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndex, isTracked: true);

            Pose rThumb = new Pose(nodeB.transform.position, Quaternion.identity);
            Pose rIndex = new Pose(nodeB.transform.position + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);
            yield return null;

            // Left hand loses tracking abruptly
            _handProvider.SimulateTrackingLost(Handedness.Left);
            yield return null;

            Assert.IsNull(_handProvider.LeftHandState.HeldNode, "Left hand held node must be cleared.");
            Assert.IsFalse(_handProvider.LeftHandState.IsTracked, "Left hand must be untracked.");
            Assert.IsFalse(_handProvider.IsNodeOwned(nodeA), "Node A must be freed from ownership.");

            Assert.AreEqual(nodeB, _handProvider.RightHandState.HeldNode, "Right hand must remain holding node B.");
            Assert.IsTrue(_handProvider.RightHandState.IsTracked, "Right hand must remain tracked.");
            Assert.IsTrue(_handProvider.IsNodeOwned(nodeB), "Node B must remain owned by right hand.");
            Assert.IsTrue(_handProvider.IsDragging, "Dragging state must remain active.");
        }

        [UnityTest]
        public IEnumerator Test20_ReleasedNode_BecomesAvailableToOtherHand()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 1);
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Left hand grabs targetNode
            Pose lThumb = new Pose(nodePos, Quaternion.identity);
            Pose lIndex = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndex, isTracked: true);
            yield return null;
            Assert.AreEqual(targetNode, _handProvider.LeftHandState.HeldNode);

            // Right hand tries to grab -> blocked
            Pose rThumb = new Pose(nodePos, Quaternion.identity);
            Pose rIndex = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);
            yield return null;
            Assert.IsNull(_handProvider.RightHandState.HeldNode);

            // Left hand releases targetNode
            Pose lIndexOpen = new Pose(nodePos + new Vector3(0.04f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndexOpen, isTracked: true);
            yield return null;
            Assert.IsFalse(_handProvider.IsNodeOwned(targetNode), "Target node must be free after left hand release.");

            // Right hand releases pinch then pinches again to grab
            Pose rIndexOpen = new Pose(nodePos + new Vector3(0.04f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndexOpen, isTracked: true);
            yield return null;

            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);
            yield return null;

            Assert.AreEqual(targetNode, _handProvider.RightHandState.HeldNode, "Right hand must now successfully claim ownership.");
            Assert.IsTrue(_handProvider.TryGetNodeOwner(targetNode, out var owner) && owner == Handedness.Right);
        }

        [UnityTest]
        public IEnumerator Test21_ProviderSwitching_ClearsOwnershipSafely()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 1);
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Left hand grabs node
            Pose lThumb = new Pose(nodePos, Quaternion.identity);
            Pose lIndex = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Left, lThumb, lIndex, isTracked: true);
            yield return null;
            Assert.IsTrue(_handProvider.IsNodeOwned(targetNode));

            // Reset all hand states (simulates provider deactivation / switching)
            _handProvider.ResetAllHandStates();
            yield return null;

            Assert.IsFalse(_handProvider.IsNodeOwned(targetNode), "Ownership mapping must be completely cleared.");
            Assert.IsNull(_handProvider.LeftHandState.HeldNode, "Left hand held node must be null.");
            Assert.IsNull(_handProvider.RightHandState.HeldNode, "Right hand held node must be null.");
            Assert.IsFalse(_handProvider.IsDragging, "Dragging must be false.");
        }

        [UnityTest]
        public IEnumerator Test22_RepeatedTrackingLoss_DoesNotProduceDuplicateReleaseEvents()
        {
            var nodes = _puzzleMgr.Nodes;
            Assert.IsTrue(nodes.Count >= 1);
            PuzzleNode targetNode = nodes[0];
            Vector3 nodePos = targetNode.transform.position;

            // Right hand grabs node
            Pose rThumb = new Pose(nodePos, Quaternion.identity);
            Pose rIndex = new Pose(nodePos + new Vector3(0.02f, 0f, 0f), Quaternion.identity);
            _handProvider.SimulateTrackedJoints(Handedness.Right, rThumb, rIndex, isTracked: true);
            yield return null;
            Assert.AreEqual(targetNode, _handProvider.RightHandState.HeldNode);

            int releaseCount = 0;
            System.Action releaseAction = () => releaseCount++;
            _handProvider.OnNodeReleased += releaseAction;

            // Tracking lost 1st time
            _handProvider.SimulateTrackingLost(Handedness.Right);
            yield return null;
            Assert.AreEqual(1, releaseCount, "Release event must fire on initial tracking loss.");

            // Repeated tracking lost notifications (simulating sensor disconnect flutter)
            _handProvider.SimulateTrackingLost(Handedness.Right);
            _handProvider.SimulateTrackingLost(Handedness.Right);
            yield return null;

            Assert.AreEqual(1, releaseCount, "Repeated tracking loss calls must NOT produce duplicate release events.");
            _handProvider.OnNodeReleased -= releaseAction;
        }
    }
}

