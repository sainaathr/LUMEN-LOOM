using System;
using System.Collections.Generic;
using UnityEngine;
using LumenLoom.Data;

namespace LumenLoom.Puzzle
{
    /// <summary>
    /// Manages puzzle chamber progression and level lifecycle.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Chamber Sequence")]
        [SerializeField] private List<LevelData> levelSequence = new List<LevelData>();
        [SerializeField] private int currentLevelIndex = 0;
        [SerializeField] private PuzzleManager puzzleManager;

        public LevelData CurrentLevel => (levelSequence != null && currentLevelIndex >= 0 && currentLevelIndex < levelSequence.Count) 
            ? levelSequence[currentLevelIndex] 
            : null;

        public int CurrentLevelIndex => currentLevelIndex;
        public int TotalLevels => levelSequence != null ? levelSequence.Count : 0;

        public event Action<LevelData> OnLevelLoaded;

        private void Start()
        {
            if (puzzleManager == null)
            {
                puzzleManager = FindFirstObjectByType<PuzzleManager>();
            }

            if (CurrentLevel != null)
            {
                OnLevelLoaded?.Invoke(CurrentLevel);
            }
        }

        public void NextLevel()
        {
            if (levelSequence == null || levelSequence.Count == 0) return;

            currentLevelIndex = (currentLevelIndex + 1) % levelSequence.Count;
            LoadLevel(currentLevelIndex);
        }

        public void LoadLevel(int index)
        {
            if (levelSequence == null || index < 0 || index >= levelSequence.Count) return;

            currentLevelIndex = index;
            LevelData data = levelSequence[currentLevelIndex];

            if (puzzleManager != null)
            {
                puzzleManager.ResetPuzzle();
            }

            OnLevelLoaded?.Invoke(data);
        }
    }
}
