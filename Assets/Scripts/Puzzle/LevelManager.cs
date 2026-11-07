using System;
using System.Collections.Generic;
using UnityEngine;
using LumenLoom.Data;
using LumenLoom.Rendering;

namespace LumenLoom.Puzzle
{
    /// <summary>
    /// Manages puzzle chamber progression, dynamic level instantiation, and multi-chamber lifecycle.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Chamber Sequence")]
        [SerializeField] private List<LevelData> levelSequence = new List<LevelData>();
        [SerializeField] private int currentLevelIndex = 0;
        [SerializeField] private bool autoAdvanceOnSolve = false;
        [SerializeField] private float advanceDelay = 2.0f;

        [Header("System References")]
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private BeamSystem beamSystem;
        [SerializeField] private BeamValidator beamValidator;

        private readonly List<GameObject> _spawnedObstacles = new List<GameObject>();
        private float _solveTime = -1f;

        public LevelData CurrentLevel => (levelSequence != null && currentLevelIndex >= 0 && currentLevelIndex < levelSequence.Count) 
            ? levelSequence[currentLevelIndex] 
            : null;

        public int CurrentLevelIndex => currentLevelIndex;
        public int TotalLevels => levelSequence != null ? levelSequence.Count : 0;
        public bool HasNextLevel => currentLevelIndex < TotalLevels - 1;

        public event Action<LevelData> OnLevelLoaded;
        public event Action OnAllChambersCompleted;

        private void Awake()
        {
            if (puzzleManager == null)
            {
                puzzleManager = FindFirstObjectByType<PuzzleManager>();
            }

            if (beamSystem == null)
            {
                beamSystem = FindFirstObjectByType<BeamSystem>();
            }

            if (beamValidator == null)
            {
                beamValidator = FindFirstObjectByType<BeamValidator>();
            }

            EnsureBuiltInLevels();
        }

