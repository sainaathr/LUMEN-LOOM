using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

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

            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 1. Set Player Identity
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.DefaultCompany.LUMENLOOM");
            PlayerSettings.companyName = "DefaultCompany";
            PlayerSettings.productName = "LUMEN LOOM";

            // 2. Set Color Space to Linear (Required by Quest OpenXR / URP)
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // 3. Set Scripting Backend to IL2CPP
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // 3. Set Target Architecture to ARM64 only (Oculus Quest requires ARM64)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // 4. Set Min SDK to Android 10.0 (API Level 29) & Target SDK to Android 14.0 (API Level 34)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // 5. Configure XR Plug-in Management & OpenXR Loader for Android
            ConfigureOpenXRForAndroid();

            Debug.Log("[AndroidBuildPipeline] Quest Android configuration complete: IL2CPP, ARM64, Min API 29, Target API 34, OpenXR Loader.");
        }

        public static void ConfigureOpenXRForAndroid()
        {
            BuildTargetGroup targetGroup = BuildTargetGroup.Android;
            string configKey = XRGeneralSettings.k_SettingsKey;
            string assetDir = "Assets/XR";
            string assetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

            if (!EditorBuildSettings.TryGetConfigObject(configKey, out XRGeneralSettingsPerBuildTarget buildTargetSettings))
            {
                buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(assetPath);
                if (buildTargetSettings == null)
                {
                    if (!Directory.Exists(assetDir))
                    {
                        Directory.CreateDirectory(assetDir);
                    }
                    buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(buildTargetSettings, assetPath);
                    AssetDatabase.SaveAssets();
                }
                EditorBuildSettings.AddConfigObject(configKey, buildTargetSettings, true);
            }

            var xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(targetGroup);
            if (xrSettings == null)
            {
                buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(targetGroup);
                xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(targetGroup);
            }

            if (xrSettings != null)
            {
                xrSettings.InitManagerOnStart = true;
                string openXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
                bool assigned = XRPackageMetadataStore.AssignLoader(xrSettings.Manager, openXRLoaderTypeName, targetGroup);
                Debug.Log($"[AndroidBuildPipeline] OpenXR Loader assigned for Android: {assigned}");

                // Enable OpenXR feature sets and individual features for Meta Quest
                var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                if (openXRSettings != null)
                {
                    foreach (var feature in openXRSettings.GetFeatures())
                    {
                        if (feature == null) continue;
                        string fname = feature.name.ToLower();
                        string typeName = feature.GetType().Name.ToLower();

                        if (fname.Contains("metaquest") || fname.Contains("handtracking") || fname.Contains("oculustouch") ||
                            typeName.Contains("metaquest") || typeName.Contains("handtracking") || typeName.Contains("oculustouch"))
                        {
                            feature.enabled = true;
                            EditorUtility.SetDirty(feature);
                            Debug.Log($"[AndroidBuildPipeline] Successfully enabled OpenXR feature: {feature.name} ({feature.GetType().Name})");
                        }
                    }

                    var questFeature = openXRSettings.GetFeature<MetaQuestFeature>();
                    if (questFeature != null)
                    {
                        questFeature.AddTargetDevice("quest3", "Quest 3", true);
                        EditorUtility.SetDirty(questFeature);
                        Debug.Log("[AndroidBuildPipeline] Explicitly registered Quest 3 target device on MetaQuestFeature.");
                    }

                    EditorUtility.SetDirty(openXRSettings);
                }

                EditorUtility.SetDirty(xrSettings);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogWarning("[AndroidBuildPipeline] Could not acquire XRGeneralSettings for Android.");
            }
        }

        public static void BuildQuestApk()
        {
            ConfigureQuestAndroidSettings();

            string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "LUMEN_LOOM_Quest"));
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
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[AndroidBuildPipeline] Build failed with result {summary.result}: {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }
    }
}
