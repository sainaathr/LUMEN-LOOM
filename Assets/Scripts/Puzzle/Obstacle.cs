using UnityEngine;

namespace LumenLoom.Puzzle
{
    /// <summary>
    /// Represents an obstacle in the puzzle field that absorbs or stops light beams.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Obstacle : MonoBehaviour
    {
        [Header("Obstacle Settings")]
        [SerializeField] private MeshRenderer obstacleRenderer;
        [SerializeField] private Color barrierColor = new Color(0.12f, 0.12f, 0.15f, 1f);
        [SerializeField] private Color hitColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] private float hitFlashDuration = 0.25f;

        private MaterialPropertyBlock _propBlock;
        private float _lastHitTime = -10f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            if (obstacleRenderer == null)
            {
                obstacleRenderer = GetComponentInChildren<MeshRenderer>();
            }
            _propBlock = new MaterialPropertyBlock();
            ApplyColor(barrierColor);
        }

        public void NotifyBeamHit()
        {
            _lastHitTime = Time.time;
            ApplyColor(hitColor);
        }

        private void Update()
        {
            if (_lastHitTime > 0 && Time.time - _lastHitTime > hitFlashDuration)
            {
                _lastHitTime = -10f;
                ApplyColor(barrierColor);
            }
        }

        private void ApplyColor(Color col)
        {
            if (obstacleRenderer == null) return;
            obstacleRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, col);
            obstacleRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
