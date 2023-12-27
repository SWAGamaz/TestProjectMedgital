using System;
using System.Collections.Generic;
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
        private Inspector _inspector;
        
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
            
            _inspector = new Inspector();

            if (axial == null || coronal == null || sagittal == null)
            {
                throw new NullReferenceException("Не удалось создать проеции срезов");
            }
            
            vertical1.Add(axial);
            vertical1.Add(coronal);
            vertical2.Add(perspective);
            vertical2.Add(sagittal);

            this.Add(vertical1);
            this.Add(vertical2);
            this.Add(_inspector);
            
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
            
            this.RegisterCallback<GeometryChangedEvent>(WorkspaceChangeSize);
        }
        
        private void WorkspaceChangeSize(GeometryChangedEvent evt)
        {
            foreach (var element in _resizibleList)
            {
                if (element.parent is GroupLayout layout)
                {
                    element.style.flexBasis = 
                        layout.Direction == FlexDirection.Column || layout.Direction == FlexDirection.ColumnReverse 
                        ? element.resolvedStyle.height 
                        : element.resolvedStyle.width;
                }
                else if(element.parent == this)
                {
                    element.style.flexBasis = element.resolvedStyle.width;
                }
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

            _listPre.Add(leftElement);
            _listPost.Add(rightElement);
            
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
                float minSize = isVertical ? element.style.minWidth.value.value : element.style.minHeight.value.value;
                float newSize = element.resolvedStyle.flexBasis.value + delta;
                if (newSize < minSize)
                {
                    element.style.flexShrink = 0;
                    element.style.flexBasis = minSize;
                }
                else
                {
                    if (element.style.flexShrink != 1 && element != _inspector) element.style.flexShrink = 1;
                    element.style.flexBasis = element.resolvedStyle.flexBasis.value + delta;
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
    }
}