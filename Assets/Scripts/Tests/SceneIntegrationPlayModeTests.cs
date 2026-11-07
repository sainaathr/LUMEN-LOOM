using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LumenLoom.Core;
using LumenLoom.Puzzle;
using LumenLoom.Rendering;

namespace LumenLoom.Tests
{
    public class SceneIntegrationPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainPuzzleScene");
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainPuzzleScene_LoadsAllCoreManagersAndEntities()
        {
            Assert.IsNotNull(GameManager.Instance, "GameManager instance should be active in scene.");

            PuzzleManager puzzleManager = Object.FindFirstObjectByType<PuzzleManager>();
            Assert.IsNotNull(puzzleManager, "PuzzleManager should be active in scene.");

            EnergySource source = Object.FindFirstObjectByType<EnergySource>();
            Assert.IsNotNull(source, "EnergySource should be present in scene.");

            TargetReceiver target = Object.FindFirstObjectByType<TargetReceiver>();
            Assert.IsNotNull(target, "TargetReceiver should be present in scene.");

            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            Assert.IsTrue(nodes.Length >= 2, "Scene should contain at least two PuzzleNodes.");

            BeamSystem beamSystem = Object.FindFirstObjectByType<BeamSystem>();
            Assert.IsNotNull(beamSystem, "BeamSystem should be present in scene.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator PuzzleNode_DirectManipulation_RotatesCorrectly()
        {
            PuzzleNode[] nodes = Object.FindObjectsByType<PuzzleNode>(FindObjectsSortMode.None);
            Assert.IsTrue(nodes.Length > 0, "At least one node required.");

            PuzzleNode node = nodes[0];
            float initialAngle = node.transform.eulerAngles.y;
            node.RotateNode(45f);

            Assert.AreEqual((initialAngle + 45f) % 360f, node.transform.eulerAngles.y, 0.05f);
            yield return null;
        }
    }
}
