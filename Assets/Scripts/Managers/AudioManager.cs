using Frameworks;
using UnityEngine;

namespace Managers
{
    public class AudioManager : ManagerBase<AudioManager>
    {
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        protected override void OnInit()
        {
            if (bgmSource == null)
            {
                Debug.LogWarning("AudioManager 缺少 bgmSource");
            }

            if (sfxSource == null)
            {
                Debug.LogWarning("AudioManager 缺少 sfxSource");
            }
        }

        protected override void OnShutdown()
        {
            StopBGM();
        }

        // BGM 播放
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || bgmSource == null)
            {
                Debug.LogWarning("某个发声物品未配置音频片段");
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        // 音效 播放
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }
    }
}
