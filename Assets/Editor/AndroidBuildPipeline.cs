using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LumenLoom.Editor
{
    /// <summary>
    /// Automation script for configuring Quest Android ARM64 settings and generating APK builds.
    /// </summary>
    public static class AndroidBuildPipeline
    {
        public static void ConfigureQuestAndroidSettings()
        {
            Debug.Log("[AndroidBuildPipeline] Configuring Quest Android Settings...");

            // Set Application Identifier
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.DefaultCompany.LUMENLOOM");
            PlayerSettings.companyName = "DefaultCompany";
            PlayerSettings.productName = "LUMEN LOOM";

            // Set Scripting Backend to IL2CPP
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // Set Target Architecture to ARM64 only (Oculus Quest requires ARM64)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Set Min SDK to Android 10.0 (API Level 29) & Target SDK to Android 14.0 (API Level 34)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            Debug.Log("[AndroidBuildPipeline] Quest Android configuration complete: IL2CPP, ARM64, Min API 29, Target API 34.");
        }

        public static void BuildQuestApk()
        {
            ConfigureQuestAndroidSettings();

            string buildDir = "D:\\Unity\\Builds\\LUMEN_LOOM_Quest";
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string apkPath = Path.Combine(buildDir, "LumenLoom_Quest.apk");
            Debug.Log($"[AndroidBuildPipeline] Starting APK build targeting: {apkPath}");

            string[] scenes = new string[]
            {
                "Assets/Scenes/MainPuzzleScene.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuildPipeline] Build succeeded! Size: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1} seconds.");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[AndroidBuildPipeline] Build failed with {summary.totalErrors} errors.");
            }
        }
    }
}
