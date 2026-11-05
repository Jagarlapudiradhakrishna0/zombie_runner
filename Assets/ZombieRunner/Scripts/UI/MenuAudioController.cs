using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace ZombieRunner.UI
{
    /// <summary>
    /// Manages the main-menu audio experience:
    ///   - Suspenseful horror music loop (Layer 1 exploration stem)
    ///   - City night wind ambience loop
    ///   - Optional sparse atmospheric stingers (metal creaks, distant rumble)
    /// Works standalone (no AudioManager dependency) so the main menu plays audio
    /// even when the AudioManager singleton has not been created yet.
    /// Respects AudioListener.volume (driven by SaveManager master volume).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuAudioController : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip menuMusicClip;
        [SerializeField] private AudioClip cityAmbienceClip;
        [SerializeField] private AudioClip buttonClickClip;

        [Header("Mixer Routing (optional)")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup ambientMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        [Header("Volume Levels")]
        [SerializeField, Range(0f, 1f)] private float musicVolume      = 0.52f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume   = 0.38f;
        [SerializeField, Range(0f, 1f)] private float uiVolume         = 0.45f;

        [Header("Fade-In")]
        [SerializeField, Min(0f)]       private float musicFadeInSeconds    = 3.5f;
        [SerializeField, Min(0f)]       private float ambienceFadeInSeconds = 2.0f;

        private AudioSource musicSource;
        private AudioSource ambienceSource;
        private AudioSource uiSource;

        private void Awake()
        {
            CreateSources();
        }

        private void Start()
        {
            StartMenuAudio();
        }

        private void CreateSources()
        {
            // Music source
            GameObject musicObj = new GameObject("MenuAudio_Music");
            musicObj.transform.SetParent(transform, false);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.spatialBlend  = 0f;
            musicSource.loop          = true;
            musicSource.playOnAwake   = false;
            musicSource.priority      = 32;
            musicSource.volume        = 0f;
            if (musicMixerGroup != null) musicSource.outputAudioMixerGroup = musicMixerGroup;

            // Ambience source
            GameObject ambObj = new GameObject("MenuAudio_Ambience");
            ambObj.transform.SetParent(transform, false);
            ambienceSource = ambObj.AddComponent<AudioSource>();
            ambienceSource.spatialBlend = 0f;
            ambienceSource.loop         = true;
            ambienceSource.playOnAwake  = false;
            ambienceSource.priority     = 48;
            ambienceSource.volume       = 0f;
            if (ambientMixerGroup != null) ambienceSource.outputAudioMixerGroup = ambientMixerGroup;

            // Tactile UI click source
            GameObject uiObj = new GameObject("MenuAudio_UI");
            uiObj.transform.SetParent(transform, false);
            uiSource = uiObj.AddComponent<AudioSource>();
            uiSource.spatialBlend = 0f;
            uiSource.loop         = false;
            uiSource.playOnAwake  = false;
            uiSource.priority     = 64;
            if (sfxMixerGroup != null) uiSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        private void StartMenuAudio()
        {
            // Apply saved volume levels from SaveManager if available
            float savedMusic  = musicVolume;
            float savedAmbient = ambienceVolume;

            if (ZombieRunner.Core.SaveManager.Instance != null)
            {
                savedMusic   = Mathf.Clamp01(musicVolume    * ZombieRunner.Core.SaveManager.Instance.MusicVolume);
                savedAmbient = Mathf.Clamp01(ambienceVolume * ZombieRunner.Core.SaveManager.Instance.AmbienceVolume);
            }

            // Start music
            if (menuMusicClip != null)
            {
                musicSource.clip = menuMusicClip;
                musicSource.Play();
                StartCoroutine(FadeIn(musicSource, savedMusic, musicFadeInSeconds));
            }

            // Start ambience
            if (cityAmbienceClip != null)
            {
                ambienceSource.clip = cityAmbienceClip;
                ambienceSource.Play();
                StartCoroutine(FadeIn(ambienceSource, savedAmbient, ambienceFadeInSeconds));
            }
        }

        private IEnumerator FadeIn(AudioSource src, float targetVolume, float duration)
        {
            if (src == null) yield break;
            float elapsed = 0f;
            src.volume = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }
            src.volume = targetVolume;
        }

        public void PlayClick()
        {
            if (uiSource != null && buttonClickClip != null)
            {
                float sfxVol = ZombieRunner.Core.SaveManager.Instance != null ? ZombieRunner.Core.SaveManager.Instance.SFXVolume : 0.85f;
                uiSource.pitch = Random.Range(0.96f, 1.04f);
                uiSource.PlayOneShot(buttonClickClip, Mathf.Clamp01(uiVolume * sfxVol));
            }
        }

        public void FadeOut(float duration = 0.4f)
        {
            if (musicSource != null && musicSource.isPlaying)
                StartCoroutine(FadeOutRoutine(musicSource, duration));
            if (ambienceSource != null && ambienceSource.isPlaying)
                StartCoroutine(FadeOutRoutine(ambienceSource, duration));
        }

        private IEnumerator FadeOutRoutine(AudioSource src, float duration)
        {
            if (src == null) yield break;
            float startVol = src.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            src.volume = 0f;
            src.Stop();
        }

        /// <summary>
        /// Update music/ambience volumes when slider changes (called by Settings UI).
        /// </summary>
        public void OnMusicVolumeChanged(float normalizedVolume)
        {
            if (musicSource != null)
                musicSource.volume = musicVolume * Mathf.Clamp01(normalizedVolume);
        }

        public void OnAmbienceVolumeChanged(float normalizedVolume)
        {
            if (ambienceSource != null)
                ambienceSource.volume = ambienceVolume * Mathf.Clamp01(normalizedVolume);
        }
    }
}
