using System;
using UnityEngine;
using LumenLoom.Puzzle;
using LumenLoom.Interaction;

namespace LumenLoom.Core
{
    public enum GameState
    {
        Initializing,
        Playing,
        ChamberSolved,
        Paused
    }

    /// <summary>
    /// Master coordinator for LUMEN LOOM application lifecycle and game states.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Initializing;

        [Header("Subsystems")]
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private NodeInteractionController interactionController;

        public GameState CurrentState => currentState;

        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (puzzleManager == null)
            {
                puzzleManager = FindFirstObjectByType<PuzzleManager>();
            }

            if (puzzleManager != null)
            {
                puzzleManager.OnPuzzleCompleted += HandlePuzzleCompleted;
                puzzleManager.OnPuzzleReset += HandlePuzzleReset;
            }

            SetState(GameState.Playing);
        }

        private void OnDestroy()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnPuzzleCompleted -= HandlePuzzleCompleted;
                puzzleManager.OnPuzzleReset -= HandlePuzzleReset;
            }
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnGameStateChanged?.Invoke(currentState);
        }

        private void HandlePuzzleCompleted()
        {
            SetState(GameState.ChamberSolved);
            Debug.Log("[LUMEN LOOM] Chamber successfully solved! Target connected.");
        }

        private void HandlePuzzleReset()
        {
            SetState(GameState.Playing);
            Debug.Log("[LUMEN LOOM] Chamber reset to initial state.");
        }
    }
}
