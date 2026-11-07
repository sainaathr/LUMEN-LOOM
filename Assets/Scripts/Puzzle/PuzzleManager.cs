using System;
using System.Collections.Generic;
using UnityEngine;
using LumenLoom.Rendering;
using LumenLoom.Audio;

namespace LumenLoom.Puzzle
{
    /// <summary>
    /// Central gameplay coordinator for active puzzle chambers.
    /// Manages node lifecycle, reset operations, and coordinates validation results.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        [Header("Chamber References")]
        [SerializeField] private EnergySource energySource;
        [SerializeField] private TargetReceiver targetReceiver;
        [SerializeField] private List<PuzzleNode> puzzleNodes = new List<PuzzleNode>();
        [SerializeField] private BeamSystem beamSystem;
        [SerializeField] private BeamValidator beamValidator;
        [SerializeField] private AudioFeedbackManager audioManager;

        [Header("Puzzle Settings")]
        [SerializeField] private Vector2 defaultXBounds = new Vector2(-4.5f, 4.5f);
        [SerializeField] private Vector2 defaultZBounds = new Vector2(-4.5f, 4.5f);
        [SerializeField] private float planeY = 0.5f;

        private bool _isSolved;

        public bool IsSolved => _isSolved;
        public List<PuzzleNode> Nodes => puzzleNodes;
        public EnergySource Source => energySource;
        public TargetReceiver Target => targetReceiver;
        public BeamValidator Validator => beamValidator;

        public event Action OnPuzzleCompleted;
        public event Action OnPuzzleReset;

        private void Awake()
        {
            if (puzzleNodes == null || puzzleNodes.Count == 0)
            {
                puzzleNodes = new List<PuzzleNode>(FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None));
            }

            ConfigureNodeBoundaries();
        }

        private void Start()
        {
            if (beamValidator != null)
            {
                beamValidator.OnPuzzleSolved += HandlePuzzleSolved;
                beamValidator.OnPuzzleUnsolved += HandlePuzzleUnsolved;
            }

            // Initial calculation
            if (beamSystem != null)
            {
                beamSystem.RecalculateBeam();
            }
        }

        private void OnDestroy()
        {
            if (beamValidator != null)
            {
                beamValidator.OnPuzzleSolved -= HandlePuzzleSolved;
                beamValidator.OnPuzzleUnsolved -= HandlePuzzleUnsolved;
            }
        }

        private void ConfigureNodeBoundaries()
        {
            foreach (var node in puzzleNodes)
            {
                if (node != null)
                {
                    node.ConfigureBounds(defaultXBounds, defaultZBounds, planeY);
                }
            }
        }

        public void ResetPuzzle()
        {
            _isSolved = false;

            foreach (var node in puzzleNodes)
            {
                if (node != null)
                {
                    node.ResetNode();
                }
            }

            if (targetReceiver != null)
            {
                targetReceiver.ResetReceiver();
            }

            if (beamValidator != null)
            {
                beamValidator.ResetValidation();
            }

            if (beamSystem != null)
            {
                beamSystem.RecalculateBeam();
            }

            if (audioManager != null)
            {
                audioManager.PlayResetSound();
            }

            OnPuzzleReset?.Invoke();
        }

        private void HandlePuzzleSolved()
        {
            if (_isSolved) return;
            _isSolved = true;

            if (audioManager != null)
            {
                audioManager.PlayTargetConnectedSound();
            }

            OnPuzzleCompleted?.Invoke();
        }

        private void HandlePuzzleUnsolved()
        {
            _isSolved = false;
        }

        public void RegisterNode(PuzzleNode node)
        {
            if (!puzzleNodes.Contains(node))
            {
                puzzleNodes.Add(node);
                node.ConfigureBounds(defaultXBounds, defaultZBounds, planeY);
                if (beamSystem != null)
                {
                    beamSystem.RefreshSceneNodes();
                    beamSystem.RecalculateBeam();
                }
            }
        }
    }
}

