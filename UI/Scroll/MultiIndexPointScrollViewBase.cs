using UnityEngine;
using System.Collections;

public abstract class MultiIndexPointScrollViewBase : ScrollViewBase
{
	// selected 값은 좌우로 치우치는 한계값까지 벗어날 수 있다.
	public override float selected
	{
		get
		{
			return this.selectedValue;
		}

		set
		{
			this.selectedValue = Mathf.Clamp(value, -this.minSideSpringOffset, this.count - 1f + this.maxSideSpringOffset);
		}
	}

	// 현재 선택된 항목의 정수 index 값. selected 에서 가장 가까운 정수(반올림) 값이다.
	// 실제 항목을 인덱싱하는데 쓰이므로 유효한 값 범위로 제한
	public override int selectedIndex
	{
		get
		{
			return Mathf.FloorToInt(Mathf.Clamp(this.selected, 0.0f, this.count - 1) + 0.5f);
		}
	}

//	private float selectedValueForUpdatePosition = float.MaxValue;

	[System.Serializable]
	public class IndexPoint
	{
		public float index;
		public Transform point;
	}

	public bool turnOffOutBoundaryItems = true;

	// index point 의 처음과 끝 두 점 위치를 포함하여 그 바깥 인덱스인 항목들은 보이지 않게 된다.
	public IndexPoint[] indexPoints;

	public float minSideSpringOffset;
	public float maxSideSpringOffset;

	protected override void Awake()
	{
		base.Awake();

		if (this.indexPoints.Length < 1)
		{
			Debug.LogError(string.Format("MultiIndexPointCardScrollView {0} need at least 1 index point.", gameObject.name));
		}

		System.Array.Sort(this.indexPoints, (IndexPoint a, IndexPoint b) => a.index.CompareTo(b.index));
	}

	protected abstract void UpdateItemVisibilityPositionRotation(int index, bool visibility, Vector3 position, Quaternion rotation);

	protected abstract void UpdateItemCurrentOffset(int index);
	protected abstract float GetItemCurrentOffset(int index);


	protected override void UpdateContentsPosition()
	{
		// propagate "selected" variable value to position
		if (selectedValueForUpdatePosition == selectedValue)	// 무조건 매 프레임 업데이트하는 것 말고는 방법이 없을지 생각해보자..
			return;

		selectedValueForUpdatePosition = selectedValue;

		if (0 == count)
			return;

		int itemIndex = 0;
		float accumulatedOffset = 0.0f;

		// 작은 쪽 인덱스 바깥 카드
		while (itemIndex < count &&
			(float)itemIndex + accumulatedOffset + GetItemCurrentOffset(itemIndex) <= this.indexPoints[0].index + this.selected)
		{
			accumulatedOffset += GetItemCurrentOffset(itemIndex);

			UpdateItemVisibilityPositionRotation(itemIndex, !this.turnOffOutBoundaryItems, this.indexPoints[0].point.position, this.indexPoints[0].point.rotation);

			UpdateItemCurrentOffset(itemIndex);

			++itemIndex;
		}

		for (int i = 0; i < this.indexPoints.Length - 1; ++i)
		{
			IndexPoint indexPointFrom = this.indexPoints[i];
			IndexPoint indexPointTo = this.indexPoints[Mathf.Min(i + 1, this.indexPoints.Length - 1)];

			float indexFrom = indexPointFrom.index + this.selected;
			float indexTo = indexPointTo.index + this.selected;

			while (itemIndex < count &&
				indexFrom < (float)itemIndex + accumulatedOffset + GetItemCurrentOffset(itemIndex) &&
				(float)itemIndex + accumulatedOffset + GetItemCurrentOffset(itemIndex) <= indexTo)
			{
				accumulatedOffset += GetItemCurrentOffset(itemIndex);

				float actualIndex = (float)itemIndex + accumulatedOffset;

				float t = (actualIndex - indexFrom) / (indexTo - indexFrom);

				UpdateItemVisibilityPositionRotation(itemIndex, true,
					Vector3.Lerp(indexPointFrom.point.position, indexPointTo.point.position, t),
					Quaternion.Slerp(indexPointFrom.point.rotation, indexPointTo.point.rotation, t));

				UpdateItemCurrentOffset(itemIndex);

				++itemIndex;
			}
		}

		// 큰 쪽 인덱스 바깥 카드
		IndexPoint lastIndexPoint = this.indexPoints[this.indexPoints.Length - 1];
		while (itemIndex < count)
		{
			accumulatedOffset += GetItemCurrentOffset(itemIndex);

			UpdateItemVisibilityPositionRotation(itemIndex, !this.turnOffOutBoundaryItems, lastIndexPoint.point.position, lastIndexPoint.point.rotation);

			UpdateItemCurrentOffset(itemIndex);

			++itemIndex;
		}
	}

	public void ForcePositionUpdate()
	{
// 		selectedValueForUpdatePosition = float.MaxValue;
	}
}
