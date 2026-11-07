using System;
using System.Collections.Generic;
using UnityEngine;
using LumenLoom.Puzzle;

namespace LumenLoom.Data
{
    [Serializable]
    public struct NodeSpawnConfig
    {
        public string nodeId;
        public Vector3 position;
        public Vector3 rotationEuler;
        public NodeRedirectType redirectType;
        public bool allowMovement;
        public bool allowRotation;
    }

    [Serializable]
    public struct ObstacleConfig
    {
        public Vector3 position;
        public Vector3 size;
    }

    /// <summary>
    /// ScriptableObject defining puzzle layouts, boundaries, and validation criteria.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "LumenLoom/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Information")]
        public string levelTitle = "Chamber 01: Divergence";
        [TextArea(2, 4)]
        public string objectiveText = "Align the optical nodes to guide the energy beam into the receiver.";

        [Header("Playfield Dimensions")]
        public Vector2 xBounds = new Vector2(-4.5f, 4.5f);
        public Vector2 zBounds = new Vector2(-4.5f, 4.5f);
        public float puzzlePlaneY = 0.5f;

        [Header("Source & Target Setup")]
        public Vector3 sourcePosition = new Vector3(-3.5f, 0.5f, -3.0f);
        public Vector3 sourceDirection = Vector3.forward;
        public Vector3 targetPosition = new Vector3(3.5f, 0.5f, 3.0f);
        public Vector3 targetForward = Vector3.back;
        public int minNodesRequired = 2;

        [Header("Nodes Layout")]
        public List<NodeSpawnConfig> initialNodes = new List<NodeSpawnConfig>();

        [Header("Obstacles")]
        public List<ObstacleConfig> obstacles = new List<ObstacleConfig>();
    }
}
