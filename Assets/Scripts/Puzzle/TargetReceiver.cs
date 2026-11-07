using System;
using UnityEngine;

namespace LumenLoom.Puzzle
{
    /// <summary>
    /// The destination target that must be energized by a light beam to complete the puzzle.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TargetReceiver : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private string targetId = "Target_Primary";
        [SerializeField] private Transform receptionPoint;
        [SerializeField] private bool requireFrontFacingHit = false;
        [SerializeField] [Range(10f, 180f)] private float acceptanceAngle = 90f;

        [Header("Visual Elements")]
        [SerializeField] private MeshRenderer receiverRenderer;
        [SerializeField] private Light targetGlowLight;
        [SerializeField] private ParticleSystem connectionParticles;
        [SerializeField] private Color inactiveColor = new Color(0.1f, 0.15f, 0.25f, 1f);
        [SerializeField] private Color activeColor = new Color(0.2f, 1.0f, 0.5f, 1f); // Vibrant Emerald Green / Cyan
        [SerializeField] private float glowIntensityActive = 3.5f;

        private bool _isEnergized;
        private MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public string TargetId => targetId;
        public bool IsEnergized => _isEnergized;
        public Vector3 ReceptionPosition => receptionPoint != null ? receptionPoint.position : transform.position;

        public event Action<TargetReceiver> OnEnergizedChanged;

        private void Awake()
        {
            if (receiverRenderer == null)
            {
                receiverRenderer = GetComponentInChildren<MeshRenderer>();
            }

            _propBlock = new MaterialPropertyBlock();
            SetEnergized(false);
        }

        /// <summary>
        /// Validates whether the incoming beam qualifies to activate this receiver.
        /// </summary>
        public bool ValidateHit(Vector3 beamIncomingDirection)
        {
            if (!requireFrontFacingHit) return true;

            // Target's front faces outwards
            float dot = Vector3.Dot(-beamIncomingDirection.normalized, transform.forward);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            return angle <= (acceptanceAngle * 0.5f);
        }

        public void SetEnergized(bool energized)
        {
            if (_isEnergized == energized) return;

            _isEnergized = energized;
            UpdateVisuals();
            OnEnergizedChanged?.Invoke(this);

            if (energized && connectionParticles != null)
            {
                connectionParticles.Play();
            }
            else if (!energized && connectionParticles != null)
            {
                connectionParticles.Stop();
            }
        }

        public void ResetReceiver()
        {
            SetEnergized(false);
        }

        private void UpdateVisuals()
        {
            if (receiverRenderer == null) return;

            Color targetColor = _isEnergized ? activeColor : inactiveColor;
            float intensity = _isEnergized ? glowIntensityActive : 0.5f;

            receiverRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, targetColor);
            _propBlock.SetColor(EmissionColorId, targetColor * intensity);
            receiverRenderer.SetPropertyBlock(_propBlock);

            if (targetGlowLight != null)
            {
                targetGlowLight.color = targetColor;
                targetGlowLight.intensity = intensity;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _isEnergized ? Color.green : Color.red;
            Gizmos.DrawWireSphere(ReceptionPosition, 0.4f);
            if (requireFrontFacingHit)
            {
                Gizmos.DrawRay(transform.position, transform.forward * 1.0f);
            }
        }
#endif
    }
}
