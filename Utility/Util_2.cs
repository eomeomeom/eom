using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Linq;


public static class Utility
{

    public static string ConvertHumanByte(long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suf[place];
    }

    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToUniversalTime();
        return dtDateTime;
    }

    public static bool CheckPossibilities(float probability)
    {
        if (UnityEngine.Random.Range(0.0f, 1f) <= probability) return true;
        return false;
    }


    public static int GetEnumCount(Type enumType)
    {
        if (false == enumType?.IsEnum)
            return 0;

        return Enum.GetValues(enumType).Length;
    }


    public static T SpawnUIElementUnderParent<T>(T prefab, PathologicalGames.SpawnPool spawnPool, Transform parent)
        where T : Component
    {
        if (null == prefab)
            return null;
        
        Transform spawnedTransform = spawnPool.Spawn(prefab.transform);
        if (null == spawnedTransform)
            return null;

        T comp = spawnedTransform.GetComponent<T>();
        if (null == comp)
        {
            spawnPool.Despawn(spawnedTransform);
            return null;
        }

        spawnedTransform.SetParent(parent, false);
        spawnedTransform.localPosition = Vector3.zero;
        spawnedTransform.localScale = Vector3.one;

        return comp;
    }

    public static T SpawnUIElementUnderProvidedCanvas<T>(T prefab, PathologicalGames.SpawnPool spawnPool)
        where T : Component
    {
        if (null == UICanvasProvider._canvas)
            return null;

        return SpawnUIElementUnderParent<T>(prefab, spawnPool, UICanvasProvider._canvas.transform);
    }

    public static void Despawn<T>(ref T instance, PathologicalGames.SpawnPool spawnPool)
        where T : Component
    {
        if (null == instance)
            return;

        instance.transform.SetParent(spawnPool.transform, false);
        spawnPool.Despawn(instance.transform);
        instance = null;
    }

    public static void DespawnUIElementUnder<T>(ref T instance, PathologicalGames.SpawnPool spawnPool)
        where T : Component
    {
        if (null == instance)
            return;

        instance.transform.SetParent(null);
        spawnPool.Despawn(instance.transform);
        instance = null;
    }
    
    public static void DestroyImmediate(Object obj)
    {
        if (obj == null)
            return;
        
        if (true == Application.isEditor)
            Object.DestroyImmediate(obj);
        else
            Object.Destroy(obj);
    }
    
    public static bool DiceDropItem(string dropId, List<DropData.Group> outReceivedReward, float reductionRate)
    {
        if (null == outReceivedReward)
            return false;
        
        DropData dropData = Data.GetDataFromTable(Data._DropDataTable, dropId);
        if (null == dropData)
            return false;

        for (int i = 0; i < dropData._Group.Count; ++i)
        {
            float r = Random.Range(0f, 1f);
            DropData.Group group = dropData._Group[i];
            if (r <= group._Rate)
            {
                if (0.0f == reductionRate)
                    outReceivedReward.Add(group);
                else
                {
                    int reductedValue = Mathf.FloorToInt(group._Value * (1.0f - reductionRate) + 0.5f);
                    var reducted = new DropData.Group(group._RewardType, group._Attribute, group._Rate, reductedValue, group._Icon);
                    outReceivedReward.Add(reducted);
                }
            }
        }

        return true;
    }
    
    public static bool ContainsCaseIgnored(this List<string> list, string a)
    {
        return (list.FindIndex(x => MatchStringCaseIgnored(a, x)) >= 0);
    }

    public static bool ContainsCaseIgnored(string container, string contained)
    {
        if (false == IsStringValid(container))
            return !IsStringValid(contained);

        return (container.IndexOf(contained, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    //public static string GetResourceID(Data.Enum.AssetResource _type)
    //{
    //    string id = "";
    //    switch (_type)
    //    {
    //        case Data.Enum.AssetResource.CORN: id = "I_CORN_01"; break;
    //        case Data.Enum.AssetResource.MEAT: id = "I_MEAT_01"; break;
    //        case Data.Enum.AssetResource.FISH: id = "I_FISH_01"; break;
    //        case Data.Enum.AssetResource.HERB: id = "I_HERB_01"; break;
    //        case Data.Enum.AssetResource.SKIN: id = "I_SKIN_01"; break;
    //        case Data.Enum.AssetResource.MINERAL: id = "I_MINERAL_01"; break;
    //    }
    //    return id;
    //}

}


