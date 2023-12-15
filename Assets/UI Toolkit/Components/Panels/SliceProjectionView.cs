using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class SliceProjectionView : ViewportPanel
{
    #region Style fields
    
    private static readonly string _sliderStyle = "slice-projection-slider";

    #endregion
    
    #region Delegates and Events

    public delegate void SliceProjectionViewCreated(SliceProjectionView view);
    public static SliceProjectionViewCreated OnSliceProjectionViewCreated;

    #endregion

    #region Public fields

    public SliceProjectionAxis ProjectionAxis => _axis;

    #endregion

    #region Private fields

    private SliceProjectionAxis _axis;
    private int _layer;
    private Transform _sliceProjections;
    private Slider _slicePositionSlider;

    #endregion
    
    public SliceProjectionView(int layer, SliceProjectionAxis axis)
    {
        _axis = axis;
        _layer = layer;
        _viewCamera.orthographic = true;
        _viewCamera.name = $"Camera_{axis.ToString()}_{layer}";
        _viewCamera.cullingMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 5) | (1 << _layer);
        
        _slicePositionSlider = new Slider(-0.5f, 0.5f, SliderDirection.Vertical);
        _slicePositionSlider.AddToClassList(_sliderStyle);
        this.Add(_slicePositionSlider);
        _slicePositionSlider.RegisterValueChangedCallback(OnSliderPositionValueChange);
    }

    private void OnSliderPositionValueChange(ChangeEvent<float> evt)
    {
        if (_sliceProjections == null) return;

        float newVal = evt.newValue; // Значение от -0.5 до 0.5
        
        _sliceProjections.localPosition = new Vector3(
            _axis == SliceProjectionAxis.Sagittal ? newVal : 0,
            _axis == SliceProjectionAxis.Coronal ? newVal : 0,
            _axis == SliceProjectionAxis.Axial ? newVal : 0
        );
    }

    public void AddSliceProjections(Transform slice)
    {
        if (_sliceProjections == slice) return;

        slice.gameObject.layer = _layer;
        _sliceProjections = slice;

        // Вычисление положения и ориентации камеры
        PositionAndOrientCamera(slice);
    }
    
    private void PositionAndOrientCamera(Transform slice)
    {
        Vector3 sliceNormal;
        Vector3 cameraPositionOffset;

        switch (_axis)
        {
            case SliceProjectionAxis.Axial:
                sliceNormal = slice.up; 
                cameraPositionOffset = Vector3.forward;
                break;
            case SliceProjectionAxis.Coronal:
                sliceNormal = slice.up; 
                cameraPositionOffset = Vector3.right; 
                break;
            case SliceProjectionAxis.Sagittal:
                sliceNormal = slice.up; 
                cameraPositionOffset = Vector3.up;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Установка ориентации камеры
        _viewCamera.transform.rotation = Quaternion.LookRotation(sliceNormal, cameraPositionOffset);

        // Установка положения камеры на расстоянии 1 метра от среза
        _viewCamera.transform.position = slice.position + sliceNormal * 1.0f; 

        // Направление камеры на центр среза
        _viewCamera.transform.LookAt(slice.position);
    }
}
