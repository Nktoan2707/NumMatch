using System.Collections.Generic;
using UnityEngine;

namespace NumMatch
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        public const string PLAYERPREFS_IS_SOUND_EFFECT_ENABLED = "PLAYERPREFS_IS_SOUND_EFFECT_ENABLED";
        public const string PLAYERPREFS_SOUND_EFFECT_VOLUME = "PLAYERPREFS_VOLUME_SOUND_EFFECT";
        public const string PLAYERPREFS_IS_MUSIC_ENABLED = "PLAYERPREFS_IS_MUSIC_ENABLED";
        public const string PLAYERPREFS_MUSIC_VOLUME = "PLAYERPREFS_VOLUME_MUSIC";
        public const int IS_SOUND_EFFECT_ENABLED_YES = 1;
        public const int IS_SOUND_EFFECT_ENABLED_NO = 0;
        public const float DEFAULT_SOUND_EFFECT_VOLUME = 1f;
        public const int IS_MUSIC_ENABLED_YES = 1;
        public const int IS_MUSIC_ENABLED_NO = 0;
        public const float DEFAULT_MUSIC_VOLUME = 0.5f;

        [SerializeField] private MusicManager musicManager;

        private bool isSoundEffectEnabled;

        public bool IsSoundEffectEnabled
        {
            get
            {
                return isSoundEffectEnabled;
            }
            set
            {
                isSoundEffectEnabled = value;
            }
        }

        private float soundEffectVolume;

        public float SoundEffectVolume
        {
            get
            {
                if (isSoundEffectEnabled)
                {
                    return soundEffectVolume;
                }
                return 0;
            }
            set
            {
                soundEffectVolume = value;
            }
        }

        private bool isMusicEnabled;

        public bool IsMusicEnabled
        {
            get
            {
                return isMusicEnabled;
            }
            set
            {
                isMusicEnabled = value;
                musicManager.ChangeMusicVolume(MusicVolume);
            }
        }

        private float musicVolume;

        public float MusicVolume
        {
            get
            {
                if (isMusicEnabled)
                {
                    return musicVolume;
                }

                return 0;
            }
            set
            {
                musicVolume = value;
                musicManager.ChangeMusicVolume(MusicVolume);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        private void Start()
        {
            LoadSavedConfiguration();
        }

        public void LoadSavedConfiguration()
        {
            IsSoundEffectEnabled = PlayerPrefs.GetInt(PLAYERPREFS_IS_SOUND_EFFECT_ENABLED, IS_SOUND_EFFECT_ENABLED_YES) == IS_SOUND_EFFECT_ENABLED_YES;
            SoundEffectVolume = GetSavedSoundEffectVolume();
            IsMusicEnabled = PlayerPrefs.GetInt(PLAYERPREFS_IS_MUSIC_ENABLED, IS_MUSIC_ENABLED_YES) == IS_MUSIC_ENABLED_YES;
            MusicVolume = GetSavedMusicVolume();
        }

        public void SaveConfiguration()
        {
            PlayerPrefs.SetInt(PLAYERPREFS_IS_SOUND_EFFECT_ENABLED, IsSoundEffectEnabled ? IS_SOUND_EFFECT_ENABLED_YES : IS_SOUND_EFFECT_ENABLED_NO);
            PlayerPrefs.SetFloat(PLAYERPREFS_SOUND_EFFECT_VOLUME, soundEffectVolume);
            PlayerPrefs.SetInt(PLAYERPREFS_IS_MUSIC_ENABLED, IsMusicEnabled ? IS_MUSIC_ENABLED_YES : IS_MUSIC_ENABLED_NO);
            PlayerPrefs.SetFloat(PLAYERPREFS_MUSIC_VOLUME, musicVolume);
        }

        public float GetSavedMusicVolume()
        {
            return PlayerPrefs.GetFloat(PLAYERPREFS_MUSIC_VOLUME, DEFAULT_MUSIC_VOLUME);
        }

        public float GetSavedSoundEffectVolume()
        {
            return PlayerPrefs.GetFloat(PLAYERPREFS_SOUND_EFFECT_VOLUME, DEFAULT_SOUND_EFFECT_VOLUME);
        }

        public void PlaySoundEffect(AudioClip audioClip, Vector3 position = default(Vector3), float volumeMultiplier = 1f)
        {
            //AudioSource.PlayClipAtPoint(audioClip, position, volumeMultiplier * SoundEffectVolume);
            Play2DSoundEffect(audioClip);
        }

        public void PlaySoundEffect(List<AudioClip> audioClipList, Vector3 position = default, float volumeMultiplier = 1f)
        {
            //AudioSource.PlayClipAtPoint(audioClipList[UnityEngine.Random.Range(0, audioClipList.Count)], position, volumeMultiplier * SoundEffectVolume);
            Play2DSoundEffect(audioClipList[UnityEngine.Random.Range(0, audioClipList.Count)]);
        }

        private void Play2DSoundEffect(AudioClip clip, float volumeMultiplier = 1f)
        {
            GameObject audioObject = new GameObject("2DAudio");
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();

            // Configure the AudioSource to play 2D sound
            audioSource.clip = clip;
            audioSource.volume = volumeMultiplier * SoundEffectVolume;
            audioSource.spatialBlend = 0f; // 0 means 2D, 1 means 3D

            // Play the sound
            audioSource.Play();

            // Destroy the GameObject after the sound finishes playing
            Destroy(audioObject, clip.length);
        }

        public void PlayMusic(AudioClip audioClip, Vector3 position = default, float volumeMultiplier = 1f)
        {
            musicManager.PlayMusic(audioClip, position, volumeMultiplier * MusicVolume);
        }
    }
}