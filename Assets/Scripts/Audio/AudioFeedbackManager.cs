using UnityEngine;

namespace LumenLoom.Audio
{
    /// <summary>
    /// Provides audio feedback for puzzle actions.
    /// Features dynamic procedural audio synthesis fallback, ensuring full sound
    /// feedback without requiring bulky external audio asset files.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioFeedbackManager : MonoBehaviour
    {
        [Header("Audio Clips (Optional Overrides)")]
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioClip releaseClip;
        [SerializeField] private AudioClip targetConnectedClip;
        [SerializeField] private AudioClip resetClip;
        [SerializeField] private AudioClip rotateTickClip;
        [SerializeField] private AudioClip obstacleBlockedClip;

        [Header("Audio Settings")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 0.7f;
        [SerializeField] private bool useProceduralFallback = true;

        private AudioSource _audioSource;
        private AudioClip _procSelectClip;
        private AudioClip _procReleaseClip;
        private AudioClip _procConnectedClip;
        private AudioClip _procResetClip;
        private AudioClip _procRotateTickClip;
        private AudioClip _procObstacleBlockedClip;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.volume = masterVolume;

            if (useProceduralFallback)
            {
                GenerateProceduralClips();
            }
        }

        public void PlayNodeSelectSound()
        {
            AudioClip clip = selectClip != null ? selectClip : _procSelectClip;
            PlayClipOneShot(clip, 0.6f);
        }

        public void PlayNodeReleaseSound()
        {
            AudioClip clip = releaseClip != null ? releaseClip : _procReleaseClip;
            PlayClipOneShot(clip, 0.4f);
        }

        public void PlayTargetConnectedSound()
        {
            AudioClip clip = targetConnectedClip != null ? targetConnectedClip : _procConnectedClip;
            PlayClipOneShot(clip, 0.9f);
        }

        public void PlayResetSound()
        {
            AudioClip clip = resetClip != null ? resetClip : _procResetClip;
            PlayClipOneShot(clip, 0.5f);
        }

        public void PlayRotationTickSound()
        {
            AudioClip clip = rotateTickClip != null ? rotateTickClip : _procRotateTickClip;
            PlayClipOneShot(clip, 0.25f);
        }

        public void PlayObstacleBlockedSound()
        {
            AudioClip clip = obstacleBlockedClip != null ? obstacleBlockedClip : _procObstacleBlockedClip;
            PlayClipOneShot(clip, 0.45f);
        }

        private void PlayClipOneShot(AudioClip clip, float volumeScale)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, volumeScale * masterVolume);
            }
        }

        /// <summary>
        /// Generates lightweight, sci-fi synthesized waveforms procedurally in memory.
        /// </summary>
        private void GenerateProceduralClips()
        {
            _procSelectClip = SynthesizeTone(440f, 660f, 0.08f, WaveType.Sine);
            _procReleaseClip = SynthesizeTone(520f, 330f, 0.07f, WaveType.Sine);
            _procConnectedClip = SynthesizeChord(new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.6f); // C Major celestial chime
            _procResetClip = SynthesizeTone(300f, 150f, 0.2f, WaveType.Triangle);
            _procRotateTickClip = SynthesizeTone(880f, 1100f, 0.035f, WaveType.Sine);
            _procObstacleBlockedClip = SynthesizeTone(180f, 90f, 0.15f, WaveType.Triangle);
        }

        private enum WaveType { Sine, Triangle }

        private AudioClip SynthesizeTone(float startFreq, float endFreq, float duration, WaveType type)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                float phase = 2f * Mathf.PI * currentFreq * ((float)i / sampleRate);

                float sample = (type == WaveType.Sine) ? Mathf.Sin(phase) : (2f * Mathf.Abs(2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f))) - 1f);

                // Smooth linear decay envelope
                float envelope = 1f - t;
                data[i] = sample * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("ProcTone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizeChord(float[] frequencies, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float combined = 0f;

                foreach (float freq in frequencies)
                {
                    float phase = 2f * Mathf.PI * freq * ((float)i / sampleRate);
                    combined += Mathf.Sin(phase);
                }

                combined /= frequencies.Length;

                // Bell envelope (quick attack, long exponential release)
                float envelope = Mathf.Exp(-3.5f * t);
                data[i] = combined * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create("ProcChord", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

