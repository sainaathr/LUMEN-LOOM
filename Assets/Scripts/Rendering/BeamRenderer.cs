using System.Collections.Generic;
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
        private readonly List<LineRenderer> _branchRenderers = new List<LineRenderer>();
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

            for (int b = 0; b < _branchRenderers.Count; b++)
            {
                if (_branchRenderers[b] != null && _branchRenderers[b].enabled)
                {
                    _branchRenderers[b].startWidth = currentWidth;
                    _branchRenderers[b].endWidth = currentWidth;
                }
            }

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

            // Render optical branches (e.g. PrismSplit)
            if (path.Branches != null && path.Branches.Count > 0)
            {
                while (_branchRenderers.Count < path.Branches.Count)
                {
                    GameObject branchObj = new GameObject($"Beam_Branch_{_branchRenderers.Count + 1}");
                    branchObj.transform.SetParent(transform);
                    LineRenderer lr = branchObj.AddComponent<LineRenderer>();
                    if (lineRenderer.material != null)
                    {
                        lr.material = lineRenderer.material;
                    }
                    lr.startWidth = beamWidth;
                    lr.endWidth = beamWidth;
                    lr.useWorldSpace = true;
                    lr.alignment = LineAlignment.View;
                    _branchRenderers.Add(lr);
                }

                for (int b = 0; b < _branchRenderers.Count; b++)
                {
                    if (b < path.Branches.Count && path.Branches[b].Points.Count >= 2)
                    {
                        var branch = path.Branches[b];
                        var lr = _branchRenderers[b];
                        lr.enabled = true;
                        lr.positionCount = branch.Points.Count;
                        for (int p = 0; p < branch.Points.Count; p++)
                        {
                            lr.SetPosition(p, branch.Points[p]);
                        }
                        Color branchColor = branch.HasReachedTarget ? connectedColor : (branch.HasLoopDetected ? loopWarningColor : normalColor);
                        lr.startColor = branchColor;
                        lr.endColor = branchColor;
                    }
                    else
                    {
                        _branchRenderers[b].positionCount = 0;
                        _branchRenderers[b].enabled = false;
                    }
                }
            }
            else
            {
                for (int b = 0; b < _branchRenderers.Count; b++)
                {
                    if (_branchRenderers[b] != null)
                    {
                        _branchRenderers[b].positionCount = 0;
                        _branchRenderers[b].enabled = false;
                    }
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

            for (int b = 0; b < _branchRenderers.Count; b++)
            {
                if (_branchRenderers[b] != null)
                {
                    _branchRenderers[b].positionCount = 0;
                    _branchRenderers[b].enabled = false;
                }
            }

            if (impactMarker != null) impactMarker.gameObject.SetActive(false);
            if (impactLight != null) impactLight.enabled = false;
        }
    }
}

