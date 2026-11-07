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

        private IInputProvider _inputProvider;
        private PuzzleNode _activeNode;

        private void Awake()
        {
            ResolveInputProvider();
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
            _inputProvider.OnDeselectRequested += HandleDeselect;
        }

        private void UnsubscribeFromInput()
        {
            if (_inputProvider == null) return;

            _inputProvider.OnNodeSelected -= HandleNodeSelected;
            _inputProvider.OnNodeDragPositionUpdated -= HandleNodeMoved;
            _inputProvider.OnNodeRotationDelta -= HandleNodeRotated;
            _inputProvider.OnNodeReleased -= HandleNodeReleased;
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
                _activeNode = null;
            }

            if (beamSystem != null)
            {
                beamSystem.RecalculateBeam();
            }
        }

        private void HandleDeselect()
        {
            _activeNode = null;
        }
    }
}

