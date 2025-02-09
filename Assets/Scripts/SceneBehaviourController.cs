using System;
using UI;
using UnityEngine;

public class SceneBehaviourController : MonoBehaviour
{
    public static SceneBehaviourController Instance;
    
    public Action<Vector3, Vector3> OnHitVolume;
    
    private bool _isListenHitVolume = false;
    private int _layerMask = (1 << 12) | (1 << 13) | (1 << 14) | (1 << 15) | (1 << 16) | (1 << 17) | (1 << 18) | (1 << 19) | (1 << 20) | (1 << 21) | (1 << 22) | (1 << 23) | (1 << 24);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Когда пользователь кликает по видовому окну вызывается ViewportClick
        ViewportPanel.OnClick += ViewportClick;

        // Когда луч пересек плоскость вызывается ActionThenClickOnSlice
        OnHitVolume += ActionThenClickOnSlice;
    
        // Можем начать отслеживать нажатия и пересечения
        StartListenHitVolume();
    }

    // Здесь организуем логику когда пользователь нажал на плоскость
    // point - точка пересечения луча и плоскости
    // normal - направление луча
    private void ActionThenClickOnSlice(Vector3 point, Vector3 normal)
    {
        Debug.Log("Точка: " + point.ToString() + " нормаль: " + normal.ToString());

        // Создаем точку
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Transform sphereTransform = sphere.transform;
        
        sphereTransform.position = point;   // Выставляем в положение пересечения
        sphereTransform.localScale = Vector3.one * 0.005f; // Выставляем радиус в 5 миллиметров (масштабируем)
    }

    private void ViewportClick(ViewportPanel vp, Vector2 normalPos)
    {
        if (_isListenHitVolume)
        {
            // Создаем луч из камеры в месте где клинул пользователь
            Ray ray = vp.ViewCamera.ViewportPointToRay(normalPos);
            
            // Если видовое окно является срезом
            if (vp is SliceProjectionView)
            {
                // Raycast сцены, с нашим лучом. Выходная переменная "hit" будет содержать результат, если таковой имеется.
                if (Physics.Raycast(ray, out UnityEngine.RaycastHit raycastHit, 1000, _layerMask))
                {
                    // Вызываем событие пересечения
                    OnHitVolume?.Invoke(raycastHit.point, raycastHit.normal);
                }
            }
            else
            {
                Debug.Log("Not a viewport");
            }
        }
    }

    public void StartListenHitVolume()
    {
        _isListenHitVolume = true;
    }

    public void StopListenHitVolume()
    {
        _isListenHitVolume = false;
    }
}
