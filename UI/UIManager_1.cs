using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class UIManager : Singleton<UIManager>
{
    private Dictionary<UICanvasType, Canvas> uiCanvases;
    private Dictionary<UIEventType, Action<object>> uiEvents;

    private Dictionary<string, Queue<UIBase>> loadedUis;
    private Dictionary<UICanvasType, List<UIBase>> activeUis;

    private UICommon uICommon;
    private UIRoot uiRoot;

    private Dictionary<UICanvasType, GameObject> blockers;

    public bool IsInit { get; private set; }

    private void Awake()
    {
        uiCanvases = new Dictionary<UICanvasType, Canvas>();
        uiEvents = new Dictionary<UIEventType, Action<object>>();
        loadedUis = new Dictionary<string, Queue<UIBase>>();
        activeUis = new Dictionary<UICanvasType, List<UIBase>>();
        blockers = new Dictionary<UICanvasType, GameObject>();
    }

    public override void Init()
    {
        if (IsInit) return;

        base.Init();

        GameObject goUiCommon = ResourceManager.InstantiateObject(UINameDefine.UI_COMMON, transform);
        uICommon = goUiCommon.GetComponent<UICommon>();

        uiCanvases.Clear();
        uiCanvases.Add(UICanvasType.COMMON, uICommon.canvas);
        uiCanvases.Add(UICanvasType.COMMON_LOADING, uICommon.canvasLoading);

        IsInit = true;
    }

    public void SetupUIRoot<T>() where T : UIRoot
    {
        uiRoot = GameObject.FindGameObjectWithTag("UIRoot").GetComponent<T>();
        if (null != uiRoot)
        {
            uiRoot.Setup();
        }
    }

    public void SetupCommonUICamera(Camera sceneMainCamera)
    {
        if (!IsInit) return;

        var cameraData = sceneMainCamera.GetUniversalAdditionalCameraData();
        cameraData.cameraStack.Add(uICommon.canvasCamera);
        cameraData.cameraStack.Add(uICommon.mouseCamera);
    }

    public UIRoot GetUIRoot()
    {
        return uiRoot;
    }

    public T GetUIRoot<T>() where T : UIRoot
    {
        return uiRoot as T;
    }

    public Canvas GetUICanvas(UICanvasType canvasType)
    {
        Canvas ret = null;
        if (uiCanvases.ContainsKey(canvasType))
        {
            ret = uiCanvases[canvasType];
        }
        return ret;
    }

    public void AddUICanvas(UICanvasType canvasType, Canvas canvas)
    {
        if (!uiCanvases.ContainsKey(canvasType))
        {
            uiCanvases.Add(canvasType, null);
        }
        uiCanvases[canvasType] = canvas;
    }

    public void RemoveUICanvas(UICanvasType canvasType)
    {
        if (uiCanvases.ContainsKey(canvasType))
        {
            uiCanvases[canvasType] = null;
            uiCanvases.Remove(canvasType);
        }
    }

    public void SetToTop(UIBase ui)
    {
        ui.transform.SetAsLastSibling();
        List<UIBase> list = activeUis[ui.CanvasType];
        list.Remove(ui);
        list.Add(ui);
    }

    public T[] GetActiveUIArray<T>(UICanvasType canvasType, string key) where T : UIBase
    {
        List<T> ret = new List<T>();
        UIBase[] uis = activeUis[canvasType].ToArray();
        foreach (UIBase ui in uis)
        {
            if (ui.Key == key)
            {
                ret.Add(ui as T);
            }
        }
        return ret.ToArray();
    }

    public UIBase[] GetActiveUIArray(UICanvasType canvasType, string key)
    {
        List<UIBase> ret = new List<UIBase>();
        if (activeUis.ContainsKey(canvasType))
        {
            UIBase[] uis = activeUis[canvasType].ToArray();
            foreach (UIBase ui in uis)
            {
                if (ui.Key == key)
                {
                    ret.Add(ui);
                }
            }
        }
        return ret.ToArray();
    }

    public T GetActiveUI<T>(UICanvasType canvasType, string key) where T : UIBase
    {
        T[] ret = GetActiveUIArray<T>(canvasType, key);
        return ret.Length > 0 ? ret[ret.Length - 1] : null;
    }

    public UIBase GetActiveUI(UICanvasType canvasType, string key)
    {
        UIBase[] ret = GetActiveUIArray(canvasType, key);
        return ret.Length > 0 ? ret[ret.Length - 1] : null;
    }

    public T LoadUI<T>(string key, Vector3 position, Quaternion rotation, Vector3 scale) where T : UIBase
    {
        if (!loadedUis.ContainsKey(key))
        {
            loadedUis.Add(key, new Queue<UIBase>());
        }
        if (loadedUis[key].Count == 0)
        {
            T newUI = ResourceManager.InstantiateObject(key).GetComponent<T>();
            loadedUis[key].Enqueue(newUI);
        }

        UIBase ui = loadedUis[key].Peek();
        bool isContains = uiCanvases.TryGetValue(ui.CanvasType, out Canvas canvas);
        if (isContains)
        {
            ui.transform.SetParent(canvas.transform);
            ui.transform.localPosition = position;
            ui.transform.localRotation = rotation;
            ui.transform.localScale = scale;
            ui.Load(key);
            ui.SetActive(false);
        }

        return isContains ? ui as T : null;
    }

    public T LoadUI<T>(string key) where T : UIBase
    {
        return LoadUI<T>(key, Vector3.zero, Quaternion.identity, Vector3.one);
    }

    public UIBase LoadUI(
        string key,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        return LoadUI<UIBase>(key, position, rotation, scale);
    }

    public UIBase LoadUI(string key)
    {
        return LoadUI<UIBase>(key, Vector3.zero, Quaternion.identity, Vector3.one);
    }

    public void AddActiveUI(UIBase ui)
    {
        if (!activeUis.ContainsKey(ui.CanvasType))
        {
            activeUis.Add(ui.CanvasType, new List<UIBase>());
        }

        if (string.IsNullOrEmpty(ui.Key))
        {
            activeUis[ui.CanvasType].Add(ui);
        }
        else
        {
            activeUis[ui.CanvasType].Add(loadedUis[ui.Key].Dequeue());
        }
    }

    public void RemoveActiveUI(UIBase ui)
    {
        if (!activeUis.ContainsKey(ui.CanvasType)) return;

        activeUis[ui.CanvasType].Remove(ui);
        if (!string.IsNullOrEmpty(ui.Key))
        {
            loadedUis[ui.Key].Enqueue(ui);
        }
    }

    public void HideUIAll(UICanvasType canvasType, string key)
    {
        UIBase[] uIBases = GetActiveUIArray(canvasType, key);
        for (int i = uIBases.Length - 1; i >= 0; i--)
        {
            uIBases[i].Hide();
        }
    }

    public void HideAll(UICanvasType canvasType)
    {
        if (activeUis.ContainsKey(canvasType))
        {
            for (int i = activeUis[canvasType].Count - 1; i >= 0; i--)
            {
                UIBase ui = activeUis[canvasType][i];
                ui.Hide();
            }
        }
    }

    public void HideTopUI(UICanvasType canvasType)
    {
        if (activeUis.ContainsKey(canvasType))
        {
            List<UIBase> list = activeUis[canvasType];
            int lastIndex = activeUis.Count - 1;
            UIBase ui = list[lastIndex];
            ui.Hide();
        }
    }

    public void UnloadAll()
    {
        List<GameObject> unloads = new List<GameObject>();
        foreach (List<UIBase> uis in activeUis.Values)
        {
            foreach (UIBase ui in uis)
            {
                if (!string.IsNullOrEmpty(ui.Key))
                {
                    unloads.Add(ui.gameObject);
                }
            }
        }
        foreach (Queue<UIBase> uis in loadedUis.Values)
        {
            foreach (UIBase ui in uis)
            {
                unloads.Add(ui.gameObject);
            }
        }
        foreach (GameObject go in unloads)
        {
            Destroy(go);
        }
        activeUis.Clear();
        loadedUis.Clear();
    }

    public UILoading ShowLoading(string text)
    {
        UIBase ui = LoadUI(UINameDefine.UI_LOADING);
        if (null != ui)
        {
            ui.Setup(text);
            ui.Show();
        }
        return ui as UILoading;
    }

    public void HideLoading()
    {
        UIBase ui = GetActiveUI(UICanvasType.COMMON_LOADING, UINameDefine.UI_LOADING);
        if (null != ui)
        {
            ui.Hide();
        }
    }

    public UIWaiting ShowWaiting(string text)
    {
        UIBase ui = LoadUI(UINameDefine.UI_WAITING);
        if (null != ui)
        {
            ui.Setup(text);
            ui.Show();
        }
        return ui as UIWaiting;
    }

    public void HideWaiting()
    {
        UIBase ui = GetActiveUI(UICanvasType.COMMON, UINameDefine.UI_WAITING);
        if (null != ui)
        {
            ui.Hide();
        }
    }

    public UIAlert ShowNetworkError(string title, string desc, Action onConfirm, Action onCancle)
    {
        UIBase ui = LoadUI(UINameDefine.UI_ALERT_NETWORK_ERROR);
        if (null != ui)
        {
            ui.Setup(new UIParameterAlertSetup
            {
                title = title,
                desc = desc,
                onConfirm = onConfirm,
                onCancel = onCancle
            });
            ui.Show();
        }
        return ui as UIAlert;
    }

    public void HideNetworkError()
    {
        UIBase ui = GetActiveUI(UICanvasType.COMMON, UINameDefine.UI_ALERT_NETWORK_ERROR);
        if (null != ui)
        {
            ui.Hide();
        }
    }

    public UIAlert ShowAlert(string title, string desc, Action onConfirm, Action onCancle)
    {
        UIBase ui = LoadUI(UINameDefine.UI_ALERT);
        if (null != ui)
        {
            ui.Setup(new UIParameterAlertSetup
            {
                title = title,
                desc = desc,
                onConfirm = onConfirm,
                onCancel = onCancle
            });
            ui.Show();
        }
        return ui as UIAlert;
    }

    public void HideAlert()
    {
        UIBase ui = GetActiveUI(UICanvasType.COMMON, UINameDefine.UI_ALERT);
        if (null != ui)
        {
            ui.Hide();
        }
    }

    public UIConfirm ShowConfirm(string title, string desc, Action onConfirm)
    {
        UIBase ui = LoadUI(UINameDefine.UI_CONFIRM);
        if (null != ui)
        {
            ui.Setup(new UIParameterConfirmSetup
            {
                title = title,
                desc = desc,
                onConfirm = onConfirm
            });
            ui.Show();
        }
        return ui as UIConfirm;
    }

    public void HideConfirm()
    {
        UIBase ui = GetActiveUI(UICanvasType.COMMON, UINameDefine.UI_CONFIRM);
        if (null != ui)
        {
            ui.Hide();
        }
    }

    public UIOnelineMessage ShowOnelineMessage(string message, float duration)
    {
        UIBase ui = LoadUI(UINameDefine.UI_ONELINE_MESSAGE);
        if (null != ui)
        {
            ui.Setup(new UIParameterOnelineMessageSetup
            {
                message = message,
                duration = duration
            });
            ui.Show();
        }
        return ui as UIOnelineMessage;
    }

    public void HideOnelineMessage()
    {
        UIBase ui = GetActiveUI(UICanvasType.COMMON, UINameDefine.UI_ONELINE_MESSAGE);
        if (null != ui)
        {
            ui.Hide();
        }
    }

    public UIBase ShowBase(string name, object param = null)
    {
        UIBase ui = LoadUI(name);
        if (ui != null)
        {
            ui.Setup(param);
            ui.Show();
        }
        return ui;
    }

    public void HideBase(string name)
    {
        UIBase ui = GetActiveUI(UICanvasType.UI, name);
        if (ui != null) ui.Hide();
    }

    public void CreateIgnoreBlockerComponent(UICanvasType rootCanvasType, GameObject rootObj, out Canvas canvas, out List<Component> raycasterList, out CanvasGroup canvasGroup)
    {
        if (rootObj == null)
        {
            canvas = null;
            canvasGroup = null;
            raycasterList = null;
            return;
        }

        canvas = GetOrAddComponent<Canvas>(rootObj);
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;

        raycasterList = new List<Component>();
        var rootCanvas = GetUICanvas(rootCanvasType);
        if (rootCanvas != null)
        {
            Component[] components = rootCanvas.GetComponents<BaseRaycaster>();
            for (int i = 0; i < components.Length; i++)
            {
                Type type = components[i].GetType();
                var comp = rootObj.GetComponent(type);
                if (comp == null)
                {
                    comp = rootObj.AddComponent(type);
                }
                raycasterList.Add(comp);
            }
        }
        else
        {
            raycasterList.Add(GetOrAddComponent<GraphicRaycaster>(rootObj));
        }

        canvasGroup = GetOrAddComponent<CanvasGroup>(rootObj);
    }

    public T GetOrAddComponent<T>(GameObject rootObj) where T : Component
    {
        if (rootObj == null) return null;

        T component = rootObj.GetComponent<T>();
        if (component == null)
        {
            component = rootObj.AddComponent<T>();
        }

        return component;
    }

    public GameObject CreateBlocker(UICanvasType canvasType, Canvas inheritedCanvas, UnityEngine.Events.UnityAction onClickBlock, string name = "")
    {
        if (inheritedCanvas == null) return null;
        Canvas rootCanvas = GetUICanvas(canvasType);
        if (rootCanvas == null) return null;

        GameObject blocker = new GameObject($"Blocker({name})");
        RectTransform rectTransform = blocker.AddComponent<RectTransform>();
        rectTransform.SetParent(rootCanvas.transform, worldPositionStays: false);
        rectTransform.anchorMin = Vector3.zero;
        rectTransform.anchorMax = Vector3.one;
        rectTransform.sizeDelta = Vector2.zero;
        Canvas canvas = blocker.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerID = inheritedCanvas.sortingLayerID;
        canvas.sortingOrder = inheritedCanvas.sortingOrder - 1;

        Component[] components = rootCanvas.GetComponents<UnityEngine.EventSystems.BaseRaycaster>();
        for (int i = 0; i < components.Length; i++)
        {
            Type type = components[i].GetType();
            if (blocker.GetComponent(type) == null)
            {
                blocker.AddComponent(type);
            }
        }

        Image image = blocker.AddComponent<Image>();
        image.color = Color.clear;
        Button button = blocker.AddComponent<Button>();
        button.onClick.AddListener(onClickBlock);

        if (!blockers.ContainsKey(canvasType)) blockers.Add(canvasType, blocker);
        DebugLogger.Log("Created Blocker(UIAbyssRemoteCtrl)");
        return blocker;
    }

    public void DestroyBlocker(UICanvasType canvasType)
    {
        if (blockers.TryGetValue(canvasType, out var go))
        {
            if (go != null) Destroy(go);
            blockers.Remove(canvasType);
        }
    }

    public void Block()
    {
        uICommon.Block();
    }

    public void UnBlock()
    {
        uICommon.Unblock();
    }

    public void CheatButtonOnOff()
    {
        uICommon.CheatButtonOnOff();
    }
    public void ShowCheat()
    {
        uICommon.uiCheat.SetActive(true);
    }
    public void InsertToast(Data.Enum.Toast_Message_Type toastType, string userName, string tileId, string msg = "", UIToast.State state = UIToast.State.BASE)
    {
        User.ReceptionToastMessages(toastType, userName, tileId);

        if (uICommon == null || uICommon.uiToast == null || User._ToastMessages_NotCheck.Count == 0) return;

        var toast = User._ToastMessages_NotCheck[User._ToastMessages_NotCheck.Count - 1];
        if (toast == null) return;

        uICommon.uiToast.Insert(toast._Local, state);
    }

    public void FadeIn(Color fadeColor, float fadeTime, Action onComplete = null)
    {
        uICommon.FadeIn(fadeColor, fadeTime, onComplete);
    }

    public void FadeOut(Color fadeColor, float fadeTime, Action onComplete = null)
    {
        uICommon.FadeOut(fadeColor, fadeTime, onComplete);
    }

    public void AddUIEvent(UIEventType eventType, Action<object> action)
    {
        if (!uiEvents.ContainsKey(eventType))
        {
            uiEvents.Add(eventType, action);
        }
        uiEvents[eventType] -= action;
        uiEvents[eventType] += action;
    }

    public void RemoveUIEvent(UIEventType eventType, Action<object> action)
    {
        if (uiEvents.ContainsKey(eventType))
        {
            uiEvents[eventType] -= action;
        }
    }

    public void RemoveAllUIEvent(UIEventType eventType)
    {
        if (uiEvents.ContainsKey(eventType))
        {
            uiEvents[eventType] = null;
            uiEvents.Remove(eventType);
        }
    }

    public void RemoveAllUIEvents()
    {
        uiEvents.Clear();
    }

    public void ClearUIEvent()
    {
        foreach (UIEventType key in uiEvents.Keys)
        {
            uiEvents[key] = null;
        }
        uiEvents.Clear();
    }

    public void SendUIEvent(UIEventType eventType, object param)
    {
        if (uiEvents.TryGetValue(eventType, out Action<object> action))
        {
            action?.Invoke(param);
        }
    }

    private List<GameObject> particlesOffByFullCoverUI = new List<GameObject>();
    public void OnOpenFullCoverUI()
    {
        var uiAbyssCanvasParticle =
            GetUICanvas(UICanvasType.UI_ABYSS).gameObject.GetComponentsInChildren<ParticleSystem>();
        foreach (var particle in uiAbyssCanvasParticle)
            if (true == particle.gameObject.CompareTag("Effect"))
            {
                this.particlesOffByFullCoverUI.Add(particle.gameObject);
                particle.gameObject.SetActive(false);
            }

        var ladnamrkCanvasParticle =
            GetUICanvas(UICanvasType.LANDMARK).gameObject.GetComponentsInChildren<ParticleSystem>();
        foreach (var particle in ladnamrkCanvasParticle)
            if (true == particle.gameObject.CompareTag("Effect"))
            {
                this.particlesOffByFullCoverUI.Add(particle.gameObject);
                particle.gameObject.SetActive(false);
            }
    }

    public void OnCloseFullCoverUI()
    {
        foreach (var particleGameObject in this.particlesOffByFullCoverUI)
        {
            particleGameObject.SetActive(true);
        }

        this.particlesOffByFullCoverUI.Clear();
    }
}

public static class UIUtil
{
    public static void ResetListener(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
    }

    public static void SetListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();

        if (action == null) return;
        button.onClick.AddListener(action);
    }

    public static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null) return;

        button.onClick.AddListener(action);
    }
}