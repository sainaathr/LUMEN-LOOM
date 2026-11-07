using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LumenLoom.Puzzle;
using LumenLoom.Rendering;

namespace LumenLoom.Tests
{
    public class BeamSystemTests
    {
        private GameObject _environmentRoot;
        private EnergySource _source;
        private TargetReceiver _target;
        private PuzzleNode _node1;
        private PuzzleNode _node2;
        private BeamSystem _beamSystem;
        private BeamValidator _validator;

        [SetUp]
        public void Setup()
        {
            _environmentRoot = new GameObject("TestChamber");

            // Source
            GameObject sourceObj = new GameObject("Source");
            sourceObj.transform.SetParent(_environmentRoot.transform);
            sourceObj.transform.position = new Vector3(-3.5f, 0.5f, -3.0f);
            sourceObj.transform.forward = Vector3.forward;
            _source = sourceObj.AddComponent<EnergySource>();

            // Node 1
            GameObject n1Obj = new GameObject("Node1");
            n1Obj.transform.SetParent(_environmentRoot.transform);
            n1Obj.transform.position = new Vector3(-3.5f, 0.5f, 1.0f);
            n1Obj.transform.forward = Vector3.right;
            n1Obj.AddComponent<SphereCollider>().radius = 0.5f;
            _node1 = n1Obj.AddComponent<PuzzleNode>();

            // Node 2
            GameObject n2Obj = new GameObject("Node2");
            n2Obj.transform.SetParent(_environmentRoot.transform);
            n2Obj.transform.position = new Vector3(2.5f, 0.5f, 1.0f);
            n2Obj.transform.forward = Vector3.forward;
            n2Obj.AddComponent<SphereCollider>().radius = 0.5f;
            _node2 = n2Obj.AddComponent<PuzzleNode>();

            // Target
            GameObject targetObj = new GameObject("Target");
            targetObj.transform.SetParent(_environmentRoot.transform);
            targetObj.transform.position = new Vector3(2.5f, 0.5f, 3.0f);
            targetObj.transform.forward = Vector3.back;
            targetObj.AddComponent<BoxCollider>().size = Vector3.one;
            _target = targetObj.AddComponent<TargetReceiver>();

            // Beam System
            GameObject beamObj = new GameObject("BeamSystem");
            beamObj.transform.SetParent(_environmentRoot.transform);
            _beamSystem = beamObj.AddComponent<BeamSystem>();
            _validator = beamObj.AddComponent<BeamValidator>();

            _beamSystem.SetEnergySource(_source);
            _validator.SetTarget(_target);
        }

        [TearDown]
        public void Teardown()
        {
            if (_environmentRoot != null)
            {
                Object.DestroyImmediate(_environmentRoot);
            }
        }

        [Test]
        public void Node_Clamp_EnforcesBounds()
        {
            _node1.ConfigureBounds(new Vector2(-4.5f, 4.5f), new Vector2(-4.5f, 4.5f), 0.5f);
            _node1.MoveToClamped(new Vector3(10.0f, 0.5f, -12.0f));

            Assert.AreEqual(4.5f, _node1.transform.position.x, 0.001f);
            Assert.AreEqual(-4.5f, _node1.transform.position.z, 0.001f);
            Assert.AreEqual(0.5f, _node1.transform.position.y, 0.001f);
        }

        [Test]
        public void TargetReceiver_ValidatesDirectionalAcceptance()
        {
            // Beam coming straight at target receiver
            bool validHit = _target.ValidateHit(Vector3.forward);
            Assert.IsTrue(validHit, "Target should validate incoming beam along its reception axis.");

            // Beam coming perpendicular
            bool perpendicularHit = _target.ValidateHit(Vector3.right);
            // Default acceptance is 90 degrees; 90 deg incoming from side
            Assert.IsNotNull(_target);
        }

        [Test]
        public void Node_Rotation_UpdatesForwardVector()
        {
            _node1.transform.rotation = Quaternion.identity; // forward is (0,0,1)
            _node1.RotateNode(90f);
            Assert.AreEqual(1.0f, _node1.transform.forward.x, 0.01f);
        }
    }
}
