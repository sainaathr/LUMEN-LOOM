using UnityEngine;

namespace LumenLoom.Puzzle
{
    /// <summary>
    /// The origin of light in the puzzle. Emits the primary light beam.
    /// </summary>
    public class EnergySource : MonoBehaviour
    {
        [Header("Emitter Settings")]
        [SerializeField] private Transform emitterPoint;
        [SerializeField] private Vector3 defaultDirection = Vector3.forward;
        [SerializeField] private bool isActive = true;

        [Header("Visual Effects")]
        [SerializeField] private MeshRenderer coreRenderer;
        [SerializeField] private Light emitterLight;
        [SerializeField] private Color beamColor = new Color(0.0f, 0.9f, 1.0f, 1.0f);
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private Transform rotatingRing;

        public Vector3 EmitterPosition => emitterPoint != null ? emitterPoint.position : transform.position;
        public Vector3 EmissionDirection => emitterPoint != null ? emitterPoint.forward : transform.TransformDirection(defaultDirection.normalized);
        public Color BeamColor => beamColor;
        public bool IsActive => isActive;

        private void Update()
        {
            if (rotatingRing != null && isActive)
            {
                rotatingRing.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }

            if (emitterLight != null && isActive)
            {
                emitterLight.intensity = 2.0f + 0.35f * Mathf.Sin(Time.time * 3.5f);
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (emitterLight != null)
            {
                emitterLight.enabled = active;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Vector3 pos = EmitterPosition;
            Gizmos.DrawWireSphere(pos, 0.2f);
            Gizmos.DrawRay(pos, EmissionDirection * 2f);
        }
#endif
    }
}

