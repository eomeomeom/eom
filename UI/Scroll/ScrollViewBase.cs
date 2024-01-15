using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public abstract class ScrollViewBase : UIBehaviour, ICanvasRaycastFilter, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerDownHandler, IPointerUpHandler
{
	public const float EPSILON = 1e-005f;

	public abstract int count
	{
		get;
    }

	private RectTransform rectTransformValue;
	protected RectTransform rectTransform
	{
		get
		{
			if (null == this.rectTransformValue)
				this.rectTransformValue = (RectTransform)transform;

			return this.rectTransformValue;
		}
	}

	protected float selectedValue;
	public virtual float selected
	{
		get
		{
			return this.selectedValue;
		}

		set
		{
			if (0 == this.count)
			{
				this.selectedValue = 0.0f;
				return;
			}

			this.selectedValue = Mathf.Clamp(value, 0.0f, this.count - 1);
		}
	}

	// 현재 선택된 항목의 정수 index 값. selected 에서 가장 가까운 정수(반올림) 값이다.
	public virtual int selectedIndex
	{
		get
		{
			return Mathf.FloorToInt(this.selected + 0.5f);
		}
	}

	protected enum State
	{
		NULL,					// state 최초 상태. IDLE 로 진입할 수 있게 하기 위해.
		IDLE,					// 아무것도 안하는 상태
		DRAG_SLIDING,			// 좌우 슬라이딩중(버튼눌림/터치됨)
		INERTIA_SLIDING,		// 관성 슬라이딩중(버튼/터치 없음)
		SNAP_SLIDING,			// 스냅 위치로 이동중
		WAIT_DRAGGING,			// 제자리에서 드래그&드랍 대기중
		DRAGGING_INITIALIZED,	// 드래그&드랍이 시작되어 커서/터치를 따라다니는 카드가 팝업되었으나, 아직 이동은 하지 않은 상태
		DRAGGING,				// 드래그&드랍 이동중
	}
	protected class StateCapsule
	{
		private State stateValue = State.NULL;
		public State state
		{
			get
			{
				return this.stateValue;
			}
		}

		public void SetState(State newState)
		{
			this.stateValue = newState;
		}
	}
	protected StateCapsule stateCapsule = new StateCapsule();

	protected void SetState(State newState, bool forceSet = false)
	{
		if (this.stateCapsule.state == newState && false == forceSet)
			return;

		switch (this.stateCapsule.state)
		{
			case State.IDLE:				OnExitIdleState(newState); break;
			case State.DRAG_SLIDING:		OnExitDragSlidingState(newState); break;
			case State.INERTIA_SLIDING:		OnExitInertiaSlidingState(newState); break;
			case State.SNAP_SLIDING:		OnExitSnapSlidingState(newState); break;
			case State.WAIT_DRAGGING:		OnExitWaitDraggingState(newState); break;
			case State.DRAGGING_INITIALIZED:OnExitDraggingInitializedState(newState); break;
			case State.DRAGGING:			OnExitDraggingState(newState); break;
		}

		State prevState = this.stateCapsule.state;
		this.stateCapsule.SetState(newState);

		switch (this.stateCapsule.state)
		{
			case State.IDLE:				OnEnterIdleState(prevState); break;
			case State.DRAG_SLIDING:		OnEnterDragSlidingState(prevState); break;
			case State.INERTIA_SLIDING:		OnEnterInertiaSlidingState(prevState); break;
			case State.SNAP_SLIDING:		OnEnterSnapSlidingState(prevState); break;
			case State.WAIT_DRAGGING:		OnEnterWaitDraggingState(prevState); break;
			case State.DRAGGING_INITIALIZED:OnEnterDraggingInitializedState(prevState); break;
			case State.DRAGGING:			OnEnterDraggingState(prevState); break;
		}
	}

	// state enter callback
	protected virtual void OnEnterIdleState(State prevState)
	{
		this.preslideMovementAccumulated = Vector2.zero;
	}

	protected virtual void OnEnterDragSlidingState(State prevState)
	{
		this.slidingSpeed = 0.0f;
	}

	protected virtual void OnEnterInertiaSlidingState(State prevState)
	{
		this.preslideMovementAccumulated = Vector2.zero;
	}

	protected virtual void OnEnterSnapSlidingState(State prevState)
	{
		this.slidingSpeed = 0.0f;

		this.actualSnapTime = this.snapTime;    // 설정값을 복사해 쓰는 이유는 경우에 따라 snap time 을 오버라이드해서 쓸 수 있기 때문
		this.snapCurTime = 0.0f;
		this.snapFrom = this.selected;
		this.snapTo = this.selectedIndex;

		this.preslideMovementAccumulated = Vector2.zero;
	}

	protected virtual void OnEnterWaitDraggingState(State prevState)
	{
		this.pushTimeCounter = 0.0f;
		this.preslideMovementAccumulated = Vector2.zero;
	}

	protected virtual void OnEnterDraggingInitializedState(State prevState) { }
	protected virtual void OnEnterDraggingState(State prevState) { }

	// state exit callback
	protected virtual void OnExitIdleState(State nextState) { }
	protected virtual void OnExitDragSlidingState(State nextState) { }
	protected virtual void OnExitInertiaSlidingState(State nextState) { }
	protected virtual void OnExitSnapSlidingState(State nextState) { }
	protected virtual void OnExitWaitDraggingState(State nextState) { }
	protected virtual void OnExitDraggingInitializedState(State nextState) { }
	protected virtual void OnExitDraggingState(State nextState) { }

	public UnityEngine.UI.Text stateDisplayText = null;


	public bool dragEnabled = false;
	private bool draggable
	{
		get
		{
			return (this.dragEnabled && this.count > 0);
		}
	}
	public RectTransform draggingIconParent;
	public float pushTimeToBeginDraggingCard = 0.5f;
	private float pushTimeCounter;

	protected Vector2 draggingPoint;


	public bool inertiaEnabled = true;
	[HideInInspector]
    public float slidingSpeed;
	public float xSlidingSensitivityMultiplier = 1.0f;
	public float xSlidingDeadZone = 5.0f;
	public enum YAxisMovementApplication
	{
		SLIDING,
		BEGIN_DRAGGING,
		IGNORE
	}
	public YAxisMovementApplication yAxisMovementApplication = YAxisMovementApplication.SLIDING;
	public float ySlidingSensitivityMultiplier = 0.0f;
	public float ySlidingDeadZone = 10.0f;

	Vector2 preslideMovementAccumulated;	// slide 상태에 들어가기 전 이동 누적값

	public float slidingSpeedDamping = 0.3f;

	Vector2 pointLastLocalCursor;

	public bool useSnap = true;
	public float snapTime = 0.1f;
	protected float actualSnapTime;
	private float snapCurTime;
	private float snapFrom;
	protected float snapTo;

	public AnimationCurve snapSlidingCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

	public bool IsScrollStopped()
	{
		return (State.IDLE == stateCapsule.state);
	}

	// ScrollViewBase 는 Graphic 을 상속받은 클래스가 아니라서 이 함수는 사용되지 않지만
	// 추후 custom raycaster 를 사용한다던가 하는 방법을 시도할 때에 사용하기 위해 남겨둠
	// 이를 사용하려는 목적은 ScrollViewBase 의 rect 영역 중 Graphic 을 상속받은 자식 오브젝트가 없는 곳에서도 raycast 가 되어
	// UI 이벤트에 반응이 가능하도록 하기 위해.
	// 현재는 ScrollViewBase 가 붙은 GameObject 에 투명한 Image 컴포넌트를 붙여서 이를 통해 raycast 가 이루어지도록 씬 구성을 해야 한다.
	public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
	{
		return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera);
	}

	public virtual void OnInitializePotentialDrag(PointerEventData eventData)
	{
	}

	public virtual void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out this.pointLastLocalCursor);

		if (null != this.draggingIconParent)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.draggingIconParent, eventData.position, eventData.pressEventCamera, out this.draggingPoint);
		}
	}

	public virtual void OnDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		if (stateCapsule.state == State.DRAGGING_INITIALIZED)
			SetState(State.DRAGGING);

		Vector2 localCursor;
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
			return;

		Vector2 pointerDelta = localCursor - this.pointLastLocalCursor;
		this.pointLastLocalCursor = localCursor;

		float horzValue = -(pointerDelta.x * this.xSlidingSensitivityMultiplier);
		float vertValue = -(pointerDelta.y * this.ySlidingSensitivityMultiplier);

		if (stateCapsule.state == State.IDLE ||
			stateCapsule.state == State.WAIT_DRAGGING ||
			stateCapsule.state == State.INERTIA_SLIDING ||
			stateCapsule.state == State.SNAP_SLIDING)
		{
			this.preslideMovementAccumulated.x += horzValue;
			this.preslideMovementAccumulated.y += vertValue;

			if (YAxisMovementApplication.BEGIN_DRAGGING == this.yAxisMovementApplication)
			{
				if (Mathf.Abs(this.preslideMovementAccumulated.x) > this.xSlidingDeadZone)
					SetState(State.DRAG_SLIDING);
				else if (
					Mathf.Abs(this.preslideMovementAccumulated.y) > this.ySlidingDeadZone &&
					draggable && CheckDraggableWithPointerEventData(eventData))
					SetState(State.DRAGGING_INITIALIZED);
			}
			else if (YAxisMovementApplication.SLIDING == this.yAxisMovementApplication)
			{
				if (Mathf.Abs(this.preslideMovementAccumulated.x) > this.xSlidingDeadZone ||
					Mathf.Abs(this.preslideMovementAccumulated.y) > this.ySlidingDeadZone)
					SetState(State.DRAG_SLIDING);
			}
			else if (YAxisMovementApplication.IGNORE == this.yAxisMovementApplication)
			{
				if (Mathf.Abs(this.preslideMovementAccumulated.x) > this.xSlidingDeadZone)
					SetState(State.DRAG_SLIDING);
			}
		}

		float slideValue = horzValue;
		if (YAxisMovementApplication.SLIDING == this.yAxisMovementApplication)
			slideValue += vertValue;

		if (stateCapsule.state ==  State.DRAG_SLIDING)
		{
			this.slidingSpeed = slideValue / Time.unscaledDeltaTime / 10000.0f;
		}

		if (null != this.draggingIconParent)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.draggingIconParent, eventData.position, eventData.pressEventCamera, out this.draggingPoint);
		}
	}

	public virtual void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		if (stateCapsule.state == State.DRAGGING)
		{
			SetState(State.IDLE);
		}
		else if (stateCapsule.state == State.DRAG_SLIDING)
		{
			if (this.inertiaEnabled)
			{
				SetState(State.INERTIA_SLIDING);
			}
			else
			{
				SetState(State.SNAP_SLIDING);
			}
		}
	}

	public virtual void OnPointerDown(PointerEventData eventData)
	{
		// ScrollViewBase 에서 pointer down 시 해야 할 처리는 OnInitializePotentialDrag() 에서 해 주고 있었..
		// 다가 scroll view 내부의 아이템에서 pointer down/up handling 을 할 때에도 정상적으로 작동하게 하기 위해
		// OnPointerDown 으로 해당 코드들을 옮겨옴.
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		// 정상적인 경우에는 의미 없는 코드이지만,
		// 드래깅 도중 포커스를 잃었다 복귀하는 경우를 '적당히' 처리해주기 위해.
		if (State.DRAGGING == stateCapsule.state)
			SetState(State.IDLE);

		// 슬라이딩중일때는 드래그 카운터 시작을 하지 않도록 처리.
		// DRAG_SLIDING 이 없는 이유는 그 상태에는 버튼이 눌러져있으므로
		if (State.INERTIA_SLIDING == stateCapsule.state || State.SNAP_SLIDING == stateCapsule.state)
			return;

		// 0 == cardList.Count 인 경우도 커버함
		if (this.selectedIndex >= this.count)
			return;

		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out this.pointLastLocalCursor);

		if (draggable && CheckDraggableWithPointerEventData(eventData))
		{
			SetState(State.WAIT_DRAGGING);

			if (null != this.draggingIconParent)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(this.draggingIconParent, eventData.position, eventData.pressEventCamera, out this.draggingPoint);
			}
		}
	}

	public virtual void OnPointerUp(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		if (stateCapsule.state == State.WAIT_DRAGGING)
		{
			SetState(State.IDLE);
		}
		else if (stateCapsule.state == State.DRAGGING_INITIALIZED)
		{
			// dragging 상태가 시작 된 후 전혀 움직이지 않고 있다가 버튼을 뗄 경우 이 쪽으로 오게 된다.
			SetState(State.IDLE);
		}
	}

	[System.Serializable]
	public class ScrollViewBaseDragDropEvent : UnityEngine.Events.UnityEvent<ScrollViewBase, ScrollViewBase> { }
	public ScrollViewBaseDragDropEvent dragDropEvent = new ScrollViewBaseDragDropEvent();

	// 참고용 코드. ScrollViewBase 를 상속받은 실제 사용할 컴포넌트 클래스에서 OnDrop 을 아래와 같은 구조로 재정의해 사용해야함.
	// 어떻게 좀 구조화 할 방법이 없을까..?
	public virtual void OnDrop(PointerEventData data)
	{
		ScrollViewBase sender = data.pointerDrag.GetComponent<ScrollViewBase>();
		if (null == sender)
			return;

		if (sender.stateCapsule.state != State.DRAGGING)
			return;

		dragDropEvent.Invoke(sender, this);
	}

	protected virtual bool CheckDraggableWithPointerEventData(PointerEventData eventData)
	{
		return true;
	}

	// selected 값으로부터 실제 스크롤 뷰 내용물의 위치를 계산해 설정해주는 함수
	protected abstract void UpdateContentsPosition();

	protected virtual void UpdateStateIdle()
	{
		// 최초 프레임이나, force update 했을 때 필요하므로 idle 상태에서도 UpdateContentPosition을 호출해야한다.
		UpdateContentsPosition();
	}

	protected virtual void UpdateStateWaitDragging()
	{
		if (this.pushTimeCounter >= this.pushTimeToBeginDraggingCard)
		{
			SetState(State.DRAGGING_INITIALIZED);
		}
		else
		{
			this.pushTimeCounter += Time.unscaledDeltaTime;
		}
	}

	protected virtual void UpdateStateDragging()
	{
	}

	protected virtual void UpdateStateSliding()
	{
		if (Mathf.Abs(this.slidingSpeed) < EPSILON) this.slidingSpeed = 0.0f;

		// applying sliding speed to "selected" variable
		if (0.0f != this.slidingSpeed)
		{
			float prevSelected = this.selected;
			this.selected += (this.slidingSpeed * Time.unscaledDeltaTime * 60.0f);

			if (prevSelected == this.selected)
			{
				this.slidingSpeed = 0.0f;
			}
		}

		// check to trigger snap sliding at the end of inertia sliding
		if (stateCapsule.state == State.INERTIA_SLIDING)
		{
			if (this.slidingSpeed == 0.0f)
			{
				if (this.useSnap && this.selectedIndex != selected)
				{
					SetState(State.SNAP_SLIDING);
				}
				else
				{
					SetState(State.IDLE);
				}
			}
		}

		// sliding speed update
		if (stateCapsule.state == State.SNAP_SLIDING)
		{
			if (this.snapCurTime >= this.actualSnapTime)
			{
				this.selected = this.snapTo;
				SetState(State.IDLE);
			}
			else
			{
				float actualT = this.snapSlidingCurve.Evaluate(this.snapCurTime / this.actualSnapTime);
				this.selected = Mathf.Lerp(this.snapFrom, this.snapTo, actualT);
				this.snapCurTime += Time.unscaledDeltaTime;
			}

			this.slidingSpeed = 0.0f;
		}
		else
		{
			if (this.slidingSpeed > 0.0f)
			{
				this.slidingSpeed -= (this.slidingSpeedDamping * Time.unscaledDeltaTime);

				if (this.slidingSpeed < 0.0f)
				{
					this.slidingSpeed = 0.0f;
				}
			}
			else if (this.slidingSpeed < 0.0f)
			{
				this.slidingSpeed += (this.slidingSpeedDamping * Time.unscaledDeltaTime);

				if (this.slidingSpeed > 0.0f)
				{
					this.slidingSpeed = 0.0f;
				}
			}
		}

		UpdateContentsPosition();
	}

	protected override void Start()
	{
		SetState(State.IDLE);
	}

	protected virtual void Update()
	{
		if (null != stateDisplayText)
			stateDisplayText.text = stateCapsule.state.ToString();

		switch (stateCapsule.state)
		{
			case State.IDLE:
				//UnityEngine.Profiling.Profiler.BeginSample("UpdateStateIdle");
				UpdateStateIdle();
				//UnityEngine.Profiling.Profiler.EndSample();
				break;

			case State.WAIT_DRAGGING:
				//UnityEngine.Profiling.Profiler.BeginSample("UpdateStateWaitDragging");
				UpdateStateWaitDragging();
				//UnityEngine.Profiling.Profiler.EndSample();
				break;

			case State.DRAGGING_INITIALIZED:
				//UnityEngine.Profiling.Profiler.BeginSample("UpdateStateDragging");
				UpdateStateDragging();
				//UnityEngine.Profiling.Profiler.EndSample();
				break;

			case State.DRAGGING:
				//UnityEngine.Profiling.Profiler.BeginSample("UpdateStateDragging");
				UpdateStateDragging();
				//UnityEngine.Profiling.Profiler.EndSample();
				break;

			case State.DRAG_SLIDING:
				//UnityEngine.Profiling.Profiler.BeginSample("UpdateStateSliding");
				UpdateStateSliding();
				//UnityEngine.Profiling.Profiler.EndSample();
				break;

			case State.INERTIA_SLIDING:
				//UnityEngine.Profiling.Profiler.BeginSample("UpdateStateSliding");
				UpdateStateSliding();
				//UnityEngine.Profiling.Profiler.EndSample();
				break;

			case State.SNAP_SLIDING:
				//UnityEngine.Profiling.Profiler.BeginSample("UpdateStateSliding");
				UpdateStateSliding();
				//UnityEngine.Profiling.Profiler.EndSample();
				break;

			default:
				break;
		}
	}
}
