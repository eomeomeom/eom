using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    public partial class AudioManager : Singleton<AudioManager>
    {
        const int MaxSoundAudioSourceCount = 4;

        public class SoundAudioSource
        {
            public int channel;
            public AudioSource audioSource;
        }

        List<SoundAudioSource> soundAudioSourceList = new List<SoundAudioSource>();

        public void SetSoundMute(bool isMute)
        {
            foreach(var source in soundAudioSourceList)
            {
                source.audioSource.mute = isMute;
            }
            User.IsSfxPlay = !isMute;
            User.SaveUserData();
        }

        void AddSoundAudioSource()
        {
            GameObject go = new GameObject(string.Format("SoundAudioSource {0}", soundAudioSourceList.Count + 1), typeof(AudioSource));
            go.transform.SetParent(this.transform);

            AudioSource audioSourceComp = go.GetComponent<AudioSource>();
            this.soundAudioSourceList.Add(new SoundAudioSource { channel = -1, audioSource = audioSourceComp });
        }

        void UpdateSound()
        {
            foreach (var s in this.soundAudioSourceList)
            {
                if (false == s.audioSource.isPlaying)
                {
                    s.audioSource.gameObject.SetActive(false);
                }
            }
        }

        public void PlaySound(int channel, string soundNameInSoundTable)
        {
            AudioClip clip = SoundTable.instance.GetSound(soundNameInSoundTable);

            if (string.IsNullOrEmpty(soundNameInSoundTable)) return;

            if(soundList.TryGetValue(soundNameInSoundTable, out AudioClip clip))
            {
                PlaySound(channel, clip);
            }
        }

        // channel 값이 0이상의 정수이면 지정된 channel 에서 플레이하고,
        // channel 값이 음수이면 비어있는 아무 채널에서나 플레이
        public void PlaySound(int channel, AudioClip clip)
        {
            if (null == clip)
            {
                return;
            }

            int targetIndex = -1;
            for (int i = 0; i < this.soundAudioSourceList.Count; ++i)
            {
                var s = this.soundAudioSourceList[i];

                if (false == s.audioSource.isPlaying)
                {
                    if (targetIndex < 0)
                    {
                        targetIndex = i;
                    }
                }
                else if (channel >= 0)
                {
                    if (channel == s.channel)
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }

            //
            if (targetIndex < 0)
            {
                AddSoundAudioSource();

                targetIndex = this.soundAudioSourceList.Count - 1;
            }

            var targetS = this.soundAudioSourceList[targetIndex];

            targetS.audioSource.Stop();
            targetS.channel = channel;
            targetS.audioSource.clip = clip;
            targetS.audioSource.gameObject.SetActive(true);
            targetS.audioSource.Play();
        }

        public void PlaySound(string soundNameInSoundTable)
        {
            if (!User.IsSfxPlay) return;
            PlaySound(-1, soundNameInSoundTable);
        }

        public void PlaySound(AudioClip clip)
        {
            if (null == clip)
            {
                return;
            }

            PlaySound(-1, clip);
        }

        public void StopSound(int channel)
        {
            foreach(var s in this.soundAudioSourceList)
            {
                if (channel == s.channel)
                {
                    if (true == s.audioSource.isPlaying)
                    {
                        s.audioSource.Stop();
                    }

                    s.audioSource.gameObject.SetActive(false);

                    break;
                }
            }
        }
    }
}
