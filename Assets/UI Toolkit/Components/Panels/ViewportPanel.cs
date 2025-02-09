using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class ViewportPanel : PanelBase, IResizible
    {
        public static Action<ViewportPanel, Vector2> OnClick;

        public Camera ViewCamera => _viewCamera;
        
        public static Camera FocusedCamera => _focusedCamera;
        private static Camera _focusedCamera;
        
        private CameraManipulator _cameraManipulator;
        
        protected Camera _viewCamera;
        
        public ViewportPanel()
        {
            _viewCamera = new GameObject("Camera").AddComponent<Camera>();
            _viewCamera.clearFlags = CameraClearFlags.SolidColor;
            _viewCamera.backgroundColor = new Color(0.1490196f, 0.145098f, 0.172549f);
            _viewCamera.orthographicSize = 0.6f;
            _viewCamera.cullingMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 5) | (1 << 11);
            _viewCamera.transform.localPosition = Vector3.forward;
            _viewCamera.transform.LookAt(Vector3.zero);
            
            this.RegisterCallback<GeometryChangedEvent>(ResizeCamera);
            
            _cameraManipulator = new CameraManipulator(_viewCamera);
            this.AddManipulator(_cameraManipulator);
            
            this.RegisterCallback<ClickEvent>(OnClickViewport);
        }

        private void ResizeCamera(GeometryChangedEvent evt)
        {
            Rect newRect = this.worldBound;
            float normalizedX = newRect.x / Screen.width;
            float normalizedY = (Screen.height - newRect.y - newRect.height) / Screen.height;
            float normalizedWidth = newRect.width / Screen.width;
            float normalizedHeight = newRect.height / Screen.height;

            _viewCamera.rect = new Rect(normalizedX, normalizedY, normalizedWidth, normalizedHeight);
        }
        
        private void OnClickViewport(ClickEvent evt)
        {
            // Получаем размеры окна или UI элемента
            var panelWidth = this.resolvedStyle.width;
            var panelHeight = this.resolvedStyle.height;

            // Преобразуем координаты клика в нормализованные значения
            OnClick?.Invoke(this, new Vector2(
                evt.localPosition.x / panelWidth,
                1 - evt.localPosition.y / panelHeight));
        }
        
        protected override void SetFocused()
        {
            base.SetFocused();

            _focusedCamera = _viewCamera;
        }

        protected override void SetUnfocused()
        {
            base.SetUnfocused();

            _focusedCamera = null;
        }
    }
}

