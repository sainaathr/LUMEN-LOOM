using System;
using System.Collections.Generic;
using UnityEngine;
using LumenLoom.Puzzle;

namespace LumenLoom.Rendering
{
    /// <summary>
    /// Represents an optical branch (such as a split from a prism).
    /// </summary>
    public class BeamBranch
    {
        public readonly List<Vector3> Points = new List<Vector3>(16);
        public TargetReceiver HitTarget;
        public bool HasReachedTarget;
        public bool HasLoopDetected;
        public bool HitObstacle;
    }

    /// <summary>
    /// Represents the full calculated path of an energy beam.
    /// </summary>
    public class BeamPath
    {
        public readonly List<Vector3> Points = new List<Vector3>(16);
        public readonly List<PuzzleNode> VisitedNodes = new List<PuzzleNode>(8);
        public TargetReceiver HitTarget;
        public bool HasReachedTarget;
        public bool HasLoopDetected;
        public bool HitObstacle;

        // Multi-branch optical support (e.g. PrismSplit)
        public readonly List<BeamBranch> Branches = new List<BeamBranch>();
        public readonly List<TargetReceiver> HitTargets = new List<TargetReceiver>();
    }

    /// <summary>
    /// Deterministic, non-allocating optical beam calculation system.
    /// Traces ray segments, calculates node redirection, detects loops and obstacles, and verifies target connections.
    /// </summary>
    public class BeamSystem : MonoBehaviour
    {
        [Header("Beam System Settings")]
        [SerializeField] private EnergySource energySource;
        [SerializeField] private int maxReflections = 10;
        [SerializeField] private float maxBeamRange = 50.0f;
        [SerializeField] private LayerMask collisionLayers = ~0; // All layers by default
        [SerializeField] private float rayOriginOffset = 0.05f; // Prevents self-intersection

        [Header("Components")]
        [SerializeField] private BeamRenderer beamRenderer;
        [SerializeField] private BeamValidator beamValidator;

        private readonly BeamPath _currentPath = new BeamPath();
        private readonly HashSet<PuzzleNode> _visitedNodeSet = new HashSet<PuzzleNode>();
        private readonly List<PuzzleNode> _allSceneNodes = new List<PuzzleNode>();

        public BeamPath CurrentPath => _currentPath;
        public EnergySource Source => energySource;

        public event Action<BeamPath> OnBeamPathCalculated;

        private void Awake()
        {
            if (beamRenderer == null)
            {
                beamRenderer = GetComponent<BeamRenderer>() ?? FindFirstObjectByType<BeamRenderer>();
            }
            if (beamValidator == null)
            {
                beamValidator = GetComponent<BeamValidator>() ?? FindFirstObjectByType<BeamValidator>();
            }
        }

        private void Start()
        {
            RefreshSceneNodes();
            RecalculateBeam();
        }

        public void SetBeamValidator(BeamValidator validator)
        {
            beamValidator = validator;
        }

        public void SetBeamRenderer(BeamRenderer renderer)
        {
            beamRenderer = renderer;
        }

        public void SetEnergySource(EnergySource source)
        {
            energySource = source;
            RecalculateBeam();
        }

        public void RefreshSceneNodes()
        {
            _allSceneNodes.Clear();
            _allSceneNodes.AddRange(FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None));
        }

