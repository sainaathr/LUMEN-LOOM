using System;
using UnityEngine;
using LumenLoom.Puzzle;

namespace LumenLoom.Interaction
{
    /// <summary>
    /// Mouse & Keyboard implementation of IInputProvider.
    /// Simulates spatial direct manipulation for desktop prototyping on Intel Iris Xe laptops without VR hardware.
    /// </summary>
    public class MouseInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Camera & Plane")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float interactionPlaneY = 0.5f;
        [SerializeField] private LayerMask interactiveMask = ~0;

        [Header("Control Parameters")]
        [SerializeField] private float rotationSpeedKeyboard = 90.0f; // degrees per second (Q/E)
        [SerializeField] private float rotationSpeedWheel = 15.0f;    // degrees per scroll tick
        [SerializeField] private bool allowScrollWheelRotation = true;

        public event Action<PuzzleNode> OnNodeSelected;
        public event Action<Vector3> OnNodeDragPositionUpdated;
        public event Action<float> OnNodeRotationDelta;
        public event Action OnNodeReleased;
        public event Action OnResetRequested;
        public event Action OnDeselectRequested;

        private bool _isDragging;
        private PuzzleNode _hoveredNode;
        private PuzzleNode _selectedNode;
        private Plane _dragPlane;

        public bool IsDragging => _isDragging;
        public PuzzleNode HoveredNode => _hoveredNode;
        public PuzzleNode SelectedNode => _selectedNode;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            _dragPlane = new Plane(Vector3.up, new Vector3(0, interactionPlaneY, 0));
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            HandleHover();
            HandleSelectionAndDrag();
            HandleRotation();
            HandleUtilityKeys();
        }

        private void HandleHover()
        {
            if (_isDragging) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactiveMask))
            {
                PuzzleNode node = hit.collider.GetComponentInParent<PuzzleNode>();
                if (node != _hoveredNode)
                {
                    if (_hoveredNode != null && _hoveredNode != _selectedNode)
                    {
                        _hoveredNode.SetHovered(false);
                    }
                    _hoveredNode = node;
                    if (_hoveredNode != null)
                    {
                        _hoveredNode.SetHovered(true);
                    }
                }
            }
            else
            {
                if (_hoveredNode != null && _hoveredNode != _selectedNode)
                {
                    _hoveredNode.SetHovered(false);
                }
                _hoveredNode = null;
            }
        }

        private void HandleSelectionAndDrag()
        {
            // Left click down: select
            if (Input.GetMouseButtonDown(0))
            {
                if (_hoveredNode != null)
                {
                    SelectNode(_hoveredNode);
                    _isDragging = true;
                }
                else
                {
                    // Clicked empty space
                    DeselectCurrentNode();
                }
            }

            // Dragging held
            if (_isDragging && _selectedNode != null)
            {
                if (Input.GetMouseButton(0))
                {
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                    if (_dragPlane.Raycast(ray, out float enter))
                    {
                        Vector3 hitWorldPoint = ray.GetPoint(enter);
                        OnNodeDragPositionUpdated?.Invoke(hitWorldPoint);
                    }
                }
                else
                {
                    // Released button
                    _isDragging = false;
                    OnNodeReleased?.Invoke();
                }
            }
        }

        private void HandleRotation()
        {
            if (_selectedNode == null) return;

            float rotDelta = 0f;

            // Q / E Keys
            if (Input.GetKey(KeyCode.Q))
            {
                rotDelta += rotationSpeedKeyboard * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                rotDelta -= rotationSpeedKeyboard * Time.deltaTime;
            }

            // Mouse wheel support
            if (allowScrollWheelRotation)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    rotDelta += scroll * rotationSpeedWheel;
                }
            }

            if (Mathf.Abs(rotDelta) > 0.0001f)
            {
                OnNodeRotationDelta?.Invoke(rotDelta);
            }
        }

        private void HandleUtilityKeys()
        {
            // Reset key (R)
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnResetRequested?.Invoke();
            }

            // Deselect key (Escape)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectCurrentNode();
                OnDeselectRequested?.Invoke();
            }
        }

        public void SelectNode(PuzzleNode node)
        {
            if (_selectedNode != null && _selectedNode != node)
            {
                _selectedNode.SetSelected(false);
            }

            _selectedNode = node;

            if (_selectedNode != null)
            {
                _selectedNode.SetSelected(true);
                OnNodeSelected?.Invoke(_selectedNode);
            }
        }

        public void DeselectCurrentNode()
        {
            if (_selectedNode != null)
            {
                _selectedNode.SetSelected(false);
                _selectedNode = null;
            }
            _isDragging = false;
        }
    }
}

