using System;
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace LumenLoom.Editor
{
    /// <summary>
    /// Post-generation hook to ensure AndroidManifest.xml includes canonical 'quest3' in com.oculus.supportedDevices.
    /// Runs after OpenXR and XR Management manifest providers (callbackOrder 100).
    /// </summary>
    public class QuestPostGenerateGradleAndroidProject : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Debug.Log($"[QuestPostGenerateGradle] Executing manifest verification in: {path}");

            // Locate all AndroidManifest.xml files under the Gradle project directory and its parent root
            string searchDir = path;
            DirectoryInfo parent = Directory.GetParent(path);
            if (parent != null && parent.Exists && (Directory.GetFiles(parent.FullName, "build.gradle").Length > 0 || Directory.GetFiles(parent.FullName, "settings.gradle").Length > 0))
            {
                searchDir = parent.FullName;
            }

            string[] manifestFiles = Directory.GetFiles(searchDir, "AndroidManifest.xml", SearchOption.AllDirectories);
            Debug.Log($"[QuestPostGenerateGradle] Found {manifestFiles.Length} manifest file(s) to inspect under {searchDir}");

            foreach (string manifestPath in manifestFiles)
            {
                ModifyManifest(manifestPath);
            }
        }

        private void ModifyManifest(string manifestPath)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(manifestPath);

                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("android", "http://schemas.android.com/apk/res/android");

                var metaNodes = doc.SelectNodes("//meta-data[@android:name='com.oculus.supportedDevices']", nsmgr);
                if (metaNodes != null && metaNodes.Count > 0)
                {
                    bool modified = false;
                    foreach (XmlNode node in metaNodes)
                    {
                        var valueAttr = node.Attributes?["android:value"];
                        if (valueAttr != null)
                        {
                            string curVal = valueAttr.Value;
                            Debug.Log($"[QuestPostGenerateGradle] Found supportedDevices in {Path.GetFileName(manifestPath)}: {curVal}");
                            if (!curVal.Contains("quest3"))
                            {
                                valueAttr.Value = curVal + "|quest3";
                                modified = true;
                                Debug.Log($"[QuestPostGenerateGradle] Updated supportedDevices in {Path.GetFileName(manifestPath)} to: {valueAttr.Value}");
                            }
                            else
                            {
                                Debug.Log($"[QuestPostGenerateGradle] supportedDevices already contains quest3: {curVal}");
                            }
                        }
                    }

                    if (modified)
                    {
                        doc.Save(manifestPath);
                        Debug.Log($"[QuestPostGenerateGradle] Successfully saved modified manifest at {manifestPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestPostGenerateGradle] Failed modifying manifest at {manifestPath}: {ex.Message}");
            }
        }
    }
}

