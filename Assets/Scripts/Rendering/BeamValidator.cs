using System;
using System.Collections.Generic;
using UnityEngine;
using LumenLoom.Puzzle;

namespace LumenLoom.Rendering
{
    /// <summary>
    /// Evaluates beam trajectories against puzzle completion criteria.
    /// Manages target receiver activation states and notifies game managers of solve status.
    /// </summary>
    public class BeamValidator : MonoBehaviour
    {
        [Header("Validation Rules")]
        [SerializeField] private TargetReceiver primaryTarget;
        [SerializeField] private List<TargetReceiver> requiredTargets = new List<TargetReceiver>();
        [SerializeField] private int minNodesRequired = 1;

        private bool _isSolved;
        private string _statusMessage = "ALIGN THE ENERGY NODES";

        public bool IsSolved => _isSolved;
        public string StatusMessage => _statusMessage;
        public TargetReceiver PrimaryTarget => primaryTarget;
        public IReadOnlyList<TargetReceiver> RequiredTargets => requiredTargets;

        public event Action OnPuzzleSolved;
        public event Action OnPuzzleUnsolved;
        public event Action<string> OnStatusMessageChanged;

        public void SetTarget(TargetReceiver target)
        {
            primaryTarget = target;
            requiredTargets.Clear();
            if (target != null)
            {
                requiredTargets.Add(target);
            }
        }

        public void SetRequiredTargets(IEnumerable<TargetReceiver> targets)
        {
            requiredTargets.Clear();
            if (targets != null)
            {
                requiredTargets.AddRange(targets);
            }
            primaryTarget = requiredTargets.Count > 0 ? requiredTargets[0] : null;
        }

        public void ClearRequiredTargets()
        {
            requiredTargets.Clear();
            primaryTarget = null;
        }

        public void ValidatePath(BeamPath path)
        {
            bool wasSolved = _isSolved;

            if (path == null || path.Points.Count < 2)
            {
                ResetReceiverStates();
                SetSolved(false, "ENERGY EMISSION INACTIVE");
                return;
            }

            if (path.HasLoopDetected)
            {
                ResetReceiverStates();
                SetSolved(false, "ENERGY LOOP DETECTED - REALIGN NODES");
                return;
            }

            // Check if multiple targets required (e.g. Chamber 03 Prism Split)
            if (requiredTargets != null && requiredTargets.Count > 1)
            {
                int hitCount = 0;
                foreach (var req in requiredTargets)
                {
                    if (req != null)
                    {
                        bool hit = path.HitTargets.Contains(req) || path.HitTarget == req;
                        req.SetEnergized(hit);
                        if (hit) hitCount++;
                    }
                }

                if (hitCount == requiredTargets.Count)
                {
                    if (path.VisitedNodes.Count >= minNodesRequired)
                    {
                        SetSolved(true, "ALL TARGETS ENERGIZED");
                        return;
                    }
                    else
                    {
                        SetSolved(false, $"CONNECT ALL {minNodesRequired} NODES BEFORE TARGETS");
                        return;
                    }
                }
                else if (hitCount > 0)
                {
                    SetSolved(false, $"TARGETS ENERGIZED: {hitCount}/{requiredTargets.Count}");
                    return;
                }
            }
            else
            {
                TargetReceiver targetToCheck = primaryTarget != null 
                    ? primaryTarget 
                    : (requiredTargets != null && requiredTargets.Count > 0 ? requiredTargets[0] : null);

                if (path.HasReachedTarget && (targetToCheck == null || path.HitTarget == targetToCheck || path.HitTargets.Contains(targetToCheck)))
                {
                    if (path.VisitedNodes.Count >= minNodesRequired)
                    {
                        if (targetToCheck != null)
                        {
                            targetToCheck.SetEnergized(true);
                        }
                        SetSolved(true, "TARGET CONNECTED");
                        return;
                    }
                    else
                    {
                        SetSolved(false, $"CONNECT ALL {minNodesRequired} NODES BEFORE TARGET");
                        return;
                    }
                }
            }

            // Beam didn't reach all required targets
            ResetReceiverStates();

            if (path.HitObstacle)
            {
                SetSolved(false, "BEAM BLOCKED BY OBSTACLE");
            }
            else
            {
                SetSolved(false, "ALIGN THE ENERGY NODES");
            }
        }

        private void ResetReceiverStates()
        {
            if (primaryTarget != null)
            {
                primaryTarget.SetEnergized(false);
            }
            if (requiredTargets != null)
            {
                for (int i = 0; i < requiredTargets.Count; i++)
                {
                    if (requiredTargets[i] != null)
                    {
                        requiredTargets[i].SetEnergized(false);
                    }
                }
            }
        }

        private void SetSolved(bool solved, string message)
        {
            _statusMessage = message;
            OnStatusMessageChanged?.Invoke(_statusMessage);

            if (_isSolved != solved)
            {
                _isSolved = solved;
                if (_isSolved)
                {
                    OnPuzzleSolved?.Invoke();
                }
                else
                {
                    OnPuzzleUnsolved?.Invoke();
                }
            }
        }

        public void ResetValidation()
        {
            _isSolved = false;
            _statusMessage = "ALIGN THE ENERGY NODES";
            ResetReceiverStates();
            OnStatusMessageChanged?.Invoke(_statusMessage);
            OnPuzzleUnsolved?.Invoke();
        }
    }
}

