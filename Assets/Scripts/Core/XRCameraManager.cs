using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using LumenLoom.Interaction;

#if !UNITY_SERVER
using UnityEngine.XR.Management;
#endif

namespace LumenLoom.Core
{
    /// <summary>
    /// Operating mode for the LUMEN LOOM spatial camera system.
    /// </summary>
    public enum CameraRigMode
    {
        Auto = 0,
        Desktop = 1,
        XR = 2
    }

    /// <summary>
    /// Orchestrates coexistence between Desktop (Orbit Camera + Mouse) and Meta Quest XR (XR Origin + 6DoF + Hands).
    /// Enforces:
    /// 1. Exactly one active AudioListener at all times (eliminates duplicate listener warnings).
    /// 2. Exactly one Camera tagged 'MainCamera' so Camera.main always resolves to the active viewpoint.
    /// 3. Zero-friction fallback to desktop mode when no OpenXR loader or XR headset is present.
    /// 4. Dynamic input provider switching on NodeInteractionController (Mouse vs Hand).
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class XRCameraManager : MonoBehaviour
    {
        [Header("Mode Configuration")]
        [SerializeField] private CameraRigMode configuredMode = CameraRigMode.Auto;

        [Header("Desktop Rig References")]
        [SerializeField] private Camera desktopCamera;
        [SerializeField] private CameraController desktopCameraController;
        [SerializeField] private AudioListener desktopAudioListener;

        [Header("XR Rig References")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera xrCamera;
        [SerializeField] private AudioListener xrAudioListener;
        [SerializeField] private TrackedPoseDriver xrTrackedPoseDriver;

        [Header("Interaction System References")]
        [SerializeField] private NodeInteractionController interactionController;
        [SerializeField] private MouseInputProvider mouseInputProvider;
        [SerializeField] private HandInputProvider handInputProvider;

        private CameraRigMode _activeMode = CameraRigMode.Desktop;
        private bool _isInitialized;

        public CameraRigMode ActiveMode => _activeMode;
        public Camera ActiveCamera => _activeMode == CameraRigMode.XR ? xrCamera : desktopCamera;
        public Camera DesktopCamera => desktopCamera;
        public Camera XRCamera => xrCamera;
        public XROrigin XROriginRig => xrOrigin;
        public AudioListener ActiveAudioListener => _activeMode == CameraRigMode.XR ? xrAudioListener : desktopAudioListener;
        public bool IsXREnvironmentActive => CheckXRSubsystemActive();

        public event Action<CameraRigMode> OnRigModeChanged;

        private void Awake()
        {
            InitializeRig();
        }

        public void InitializeRig()
        {
            ResolveReferences();

            CameraRigMode targetMode = configuredMode;
            if (targetMode == CameraRigMode.Auto)
            {
                targetMode = CheckXRSubsystemActive() ? CameraRigMode.XR : CameraRigMode.Desktop;
            }

            ApplyRigMode(targetMode);
            _isInitialized = true;
        }

        public void ResolveReferences()
        {
            // 1. Locate Desktop Camera components
            if (desktopCamera == null)
            {
                // First look for GameObject specifically named "Main Camera" or with CameraController
                CameraController cc = FindFirstObjectByType<CameraController>(FindObjectsInactive.Include);
                if (cc != null)
                {
                    desktopCamera = cc.GetComponent<Camera>();
                    desktopCameraController = cc;
                    desktopAudioListener = cc.GetComponent<AudioListener>();
                }
                else
                {
                    Camera[] cams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (int i = 0; i < cams.Length; i++)
                    {
                        if (cams[i].name.Contains("Main Camera") || cams[i].name.Contains("Desktop"))
                        {
                            desktopCamera = cams[i];
                            break;
                        }
                    }
                }
            }

            if (desktopCamera != null)
            {
                if (desktopCameraController == null) desktopCameraController = desktopCamera.GetComponent<CameraController>();
                if (desktopAudioListener == null) desktopAudioListener = desktopCamera.GetComponent<AudioListener>();
            }

            // 2. Locate XR Origin components
            if (xrOrigin == null)
            {
                xrOrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            }

            if (xrOrigin != null)
            {
                if (xrCamera == null)
                {
                    xrCamera = xrOrigin.Camera;
                    if (xrCamera == null)
                    {
                        xrCamera = xrOrigin.GetComponentInChildren<Camera>(true);
                    }
                }
            }

            if (xrCamera != null)
            {
                if (xrAudioListener == null) xrAudioListener = xrCamera.GetComponent<AudioListener>();
                if (xrTrackedPoseDriver == null) xrTrackedPoseDriver = xrCamera.GetComponent<TrackedPoseDriver>();
            }

            // 3. Locate Interaction references
            if (interactionController == null)
            {
                interactionController = FindFirstObjectByType<NodeInteractionController>(FindObjectsInactive.Include);
            }
            if (mouseInputProvider == null)
            {
                mouseInputProvider = FindFirstObjectByType<MouseInputProvider>(FindObjectsInactive.Include);
            }
            if (handInputProvider == null)
            {
                handInputProvider = FindFirstObjectByType<HandInputProvider>(FindObjectsInactive.Include);
            }
        }

        /// <summary>
        /// Detects whether an active OpenXR loader, XR display subsystem, or active XR device is present.
        /// </summary>
        public static bool CheckXRSubsystemActive()
        {
            // 1. Check UnityEngine.XR device flag
            if (XRSettings.isDeviceActive)
            {
                return true;
            }

            // 2. Check active XR loader from XRGeneralSettings
#if !UNITY_SERVER
            try
            {
                if (XRGeneralSettings.Instance != null &&
                    XRGeneralSettings.Instance.Manager != null &&
                    XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    return true;
                }
            }
            catch
            {
                // XRGeneralSettings can throw if called during early editor domain reload
            }

            // 3. Check running XRDisplaySubsystems
            try
            {
                var displayList = new List<XRDisplaySubsystem>();
                SubsystemManager.GetSubsystems(displayList);
                for (int i = 0; i < displayList.Count; i++)
                {
                    if (displayList[i].running)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Safely catch subsystem query exceptions
            }
#endif

            return false;
        }

        /// <summary>
        /// Explicitly changes the active camera rig mode between Desktop and XR.
        /// </summary>
        public void SetRigMode(CameraRigMode mode)
        {
            if (mode == CameraRigMode.Auto)
            {
                mode = CheckXRSubsystemActive() ? CameraRigMode.XR : CameraRigMode.Desktop;
            }

            ApplyRigMode(mode);
        }

        private void ApplyRigMode(CameraRigMode mode)
        {
            _activeMode = mode;

            if (mode == CameraRigMode.XR)
            {
                ActivateXRMode();
            }
            else
            {
                ActivateDesktopMode();
            }

            OnRigModeChanged?.Invoke(_activeMode);
        }

        private void ActivateDesktopMode()
        {
            // 1. Deactivate XR Origin and XR Camera
            if (xrOrigin != null)
            {
                xrOrigin.gameObject.SetActive(false);
            }
            if (xrCamera != null)
            {
                xrCamera.tag = "Untagged";
                xrCamera.enabled = false;
                if (xrAudioListener != null)
                {
                    xrAudioListener.enabled = false;
                }
            }

            // 2. Activate Desktop Camera & AudioListener
            if (desktopCamera != null)
            {
                desktopCamera.gameObject.SetActive(true);
                desktopCamera.enabled = true;
                desktopCamera.tag = "MainCamera";

                if (desktopCameraController != null)
                {
                    desktopCameraController.enabled = true;
                }
                if (desktopAudioListener != null)
                {
                    desktopAudioListener.enabled = true;
                }
            }

            // 3. Ensure NodeInteractionController is bound to MouseInputProvider
            if (interactionController != null && mouseInputProvider != null)
            {
                interactionController.SetInputProvider(mouseInputProvider);
            }

            Debug.Log("[XRCameraManager] Activated Desktop Mode (Orbit Camera, Mouse Interaction, Single AudioListener).");
        }

        private void ActivateXRMode()
        {
            // 1. Deactivate Desktop Camera controls and AudioListener
            if (desktopCameraController != null)
            {
                desktopCameraController.enabled = false;
            }
            if (desktopAudioListener != null)
            {
                desktopAudioListener.enabled = false;
            }
            if (desktopCamera != null)
            {
                desktopCamera.tag = "Untagged";
                desktopCamera.enabled = false;
                desktopCamera.gameObject.SetActive(false);
            }

            // 2. Activate XR Origin & XR Camera
            if (xrOrigin != null)
            {
                xrOrigin.gameObject.SetActive(true);
            }
            if (xrCamera != null)
            {
                xrCamera.gameObject.SetActive(true);
                xrCamera.enabled = true;
                xrCamera.tag = "MainCamera";

                if (xrAudioListener != null)
                {
                    xrAudioListener.enabled = true;
                }
                if (xrTrackedPoseDriver != null)
                {
                    xrTrackedPoseDriver.enabled = true;
                }
            }

            // 3. Ensure NodeInteractionController is bound to HandInputProvider (or fallback to mouse if unavailable)
            if (interactionController != null)
            {
                if (handInputProvider != null)
                {
                    interactionController.SetInputProvider(handInputProvider);
                }
                else if (mouseInputProvider != null)
                {
                    interactionController.SetInputProvider(mouseInputProvider);
                }
            }

            Debug.Log("[XRCameraManager] Activated XR Mode (XR Origin, 6DoF TrackedPoseDriver, Hand Interaction, Single AudioListener).");
        }

        /// <summary>
        /// Convenience method to configure references programmatically.
        /// </summary>
        public void ConfigureReferences(
            Camera desktopCam,
            CameraController desktopCtrl,
            AudioListener desktopListener,
            XROrigin origin,
            Camera xrCam,
            AudioListener xrListener,
            TrackedPoseDriver poseDriver,
            NodeInteractionController nodeCtrl,
            MouseInputProvider mouseInput,
            HandInputProvider handInput)
        {
            desktopCamera = desktopCam;
            desktopCameraController = desktopCtrl;
            desktopAudioListener = desktopListener;
            xrOrigin = origin;
            xrCamera = xrCam;
            xrAudioListener = xrListener;
            xrTrackedPoseDriver = poseDriver;
            interactionController = nodeCtrl;
            mouseInputProvider = mouseInput;
            handInputProvider = handInput;
        }
    }
}

