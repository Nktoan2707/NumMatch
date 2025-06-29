using UnityEngine;

namespace NumMatch
{
    public class MusicManager : MonoBehaviour
    {
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void ChangeMusicVolume(float volume)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }

        public void PlayMusic(AudioClip audioClip, Vector3 position, float volume = 1f)
        {
            if (audioClip == audioSource.clip)
            {
                return;
            }

            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.loop = true;
            audioSource.Play();

            transform.position = position;
        }
    }
}