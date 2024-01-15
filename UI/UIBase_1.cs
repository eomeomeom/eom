using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using echo17.Signaler.Core;

public abstract class UIBase : MonoBehaviour, ISubscriber, IBroadcaster
{
    public SignalGroupProperty signalProperty;
    public long signalOrder;

    [SerializeField] private UICanvasType canvasType;
    private bool isReleased = false;

    public UICanvasType CanvasType { get { return canvasType; } }
    public string Key { get; private set; }

    [SerializeField] private bool isFullCoverUI = false;        // 화면 전체를 덮는 UI 인지 여부

    private void Awake()
    {
        isReleased = false;
        OnAwake();
    }

    private void Start()
    {
        OnStart();
    }

    private void OnEnable()
    {
        // 화면 전체를 덮는 UI 가 보여질 때에는 Abyss 와 Landmark 캔버스를 꺼준다
        if (true == this.isFullCoverUI)
            UIManager._instance.OnOpenFullCoverUI();
        
        OnActivate();
    }

    private void OnDisable()
    {
        // 화면 전체를 덮는 UI 를 닫을 때에는 꺼줬던 Abyss 와 Landmark 캔버스를 다시 켜줌
        if (true == this.isFullCoverUI && !UIManager.IsDetroying)
            UIManager._instance.OnCloseFullCoverUI();
        
        OnInActivate();
    }

    private void OnDestroy()
    {
        if (!isReleased)
        {
            isReleased = true;
            OnRelease();
        }
    }

    protected virtual void OnLoaded() { }
    protected virtual void OnAwake() { }
    protected virtual void OnStart() { }
    protected virtual void OnActivate() { }
    protected virtual void OnInActivate() { }
    protected virtual void OnRelease() { }
    public void Load(string key) 
    { 
        Key = key;
        OnLoaded();
    }

    /// <summary>
    /// UI 활성화 상태 여부
    /// </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    /// <summary>
    /// UI 활성화 / 비활성화 설정
    /// </summary>
    /// <param name="active"></param>
    public void SetActive(bool active)
    {
        if(gameObject.activeSelf != active)
        {
            gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// UI 초기화 할 때 실행
    /// </summary>
    /// <param name="param"></param>
    public virtual void Setup(object param) { }

    /// <summary>
    /// UI 활성화 할 때 실행
    /// </summary>
    /// <param name="param"></param>
    public virtual void Show()
    {
        UIManager._instance.AddActiveUI(this);
        SetActive(true);
    }

    /// <summary>
    /// UI 비활성화 할 때 실행
    /// </summary>
    public virtual void Hide()
    {
        UIManager._instance.RemoveActiveUI(this);
        SetActive(false);
    }

    protected MessageSubscription<T> Subscribe<T>(MessageAction<T> action)
    {
        long? _group = null;
        if (signalProperty.groupValue != 0) _group = signalProperty.groupValue;

        return Signaler.Instance.Subscribe(this, action, _group, signalOrder);
    }

    protected RequestSubscription<T,R> Subscribe<T,R>(RequestAction<T,R> action)
    {
        long? _group = null;
        if (signalProperty.groupValue != 0) _group = signalProperty.groupValue;

        return Signaler.Instance.Subscribe(this, action, _group, signalOrder);
    }

    protected void UnSubscribe<T>(MessageSubscription<T> subscription)
    {
        if (subscription == null) return;
        subscription.UnSubscribe();
    }

    protected void UnSubscribe<T,R>(RequestSubscription<T,R> subscription)
    {
        if (subscription == null) return;
        subscription.UnSubscribe();
    }

    protected int Broadcast<T>(T signal, long targetGroup) where T : ISignal
    {
        long? _group = null;
        if (targetGroup != 0) _group = targetGroup;

        signal.SenderGroup = signalProperty.groupValue;
        signal.SenderTag = "";
        signal.TargetTag = string.IsNullOrEmpty(signal.TargetTag) ? "" : signal.TargetTag;

        return Signaler.Instance.Broadcast(this, signal, _group);
    }

    protected int[] Broadcast<T>(SignalData<T> signalData) where T : struct, ISignal
    {
        int[] ret = new int[signalData.signalTargets.Length];
        int index = 0;
        foreach (var target in signalData.signalTargets)
        {
            long? _group = null;
            if (target.groupValue != 0) _group = target.groupValue;

            signalData.signal.SenderGroup = signalProperty.groupValue;
            signalData.signal.SenderTag = "";
            signalData.signal.TargetTag = string.IsNullOrEmpty(signalData.signal.TargetTag) ? "" : signalData.signal.TargetTag;

            int result = Signaler.Instance.Broadcast(this, signalData.signal, _group);
            ret[index] = result;
        }
        return ret;
    }
}
