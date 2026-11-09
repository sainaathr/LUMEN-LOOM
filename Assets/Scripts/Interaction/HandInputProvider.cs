using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using Unity.XR.CoreUtils;
using LumenLoom.Puzzle;
using LumenLoom.Rendering;

namespace LumenLoom.Interaction
{
    /// <summary>
    /// Independent tracking and interaction state for a single hand (Left or Right).
    /// </summary>
    [Serializable]
    public class HandTrackingState
    {
        public Handedness Handedness;
        public bool IsTracked;
        public bool IsPinched;
        public Vector3 LastPinchPosition;
        public float LastPinchDistance;
        public PuzzleNode HeldNode;

        public HandTrackingState(Handedness handedness)
        {
            Handedness = handedness;
            IsTracked = false;
            IsPinched = false;
            LastPinchPosition = Vector3.zero;
            LastPinchDistance = float.MaxValue;
            HeldNode = null;
        }

        public void Reset()
        {
            IsTracked = false;
            IsPinched = false;
            LastPinchPosition = Vector3.zero;
            LastPinchDistance = float.MaxValue;
            HeldNode = null;
        }
    }

    /// <summary>
    /// Genuine OpenXR / Unity XR Hands (1.4.1) input provider for LUMEN LOOM.
    /// Implements IInputProvider to map real tracked joint poses (ThumbTip, IndexTip)
    /// into optical puzzle node selection, dragging, and release.
    /// 
    /// Features:
    /// 1. Low-allocation joint extraction from XRHandSubsystem during Dynamic update phase.
    /// 2. Dual-threshold Schmitt trigger hysteresis (0.025m engage, 0.035m release) to eliminate pinch flickering.
    /// 3. Proximity-based optical node selection (0.15m radius) with zero duplicate grabs.
    /// 4. Independent left and right hand tracking state machines.
    /// 5. Immediate fail-safe release upon tracking loss.
    /// 6. Test injection hooks for deterministic UTF automated testing without physical VR hardware.
    /// </summary>
    public class HandInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("XR Hands Pinch Configuration")]
        [Tooltip("Distance in meters between thumb and index fingertips to engage pinch (nominal: 0.025m = 25mm).")]
        [SerializeField] private float pinchEngageThreshold = 0.025f;

        [Tooltip("Distance in meters between thumb and index fingertips to release pinch (nominal: 0.035m = 35mm).")]
        [SerializeField] private float pinchReleaseThreshold = 0.035f;

        [Tooltip("Maximum distance in meters from pinch center to an optical node for selection.")]
        [SerializeField] private float selectionRadius = 0.15f;

        [Header("Rotation & Gestures")]
        [SerializeField] private float rotationScaleMultiplier = 1.0f;
        [SerializeField] private bool enableWristTurnRotation = false; // Rotation deferred to dedicated gesture

        [Header("Scene References")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private BeamSystem beamSystem;

        [Header("Debug Simulation (Editor / Play Mode)")]
        [SerializeField] private bool simulateHandWithKeys = false;

        // Public properties
        public float PinchEngageThreshold { get => pinchEngageThreshold; set => pinchEngageThreshold = value; }
        public float PinchReleaseThreshold { get => pinchReleaseThreshold; set => pinchReleaseThreshold = value; }
        public float SelectionRadius { get => selectionRadius; set => selectionRadius = value; }
        public float PinchThreshold { get => pinchEngageThreshold; set => pinchEngageThreshold = value; } // Backward compatibility
        public bool EnableWristTurnRotation { get => enableWristTurnRotation; set => enableWristTurnRotation = value; }
        public bool SimulateHandWithKeys { get => simulateHandWithKeys; set => simulateHandWithKeys = value; }

        public HandTrackingState LeftHandState => _leftHandState;
        public HandTrackingState RightHandState => _rightHandState;
        public XRHandSubsystem HandSubsystem => _handSubsystem;

        // IInputProvider implementation
        public event Action<PuzzleNode> OnNodeSelected;
        public event Action<Vector3> OnNodeDragPositionUpdated;
        public event Action<float> OnNodeRotationDelta;
        public event Action OnNodeReleased;
        public event Action OnResetRequested;
        public event Action OnDeselectRequested;

        private bool _isDragging;
        private PuzzleNode _hoveredNode;
        private PuzzleNode _selectedNode;

        public bool IsDragging => _isDragging;
        public PuzzleNode HoveredNode => _hoveredNode;
        public PuzzleNode SelectedNode => _selectedNode;

        // Hand states
        private readonly HandTrackingState _leftHandState = new HandTrackingState(Handedness.Left);
        private readonly HandTrackingState _rightHandState = new HandTrackingState(Handedness.Right);

        // Explicit node ownership mapping: PuzzleNode -> Handedness
        private readonly Dictionary<PuzzleNode, Handedness> _nodeOwners = new Dictionary<PuzzleNode, Handedness>();

        // Subsystem tracking
        private XRHandSubsystem _handSubsystem;
        private static readonly List<XRHandSubsystem> s_Subsystems = new List<XRHandSubsystem>();
        private static readonly Collider[] s_OverlapBuffer = new Collider[8];
        private float _subsystemPollTimer;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            TryAcquireSubsystem();
        }

        private void OnDisable()
        {
            UnregisterSubsystem();
            ResetAllHandStates();
        }

        private void OnDestroy()
        {
            UnregisterSubsystem();
        }

        private void Update()
        {
            // If subsystem is not yet found or stopped, poll periodically
            if (_handSubsystem == null || !_handSubsystem.running)
            {
                _subsystemPollTimer += Time.unscaledDeltaTime;
                if (_subsystemPollTimer >= 1.0f)
                {
                    _subsystemPollTimer = 0f;
                    TryAcquireSubsystem();
                }
            }
        }

        public void ResolveReferences()
        {
            if (xrOrigin == null)
            {
                xrOrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            }
            if (puzzleManager == null)
            {
                puzzleManager = FindFirstObjectByType<PuzzleManager>(FindObjectsInactive.Include);
            }
            if (beamSystem == null)
            {
                beamSystem = FindFirstObjectByType<BeamSystem>(FindObjectsInactive.Include);
            }
        }

        #region XRHandSubsystem Lifecycle & Acquisition

        public void TryAcquireSubsystem()
        {
            if (_handSubsystem != null && _handSubsystem.running) return;

            s_Subsystems.Clear();
            SubsystemManager.GetSubsystems(s_Subsystems);
            for (int i = 0; i < s_Subsystems.Count; i++)
            {
                if (s_Subsystems[i].running)
                {
                    RegisterSubsystem(s_Subsystems[i]);
                    return;
                }
            }

            if (s_Subsystems.Count > 0)
            {
                RegisterSubsystem(s_Subsystems[0]);
            }
        }

        public void RegisterSubsystem(XRHandSubsystem subsystem)
        {
            if (_handSubsystem == subsystem) return;
            UnregisterSubsystem();

            _handSubsystem = subsystem;
            if (_handSubsystem != null)
            {
                _handSubsystem.updatedHands += OnUpdatedHands;
                _handSubsystem.trackingAcquired += OnTrackingAcquired;
                _handSubsystem.trackingLost += OnTrackingLost;
            }
        }

        public void UnregisterSubsystem()
        {
            if (_handSubsystem != null)
            {
                _handSubsystem.updatedHands -= OnUpdatedHands;
                _handSubsystem.trackingAcquired -= OnTrackingAcquired;
                _handSubsystem.trackingLost -= OnTrackingLost;
                _handSubsystem = null;
            }
        }

        private void OnTrackingAcquired(XRHand hand)
        {
            HandTrackingState state = GetHandState(hand.handedness);
            if (state != null)
            {
                state.IsTracked = true;
            }
        }

        private void OnTrackingLost(XRHand hand)
        {
            HandTrackingState state = GetHandState(hand.handedness);
            if (state != null)
            {
                HandleHandTrackingLost(state);
            }
        }

        private HandTrackingState GetHandState(Handedness handedness)
        {
            if (handedness == Handedness.Left) return _leftHandState;
            if (handedness == Handedness.Right) return _rightHandState;
            return null;
        }

        #endregion

        #region Joint Processing & Coordinate Mapping

        private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType updateType)
        {
            // Critical: process game logic exclusively during Dynamic update. Ignore BeforeRender to avoid duplicate work.
            if (updateType != XRHandSubsystem.UpdateType.Dynamic) return;

            ProcessHand(subsystem.rightHand, _rightHandState);
            ProcessHand(subsystem.leftHand, _leftHandState);
        }

        public void ProcessHand(XRHand hand, HandTrackingState state)
        {
            if (state == null) return;

            if (!hand.isTracked)
            {
                if (state.IsTracked)
                {
                    HandleHandTrackingLost(state);
                }
                return;
            }

            state.IsTracked = true;

            XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);

            if (!thumbTip.TryGetPose(out Pose thumbPose) || !indexTip.TryGetPose(out Pose indexPose))
            {
                // Incomplete pose data
                return;
            }

            // Verify tracking flags
            if ((thumbTip.trackingState & XRHandJointTrackingState.Pose) == 0 ||
                (indexTip.trackingState & XRHandJointTrackingState.Pose) == 0)
            {
                return;
            }

            // Transform tracking coordinates to world space exactly once
            Transform originTransform = GetTrackingOriginTransform();
            Vector3 worldThumbPos = originTransform != null ? originTransform.TransformPoint(thumbPose.position) : thumbPose.position;
            Vector3 worldIndexPos = originTransform != null ? originTransform.TransformPoint(indexPose.position) : indexPose.position;

            float distance = Vector3.Distance(worldThumbPos, worldIndexPos);
            Vector3 pinchCenter = (worldThumbPos + worldIndexPos) * 0.5f;

            EvaluatePinchState(state, distance, pinchCenter);
        }

        private Transform GetTrackingOriginTransform()
        {
            if (xrOrigin != null)
            {
                if (xrOrigin.CameraFloorOffsetObject != null)
                {
                    return xrOrigin.CameraFloorOffsetObject.transform;
                }
                return xrOrigin.transform;
            }
            return null;
        }

        #endregion

        #region Explicit Node Ownership & Conflict Resolution

        public bool IsNodeOwned(PuzzleNode node)
        {
            if (node == null) return false;
            return _nodeOwners.ContainsKey(node);
        }

        public bool TryGetNodeOwner(PuzzleNode node, out Handedness owner)
        {
            if (node == null)
            {
                owner = Handedness.Invalid;
                return false;
            }
            return _nodeOwners.TryGetValue(node, out owner);
        }

        public bool ClaimNodeOwnership(PuzzleNode node, Handedness hand)
        {
            if (node == null) return false;
            if (_nodeOwners.TryGetValue(node, out var currentOwner))
            {
                if (currentOwner != hand) return false; // Already owned by opposite hand
                return true;
            }
            _nodeOwners[node] = hand;
            return true;
        }

        public void ReleaseNodeOwnership(PuzzleNode node, Handedness hand)
        {
            if (node == null) return;
            if (_nodeOwners.TryGetValue(node, out var currentOwner) && currentOwner == hand)
            {
                _nodeOwners.Remove(node);
            }
        }

        public void ClearHandOwnership(Handedness hand)
        {
            List<PuzzleNode> toRemove = null;
            foreach (var kvp in _nodeOwners)
            {
                if (kvp.Value == hand)
                {
                    if (toRemove == null) toRemove = new List<PuzzleNode>();
                    toRemove.Add(kvp.Key);
                }
            }
            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    _nodeOwners.Remove(toRemove[i]);
                }
            }
        }

        public void ClearAllOwnership()
        {
            _nodeOwners.Clear();
        }

        #endregion

        #region Pinch State Machine & Hysteresis

        /// <summary>
        /// Evaluates thumb-index pinch using dual-threshold hysteresis and explicit ownership protection.
        /// </summary>
        public void EvaluatePinchState(HandTrackingState state, float distance, Vector3 pinchCenter)
        {
            state.LastPinchDistance = distance;
            state.LastPinchPosition = pinchCenter;

            if (!state.IsPinched)
            {
                // Enter pinch when distance <= engage threshold
                if (distance <= pinchEngageThreshold)
                {
                    state.IsPinched = true;

                    // Requirement 3: A hand cannot select another node while it is already holding one
                    if (state.HeldNode == null)
                    {
                        // Attempt to grab an optical node within proximity that is not owned by other hand
                        PuzzleNode nearest = FindNearestNode(pinchCenter, selectionRadius, state.Handedness);
                        if (nearest != null && ClaimNodeOwnership(nearest, state.Handedness))
                        {
                            state.HeldNode = nearest;
                            HandleMetaHandGrabStart(nearest, state.Handedness);
                        }
                    }
                }
            }
            else
            {
                // Exit pinch only when distance exceeds release threshold
                if (distance > pinchReleaseThreshold)
                {
                    state.IsPinched = false;
                    if (state.HeldNode != null)
                    {
                        PuzzleNode releasedNode = state.HeldNode;
                        ReleaseNodeOwnership(releasedNode, state.Handedness);
                        state.HeldNode = null;
                        HandleMetaHandGrabEnd(releasedNode, state.Handedness);
                    }
                }
                else
                {
                    // Pinch maintained: continuously update held node's position
                    if (state.HeldNode != null)
                    {
                        HandleMetaHandMove(state.HeldNode, pinchCenter);
                    }
                }
            }
        }

        public PuzzleNode FindNearestNode(Vector3 worldPoint, float maxRadius, Handedness requestingHand = Handedness.Invalid)
        {
            PuzzleNode nearest = null;
            float minSqrDist = maxRadius * maxRadius;

            // 1. Check nodes registered in PuzzleManager if available
            if (puzzleManager != null && puzzleManager.Nodes != null && puzzleManager.Nodes.Count > 0)
            {
                for (int i = 0; i < puzzleManager.Nodes.Count; i++)
                {
                    PuzzleNode n = puzzleManager.Nodes[i];
                    if (n == null) continue;

                    // Explicit ownership protection: cannot select node already owned by another hand
                    if (requestingHand != Handedness.Invalid && _nodeOwners.TryGetValue(n, out var owner) && owner != requestingHand)
                    {
                        continue;
                    }

                    float sqrDist = (n.transform.position - worldPoint).sqrMagnitude;
                    if (sqrDist <= minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        nearest = n;
                    }
                }
                if (nearest != null) return nearest;
            }

            // 2. Fallback to Physics overlap sphere with non-allocating buffer
            int hitCount = Physics.OverlapSphereNonAlloc(worldPoint, maxRadius, s_OverlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = s_OverlapBuffer[i];
                if (col == null) continue;

                PuzzleNode n = col.GetComponentInParent<PuzzleNode>();
                if (n != null)
                {
                    if (requestingHand != Handedness.Invalid && _nodeOwners.TryGetValue(n, out var owner) && owner != requestingHand)
                    {
                        continue;
                    }

                    float sqrDist = (n.transform.position - worldPoint).sqrMagnitude;
                    if (sqrDist <= minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        nearest = n;
                    }
                }
            }

            return nearest;
        }

        #endregion

        #region Tracking Loss & Event Hooks

        public void HandleHandTrackingLost(HandTrackingState state)
        {
            if (state == null) return;

            bool hadHeldNode = state.HeldNode != null;
            bool wasPinched = state.IsPinched;
            bool wasTracked = state.IsTracked;

            // Requirement 8: Repeated tracking loss does not produce duplicate release events
            if (!hadHeldNode && !wasPinched && !wasTracked)
            {
                return;
            }

            PuzzleNode releasedNode = state.HeldNode;
            state.IsTracked = false;
            state.IsPinched = false;
            state.HeldNode = null;

            if (releasedNode != null)
            {
                ReleaseNodeOwnership(releasedNode, state.Handedness);
                releasedNode.SetSelected(false);
            }

            HandTrackingState otherHand = (state.Handedness == Handedness.Left) ? _rightHandState : _leftHandState;

            if (otherHand.HeldNode != null)
            {
                // Other hand continues dragging its node
                _selectedNode = otherHand.HeldNode;
                _isDragging = true;
                if (hadHeldNode)
                {
                    OnNodeReleased?.Invoke();
                }
            }
            else
            {
                // Neither hand holds any node
                _selectedNode = null;
                if (_isDragging || hadHeldNode)
                {
                    _isDragging = false;
                    OnNodeReleased?.Invoke();
                }
                if (_hoveredNode != null)
                {
                    SetHoveredNode(null);
                }
                OnDeselectRequested?.Invoke();
            }
        }

        public void HandleMetaHandGrabStart(PuzzleNode node, Handedness hand = Handedness.Right)
        {
            if (node == null) return;

            _selectedNode = node;
            _selectedNode.SetSelected(true);
            _isDragging = true;
            OnNodeSelected?.Invoke(_selectedNode);
        }

        public void HandleMetaHandGrabStart(PuzzleNode node)
        {
            HandleMetaHandGrabStart(node, Handedness.Right);
        }

        public void HandleMetaHandMove(PuzzleNode node, Vector3 handWorldPosition)
        {
            if (node == null) return;
            node.MoveToClamped(handWorldPosition);

            if (node == _selectedNode)
            {
                OnNodeDragPositionUpdated?.Invoke(handWorldPosition);
            }
            else if (beamSystem != null)
            {
                beamSystem.RecalculateBeam();
            }
        }

        public void HandleMetaHandMove(Vector3 handWorldPosition)
        {
            if (_selectedNode != null)
            {
                HandleMetaHandMove(_selectedNode, handWorldPosition);
            }
        }

        public void HandleMetaHandRotate(float angleDeltaDegrees)
        {
            if (_selectedNode == null || !enableWristTurnRotation) return;
            OnNodeRotationDelta?.Invoke(angleDeltaDegrees * rotationScaleMultiplier);
        }

        public void HandleMetaHandGrabEnd(PuzzleNode node, Handedness hand)
        {
            if (node != null)
            {
                node.SetSelected(false);
            }

            HandTrackingState otherHand = (hand == Handedness.Left) ? _rightHandState : _leftHandState;

            if (otherHand.HeldNode != null)
            {
                // Other hand is still holding a node
                _selectedNode = otherHand.HeldNode;
                _isDragging = true;
                OnNodeReleased?.Invoke();
            }
            else
            {
                // No hand is holding any node
                _selectedNode = null;
                _isDragging = false;
                OnNodeReleased?.Invoke();
            }
        }

        public void HandleMetaHandGrabEnd()
        {
            PuzzleNode nodeToRelease = _selectedNode;
            Handedness hand = Handedness.Right;
            if (_leftHandState.HeldNode != null)
            {
                nodeToRelease = _leftHandState.HeldNode;
                hand = Handedness.Left;
            }
            else if (_rightHandState.HeldNode != null)
            {
                nodeToRelease = _rightHandState.HeldNode;
                hand = Handedness.Right;
            }
            HandleMetaHandGrabEnd(nodeToRelease, hand);
        }

        public void ResetAllHandStates()
        {
            if (_leftHandState.HeldNode != null)
            {
                _leftHandState.HeldNode.SetSelected(false);
            }
            if (_rightHandState.HeldNode != null)
            {
                _rightHandState.HeldNode.SetSelected(false);
            }
            _leftHandState.Reset();
            _rightHandState.Reset();
            ClearAllOwnership();

            if (_selectedNode != null)
            {
                _selectedNode.SetSelected(false);
                _selectedNode = null;
            }

            if (_isDragging)
            {
                _isDragging = false;
                OnNodeReleased?.Invoke();
            }
            _hoveredNode = null;
        }

        public void HandleMetaTrackingLost()
        {
            ResetAllHandStates();
        }

        public void HandleMetaHandResetTrigger()
        {
            OnResetRequested?.Invoke();
        }

        public void SelectNode(PuzzleNode node)
        {
            if (node == null) return;
            ClaimNodeOwnership(node, Handedness.Right);
            _rightHandState.HeldNode = node;
            _rightHandState.IsPinched = true;
            _rightHandState.IsTracked = true;
            HandleMetaHandGrabStart(node, Handedness.Right);
        }

        public void DeselectCurrentNode()
        {
            ResetAllHandStates();
            OnDeselectRequested?.Invoke();
        }

        public void SetHoveredNode(PuzzleNode node)
        {
            if (_hoveredNode != node)
            {
                if (_hoveredNode != null && _hoveredNode != _selectedNode)
                {
                    _hoveredNode.SetHovered(false);
                }
                _hoveredNode = node;
                if (_hoveredNode != null)
                {
                    _hoveredNode.SetHovered(true);
                }
            }
        }

        #endregion

        #region Test Injection API (Automated UTF Testing)

        /// <summary>
        /// Injects simulated tracked joint poses for automated PlayMode and EditMode tests.
        /// Allows deterministic testing of pinch engage, release, hysteresis, coordinate mapping, and grab start.
        /// </summary>
        public void SimulateTrackedJoints(Handedness handedness, Pose thumbPose, Pose indexPose, bool isTracked = true, bool isWorldSpace = true)
        {
            HandTrackingState state = GetHandState(handedness);
            if (state == null) return;

            if (!isTracked)
            {
                HandleHandTrackingLost(state);
                return;
            }

            state.IsTracked = true;

            Vector3 worldThumbPos;
            Vector3 worldIndexPos;

            if (isWorldSpace)
            {
                worldThumbPos = thumbPose.position;
                worldIndexPos = indexPose.position;
            }
            else
            {
                Transform originTransform = GetTrackingOriginTransform();
                worldThumbPos = originTransform != null ? originTransform.TransformPoint(thumbPose.position) : thumbPose.position;
                worldIndexPos = originTransform != null ? originTransform.TransformPoint(indexPose.position) : indexPose.position;
            }

            float distance = Vector3.Distance(worldThumbPos, worldIndexPos);
            Vector3 pinchCenter = (worldThumbPos + worldIndexPos) * 0.5f;

            EvaluatePinchState(state, distance, pinchCenter);
        }

        /// <summary>
        /// Simulates tracking loss for a specific hand in automated tests.
        /// </summary>
        public void SimulateTrackingLost(Handedness handedness)
        {
            HandTrackingState state = GetHandState(handedness);
            if (state != null)
            {
                HandleHandTrackingLost(state);
            }
        }

        #endregion
    }
}
