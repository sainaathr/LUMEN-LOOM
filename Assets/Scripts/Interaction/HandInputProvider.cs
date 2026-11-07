using System;
using UnityEngine;
using LumenLoom.Puzzle;

namespace LumenLoom.Interaction
{
    /// <summary>
    /// Architectural bridge for future Meta Quest Hand-Tracking integration.
    /// Implements IInputProvider to map Meta Interaction SDK hand events (Pinch, Grab, Wrist-Turn, Distance-Ray)
    /// into standard game-level interaction actions without coupling the core puzzle engine to proprietary APIs.
    /// </summary>
    public class HandInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Hand Interaction Configuration")]
        [SerializeField] private float pinchThreshold = 0.75f;
        [SerializeField] private float rotationScaleMultiplier = 1.0f;
        [SerializeField] private bool enableWristTurnRotation = true;

        [Header("Debug Simulation (Editor / Play Mode)")]
        [SerializeField] private bool simulateHandWithKeys = false;

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

        /// <summary>
        /// Hook invoked by Meta XR / Oculus Interaction SDK GrabInteractable or RayInteractable.
        /// </summary>
        public void HandleMetaHandGrabStart(PuzzleNode node)
        {
            if (node == null) return;

            if (_selectedNode != null && _selectedNode != node)
            {
                _selectedNode.SetSelected(false);
            }

            _selectedNode = node;
            _selectedNode.SetSelected(true);
            _isDragging = true;
            OnNodeSelected?.Invoke(_selectedNode);
        }

        /// <summary>
        /// Hook invoked continuously while a hand is actively holding / pinching a node.
        /// Supplies the world position of the pinch anchor or hand palm.
        /// </summary>
        public void HandleMetaHandMove(Vector3 handWorldPosition)
        {
            if (!_isDragging || _selectedNode == null) return;
            OnNodeDragPositionUpdated?.Invoke(handWorldPosition);
        }

        /// <summary>
        /// Hook invoked when hand wrist rolls or gesture rotates.
        /// </summary>
        public void HandleMetaHandRotate(float angleDeltaDegrees)
        {
            if (_selectedNode == null) return;
            OnNodeRotationDelta?.Invoke(angleDeltaDegrees * rotationScaleMultiplier);
        }

        /// <summary>
        /// Hook invoked when hand unpinches / releases grab.
        /// </summary>
        public void HandleMetaHandGrabEnd()
        {
            if (!_isDragging) return;

            _isDragging = false;
            OnNodeReleased?.Invoke();
        }

        /// <summary>
        /// Hook invoked by a two-handed palm clap or spatial UI button for reset.
        /// </summary>
        public void HandleMetaHandResetTrigger()
        {
            OnResetRequested?.Invoke();
        }

        public void SelectNode(PuzzleNode node)
        {
            HandleMetaHandGrabStart(node);
        }

        public void DeselectCurrentNode()
        {
            if (_selectedNode != null)
            {
                _selectedNode.SetSelected(false);
                _selectedNode = null;
            }
            _isDragging = false;
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
    }
}
