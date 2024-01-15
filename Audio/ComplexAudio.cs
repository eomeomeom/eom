using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    [System.Serializable]
    public class ComplexAudio
    {
        public AudioClip introClip;
        public AudioClip loopClip;

        AudioSource audioSource;

        public enum State
        {
            Stopped,
            PlayingIntro,
            PlayingLoop,
        }

        State state = State.Stopped;

        public bool IsPlaying()
        {
            return (State.Stopped != this.state);
        }

        public void Play(AudioSource audioSource_)
        {
            Stop();

            if (null == audioSource_)
            {
                Debug.LogWarning("Trying to play ComplexAudio with NULL audio source!!!", AudioManager._instance);
                return;
            }

            if (null == this.introClip && null == this.loopClip)
            {
                return;
            }

            this.audioSource = audioSource_;
            this.audioSource.Stop();

            if (null != this.introClip)
            {
                PlayIntro();
            }
            else
            {
                PlayLoop();
            }
        }

        void PlayIntro()
        {
            this.audioSource.clip = this.introClip;
            this.audioSource.loop = false;

            this.audioSource.Play();
            this.state = State.PlayingIntro;
        }

        void PlayLoop()
        {
            this.audioSource.clip = this.loopClip;
            this.audioSource.loop = true;

            this.audioSource.Play();
            this.state = State.PlayingLoop;
        }

        public void Stop()
        {
            if (State.Stopped == this.state)
            {
                return;
            }

            this.state = State.Stopped;

            this.audioSource.Stop();
            this.audioSource = null;
        }


        public void Update()
        {
            if (State.Stopped == this.state)
            {
                return;
            }

            if (State.PlayingIntro == this.state && false == this.audioSource.isPlaying)
            {
                if (null == this.loopClip)
                {
                    Stop();
                }
                else
                {
                    PlayLoop();
                }
            }
        }
    }
}

