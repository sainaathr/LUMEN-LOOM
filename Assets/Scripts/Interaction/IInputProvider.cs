using System;
using UnityEngine;
using LumenLoom.Puzzle;

namespace LumenLoom.Interaction
{
    /// <summary>
    /// Abstract interface for game inputs. Decouples mouse/keyboard simulation from VR hand-tracking interactions.
    /// Both MouseInputProvider and future HandInputProvider (Meta XR SDK) implement this contract.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Fired when an interactive node is picked/selected.
        /// </summary>
        event Action<PuzzleNode> OnNodeSelected;

        /// <summary>
        /// Fired continuously while dragging a node to provide a target 3D world position.
        /// </summary>
        event Action<Vector3> OnNodeDragPositionUpdated;

        /// <summary>
        /// Fired when a rotation delta (degrees around the Y axis) is applied.
        /// </summary>
        event Action<float> OnNodeRotationDelta;

        /// <summary>
        /// Fired when the active node is released / ungrabbed.
        /// </summary>
        event Action OnNodeReleased;

        /// <summary>
        /// Fired when the player requests puzzle reset (e.g., 'R' key or gesture).
        /// </summary>
        event Action OnResetRequested;

        /// <summary>
        /// Fired when player requests deselection without action (e.g., Escape).
        /// </summary>
        event Action OnDeselectRequested;

        /// <summary>
        /// True if an active grab / drag is currently underway.
        /// </summary>
        bool IsDragging { get; }

        /// <summary>
        /// The node currently under hover / point.
        /// </summary>
        PuzzleNode HoveredNode { get; }

        /// <summary>
        /// The currently selected node, if any.
        /// </summary>
        PuzzleNode SelectedNode { get; }

        /// <summary>
        /// Programmatically selects a node.
        /// </summary>
        void SelectNode(PuzzleNode node);

        /// <summary>
        /// Clears the currently selected node.
        /// </summary>
        void DeselectCurrentNode();
    }
}

