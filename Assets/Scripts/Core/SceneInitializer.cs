using UnityEngine;
using LumenLoom.Puzzle;
using LumenLoom.Rendering;
using LumenLoom.Interaction;
using LumenLoom.Audio;
using LumenLoom.UI;

namespace LumenLoom.Core
{
    /// <summary>
    /// Automatic scene orchestrator.
    /// Ensures that when the scene starts or is opened in Unity, all required chamber geometry,
    /// energy nodes, materials, lighting, and game managers are instantiated and wired up.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneInitializer : MonoBehaviour
    {
        [Header("Auto Setup Options")]
        [SerializeField] private bool autoConstructIfEmpty = true;

        private void Awake()
        {
            if (autoConstructIfEmpty)
            {
                EnsureChamberBuilt();
            }
        }

        public void EnsureChamberBuilt()
        {
            // 1. Ensure Lighting & Environment
            SetupEnvironment();

            // 2. Ensure Audio Feedback Manager
            AudioFeedbackManager audioMgr = FindFirstObjectByType<AudioFeedbackManager>();
            if (audioMgr == null)
            {
                GameObject audioObj = new GameObject("[Audio_Manager]");
                audioMgr = audioObj.AddComponent<AudioFeedbackManager>();
            }

            // 3. Ensure Beam System & Renderer
            BeamSystem beamSys = FindFirstObjectByType<BeamSystem>();
            BeamRenderer beamRend = FindFirstObjectByType<BeamRenderer>();
            BeamValidator beamVal = FindFirstObjectByType<BeamValidator>();

            if (beamSys == null)
            {
                GameObject beamObj = new GameObject("[Beam_System]");
                beamSys = beamObj.AddComponent<BeamSystem>();
                beamRend = beamObj.AddComponent<BeamRenderer>();
                beamVal = beamObj.AddComponent<BeamValidator>();
            }

            // 4. Ensure Energy Source
            EnergySource source = FindFirstObjectByType<EnergySource>();
            if (source == null)
            {
                source = CreateEnergySource(new Vector3(-3.5f, 0.5f, -3.0f), Vector3.forward);
            }

            // 5. Ensure Target Receiver
            TargetReceiver target = FindFirstObjectByType<TargetReceiver>();
            if (target == null)
            {
                target = CreateTargetReceiver(new Vector3(2.5f, 0.5f, 3.0f), Vector3.back);
            }

            // 6. Ensure Interactive Puzzle Nodes (at least 2 nodes)
            PuzzleNode[] existingNodes = FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            if (existingNodes == null || existingNodes.Length < 2)
            {
                if (existingNodes == null || existingNodes.Length == 0)
                {
                    // Node 1: Diverter node
                    CreatePuzzleNode("Node_Alpha", new Vector3(-3.5f, 0.5f, 1.0f), Quaternion.Euler(0, 45f, 0));
                    // Node 2: Aligner node
                    CreatePuzzleNode("Node_Beta", new Vector3(0.0f, 0.5f, 1.0f), Quaternion.Euler(0, 135f, 0));
                }
            }

            // 7. Ensure PuzzleManager & GameManager
            PuzzleManager puzzleMgr = FindFirstObjectByType<PuzzleManager>();
            if (puzzleMgr == null)
            {
                GameObject puzObj = new GameObject("[Puzzle_Manager]");
                puzzleMgr = puzObj.AddComponent<PuzzleManager>();
            }

            GameManager gameMgr = FindFirstObjectByType<GameManager>();
            if (gameMgr == null)
            {
                GameObject gmObj = new GameObject("[Game_Manager]");
                gameMgr = gmObj.AddComponent<GameManager>();
            }

            // 8. Ensure Interaction Systems
            MouseInputProvider mouseInput = FindFirstObjectByType<MouseInputProvider>();
            if (mouseInput == null)
            {
                GameObject inputObj = new GameObject("[Input_Provider_Mouse]");
                mouseInput = inputObj.AddComponent<MouseInputProvider>();
            }

            NodeInteractionController controller = FindFirstObjectByType<NodeInteractionController>();
            if (controller == null)
            {
                GameObject ctrlObj = new GameObject("[Interaction_Controller]");
                controller = ctrlObj.AddComponent<NodeInteractionController>();
                controller.SetInputProvider(mouseInput);
            }

            // 9. Ensure UI Manager
            UIManager uiMgr = FindFirstObjectByType<UIManager>();
            if (uiMgr == null)
            {
                GameObject uiObj = new GameObject("[UI_Manager]");
                uiMgr = uiObj.AddComponent<UIManager>();
            }

            // 10. Wire references
            beamSys.SetEnergySource(source);
            beamVal.SetTarget(target);
            beamSys.RefreshSceneNodes();
            beamSys.RecalculateBeam();
        }

        private void SetupEnvironment()
        {
            // Floor pedestal
            GameObject floor = GameObject.Find("Chamber_Floor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Chamber_Floor";
                floor.transform.position = new Vector3(0, -0.1f, 0);
                floor.transform.localScale = new Vector3(11.0f, 0.2f, 11.0f);

                Material floorMat = Resources.Load<Material>("Mat_SciFi_DarkFloor");
                if (floorMat == null)
                {
                    floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    floorMat.color = new Color(0.04f, 0.06f, 0.1f, 1f);
                }
                floor.GetComponent<MeshRenderer>().material = floorMat;
            }

            // Boundary rails
            GameObject boundary = GameObject.Find("Chamber_Boundary");
            if (boundary == null)
            {
                boundary = new GameObject("Chamber_Boundary");
                CreateBoundaryRail(boundary.transform, new Vector3(0, 0.1f, -4.6f), new Vector3(9.4f, 0.2f, 0.2f));
                CreateBoundaryRail(boundary.transform, new Vector3(0, 0.1f, 4.6f), new Vector3(9.4f, 0.2f, 0.2f));
                CreateBoundaryRail(boundary.transform, new Vector3(-4.6f, 0.1f, 0), new Vector3(0.2f, 0.2f, 9.4f));
                CreateBoundaryRail(boundary.transform, new Vector3(4.6f, 0.1f, 0), new Vector3(0.2f, 0.2f, 9.4f));
            }

            // Camera setup
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }
            if (cam.GetComponent<CameraController>() == null)
            {
                cam.gameObject.AddComponent<CameraController>();
            }

            // Directional Light
            Light dirLight = FindFirstObjectByType<Light>();
            if (dirLight == null || dirLight.type != LightType.Directional)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light l = lightObj.AddComponent<Light>();
                l.type = LightType.Directional;
                l.color = new Color(0.85f, 0.92f, 1.0f);
                l.intensity = 0.9f;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            }
        }

        private void CreateBoundaryRail(Transform parent, Vector3 localPos, Vector3 localScale)
        {
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Rail";
            rail.transform.SetParent(parent);
            rail.transform.localPosition = localPos;
            rail.transform.localScale = localScale;

            Material railMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            railMat.color = new Color(0.1f, 0.25f, 0.4f, 1f);
            rail.GetComponent<MeshRenderer>().material = railMat;
        }

        private EnergySource CreateEnergySource(Vector3 position, Vector3 forwardDir)
        {
            GameObject sourceObj = new GameObject("Energy_Source");
            sourceObj.transform.position = position;
            sourceObj.transform.forward = forwardDir;

            // Pedestal base
            GameObject baseCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseCylinder.name = "Base";
            baseCylinder.transform.SetParent(sourceObj.transform);
            baseCylinder.transform.localPosition = new Vector3(0, -0.3f, 0);
            baseCylinder.transform.localScale = new Vector3(0.9f, 0.2f, 0.9f);

            // Emitter core
            GameObject coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreSphere.name = "Emitter_Core";
            coreSphere.transform.SetParent(sourceObj.transform);
            coreSphere.transform.localPosition = Vector3.zero;
            coreSphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Rotating Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Focus_Ring";
            ring.transform.SetParent(sourceObj.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(0.75f, 0.05f, 0.75f);

            // Add Light
            GameObject lightObj = new GameObject("Source_Glow");
            lightObj.transform.SetParent(sourceObj.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light glowLight = lightObj.AddComponent<Light>();
            glowLight.color = new Color(0.0f, 0.85f, 1.0f);
            glowLight.intensity = 2.0f;
            glowLight.range = 5.0f;

            // Add script
            EnergySource sourceComp = sourceObj.AddComponent<EnergySource>();
            return sourceComp;
        }

        private TargetReceiver CreateTargetReceiver(Vector3 position, Vector3 forwardDir)
        {
            GameObject targetObj = new GameObject("Target_Receiver");
            targetObj.transform.position = position;
            targetObj.transform.forward = forwardDir;

            // Base
            GameObject baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.name = "Receiver_Base";
            baseCube.transform.SetParent(targetObj.transform);
            baseCube.transform.localPosition = new Vector3(0, -0.3f, 0);
            baseCube.transform.localScale = new Vector3(1.1f, 0.25f, 1.1f);

            // Sensor Core
            GameObject coreCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coreCube.name = "Sensor_Core";
            coreCube.transform.SetParent(targetObj.transform);
            coreCube.transform.localPosition = Vector3.zero;
            coreCube.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            // Add Light
            GameObject lightObj = new GameObject("Target_Light");
            lightObj.transform.SetParent(targetObj.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light glow = lightObj.AddComponent<Light>();
            glow.color = new Color(0.1f, 0.2f, 0.3f);
            glow.intensity = 1.0f;
            glow.range = 4.0f;

            // Add BoxCollider for beam hit detection
            BoxCollider col = targetObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.9f, 0.9f, 0.9f);

            TargetReceiver targetComp = targetObj.AddComponent<TargetReceiver>();
            return targetComp;
        }

        private PuzzleNode CreatePuzzleNode(string nodeName, Vector3 position, Quaternion rotation)
        {
            GameObject nodeObj = new GameObject(nodeName);
            nodeObj.transform.position = position;
            nodeObj.transform.rotation = rotation;

            // Cylindrical housing
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Node_Housing";
            body.transform.SetParent(nodeObj.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);

            // Optical lens / arrow pointing forward
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.name = "Direction_Pointer";
            pointer.transform.SetParent(nodeObj.transform);
            pointer.transform.localPosition = new Vector3(0, 0.25f, 0.45f);
            pointer.transform.localScale = new Vector3(0.2f, 0.15f, 0.4f);

            // Glow Light
            GameObject lightObj = new GameObject("Node_Glow");
            lightObj.transform.SetParent(nodeObj.transform);
            lightObj.transform.localPosition = new Vector3(0, 0.3f, 0);
            Light nodeLight = lightObj.AddComponent<Light>();
            nodeLight.color = new Color(0.1f, 0.6f, 1.0f);
            nodeLight.intensity = 1.5f;
            nodeLight.range = 3.5f;

            // Collider for interaction & raycast hit
            SphereCollider col = nodeObj.AddComponent<SphereCollider>();
            col.radius = 0.55f;

            PuzzleNode nodeComp = nodeObj.AddComponent<PuzzleNode>();
            return nodeComp;
        }
    }
}

