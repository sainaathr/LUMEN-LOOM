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
        [SerializeField] private int unlockedLevelIndex = 0;
        [SerializeField] private bool autoAdvanceOnSolve = false;
        [SerializeField] private float advanceDelay = 2.0f;

        [Header("System References")]
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private BeamSystem beamSystem;
        [SerializeField] private BeamValidator beamValidator;

        private readonly List<GameObject> _spawnedObstacles = new List<GameObject>();
        private TargetReceiver _secondaryTarget;
        private float _solveTime = -1f;

        public LevelData CurrentLevel => (levelSequence != null && currentLevelIndex >= 0 && currentLevelIndex < levelSequence.Count) 
            ? levelSequence[currentLevelIndex] 
            : null;

        public int CurrentLevelIndex => currentLevelIndex;
        public int UnlockedLevelIndex => unlockedLevelIndex;
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
                LoadLevel(currentLevelIndex, true);
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
            if (currentLevelIndex + 1 > unlockedLevelIndex && currentLevelIndex + 1 < TotalLevels)
            {
                unlockedLevelIndex = currentLevelIndex + 1;
                Debug.Log($"[LUMEN LOOM] Chamber {unlockedLevelIndex + 1} unlocked!");
            }
        }

        public bool IsLevelUnlocked(int index)
        {
            return index >= 0 && index <= unlockedLevelIndex && index < TotalLevels;
        }

        public void NextLevel()
        {
            if (currentLevelIndex < TotalLevels - 1 && IsLevelUnlocked(currentLevelIndex + 1))
            {
                LoadLevel(currentLevelIndex + 1);
            }
            else if (currentLevelIndex == TotalLevels - 1)
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

        public void LoadLevel(int index, bool forceUnlock = false)
        {
            if (levelSequence == null || index < 0 || index >= levelSequence.Count) return;

            if (forceUnlock && index > unlockedLevelIndex)
            {
                unlockedLevelIndex = index;
            }

            if (!IsLevelUnlocked(index) && !forceUnlock)
            {
                Debug.LogWarning($"[LUMEN LOOM] Chamber {index + 1} is locked! Complete preceding chambers first.");
                return;
            }

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

            // 2. Position Target Receiver(s)
            if (puzzleManager != null && puzzleManager.Target != null)
            {
                puzzleManager.Target.transform.position = data.targetPosition;
                puzzleManager.Target.transform.forward = data.targetForward;
            }

            // Handle Secondary Target (Chamber 03 Dual-Target Support)
            if (data.useDualTargets)
            {
                if (_secondaryTarget == null)
                {
                    _secondaryTarget = FindSecondaryTarget();
                    if (_secondaryTarget == null)
                    {
                        _secondaryTarget = CreateSecondaryTarget();
                    }
                }
                _secondaryTarget.gameObject.SetActive(true);
                _secondaryTarget.transform.position = data.target2Position;
                _secondaryTarget.transform.forward = data.target2Forward;

                if (beamValidator != null && puzzleManager != null)
                {
                    beamValidator.SetRequiredTargets(new[] { puzzleManager.Target, _secondaryTarget });
                }
            }
            else
            {
                if (_secondaryTarget != null)
                {
                    _secondaryTarget.gameObject.SetActive(false);
                }
                if (beamValidator != null && puzzleManager != null)
                {
                    beamValidator.SetTarget(puzzleManager.Target);
                }
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

                    // Obsidian / dark metallic styling
                    Material obsMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    obsMat.color = new Color(0.12f, 0.13f, 0.16f, 1f);
                    obsObj.GetComponent<MeshRenderer>().material = obsMat;

                    obsObj.tag = "Obstacle";
                    obsObj.AddComponent<Obstacle>();

                    _spawnedObstacles.Add(obsObj);
                }
            }

            // 5. Configure interactive nodes
            if (puzzleManager != null && data.initialNodes != null && data.initialNodes.Count > 0)
            {
                puzzleManager.FindSceneEntities();
                var sceneNodes = new List<PuzzleNode>(puzzleManager.Nodes);

                // Dynamically spawn additional nodes if chamber requires more
                while (sceneNodes.Count < data.initialNodes.Count)
                {
                    PuzzleNode newNode = CreateAdditionalNode($"Node_{sceneNodes.Count + 1}");
                    sceneNodes.Add(newNode);
                }

                for (int i = 0; i < sceneNodes.Count; i++)
                {
                    var node = sceneNodes[i];
                    if (node != null)
                    {
                        if (i < data.initialNodes.Count)
                        {
                            node.gameObject.SetActive(true);
                            var cfg = data.initialNodes[i];
                            node.transform.position = cfg.position;
                            node.transform.rotation = Quaternion.Euler(cfg.rotationEuler);
                            node.SetRedirectType(cfg.redirectType);
                            node.ConfigureBounds(data.xBounds, data.zBounds, data.puzzlePlaneY);
                            node.ResetNode();
                        }
                        else
                        {
                            node.gameObject.SetActive(false);
                        }
                    }
                }
                puzzleManager.FindSceneEntities();
            }

            // 6. Refresh Beam System
            if (beamSystem != null)
            {
                beamSystem.RefreshSceneNodes();
                beamSystem.RecalculateBeam();
            }
        }

        private TargetReceiver FindSecondaryTarget()
        {
            var targets = FindObjectsByType<TargetReceiver>(FindObjectsSortMode.None);
            foreach (var t in targets)
            {
                if (puzzleManager != null && t != puzzleManager.Target)
                {
                    return t;
                }
            }
            return null;
        }

        private TargetReceiver CreateSecondaryTarget()
        {
            GameObject targetObj = new GameObject("Target_Secondary");
            GameObject baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.name = "Receiver_Base";
            baseCube.transform.SetParent(targetObj.transform);
            baseCube.transform.localPosition = new Vector3(0, -0.3f, 0);
            baseCube.transform.localScale = new Vector3(1.1f, 0.25f, 1.1f);
            DestroyImmediate(baseCube.GetComponent<Collider>());

            GameObject sensorCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sensorCube.name = "Sensor_Core";
            sensorCube.transform.SetParent(targetObj.transform);
            sensorCube.transform.localPosition = Vector3.zero;
            sensorCube.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            DestroyImmediate(sensorCube.GetComponent<Collider>());

            GameObject lightObj = new GameObject("Target_Light");
            lightObj.transform.SetParent(targetObj.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light glow = lightObj.AddComponent<Light>();
            glow.color = new Color(0.1f, 0.2f, 0.3f);
            glow.intensity = 1.0f;
            glow.range = 4.0f;

            BoxCollider col = targetObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.9f, 0.9f, 0.9f);

            return targetObj.AddComponent<TargetReceiver>();
        }

        private PuzzleNode CreateAdditionalNode(string nodeName)
        {
            GameObject nodeObj = new GameObject(nodeName);
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Node_Housing";
            body.transform.SetParent(nodeObj.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
            DestroyImmediate(body.GetComponent<Collider>());

            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.name = "Direction_Pointer";
            pointer.transform.SetParent(nodeObj.transform);
            pointer.transform.localPosition = new Vector3(0, 0.25f, 0.45f);
            pointer.transform.localScale = new Vector3(0.2f, 0.15f, 0.4f);
            DestroyImmediate(pointer.GetComponent<Collider>());

            SphereCollider col = nodeObj.AddComponent<SphereCollider>();
            col.radius = 0.55f;

            PuzzleNode nodeComp = nodeObj.AddComponent<PuzzleNode>();
            nodeComp.SetDirectionIndicator(pointer.transform);
            return nodeComp;
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
                // Chamber 01: Divergence (The Baseline)
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
                    new NodeSpawnConfig { nodeId = "Node_Alpha", position = new Vector3(-3.5f, 0.5f, 1.0f), rotationEuler = new Vector3(0, 45f, 0), redirectType = NodeRedirectType.OrientationForward, allowMovement = true, allowRotation = true },
                    new NodeSpawnConfig { nodeId = "Node_Beta", position = new Vector3(0.0f, 0.5f, 1.0f), rotationEuler = new Vector3(0, 135f, 0), redirectType = NodeRedirectType.OrientationForward, allowMovement = true, allowRotation = true }
                };
                levelSequence.Add(ch1);

                // Chamber 02: Occlusion Barrier (Obstacle Challenge)
                LevelData ch2 = ScriptableObject.CreateInstance<LevelData>();
                ch2.levelTitle = "Chamber 02: Occlusion Barrier";
                ch2.objectiveText = "Route the light around the central energy-absorbing obstacle.";
                ch2.sourcePosition = new Vector3(-3.5f, 0.5f, -3.0f);
                ch2.sourceDirection = Vector3.forward;
                ch2.targetPosition = new Vector3(3.5f, 0.5f, -3.0f);
                ch2.targetForward = Vector3.forward;
                ch2.minNodesRequired = 2;
                ch2.initialNodes = new List<NodeSpawnConfig>
                {
                    new NodeSpawnConfig { nodeId = "Node_Alpha", position = new Vector3(-3.5f, 0.5f, 2.5f), rotationEuler = new Vector3(0, 0f, 0), redirectType = NodeRedirectType.OrientationForward, allowMovement = true, allowRotation = true },
                    new NodeSpawnConfig { nodeId = "Node_Beta", position = new Vector3(3.5f, 0.5f, 2.5f), rotationEuler = new Vector3(0, 0f, 0), redirectType = NodeRedirectType.OrientationForward, allowMovement = true, allowRotation = true }
                };
                ch2.obstacles = new List<ObstacleConfig>
                {
                    new ObstacleConfig { position = new Vector3(0f, 0.5f, 0f), size = new Vector3(3.5f, 1.2f, 0.8f) }
                };
                levelSequence.Add(ch2);

                // Chamber 03: Prism Bifurcation (Multi-Beam Challenge)
                LevelData ch3 = ScriptableObject.CreateInstance<LevelData>();
                ch3.levelTitle = "Chamber 03: Prism Bifurcation";
                ch3.objectiveText = "Split the energy beam into dual paths and energize both targets simultaneously.";
                ch3.useDualTargets = true;
                ch3.sourcePosition = new Vector3(-3.5f, 0.5f, -3.0f);
                ch3.sourceDirection = Vector3.forward;
                ch3.targetPosition = new Vector3(-3.5f, 0.5f, 3.5f);
                ch3.targetForward = Vector3.back;
                ch3.target2Position = new Vector3(3.5f, 0.5f, 3.5f);
                ch3.target2Forward = Vector3.back;
                ch3.minNodesRequired = 2;
                ch3.initialNodes = new List<NodeSpawnConfig>
                {
                    new NodeSpawnConfig { nodeId = "Node_Alpha", position = new Vector3(-3.5f, 0.5f, 0.0f), rotationEuler = new Vector3(0, 45f, 0), redirectType = NodeRedirectType.PrismSplit, allowMovement = true, allowRotation = true },
                    new NodeSpawnConfig { nodeId = "Node_Beta", position = new Vector3(3.5f, 0.5f, 0.0f), rotationEuler = new Vector3(0, 90f, 0), redirectType = NodeRedirectType.OrientationForward, allowMovement = true, allowRotation = true }
                };
                levelSequence.Add(ch3);
            }
        }
    }
}
