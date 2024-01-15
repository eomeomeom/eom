using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraHelper : MonoBehaviour
{
    private Camera _camera;
    private float? _currFixedVerticalFov;
    private int? _currScreenWidth;
    private int? _currScreenHeight;
    public bool _getCameraFovOnStart = true;
    public float _baseVerticalFov = 45.0f;
    public bool _isLetterbox;

    public float _baseOrthographicSize;
    public float _baseScreenRatio;

    void Start()
    {
        _camera = GetComponent<Camera>();

        if (_getCameraFovOnStart)
        {
            _baseVerticalFov = _camera.fieldOfView;
        }
    }

    void LateUpdate()
    {
        bool changed = false;

        if (false == _currFixedVerticalFov.HasValue || _currFixedVerticalFov.Value == _baseVerticalFov)
        {
            changed = true;
        }
        else if (false == _currScreenWidth.HasValue || _currScreenWidth == Screen.width)
        {
            changed = true;
        }
        else if (false == _currScreenHeight.HasValue || _currScreenHeight == Screen.height)
        {
            changed = true;
        }

        if (changed)
        {
            _currFixedVerticalFov = _baseVerticalFov;
            _currScreenWidth = Screen.width;
            _currScreenHeight = Screen.height;

            float standardRatio = CQScreenSetupData.Instance.standardWidth / CQScreenSetupData.Instance.standardHeight;
            float screenRatio = (float)Screen.width / (float)Screen.height;

            if (_camera.orthographic)
            {
                if (screenRatio > standardRatio)
                {
                    _camera.orthographicSize = _baseOrthographicSize;
                }
                else
                {
                   _camera.orthographicSize = _baseOrthographicSize + ((_baseScreenRatio - screenRatio) * 20);         
                }
            }
            else
            {
                if (this._isLetterbox)
                {
                    _camera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

                    if (screenRatio > standardRatio)
                    {
                        // 기준 종횡비보다 화면 종횡비가 크다 -> 기준화면보다 가로로 더 넓은 상황. 좌우 레터박스 적용.
                        _camera.fieldOfView = _baseVerticalFov;
                    }
                    else
                    {
                        float ratio = standardRatio / screenRatio;
                        _camera.fieldOfView = Mathf.Atan(Mathf.Tan(_baseVerticalFov / 2.0f * Mathf.Deg2Rad) * ratio) * Mathf.Rad2Deg * 2.0f;
                    }
                }
                else
                {
                    if (screenRatio > standardRatio)
                    {
                        // 기준 종횡비보다 화면 종횡비가 크다 -> 기준화면보다 가로로 더 넓은 상황. 좌우 레터박스 적용.
                        float ratio = standardRatio / screenRatio;
                        _camera.rect = new Rect((1.0f - ratio) / 2.0f, 0.0f, ratio, 1.0f);
                        _camera.fieldOfView = _baseVerticalFov;
                    }
                    else
                    {
                        // 기준 종횡비보다 화면 종횡비가 작다 -> 기준화면보다 세로로 더 긴 상황. 상하 레터박스 적용.
                        float ratio = screenRatio / standardRatio;
                        _camera.rect = new Rect(0.0f, (1.0f - ratio) / 2.0f, 1.0f, ratio);
                        _camera.fieldOfView = _baseVerticalFov;
                    }
                }
            }

        }
    }
}
