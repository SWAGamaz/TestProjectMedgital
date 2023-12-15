using UnityEngine.UIElements;

namespace UI
{
    public class GroupLayout : VisualElement, IGroup
    {
        #region Properties
    
        public FlexDirection Direction { 
            get => this.resolvedStyle.flexDirection;
            set => SetDirection(value);
        }
        
        #endregion

        public GroupLayout(FlexDirection flexDirection)
        {
            this.style.flexDirection = flexDirection;
        }

        public void SetDirection(FlexDirection flexDirection)
        {
            if (flexDirection == Direction) return;
        }
    }
}
