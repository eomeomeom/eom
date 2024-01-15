using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;



public class MultiIndexPointCardScrollView : MultiIndexPointScrollViewBase, IPointerEnterHandler, IPointerExitHandler
{
	protected class CardInstanceAndGameObject
	{
		public NTLua.LuaMonoBehaviour cardInstance;
		public GameObject gameObject;
		float offset;
		float elapsedTimeSinceOffsetReductionBegan;

		public void UpdateCurrentOffset(float offsetReductionTime, float deltaTime)
		{
			if (0.0f == this.offset)
				return;

			this.elapsedTimeSinceOffsetReductionBegan += deltaTime;
			if (this.elapsedTimeSinceOffsetReductionBegan >= offsetReductionTime)
			{
				this.offset = 0.0f;
				this.elapsedTimeSinceOffsetReductionBegan = 0.0f;
			}
		}

		public float GetCurrentOffset(float offsetReductionTime, AnimationCurve curve)
		{
			if (offsetReductionTime <= 0.0f)
				return 0.0f;

			float actualT = curve.Evaluate(Mathf.Min(1.0f, this.elapsedTimeSinceOffsetReductionBegan / offsetReductionTime));

			return this.offset * (1.0f - actualT);
		}

		public void AddOffset(float additionalOffset, float offsetReductionTime, AnimationCurve curve)
		{
			if (0.0f == additionalOffset)
				return;

			this.offset = GetCurrentOffset(offsetReductionTime, curve) + additionalOffset;
			this.elapsedTimeSinceOffsetReductionBegan = 0.0f;
		}
	}

	public float offsetReductionTime = 0.2f;
	public AnimationCurve offsetReductionAnimationCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

	protected List<CardInstanceAndGameObject> cardList = new List<CardInstanceAndGameObject>();

	public override int count
	{
		get
		{
			return cardList.Count;
		}
	}

	public NTLua.LuaMonoBehaviour draggingCard;

	protected bool buttonDown = false;

	public delegate void PointerEventDelegate(PointerEventData eventData);
	public PointerEventDelegate pointerEnterCallback = null;
	public PointerEventDelegate pointerExitCallback = null;

	// 반환값은 add(insert) 된 index
	public int Add(int cardIdInPlay, string cardIdInTable, bool useOffsetReduction = true)
	{
		int insertIndex = cardList.Count;

		return Insert(cardIdInPlay, cardIdInTable, insertIndex, useOffsetReduction);
	}

	public int Insert(int cardIdInPlay, string cardIdInTable, int insertIndex, bool useOffsetReduction = true)
	{
		BattleSceneCardItem cardInst = BattleSceneCardItem.Spawn(cardIdInPlay, cardIdInTable);
		if (null == cardInst)
			return -1;

		CardInstanceAndGameObject cigo = new CardInstanceAndGameObject();
		cigo.gameObject = GameManager.instance.battleScene.GetGameObjectFromPool();
		cigo.cardInstance = cardInst;

		if (insertIndex < 0 || insertIndex > cardList.Count)
			insertIndex = cardList.Count;

		// 추가할 카드를 넣기 전에, 해당 인덱스에 이미 카드가 존재하면 스르륵 밀려나는것처럼 보이게 한다.
		if (true == useOffsetReduction)
		{
			if (insertIndex < cardList.Count)
				cardList[insertIndex].AddOffset(-1.0f, offsetReductionTime, offsetReductionAnimationCurve);

			SetState(State.SNAP_SLIDING, true);
		}

		cardList.Insert(insertIndex, cigo);
		cigo.gameObject.transform.SetParent(this.gameObject.transform, false);
		cardInst.transform.SetParent(cigo.gameObject.transform, false);

		ForcePositionUpdate();

		return insertIndex;
	}

	public bool Remove(int index, bool useOffsetReduction = true)
	{
		if (index > cardList.Count)
			return false;

		CardInstanceAndGameObject cigo = cardList[index];
		cardList.RemoveAt(index);

		cigo.cardInstance.transform.SetParent(null, false);
		GameManager.instance.battleScene.ReturnGameObjectToPool(cigo.gameObject);
		cigo.gameObject = null;

		cigo.cardInstance.Despawn();
		cigo.cardInstance = null;

		if (true == useOffsetReduction)
		{
			if (index < cardList.Count)
			{
				cardList[index].AddOffset(1.0f, offsetReductionTime, offsetReductionAnimationCurve);
			}

			SetState(State.SNAP_SLIDING, true);
		}

		ForcePositionUpdate();

		return true;
	}

	public virtual void Clear()
	{
		foreach (CardInstanceAndGameObject cigo in cardList)
		{
			cigo.cardInstance.transform.SetParent(null, false);
			BattleSceneWrapper.instance.ReturnGameObjectToPool(cigo.gameObject);
			cigo.gameObject = null;

			BattleSceneCardItemWrapper.Despawn(cigo.cardInstance);
			cigo.cardInstance = null;
		}
		cardList.Clear();

		ForcePositionUpdate();
	}

	public NTLua.LuaMonoBehaviour GetCardInstance(int index)
	{
		return cardList[index].cardInstance;
	}

