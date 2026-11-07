using UnityEngine;

namespace LumenLoom.Rendering
{
    /// <summary>
    /// Renders the computed optical energy beam using an optimized LineRenderer
    /// and dynamic light pulses.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class BeamRenderer : MonoBehaviour
    {
        [Header("Beam Geometry & Style")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float beamWidth = 0.08f;
        [SerializeField] private Color normalColor = new Color(0.1f, 0.85f, 1.0f, 1.0f);
        [SerializeField] private Color connectedColor = new Color(0.2f, 1.0f, 0.6f, 1.0f);
        [SerializeField] private Color loopWarningColor = new Color(1.0f, 0.35f, 0.1f, 1.0f);

        [Header("Beam Pulse Animation")]
        [SerializeField] private float scrollSpeed = 3.0f;
        [SerializeField] private float pulseFrequency = 4.0f;
        [SerializeField] private float pulseAmplitude = 0.02f;

        [Header("Endpoint Impact Indicator")]
        [SerializeField] private Transform impactMarker;
        [SerializeField] private Light impactLight;

        private Material _beamMaterial;
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            lineRenderer.startWidth = beamWidth;
            lineRenderer.endWidth = beamWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.View;

            // Cache material instance
            if (lineRenderer.material != null)
            {
                _beamMaterial = lineRenderer.material;
            }
        }

        private void Update()
        {
            if (lineRenderer.positionCount <= 0) return;

            // Dynamic pulse animation
            float pulse = Mathf.Sin(Time.time * pulseFrequency) * pulseAmplitude;
            float currentWidth = Mathf.Max(0.02f, beamWidth + pulse);
            lineRenderer.startWidth = currentWidth;
            lineRenderer.endWidth = currentWidth;

            // Scroll texture if material has texture
            if (_beamMaterial != null && _beamMaterial.HasProperty(MainTexId))
            {
                Vector2 offset = _beamMaterial.GetTextureOffset(MainTexId);
                offset.x -= Time.deltaTime * scrollSpeed;
                _beamMaterial.SetTextureOffset(MainTexId, offset);
            }
        }

        public void RenderBeam(BeamPath path)
        {
            if (path == null || path.Points.Count < 2)
            {
                ClearBeam();
                return;
            }

            lineRenderer.enabled = true;
            lineRenderer.positionCount = path.Points.Count;

            for (int i = 0; i < path.Points.Count; i++)
            {
                lineRenderer.SetPosition(i, path.Points[i]);
            }

            Color activeBeamColor = normalColor;
            if (path.HasReachedTarget)
            {
                activeBeamColor = connectedColor;
            }
            else if (path.HasLoopDetected)
            {
                activeBeamColor = loopWarningColor;
            }

            lineRenderer.startColor = activeBeamColor;
            lineRenderer.endColor = activeBeamColor;

            if (_beamMaterial != null)
            {
                if (_beamMaterial.HasProperty(BaseColorId))
                {
                    _beamMaterial.SetColor(BaseColorId, activeBeamColor);
                }
                if (_beamMaterial.HasProperty(EmissionColorId))
                {
                    _beamMaterial.SetColor(EmissionColorId, activeBeamColor * 2.0f);
                }
            }

            // Update impact marker position at the beam's terminus
            Vector3 terminus = path.Points[path.Points.Count - 1];
            if (impactMarker != null)
            {
                impactMarker.gameObject.SetActive(true);
                impactMarker.position = terminus;
            }

            if (impactLight != null)
            {
                impactLight.enabled = true;
                impactLight.transform.position = terminus;
                impactLight.color = activeBeamColor;
            }
        }

        public void ClearBeam()
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;

            if (impactMarker != null) impactMarker.gameObject.SetActive(false);
            if (impactLight != null) impactLight.enabled = false;
        }
    }
}
