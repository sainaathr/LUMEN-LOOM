using UnityEngine;

namespace LumenLoom.Core
{
    /// <summary>
    /// Configures and controls the desktop prototype camera for optimal spatial viewing.
    /// Provides comfortable high-angle perspective looking into the puzzle chamber.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Framing Settings")]
        [SerializeField] private Vector3 lookTarget = new Vector3(0, 0.5f, 0);
        [SerializeField] private Vector3 defaultPosition = new Vector3(0, 8.5f, -8.0f);
        [SerializeField] private float fieldOfView = 50.0f;

        [Header("Subtle Orbit Controls (Right Mouse Button)")]
        [SerializeField] private bool enableOrbit = true;
        [SerializeField] private float orbitSpeed = 3.0f;
        [SerializeField] private float minPitch = 25f;
        [SerializeField] private float maxPitch = 80f;

        private float _currentYaw = 0f;
        private float _currentPitch = 45f;
        private float _distance = 11.5f;
        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null)
            {
                _cam.fieldOfView = fieldOfView;
            }

            Vector3 diff = defaultPosition - lookTarget;
            _distance = diff.magnitude;
            transform.position = defaultPosition;
            transform.LookAt(lookTarget);
        }

        private void LateUpdate()
        {
            if (enableOrbit && Input.GetMouseButton(1)) // Hold RMB to inspect scene from different angles
            {
                _currentYaw += Input.GetAxis("Mouse X") * orbitSpeed;
                _currentPitch -= Input.GetAxis("Mouse Y") * orbitSpeed;
                _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

                Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, 0);
                transform.position = lookTarget + (rot * (Vector3.back * _distance));
                transform.LookAt(lookTarget);
            }
        }

        public void ResetCamera()
        {
            transform.position = defaultPosition;
            transform.LookAt(lookTarget);
            _currentPitch = 45f;
            _currentYaw = 0f;
        }
    }
}