	public NTLua.LuaMonoBehaviour GetSelectedCardInstance()
	{
		if (selectedIndex < 0 || selectedIndex >= cardList.Count)
		{
			return null;
		}

		return GetCardInstance(selectedIndex);
	}

	public int FindCardInstanceIndex(int cardInstanceId)
	{
		return this.cardList.FindIndex(x => BattleSceneCardItemWrapper.GetInstanceId(x.cardInstance) == cardInstanceId);
	}

	public int FindCardInstanceIndex(NTLua.LuaMonoBehaviour card)
	{
		return this.cardList.FindIndex(x => x.cardInstance == card);
	}

	static bool IsTargetOrIsParentOfTarget(GameObject obj, GameObject target)
	{
		if (null == target)
			return false;

		if (obj == target)
			return true;

		if (null == target.transform.parent)
			return false;

		return IsTargetOrIsParentOfTarget(obj, target.transform.parent.gameObject);
	}

	public override void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
	{
		base.OnPointerDown(eventData);

		buttonDown = true;
	}

	// 카드 팝업 조건 체크. true 면 card popup 발동, false 면 안함.
	protected virtual bool CheckForCardPopup()
	{
		return
			(State.IDLE == stateCapsule.state || State.SNAP_SLIDING == stateCapsule.state) &&
			count > 0;
	}

	public override void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
	{
		base.OnPointerUp(eventData);

		// 카드팝업이 보이고 있는지 체크해서 보여주는 부분
		if (CheckForCardPopup())// &&
								//false == GameManager.instance.cardPopup.gameObject.activeSelf)
		{
			//GameManager.instance.cardPopup.Show(cardList[selectedIndex].cardInstance.cardInfo);
			Debug.Log("BattleScene card popup!");
		}

		buttonDown = false;
	}

	public override void OnDrop(PointerEventData data)
	{
		MultiIndexPointCardScrollView sender = data.pointerDrag.GetComponent<MultiIndexPointCardScrollView>();
		if (null == sender)
			return;

		if (sender.stateCapsule.state != State.DRAGGING)
			return;

		dragDropEvent.Invoke(sender, this);
	}

	protected override void OnEnterDraggingInitializedState(State prevState)
	{
		// dragging 준비 상태로 진입할 때 필요한 처리들
		if (null == this.draggingCard)
		{
			var cardInstance = this.cardList[selectedIndex].cardInstance;
			this.draggingCard = BattleSceneCardItemWrapper.SpawnCloned(cardInstance);
		}

		BattleSceneCardItemWrapper.BlockRaycast(this.draggingCard, false);
		BattleSceneCardItemWrapper.SetVisible(this.draggingCard, true);
		this.draggingCard.transform.SetParent(draggingIconParent, false);
		this.draggingCard.transform.SetAsLastSibling();

		this.draggingCard.transform.position = draggingPoint;

		BattleSceneCardItemWrapper.EnableTilt(this.draggingCard, true);
	}

	void ReturnDraggingCardToPool()
	{
		if (null == this.draggingCard)
			return;

		BattleSceneCardItemWrapper.Despawn(this.draggingCard);
		this.draggingCard = null;
	}

	protected override void OnExitDraggingInitializedState(State nextState)
	{
		if (State.DRAGGING != nextState)
			ReturnDraggingCardToPool();
	}

	protected override void OnExitDraggingState(State nextState)
	{
		ReturnDraggingCardToPool();
	}

	protected override bool CheckDraggableWithPointerEventData(PointerEventData eventData)
	{
		if (0 == count)
			return false;

		// 드래깅은 중앙의 선택된 카드를 클릭하는것으로만 시작할 수 있다.
		if (false == IsTargetOrIsParentOfTarget(this.cardList[selectedIndex].cardInstance.gameObject, eventData.pointerPressRaycast.gameObject))
			return false;

		return true;
	}

	protected override void UpdateStateDragging()
	{
		base.UpdateStateDragging();

		this.draggingCard.transform.position = this.draggingPoint;
	}

	protected override void UpdateItemVisibilityPositionRotation(int index, bool visibility, Vector3 position, Quaternion rotation)
	{
		var cigo = cardList[index];

		BattleSceneCardItemWrapper.SetVisible(cigo.cardInstance, visibility);
		cigo.gameObject.transform.position = position;
		cigo.gameObject.transform.rotation = rotation;
	}

	protected override void UpdateItemCurrentOffset(int index)
	{
		this.cardList[index].UpdateCurrentOffset(offsetReductionTime, Time.unscaledDeltaTime);
	}

	protected override float GetItemCurrentOffset(int index)
	{
		return this.cardList[index].GetCurrentOffset(offsetReductionTime, offsetReductionAnimationCurve);
	}

	public void SnapToIndex(int index, float snapTime)
	{
		if (true == buttonDown)
			return;

		if ((float)index == selected)
			return;

		SetState(State.SNAP_SLIDING);
		snapTo = (float)index;
		actualSnapTime = snapTime;
	}

	public virtual void OnPointerEnter(PointerEventData eventData)
	{
		if (null != pointerEnterCallback)
			pointerEnterCallback(eventData);
	}

	public virtual void OnPointerExit(PointerEventData eventData)
	{
		if (null != pointerExitCallback)
			pointerExitCallback(eventData);
	}
}
