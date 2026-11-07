using System;
using System.Collections.Generic;
using UnityEngine;
using LumenLoom.Puzzle;

namespace LumenLoom.Rendering
{
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

        private void Start()
        {
            RefreshSceneNodes();
            RecalculateBeam();
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
            _currentPath.Points.Clear();
            _currentPath.VisitedNodes.Clear();
            _visitedNodeSet.Clear();
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

            int bounces = 0;

            while (bounces < maxReflections)
            {
                bounces++;

                // Cast ray forward
                if (Physics.Raycast(currentOrigin + currentDirection * rayOriginOffset, currentDirection, out RaycastHit hit, maxBeamRange, collisionLayers))
                {
                    _currentPath.Points.Add(hit.point);

                    // 1. Check for Target Receiver
                    TargetReceiver target = hit.collider.GetComponentInParent<TargetReceiver>();
                    if (target != null)
                    {
                        if (target.ValidateHit(currentDirection))
                        {
                            _currentPath.HitTarget = target;
                            _currentPath.HasReachedTarget = true;
                        }
                        break; // Beam absorbed by target
                    }

                    // 2. Check for Puzzle Node
                    PuzzleNode node = hit.collider.GetComponentInParent<PuzzleNode>();
                    if (node != null)
                    {
                        // Infinite loop detection
                        if (_visitedNodeSet.Contains(node))
                        {
                            _currentPath.HasLoopDetected = true;
                            break;
                        }

                        _visitedNodeSet.Add(node);
                        _currentPath.VisitedNodes.Add(node);

                        // Calculate new origin and direction
                        currentOrigin = node.GetOutputOrigin();
                        currentDirection = node.CalculateOutputDirection(currentDirection, hit.normal).normalized;
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