        /// <summary>
        /// Recalculates the complete beam trajectory from source through all nodes to target or termination.
        /// </summary>
        public void RecalculateBeam()
        {
            Physics.SyncTransforms();

            _currentPath.Points.Clear();
            _currentPath.VisitedNodes.Clear();
            _visitedNodeSet.Clear();
            _currentPath.Branches.Clear();
            _currentPath.HitTargets.Clear();
            _currentPath.HitTarget = null;
            _currentPath.HasReachedTarget = false;
            _currentPath.HasLoopDetected = false;
            _currentPath.HitObstacle = false;

            if (energySource == null || !energySource.IsActive)
            {
                ClearNodeConduction();
                if (beamRenderer != null) beamRenderer.ClearBeam();
                if (beamValidator != null) beamValidator.ValidatePath(_currentPath);
                OnBeamPathCalculated?.Invoke(_currentPath);
                return;
            }

            Vector3 currentOrigin = energySource.EmitterPosition;
            Vector3 currentDirection = energySource.EmissionDirection.normalized;

            _currentPath.Points.Add(currentOrigin);

            PuzzleNode lastEmittingNode = null;
            int bounces = 0;

            while (bounces < maxReflections)
            {
                bounces++;

                // Prevent raycasting inside the emitting node's collider
                float currentOffset = lastEmittingNode != null ? 0.6f : rayOriginOffset;
                Vector3 rayOrigin = currentOrigin + currentDirection * currentOffset;

                // Cast ray forward
                if (Physics.Raycast(rayOrigin, currentDirection, out RaycastHit hit, maxBeamRange, collisionLayers))
                {
                    PuzzleNode hitNode = hit.collider.GetComponentInParent<PuzzleNode>();
                    if (hitNode != null && hitNode == lastEmittingNode)
                    {
                        // Ignore self-intersection with the node the beam just emitted from
                        continue;
                    }

                    _currentPath.Points.Add(hit.point);

                    // 1. Check for Target Receiver
                    TargetReceiver target = hit.collider.GetComponentInParent<TargetReceiver>();
                    if (target != null)
                    {
                        if (target.ValidateHit(currentDirection))
                        {
                            _currentPath.HitTarget = target;
                            _currentPath.HasReachedTarget = true;
                            if (!_currentPath.HitTargets.Contains(target))
                            {
                                _currentPath.HitTargets.Add(target);
                            }
                        }
                        break; // Beam absorbed by target
                    }

                    // 2. Check for Puzzle Node
                    if (hitNode != null)
                    {
                        // Infinite loop detection
                        if (_visitedNodeSet.Contains(hitNode))
                        {
                            _currentPath.HasLoopDetected = true;
                            break;
                        }

                        _visitedNodeSet.Add(hitNode);
                        _currentPath.VisitedNodes.Add(hitNode);
                        lastEmittingNode = hitNode;

                        // Check for Prism Split: bifurcate secondary branch
                        if (hitNode.RedirectType == NodeRedirectType.PrismSplit)
                        {
                            Vector3 secondaryDir = hitNode.CalculateSecondaryOutputDirection(currentDirection, hit.normal).normalized;
                            Vector3 secondaryOrigin = hitNode.GetOutputOrigin();
                            BeamBranch secBranch = new BeamBranch();
                            TraceBranch(secondaryOrigin, secondaryDir, hitNode, secBranch, maxReflections - bounces);
                            _currentPath.Branches.Add(secBranch);
                        }

                        // Calculate new origin and direction for primary beam
                        currentOrigin = hitNode.GetOutputOrigin();
                        currentDirection = hitNode.CalculateOutputDirection(currentDirection, hit.normal).normalized;
                        _currentPath.Points.Add(currentOrigin);
                        continue;
                    }

                    // 3. Check for Obstacle
                    Obstacle obstacle = hit.collider.GetComponentInParent<Obstacle>();
                    if (obstacle != null)
                    {
                        obstacle.NotifyBeamHit();
                        _currentPath.HitObstacle = true;
                        break;
                    }

                    // 4. Any other non-interactive surface stops the beam
                    break;
                }
                else
                {
                    // No collision: project beam to maximum distance
                    _currentPath.Points.Add(currentOrigin + currentDirection * maxBeamRange);
                    break;
                }
            }

            // Sync HitTarget if multiple targets hit
            if (_currentPath.HitTargets.Count > 0)
            {
                _currentPath.HasReachedTarget = true;
                if (_currentPath.HitTarget == null)
                {
                    _currentPath.HitTarget = _currentPath.HitTargets[0];
                }
            }

            // Update conduction state on all puzzle nodes
            UpdateNodeConductionStates();

            // Update visuals
            if (beamRenderer != null)
            {
                beamRenderer.RenderBeam(_currentPath);
            }

            // Validate puzzle state
            if (beamValidator != null)
            {
                beamValidator.ValidatePath(_currentPath);
            }

            OnBeamPathCalculated?.Invoke(_currentPath);
        }

        private void TraceBranch(Vector3 startOrigin, Vector3 startDirection, PuzzleNode emitterNode, BeamBranch branch, int remainingReflections)
        {
            branch.Points.Add(startOrigin);
            Vector3 currentOrigin = startOrigin;
            Vector3 currentDirection = startDirection;
            PuzzleNode lastEmittingNode = emitterNode;
            int bounces = 0;

            while (bounces < remainingReflections)
            {
                bounces++;
                float currentOffset = lastEmittingNode != null ? 0.6f : rayOriginOffset;
                Vector3 rayOrigin = currentOrigin + currentDirection * currentOffset;

                if (Physics.Raycast(rayOrigin, currentDirection, out RaycastHit hit, maxBeamRange, collisionLayers))
                {
                    PuzzleNode hitNode = hit.collider.GetComponentInParent<PuzzleNode>();
                    if (hitNode != null && hitNode == lastEmittingNode)
                    {
                        continue;
                    }

                    branch.Points.Add(hit.point);

                    TargetReceiver target = hit.collider.GetComponentInParent<TargetReceiver>();
                    if (target != null)
                    {
                        if (target.ValidateHit(currentDirection))
                        {
                            branch.HitTarget = target;
                            branch.HasReachedTarget = true;
                            if (!_currentPath.HitTargets.Contains(target))
                            {
                                _currentPath.HitTargets.Add(target);
                            }
                        }
                        break;
                    }

                    if (hitNode != null)
                    {
                        if (_visitedNodeSet.Contains(hitNode))
                        {
                            branch.HasLoopDetected = true;
                            _currentPath.HasLoopDetected = true;
                            break;
                        }

                        _visitedNodeSet.Add(hitNode);
                        _currentPath.VisitedNodes.Add(hitNode);
                        lastEmittingNode = hitNode;

                        currentOrigin = hitNode.GetOutputOrigin();
                        currentDirection = hitNode.CalculateOutputDirection(currentDirection, hit.normal).normalized;
                        branch.Points.Add(currentOrigin);
                        continue;
                    }

                    Obstacle obstacle = hit.collider.GetComponentInParent<Obstacle>();
                    if (obstacle != null)
                    {
                        obstacle.NotifyBeamHit();
                        branch.HitObstacle = true;
                        _currentPath.HitObstacle = true;
                        break;
                    }

                    break;
                }
                else
                {
                    branch.Points.Add(currentOrigin + currentDirection * maxBeamRange);
                    break;
                }
            }
        }

        private void UpdateNodeConductionStates()
        {
            for (int i = 0; i < _allSceneNodes.Count; i++)
            {
                PuzzleNode node = _allSceneNodes[i];
                if (node != null)
                {
                    node.SetConductingBeam(_visitedNodeSet.Contains(node));
                }
            }
        }

        private void ClearNodeConduction()
        {
            for (int i = 0; i < _allSceneNodes.Count; i++)
            {
                if (_allSceneNodes[i] != null)
                {
                    _allSceneNodes[i].SetConductingBeam(false);
                }
            }
        }
    }
}

