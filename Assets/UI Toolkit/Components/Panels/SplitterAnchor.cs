using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class SplitterAnchor : VisualElement
    {
        #region Properties
        
        public SplitterManipulator Manipulator => _manipulator;
        
        #endregion
        
        #region Private fields

        private Splitter _target;
        private static float _pixelWidth = 11f;
        private static float _pixelCenter = 5f; // Floor to Int (_pixelWidth / 2)
        private SplitterManipulator _manipulator;
        
        #endregion

        public SplitterAnchor(Splitter target)
        {
            _target = target;

            _manipulator = new SplitterManipulator(this);
            _manipulator.OnDragUpdateHandler += OnDragUpdateSplitter;
            this.AddManipulator(_manipulator);
            
            target.RegisterCallback<GeometryChangedEvent>(OnSplitterChanged);
            ResetAnchor();
        }

        private void OnSplitterChanged(GeometryChangedEvent evt)
        {
            ResetAnchor();
        }

        private void OnDragUpdateSplitter(Vector2 position)
        {
            if (_target.Direction == SplitterDirection.Vertical)
                this.style.left = position.x - _pixelCenter;
            else
                this.style.top = position.y - _pixelCenter;
        }

        public void ResetAnchor()
        {
            Workspace.Instance.Add(this);
            
            bool isVert = _target.Direction == SplitterDirection.Vertical;
            Rect rect = _target.worldBound;
            
            this.style.width = isVert? _pixelWidth : rect.width;
            this.style.height = isVert? rect.height : _pixelWidth;
            this.style.top = rect.y - (isVert? 0 : _pixelCenter);
            this.style.left = rect.x - (isVert? _pixelCenter : 0);;
        }
    }
}