        private void Start()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnPuzzleCompleted += HandlePuzzleSolved;
            }

            if (levelSequence.Count > 0)
            {
                LoadLevel(currentLevelIndex);
            }
        }

        private void OnDestroy()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnPuzzleCompleted -= HandlePuzzleSolved;
            }
        }

        private void Update()
        {
            if (autoAdvanceOnSolve && _solveTime > 0 && Time.time - _solveTime >= advanceDelay)
            {
                _solveTime = -1f;
                NextLevel();
            }
        }

        private void HandlePuzzleSolved()
        {
            _solveTime = Time.time;
        }

        public void NextLevel()
        {
            if (currentLevelIndex < TotalLevels - 1)
            {
                LoadLevel(currentLevelIndex + 1);
            }
            else
            {
                OnAllChambersCompleted?.Invoke();
                Debug.Log("[LUMEN LOOM] All puzzle chambers completed!");
            }
        }

        public void PreviousLevel()
        {
            if (currentLevelIndex > 0)
            {
                LoadLevel(currentLevelIndex - 1);
            }
        }

        public void LoadLevel(int index)
        {
            if (levelSequence == null || index < 0 || index >= levelSequence.Count) return;

            currentLevelIndex = index;
            LevelData data = levelSequence[currentLevelIndex];
            _solveTime = -1f;

            ApplyChamberLayout(data);

            if (puzzleManager != null)
            {
                puzzleManager.ResetPuzzle();
            }

            OnLevelLoaded?.Invoke(data);
        }

        /// <summary>
        /// Configures chamber geometry, obstacles, source, and target according to the LevelData definition.
        /// </summary>
        public void ApplyChamberLayout(LevelData data)
        {
            if (data == null) return;

            // 1. Position Energy Source
            if (puzzleManager != null && puzzleManager.Source != null)
            {
                puzzleManager.Source.transform.position = data.sourcePosition;
                puzzleManager.Source.transform.forward = data.sourceDirection;
            }

            // 2. Position Target Receiver
            if (puzzleManager != null && puzzleManager.Target != null)
            {
                puzzleManager.Target.transform.position = data.targetPosition;
                puzzleManager.Target.transform.forward = data.targetForward;
            }

            // 3. Clear existing spawned obstacles
            foreach (var obs in _spawnedObstacles)
            {
                if (obs != null) Destroy(obs);
            }
            _spawnedObstacles.Clear();

            // 4. Spawn new obstacles
            if (data.obstacles != null)
            {
                for (int i = 0; i < data.obstacles.Count; i++)
                {
                    ObstacleConfig cfg = data.obstacles[i];
                    GameObject obsObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obsObj.name = $"Obstacle_{i + 1}";
                    obsObj.transform.position = cfg.position;
                    obsObj.transform.localScale = cfg.size;

                    // Tag and layer
                    obsObj.tag = "Obstacle";
                    obsObj.AddComponent<Obstacle>();

                    _spawnedObstacles.Add(obsObj);
                }
            }

            // 5. Configure interactive nodes
            if (puzzleManager != null && data.initialNodes != null && data.initialNodes.Count > 0)
            {
                var sceneNodes = puzzleManager.Nodes;
                for (int i = 0; i < data.initialNodes.Count && i < sceneNodes.Count; i++)
                {
                    var cfg = data.initialNodes[i];
                    var node = sceneNodes[i];
                    if (node != null)
                    {
                        node.transform.position = cfg.position;
                        node.transform.rotation = Quaternion.Euler(cfg.rotationEuler);
                        node.ConfigureBounds(data.xBounds, data.zBounds, data.puzzlePlaneY);
                    }
                }
            }

            // 6. Refresh Beam System
            if (beamSystem != null)
            {
                beamSystem.RefreshSceneNodes();
                beamSystem.RecalculateBeam();
            }
        }

        /// <summary>
        /// Populates default progression chambers if no serialized LevelData assets are loaded.
        /// </summary>
        private void EnsureBuiltInLevels()
        {
            if (levelSequence == null)
            {
                levelSequence = new List<LevelData>();
            }

            if (levelSequence.Count == 0)
            {
                // Chamber 01: Divergence
                LevelData ch1 = ScriptableObject.CreateInstance<LevelData>();
                ch1.levelTitle = "Chamber 01: Divergence";
                ch1.objectiveText = "Align the optical nodes to guide the energy beam into the receiver.";
                ch1.sourcePosition = new Vector3(-3.5f, 0.5f, -3.0f);
                ch1.sourceDirection = Vector3.forward;
                ch1.targetPosition = new Vector3(2.5f, 0.5f, 3.0f);
                ch1.targetForward = Vector3.back;
                ch1.minNodesRequired = 2;
                ch1.initialNodes = new List<NodeSpawnConfig>
                {
                    new NodeSpawnConfig { nodeId = "Node_Alpha", position = new Vector3(-3.5f, 0.5f, 1.0f), rotationEuler = new Vector3(0, 45f, 0), allowMovement = true, allowRotation = true },
                    new NodeSpawnConfig { nodeId = "Node_Beta", position = new Vector3(0.0f, 0.5f, 1.0f), rotationEuler = new Vector3(0, 135f, 0), allowMovement = true, allowRotation = true }
                };
                levelSequence.Add(ch1);

                // Chamber 02: Occlusion Barrier
                LevelData ch2 = ScriptableObject.CreateInstance<LevelData>();
                ch2.levelTitle = "Chamber 02: Occlusion Barrier";
                ch2.objectiveText = "Route the light around the central absorbing barrier.";
                ch2.sourcePosition = new Vector3(-3.5f, 0.5f, -3.0f);
                ch2.sourceDirection = Vector3.forward;
                ch2.targetPosition = new Vector3(3.5f, 0.5f, -3.0f);
                ch2.targetForward = Vector3.forward;
                ch2.minNodesRequired = 2;
                ch2.initialNodes = new List<NodeSpawnConfig>
                {
                    new NodeSpawnConfig { nodeId = "Node_Alpha", position = new Vector3(-3.5f, 0.5f, 2.5f), rotationEuler = new Vector3(0, 0f, 0), allowMovement = true, allowRotation = true },
                    new NodeSpawnConfig { nodeId = "Node_Beta", position = new Vector3(3.5f, 0.5f, 2.5f), rotationEuler = new Vector3(0, 180f, 0), allowMovement = true, allowRotation = true }
                };
                ch2.obstacles = new List<ObstacleConfig>
                {
                    new ObstacleConfig { position = new Vector3(0f, 0.5f, 0f), size = new Vector3(3.5f, 1.2f, 0.8f) }
                };
                levelSequence.Add(ch2);

                // Chamber 03: Specular Corridor
                LevelData ch3 = ScriptableObject.CreateInstance<LevelData>();
                ch3.levelTitle = "Chamber 03: Specular Corridor";
                ch3.objectiveText = "Navigate a zigzag corridor requiring precise orthogonal alignment.";
                ch3.sourcePosition = new Vector3(-3.5f, 0.5f, -3.5f);
                ch3.sourceDirection = Vector3.forward;
                ch3.targetPosition = new Vector3(-3.5f, 0.5f, 3.5f);
                ch3.targetForward = Vector3.back;
                ch3.minNodesRequired = 2;
                ch3.initialNodes = new List<NodeSpawnConfig>
                {
                    new NodeSpawnConfig { nodeId = "Node_Alpha", position = new Vector3(-3.5f, 0.5f, -0.5f), rotationEuler = new Vector3(0, 90f, 0), allowMovement = true, allowRotation = true },
                    new NodeSpawnConfig { nodeId = "Node_Beta", position = new Vector3(1.5f, 0.5f, -0.5f), rotationEuler = new Vector3(0, 0f, 0), allowMovement = true, allowRotation = true }
                };
                ch3.obstacles = new List<ObstacleConfig>
                {
                    new ObstacleConfig { position = new Vector3(-3.5f, 0.5f, 1.5f), size = new Vector3(1.5f, 1.2f, 0.5f) },
                    new ObstacleConfig { position = new Vector3(0f, 0.5f, -2.0f), size = new Vector3(2.5f, 1.2f, 0.5f) }
                };
                levelSequence.Add(ch3);
            }
        }
    }
}
