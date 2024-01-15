using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;

public interface IScrollerData<T>
{
    public void SetIndex(int index);
    public void SetData(T data);
}

public class ScrollerDelegate<T> : IEnhancedScrollerDelegate
{
    public SmallList<T> datas;
    public EnhancedScrollerCellView cellViewPrefab;
    public float cellViewSize = 100f;

    public ScrollerDelegate(EnhancedScrollerCellView cellView, SmallList<T> datas, float cellViewSize = 100f)
    {
        this.datas = datas;
        this.cellViewPrefab = cellView;
        this.cellViewSize = cellViewSize;
    }

    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return datas.Count;
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return cellViewSize;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        EnhancedScrollerCellView cellView = scroller.GetCellView(cellViewPrefab);

        cellView.name = "Cell " + dataIndex.ToString();
        if(cellView is IScrollerData<T>)
        {
            IScrollerData<T> cellData = cellView as IScrollerData<T>;
            cellData.SetIndex(dataIndex);
            cellData.SetData(datas[dataIndex]);
        }

        return cellView;
    }
}
