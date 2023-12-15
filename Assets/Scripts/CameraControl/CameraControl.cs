using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    public static CameraControl Instance => _instance; 

    public InputActionAsset mapActions;
    
    private static CameraControl _instance;
    private Camera _controlledCamera;
    private InputAction _panAction;
    private InputAction _zoomAction;
    private InputAction _rotateAction;
    
    private void Awake()
    {
        _instance = this;
        
        _panAction = mapActions.FindAction("Pan");
        _zoomAction = mapActions.FindAction("Zoom");
        _rotateAction = mapActions.FindAction("Orbit");
    }
    
    private void OnEnable()
    {
        _panAction.Enable();
        _zoomAction.Enable();
        _rotateAction.Enable();
    }

    private void OnDisable()
    {
        _panAction.Disable();
        _zoomAction.Disable();
        _rotateAction.Disable();
    }

    public void SetCamera(Camera camera)
    {
        if (_controlledCamera != camera)
        {
            _controlledCamera = camera;
        }
    } 
    
    private void Update()
    {
        if (_controlledCamera == null) return;

        if (_controlledCamera.orthographic)
        {
            // Ортогональная камера
            HandleOrthographicControls();
        }
        else
        {
            // Перспективная камера
            HandlePerspectiveControls();
        }
    }

    private void HandleOrthographicControls()
    {
        PanCamera(_panAction.ReadValue<Vector2>());
        ZoomCameraOrthographic(_zoomAction.ReadValue<float>());
    }

    private void HandlePerspectiveControls()
    {
        PanCamera(_panAction.ReadValue<Vector2>());
        ZoomCameraPerspective(_zoomAction.ReadValue<float>());
        OrbitCamera(_rotateAction.ReadValue<Vector2>());
    }

    private void PanCamera(Vector2 input)
    {
        Vector3 moveDirection = new Vector3(-input.x, -input.y, 0);
        moveDirection = _controlledCamera.transform.TransformDirection(moveDirection);
        moveDirection = Vector3.ProjectOnPlane(moveDirection, _controlledCamera.transform.forward);

        // Уменьшаем скорость панорамирования при уменьшении orthographicSize
        float panSpeed = 5f * _controlledCamera.orthographicSize;
        _controlledCamera.transform.Translate(moveDirection * panSpeed * Time.deltaTime, Space.World);
    }

    private void ZoomCameraOrthographic(float input)
    {
        float zoomSpeed = 5f;
        _controlledCamera.orthographicSize -= input * zoomSpeed * Time.deltaTime;
        _controlledCamera.orthographicSize = Mathf.Max(_controlledCamera.orthographicSize, 0.1f);
    }

    private void ZoomCameraPerspective(float input)
    {
        float zoomSpeed = 20f;
        _controlledCamera.transform.Translate(_controlledCamera.transform.forward * input * zoomSpeed * Time.deltaTime, Space.World);
    }

    private void OrbitCamera(Vector2 input)
    {
        float rotateSpeed = 100f;

        // Горизонтальное вращение
        _controlledCamera.transform.RotateAround(Vector3.zero, Vector3.up, input.x * rotateSpeed * Time.deltaTime);

        // Вертикальное вращение
        float pitchAngle = Mathf.Clamp(-input.y * rotateSpeed * Time.deltaTime, -89f, 89f); // Ограничение угла наклона
        Vector3 rightAxis = Vector3.Cross(Vector3.up, _controlledCamera.transform.forward).normalized;
        _controlledCamera.transform.RotateAround(Vector3.zero, rightAxis, pitchAngle);
    }
}
