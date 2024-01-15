using System;
using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    [Tooltip("UI 하이어라키 순서")]
    public int order = 0;
    [Tooltip("안드로이드 뒤로가기 버튼 반응 여부")]
    public bool isBackBtnInteraction = true;
    [Tooltip("기존에 활성화 되어있는 동일 한 UI 오브젝트를 재 사용 할지 여부")]
    public bool isRecycle = true;
    [Tooltip("이전 UI를 비활성화 시킬지 여부\n (True : 활성화, False : 비활성화)")]
    public bool isOverdraw = false;
    [Tooltip("UI 매니저의 Hide 호출을 무시 할지 여부\n(UI 객체가 일괄 Hide 처리에 의해 Hide 되는 것을 방지)")]
    public bool ignoreHidingByManager = false;

    public Action<UIBase> OnHideUI { get; set; }

    public string Address { get; set; }
    
    public int RuntimeOrder { get; set; }

    public virtual void Show(object param = null) 
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide() 
    {
        OnHideUI?.Invoke(this);
        gameObject.SetActive(false);
    }
}