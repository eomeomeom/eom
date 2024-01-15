using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = SOA_DEBUG.Debug;

public class ResourceManager : Singleton<ResourceManager>
{
    private static Dictionary<string, string[]> loadedLocations = new Dictionary<string, string[]>();
    private static Dictionary<string, UnityEngine.Object> loadedResources = new Dictionary<string, UnityEngine.Object>();
    private static Dictionary<string, AsyncOperationHandle> addressableHandles = new Dictionary<string, AsyncOperationHandle>();

    public static string DownloadURL { get; set; }

    public bool IsInit { get; private set; }

    static ResourceManager() { }

    public static void LoadAddressable<T>(string key, Action<T> complete = null) where T : UnityEngine.Object
    {
        Addressables.LoadAssetAsync<T>(key).Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                if (!addressableHandles.ContainsKey(key))
                {
                    addressableHandles.Add(key, op);
                }
                complete?.Invoke(op.Result);
            }
        };
    }

    public static T LoadAddressableWaitForCompletion<T>(string key) where T : UnityEngine.Object
    {
        T ret;
        if (addressableHandles.TryGetValue(key, out AsyncOperationHandle handle))
        {
            ret = handle.Result as T;
        }
        else
        {
            var h = Addressables.LoadAssetAsync<T>(key);
            ret = h.WaitForCompletion();
            if (!addressableHandles.ContainsKey(key))
            {
                addressableHandles.Add(key, h);
            }
        }
        return ret;
    }

    public static T GetAddressable<T>(string key) where T : UnityEngine.Object
    {
        T ret = null;
        if (addressableHandles.TryGetValue(key, out AsyncOperationHandle handle))
        {
            ret = handle.Result as T;
        }
        else
        {
            Debug.LogBold($"<color=red>에셋이 로드 되어 있지 않음. Key : {key}</color>");
        }
        return ret;
    }

    public static void InstantiateAddressable(string key, Action<GameObject> complete = null)
    {
        Addressables.InstantiateAsync(key).Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                complete?.Invoke(op.Result);
            }
        };
    }

    public static GameObject InstantiateAddressableWaitForCompletion(string key)
    {
        var h = Addressables.InstantiateAsync(key);
        return h.WaitForCompletion();
    }

    public static void ReleaseAddressable(string key)
    {
        if(addressableHandles.TryGetValue(key, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
        }
        addressableHandles.Remove(key);
    }

    public static void ReleaseInstanceAddressable(GameObject inst)
    {
        Addressables.ReleaseInstance(inst);
    }

    public override void Init()
    {
        base.Init();

        if (IsInit)
        {
            Debug.LogBold("<color=blue>[ Resource Manager ] Already Initialize</color>");
            return;
        }
        IsInit = true;
        UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = ExceptionHandler;
        Debug.LogBold($"<color=blue>[ Resource Manager ] Initialize </color>");
    }

    private void ExceptionHandler(AsyncOperationHandle handle, Exception exception)
    {
        var type = exception.GetType();
        if (type == typeof(InvalidKeyException))
        {
            var ex = exception as InvalidKeyException;
            Debug.LogBold($"<color=red>어드레서블 에셋을 찾을 수 없음\nKey : {ex.Key}</color>");
        }
        else
        {
            Addressables.LogException(handle, exception);
        }
    }
}