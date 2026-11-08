using System;
using UnityEngine;

namespace LumenLoom.Puzzle
{
    public enum NodeRedirectType
    {
        OrientationForward, // Beam enters and fires strictly along node's transform.forward
        MirrorReflection,   // Beam bounces specularly off the node's forward surface normal
        PrismSplit          // Splits incoming beam into forward and orthogonal right branches
    }

    /// <summary>
    /// Represents an interactive energy node in the 3D puzzle space.
    /// Can be translated, rotated, and emits visual feedback based on selection and beam conduction.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PuzzleNode : MonoBehaviour
    {
        [Header("Node Configuration")]
        [SerializeField] private string nodeId = "Node_1";
        [SerializeField] private NodeRedirectType redirectType = NodeRedirectType.OrientationForward;
        [SerializeField] private Transform beamOutputPoint;
        [SerializeField] private Transform beamInputPoint;

        [Header("Movement & Boundary")]
        [SerializeField] private bool allowMovement = true;
        [SerializeField] private bool allowRotation = true;
        [SerializeField] private Vector2 xBounds = new Vector2(-4.0f, 4.0f);
        [SerializeField] private Vector2 zBounds = new Vector2(-4.0f, 4.0f);
        [SerializeField] private float fixedY = 0.5f;

        [Header("Visual Feedback & Materials")]
        [SerializeField] private MeshRenderer nodeRenderer;
        [SerializeField] private Color idleColor = new Color(0.1f, 0.4f, 0.7f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.2f, 0.7f, 1.0f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.95f, 1.0f, 1f);
        [SerializeField] private Color beamActiveColor = new Color(0.2f, 1.0f, 0.85f, 1f);
        [SerializeField] private Light pointGlowLight;

        [Header("Direction Indicator")]
        [SerializeField] private Transform directionIndicator; // Visual arrow or lens

        // Internal State
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _baseScale;
        private bool _isHovered;
        private bool _isSelected;
        private bool _isConductingBeam;
        private float _lastRotatePulseTime = -10f;
        private MaterialPropertyBlock _propBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public string NodeId => nodeId;
        public NodeRedirectType RedirectType => redirectType;
        public bool IsSelected => _isSelected;
        public bool IsConductingBeam => _isConductingBeam;
        public Vector2 XBounds => xBounds;
        public Vector2 ZBounds => zBounds;
        public float FixedY => fixedY;

        public event Action<PuzzleNode> OnTransformUpdated;

        private void Awake()
        {
            if (nodeRenderer == null)
            {
                nodeRenderer = GetComponentInChildren<MeshRenderer>();
            }

            _propBlock = new MaterialPropertyBlock();
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _baseScale = transform.localScale;

            // Ensure Y coordinate is fixed
            Vector3 pos = transform.position;
            pos.y = fixedY;
            transform.position = pos;

            UpdateVisuals();
        }

        private void Update()
        {
            if (_isSelected)
            {
                // Subtle holographic pulse when actively selected
                float pulse = 1.0f + 0.05f * Mathf.Sin(Time.time * 6.0f);
                transform.localScale = _baseScale * pulse;
            }
            else if (transform.localScale != _baseScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, Time.deltaTime * 12.0f);
            }
        }

        public void SetRedirectType(NodeRedirectType type)
        {
            redirectType = type;
        }

        public void SetDirectionIndicator(Transform indicator)
        {
            directionIndicator = indicator;
        }

        /// <summary>
        /// Output origin for redirected light beam.
        /// </summary>
        public Vector3 GetOutputOrigin()
        {
            return beamOutputPoint != null ? beamOutputPoint.position : transform.position;
        }

        /// <summary>
        /// Calculates primary redirected beam direction based on incoming hit vector and node configuration.
        /// </summary>
        public Vector3 CalculateOutputDirection(Vector3 incomingDirection, Vector3 hitNormal)
        {
            if (redirectType == NodeRedirectType.OrientationForward || redirectType == NodeRedirectType.PrismSplit)
            {
                return transform.forward;
            }
            else // MirrorReflection
            {
                Vector3 normal = transform.forward; // Face normal
                return Vector3.Reflect(incomingDirection, normal).normalized;
            }
        }

        /// <summary>
        /// Calculates secondary output beam for bifurcating optical elements (e.g. PrismSplit).
        /// </summary>
        public Vector3 CalculateSecondaryOutputDirection(Vector3 incomingDirection, Vector3 hitNormal)
        {
            if (redirectType == NodeRedirectType.PrismSplit)
            {
                // Orthogonal right output branch
                return transform.right;
            }
            return Vector3.zero;
        }

        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered) return;
            _isHovered = hovered;
            UpdateVisuals();
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;
            _isSelected = selected;
            UpdateVisuals();
        }

        public void SetConductingBeam(bool conducting)
        {
            if (_isConductingBeam == conducting) return;
            _isConductingBeam = conducting;
            UpdateVisuals();
        }

        /// <summary>
        /// Moves node to world position clamped inside the designated puzzle area boundary.
        /// </summary>
        public void MoveToClamped(Vector3 worldTarget)
        {
            if (!allowMovement) return;

            float clampedX = Mathf.Clamp(worldTarget.x, xBounds.x, xBounds.y);
            float clampedZ = Mathf.Clamp(worldTarget.z, zBounds.x, zBounds.y);
            Vector3 targetPos = new Vector3(clampedX, fixedY, clampedZ);

            transform.position = targetPos;
            OnTransformUpdated?.Invoke(this);
        }

        /// <summary>
        /// Rotates node around the vertical Y axis by deltaDegrees.
        /// </summary>
        public void RotateNode(float deltaDegrees)
        {
            if (!allowRotation) return;

            transform.Rotate(Vector3.up, deltaDegrees, Space.World);
            OnTransformUpdated?.Invoke(this);
        }

        /// <summary>
        /// Restores node to its initial position and rotation.
        /// </summary>
        public void ResetNode()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _baseScale;
            _isHovered = false;
            _isSelected = false;
            _isConductingBeam = false;
            UpdateVisuals();
            OnTransformUpdated?.Invoke(this);
        }

        public void ConfigureBounds(Vector2 minMaxX, Vector2 minMaxZ, float y)
        {
            xBounds = minMaxX;
            zBounds = minMaxZ;
            fixedY = y;
        }

        private void UpdateVisuals()
        {
            if (nodeRenderer == null) return;

            Color targetColor = idleColor;
            float intensity = 1.0f;

            if (_isSelected)
            {
                targetColor = selectedColor;
                intensity = 2.5f;
            }
            else if (_isHovered)
            {
                targetColor = hoverColor;
                intensity = 1.8f;
            }
            else if (_isConductingBeam)
            {
                targetColor = beamActiveColor;
                intensity = 2.0f;
            }

            nodeRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, targetColor);
            _propBlock.SetColor(EmissionColorId, targetColor * intensity);
            nodeRenderer.SetPropertyBlock(_propBlock);

            if (pointGlowLight != null)
            {
                pointGlowLight.color = targetColor;
                pointGlowLight.intensity = intensity;
                pointGlowLight.range = _isSelected ? 5.0f : (_isHovered ? 4.0f : 3.0f);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.forward * 1.5f);

            // Draw bounding box
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Vector3 center = new Vector3((xBounds.x + xBounds.y) * 0.5f, fixedY, (zBounds.x + zBounds.y) * 0.5f);
            Vector3 size = new Vector3(xBounds.y - xBounds.x, 0.2f, zBounds.y - zBounds.x);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}

