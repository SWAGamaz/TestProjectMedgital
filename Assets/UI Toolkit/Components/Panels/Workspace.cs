using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class Workspace : VisualElement
    {
        #region Properties
    
        public static Workspace Instance { get; private set; }
        
        #endregion
        
        #region Private fields

        private List<Splitter> _splittersList;
        private List<VisualElement> _resizibleList = new List<VisualElement>();
        private List<VisualElement> _listPre = new List<VisualElement>();
        private List<VisualElement> _listPost = new List<VisualElement>();
        private Splitter _draggingSplitter;

        #endregion

        public Workspace()
        {
            Instance = this;
            
            // Создаем стандартную структуру

            GroupLayout vertical1 = new GroupLayout(FlexDirection.Column);
            ViewportPanel axial = SliceProjectionViewManager.CreateView(SliceProjectionAxis.Axial);
            ViewportPanel coronal = SliceProjectionViewManager.CreateView(SliceProjectionAxis.Coronal);
            
            GroupLayout vertical2 = new GroupLayout(FlexDirection.Column);
            ViewportPanel perspective = new PerspectiveView();
            ViewportPanel sagittal = SliceProjectionViewManager.CreateView(SliceProjectionAxis.Sagittal);

            if (axial == null || coronal == null || sagittal == null)
            {
                throw new NullReferenceException("Не удалось создать проеции срезов");
            }
            
            vertical1.Add(axial);
            vertical1.Add(coronal);
            vertical2.Add(perspective);
            vertical2.Add(sagittal);
            
            Inspector inspector = new Inspector();
            
            this.Add(vertical1);
            this.Add(vertical2);
            this.Add(inspector);
            
            // Определяем все элементы IResizible

            foreach (var elem in this.Query<VisualElement>().ToList())
            {
                if (elem is IResizible)
                {
                    _resizibleList.Add(elem);
                }
            }
            
            // Заполняем сплиттерами
            _splittersList = new List<Splitter>();
            BuildSplitters(this);

            // Обнавляем положения после перерасчета геометрии
            // Например в случае изменении размера приложения
            this.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            foreach (var elem in _resizibleList)
            {
                elem.style.width = elem.worldBound.width;
                elem.style.height = elem.worldBound.height;
            }
            
            foreach (var splitter in _splittersList)
            {
                splitter.ResetAnchor();
            }
        }

        private void BuildSplitters(VisualElement parent)
        {
            // Получите общее количество дочерних элементов
            int childCount = parent.childCount;

            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = parent[i];

                // Рекурсивный проход
                if (child is GroupLayout childGroup)
                    BuildSplitters(child);

                // Если не последний элемент
                if (i != childCount - 1)
                {
                    if (child is GroupLayout or IResizible)
                    {
                        bool isVert = parent == this;

                        if (parent is GroupLayout parentLayout)
                            isVert = parentLayout.Direction is FlexDirection.Row or FlexDirection.RowReverse;

                        // Если группа горизонтальная или сам workspace, то напавление сплитера вертикальное и наоборот
                        SplitterDirection direction =
                            (isVert) ? SplitterDirection.Vertical : SplitterDirection.Horizontal;
                        // Создаем разделитель
                        Splitter splitter = new Splitter(parent, direction);
                        splitter.OnDragStartHandler += OnDragStart;
                        // Размещаем сплиттер за элементом
                        splitter.PlaceInFront(child);
                        _splittersList.Add(splitter);
                    }
                }
            }
        }

        private void OnDragStart(Splitter splitter)
        {
            if (_draggingSplitter != null)
            {
                Debug.LogError("Drag splitter already use");
                return;
            }
            
            _draggingSplitter = splitter;
            
            // Получите индекс текущего splitter в списке дочерних элементов
            int splitterIndex = splitter.parent.IndexOf(splitter);

            // Получите левый/верхний элемент (элемент перед splitter'ом)
            VisualElement leftElement = splitter.parent.ElementAt(splitterIndex - 1);

            // Получите правый/нижний элемент (элемент после splitter'а)
            VisualElement rightElement = splitter.parent.ElementAt(splitterIndex + 1);
            
            _listPre.Clear();
            _listPost.Clear();
            
            if (splitter.Direction == SplitterDirection.Vertical)
            {
                GetAllVerticalLastResizibleElements(leftElement);
                GetAllVerticalFirstResizibleElements(rightElement);
            }
            else
            {
                GetAllHorizontalDepthResizibleElements(leftElement);
                GetAllHorizontalSurfaceResizibleElements(rightElement);
            }

            splitter.OnDragUpdateHandler += OnDragUpdate;
            splitter.OnDragEndHandler += OnDragEnd;
        }
        
        
        private void OnDragUpdate(Vector2 position)
        {
            if (_draggingSplitter.Direction == SplitterDirection.Vertical)
            {
                ApplyDeltaToElements(_listPre, _listPost, position.x, true);
            }
            else
            {
                ApplyDeltaToElements(_listPre, _listPost, position.y, false);
            }
        }

        private void ApplyDeltaToElements(List<VisualElement> preElements, List<VisualElement> postElements, float position, bool isVertical)
        {
            float minDelta = float.MaxValue;
            minDelta = CalculateMinDelta(preElements, position, isVertical, minDelta);
            minDelta = CalculateMinDelta(postElements, position, isVertical, minDelta, true);

            UpdateElementSizes(preElements, minDelta, isVertical);
            UpdateElementSizes(postElements, -minDelta, isVertical);
        }

        private float CalculateMinDelta(List<VisualElement> elements, float position, bool isVertical, float minDelta, bool isPost = false)
        {
            foreach (var element in elements)
            {
                var eRect = element.worldBound;
                float minSize = isVertical ? element.resolvedStyle.minWidth.value : element.resolvedStyle.minHeight.value;
                float newSize = Mathf.Max(minSize, (isVertical ? position - eRect.x : position - eRect.y));
                float deltaSize = newSize - (isVertical ? eRect.width : eRect.height);

                if (!isPost)
                {
                    if (deltaSize < minDelta)
                    {
                        minDelta = deltaSize;
                    }
                }
                else
                {
                    float availableSize = (isVertical ? eRect.width : eRect.height) - minSize;
                    if (availableSize < minDelta)
                    {
                        minDelta = availableSize;
                    }
                }
            }
            return minDelta;
        }

        private void UpdateElementSizes(List<VisualElement> elements, float delta, bool isVertical)
        {
            foreach (var element in elements)
            {
                var eRect = element.worldBound;
                if (isVertical)
                {
                    element.style.width = Mathf.Max(element.resolvedStyle.minWidth.value, eRect.width + delta);
                }
                else
                {
                    element.style.height = Mathf.Max(element.resolvedStyle.minHeight.value, eRect.height + delta);
                }
            }
        }


        private void OnDragEnd(Splitter splitter)
        {
            splitter.OnDragUpdateHandler -= OnDragUpdate;
            splitter.OnDragEndHandler -= OnDragEnd;

            _listPost.Clear();
            _listPre.Clear();
            _draggingSplitter = null;
            
            foreach (var s in _splittersList)
            {
                s.ResetAnchor();
            }
        }
        
        // Получаем все элементы первого уровня в горизонтальной линии
        // То есть все элементы находящиеся Сверху
        private void GetAllHorizontalSurfaceResizibleElements(VisualElement parent)
        {
            if (parent is IResizible) _listPost.Add(parent);
            else if (parent is GroupLayout layout)
            {
                if (parent.childCount <= 0) return;
                if (layout.Direction == FlexDirection.Column || layout.Direction == FlexDirection.ColumnReverse)
                {
                    GetAllHorizontalSurfaceResizibleElements(parent[0]);
                }
                else
                {
                    foreach (var child in parent.Children())
                    {
                        GetAllHorizontalSurfaceResizibleElements(child);
                    }

                }
            }
        }
        
        // Получаем все элементы последнего уровня в горизонтальной линии
        // То есть все элементы находящиеся Снизу
        private void GetAllHorizontalDepthResizibleElements(VisualElement parent)
        {
            if (parent is IResizible) _listPre.Add(parent);
            else if (parent is GroupLayout layout)
            {
                if (parent.childCount <= 0) return;
                if (layout.Direction == FlexDirection.Column || layout.Direction == FlexDirection.ColumnReverse)
                {
                    GetAllHorizontalDepthResizibleElements(parent[parent.childCount - 1]);
                }
                else
                {
                    foreach (var child in parent.Children())
                    {
                        GetAllHorizontalDepthResizibleElements(child);
                    }

                }
            }
        }
        
        // Получаем все первые элементы относительно вертикальной линни
        // То есть все элементы находящиеся Слева
        private void GetAllVerticalFirstResizibleElements(VisualElement parent)
        {
            if (parent is IResizible) _listPost.Add(parent);
            else if (parent is GroupLayout layout)
            {
                if (parent.childCount <= 0) return;
                if (layout.Direction == FlexDirection.Column || layout.Direction == FlexDirection.ColumnReverse)
                {
                    foreach (var child in parent.Children())
                    {
                        GetAllVerticalFirstResizibleElements(child);
                    }
                }
                else
                {
                    GetAllVerticalFirstResizibleElements(parent[0]);
                }
            }
        }
        
        // Получаем все последние элементы относительно вертикальной линни
        // То есть все элементы находящиеся Справа
        private void GetAllVerticalLastResizibleElements(VisualElement parent)
        {
            if (parent is IResizible) _listPre.Add(parent);
            else if (parent is GroupLayout layout)
            {
                if (parent.childCount <= 0) return;
                if (layout.Direction == FlexDirection.Column || layout.Direction == FlexDirection.ColumnReverse)
                {
                    foreach (var child in parent.Children())
                    {
                        GetAllVerticalLastResizibleElements(child);
                    }
                }
                else
                {
                    GetAllVerticalLastResizibleElements(parent[parent.childCount - 1]);
                }
            }
        }
    }
}