using UnityEngine;

namespace NumMatch
{
    public class SceneMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip audioClip;

        private void Start()
        {
            if (audioClip == null)
            {
                return;
            }

            SoundManager.Instance.PlayMusic(audioClip);
        }
    }
}