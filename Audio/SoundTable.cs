using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    [CreateAssetMenu(fileName = "SoundTable", menuName = "SoundTable")]
    public class SoundTable : ScriptableObject, ISerializationCallbackReceiver
    {
        #region Static & Property & Method
        private static SoundTable instanceValue;
        public static SoundTable instance
        {
            get
            {
                if (instanceValue == null)
                {
                    ResourceManager.LoadAddressable<SoundTable>("Assets/Game/Resources/ScriptableObjects/SoundTable.asset", (res) =>{instanceValue = res;});
                }
                return instanceValue;
            }
        }
        #endregion

        #region Serialization
        public void OnBeforeSerialize()
        {
            // do nothing
        }

        public void OnAfterDeserialize()
        {
            UpdateDictionary();
        }

        public void UpdateDictionary()
        {
            this.soundListDict.Clear();

            foreach (var s in soundList)
            {
                this.soundListDict[s.name.ToLower()] = s.audioClip;
            }
        }
        #endregion

        #region Member variables for serialization
        [System.Serializable]
        public class NameAndAudioClip
        {
            public string name;
            public AudioClip audioClip;
        }
        public List<NameAndAudioClip> soundList = new List<NameAndAudioClip>();
        #endregion

        // runtime fast lookup dictionary
        Dictionary<string, AudioClip> soundListDict = new Dictionary<string, AudioClip>();

        public AudioClip GetSound(string soundName)
        {
            if (true == string.IsNullOrEmpty(soundName))
            {
                return null;
            }

            AudioClip retClip;
            if (false == this.soundListDict.TryGetValue(soundName.ToLower(), out retClip))
            {
                return null;
            }

            return retClip;
        }

#if UNITY_EDITOR
        public void ClearTable()
        {
            this.soundList.Clear();
        }
#endif
    }
}