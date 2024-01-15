using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    public partial class AudioManager : Singleton<AudioManager>
    {
        public AudioSource bgmAudioSource;

        [System.Serializable]
        public class NameAndComplexAudio
        {
            public string name;
            public ComplexAudio complexAudio;
        }

        List<NameAndComplexAudio> bgmList = new List<NameAndComplexAudio>();
        Dictionary<string, AudioClip> soundList = new Dictionary<string, AudioClip>();

        ComplexAudio currentBgm = null;

        private AudioListener audioListener;
        void Awake()
        {
            audioListener = this.gameObject.AddComponent<AudioListener>();
            bgmAudioSource = this.gameObject.AddComponent<AudioSource>();

            bgmList.Clear();
            soundList.Clear();

            //aduio source load
            ////////////////////

            
            for (int i = 0; i < MaxSoundAudioSourceCount; ++i)
            {
                AddSoundAudioSource();
            }

        }

        private void Update()
        {
            if (null != this.currentBgm)
            {
                this.currentBgm.Update();
            }

            UpdateSound();
        }

        public void SetBgmMute(bool isMute)
        {
            bgmAudioSource.mute = isMute;
            User.IsBgmPlay = !isMute;
            User.SaveUserData();
        }

        public bool PlayBGM(string bgmName)
        {
            NameAndComplexAudio foundCA = null;
            foundCA = this.bgmList.Find(x => 0 == string.Compare(bgmName, x.name, true));

            if (null == foundCA)
            {
                return false;
            }

            // 플레이 요청이 들어온 BGM 과 현재 플레이중인 BGM 이 같으면 새로 시작하지 않는다.
            if (foundCA.complexAudio == this.currentBgm &&
                true == this.currentBgm.IsPlaying())
            {
                return true;
            }

            if (null != currentBgm)
            {
                currentBgm.Stop();
            }

            foundCA.complexAudio.Play(this.bgmAudioSource);
            this.currentBgm = foundCA.complexAudio;

            return true;
        }

        public void StopBGM()
        {
            if (null == this.currentBgm)
                return;
            
            this.currentBgm.Stop();
            this.currentBgm = null;
        }
        public void AudioOnOff()
        {
            audioListener.enabled = !audioListener.enabled; 
        }
    }
}
