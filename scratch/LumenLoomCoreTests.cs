using System;
using System.Collections.Generic;

namespace LumenLoom.Tests
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 back { get { return new Vector3(0, 0, -1); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }

        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }

        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }
        public Vector3 normalized
        {
            get
            {
                float mag = magnitude;
                return mag > 1e-5f ? new Vector3(x / mag, y / mag, z / mag) : zero;
            }
        }

        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }

        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
        {
            float num = -2f * Dot(inNormal, inDirection);
            return new Vector3(inNormal.x * num + inDirection.x, inNormal.y * num + inDirection.y, inNormal.z * num + inDirection.z);
        }

        public override string ToString() { return string.Format("({0:F2}, {1:F2}, {2:F2})", x, y, z); }
    }

    public class MockNode
    {
        public string Id;
        public Vector3 Position;
        public Vector3 Forward;
        public bool IsConducting;

        public MockNode(string id, Vector3 pos, Vector3 fwd)
        {
            Id = id;
            Position = pos;
            Forward = fwd.normalized;
        }

        public Vector3 CalculateOutputDirection(Vector3 incomingDir)
        {
            return Forward;
        }

        public void MoveToClamped(Vector3 target, float minX, float maxX, float minZ, float maxZ)
        {
            float cx = Math.Max(minX, Math.Min(maxX, target.x));
            float cz = Math.Max(minZ, Math.Min(maxZ, target.z));
            Position = new Vector3(cx, Position.y, cz);
        }

        public void RotateDegreesY(float degrees)
        {
            float rad = degrees * (float)Math.PI / 180f;
            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);
            float newX = Forward.x * cos + Forward.z * sin;
            float newZ = -Forward.x * sin + Forward.z * cos;
            Forward = new Vector3(newX, 0, newZ).normalized;
        }
    }

    public class MockTarget
    {
        public Vector3 Position;
        public Vector3 Forward;
        public float AcceptanceAngleDeg;
        public bool IsEnergized;

        public MockTarget(Vector3 pos, Vector3 fwd, float angleDeg)
        {
            Position = pos;
            Forward = fwd.normalized;
            AcceptanceAngleDeg = angleDeg;
        }

        public bool ValidateHit(Vector3 incomingBeamDir)
        {
            float dot = Vector3.Dot(-incomingBeamDir.normalized, Forward);
            dot = Math.Max(-1f, Math.Min(1f, dot));
            float angle = (float)Math.Acos(dot) * (180f / (float)Math.PI);
            return angle <= (AcceptanceAngleDeg * 0.5f);
        }
    }

    public class Program
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;

        public static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" LUMEN LOOM - CORE LOGIC & ALGORITHMIC TEST SUITE ");
            Console.WriteLine("==================================================");

            Test_ForwardRedirection();
            Test_SpecularReflection();
            Test_BoundaryClamping();
            Test_NodeRotation();
            Test_TargetAcceptanceAngle();
            Test_InfiniteLoopDetection();
            Test_EndToEndPuzzleSolvePath();

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(string.Format("RESULTS: {0} / {1} TESTS PASSED.", _testsPassed, _testsRun));
            Console.WriteLine("==================================================");

            if (_testsPassed == _testsRun)
            {
                Environment.Exit(0);
            }
            else
            {
                Environment.Exit(1);
            }
        }

        private static void Assert(bool condition, string testName, string details)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine("[PASS] " + testName);
            }
            else
            {
                Console.WriteLine("[FAIL] " + testName + " - " + details);
            }
        }

        private static void Test_ForwardRedirection()
        {
            MockNode node = new MockNode("N1", new Vector3(0, 0, 0), Vector3.right);
            Vector3 incoming = Vector3.forward;
            Vector3 output = node.CalculateOutputDirection(incoming);

            Assert(Math.Abs(output.x - 1.0f) < 1e-4f && Math.Abs(output.z) < 1e-4f,
                "Forward Redirection Test: Incoming beam is redirected along node's forward vector (X axis).", "");
        }

        private static void Test_SpecularReflection()
        {
            Vector3 inDir = new Vector3(1, 0, 0); // Moving +X
            Vector3 normal = new Vector3(-1, 0, 1).normalized; // 45 degree mirror surface
            Vector3 reflected = Vector3.Reflect(inDir, normal).normalized;

            Assert(Math.Abs(reflected.z - 1.0f) < 1e-3f && Math.Abs(reflected.x) < 1e-3f,
                "Specular Reflection Test: Beam bounces 90 degrees according to Snell's law.", "");
        }

        private static void Test_BoundaryClamping()
        {
            MockNode node = new MockNode("N_Clamp", new Vector3(0, 0.5f, 0), Vector3.forward);
            node.MoveToClamped(new Vector3(10.0f, 0.5f, -15.0f), -4.5f, 4.5f, -4.5f, 4.5f);

            Assert(Math.Abs(node.Position.x - 4.5f) < 1e-4f && Math.Abs(node.Position.z - (-4.5f)) < 1e-4f,
                "Boundary Clamping Test: Node position stays clamped within [-4.5, 4.5] chamber boundary.", "");
        }

        private static void Test_NodeRotation()
        {
            MockNode node = new MockNode("N_Rot", new Vector3(0, 0.5f, 0), Vector3.forward); // Facing +Z
            node.RotateDegreesY(90f); // Rotate 90 deg clockwise around Y -> should face +X

            Assert(Math.Abs(node.Forward.x - 1.0f) < 1e-3f && Math.Abs(node.Forward.z) < 1e-3f,
                "Node Rotation Test: Rotating 90 degrees around Y aligns node precisely with +X axis.", "");
        }

        private static void Test_TargetAcceptanceAngle()
        {
            MockTarget target = new MockTarget(new Vector3(0, 0, 5), Vector3.back, 90f); // Faces -Z

            bool hitHeadOn = target.ValidateHit(Vector3.forward);
            bool hitPerpendicular = target.ValidateHit(Vector3.right);

            Assert(hitHeadOn && !hitPerpendicular,
                "Target Acceptance Test: Head-on hit validates; perpendicular hit is rejected.", "");
        }

        private static void Test_InfiniteLoopDetection()
        {
            MockNode nodeA = new MockNode("NodeA", new Vector3(0, 0, 0), Vector3.forward);
            MockNode nodeB = new MockNode("NodeB", new Vector3(0, 0, 5), Vector3.back);

            HashSet<MockNode> visited = new HashSet<MockNode>();
            bool loopDetected = false;
            MockNode current = nodeA;

            for (int bounce = 0; bounce < 10; bounce++)
            {
                if (visited.Contains(current))
                {
                    loopDetected = true;
                    break;
                }
                visited.Add(current);
                current = (current == nodeA) ? nodeB : nodeA;
            }

            Assert(loopDetected,
                "Infinite Loop Detection Test: Visited node set terminates cycle cleanly without hanging.", "");
        }

        private static void Test_EndToEndPuzzleSolvePath()
        {
            Vector3 sourcePos = new Vector3(-3.5f, 0.5f, -3.0f);
            Vector3 sourceDir = Vector3.forward;

            MockNode node1 = new MockNode("N1", new Vector3(-3.5f, 0.5f, 1.0f), Vector3.right);
            MockNode node2 = new MockNode("N2", new Vector3(2.5f, 0.5f, 1.0f), Vector3.forward);
            MockTarget target = new MockTarget(new Vector3(2.5f, 0.5f, 3.0f), Vector3.back, 90f);

            Vector3 beamOrigin = sourcePos;
            Vector3 beamDir = sourceDir;

            bool reachedNode1 = (Math.Abs(node1.Position.x - beamOrigin.x) < 0.1f && node1.Position.z > beamOrigin.z);
            beamOrigin = node1.Position;
            beamDir = node1.CalculateOutputDirection(beamDir);

            bool reachedNode2 = (Math.Abs(node2.Position.z - beamOrigin.z) < 0.1f && node2.Position.x > beamOrigin.x);
            beamOrigin = node2.Position;
            beamDir = node2.CalculateOutputDirection(beamDir);

            bool targetHit = (Math.Abs(target.Position.x - beamOrigin.x) < 0.1f && target.Position.z > beamOrigin.z);
            bool targetEnergized = target.ValidateHit(beamDir);

            Assert(reachedNode1 && reachedNode2 && targetHit && targetEnergized,
                "End-to-End Chamber 01 Solve Test: Light travels Source -> Node 1 -> Node 2 -> Target Receiver successfully.", "");
        }
    }
}
