using UnityEngine;
using LumenLoom.Puzzle;
using LumenLoom.Rendering;
using LumenLoom.Audio;

namespace LumenLoom.Interaction
{
    /// <summary>
    /// Mediates between the active IInputProvider and interactive puzzle nodes.
    /// Handles movement clamping, continuous beam updates during transformation, and audio feedback.
    /// </summary>
    public class NodeInteractionController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private MonoBehaviour inputProviderComponent;
        [SerializeField] private BeamSystem beamSystem;
        [SerializeField] private AudioFeedbackManager audioManager;
        [SerializeField] private PuzzleManager puzzleManager;

        private IInputProvider _inputProvider;
        private PuzzleNode _activeNode;

        public PuzzleNode ActiveNode => _activeNode;
        public IInputProvider InputProvider => _inputProvider;

        private void Awake()
        {
            ResolveInputProvider();
            if (puzzleManager == null)
            {
                puzzleManager = FindFirstObjectByType<PuzzleManager>();
            }
            if (beamSystem == null)
            {
                beamSystem = FindFirstObjectByType<BeamSystem>();
            }
            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<AudioFeedbackManager>();
            }
        }

        private void OnEnable()
        {
            SubscribeToInput();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
        }

        public void SetInputProvider(IInputProvider provider)
        {
            UnsubscribeFromInput();
            _inputProvider = provider;
            inputProviderComponent = provider as MonoBehaviour;
            SubscribeToInput();
        }

        private void ResolveInputProvider()
        {
            if (inputProviderComponent != null && inputProviderComponent is IInputProvider provider)
            {
                _inputProvider = provider;
            }
            else
            {
                _inputProvider = GetComponent<IInputProvider>();
                if (_inputProvider == null)
                {
                    _inputProvider = FindFirstObjectByType<MouseInputProvider>();
                }
            }
        }

        private void SubscribeToInput()
        {
            if (_inputProvider == null) return;

            _inputProvider.OnNodeSelected += HandleNodeSelected;
            _inputProvider.OnNodeDragPositionUpdated += HandleNodeMoved;
            _inputProvider.OnNodeRotationDelta += HandleNodeRotated;
            _inputProvider.OnNodeReleased += HandleNodeReleased;
            _inputProvider.OnResetRequested += HandleResetRequested;
            _inputProvider.OnDeselectRequested += HandleDeselect;
        }

        private void UnsubscribeFromInput()
        {
            if (_inputProvider == null) return;

            _inputProvider.OnNodeSelected -= HandleNodeSelected;
            _inputProvider.OnNodeDragPositionUpdated -= HandleNodeMoved;
            _inputProvider.OnNodeRotationDelta -= HandleNodeRotated;
            _inputProvider.OnNodeReleased -= HandleNodeReleased;
            _inputProvider.OnResetRequested -= HandleResetRequested;
            _inputProvider.OnDeselectRequested -= HandleDeselect;
        }

        private void HandleNodeSelected(PuzzleNode node)
        {
            _activeNode = node;
            if (audioManager != null)
            {
                audioManager.PlayNodeSelectSound();
            }
        }

        private void HandleNodeMoved(Vector3 targetWorldPosition)
        {
            if (_activeNode == null) return;

            _activeNode.MoveToClamped(targetWorldPosition);

            if (beamSystem != null)
            {
                beamSystem.RecalculateBeam();
            }
        }

        private void HandleNodeRotated(float deltaDegrees)
        {
            if (_activeNode == null) return;

            _activeNode.RotateNode(deltaDegrees);

            if (beamSystem != null)
            {
                beamSystem.RecalculateBeam();
            }
        }

        private void HandleNodeReleased()
        {
            if (_activeNode != null)
            {
                if (audioManager != null)
                {
                    audioManager.PlayNodeReleaseSound();
                }
                // Retain _activeNode so rotation via keyboard (Q/E) or scroll wheel works seamlessly while selected
            }

            if (beamSystem != null)
            {
                beamSystem.RecalculateBeam();
            }
        }

        private void HandleResetRequested()
        {
            _activeNode = null;
            if (puzzleManager != null)
            {
                puzzleManager.ResetPuzzle();
            }
        }

        private void HandleDeselect()
        {
            if (_activeNode != null)
            {
                _activeNode.SetSelected(false);
                _activeNode = null;
            }
        }
    }
}

