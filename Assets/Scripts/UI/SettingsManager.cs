using System;
using UnityEngine;
using UnityEngine.Audio;

namespace LumenLoom.UI
{
    /// <summary>
    /// Manages player-configurable settings for LUMEN LOOM.
    /// Persists lightweight options via UnityEngine.PlayerPrefs.
    /// All volume ranges are [0, 1] (linear); AudioMixer receives dB conversion internally.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        // ── PlayerPrefs Keys ───────────────────────────────────────────────────
        private const string KEY_MASTER_VOL    = "settings_master_vol";
        private const string KEY_SFX_VOL       = "settings_sfx_vol";
        private const string KEY_MUSIC_VOL     = "settings_music_vol";
        private const string KEY_VISUAL_FX     = "settings_visual_fx";
        private const string KEY_BEAM_GLOW     = "settings_beam_glow";
        private const string KEY_TUTORIAL_DONE = "settings_tutorial_done";

        // ── Serialized References ──────────────────────────────────────────────
        [Header("Audio Mixer (optional)")]
        [Tooltip("If assigned, master/SFX/music volumes are applied to the mixer.")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Mixer Parameter Names")]
        [SerializeField] private string masterVolParam = "MasterVolume";
        [SerializeField] private string sfxVolParam    = "SFXVolume";
        [SerializeField] private string musicVolParam  = "MusicVolume";

        // ── Runtime State ──────────────────────────────────────────────────────
        private float _masterVolume  = 1.0f;
        private float _sfxVolume     = 1.0f;
        private float _musicVolume   = 0.7f;
        private bool  _visualFxOn    = true;
        private bool  _beamGlowOn    = true;
        private bool  _tutorialDone  = false;

        // ── Public Properties ──────────────────────────────────────────────────
        public float MasterVolume  => _masterVolume;
        public float SfxVolume     => _sfxVolume;
        public float MusicVolume   => _musicVolume;
        public bool  VisualFxOn    => _visualFxOn;
        public bool  BeamGlowOn    => _beamGlowOn;
        public bool  TutorialDone  => _tutorialDone;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action OnSettingsChanged;

        // ── Singleton ──────────────────────────────────────────────────────────
        public static SettingsManager Instance { get; private set; }

        // ── Unity Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Load / Save ────────────────────────────────────────────────────────

        /// <summary>Load all settings from PlayerPrefs, applying safe defaults.</summary>
        public void LoadSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1.0f);
            _sfxVolume    = PlayerPrefs.GetFloat(KEY_SFX_VOL,    1.0f);
            _musicVolume  = PlayerPrefs.GetFloat(KEY_MUSIC_VOL,  0.7f);
            _visualFxOn   = PlayerPrefs.GetInt(KEY_VISUAL_FX,    1) == 1;
            _beamGlowOn   = PlayerPrefs.GetInt(KEY_BEAM_GLOW,    1) == 1;
            _tutorialDone = PlayerPrefs.GetInt(KEY_TUTORIAL_DONE, 0) == 1;

            ApplyVolumes();
            Debug.Log($"[SettingsManager] Loaded — master:{_masterVolume:F2} sfx:{_sfxVolume:F2} music:{_musicVolume:F2} vfx:{_visualFxOn} glow:{_beamGlowOn}");
        }

        /// <summary>Persist all settings to PlayerPrefs.</summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER_VOL,    _masterVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOL,       _sfxVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL,     _musicVolume);
            PlayerPrefs.SetInt  (KEY_VISUAL_FX,     _visualFxOn  ? 1 : 0);
            PlayerPrefs.SetInt  (KEY_BEAM_GLOW,     _beamGlowOn  ? 1 : 0);
            PlayerPrefs.SetInt  (KEY_TUTORIAL_DONE, _tutorialDone ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── Setters ────────────────────────────────────────────────────────────

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            ApplyVolumes();
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            ApplyVolumes();
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            ApplyVolumes();
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetVisualFxEnabled(bool enabled)
        {
            _visualFxOn = enabled;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetBeamGlowEnabled(bool enabled)
        {
            _beamGlowOn = enabled;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>Mark the tutorial as completed so it won't auto-play again.</summary>
        public void MarkTutorialCompleted()
        {
            _tutorialDone = true;
            SaveSettings();
        }

        /// <summary>Allow the tutorial to replay on next session.</summary>
        public void ResetTutorial()
        {
            _tutorialDone = false;
            SaveSettings();
            Debug.Log("[SettingsManager] Tutorial reset — will replay on next launch.");
        }

        // ── Progress Reset ─────────────────────────────────────────────────────

        /// <summary>
        /// Reset all puzzle progress (SaveSystem data).
        /// Does NOT reset audio/visual settings.
        /// Call only after user confirmation dialog.
        /// </summary>
        public void ResetProgress()
        {
            LumenLoom.Core.SaveSystem.Reset();
            Debug.Log("[SettingsManager] Puzzle progress has been reset.");
        }

        // ── Internals ──────────────────────────────────────────────────────────

        /// <summary>Convert linear [0,1] volume to dB and push to AudioMixer if present.</summary>
        private void ApplyVolumes()
        {
            if (audioMixer == null) return;

            audioMixer.SetFloat(masterVolParam, LinearToDb(_masterVolume));
            audioMixer.SetFloat(sfxVolParam,    LinearToDb(_sfxVolume));
            audioMixer.SetFloat(musicVolParam,  LinearToDb(_musicVolume));
        }

        /// <summary>Convert linear volume [0,1] → decibels [-80, 0].</summary>
        private static float LinearToDb(float linear)
        {
            // Avoid log(0) = -inf; clamp to -80 dB minimum.
            return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        }
    }
}

