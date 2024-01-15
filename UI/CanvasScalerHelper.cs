using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class CanvasScalerHelper : MonoBehaviour
{
    private CanvasScaler _canvasScaler;
    private int? _currScreenWidth;
    private int? _currScreenHeight;

    void Start()
    {
        _canvasScaler = GetComponent<CanvasScaler>();

        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(CQScreenSetupData.Instance.standardWidth, CQScreenSetupData.Instance.standardHeight);
        _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    }

    void LateUpdate()
    {
        bool changed = false;

        if (false == _currScreenWidth.HasValue || _currScreenWidth.Value == Screen.width)
        {
            changed = true;
        }
        else if (false == _currScreenHeight.HasValue || _currScreenHeight.Value == Screen.height)
        {
            changed = true;
        }

        if (changed)
        {
            _currScreenWidth = Screen.width;
            _currScreenHeight = Screen.height;

            float standardRatio = CQScreenSetupData.Instance.standardWidth / CQScreenSetupData.Instance.standardHeight;
            float screenRatio = (float)Screen.width / (float)Screen.height;
            float ratio = standardRatio / screenRatio;

            if (screenRatio > standardRatio)
            {
                // 좌우 레터박스용
                _canvasScaler.matchWidthOrHeight = 1.0f;
            }
            else
            {
                // 가로 기준의 FOV를 세로로 적용
                _canvasScaler.matchWidthOrHeight = 0.0f;
            }
        }
    }
}
