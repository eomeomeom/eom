using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AudioSystem
{
    [CreateAssetMenu(fileName = "VoiceTable", menuName = "VoiceTable")]
    public class VoiceTable : ScriptableObject, ISerializationCallbackReceiver
    {
        #region Static & Property & Method
        private static VoiceTable instanceValue;
        public static VoiceTable instance
        {
            get
            {
                if (instanceValue == null)
                    instanceValue = ResourceManager.GetAsset<VoiceTable>("VoiceTable");
                    Resources.Load<VoiceTable>("VoiceTable");

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
            voiceTableDict.Clear();

            foreach (var s in voiceTable)
            {
                foreach (var a in s.situationTableList)
                {
                    Dictionary<string, List<AudioClip>> sitTbl;
                    string cardId = s.cardId.ToLower();
                    if (false == voiceTableDict.TryGetValue(cardId, out sitTbl))
                    {
                        sitTbl = new Dictionary<string, List<AudioClip>>();
                        voiceTableDict[cardId] = sitTbl;
                    }

                    sitTbl[a.situation.ToLower()] = a.audioClipList;
                }
            }
        }
        #endregion

        #region Member variables for serialization
        [System.Serializable]
        public class SituationTable
        {
            public string situation;
            public List<AudioClip> audioClipList = new List<AudioClip>();
        }

        [System.Serializable]
        public class CardIdAndSituationTableList
        {
            public string cardId;
            public List<SituationTable> situationTableList = new List<SituationTable>();
        }

        public List<CardIdAndSituationTableList> voiceTable = new List<CardIdAndSituationTableList>();
        #endregion

        // runtime fast lookup dictionary
        Dictionary<string, Dictionary<string, List<AudioClip>>> voiceTableDict = new Dictionary<string, Dictionary<string, List<AudioClip>>>();

        public AudioClip GetVoice(string cardId, string situation)
        {
            return GetVoice(cardId, situation, -1);
        }

        public AudioClip GetVoice(string cardId, string situation, int designatedIndex)
        {
            Dictionary<string, List<AudioClip>> s;
            if (false == voiceTableDict.TryGetValue(cardId.ToLower(), out s))
            {
                return null;
            }

            List<AudioClip> a;
            if (false == s.TryGetValue(situation.ToLower(), out a))
            {
                return null;
            }

            if (0 == a.Count)
            {
                return null;
            }

            int actualDesignatedIndex = 0;

            if (-1 == designatedIndex)
            {
                actualDesignatedIndex = Random.Range(0, a.Count);
            }
            else
            {
                actualDesignatedIndex = Mathf.Min(designatedIndex, a.Count - 1);
            }

            return a[actualDesignatedIndex];
        }

#if UNITY_EDITOR
        public void ClearTable()
        {
            this.voiceTable.Clear();
        }
        public void AddItem(string cardId, string situation, AudioClip audioClip)
        {
            CardIdAndSituationTableList c = this.voiceTable.Find(x => (0 == string.Compare(x.cardId, cardId, true)));
            if (null == c)
            {
                c = new CardIdAndSituationTableList();
                c.cardId = cardId;
                this.voiceTable.Add(c);
            }

            SituationTable s = c.situationTableList.Find(x => (0 == string.Compare(x.situation, situation, true)));
            if (null == s)
            {
                s = new SituationTable();
                s.situation = situation;
                c.situationTableList.Add(s);
            }

            s.audioClipList.Add(audioClip);
        }
#endif
    }
}