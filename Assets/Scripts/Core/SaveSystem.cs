using System;
using System.IO;
using UnityEngine;

namespace LumenLoom.Core
{
    /// <summary>
    /// Persistent save system for LUMEN LOOM.
    /// Stores progress as versioned JSON in Application.persistentDataPath.
    /// Thread-safe reads; writes happen on the Unity main thread only.
    /// </summary>
    public static class SaveSystem
    {
        // ── Constants ──────────────────────────────────────────────────────────
        private const int    CURRENT_VERSION   = 1;
        private const string SAVE_FILENAME     = "lumenloom_save.json";
        private const string BACKUP_FILENAME   = "lumenloom_save.json.bak";

        // Cached path — computed once, never changes at runtime.
        private static readonly string SavePath   = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
        private static readonly string BackupPath = Path.Combine(Application.persistentDataPath, BACKUP_FILENAME);

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Save player progress to disk.</summary>
        /// <param name="data">Populated SaveData instance.</param>
        public static void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            data.saveVersion    = CURRENT_VERSION;
            data.lastSavedUtc   = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data, prettyPrint: true);

            try
            {
                // Keep a one-step backup before overwriting.
                if (File.Exists(SavePath))
                    File.Copy(SavePath, BackupPath, overwrite: true);

                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Progress saved — unlocked: {data.unlockedChamberIndex}, completed: {data.completedChambers?.Length ?? 0}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to write save file: {ex.Message}");
            }
        }

        /// <summary>
        /// Load saved progress. Returns a default SaveData (no progress) if the
        /// file is absent, corrupted, or version-mismatched.
        /// </summary>
        public static SaveData Load()
        {
            TryLoad(out SaveData data, out _);
            return data;
        }

        /// <summary>
        /// Try to load with explicit error reporting.
        /// </summary>
        /// <param name="data">Loaded data, or a fresh default on failure.</param>
        /// <param name="error">Human-readable error message, or empty on success.</param>
        /// <returns>True if loaded successfully from disk.</returns>
        public static bool TryLoad(out SaveData data, out string error)
        {
            if (!File.Exists(SavePath))
            {
                data  = CreateDefault();
                error = string.Empty;
                return false;
            }

            try
            {
                string raw = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SaveData>(raw);

                if (data == null)
                    throw new InvalidDataException("JsonUtility returned null.");

                // Version migration hook — extend as needed in future versions.
                if (data.saveVersion != CURRENT_VERSION)
                {
                    Debug.LogWarning($"[SaveSystem] Save version {data.saveVersion} differs from current {CURRENT_VERSION}. Resetting to defaults.");
                    data  = CreateDefault();
                    error = $"Save version mismatch (found {data.saveVersion}, expected {CURRENT_VERSION}).";
                    return false;
                }

                error = string.Empty;
                Debug.Log($"[SaveSystem] Progress loaded — unlocked: {data.unlockedChamberIndex}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Save file corrupt — attempting backup restore. Error: {ex.Message}");

                // Attempt backup restore.
                if (TryRestoreBackup(out SaveData backupData))
                {
                    data  = backupData;
                    error = "Primary save corrupt; restored from backup.";
                    return true;
                }

                data  = CreateDefault();
                error = $"Save corrupt and backup unavailable: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Delete all saved progress and reset to a fresh state.
        /// Does NOT affect Unity PlayerPrefs settings.
        /// </summary>
        public static void Reset()
        {
            try
            {
                if (File.Exists(SavePath))   File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
                Debug.Log("[SaveSystem] Progress reset — save files deleted.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save files: {ex.Message}");
            }
        }

        /// <summary>Returns true if a save file exists on disk.</summary>
        public static bool HasSave() => File.Exists(SavePath);

        /// <summary>Returns the full path where the save file is stored (for diagnostics).</summary>
        public static string GetSavePath() => SavePath;

        // ── Internals ──────────────────────────────────────────────────────────

        private static SaveData CreateDefault() => new SaveData
        {
            saveVersion          = CURRENT_VERSION,
            unlockedChamberIndex = 0,
            completedChambers    = new bool[0],
            lastSavedUtc         = string.Empty
        };

        private static bool TryRestoreBackup(out SaveData data)
        {
            if (!File.Exists(BackupPath))
            {
                data = null;
                return false;
            }

            try
            {
                string raw = File.ReadAllText(BackupPath);
                data = JsonUtility.FromJson<SaveData>(raw);
                return data != null && data.saveVersion == CURRENT_VERSION;
            }
            catch
            {
                data = null;
                return false;
            }
        }
    }

    // ── Data Contract ──────────────────────────────────────────────────────────

    /// <summary>
    /// Serialisable save-data structure.
    /// Add new fields at the end and increment CURRENT_VERSION when breaking
    /// backward compatibility.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Format version — bump when adding breaking fields.</summary>
        public int saveVersion;

        /// <summary>Index of the highest unlocked chamber (0-based).</summary>
        public int unlockedChamberIndex;

        /// <summary>
        /// Per-chamber completion flags.
        /// Array is resized as new chambers are added.
        /// </summary>
        public bool[] completedChambers;

        /// <summary>ISO-8601 UTC timestamp of last successful save.</summary>
        public string lastSavedUtc;
    }
}

