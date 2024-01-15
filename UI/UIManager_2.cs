using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = SOA_DEBUG.Debug;

public class UIManager : Singleton<UIManager>
{
    private readonly int POOL_SIZE = 2;

    private Dictionary<string, List<UIBase>> pools;
    private List<UIBase> activeUIs;

    public static bool IsInit { get; private set; }
    public UI_Common CommonUI { get; private set; }
    
    public UIBase TopUI
    {
        get
        {
            return (activeUIs.Count > 0) ? activeUIs[^1] : null;
        }
    }

    private void Awake()
    {
        IsInit = false;

        pools = new Dictionary<string, List<UIBase>>();
        activeUIs = new List<UIBase>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIBase ui = TopUI;
            if (ui != null && ui.isBackBtnInteraction)
            {
                HideTop();
            }
        }
    }

    public override void Init()
    {
        base.Init();

        if (IsInit)
        {
            Debug.LogBold("<color=magenta>[ UI MANAGER ] Already Initialize</color>");
            return;
        }

        IsInit = true;

        var commonAsset = ResourceManager.LoadAddressableWaitForCompletion<GameObject>(DefineName.UI.UI_COMMON);
        var commonGo = GameObject.Instantiate(commonAsset);
        if (commonGo != null)
        {
            commonGo.transform.SetParent(transform);
            commonGo.transform.localPosition = Vector3.zero;
            commonGo.transform.localRotation = Quaternion.identity;
            commonGo.transform.localScale = Vector3.one;
            CommonUI = commonGo.GetComponent<UI_Common>();
        }

        Debug.LogBold("<color=magenta>[ UI MANAGER ] Initialize</color>");
    }

    public void AttachCommonUICamera(Camera camera)
    {
        var cameraData = camera.GetUniversalAdditionalCameraData();
        if (!cameraData.cameraStack.Contains(CommonUI.canvasCamera))
        {
            cameraData.cameraStack.Add(CommonUI.canvasCamera);
        }
        if (!cameraData.cameraStack.Contains(CommonUI.mouseCamera))
        {
            cameraData.cameraStack.Add(CommonUI.mouseCamera);
        }
    }

    public void DetachCommonUICamera(Camera camera)
    {
        var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.cameraStack.Remove(CommonUI.canvasCamera);
        cameraData.cameraStack.Remove(CommonUI.mouseCamera);
    }

    private T CreatePoolItem<T>(string path) where T : UIBase
    {
        T ret = null;
        var asset = ResourceManager.GetAddressable<GameObject>(path);
        if(asset != null)
        {
            var go = GameObject.Instantiate(asset);
            if (go != null)
            {
                ret = go.GetComponent<T>();

                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                go.SetActive(false);

                pools[path].Add(ret);
            }
        }
        return ret;
    }

    private void RemoveStack(UIBase ui)
    {
        activeUIs.Remove(ui);
        ui.transform.SetParent(transform);
    }

    private void UpdateOrder()
    {
        var sorted = activeUIs.OrderBy(x => x.RuntimeOrder).ToArray();
        for (int i = 0; i < sorted.Length; i++)
        {
            sorted[i].transform.SetSiblingIndex(i);
        }
    }

    private void OnHideUI(UIBase ui)
    {
        ui.OnHideUI = null;
        RemoveStack(ui);
        if(activeUIs.Count > 0)
        {
            TopUI.gameObject.SetActive(true);
        }
    }

    public UIBase ShowUIBase(string address, object param = null)
    {
        return ShowUIBase<UIBase>(address, param);
    }

    public T ShowUIBase<T>(string address, object param = null) where T : UIBase
    {
        T ret = null;
        bool added = false;

        if (!pools.ContainsKey(address))
        {
            pools.Add(address, new List<UIBase>());
            ret = CreatePoolItem<T>(address);
            added = true;
        }
        else
        {
            var p = pools[address];
            ret = p[^1] as T;
            added = !activeUIs.Contains(ret);
        }

        if(ret != null)
        {
            var pool = pools[address];
            if (!ret.isRecycle)
            {
                ret = null;
                foreach (var item in pool)
                {
                    if (item.transform.parent == transform && !item.gameObject.activeSelf)
                    {
                        ret = item as T;
                        break;
                    }
                }
                if (ret == null)
                {
                    for (int i = 0; i < POOL_SIZE; i++)
                    {
                        CreatePoolItem<T>(address);
                    }
                    int lastIdx = pool.Count - 1;
                    ret = pool[lastIdx] as T;
                }
                added = true;
            }

            ret.RuntimeOrder = ret.order + activeUIs.Count;
            ret.Address = address;
            ret.OnHideUI = OnHideUI;
            ret.transform.SetParent(CommonUI.tfUIRoot);
            ret.transform.localPosition = Vector3.zero;
            ret.transform.localRotation = Quaternion.identity;
            ret.transform.localScale = Vector3.one;
            RectTransform rt = ret.transform as RectTransform;
            rt.anchoredPosition = Vector2.zero;
            if (added)
            {
                if (!ret.isOverdraw && activeUIs.Count > 0)
                {
                    TopUI.gameObject.SetActive(false);
                }
                activeUIs.Add(ret);
            }
            UpdateOrder();
            ret.Show(param);
        }

        return ret;
    }

    public void HideTop()
    {
        if (activeUIs.Count > 0)
        {
            var ui = activeUIs[^1];
            if (!ui.ignoreHidingByManager)
            {
                activeUIs[^1].Hide();
            }
        }
    }

    public void HideAll()
    {
        List<UIBase> removed = new();
        int cnt = activeUIs.Count;
        for (int i = 0; i < cnt; i++)
        {
            var ui = activeUIs[i];
            if (ui.ignoreHidingByManager) continue;
            ui.OnHideUI = null;
            ui.Hide();
            removed.Add(ui);
        }
        foreach(var item in removed)
        {
            RemoveStack(item);
        }
        if(activeUIs.Count > 0)
        {
            TopUI.gameObject.SetActive(true);
        }
    }

    public void UnloadAll()
    {
        List<string> removedAddress = new List<string>();
        foreach (var list in pools.Values)
        {
            foreach (var ui in list)
            {
                ui.OnHideUI = null;
                ui.Hide();
                RemoveStack(ui);
                Destroy(ui.gameObject);
                removedAddress.Add(ui.Address);
            }
        }
        foreach (string address in removedAddress)
        {
            if (!string.IsNullOrEmpty(address))
            {
                if (pools.TryGetValue(address, out List<UIBase> list))
                {
                    list.Clear();
                    ResourceManager.ReleaseAddressable(address);
                }
            }
        }
        pools.Clear();
        activeUIs.Clear();
    }
}
