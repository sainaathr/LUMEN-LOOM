using System;
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
        [SerializeField] private int minNodesRequired = 1;

        private bool _isSolved;
        private string _statusMessage = "ALIGN THE ENERGY NODES";

        public bool IsSolved => _isSolved;
        public string StatusMessage => _statusMessage;

        public event Action OnPuzzleSolved;
        public event Action OnPuzzleUnsolved;
        public event Action<string> OnStatusMessageChanged;

        public void SetTarget(TargetReceiver target)
        {
            primaryTarget = target;
        }

        public void ValidatePath(BeamPath path)
        {
            bool wasSolved = _isSolved;

            if (path == null || path.Points.Count < 2)
            {
                SetSolved(false, "ENERGY EMISSION INACTIVE");
                return;
            }

            if (path.HasLoopDetected)
            {
                SetSolved(false, "ENERGY LOOP DETECTED - REALIGN NODES");
                return;
            }

            if (path.HasReachedTarget && (primaryTarget == null || path.HitTarget == primaryTarget))
            {
                if (path.VisitedNodes.Count >= minNodesRequired)
                {
                    if (path.HitTarget != null)
                    {
                        path.HitTarget.SetEnergized(true);
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

            // Beam didn't reach target
            if (primaryTarget != null)
            {
                primaryTarget.SetEnergized(false);
            }

            if (path.HitObstacle)
            {
                SetSolved(false, "BEAM BLOCKED BY OBSTACLE");
            }
            else
            {
                SetSolved(false, "ALIGN THE ENERGY NODES");
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
            if (primaryTarget != null)
            {
                primaryTarget.ResetReceiver();
            }
            OnStatusMessageChanged?.Invoke(_statusMessage);
            OnPuzzleUnsolved?.Invoke();
        }
    }
}

